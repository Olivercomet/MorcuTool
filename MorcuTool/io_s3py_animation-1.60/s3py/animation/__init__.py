from io import SEEK_CUR, SEEK_SET, BytesIO
import math
from s3py.core import Serializable, PackedResource, ExternalResource, ResourceKey
from s3py.helpers import get_subclasses, FNV64, first, Enum
from s3py.io import StreamReader, StreamWriter, StreamPtr, RCOL


class TrackMask(RCOL):
    ID = 0x033260E3
    TAG = 'TkMk'
    RESERVED_COUNT = 28

    class VERSION:
        DEFAULT = 0x200

    def __init__(self, key):
        self.version = self.VERSION.DEFAULT
        self.rig = ExternalResource(ResourceKey())
        self.unknown = 1.0
        self.reserved = [0] * self.RESERVED_COUNT
        self.bone_weights = []
        RCOL.__init__(self, key)

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        self.rig.key = s.tgi('ITG')
        self.unknown = s.f32()
        for i in range(self.RESERVED_COUNT): self.reserved[i] = s.u8()
        cValues = s.u32()
        self.bone_weights = [0.0] * cValues
        for i in range(cValues): self.bone_weights[i] = s.f32()

    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        s.tgi(self.rig.key, 'ITG')
        s.f32(self.unknown)
        for i in range(self.RESERVED_COUNT): s.u8(self.reserved[i])
        s.u32(len(self.bone_weights))
        for val in self.bone_weights: s.f32(val)


class Frame:
    __slots__ = {
        'data',
        'flags',
        'frame_index'
    }

    def __init__(self):
        self.data = []
        self.flags = 0
        self.frame_index = 0

    @staticmethod
    def unpack(unpacked, offset, scale):
        return (unpacked * scale) + offset

    @staticmethod
    def pack(packed, offset, scale):
        return (packed - offset) / scale

    def write(self, stream, cdi, floats):
        s = StreamWriter(stream)
        s.u16(self.frame_index)
        flags = self.flags << 4
        indices = []
        packedVals = []
        if  cdi.flags.type == CurveDataType.VECTOR3_INDEXED:
            for i in range(cdi.float_count()):
                packedIndex = Frame.pack(self.data[i], cdi.offset, cdi.scale)
                if packedIndex < 0: flags |= 1 << i
                if not packedIndex in floats: floats.append(packedIndex)
                indices[i] = floats.index(packedIndex)
            s.u16(flags)
            for f in indices: s.u16(f)
        else:
            bitsPerFloat = cdi.bits_per_float()
            maxPackedVal = math.pow(2, bitsPerFloat) - 1.0
            for packedWritten in range(cdi.packed_count()):
                packed = 0
                for packedIndex in range(int(cdi.float_count() / cdi.packed_count())):
                    floatIndex = packedWritten + packedIndex
                    val = Frame.pack(self.data[floatIndex], cdi.offset, cdi.scale)
                    if val < 0: flags |= 1 << floatIndex
                    val = math.fabs(val)
                    packed |= int(math.floor(val * maxPackedVal)) << (packedIndex * bitsPerFloat )
                packedVals.append(packed)
            s.u16(flags)
            for f in packedVals:
                if cdi.flags.type == CurveDataType.VECTOR3_PACKED: s.u32(f)
                elif cdi.flags.type in ( CurveDataType.VECTOR4_PACKED, CurveDataType.SCALAR): s.u16(f)
                else: raise Exception("Unknown packed format type")
        return self

    def read(self, stream, cdi, floats):
        assert isinstance(cdi,CurveDataInfo)
        s = StreamReader(stream)
        self.data = []
        self.frame_index = s.u16()
        flags = s.u16()
        self.flags = flags >> 4
        if  cdi.flags.type == CurveDataType.VECTOR3_INDEXED:
            for floatsRead in range(cdi.float_count()):
                index = s.u16()
                val = floats[index]
                if flags & 1 << floatsRead: val *= -1
                self.data.append(Frame.unpack(val, cdi.offset, cdi.scale))
        else:
            for packedRead in range(cdi.packed_count()):
                if cdi.flags.type == CurveDataType.VECTOR3_PACKED:
                    packed = s.u32()
                elif cdi.flags.type in ( CurveDataType.VECTOR4_PACKED, CurveDataType.SCALAR):
                    packed = s.u16()
                else:
                    raise Exception("Unknown packed format type")
                for packedIndex in range(int(cdi.float_count() / cdi.packed_count())):
                    floatIndex = packedIndex + packedRead
                    bitsPerFloat = cdi.bits_per_float()
                    maxPackedVal = math.pow(2, bitsPerFloat) - 1.0
                    mask = int(maxPackedVal) << (packedIndex * bitsPerFloat)
                    val = ( (packed & mask) >> (packedIndex * bitsPerFloat)) / maxPackedVal
                    if flags & 1 << floatIndex: val *= -1.0
                    self.data.append(Frame.unpack(val, cdi.offset, cdi.scale))
        return self

    def __str__(self):
        return "[%i]: %s" % (self.frame_index, self.data)


class Track:
    def __init__(self, key=0):
        self.track_key = key
        self.curves = {}

    def __eq__(self, other):
        return isinstance(other, Track) and self.track_key == other.track_key
    def __contains__(self, item):
        return item in self.curves
    def __getitem__(self, item):
        return self.curves[item] if item in self.curves else None
    def __setitem__(self, key, value):
        assert isinstance(value,Curve) and value.type == key
        self.curves[key] = value
    def __len__(self):
        return len(self.curves)
    def __hash__(self):
        return hash(self.track_key)
    def __eq__(self, other):
        return isinstance(other,Track) and  self.track_key == other.track_key
    def __iter__(self):
        return iter(self.curves.values())
    def __str__(self):
        return '{%08X} %s' %(self.track_key, str(['{%s}:[%s]'%(c.type,len(c.frames)) for c in self]))
    def add_curve(self,curve):
        assert isinstance(curve,Curve)
        self.curves[curve.type] = curve
    def remove_curve(self,curve):
        if isinstance(curve,Curve):
            curve = curve.type
            self.curves[curve] = None
    def get_pos(self):
        return self[CurveType.POSITION]
    def set_pos(self,value):
        self[CurveType.POSITION] = value
    def get_rot(self):
        return self[CurveType.ORIENTATION]
    def set_rot(self,value):
        self[CurveType.ORIENTATION] = value
    def get_morph(self):
        return self[CurveType.MORPH]
    def set_morph(self,value):
        self[CurveType.MORPH] = value

    positions = property(get_pos,set_pos)
    orientations = property(get_rot,set_rot)
    morphs = property(get_morph,set_morph)



class Curve:
    @staticmethod
    def create_position():
        inst = Curve(CurveType.POSITION)
        inst.flags.type = CurveDataType.VECTOR3_PACKED
        inst.flags.static = False
        inst.flags.unknown = 1
        return inst

    @staticmethod
    def create_orientation():
        inst = Curve(CurveType.ORIENTATION)
        inst.flags.type = CurveDataType.VECTOR4_PACKED
        inst.flags.static = False
        inst.flags.unknown = 1
        return inst

    @staticmethod
    def create_morph():
        inst = Curve(CurveType.MORPH)
        inst.flags.type = CurveDataType.SCALAR
        inst.flags.static = False
        inst.flags.unknown = 0
        return inst

    def __init__(self,type):
        self.type = type
        self.frames = []
        self.flags = CurveFlags()

    def __str__(self):
        return '{%i} Frames[%i] ' % (self.type, len(self.frames))


class CurveFlags(Serializable):
    def __init__(self, stream=None):
        self.type = CurveDataType.VECTOR4_PACKED
        self.static = False
        self.unknown = CurveDataFormat.PACKED
        Serializable.__init__(self, stream)

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        flags = s.u8()
        self.type = (flags & 0x07) >> 0
        self.static = ((flags & 0x08) >> 3) == 1
        self.unknown = (flags & 0xF0) >> 4
        return self

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        flags = 0
        flags |= (self.type << 0)
        if self.static: flags |= (1 << 1)
        flags |= (self.unknown << 4)
        s.u8(flags)
        return self


class CurveDataFormat(Enum):
    INDEXED = 0x00
    PACKED = 0x01


class CurveType(Enum):
    POSITION = 0x01
    ORIENTATION = 0x02
    MORPH = 0x07


class CurveDataType(Enum):
    VECTOR3_PACKED = 0x02
    VECTOR3_INDEXED = 0x03
    VECTOR4_PACKED = 0x04
    SCALAR = 0x05



class CurveDataInfo:
    def __init__(self):
        self.frame_data_ptr = 0
        self.track_key = 0
        self.offset = 0.0
        self.scale = 1.0
        self.frame_count = 0
        self.flags = CurveFlags()
        self.type = CurveType.POSITION

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.frame_data_ptr = StreamPtr.begin_read(s)
        self.track_key = s.u32()
        self.offset = s.f32()
        self.scale = s.f32()
        self.frame_count = s.u16()
        self.flags.read(stream)
        self.type = s.u8()
        return self

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        self.frame_data_ptr = StreamPtr.begin_write(s)
        s.u32(self.track_key)
        s.f32(self.offset)
        s.f32(self.scale)
        s.u16(self.frame_count)
        self.flags.write(stream)
        s.u8(self.type)
        return self


    def process_frames(self, frames):
        self.scale = 1.0
        self.offset = 0.0
        min = 0.0
        max = 0.0
        for f in frames:
            for val in f.data:
                if val > max: max = val
                if val < min: min = val
        self.offset = (min + max) / 2.0
        self.scale = (min - max) / 2.0
        if self.scale == 0.0:
            self.scale = 1.0


    def float_count(self):
        if self.flags.type == CurveDataType.SCALAR:
            return 1
        if self.flags.type in (CurveDataType.VECTOR3_INDEXED, CurveDataType.VECTOR3_PACKED):
            return 3
        elif self.flags.type == CurveDataType.VECTOR4_PACKED:
            return 4
        else:
            raise Exception("Unknown frame type")

    def bits_per_float(self):
        if self.flags.type == CurveDataType.SCALAR:
            return 16
        elif self.flags.type in (CurveDataType.VECTOR3_INDEXED, CurveDataType.VECTOR3_PACKED):
            return 10
        elif self.flags.type == CurveDataType.VECTOR4_PACKED:
            return 12
        else:
            raise Exception("Unknown frame type")

    def packed_count(self):
        if self.flags.type in (CurveDataType.VECTOR3_INDEXED, CurveDataType.VECTOR3_PACKED,CurveDataType.SCALAR):
            return 1
        elif self.flags.type == CurveDataType.VECTOR4_PACKED:
            return 4
        else:
            raise Exception("Unknown frame type")


class Clip(Serializable):
    def __init__(self, stream=None):
        self.version = 2
        self.frame_duration = 1.0/30.0
        self.max_frame_count = 0
        self.name = ''
        self.source_file_name = ''
        self.unknown1 = 0
        self.unknown2 = 0x6C73
        self.tracks = []
        Serializable.__init__(self, stream)

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        indexedFloats = []
        curves = []
        curveMap = {}
        for t in self.tracks:
            assert isinstance(t,Track)
            for curve in t:
                assert isinstance(curve,Curve)
                if len(curve.frames) > self.max_frame_count: self.max_frame_count = len(curve.frames) + 1
                cdi = CurveDataInfo()
                curve.flags.static = len(curve.frames) == 0
                cdi.frame_count = len(curve.frames)
                cdi.process_frames(curve.frames)
                cdi.flags = curve.flags
                cdi.type = curve.type
                cdi.track_key = t.track_key
                curveMap[cdi] = curve.frames
                curves.append(cdi)

        cCurves = len(curves)
        cFloats = len(indexedFloats)

        #Begin writing...
        s.chars('_pilC3S_')
        s.u32(self.version)
        s.u32(self.unknown1)
        s.f32(self.frame_duration)
        s.u16(self.max_frame_count)
        s.u16(self.unknown2)

        s.u32(cCurves)
        s.u32(cFloats)
        curveOffset = StreamPtr.begin_write(s)
        frameOffset = StreamPtr.begin_write(s)
        nameOffset = StreamPtr.begin_write(s)
        srcNameOffset = StreamPtr.begin_write(s)

        curveOffset.end()
        for curve in curves:
            curve.write(stream)
        nameOffset.end()
        s.zs(self.name)
        srcNameOffset.end()
        s.zs(self.source_file_name)

        frameOffset.end()
        for curve in curves:
            curve.frame_data_ptr.end()
            frames = curveMap[curve]
            for f in frames: f.write(stream, curve, indexedFloats)
        return self

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        tag = s.chars(8)
        if not tag == '_pilC3S_': raise Exception("Not a valid _S3Clip_")
        self.version = s.u32()
        self.unknown1 = s.u32()
        self.frame_duration = s.f32()
        self.max_frame_count = s.u16()
        self.unknown2 = s.u16()
        cCurves = s.u32()
        cFloats = s.u32()
        curveOffset = StreamPtr.begin_read(s)
        frameOffset = StreamPtr.begin_read(s)
        nameOffset = StreamPtr.begin_read(s)
        srcNameOffset = StreamPtr.begin_read(s)

        curveOffset.end()
        curves = []
        for curveIndex in range(cCurves):
            cdi = CurveDataInfo()
            cdi.read(stream)
            curves.append(cdi)
        nameOffset.end()
        self.name = s.zs()

        srcNameOffset.end()
        self.source_file_name = s.zs()

        frameOffset.end()
        indexedFloats = []
        for floatIndex in range(cFloats):
            indexedFloats.append(s.f32())
        trackMap = {}
        self.tracks = []
        for curveIndex,cdi in enumerate(curves):
            cdi.frame_data_ptr.end()
            if cdi.track_key not in trackMap.keys():
                t = Track(cdi.track_key)
                trackMap[cdi.track_key] = t
                self.tracks.append(t)
            track = trackMap[cdi.track_key]
            frames = []
            for frameIndex in range(cdi.frame_count):
                f = Frame()
                f.read(stream, cdi, indexedFloats)
                frames.append(f)
            curve = Curve(cdi.type)
            curve.flags = cdi.flags
            curve.frames = frames
            track[curve.type] = curve
        return self

class ClipResource(PackedResource):
    ID = 0x6B20C4F3
    PADDING_CHAR = 0x7E


    @classmethod
    def create_key(cls,name):
        assert len(name) > 4
        s = name.split('_',1)
        assert len(s[0]) >=1

        flags = {
            'b' : 0x01,
            'p' : 0x02,
            'c' : 0x03,
            't' : 0x04,
            'h' : 0x05,
            'e' : 0x06,
            'ad': 0x08,
            'cd': 0x09,
            'al': 0x0A,
            'ac': 0x0D,
            'cc': 0x0E,
            'ah': 0x10,
            'ch': 0x11,
            'ab': 0x12,
            'ar': 0x13
        }

        generic_name = name
        generic_actor_types = {'a','o'}
        def get_generic_actor_type(s): return 'o' if s == 'o' else 'a'

        actors = s[0].split('2',1)
        mask = 0
        xAge = 0
        yAge = 0
        if actors[0] in flags:
            xAge = flags[actors[0]]
        if len(actors) >1:
            if not  actors[0] in generic_actor_types or not actors[1] in generic_actor_types:
                if actors[1] in flags:
                    yAge = flags[actors[1]]
                generic_name = '%s2%s_%s'% (get_generic_actor_type(actors[0]),get_generic_actor_type(actors[1]),s[1])
                mask = 0x8000 | xAge << 8 | yAge
        elif not actors[0] in generic_actor_types:
            generic_name = 'a_'+ s[1]
            mask = 0x8000 | xAge<<8
        instance = FNV64.hash(generic_name)
        instance &=0x7FFFFFFFFFFFFFFF
        instance ^= mask << 48
        group = 0x48000000 if xAge > 0x06 or yAge >0x06 else 0
        return ResourceKey(cls.ID,group,instance)

    def __init__(self, key=None):
        PackedResource.__init__(self, key)
        self.actor_name = None
        self.unknown1 = 0
        self.unknown2 = 0
        self.clip = Clip()
        self.ik_info = ClipIKInfo()
        self.event_table = ClipEventTable()
        self.vector = [0.0, 0.0, 0.0, 1.0]
        self.unknown3 = [0] * 16


    def read(self, stream, resources=None):
        s = StreamReader(stream)
        if s.u32() != self.ID: raise Exception("Not a valid clip resource")
        if s.u32(): raise Exception("Linked clip offset not supported")
        clipSize = s.u32()
        clip_ptr = StreamPtr.begin_read(s, True)
        ik_ptr = StreamPtr.begin_read(s, True)
        actor_ptr = StreamPtr.begin_read(s, True)
        event_ptr = StreamPtr.begin_read(s, True)
        self.unknown1 = s.u32()
        self.unknown2 = s.u32()
        vector_ptr = StreamPtr.begin_read(s, True)
        self.unknown3 = []
        for i in range(16): self.unknown3.append(s.u8())

        a= clip_ptr.seek_data()
        clipStream = BytesIO()
        clipStream.write(stream.read(clipSize))
        clipStream.seek(0, SEEK_SET)
        self.clip = Clip()
        self.clip.read(clipStream)
        clipStream.close()

        assert actor_ptr.seek_data()
        self.actor_name = s.zs()

        if ik_ptr.seek_data():
            self.ik_info = ClipIKInfo()
            self.ik_info.read(stream)
        else:
            self.ik_info = None

        assert event_ptr.seek_data()
        self.event_table.read(stream)

        self.vector = []
        assert vector_ptr.seek_data()
        for i in range(4): self.vector.append(s.f32())

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.ID)
        s.u32(0)
        clip_stream = BytesIO()
        self.clip.write(clip_stream)
        s.u32(clip_stream.tell())
        clip_stream.seek(0, SEEK_SET)
        clip_ptr = StreamPtr.begin_write(s, True)
        ik_ptr = StreamPtr.begin_write(s, True)
        actor_ptr = StreamPtr.begin_write(s, True)
        event_ptr = StreamPtr.begin_write(s, True)
        s.u32(self.unknown1)
        s.u32(self.unknown2)
        vector_ptr = StreamPtr.begin_write(s, True)
        for i in range(16): s.u8(self.unknown3[i])
        clip_ptr.end()
        stream.write(clip_stream.read())
        s.align(char=self.PADDING_CHAR)
        if self.ik_info != None:
            ik_ptr.end()
            self.ik_info.write(stream)
            s.align(char=self.PADDING_CHAR)
        actor_ptr.end()
        s.zs(self.actor_name)
        s.align(char=self.PADDING_CHAR)
        event_ptr.end()
        self.event_table.write(stream)
        s.align(char=self.PADDING_CHAR)
        vector_ptr.end()
        for i in range(4): s.f32(self.vector[i])


class Event(Serializable):
    TYPE = 0

    TAG = 0xC1E4

    @staticmethod
    def get_event_class(type):
        for cls in Event.__subclasses__():
            if cls.TYPE == type:
                return cls
        raise Exception('Unable to find class for event type 0x%08X' % type)

    def __init__(self, stream=None):
        self.id = 0
        self.time_code = 0.0
        self.unknown1 = -1.0
        self.unknown2 = -1.0
        self.unknown3 = 0
        self.name = ""
        Serializable.__init__(self, stream)

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        assert s.u16() == self.TAG
        self.id = s.u32()
        self.time_code = s.f32()
        self.unknown1 = s.f32()
        self.unknown2 = s.f32()
        self.unknown3 = s.u32()
        length = s.u32()
        self.name = s.zs()
        assert len(self.name) == length
        s.align()

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        s.u16(self.TAG)
        s.u32(self.id)
        s.f32(self.time_code)
        s.f32(self.unknown1)
        s.f32(self.unknown2)
        s.u32(self.unknown3)
        s.u32(len(self.name))
        s.zs(self.name)
        s.align()


class EventAttachObject(Event):
    TYPE = 1

    def __init__(self):
        Event.__init__(self)
        self.prop_actor_name = None
        self.object_actor_name = None
        self.slot_name = None
        self.unknown4 = 0
        self.matrix =\
        [
            [1.0, 0.0, 0.0, 0.0],
            [0.0, 1.0, 0.0, 0.0],
            [0.0, 0.0, 1.0, 0.0],
            [0.0, 0.0, 0.0, 1.0],
        ]

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.prop_actor_name = s.u32()
        self.object_actor_name = s.u32()
        self.slot_name = s.u32()
        self.unknown4 = s.u32()
        self.matrix = [[s.f32() for i in range(4)] for i in range(4)]

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.hash(self.prop_actor_name)
        s.hash(self.object_actor_name)
        s.hash(self.slot_name)
        s.u32(self.unknown4)

        for i in range(4):
            for j in range(4): s.f32(self.matrix[i][j])


class EventUnparent(Event):
    TYPE = 2

    def __init__(self):
        Event.__init__(self)
        self.object_actor_name = None

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.object_actor_name = s.u32()

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.hash(self.object_actor_name)


class EventPlaySound(Event):
    TYPE = 3

    def __init__(self):
        Event.__init__(self)
        self.sound_name = None

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.sound_name = s.zs()
        s.seek(127 - len(self.sound_name), SEEK_CUR)

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.zs(self.sound_name)
        for i in range(127 - len(self.sound_name)): s.u8(0)


class EventSACS(Event):
    TYPE = 4

    def __init__(self): Event.__init__(self)

    def read(self, stream, resource=None): Event.read(self, stream)

    def write(self, stream, resource=None): Event.write(self, stream)


class EventStartEffect(Event):
    TYPE = 5

    def __init__(self):
        Event.__init__(self)
        self.unknown4 = 0
        self.unknown5 = 0
        self.effect_name = None
        self.actor_name = None
        self.slot_name = None
        self.unknown6 = 0

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.unknown4 = s.u32()
        self.unknown5 = s.u32()
        self.effect_name = s.u32()
        self.actor_name = s.u32()
        self.slot_name = s.u32()
        self.unknown6 = s.u32()

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.u32(self.unknown4)
        s.u32(self.unknown5)
        s.hash(self.effect_name)
        s.hash(self.actor_name)
        s.hash(self.slot_name)
        s.u32(self.unknown6)


class EventVisibility(Event):
    TYPE = 6

    def __init__(self):
        Event.__init__(self)
        self.visibility = 1.0

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.visibility = s.f32()

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.f32(self.visibility)


class EventDestroyProp(Event):
    TYPE = 9

    def __init__(self):
        Event.__init__(self)
        self.prop_actor_name = None

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.prop_actor_name = s.u32()

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.hash(self.prop_actor_name)


class EventStopEffect(Event):
    TYPE = 10

    def __init__(self):
        Event.__init__(self)
        self.effect_name = None
        self.unknown4 = 0

    def read(self, stream, resource=None):
        Event.read(self, stream)
        s = StreamReader(stream)
        self.effect_name = s.u32()
        self.unknown4 = s.u32()

    def write(self, stream, resource=None):
        Event.write(self, stream)
        s = StreamWriter(stream)
        s.hash(self.effect_name)
        s.u32(self.unknown4)


class ClipEventTable(Serializable):
    EVENT_CLASSES = None
    TAG = '=CE='

    class VERSION:
        DEFAULT = 0x00000103

    def get_event_class(self, type):
        if self.EVENT_CLASSES == None:
            self.EVENT_CLASSES = {}
            classes = get_subclasses(Event)
            for c in classes:
                if c.TYPE: self.EVENT_CLASSES[c.TYPE] = c
        if not type in self.EVENT_CLASSES: raise Exception("Clip Event Type %x is not supported!" % type)
        return self.EVENT_CLASSES[type]

    def __init__(self, stream=None):
        self.version = self.VERSION.DEFAULT
        self.events = []
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        assert s.chars(4) == self.TAG
        self.version = s.u32()
        cEvents = s.i32()
        length = s.u32()
        start_offset = s.u32()
        assert (start_offset == 4 and cEvents > 0) or (start_offset == 0 == cEvents)
        self.events = []
        start = stream.tell()
        for event_index in range(cEvents):
            type = s.u16()
            event = Event.get_event_class(type)()
            event.read(stream)
            self.events.append(event)
        actual = stream.tell() - start
        #assert actual == length

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.chars(self.TAG)
        s.u32(self.version)
        s.i32(len(self.events))
        size_offset = stream.tell()
        s.u32(0)
        s.u32(0 if not len(self.events) else 4)
        start_pos = stream.tell()
        for event in self.events:
            s.u16(event.TYPE)
            event.write(stream)
        end_pos = stream.tell()
        stream.seek(size_offset, SEEK_SET)
        s.u32(end_pos - start_pos)
        stream.seek(end_pos, SEEK_SET)


class IKSlotTarget(Serializable):
    BYTE_SIZE = 1028

    def __init__(self, stream=None):
        self.index = 0
        self.slot_target_namespace = None
        self.slot_target_bone = None
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.index = s.i32()
        self.slot_target_namespace = s.zs()
        stream.seek(511 - len(self.slot_target_namespace), SEEK_CUR)
        self.slot_target_bone = s.zs()
        stream.seek(511 - len(self.slot_target_bone), SEEK_CUR)

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.index)
        s.zs(self.slot_target_namespace)
        for i in range(511 - len(self.slot_target_namespace)): s.u8(0x23)
        s.zs(self.slot_target_bone)
        for i in range(511 - len(self.slot_target_bone)): s.u8(0x23)

    def __str__(self):
        return "%s:%s" % (self.slot_target_namespace, self.slot_target_bone)


class ClipIKInfo(Serializable):
    def __init__(self, stream=None):
        self.chains = []
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        chain_offsets = []
        cChains = s.i32()
        start_chains = stream.tell()
        self.chains = []
        for chain_index in range(cChains):
            chain_offsets.append(start_chains + s.u32())
        for chain_index in range(cChains):
            assert stream.tell() == chain_offsets[chain_index]
            assert s.u32() == 0x7e7e7e7e
            cTargets = s.i32()
            start_targets = stream.tell()
            target_offsets = []
            targets = []
            for target_index in range(cTargets):
                target_offsets.append(start_targets + s.u32())
            for target_index in range(cTargets):
                assert stream.tell() == target_offsets[target_index]
                target = IKSlotTarget()
                target.read(stream)
                targets.append(target)
            self.chains.append(targets)

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.i32(len(self.chains))
        chain_pos = (4 * len(self.chains))
        for targets in self.chains:
            s.u32(chain_pos)
            chain_pos += 8 + ( (4 + IKSlotTarget.BYTE_SIZE) * len(targets))
        for targets in self.chains:
            s.u32(0x7e7e7e7e)
            s.i32(len(targets))
            target_pos = 4 * len(targets)
            for target_index in range(len(targets)):
                s.u32(target_pos)
                target_pos += IKSlotTarget.BYTE_SIZE
            for target in targets:
                target.write(stream)



