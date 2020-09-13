from io import SEEK_SET, BytesIO, SEEK_CUR
import struct
from s3py.core import PackedResource, Serializable, ResourceKeyProvider, ExternalResource, ResourceKey
from s3py.helpers import HashedString, Flag

class StreamWrapper(object):
    def __init__(self, stream):
        self.f = stream

    def stream(self):
        return self.f

    def tell(self):
        return self.f.tell()

    def seek(self, offset, whence):
        self.f.seek(offset, whence)

    def verify_tgi(self, pattern):
        if len(pattern) != 3 or not 't' in pattern or not 'g' in pattern or not 'i' in pattern:
            raise Exception("Invalid format for TGI.  Must contain t, g, and i, and must not exceed 3 characters")


class StreamReader(StreamWrapper):
    def __init__(self, stream):
        StreamWrapper.__init__(self, stream)

    def read(self, l, size, order='<'):
        d = self.f.read(size)
        l = struct.unpack(order + l, d)[0]
        return l

    def bytes(self, count):
        return [self.u8() for i in range(count)]

    def hash(self, lookup=None):
        if not lookup: lookup = {}
        h = self.u32()
        if not h: return  None
        return HashedString(lookup[h]) if h in lookup else HashedString(h)

    def tgi(self, pattern='tgi', order='<'):
        pattern = pattern.lower()
        self.verify_tgi(pattern)
        key = ResourceKey()
        for f in pattern:
            if f == 't': key.t = self.u32(order)
            elif f == 'g': key.g = self.u32(order)
            elif f == 'i': key.i = self.u64(order)
        return key

    def i(self, size, order='<'):
        if size == 8: return self.read('b', 1, order)
        elif size == 16: return self.read('h', 2, order)
        elif size == 32: return self.read('i', 4, order)
        elif size == 64: return self.read('q', 8, order)
        else: raise Exception("Invalid integer size")

    def u(self, size, order='<'):
        if size == 8: return self.read('B', 1, order)
        elif size == 16: return self.read('H', 2, order)
        elif size == 32: return self.read('I', 4, order)
        elif size == 64: return self.read('Q', 8, order)
        else: raise Exception("Invalid integer size")

    def i8(self, order='<'):
        return self.i(8, order)

    def i16(self, order='<'):
        return self.i(16, order)

    def i32(self, order='<'):
        return self.i(32, order)

    def i64(self, order='<'):
        return self.i(64, order)

    def u8(self, order='<'):
        return self.u(8, order)

    def u16(self, order='<'):
        return self.u(16, order)

    def u32(self, order='<'):
        return self.u(32, order)

    def u64(self, order='<'):
        return self.u(64, order)

    def f32(self, order='<'):
        return self.read('f', 4, order)

    def f64(self, order='<'):
        return self.read('d', 4, order)

    def chars(self, count, size=8, order='<'):
        c = ""
        for i in range(count):
            c += chr(self.i(size, order))
        return c

    def s7(self, size=8, order='<'):
        b = self.u8()
        if not b:
            return ''

        length = b & 0x7F
        while Flag.is_set(b, 0x80):
            b = self.u8()
            length += b & 0x7F
        if size == 16:
            length /= 2
        return self.chars(int(length), size, order)

    def p8(self, size=8, order='<'):
        return self.chars(self.i8(), size, order)

    def p16(self, size=8, order='<'):
        return self.chars(self.i16(), size, order)

    def p32(self, size=8, order='<'):
        return self.chars(self.i32(), size, order)

    def p64(self, size=8, order='<'):
        return self.chars(self.i64(), size, order)

    def zs(self, size=8, order='<'):
        s = ''
        c = None
        while c is not 0:
            c = self.i(size, order)
            if c is not 0: s += chr(c)
        return s

    def m44(self, order='<'):
        return [[self.f32(order) for i in range(4)] for j in range(4)]

    def m43(self, order='<'):
        return [[self.f32(order) for i in range(4)] for j in range(3)]

    def v(self, len, order='<'):
        return [self.f32(order) for i in range(len)]

    def align(self, mod=4, char=None):
        pos = self.f.tell()
        length = 0 if not pos % mod else mod - (pos % mod)
        for i in range(length):
            c = self.u8()
            if not char == None and not char == c: raise IOError(
                "Expected padding character %x, but got %x" % (char, c))


class StreamWriter(StreamWrapper):
    def __init__(self, stream):
        StreamWrapper.__init__(self, stream)

    def v(self, data, order='<'):
        for f in data: self.f32(f, order)

    def m43(self, data, order='<'):
        for i in range(3):
            for j in range(4):
                self.f32(data[i][j], order)

    def m44(self, data, order='<'):
        for i in range(4):
            for j in range(4):
                self.f32(data[i][j], order)

    def bytes(self, bs):
        for b in bs: self.i8(b)

    def write(self, l, data, order='<'):
        self.f.write(struct.pack(l, data))

    def hash(self, string):
        self.u32(HashedString(string))

    def tgi(self, key, pattern='tgi', order='<'):
        pattern = pattern.lower()
        self.verify_tgi(pattern)
        for f in pattern:
            if f == 't': self.u32(key.t, order)
            elif f == 'g': self.u32(key.g, order)
            elif f == 'i': self.u64(key.i, order)

    def i(self, data, size, order='<'):
        data = int(data)
        if size == 8: self.write('b', data, order)
        elif size == 16: self.write('h', data, order)
        elif size == 32: self.write('i', data, order)
        elif size == 64: self.write('q', data, order)
        else: raise Exception("Invalid integer size")

    def u(self, data, size, order='<'):
        data = int(data)
        if size == 8: self.write('B', data, order)
        elif size == 16: self.write('H', data, order)
        elif size == 32: self.write('I', data, order)
        elif size == 64: self.write('Q', data, order)
        else: raise Exception("Invalid integer size")

    def i8(self, data, order='<'):
        self.i(data, 8, order)

    def i16(self, data, order='<'):
        self.i(data, 16, order)

    def i32(self, data, order='<'):
        self.i(data, 32, order)

    def i64(self, data, order='<'):
        self.i(data, 64, order)

    def u8(self, data, order='<'):
        self.u(data, 8, order)

    def u16(self, data, order='<'):
        self.u(data, 16, order)

    def u32(self, data, order='<'):
        self.u(data, 32, order)

    def u64(self, data, order='<'):
        self.u(data, 64, order)

    def f32(self, data, order='<'):
        self.write('f', data, order)

    def f64(self, data, order='<'):
        self.write('d', data, order)

    def chars(self, data, size=8, order='<'):
        for c in data: self.c(c, size, order)

    def c(self, data, size=8, order='<'):
        self.i(ord(data), size, order)

    def s7(self, data, size=8, order='<'):
        length = len(data)
        length *= (size / 8)
        length = int(length)
        while length > 0x7F:
            self.u8(0x80)
            length -= 0x80
        self.u8(length)
        self.chars(data, size, order)

    def p8(self, data, size=8, order='<'):
        self.i8(len(data))
        self.chars(data, size, order)

    def p16(self, data, size=8, order='<'):
        self.i16(len(data))
        self.chars(data, size, order)

    def p32(self, data, size=8, order='<'):
        self.i32(len(data))
        self.chars(data, size, order)

    def p64(self, data, size=8, order='<'):
        self.i64(len(data))
        self.chars(data, size, order)

    def zs(self, data, size=8, order='<'):
        self.chars(data, size, order)
        self.i8(0)

    def align(self, mod=4, char=0x00):
        pos = self.f.tell()
        length = 0 if not pos % mod else mod - (pos % mod)
        for i in range(length): self.u8(char)


class StreamPtr:
    def __init__(self, pointer, offset, mode, stream, relative):
        self.pointer = pointer
        self.offset = offset
        self.mode = mode
        self.stream = stream
        self.relative = relative

    def seek_ptr(self):
        self.stream.seek(self.pointer, SEEK_SET)

    def seek_data(self):
        offset = self.offset
        if self.relative:
            if not self.offset: return False
            offset += self.pointer
        self.stream.seek(offset, SEEK_SET)
        return True

    @staticmethod
    def begin_read(s, relative=False):
        ptr = StreamPtr(s.tell(), s.u32(), 'read', s, relative)
        return ptr

    @staticmethod
    def begin_write(s, relative=False):
        ptr = StreamPtr(s.tell(), 0, 'write', s, relative)
        s.u32(0)
        return ptr

    def end(self):
        if self.mode == 'write':
            self.offset = self.stream.tell()
            final = self.offset
            if self.relative: final -= self.pointer
            self.stream.seek(self.pointer, SEEK_SET)
            self.stream.u32(final)
            self.stream.seek(self.offset, SEEK_SET)
        elif self.mode == 'read':
            final = self.offset
            if self.relative:
                if not self.offset: return
                final += self.pointer
            if self.stream.tell() != final: print(
                Exception("Bad offset: Expected " + hex(final) + ", but got " + hex(self.stream.tell())))

    def abs_offset(self):
        offset = self.offset
        if self.relative:
            if not self.offset: return -1
            offset += self.pointer
        return offset


class TGIList(ResourceKeyProvider):
    def __init__(self, order='tgi', count_size=32, use_length=True, add_eight=False, package=None):
        self.package = package
        self.count_size = count_size
        self.order = order
        self.use_length = use_length
        self.add_eight = add_eight
        self.blocks = []
        self.offset = 0
        self.length = 0


    def get_resource(self, index):
        return self.blocks[index]

    def get_resource_index(self, resource):
        if not resource in self.blocks: self.blocks.append(resource)
        return self.blocks.index(resource)

    def begin_read(self, stream):
        s = StreamReader(stream)
        self.position = stream.tell()
        self.offset = s.u32() + stream.tell()
        if self.use_length:
            self.length = s.u32()
        cur = stream.tell()
        s.seek(self.offset, SEEK_SET)
        start = stream.tell()

        count = s.i(self.count_size)
        for i in range(count):
            block = s.tgi(self.order)
            resource = None
            if not self.package is None:
                resource = self.package.get_resource(key=block, recursive=True)
            if resource is None:
                resource = ExternalResource(block)
            self.blocks.append(resource)
        end = stream.tell()
        if self.add_eight:
            end += 8
        s.seek(cur, SEEK_SET)

        if self.use_length:
            actual = end - start
            if actual != self.length: raise Exception(
                "Invalid TGI block list length, expected 0x%X, but got 0x%X" % (self.length, actual))

    def end_read(self, stream):
        if stream.tell() != self.offset:
            raise Exception("Invalid offset: expected , expected 0x%X, but got 0x%X" % (self.offset, stream.tell()))
        stream.seek(self.length, SEEK_CUR)

    def begin_write(self, stream, write_length=True):
        s = StreamWriter(stream)
        self.position = stream.tell()
        self.blocks = []
        s.u32(0)
        if self.use_length:
            s.u32(0)

    def end_write(self, stream):
        s = StreamWriter(stream)
        start = stream.tell()
        self.offset = start

        s.i(len(self.blocks), self.count_size)
        for block in self.blocks:
            s.tgi(block.key, self.order)
        end = stream.tell()
        self.length = end - start
        if self.add_eight:
            self.length += 8
        stream.seek(self.position, SEEK_SET)
        s.u32(self.offset)
        if self.use_length:
            s.u32(self.length)
        stream.seek(end, SEEK_SET)


class RCOL(PackedResource):
    class Reference:
        PUBLIC = 0x00000000
        PRIVATE = 0x10000000
        EXTERNAL = 0x20000000
        DELAYED = 0x30000000

    class Serializer(Serializable, ResourceKeyProvider):
        class Chunk:
            def __init__(self, key):
                self.key = key
                self.data = None
                self.resource = None

            def __eq__(self, other):
                return other.key == self.key

            def __hash__(self):
                return hash(self.key)

        class VERSION:
            DEFAULT = 0x00000003

        def __init__(self, stream=None, package=None):
            Serializable.__init__(self, stream)
            self.version = self.VERSION.DEFAULT
            self.__delayed = []
            self.__external = []
            self.__public = []
            self.__private = []
            self.__data = {}
            self.package = package

        def get_block(self, index, resource_type):
            if not isinstance(resource_type, tuple):
                resource_type = resource_type,
            t = index & 0xF0000000
            i = (index & 0x0FFFFFFF) - 1
            chunk = None
            if i < 0: return None
            elif t == RCOL.Reference.DELAYED:
                key = self.__delayed[i]
                resource = None
                if self.package != None:
                    resource = self.package.get_resource(key=key, recursive=True)
                else:
                    resource = ExternalResource(key)
                return  resource
            elif t == RCOL.Reference.PUBLIC: chunk = self.__public[i]
            elif t == RCOL.Reference.PRIVATE: chunk = self.__private[i]
            else: raise Exception("Unknown RCOL Reference Type: 0x%X" % t)
            if chunk.resource == None:
                data = chunk.data
                for r in resource_type:
                    if r.ID == chunk.key.t:
                        chunk.resource = r(chunk.key)

                assert chunk.resource != None

                with  BytesIO() as stream:
                    stream.write(data)
                    data_len = stream.tell()
                    stream.seek(0, SEEK_SET)
                    chunk.resource.read_rcol(stream, self)
                    assert stream.tell() == data_len
            return chunk.resource

        def get_block_index(self, block, reference_type=None):
            if block is None: return 0
            if isinstance(block, ExternalResource):
                reference_type = RCOL.Reference.DELAYED
            else:
                if reference_type == None:
                    reference_type = RCOL.Reference.PRIVATE

            if reference_type == RCOL.Reference.PUBLIC: list = self.__public
            elif reference_type == RCOL.Reference.PRIVATE: list = self.__private
            elif reference_type == RCOL.Reference.DELAYED:
                if self.package != None:
                    self.package.save_resource(block)
                list = self.__delayed
            else: raise Exception("Unknown RCOL Reference Type: 0x%X" % reference_type)

            if not block in list:
                list.append(block)
                if not reference_type == RCOL.Reference.DELAYED:
                    stream = BytesIO()
                    block.write_rcol(stream, self)
                    stream.seek(0, SEEK_SET)
                    data = stream.read(-1)
                    self.__data[block] = data
                    stream.close()

            index = list.index(block) + 1
            index |= reference_type
            return index

        def get_resource(self, index):
            return self.get_block(index, ExternalResource)

        def get_resource_index(self, key):
            return self.get_block_index(ExternalResource(key), RCOL.Reference.DELAYED)

        def read(self, stream, resource=None):
            s = StreamReader(stream)
            self.version = s.u32()
            cPublic = s.u32()
            cExternal = s.u32()
            assert cExternal == 0
            cDelayed = s.u32()
            cChunks = s.u32()
            keys = []
            for i in range(cChunks):
                key = s.tgi('ITG')
                keys.append(key)
            for i in range(cDelayed):
                key = s.tgi('ITG')
                self.__delayed.append(ExternalResource(key))
            for i in range(cChunks):
                key = keys[i]
                offset = s.u32()
                size = s.u32()
                cur = s.tell()
                s.seek(offset, SEEK_SET)
                data = stream.read(size)
                s.seek(cur, SEEK_SET)
                block = self.Chunk(key)
                block.data = data
                if i < cPublic: self.__public.append(block)
                else: self.__private.append(block)

        def write(self, stream, resource=None):
            chunks = []
            chunks.extend(self.__public)
            chunks.extend(self.__private)
            s = StreamWriter(stream)
            s.u32(self.version)
            s.i32(len(self.__public))
            s.i32(len(self.__external))
            s.i32(len(self.__delayed))
            s.i32(len(chunks))
            for block in chunks: s.tgi(block.key, 'ITG')
            for block in self.__delayed: s.tgi(block.key, 'ITG')
            data_pos = stream.tell() + (8 * len(chunks))
            data_list = []
            for block in chunks:
                data = self.__data.pop(block)
                data_len = len(data)
                s.u32(data_pos)
                s.u32(data_len)
                data_pos += data_len
                data_list.append(data)
            for data in data_list:
                stream.write(data)

    TAG = ''
    BLOCK_ID = 0x00000000
    ROOT_REFERENCE_TYPE = Reference.PUBLIC
    ROOT_BLOCK_TYPE = None

    def __init__(self, key=None, stream=None, resources=None):
        PackedResource.__init__(self, key, stream, resources)

    def read_tag(self, stream):
        stream_reader = StreamReader(stream)
        tag_len = len(self.TAG)
        t = stream_reader.chars(tag_len)
        if not t == self.TAG: raise  IOError(
            "Expected %s but got %s at 0x%X " % (self.TAG, t, (stream.tell() - tag_len)))

    def write_tag(self, stream):
        stream_writer = StreamWriter(stream)
        stream_writer.chars(self.TAG)

    def read_rcol(self, stream, rcol):
        raise NotImplementedError()

    def write_rcol(self, stream, rcol):
        raise NotImplementedError()

    def read(self, stream, package=None):
        rcol = self.Serializer(package=package)
        rcol.read(stream)
        self.read_rcol(stream, rcol)

    def write(self, stream, package=None):
        rcol = self.Serializer(package=package)
        rcol.get_block_index(self, self.ROOT_REFERENCE_TYPE)
        rcol.write(stream)