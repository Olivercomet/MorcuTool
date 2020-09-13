class Serializable(object):
    def __init__(self, stream=None, resources=None):
        if stream != None:
            self.read(stream, resources)

    def read(self, stream, resources=None):
        """
        Reads data from a stream

        stream IOBase: stream source
        """
        raise NotImplementedError()

    def write(self, stream, resources=None):
        """
        Reads data from a stream

        stream IOBase: stream source
        """
        raise NotImplementedError()


class ChildElement(object):
    def __init__(self, parent=None):
        self.parent = parent


class ResourceKey(object):
    def __init__(self, t=0, g=0, i=0, key=None):
        """
        t int: Resource Type
        g int: Resource Group
        i int: Resource Instance
        key ResourceKey: other resource key
        """
        if isinstance(key, Resource):
            key = key.key
        if isinstance(key, ResourceKey):
            self.t = key.t
            self.g = key.g
            self.i = key.i
        else:
            self.t = t
            self.g = g
            self.i = i

    def __hash__(self):
        return hash(self.t) ^ hash(self.g) ^ hash(self.i)

    def clone(self):
        return ResourceKey(key=self)

    def copy(self):
        return self.clone()

    def __eq__(self, other):
        return isinstance(other, ResourceKey) and self.t == other.t and self.g == other.g and self.i == other.i

    def __str__(self):
        return "%08X:%08X:%016X" % (self.t, self.g, self.i)

    def s3pi_name(self,tag,extension,name=None):
        return 'S3_%08X_%08X_%016X_%s%%%%+%s.%s' % (self.t,self.g,self.i,name,tag,extension)


class Resource(object):
    ID = 0x00000000

    def __init__(self, key=None):
        """
        key ResourceKey:
        """
        if not key:
            key = ResourceKey(self.ID)
        self.key = ResourceKey(key=key)

    def parse_version(self):
        raise NotImplementedError()
    def __str__(self):
        return str(self.key)
    def __eq__(self, other):
        return isinstance(other,Resource) and self.key == other.key
    def __hash__(self):
        return hash(self.key)


class ExternalResource(Resource):
    def __init__(self, key=None):
        Resource.__init__(self, key)


class ResourceKeyProvider:
    def get_resource(self, index):
        """
        Retrieves a resource from an index
        index int:
        return Resource:
        """
        raise NotImplementedError("get_key not implemented!")

    def get_resource_index(self, key):
        """
        Adds a resource to this collection if not present, and returns an index to it
        resource Resource: resource to be indexed
        return int:
        """
        raise NotImplementedError("get_index not implemented!")


class PackedResource(Serializable, Resource):
    def __init__(self, key=None, stream=None, resources=None, name=None):
        Serializable.__init__(self, stream, resources)
        Resource.__init__(self, key)
        self.resource_name = name

    def __str__(self):
        return "%s %s %s" % (type(self), self.resource_name, self.key)


class DefaultResource(PackedResource):
    def __init__(self, key=None, stream=None):
        self.data = None
        PackedResource.__init__(self, key, stream)

    def read(self, stream, resource=None):
        self.data = stream.read(-1)

    def write(self, stream, resource=None):
        stream.write(self.data)