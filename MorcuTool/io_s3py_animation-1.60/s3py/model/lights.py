from io import SEEK_SET
from s3py.core import Serializable
from s3py.helpers import get_subclasses
from s3py.io import StreamWriter, StreamReader, RCOL


class Light(Serializable):
    TYPE = 0x00000000
    SUB_CLASSES = None


    def __init__(self, stream=None):
        self.transform = [0.0, 0.0, 0.0]
        self.colour = [0.0, 0.0, 0.0]
        self.intensity = 1.0
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.transform = [s.f32() for i in range(3)]
        self.colour = [s.f32() for i in range(3)]
        self.intensity = s.f32()

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        for i in range(3): s.f32(self.transform[i])
        for i in range(3): s.f32(self.colour[i])
        s.f32(self.intensity)

    @staticmethod
    def get_type(t):
        if Light.SUB_CLASSES == None:
            Light.SUB_CLASSES = {}
            classes = get_subclasses(Light)
            for c in classes: Light.SUB_CLASSES[c.TYPE] = c
        if not t in Light.SUB_CLASSES: raise Exception("Unknown light type: " + hex(t))
        return  Light.SUB_CLASSES[t]


class AmbientLight(Light):
    TYPE = 0x00000001


class DirectionalLight(Light):
    TYPE = 0x00000002


class PointLight(Light):
    TYPE = 0x00000003


class SpotLight(Light):
    TYPE = 0x00000004

    def __init__(self):
        Light.__init__(self)
        self.at = [0.0, 0.0, 0.0]
        self.falloff_angle = 0.0
        self.blur_scale = 0.0

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        Light.read(self, stream)
        self.at = [s.f32() for i in range(3)]
        self.falloff_angle = s.f32()
        self.blur_scale = s.f32()

    def write(self, stream, resource=None):
        Light.write(self, stream)
        s = StreamWriter(stream)
        for i in range(3): s.f32(self.at[i])
        s.f32(self.falloff_angle)
        s.f32(self.blur_scale)


class LampShadeLight(Light):
    TYPE = 0x00000005

    def __init__(self):
        Light.__init__(self)
        self.at = [0.0, 0.0, 0.0]
        self.falloff_angle = 0.0
        self.shade_light_rig_multiplier = 0.0
        self.bottom_angle = 0
        self.shade_colour = [0.0, 0.0, 0.0]

    def read(self, stream, resource=None):
        Light.read(self, stream)
        s = StreamReader(stream)
        self.at = [s.f32() for i in range(3)]
        self.falloff_angle = s.f32()
        self.shade_light_rig_multiplier = s.f32()
        self.bottom_angle = s.f32()
        self.shade_colour = [s.f32() for i in range(3)]

    def write(self, stream, resource=None):
        Light.write(self, stream)
        s = StreamWriter(stream)
        for i in range(3): s.f32(self.at[i])
        s.f32(self.falloff_angle)
        s.f32(self.shade_light_rig_multiplier)
        s.f32(self.bottom_angle)
        for i in range(3): s.f32(self.shade_colour[i])


class TubeLight(Light):
    TYPE = 0x00000006

    def __init__(self):
        Light.__init__(self)
        self.at = [0.0, 0.0, 0.0]
        self.tube_length = 0.0
        self.blur_scale = 0.0

    def read(self, stream, resource=None):
        Light.read(self, stream)
        s = StreamReader(stream)
        self.at = [s.f32() for i in range(3)]
        self.tube_length = s.f32()
        self.blur_scale = s.f32()

    def write(self, stream, resource=None):
        Light.write(self, stream)
        s = StreamWriter(stream)
        for i in range(3): s.f32(self.at[i])
        s.f32(self.tube_length)
        s.f32(self.blur_scale)


class SquareAreaLight(Light):
    TYPE = 0x00000009

    def __init__(self):
        Light.__init__(self)
        self.at = [0.0, 0.0, 0.0]
        self.right = [0.0, 0.0, 0.0]
        self.width = 0.0
        self.height = 0.0
        self.falloff_angle = 0
        self.window_top_bottom_angle = 0

    def read(self, stream, resource=None):
        Light.read(self, stream)
        s = StreamReader(stream)
        self.at = [s.f32() for i in range(3)]
        self.right = [s.f32() for i in range(3)]
        self.width = s.f32()
        self.height = s.f32()
        self.falloff_angle = s.f32()
        self.window_top_bottom_angle = s.f32()

    def write(self, stream, resource=None):
        Light.write(self, stream)
        s = StreamWriter(stream)
        for i in range(3): s.f32(self.at[i])
        for i in range(3): s.f32(self.right[i])
        s.f32(self.width)
        s.f32(self.height)
        s.f32(self.falloff_angle)
        s.f32(self.window_top_bottom_angle)


class SquareWindowLight(SquareAreaLight):
    TYPE = 0x00000007


class DiscAreaLight(Light):
    TYPE = 0x0000000A

    def __init__(self):
        Light.__init__(self)
        self.at = [0.0, 0.0, 0.0]
        self.right = [0.0, 0.0, 0.0]
        self.radius = 0.0

    def read(self, stream, resource=None):
        Light.read(self, stream)
        s = StreamReader(stream)
        self.at = [s.f32() for i in range(3)]
        self.right = [s.f32() for i in range(3)]
        self.radius = s.f32()

    def write(self, stream, resource=None):
        Light.write(self, stream)
        s = StreamWriter(stream)
        for i in range(3): s.f32(self.at[i])
        for i in range(3): s.f32(self.right[i])
        s.f32(self.radius)


class CircularWindowLight(DiscAreaLight):
    TYPE = 0x00000008


class WorldLight(Light):
    TYPE = 0x0000000B


class Occluder(Serializable):
    class TYPES:
        DISC = 0x00000000
        RECTANGLE = 0x00000001

    def __init__(self, stream=None):
        self.type = self.TYPES.RECTANGLE
        self.origin = [0.0, 0.0, 0.0]
        self.normal = [0.0, 0.0, 0.0]
        self.x_axis = [0.0, 0.0, 0.0]
        self.y_axis = [0.0, 0.0, 0.0]
        self.pair_offset = 0.0
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.type = s.u32()
        self.origin = [s.f32() for i in range(3)]
        self.normal = [s.f32() for i in range(3)]
        self.x_axis = [s.f32() for i in range(3)]
        self.y_axis = [s.f32() for i in range(3)]
        self.pair_offset = s.f32()

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.type)
        for i in range(3): s.f32(self.origin[i])
        for i in range(3): s.f32(self.normal[i])
        for i in range(3): s.f32(self.x_axis[i])
        for i in range(3): s.f32(self.y_axis[i])
        s.f32(self.pair_offset)


class LightRig(RCOL):
    ID = 0x03B4C61D
    TAG = 'LITE'

    class VERSION:
        DEFAULT = 0x00000004

    def __init__(self, key):
        RCOL.__init__(self, key)
        self.version = self.VERSION.DEFAULT
        self.lights = []
        self.occluders = []

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        self.version = s.u32()
        num = s.u32()
        cLights = s.u8()
        cOccluders = s.u8()
        assert num == (4 + (cLights * 128) + (cOccluders * 14))
        num2 = s.u16()
        assert num2 == (cOccluders * 14)

        self.lights = []
        for light_index in range(cLights):
            start = stream.tell()
            light_type = s.u32()
            light = Light.get_type(light_type)()
            light.read_rcol(stream)
            stream.seek(start + 128, SEEK_SET)
            self.lights.append(light)
        self.occluders = [Occluder(stream) for i in range(cOccluders)]

    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        cLights = len(self.lights)
        cOccluders = len(self.occluders)
        s.u32(4 + (cLights * 128) + (cOccluders * 14))
        s.u8(cLights)
        s.u8(cOccluders)
        s.u16(cOccluders * 14)
        for light in self.lights:
            start = stream.tell()
            light.write_rcol(stream)
            end = stream.tell()
            size = end - start
            blank = 128 - size
            dwords = int(blank / 4)
            for i in range(int(dwords)): s.u32(0)
        for occluder in self.occluders:
            occluder.write_rcol(stream)
