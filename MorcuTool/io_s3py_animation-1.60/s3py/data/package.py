from genericpath import exists
import io
import os
import shutil
import tempfile
from io import BytesIO, SEEK_SET, SEEK_CUR, open
from s3py.core import Serializable, ResourceKey,  Resource, PackedResource, ChildElement, DefaultResource
from s3py.helpers import Flag, first
from s3py.io import StreamReader, StreamWriter


class Package(object):

    def __init__(self, filename=None):
        """
        """
        self.stream = None
        self.filename = shutil.abspath(filename)
        if filename:
            self.stream = io.open(self.filename,'rb')
        self.entries = []
        self.header = self.Header()
        self.index_flags = 0
        self.static_key = ResourceKey()
        self.__name_map = None
        self.__loaded_resources = {}
        self.__pending_deletes = {}
        self.cache = False
        self.load()



    def get_name_map(self):
        if not self.__name_map:
            nmi = first(self.find_all_type(NameMap.ID))
            if not nmi:
                print('no name map found, creating a new one')
                nmap = NameMap()
                nmi = self.save_resource(nmap)
            print(nmi)
            self.__name_map = nmi.fetch(NameMap)
        return self.__name_map

    name_map = property(get_name_map)

    def load(self):
        if not self.stream:
            return
        s = StreamReader(self.stream)
        self.entries = []
        self.header.read(self.stream)
        self.stream.seek(self.header.index_offset, SEEK_SET)
        self.index_flags = s.u32()
        if Flag.is_set(self.index_flags, self.INDEX_FLAG.TYPE): self.static_key.t = s.u32()
        if Flag.is_set(self.index_flags, self.INDEX_FLAG.GROUP): self.static_key.g = s.u32()
        if Flag.is_set(self.index_flags, self.INDEX_FLAG.INSTANCE_HI): self.static_key.i = s.u32() <<32
        if Flag.is_set(self.index_flags, self.INDEX_FLAG.INSTANCE_LO): self.static_key.i |= s.u32()
        for i in range(self.header.index_entry_count):
            index = self.IndexEntry(self)
            index.key = ResourceKey()
            if Flag.is_set(self.index_flags, self.INDEX_FLAG.TYPE):
                index.key.t = self.static_key.t
            else:
                index.key.t = s.u32()
            if Flag.is_set(self.index_flags, self.INDEX_FLAG.GROUP):
                index.key.g = self.static_key.g
            else:
                index.key.g = s.u32()
            if Flag.is_set(self.index_flags, self.INDEX_FLAG.INSTANCE_HI):
                instance_hi = self.static_key.i >> 32
            else:
                instance_hi = s.u32()
            if Flag.is_set(self.index_flags, self.INDEX_FLAG.INSTANCE_LO):
                instance_lo = self.static_key.i & 0xFFFFFFFF
            else:
                instance_lo = s.u32()
            index.key.i = (instance_hi << 32) + instance_lo
            index.offset = s.u32()
            index.file_size = s.u32() & 0x0FFFFFFF
            index.mem_size = s.u32()
            flag = s.u16()
            index.compressed = flag == 0xFFFF
            index.unknown = s.u16()
            self.entries.append(index)
        nmap = self.name_map
        for entry in self.entries:
            entry.name =nmap[entry.key.i]
        print("Done.")

    def commit(self):
        """
        """
        tmp_filename = tempfile.mktemp()
        tmp_stream = io.open(tmp_filename,'wb')

        s = StreamWriter(tmp_stream)
        self.header.index_entry_count = len(self.entries)

        pkg_stream = None
        if self.stream:
            pkg_stream = self.stream
        elif exists(self.filename):
            pkg_stream = io.open(self.filename,'rb')


        #skip over header
        tmp_stream.seek(self.Header.SIZE, SEEK_SET)

        # Write data, update header and index
        self.index_flags = 0
        self.static_key = ResourceKey()
        if any(self.entries):
            self.index_flags = self.INDEX_FLAG.TYPE | self.INDEX_FLAG.GROUP
            static_key = None
            for index in self.entries:
                if index.delete_pending:
                    self.entries.remove(index)
                    continue
                data = None
                if index.key in self.__loaded_resources:
                    index.compressed = False
                    wrapper = self.__loaded_resources[index.key]
                    with BytesIO() as wrapper_stream:
                        wrapper.write(wrapper_stream)
                        wrapper_stream.seek(0,SEEK_SET)
                        data = wrapper_stream.read(-1)
                    index.file_size = len(data)
                    index.mem_size = index.file_size
                elif isinstance(pkg_stream, io.FileIO):
                    pkg_stream.seek(index.offset)
                    data = pkg_stream.read(index.mem_size)
                else:
                    continue
#                if static_key == None: static_key = index.key.clone()
#                if not index.key.t == static_key.t:
#                    static_key.t = 0
#                    self.index_flags = Flag.unset(self.index_flags, self.INDEX_FLAG.TYPE)
#                if not index.key.g == static_key.g:
#                    static_key.g = 0
#                    self.index_flags = Flag.unset(self.index_flags, self.INDEX_FLAG.GROUP)
                index.offset = tmp_stream.tell()
                tmp_stream.write(data)
            if self.index_flags: self.static_key = static_key

        # Write index table
        self.header.index_offset = tmp_stream.tell()
        s.u32(0)
#        if Flag.is_set(self.index_flags, self.INDEX_FLAG.TYPE):
#            s.u32(self.static_key.t)
#        if Flag.is_set(self.index_flags, self.INDEX_FLAG.GROUP):
#            s.u32(self.static_key.g)
        for index in self.entries:
#            if not Flag.is_set(self.index_flags, self.INDEX_FLAG.TYPE):
#                s.u32(index.key.t)
#            if not Flag.is_set(self.index_flags, self.INDEX_FLAG.GROUP):
#                s.u32(index.key.g)
            s.u32(index.key.t)
            s.u32(index.key.g)
            instance_hi = index.key.i >> 32
            instance_lo = index.key.i & 0xFFFFFFFF
            s.u32(instance_hi)
            s.u32(instance_lo)
            s.u32(index.offset)
            s.u32(index.file_size | 0x80000000)
            s.u32(index.mem_size)
            s.u16(0 if not index.compressed else 0xFFFF)
            s.u16(index.unknown)
        end = tmp_stream.tell()
        self.header.index_size = end - self.header.index_offset

        # Go back and write header
        tmp_stream.seek(0, SEEK_SET)
        self.header.write(tmp_stream)
        if pkg_stream:
            pkg_stream.close()
            os.unlink(self.filename)

        tmp_stream.close()
        shutil.move(tmp_filename,self.filename)

    def find(self, selector=None):
        for item in self.find_all(selector): return item
    def find_name(self, name):
        return self.find(lambda index: index.key.i in self.name_map.names and self.name_map.names[index.key.i] == name)
    def find_key(self, key):
        if isinstance(key,Resource): key = key.key
        return self.find(lambda index: index.key == key)
    def find_all(self, selector=None):
        return filter(selector, self.entries)
    def find_all_type(self,type):
        return self.find_all(lambda index: index.key.t == type)
    def find_all_instance(self, instance):
        return self.find_all(lambda index: index.key.i == instance)
    def get_resource(self,wrapper=None, index=None, key=None, recursive=False):
        if not index and key:
            index = self.find_key(key)
        else: key = index.key
        if index.key in self.__loaded_resources:
            return self.__loaded_resources[index.key]
        if index == None:
            return None
        print("Loading resource %s"%index.key)
        data = None
        if self.stream:
            package_stream = self.stream
            package_stream.seek(index.offset, SEEK_SET)
            data = package_stream.read(index.file_size)


        resource_stream = BytesIO()
        resource_stream.write(data)
        resource_stream.seek(0, SEEK_SET)
        if index.compressed:
            uncompressed = self.Compression.uncompress(resource_stream, index.file_size, index.mem_size)
            uncompressed.seek(0, SEEK_SET)
            resource_stream.close()
            resource_stream = uncompressed
        if not wrapper:
            return resource_stream
#        name = None
#        if key.t != NameMap.ID and key.i in self.name_map.names:
#            name = self.name_map.names[key.i]
        resource = wrapper(key)
        resource.resource_name = index.name
        resource.read(resource_stream)
        if self.cache:
            self.__loaded_resources[index] = resource
        return resource

    def remove_resource(self, resource):
        """
        """
        index = None
        if isinstance(resource, Package.IndexEntry): index = resource
        elif isinstance(resource, ResourceKey): index = self.find_key(resource)
        elif isinstance(resource, Resource): index = self.find_key(resource.key)
        else:
            return
        if index:
            index.delete_pending = True

    def save_resource(self, resource):
        """
        """
        if not isinstance(resource, PackedResource):
            return
        index = self.find_key(resource.key)
        if index == None:
            index = self.IndexEntry(self)
            index.key = resource.key
            self.entries.append(index)
        self.__loaded_resources[index.key] = resource
        return index

    def revert_resource(self,resource,wrapper=None):

        if not isinstance(resource, PackedResource):
            return
        index = self.find_key(resource.key)
        if index:
            if resource.key in self.__loaded_resources:
                self.__loaded_resources.pop(resource.key)
            if not wrapper:
                wrapper = type(resource)
            return index.fetch(wrapper)





    class INDEX_FLAG:
        TYPE = 0x00000001
        GROUP = 0x00000002
        INSTANCE_HI = 0x00000004
        INSTANCE_LO = 0x00000008


    class IndexEntry(object):
        SIZE = 32
        __slots__ = {
            'source',
            'offset',
            'file_size',
            'mem_size',
            'compressed',
            'unknown',
            'key',
            'delete_pending',
            'name'
        }

        def __init__(self, package):
            """
            """
            self.offset = 0
            self.file_size = 0
            self.mem_size = 0
            self.compressed = False
            self.unknown = 1
            self.key = ResourceKey()
            self.delete_pending = False
            self.source = package
            self.name = ''


        def fetch(self, wrapper=DefaultResource):
            p = self.source
            if isinstance(p, Package):
                return p.get_resource(wrapper, index=self)

        def __str__(self):
            return str(self.key)

        def __eq__(self, other):
            return isinstance(other, Package.IndexEntry) and other.key == self.key

        def __hash__(self):
            return hash(self.key)

    class Header(Serializable):
        MAGIC = 'DBPF'
        SIZE = 0x00000060

        def __init__(self, stream=None):
            """
            """
            self.major_version = 2
            self.minor_version = 0
            self.unknown1 = 0
            self.unknown2 = 0
            self.unknown3 = 0
            self.date_created = 0
            self.date_modified = 0

            self.index_major_version = 0
            self.index_entry_count = 0
            self.index_entry_offset = 0
            self.index_size = 0
            #<2.0
            self.hole_count = 0
            self.hole_offset = 0
            self.hole_size = 0
            self.index_minor_version = 0

            self.index_offset = 0
            self.unknown4 = 0
            self.reserved = [0] * 24
            Serializable.__init__(self, stream)

        def read(self, stream, resource=None):
            """
            """
            s = StreamReader(stream)
            tag = s.chars(4)
            assert tag == self.MAGIC

            self.major_version = s.u32()
            self.minor_version = s.u32()

            self.unknown1 = s.u32()
            self.unknown2 = s.u32()
            self.unknown3 = s.u32()
            self.date_created = s.u32()
            self.date_modified = s.u32()
            self.index_major_version = s.u32()

            self.index_entry_count = s.u32()
            self.index_entry_offset = s.u32()
            self.index_size = s.u32()

            self.hole_count = s.u32()
            self.hole_offset = s.u32()
            self.hole_size = s.u32()

            self.index_minor_version = s.u32()

            self.index_offset = s.u32()
            self.unknown4 = s.u32()
            self.reserved = [0] * 24
            for i in range(24): self.reserved[i] = s.u32()

        def write(self, stream, resource=None):
            """
            """
            s = StreamWriter(stream)
            s.chars(self.MAGIC)
            s.u32(self.major_version)
            s.u32(self.minor_version)
            s.u32(self.unknown1)
            s.u32(self.unknown2)
            s.u32(self.unknown3)
            s.u32(self.date_created)
            s.u32(self.date_modified)

            s.u32(self.index_major_version)
            s.u32(self.index_entry_count)
            s.u32(self.index_entry_offset)
            s.u32(self.index_size)
            s.u32(self.hole_count)
            s.u32(self.hole_offset)
            s.u32(self.hole_size)
            s.u32(self.index_minor_version)

            s.u32(self.index_offset)
            s.u32(self.unknown4)
            for i in range(24): s.u32(self.reserved[i])

    class Compression:
        """
        Adapted from s3pi
        """

        @staticmethod
        def copy_a(stream, offset, length):
            while length > 0:
                dst = stream.tell()
                block_len = min(offset, length)
                length -= block_len
                stream.seek(-1 * offset, SEEK_CUR)
                block = stream.read(block_len)
                stream.seek(dst, SEEK_SET)
                stream.write(block)

        @staticmethod
        def copy_b(stream, offset, length):
            while length > 0:
                dst = stream.tell()
                length -= 1
                stream.seek(-1 * offset, SEEK_CUR)
                block = stream.read(1)
                stream.seek(dst, SEEK_SET)
                stream.write(block)

        @classmethod
        def uncompress(cls,input_stream, file_size, mem_size):
            output_stream = BytesIO()
            br = StreamReader(input_stream)
            end = input_stream.tell() + file_size
            data = br.bytes(2)
            data_len = (4 if ((data[0] & 0x80) != 0) else 3 ) * ( 2 if ((data[0] & 0x01) != 0) else 1 )
            data = br.bytes(data_len)
            real_size = 0
            for i in range(data_len): real_size = (real_size << 8) + data[i]
            assert real_size == mem_size
            while input_stream.tell() < end:
                copy_size = 0
                copy_offset = 0
                packing = br.u8()
                if packing < 0x80:
                    data = br.bytes(1)
                    data_len = packing & 0x03
                    copy_size = ((packing >> 2) & 0x07) + 3
                    copy_offset = (((packing << 3) & 0x300) | data[0]) + 1
                elif packing < 0xC0:
                    data = br.bytes(2)
                    data_len = (data[0] >> 6) & 0x03
                    copy_size = (packing & 0x3F) + 4
                    copy_offset = (((data[0] << 8) & 0x3F00) | data[1]) + 1
                elif packing < 0xE0:
                    data = br.bytes(3)
                    data_len = packing & 0x03
                    copy_size = (((packing << 6) & 0x300) | data[2]) + 5
                    copy_offset = (((packing << 12) & 0x10000) | data[0] << 8 | data[1]) + 1
                elif packing < 0xFC:
                    data_len = (((packing & 0x1F) + 1) << 2)
                else:
                    data_len = packing & 0x03
                if data_len > 0:
                    output_stream.write(input_stream.read(data_len))
                if copy_size < copy_offset > 8:
                    cls.copy_a(output_stream, copy_offset, copy_size)
                else:
                    cls.copy_b(output_stream, copy_offset, copy_size)
            output_stream.seek(0, SEEK_SET)
            return output_stream


class NameMap(PackedResource):
    ID = 0x0166038C

    class VERSION:
        DEFAULT = 0x00000001

    def __init__(self, key=None):
        PackedResource.__init__(self, key)
        self.names = {}
        self.version = self.VERSION.DEFAULT

    def __getitem__(self, item):
        return '' if not item in self.names else self.names[item]

    def __setitem__(self, key, value):
        self.names[key] = value
    def __contains__(self, item):
        return item in self.names
    def __len__(self):
        return len(self.names)

    def clear(self):
        self.names.clear()

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.version = s.u32()
        cNames = s.u32()
        for i in range(cNames):
            iid = s.u64()
            name = s.p32()
            self.names[iid] = name

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        keys = self.names.keys()
        s.u32(len(keys))
        for key in keys:
            s.u64(key)
            s.p32(self.names[key])

