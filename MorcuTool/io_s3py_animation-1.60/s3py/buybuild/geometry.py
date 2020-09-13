from io import SEEK_END, SEEK_SET, BytesIO
from s3py.core import Serializable, ChildElement
from s3py.helpers import FNV32, Flag
from s3py.io import StreamWriter, StreamReader, RCOL
from s3py.model import LOD_ID
from s3py.model.geometry import Mesh, SkinController, BoundingBox, Vertex
from s3py.model.material import MaterialDefinition, MaterialSet


class ObjectMesh(Serializable, ChildElement, Mesh):
    class Flags:
        BASIN_INTERIOR = 0x00000001
        HD_EXTERIOR_LIT = 0x00000002
        PORTAL_SIDE = 0x00000004
        DROP_SHADOW = 0x00000008
        SHADOW_CASTER = 0x00000010
        FOUNDATION = 0x00000020
        PICKABLE = 0x00000040

    class PrimitiveType:
        POINT_LIST = 0x00000000
        LINE_LIST = 0x00000001
        LINE_STRIP = 0x00000002
        TRIANGLE_LIST = 0x00000003
        TRIANGLE_FAN = 0x00000004
        TRIANGLE_STRIP = 0x00000005
        QUAD_LIST = 0x00000006
        DISPLAY_LIST = 0x00000007


    class State(Serializable):
        def __init__(self, stream=None):
            self.name = 0
            self.start_index = 0
            self.start_vertex = 0
            self.vertex_count = 0
            self.index_count = 0
            Serializable.__init__(self, stream)

        def read(self, stream, resource=None):
            s = StreamReader(stream)
            self.name = s.u32()
            self.start_index = s.i32()
            self.start_vertex = s.i32()
            self.vertex_count = s.i32()
            self.index_count = s.i32()

        def write(self, stream, resource=None):
            s = StreamWriter(stream)
            s.hash(self.name)
            s.i32(self.start_index)
            s.i32(self.start_vertex)
            s.i32(self.vertex_count)
            s.i32(self.index_count)

    def is_dropshadow(self):
        return isinstance(self.material,MaterialDefinition) and int(self.material.shader_name) == 0xC09C7582

    def __init__(self, stream=None, resources=None, parent=None):
        self.name = None
        self.material = None
        self.vertex_format = None
        self.vertex_buffer = None
        self.index_buffer = None
        self.flags = self.Flags.PICKABLE
        self.primitive_type = self.PrimitiveType.TRIANGLE_LIST
        self.stream_offset = 0
        self.start_vertex = 0
        self.start_index = 0
        self.min_vertex_index = 0
        self.vertex_count = 0
        self.primitive_count = 0
        self.bounds = BoundingBox()
        self.skin_controller = None
        self.bone_references = []
        self.scale_offsets = None
        self.states = []
        self.parent_name = 0
        self.mirror_plane_normal = [0.0, 0.0, 0.0]
        self.mirror_plane_offset = 0.0
        ChildElement.__init__(self,parent)
        Serializable.__init__(self, stream, resources)
    def get_vertex_format(self):
        return self.vertex_format if self.vertex_format != None else VertexFormat.default_sunshadow() if Flag.is_set(self.flags,self.Flags.SHADOW_CASTER) else VertexFormat.default_drop_shadow()



    def read(self, stream, rcol):
        s = StreamReader(stream)
        data_len = s.u32()
        end = stream.tell() + data_len
        self.name = s.u32()
        self.material = rcol.get_block(s.u32(), (MaterialDefinition, MaterialSet))
        self.vertex_format = rcol.get_block(s.u32(), VertexFormat)
        self.vertex_buffer = rcol.get_block(s.u32(), (VertexBuffer, VertexBufferShadow))
        self.index_buffer = rcol.get_block(s.u32(), (IndexBuffer, IndexBufferShadow))
        flags = s.u32()
        self.flags = flags >> 8
        self.primitive_type = flags & 0x000000FF
        self.stream_offset = s.u32()
        self.start_vertex = s.i32()
        self.start_index = s.i32()
        self.min_vertex_index = s.i32()
        self.vertex_count = s.i32()
        self.primitive_count = s.i32()
        self.bounds.read(stream)
        self.skin_controller = rcol.get_block(s.u32(), ObjectSkinController)
        self.bone_references = [s.u32() for i in range(s.i32())]
        self.scale_offsets = rcol.get_block(s.u32(), MaterialDefinition)
        self.states = [self.State(stream) for i in range(s.i32())]
        if self.parent.version > ModelLod.VERSION.DEFAULT:
            self.parent_name = s.u32()
            self.mirror_plane_normal = [s.f32() for i in range(3)]
            self.mirror_plane_offset = s.f32()
        if not stream.tell() == end: raise Exception(
            "Invalid MLOD.Mesh data length: expected 0x%X, but got 0x%08X" % (end, stream.tell()))

    def write(self, stream, rcol):
        s = StreamWriter(stream)
        len_offset = stream.tell()
        s.u32(0)
        start = stream.tell()
        s.hash(self.name)
        s.u32(rcol.get_block_index(self.material))
        s.u32(rcol.get_block_index(self.vertex_format))
        s.u32(rcol.get_block_index(self.vertex_buffer))
        s.u32(rcol.get_block_index(self.index_buffer))

        flags = self.primitive_type
        flags |= (self.flags << 8)
        s.u32(flags)
        s.u32(self.stream_offset)
        s.i32(self.start_vertex)
        s.i32(self.start_index)
        s.i32(self.min_vertex_index)
        s.i32(self.vertex_count)
        s.i32(self.primitive_count)
        self.bounds.write(stream)
        s.u32(rcol.get_block_index(self.skin_controller))
        s.i32(len(self.bone_references))
        for bone in self.bone_references: s.u32(bone)
        s.u32(rcol.get_block_index(self.scale_offsets))
        s.i32(len(self.states))
        for state in self.states: state.write_rcol(self, rcol)
        if self.parent.version > ModelLod.VERSION.DEFAULT:
            s.hash(self.parent_name)
            for i in range(3): s.f32(self.mirror_plane_normal[i])
            s.f32(self.mirror_plane_offset)
        end = stream.tell()
        stream.seek(len_offset, SEEK_SET)
        s.u32(end - start)
        stream.seek(end, SEEK_SET)

    def get_uv_scales(self):
        uvscales = [1/0x7FFF] * 3
        key = 'uvscales'
        material = self.material
        if material == None: return  None
        while not  isinstance(material, MaterialDefinition):
            if isinstance(material, MaterialSet):
                material = material.default_material.material
            else:
                raise Exception("Expected a MaterialDefinition or MaterialSet")
        if key in material.material_block:
            uvscales = material.material_block[key]
        return uvscales

    def get_vertices(self):
        uvscales = self.get_uv_scales()
        vrtf = self.get_vertex_format()
        verts = self.vertex_buffer.buffer.read_vertices(self.stream_offset, vrtf, self.vertex_count,
            uvscales)
        return verts

    def get_triangles(self):
        primitive_size = 0
        if self.primitive_type == self.PrimitiveType.TRIANGLE_LIST:
            primitive_size = 3
        else:
            raise NotImplementedError()
        return [[self.index_buffer.buffer[self.start_index + (primitive_index * primitive_size) + i] for i in
                 range(primitive_size)] for primitive_index in range(self.primitive_count)]


class ModelLod(RCOL):
    TAG = 'MLOD'
    ID = 0x01D10F34

    class VERSION:
        DEFAULT = 0x00000201
        EXTENDED = 0x00000202

    def __init__(self, key=None):
        RCOL.__init__(self, key)
        self.version = 0
        self.meshes = []

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        self.meshes = [ObjectMesh(stream, rcol, self) for i in range(s.i32())]

    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        s.i32(len(self.meshes))
        for mesh in self.meshes: mesh.write(stream, rcol)


class IndexBuffer(RCOL):
    TAG = 'IBUF'
    ID = 0x01D0E70F

    class VERSION:
        DEFAULT = 0x000000100

    class FLAGS:
        DIFFERENCED_INDICES = 0x00000001
        INDEX_32 = 0x00000002
        DISPLAY_LIST = 0x00000004


    def __init__(self, key):
        RCOL.__init__(self, key)
        self.version = self.VERSION.DEFAULT
        self.buffer = []
        self.flags = 0
        self.unknown = 0

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        self.flags = s.u32()
        self.unknown = s.u32()
        start = stream.tell()
        stream.seek(0, SEEK_END)
        end = stream.tell()
        stream.seek(start, SEEK_SET)
        self.buffer = []
        last = 0
        while stream.tell() < end:
            cur = s.i32() if Flag.is_set(self.flags, self.FLAGS.INDEX_32) else s.i16()
            if Flag.is_set(self.flags, self.FLAGS.DIFFERENCED_INDICES):
                cur += last
                last = cur
            self.buffer.append(cur)


    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        s.u32(self.flags)
        s.u32(self.unknown)
        last = 0
        for i in range(len(self.buffer)):
            cur = self.buffer[i]
            if Flag.is_set(self.flags, self.FLAGS.DIFFERENCED_INDICES):
                cur -= last
                last = self.buffer[i]
            s.i32(cur) if Flag.is_set(self.flags, self.FLAGS.INDEX_32) else s.i16(cur)


class VertexFormat(RCOL):
    TAG = 'VRTF'
    ID = 0x01D0E723

    def __init__(self, key=None):
        self.stride = 0
        self.version = self.VERSION.DEFAULT
        self.is_extended_format = False
        self.declarations = []
        RCOL.__init__(self, key)


    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()

        self.stride = s.i32()
        cDeclarations = s.i32()
        self.is_extended_format = s.u32() > 0

        self.declarations = []
        for declaration_index in range(cDeclarations):
            declaration = self.Declaration()
            if self.is_extended_format:
                declaration.usage = s.u32()
                declaration.usage_index = s.u32()
                declaration.format = s.u32()
                declaration.offset = s.u32()
            else:
                declaration.usage = s.u8()
                declaration.usage_index = s.u8()
                declaration.format = s.u8()
                declaration.offset = s.u8()
            self.declarations.append(declaration)


    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        s.i32(self.stride)
        s.i32(len(self.declarations))
        s.u32(1 if self.is_extended_format else 0)
        for declaration in self.declarations:
            if self.is_extended_format:
                s.u32(declaration.usage)
                s.u32(declaration.usage_index)
                s.u32(declaration.format)
                s.u32(declaration.offset)
            else:
                s.u8(declaration.usage)
                s.u8(declaration.usage_index)
                s.u8(declaration.format)
                s.u8(declaration.offset)


    def add_declaration(self, usage, format):
        declaration = self.Declaration()
        declaration.usage = usage
        declaration.format = format
        declaration.offset = self.stride
        for d in self.declarations:
            if d.usage == usage: declaration.usage_index += 1
        self.declarations.append(declaration)
        self.stride += self.FORMAT.byte_size(format)


    @staticmethod
    def from_vertex(vertex):
        vrtf = VertexFormat()
        if not vertex.position == None: vrtf.add_declaration(VertexFormat.USAGE.POSITION, VertexFormat.FORMAT.SHORT4)
        if not vertex.normal == None: vrtf.add_declaration(VertexFormat.USAGE.NORMAL, VertexFormat.FORMAT.COLOR_UBYTE4)
        if not vertex.uv == None:
            for uv_index in range(vertex.uv):
                vrtf.add_declaration(VertexFormat.USAGE.UV, VertexFormat.FORMAT.SHORT2)
        if not vertex.blend_indices == None: vrtf.add_declaration(VertexFormat.USAGE.BLEND_INDEX,
            VertexFormat.FORMAT.UBYTE4)
        if not vertex.blend_weights == None: vrtf.add_declaration(VertexFormat.USAGE.BLEND_WEIGHT,
            VertexFormat.FORMAT.COLOR_UBYTE4)

        pass

    @classmethod
    def default_sunshadow(cls):
        vrtf = VertexFormat()
        vrtf.add_declaration(cls.USAGE.POSITION, cls.FORMAT.SHORT4)
        return vrtf

    @classmethod
    def default_drop_shadow(cls):
        vrtf = VertexFormat()
        vrtf.add_declaration(cls.USAGE.POSITION, cls.FORMAT.USHORT4N)
        vrtf.add_declaration(cls.USAGE.UV, cls.FORMAT.SHORT4)
        return vrtf

    @staticmethod
    def default_mesh(mesh):
        return VertexFormat.default_sunshadow() if Flag.is_set(mesh.flags,
            ObjectMesh.Flags.SHADOW_CASTER) else VertexFormat.default_drop_shadow()

    class VERSION:
        DEFAULT = 0x00000002

    class USAGE:
        POSITION = 0x00000000
        NORMAL = 0x00000001
        UV = 0x00000002
        BLEND_INDEX = 0x00000003
        BLEND_WEIGHT = 0x00000004
        TANGENT = 0x00000005
        COLOR = 0x00000006

    class FORMAT:
        UBYTE_MAP = {0: 2, 1: 1, 2: 0, 3: 3}
        FLOAT = 0x00000000
        FLOAT2 = 0x00000001
        FLOAT3 = 0x00000002
        FLOAT4 = 0x00000003
        UBYTE4 = 0x00000004
        COLOR_UBYTE4 = 0x00000005
        SHORT2 = 0x00000006
        SHORT4 = 0x00000007
        UBYTE4N = 0x00000008
        SHORT2N = 0x00000009
        SHORT4N = 0x0000000A
        USHORT2N = 0x0000000B
        USHORT4N = 0x0000000C
        DEC3N = 0x0000000D
        UDEC3N = 0x0000000E
        FLOAT16_2 = 0x0000000F
        FLOAT16_4 = 0x00000010

        @classmethod
        def float_count(cls,f):
            if f == cls.FLOAT: return 1
            elif f in (cls.FLOAT2, cls.USHORT2N, cls.SHORT2): return 2
            elif f in (cls.SHORT4, cls.SHORT4N, cls.UBYTE4N,
                       cls.USHORT4N,
                       cls.FLOAT3): return 3
            elif f in (
                cls.COLOR_UBYTE4, cls.FLOAT4, cls.UBYTE4): return 4
            else: raise NotImplementedError()

        @classmethod
        def byte_size(cls,f):
            if f in (
                cls.FLOAT, cls.UBYTE4, cls.UBYTE4N, cls.COLOR_UBYTE4, cls.SHORT2,
                cls.USHORT2N): return 4
            elif f in (cls.USHORT4N, cls.FLOAT2, cls.SHORT4, cls.SHORT4N): return 8
            elif f == cls.FLOAT3: return 12
            elif f == cls.FLOAT4: return 16
            else: raise NotImplementedError()


    class Declaration:
        def __init__(self):
            self.usage = VertexFormat.USAGE.POSITION
            self.usage_index = 0
            self.format = VertexFormat.FORMAT.SHORT4
            self.offset = 0


class VertexBuffer(RCOL):
    TAG = 'VBUF'
    ID = 0x01D0E6FB

    class Buffer:
        def __init__(self):
            self.stream = BytesIO()
            self.reader = StreamReader(self.stream)
            self.writer = StreamWriter(self.stream)

        def delete_vertices(self, offset, vrtf, count):
            end_offset = offset + vrtf.stride * count
            self.stream.seek(end_offset, SEEK_SET)
            end_data = self.stream.read(-1)
            self.stream.seek(offset, SEEK_SET)
            self.stream.truncate()
            self.stream.writable(end_data)

        def read_vertices(self, offset, vrtf, count,uvscales):
            self.stream.seek(offset, SEEK_SET)
            return [self.read_vertex(vrtf,uvscales) for i in range(count)]

        def read_vertex(self, vrtf, uvscales):
            vertex = Vertex()
            start = self.stream.tell()
            end = start + vrtf.stride
            for declaration in vrtf.declarations:
                u = declaration.usage
                value = self.read_element(declaration,uvscales[declaration.usage_index])
                if u == VertexFormat.USAGE.POSITION: vertex.position = value
                elif u == VertexFormat.USAGE.NORMAL: vertex.normal = value
                elif u == VertexFormat.USAGE.UV:
                    if vertex.uv == None: vertex.uv = []
                    vertex.uv.append(value)
                elif u == VertexFormat.USAGE.BLEND_INDEX: vertex.blend_indices = value
                elif u == VertexFormat.USAGE.BLEND_WEIGHT: vertex.blend_weights = value
                elif u == VertexFormat.USAGE.COLOR: vertex.colour = value
                elif u == VertexFormat.USAGE.TANGENT: vertex.tangent = value
                else:
                    raise Exception("Unknown usage %s", declaration.usage)
            actual = self.stream.tell()
            return vertex

        def write_vertices(self, vrtf, vertices, uvscales=None):
            self.stream.seek(0, SEEK_END)
            offset = self.stream.tell()
            for vertex in vertices: self.write_vertex(vrtf, vertex)

        def write_vertex(self, vrtf, v):
            for declaration in vrtf.declarations:
                u = declaration.usage
                if u == VertexFormat.USAGE.POSITION: data = v.position
                elif u == VertexFormat.USAGE.NORMAL: data = v.normal
                elif u == VertexFormat.USAGE.UV: data = v.uv[vrtf.usage_index]
                elif u == VertexFormat.USAGE.BLEND_INDEX: data = v.blend_indices
                elif u == VertexFormat.USAGE.BLEND_WEIGHT: data = v.blend_weights
                elif u == VertexFormat.USAGE.COLOR: data = v.colour
                elif u == VertexFormat.USAGE.TANGENT: data = v.tangents
                else: raise Exception('Unknown VRTF usage type %i' % u)
                self.write_element(declaration, data)

        def write_element(self, declaration, value):
            pass

        def read_element(self, declaration, uvscale):
            float_count = VertexFormat.FORMAT.float_count(declaration.format)
            value = [0.0] * float_count
            f = declaration.format
            u = declaration.usage
            if u == VertexFormat.USAGE.UV:
                if f == VertexFormat.FORMAT.SHORT2:
                    for i in range(float_count): value[i] = self.reader.i16() * uvscale
                elif f == VertexFormat.FORMAT.SHORT4:
                    shorts = [self.reader.i16() for i in range(4)]
                    assert shorts[2] == 0
                    value = [shorts[0] /0x7FFF, shorts[1]/0x7FFF, shorts[3] /0x1FF]
            elif f in (VertexFormat.FORMAT.FLOAT, VertexFormat.FORMAT.FLOAT2, VertexFormat.FORMAT.FLOAT3,
                       VertexFormat.FORMAT.FLOAT4):
                for i in range(float_count): value[i] = self.reader.f32()
            elif f == VertexFormat.FORMAT.UBYTE4:
                for i in range(float_count): value[i] = self.reader.i8()
            elif f == VertexFormat.FORMAT.COLOR_UBYTE4:
                if u == VertexFormat.USAGE.COLOR:
                    for i in range(float_count): value[i] = self.reader.u8() / 0xFF
                elif u == VertexFormat.USAGE.BLEND_WEIGHT:
                    for i in range(float_count): value[VertexFormat.FORMAT.UBYTE_MAP[i]] = self.reader.u8() / 0xFF
                elif u in (VertexFormat.USAGE.NORMAL, VertexFormat.USAGE.TANGENT):
                    bytes = [self.reader.u8() for i in range(4)]
                    for i in range(float_count - 1):
                        value[i] = -1 if bytes[2 - i] == 0 else ( ((bytes[2 - i] + 1) / 128.0) - 1)
                    determinant = 0.0
                    if not bytes[3]: determinant = -1.0
                    elif bytes[3] == 127.0: determinant = 0.0
                    elif bytes[3] == 255.0: determinant = 1.0
                    else: print("Unexpected handedness %i " % bytes[3])
                    value[float_count - 1] = determinant
                else:
                    raise Exception("Unhandled usage %s for format %s" % (u, f))
            elif f == VertexFormat.FORMAT.SHORT2:
                for i in range(float_count): value[i] = self.reader.i16() / 0xFFFF
            elif f == VertexFormat.FORMAT.SHORT4:
                shorts = [self.reader.i16() for i in range(3)]
                scalar = self.reader.u16()
                if not scalar: scalar = 0x7FFF
                for i in range(float_count): value[i] = float(shorts[i]) / float(scalar)
            elif f == VertexFormat.FORMAT.USHORT4N:
                shorts = [self.reader.i16() for i in range(3)]
                scalar = self.reader.u16()
                if not scalar: scalar = 511
                for i in range(float_count): value[i] = shorts[i] / scalar
            elif f == VertexFormat.FORMAT.UBYTE4:
                data = [self.reader.i8() for i in range(4)]
            else:
                raise Exception("Unhandled format %s" % f)
            return value

        def __del__(self):
            if self.stream != None:
                self.stream.close()

    class VERSION:
        DEFAULT = 0x00000101

    def __init__(self, key):
        RCOL.__init__(self, key)
        self.swizzle_info = SwizzleInfo(None)
        self.buffer = self.Buffer()

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        assert s.u32() == 0
        self.swizzle_info = rcol.get_block(s.u32(), SwizzleInfo)
        start = stream.tell()
        stream.seek(0, SEEK_END)
        end = stream.tell()
        stream.seek(start, SEEK_SET)
        length = end - start
        self.buffer.stream.seek(0, SEEK_SET)
        self.buffer.stream.truncate()
        self.buffer.stream.write(stream.read(length))
        self.buffer.stream.seek(0, SEEK_SET)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.u32(0)
        s.u32(rcol.get_block_index(self.swizzle_info))
        self.buffer.stream.seek(0, SEEK_SET)
        stream.write(self.buffer.stream.read())


class Swizzle:
    SWIZZLE_32 = 0x00000001
    SWIZZLE_16x2 = 0x00000002

class SwizzleInfo(RCOL):
    ID = 0x00000000

    class VERSION:
        STANDARD = 0x00000101


    class Segment(Serializable):
        def __init__(self, stream=None):
            self.vertex_size = 0
            self.vertex_count = 0
            self.byte_offset = 0
            self.commands = []
            Serializable.__init__(self, stream)

        def read(self, stream, resource=None):
            s = StreamReader(stream)
            self.vertex_size = s.u32()
            self.vertex_count = s.u32()
            self.byte_offset = s.u32()
            self.commands = [s.u32() for cmd_index in range(int(self.vertex_size / 4))]

        def write(self, stream, resource=None):
            s = StreamWriter(stream)
            s.u32(self.vertex_size)
            s.u32(self.vertex_count)
            s.u32(self.byte_offset)
            assert len(self.commands) == (int(self.vertex_size / 4))
            for cmd in self.commands: s.u32(cmd)

        @staticmethod
        def from_mesh(mesh):
            vrtf = mesh.vertex_format
            segment = SwizzleInfo.Segment()
            segment.vertex_size = mesh.vrtf.stride
            segment.byte_offset = mesh.stream_offset
            segment.vertex_count = mesh.vertex_count
            for d in vrtf.declarations:
                if d.format == VertexFormat.FORMAT.FLOAT4:
                    segment.commands.extend(
                        [Swizzle.SWIZZLE_32, Swizzle.SWIZZLE_32, Swizzle.SWIZZLE_32,
                         Swizzle.SWIZZLE_32])
                elif d.format == VertexFormat.FORMAT.FLOAT3:
                    segment.commands.extend([Swizzle.SWIZZLE_32, Swizzle.SWIZZLE_32,
                                             Swizzle.SWIZZLE_32])
                elif d.format == VertexFormat.FORMAT.FLOAT2:
                    segment.commands.extend([Swizzle.SWIZZLE_32, Swizzle.SWIZZLE_32])
                elif d.format in  (
                    VertexFormat.FORMAT.FLOAT, VertexFormat.FORMAT.UBYTE4, VertexFormat.FORMAT.COLOR_UBYTE4,
                    VertexFormat.FORMAT.UBYTE4N, VertexFormat.FORMAT.DEC3N,
                    VertexFormat.FORMAT.UDEC3N):
                    segment.commands.append(Swizzle.SWIZZLE_32)
                elif d.format in(VertexFormat.FORMAT.SHORT2, VertexFormat.FORMAT.SHORT2N, VertexFormat.FORMAT.USHORT2N,
                                 VertexFormat.FORMAT.FLOAT16_2):
                    segment.commands.append(Swizzle.SWIZZLE_16x2)
                elif d.format in (
                    VertexFormat.FORMAT.SHORT4, VertexFormat.FORMAT.SHORT4N, VertexFormat.FORMAT.FLOAT16_4):
                    segment.commands.extend([Swizzle.SWIZZLE_16x2, Swizzle.SWIZZLE_16x2])
            return segment

    def __init__(self, key=None):
        RCOL.__init__(self, key)
        self.segments = []

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.segments = [self.Segment(stream) for i in range(s.i32())]

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        s.i32(len(self.segments))
        for segment in self.segments: segment.write(stream)

    def add_mesh(self, mesh):
        self.segments.append(self.Segment.from_mesh(mesh))

    def __eq__(self, other):
        return self is other
    def __hash__(self):
        return object.__hash__(self)


class IndexBufferShadow(IndexBuffer):
    ID = 0x0229684F


class VertexBufferShadow(VertexBuffer):
    ID = 0x0229684B


class Model(RCOL):
    TAG = 'MODL'
    ID = 0x01661233

    class VERSION():
        STANDARD = 0x00000100
        EXTENDED = 0x00000102

    class LOD(Serializable):
        class FLAGS:
            NONE = 0x00000000
            PORTAL = 0x00000001
            DOOR = 0x00000002

        def __init__(self, stream=None, rcol=None):
            self.model = None
            self.flags = Model.LOD.FLAGS.NONE
            self.id = LOD_ID.MEDIUM_DETAIL
            self.min_z = 0.0
            self.max_z = 0.0
            Serializable.__init__(self, stream, rcol)

        def read(self, stream, resources):
            s = StreamReader(stream)
            self.model = resources.get_block(s.u32(), ModelLod)
            self.flags = s.u32()
            self.id = s.u16()
            self.is_sunshadow = bool(s.u16())
            self.min_z = s.f32()
            self.max_z = s.f32()

        def write(self, stream, resources):
            s = StreamWriter(stream)
            s.u32(resources.get_block_index(self.model, RCOL.Reference.PUBLIC))
            s.u32(self.flags)
            s.u16(self.id)
            s.u16(int(self.is_sunshadow))
            s.f32(self.min_z)
            s.f32(self.max_z)


    def __init__(self, key, stream=None, rcol=None):
        self.version = self.VERSION.STANDARD
        self.bounds = BoundingBox()
        self.extra_bounds = []
        self.fade_type = 0
        self.custom_fade_distance = 0.0
        self.lods = []
        RCOL.__init__(self, key, stream)


    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        cLods = s.i32()
        self.bounds.read(stream)
        if self.version >= self.VERSION.EXTENDED:
            self.extra_bounds = [BoundingBox(stream=stream) for i in range(s.i32())]
            self.fade_type = s.u32()
            self.custom_fade_distance = s.f32()
        self.lods = [self.LOD(stream, rcol) for i in range(cLods)]

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.i32(len(self.lods))
        self.bounds.write(stream)
        if self.version >= self.VERSION.EXTENDED:
            s.i32(len(self.extra_bounds))
            for extra in self.extra_bounds:
                extra.write(stream)
            s.u32(self.fade_type)
            s.f32(self.custom_fade_distance)
        for lod in self.lods:
            lod.write_rcol(stream, rcol)


class ObjectSkinController(RCOL, SkinController):
    TAG = 'SKIN'
    ID = 0x01D0E76B

    def __init__(self, key=None):
        RCOL.__init__(self, key)
        SkinController.__init__(self)
        self.version = 0

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        cBones = s.i32()
        names = [s.u32() for i in range(cBones)]
        poses = [s.m43() for pose_index in range(cBones)]
        self.bones = [self.Bone(names[i], poses[i]) for i in range(cBones)]

    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        cBones = len(self.bones)
        s.i32(cBones)
        for i in range(cBones): s.hash(self.bones[i].name)
        for bone in self.bones:
            s.m43(bone.inverse_bind_pose)

