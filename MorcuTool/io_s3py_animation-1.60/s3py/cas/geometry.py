from ctypes import c_int16
import math
from s3py.core import Serializable, PackedResource, ExternalResource, ResourceKey
from s3py.helpers import Flag
from s3py.io import StreamReader, StreamWriter, TGIList, StreamPtr, RCOL
from s3py.model.geometry import Mesh, Vertex, SkinController
from s3py.model.material import MaterialBlock


class Blend:
    class Vertex(object):
        __slots__ = {
            'position',
            'normal',
            'id'
        }
        def __init__(self):
            self.position = None
            self.normal = None
            self.id = 0

    class LOD(object):
        def __init__(self):
            self.vertices = []

    def __init__(self):
        self.age_gender_flags = 0
        self.blend_region = 0
        self.lods = []


class SlotPose(RCOL):
    class Item(object):
        def __init__(self, name=None, offset=None, scale=None, rotation=None):
            if offset == None:
                offset = [0.0] * 3
            if scale == None:
                scale = [1.0] * 3
            if rotation == None:
                rotation = [0.0, 0.0, 0.0, 1.0]
            self.bone_name = None
            self.offset = offset
            self.scale = scale
            self.rotation = rotation

    ID = 0x0355E0A6

    def __init__(self, key=None, stream=None):
        self.version = 0x00000000
        self.deltas = []
        RCOL.__init__(self, key, stream)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.version = s.u32()
        self.deltas = [
            self.Item(s.u32(), [s.f32()for i in range(3)], [s.f32() for i in range(3)], [s.f32()for i in range(4)])]

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        s.u32(self.version)
        s.i32(len(self.deltas))
        for delta in self.deltas:
            s.hash(delta.bone_name)
            for i in range(3): s.f32(delta.offset[i])
            for i in range(3): s.f32(delta.scale[i])
            for i in range(4): s.f32(delta.rotation[i])


class BodyGeometry(RCOL, Mesh):
    TAG = 'GEOM'
    ID = 0x015A1849
    BLOCK_ID = 0x00000000

    class VertexFormat(Serializable):
        def __init__(self, stream=None):
            self.declarations = []
            Serializable.__init__(self, stream)

        def read(self, stream, resource=None):
            s = StreamReader(stream)
            self.declarations = [self.Declaration(stream) for i in range(s.i32())]

        def write(self, stream, resource=None):
            s = StreamWriter(stream)
            s.i32(len(self.declarations))
            for declaration in self.declarations: declaration.write_rcol(stream)

        def from_vertex(self, vertex):
            self.declarations = []
            if vertex.position: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.POSITION, self.Declaration.FORMAT.FLOAT, 12))
            if vertex.normal: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.NORMAL, self.Declaration.FORMAT.FLOAT, 12))
            if vertex.uv:
                for uv in vertex.uv:
                    self.declarations.append(
                        self.Declaration(self.Declaration.USAGE.UV, self.Declaration.FORMAT.FLOAT, 8))
            if vertex.blend_indices: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.BLEND_INDEX, self.Declaration.FORMAT.BYTE, 4))
            if vertex.blend_weights: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.BLEND_WEIGHT, self.Declaration.FORMAT.FLOAT, 16))
            if vertex.tangents: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.TANGENT, self.Declaration.FORMAT.FLOAT, 12))
            if vertex.colour: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.COLOUR, self.Declaration.FORMAT.BYTE, 4))
            if vertex.id: self.declarations.append(
                self.Declaration(self.Declaration.USAGE.ID, self.Declaration.FORMAT.UINT, 4))

        class Declaration(Serializable):
            class USAGE:
                POSITION = 0x00000001
                NORMAL = 0x00000002
                UV = 0x00000003
                BLEND_INDEX = 0x00000004
                BLEND_WEIGHT = 0x00000005
                TANGENT = 0x00000006
                COLOUR = 0x00000007
                ID = 0x0000000A

            class FORMAT:
                FLOAT = 0x00000001
                BYTE = 0x00000002
                ARGB = 0x00000003
                UINT = 0x00000004

            def __init__(self, stream=None, usage=0, format=0, size=0):
                self.usage = usage
                self.format = format
                self.size = size
                Serializable.__init__(self, stream)

            def read(self, stream, resource=None):
                s = StreamReader(stream)
                self.usage = s.u32()
                self.type = s.u32()
                self.size = s.u8()

            def write(self, stream, resource=None):
                s = StreamWriter(stream)
                s.u32(self.usage)
                s.u32(self.type)
                s.u8(self.size)

    def __init__(self, key=None):
        RCOL.__init__(self, key)
        self.version = 0
        self.indices = []
        self.vertices = []
        self.shader = None
        self.material = MaterialBlock()
        self.merge_group = 0
        self.skin_controller = ExternalResource(ResourceKey(BodySkinController.ID))
        self.sort_order = 0
        self.vertex_format = self.VertexFormat()
        self.bones = []

    def min_vertex_id(self):
        if not any(self.vertices) or not any(filter(lambda f: f.usage == BodyGeometry.VertexFormat.Declaration.USAGE.ID,
            self.vertex_format.declarations)):
            return 0
        return min((vertex.id for vertex in self.vertices if vertex.id != None))

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        tgi = TGIList()
        tgi.begin_read(stream)
        self.shader = s.u32()
        if self.shader:
            end_material = s.u32() + stream.tell()
            self.material = MaterialBlock()
            self.material.read(stream, tgi)
            assert stream.tell() == end_material
        self.merge_group = s.u32()
        self.sort_order = s.u32()
        cVertices = s.u32()
        self.vertex_format.read(stream)
        for vertex_index in range(cVertices):
            vertex = Vertex()
            for declaration in self.vertex_format.declarations:
                if declaration.usage == self.VertexFormat.Declaration.USAGE.POSITION:
                    vertex.position = [s.f32(), s.f32(), s.f32()]
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.NORMAL:
                    vertex.normal = [s.f32(), s.f32(), s.f32()]
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.UV:
                    uv = [s.f32(), s.f32()]
                    if vertex.uv == None:
                        vertex.uv = []
                    vertex.uv.append(uv)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.BLEND_INDEX:
                    vertex.blend_indices = [s.i8(), s.i8(), s.i8(), s.i8()]
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.BLEND_WEIGHT:
                    vertex.blend_weights = [s.f32(), s.f32(), s.f32(), s.f32()]
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.TANGENT:
                    vertex.tangent = [s.f32(), s.f32(), s.f32()]
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.COLOUR:
                    vertex.colour = [s.u8(), s.u8(), s.u8(), s.u8()]
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.ID:
                    vertex.id = s.u32()
            self.vertices.append(vertex)
        assert s.u32() == 1
        bytes_per_index = s.u8()
        assert bytes_per_index == 2
        self.indices = [[s.u16() for i in range(3)] for i in range(int(s.u32() / 3))]
        self.skin_controller = tgi.get_resource(s.u32())
        self.bones = [s.u32() for i in range(s.u32())]
        tgi.end_read(stream)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        tgi = TGIList(32, 'TGI')
        tgi.begin_write(stream)
        s.hash(self.shader)
        if self.shader:
            self.material.write(stream, tgi)
        s.u32(self.merge_group)
        s.u32(self.sort_order)
        s.u32(len(self.vertices))
        self.vertex_format.from_vertex(self.vertices[0])
        self.vertex_format.write(stream)
        for vertex in self.vertices:
            uv_index = 0
            for declaration in self.vertex_format.declarations:
                if declaration.usage == self.VertexFormat.Declaration.USAGE.POSITION:
                    for i in range(3): s.f32(vertex.position)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.NORMAL:
                    for i in range(3): s.f32(vertex.normal)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.UV:
                    for i in range(2): s.f32(vertex.uv[uv_index])
                    uv_index += 1
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.BLEND_INDEX:
                    for i in range(4): s.i8(vertex.blend_indices)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.BLEND_WEIGHT:
                    for i in range(4): s.f32(vertex.blend_weights)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.TANGENT:
                    for i in range(3): s.f32(vertex.tangents)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.COLOUR:
                    for i in range(4): s.i8(vertex.colour)
                elif declaration.usage == self.VertexFormat.Declaration.USAGE.ID:
                    s.u32(vertex.id)
        s.u32(tgi.get_resource_index(self.skin_controller))
        s.u32(len(self.bones))
        for bone in self.bones: s.u32(bone)

    def get_vertices(self):
        return self.vertices

    def get_triangles(self):
        return self.indices

    def __str__(self):
        return "%s : Vertices:(%i) Faces:(%i)" % (PackedResource.__str__(self), len(self.vertices), len(self.indices))


class BlendGeometry(PackedResource):
    ID = 0x067CAA11
    TAG = 'BGEO'

    class VERSION:
        STANDARD = 0x00000300
        OTHER = 0x00030000

    def unpack(self, packed): return c_int16(packed ^ 0x8000).value / 2000.0
    def pack(self, unpacked): return c_int16(math.floor(unpacked * 2000.0) ^ 0x8000).value

    class BlendVertex:
        __slots__ = {
            'position',
            'normal'
        }

        def __init__(self):
            self.position = None
            self.normal = None

    class VertexPtr:
        FLAG_HAS_POSITION = 0x00000001
        FLAG_HAS_NORMAL = 0x00000002

        def __init__(self, val):
            self.value = val

        def get_offset(self):
            return self.value >> 2

        def set_offset(self, value):
            self.value = (value << 2) + (self.value & 0x00000003)

        offset = property(get_offset, set_offset)

        def get_has_position(self):
            return Flag.is_set(self.value, self.FLAG_HAS_POSITION)

        def set_has_position(self, value):
            self.value = Flag.set(self.value, self.FLAG_HAS_POSITION) if value else Flag.unset(self.value,
                self.FLAG_HAS_POSITION)

        has_position = property(get_has_position, set_has_position)

        def get_has_normal(self):
            return Flag.is_set(self.value, self.FLAG_HAS_NORMAL)

        def set_has_normal(self, value):
            self.value = Flag.set(self.value, self.FLAG_HAS_NORMAL) if value else Flag.unset(self.value,
                self.FLAG_HAS_NORMAL)

        has_normal = property(get_has_normal, set_has_normal)

    class LodPtr:
        def __init__(self, start_vertex_id, vertex_count, vector_count):
            self.start_vertex_id = start_vertex_id
            self.vertex_count = vertex_count
            self.vector_count = vector_count

        def __str__(self):
            return "0x%08X 0x%08X 0x%08X" % (self.start_vertex_id, self.vertex_count, self.vector_count)

    def __init__(self, key=None, stream=None):
        self.blends = []
        self.version = self.VERSION.STANDARD
        PackedResource.__init__(self, key, stream)

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.chars(self.TAG)
        s.u32(self.version)
        cBlends = len(self.blends)
        cLods = 0
        if cBlends:
            len(self.blends[0].lods)
            #TODO: write this crazy thing


    def read(self, stream, resource=None):
        s = StreamReader(stream)
        assert s.chars(4) == self.TAG
        self.version = s.u32()

        cBlends = s.i32()
        cLods = s.i32()
        cPointers = s.i32()
        cVectors = s.i32()
        assert s.i32() == 0x00000008
        assert s.i32() == 0x0000000C
        blend_ptr = StreamPtr.begin_read(s)
        vertex_ptr = StreamPtr.begin_read(s)
        vector_ptr = StreamPtr.begin_read(s)
        blend_ptr.end()
        lod_ptrs = []
        for blend_index in range(cBlends):
            blend = Blend()
            blend.age_gender_flags = s.u32()
            blend.blend_region = s.u32()
            self.blends.append(blend)
            blend.lods = [Blend.LOD() for lod_index in range(cLods)]
            lod_ptrs.append([self.LodPtr(s.u32(), s.u32(), s.u32()) for lod_index in range(cLods)])

        vertex_ptr.end()
        pointers = [self.VertexPtr(s.i16()) for pointer_index in range(cPointers)]
        vector_ptr.end()
        vectors = [[self.unpack(s.i16()) for i in range(3)] for vector_index in range(cVectors)]

        for blend_index, blend in enumerate(self.blends):
            start_vector_ptr = 0
            current_vector_offset = 0
            blend_ptr = lod_ptrs[blend_index]
            for lod_index, lod in enumerate(blend.lods):
                lod_blend_index = blend_index + lod_index
                if lod_blend_index >= len(blend_ptr):
                    print('Skipping missing LOD %s - %s'%(lod_blend_index,len(blend_ptr)))
                    continue
                lod_ptr = blend_ptr[blend_index + lod_index]
                current_vertex_id = lod_ptr.start_vertex_id
                for vector_ptr_index in range(lod_ptr.vertex_count):
                    vertex = Blend.Vertex()
                    vector_ptr = pointers[vector_ptr_index + start_vector_ptr]
                    current_vector_offset += vector_ptr.offset
                    vertex.id = current_vertex_id
                    vertex_vector_offset = 0
                    if vector_ptr.has_position:
                        vertex.position = vectors[current_vector_offset + vertex_vector_offset]
                        vertex_vector_offset += 1
                    if vector_ptr.has_normal:
                        vertex.normal = vectors[current_vector_offset + vertex_vector_offset]
                        vertex_vector_offset += 1
                    current_vertex_id += 1
                    lod.vertices.append(vertex)
                start_vector_ptr += lod_ptr.vertex_count
                current_vector_offset += lod_ptr.vector_count


class BodySkinController(SkinController, PackedResource):
    ID = 0x00AE6C67

    def __init__(self, key):
        PackedResource.__init__(self, key)
        SkinController.__init__(self)
        self.version = 0

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.version = s.u32()
        names = [s.s7(16, '>') for i in range(s.u32())]
        poses = [[[s.f32() for j in range(3)] for i in range(4)] for pose_index in range(s.u32())]
        self.bones = [self.Bone(names[i], pose) for i, pose in enumerate(poses)]

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.version)
        s.u32(len(self.bones))
        for bone in self.bones:
            s.s7(bone.name, 16, '>')
        s.u32(len(self.bones))
        for bone in self.bones:
            for i in range(3):
                for j in range(4):
                    s.f32(bone[i][j])
