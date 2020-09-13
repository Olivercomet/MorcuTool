import math
from s3py.core import Serializable
from s3py.io import StreamWriter, StreamReader


class Vertex:
    __slots__ = {
        'position',
        'normal',
        'uv',
        'blend_indices',
        'blend_weights',
        'tangent',
        'colour',
        'id'
    }

    def __init__(self):
        self.position = None
        self.normal = None
        self.uv = None
        self.blend_indices = None
        self.blend_weights = None
        self.tangent = None
        self.colour = None
        self.id = None

    def __str__(self):
        return "Position: %s, Normal: %s, UV: %s" % (self.position, self.normal, self.uv)


class BoundingBox(Serializable):
    def __init__(self, dimensions=3, stream=None, resources=None):
        self.dimensions = dimensions
        self.min = [0.0] * dimensions
        self.max = [0.0] * dimensions
        Serializable.__init__(self, stream, resources)

    def clear(self):
        for i in range(self.dimensions):
            self.min[i] = 0.0
            self.max[i] = 0.0

    def add(self, vector):
        if isinstance(vector, BoundingBox):
            self.add(vector.min)
            self.add(vector.max)
            return
        if len(vector) != self.dimensions:
            raise Exception('Expected a %sD Vector' % self.dimensions)
        for index, value in enumerate(vector):
            if value < self.min[index]:
                self.min[index] = value
            if value > self.max[index]:
                self.max[index] = value

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        self.min = [s.f32() for i in range(self.dimensions)]
        self.max = [s.f32() for i in range(self.dimensions)]

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        for val in self.min: s.f32(val)
        for val in self.max: s.f32(val)


class SkinController:
    class Bone:
        def __init__(self, name=None, inverse_bind_pose=None):
            self.name = name
            if inverse_bind_pose is None: inverse_bind_pose = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0]]
            self.inverse_bind_pose = inverse_bind_pose

    def __init__(self):
        self.bones = []


class Mesh:
    """
    An interface for mesh types
    """

    def get_vertices(self):
        raise NotImplementedError()

    def get_triangles(self):
        raise NotImplementedError()
