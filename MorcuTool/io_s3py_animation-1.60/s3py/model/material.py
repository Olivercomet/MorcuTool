from io import SEEK_SET, SEEK_CUR, BytesIO
from s3py.core import PackedResource, ExternalResource, ResourceKey
from s3py.helpers import get_subclasses, FNV32, HashedString
from s3py.io import StreamReader, StreamWriter, StreamPtr, TGIList, RCOL


class MaterialBlock(object):
    def __init__(self):
        self.__parameters = {}

    TAG_TEXTURES = 'MTNF'
    TAG_NO_TEXTURES = 'MTRL'

    def __setitem__(self, key, value):
        hash = HashedString(key)
        self.__parameters[hash] = value

    def __getitem__(self, item):
        hash = HashedString(item)
        return None if not hash in self.__parameters else self.__parameters[hash]

    def __iter__(self):
        return self.__parameters

    def __contains__(self, item):
        hash = HashedString(item)
        return hash in self.__parameters

    def __len__(self):
        return len(self.__parameters.keys())


    def read(self, stream, keys):
        s = StreamReader(stream)
        tag = s.chars(4)
        if not tag == self.TAG_TEXTURES or tag == self.TAG_NO_TEXTURES:
            raise IOError(
                "Invalid data, expected %s or %s, but got %s" % (self.TAG_TEXTURES, self.TAG_NO_TEXTURES, tag))
        zero = s.u32()
        assert zero == 0

        param_len = s.u32()
        cParams = s.i32()
        items = []
        for i in range(cParams):
            item = self.Item()
            item.read_pointer(stream)
            items.append(item)
        start = stream.tell()
        for item in items:
            self.__parameters[item.name] = item.read_data(stream, keys)
        end = stream.tell()
        assert (end - start) == param_len

    def write(self, stream, keys):
        items = []
        for key in self.__parameters.keys():
            val = self.__parameters[key]
            item = self.Item(key, val)
            items.append(item)

        s = StreamWriter(stream)
        s.chars(self.TAG_TEXTURES)
        s.u32(0)
        data_len_ptr = stream.tell()
        s.u32(0)
        s.u32(len(items))

        for item in items: item.write_pointer(stream)
        start = stream.tell()
        for item in items: item.write_data(stream, keys)
        end = stream.tell()
        stream.seek(data_len_ptr, SEEK_SET)
        s.u32(end - start)
        stream.seek(end, SEEK_SET)

    class Item(object):
        class TYPE:
            FLOAT = 1
            INT = 2
            MATRIX = 3
            TEXTURE = 4

        def __init__(self, name=None, value=None):
            self.name = name
            self.type_code = 0
            self.size32 = 0
            self.pointer = None
            self.value = value

        def read_pointer(self, stream):
            s = StreamReader(stream)
            self.name = s.u32()
            self.type_code = s.u32()
            self.size32 = s.u32()
            self.pointer = StreamPtr.begin_read(s)

        def write_pointer(self, stream):
            self.size32 = 1
            t = type(self.value)
            if isinstance(self.value, list):
                self.size32 = len(self.value)
                t = type(self.value[0])
            if t == float:
                self.type_code = self.TYPE.FLOAT
            elif t == int:
                self.type_code = self.TYPE.INT
            elif  t == ExternalResource:
                self.type_code = self.TYPE.TEXTURE
            elif t == ResourceKey:
                self.size32 = 4
                self.type_code = self.TYPE.TEXTURE
            else:
                raise NotImplementedError("Serialization of type %s is not supported in this format!" % t)
            s = StreamWriter(stream)
            s.hash(self.name)
            s.u32(self.type_code)
            s.u32(self.size32)
            self.pointer = StreamPtr.begin_write(s)

        def write_data(self, stream, keys):
            s = StreamWriter(stream)
            self.pointer.end()
            if self.type_code == self.TYPE.FLOAT:
                if self.size32 == 1:
                    s.f32(self.value)
                else:
                    for f in self.value: s.f32(f)
            elif self.type_code == self.TYPE.INT:
                if self.size32 == 1:
                    s.i32(self.value)
                else:
                    for i in self.value: s.i32(i)
            elif self.type_code == self.TYPE.TEXTURE:
                if not keys == None:
                    s.u32(keys.get_resource_index(self.value))
                    s.u32(0)
                    s.u32(0)
                    s.u32(0)
                else:
                    s.tgi(self.value, 'ITG')
                    s.u32(0)

        def read_data(self, stream, keys=None):
            s = StreamReader(stream)
            if self.type_code == self.TYPE.FLOAT:
                return s.f32() if self.size32 == 1 else [s.f32() for i in range(self.size32)]
            if self.type_code == self.TYPE.INT:
                return s.i32() if self.size32 == 1 else[s.i32() for i in range(self.size32)]
            if self.type_code == self.TYPE.TEXTURE:
                if self.size32 == 4:
                    val = keys.get_resource(s.u32())
                    stream.seek(12, SEEK_CUR)
                    return val
                elif self.size32 == 5:
                    key = s.tgi('ITG')
                    stream.seek(4, SEEK_CUR)
                    return key


class AnimatedTexture(RCOL):
    TAG = 'ANIM'
    ID = 0x63A33EA7

    class VERSION:
        DEFAULT = 0x00000001

    def __init__(self, key):
        self.version = self.VERSION.DEFAULT
        self.framerate = 6.0
        self.frames = []
        RCOL.__init__(self, key)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.framerate = s.f32()
        cFrames = s.i32()
        offsets = [s.u32() for i in range(cFrames)]
        offsets.append(-1)
        for frame_index in range(cFrames):
            offset = offsets[frame_index]
            next = offsets[frame_index + 1]
            cBytes = next - offset if next > 0 else -1
            data = stream.read(cBytes)
            self.frames.append(data)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.f32(self.framerate)
        cFrames = len(self.frames)
        s.i32(cFrames)
        data_pos = stream.tell() + (cFrames * 4)
        for frame in self.frames:
            s.u32(data_pos)
            data_pos += len(frame)
        for frame in self.frames:
            stream.write(frame)


class MaterialDefinition(RCOL):
    TAG = 'MATD'
    ID = 0x01D0E75D

    def __init__(self, key):
        RCOL.__init__(self, key)
        self.version = self.VERSION.STANDARD
        self.is_video_surface = False
        self.is_painting_surface = False
        self.material_block = MaterialBlock()
        self.resource_name = None
        self.shader_name = None

    class VERSION:
        OLD = 0x102
        STANDARD = 0x103

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        self.name = s.u32()
        self.shader_name = s.u32()
        data_len = s.u32()
        if self.version >= self.VERSION.STANDARD:
            self.is_video_surface = s.u32() > 0
            self.is_painting_surface = s.u32() > 0
        material_block_stream = BytesIO()
        material_block_stream.write(stream.read(data_len))
        material_block_stream.seek(0, SEEK_SET)
        self.material_block.read(material_block_stream, rcol)

    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        s.hash(self.name)
        s.hash(self.shader_name)
        material_block_stream = BytesIO()
        self.material_block.write(material_block_stream, rcol)
        s.u32(material_block_stream.tell())
        if self.version >= self.VERSION.STANDARD:
            s.u32(0 if not self.is_video_surface else 1)
            s.u32(0 if not self.is_painting_surface else 1)
        material_block_stream.seek(0, SEEK_SET)
        stream.write(material_block_stream.read(-1))


class MaterialSet(RCOL):
    TAG = 'MTST'
    ID = 0x02019972
    NAMES = {
        0x2EA8FB98: 'default',
        0xEEAB4327: 'dirty',
        0x2E5DF9BB: 'verydirty',
        0xC3867C32: 'burnt',
        0x257FB026: 'clogged'
    }

    class Element(object):
        def __init__(self, name=None, material=None):
            self.name = name
            self.material = material

        def __str__(self): return "%s : %s" % ( self.name, self.material)

    def __init__(self, key=None, stream=None, resources=None):
        self.version = 0
        self.elements = []
        self.default_material = self.Element()
        RCOL.__init__(self, key, stream, resources)


    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        name = s.hash(self.NAMES)
        value = rcol.get_block(s.u32(),(MaterialDefinition, MaterialSet))
        self.default_material = self.Element(name, value)
        self.elements = []
        cItems = s.u32()
        for i in range(cItems):
            value = rcol.get_block(s.u32(), (MaterialDefinition, MaterialSet))
            name = s.hash(self.NAMES)
            self.elements.append(self.Element(name, value))


    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        s.hash(self.default_material.name)
        s.u32(rcol.get_block_index(self.default_material.material))
        s.u32(len(self.elements))
        for state in self.elements:
            s.u32(rcol.get_block_index(state.material))
            s.hash(state.name)


class ShaderModels(object):
    SM_1_0 = 0x00000000
    SM_1_1 = 0x00000001
    SM_2_0 = 0x00000002
    SM_3_0 = 0x00000003
    SM_Highest = 0x7FFFFFFF


class RenderTexture(object):
    A = 0x21E9CD3
    B = 0x21E9CD5


class TextureStep():
    class Param():
        TYPE = -1
        PARAM_TYPES = None

        def __init__(self, name, value=None):
            self.name = HashedString(name)
            self.value = value

        def __str__(self):
            return "%s = %s" % (self.name, self.value)

        def read(self, stream, tgi):
            self.value = self.read_value(StreamReader(stream), tgi)

        def write(self, stream, tgi):
            self.write_value(StreamWriter(stream), self.value, tgi)

        def read_value(self, s, tgi):
            raise NotImplementedError()

        def write_value(self, s, value, tgi):
            raise NotImplementedError()

        @staticmethod
        def get_type(typecode):
            if TextureStep.Param.PARAM_TYPES == None:
                TextureStep.Param.PARAM_TYPES = {}
                for c in get_subclasses(TextureStep.Param):
                    TextureStep.Param.PARAM_TYPES[c.TYPE] = c
            return TextureStep.Param.PARAM_TYPES[typecode]

    class BoolParam(Param):
        TYPE = 0

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return bool(s.u8())

        def write_value(self, s, value, tgi):
            s.u8(value)

    class ByteParam(Param):
        TYPE = 1

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.i8()

        def write_value(self, s, value, tgi):
            s.i8(value)

    class Int16Param(Param):
        TYPE = 2

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.i16()

        def write_value(self, s, value, tgi):
            s.i16(value)

    class Int32Param(Param):
        TYPE = 3

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.i32()

        def write_value(self, s, value, tgi):
            s.i32(value)

    class Int64Param(Param):
        TYPE = 4

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.i64()

        def write_value(self, s, value, tgi):
            s.i64(value)

    class UByteParam(Param):
        TYPE = 5

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.u8()

        def write_value(self, s, value, tgi):
            s.u8(value)

    class UInt16Param(Param):
        TYPE = 6

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.u16()

        def write_value(self, s, value, tgi):
            s.u16(value)

    class UInt32Param(Param):
        TYPE = 7

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.u32()

        def write_value(self, s, value, tgi):
            s.u32(value)

    class UInt64Param(Param):
        TYPE = 8

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.u64()

        def write_value(self, s, value, tgi):
            s.u64(value)

    class FloatParam(Param):
        TYPE = 9

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.f32()

        def write_value(self, s, value, tgi):
            s.f32(value)

    class RectParam(Param):
        TYPE = 10

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return [s.f32() for i in range(4)]

        def write_value(self, s, value, tgi):
            for i in range(4): s.f32(value[i])

    class Vector4Param(Param):
        TYPE = 11

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return [s.f32() for i in range(4)]

        def write_value(self, s, value, tgi):
            for i in range(4): s.f32(value[i])

    class ResourceIndexParam(Param):
        TYPE = 12

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return tgi.get_resource(s.i8())

        def write_value(self, s, value, tgi):
            s.i8(tgi.get_resource_index(value))

    class StringParam(Param):
        TYPE = 13

        def __init__(self, name, value=None):
            Param.__init__(self, name, value)

        def read_value(self, s, tgi):
            return s.p16()

        def write_value(self, s, value, tgi):
            s.p16(value)


    STEPS = None
    ID = 0x00000000

    @staticmethod
    def get_step(id):
        if TextureStep.STEPS == None:
            TextureStep.STEPS = {}
            cs = get_subclasses(TextureStep)
            for c in cs:
                if c.ID > 0:
                    TextureStep.STEPS[c.ID] = c
        return TextureStep.STEPS[id]

    def __init__(self):
        self.params = {}
        self.params['Id'] = self.ID
        self.params['UiVisible'] = False
        self.params['MinShaderModel'] = ShaderModels.SM_1_0
        self.params['SkipShaderModel'] = ShaderModels.SM_Highest
        self.params['MinDetailLevel'] = 0xFFFFFFFF
        self.params['SkipDetailLevel'] = 0
        self.params['Description'] = 'TextureStep'


class DrawableTextureStep(TextureStep):
    def __init__(self):
        TextureStep.__init__(self)
        self.params['ColorWrite'] = 0
        self.params['DestRect'] = [0.0, 0.0, 1.0, 1.0]

        self.params['EnableBlending'] = True
        self.params['SrcBlend'] = 0
        self.params['DestBlend'] = 0

        self.params['MaskBias'] = 0.0
        self.params['MaskSelect'] = 0.0
        self.params['MaskSource'] = 0
        self.params['MaskKey'] = None


class SamplingTextureStep(DrawableTextureStep):
    def __init__(self):
        DrawableTextureStep.__init__(self)
        self.params['Rotation'] = 0.0
        self.params['SourceRect'] = [0.0, 0.0, 1.0, 1.0]
        self.params['EnableFiltering'] = False


class TextureStepDrawImage(SamplingTextureStep):
    ID = 0xA15200B1

    def __init__(self):
        SamplingTextureStep.__init__(self)
        self.params['ImageSource'] = None
        self.params['ImageKey'] = None


class TextureStepColorFill(DrawableTextureStep):
    ID = 0x9CD1269D

    def __init__(self):
        DrawableTextureStep.__init__(self)
        self.params['Color'] = 0xFFFFFFFF


class TextureStepDrawFabric(SamplingTextureStep):
    ID = 0x034210A5

    def __init__(self):
        SamplingTextureStep.__init__(self)
        self.params['DefaultColor'] = 0xFFFFFFFF
        self.params['Width'] = 0xFF
        self.params['Height'] = 0xFF
        self.params['DefaultFabric'] = ExternalResource()


class TextureStepChannelSelect(TextureStepDrawImage):
    ID = 0x1E363B9B

    def __init__(self):
        TextureStepDrawImage.__init__(self)
        self.params['ChannelSelect'] = [0.0, 0.0, 0.0, 0.0]


class TextureStepHSVShift(TextureStepDrawImage):
    ID = 0xDC0984B9

    def __init__(self):
        TextureStepDrawImage.__init__(self)
        self.params['HSVShift'] = [0.0, 0.0, 0.0, 0.0]


class TextureStepSetTarget(TextureStep):
    ID = 0xD6BD8695

    def __init__(self):
        TextureStep.__init__(self)
        self.params['RenderTarget'] = 0


class TextureStepSkinTone(DrawableTextureStep):
    ID = 0x43B554E3

    def __init__(self):
        DrawableTextureStep.__init__(self)


class TextureStepHairTone(DrawableTextureStep):
    ID = 0x5D7C85D4

    def __init__(self):
        DrawableTextureStep.__init__(self)


class TextureStepCASPickData(TextureStepDrawImage):
    ID = 0xC6B6AC1F

    def __init__(self):
        TextureStepDrawImage.__init__(self)


class TextureStepRemappedChannelSelect(TextureStepChannelSelect):
    ID = 0x890805DB

    def __init__(self):
        TextureStepChannelSelect.__init__(self)
        self.params['ReverseMap'] = True


class TextureCompositor(PackedResource):
    ID=0x033A1435
    def __init__(self, key=None, stream=None, resources=None, name=None):
        self.version = 0
        self.fabrics = []
        self.steps = []
        self.target_size = 0
        self.part_type = 0
        PackedResource.__init__(self, key, stream, resources, name)


    def read(self, stream, resources=None):
        s = StreamReader(stream)
        self.version = s.u32()

        use_tgi = not isinstance(resources, TGIList)
        tgi = resources
        if use_tgi:
            tgi = TGIList(order='IGT', use_length=False, package=resources, count_size=8)
            tgi.begin_read(stream)

        if self.version >= 7:
            cFabrics = s.i8()
            for fabric_index in range(cFabrics):
                key = tgi.get_resource(s.u8())
                fabric = TextureCompositor(key)
                fabric_len = s.u32()
                with BytesIO() as fabric_stream:
                    fabric_stream.write(stream.read(fabric_len))
                    fabric_stream.seek(0, SEEK_SET)
                    fabric.read(fabric_stream, tgi)
                self.fabrics.append(fabric)
        self.target_size = s.u32()
        self.part_type = s.u32()
        assert s.u8() == 0
        cSteps = s.i32()
        if self.version >= 0x08:
            assert s.u8() == 0
        self.steps = []
        for step_index in range(cSteps):
            self.steps.append(self.read_step(stream, tgi))
        if use_tgi:
            tgi.end_read(stream)
        else:
            assert s.u32() == 0

    def read_step(self, stream, tgi):
        hash = {}
        while self.read_property(stream, hash, tgi):
            pass
        step = TextureStep.get_step(hash[FNV32.hash('id')])()
        for key in step.params.keys():
            hname = FNV32.hash(key)
            if hname in hash:
                step.params[key] = hash[hname]
        return step

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        s.u32(self.version)

        tgi = resources
        use_tgi = not isinstance(tgi, TGIList)
        if use_tgi:
            tgi = TGIList(order='IGT', use_length=False, package=resources, count_size=8)
            tgi.begin_write(self, stream)
        if self.version >= 7:
            s.i8(len(self.fabrics))
            for fabric in self.fabrics:
                s.i8(tgi.get_resource_index(fabric))
                fabric_len = 0
                with BytesIO() as fabric_stream:
                    fabric.write(fabric_stream, tgi)
                    s.u32(fabric_stream.tell())
                    fabric_stream.seek(0, SEEK_SET)
                    stream.write(fabric_stream.read(-1))
            s.u32(self.target_size)
            s.u32(self.part_type)
            s.u8(0)
            s.i32(len(self.steps))
            if self.version >= 0x08:
                s.u8(0)
            for step in self.steps:
                self.write_step(stream, tgi, step)
        if use_tgi:
            tgi.end_read(stream)
        else:
            s.u32(0)

    def write_step(self, stream, tgi, step):
        s = StreamWriter(stream)
        for key in step.params.keys():
            self.write_property(stream, key, step.params[key], tgi)
        s.u32(0)

    def write_property(self, stream, key, value, tgi):
        s = StreamWriter(stream)
        s.u32(key)
        if value is None:
            s.i8(1)
            return
        else:
            s.i8(0)
            if isinstance(value, bool):
                s.i8(0)
                s.u8(value)

        pass

    def read_property(self, stream, hash, tgi):
        s = StreamReader(stream)
        id = s.u32()
        value = None
        if not id:
            return False
        if not s.u8():
            t = s.u8()
            if   t == 0x00: value = bool(s.u8())
            elif t == 0x01: value = s.i8()
            elif t == 0x02: value = s.i16()
            elif t == 0x03: value = s.i32()
            elif t == 0x04: value = s.i64()
            elif t == 0x05: value = s.u8()
            elif t == 0x06: value = s.u16()
            elif t == 0x07: value = s.u32()
            elif t == 0x08: value = s.u64()
            elif t == 0x09: value = s.f32()
            elif t == 0x0A: value = [s.f32() for i in range(4)]
            elif t == 0x0B: value = [s.f32() for i in range(4)]
            elif t == 0x0C: value = tgi.get_resource(s.u8())
            elif t == 0x0D: value = s.p16()
            else: raise Exception("Unknown TXTC parameter type %s" % t)
        hash[id] = value
        return True





