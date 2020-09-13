import math
from s3py.core import Serializable, ExternalResource, ResourceKey
from s3py.io import StreamReader, TGIList, StreamWriter, RCOL
from s3py.model.geometry import BoundingBox


class LOD_ID:
    HIGH_DETAIL = 0x00000000
    MEDIUM_DETAIL = 0x00000001
    LOW_DETAIL = 0x00000002
    SHADOW_HIGH_DETAIL = 0x00010000
    SHADOW_MEDIUM_DETAIL = 0x00010001
    SHADOW_LOW_DETAIL = 0x00010002


class FootprintTypeFlags:
    ForPlacement = 0x00000001
    ForPathing = 0x00000002
    IsEnabled = 0x00000004
    IsDiscouraged = 0x00000008
    ForShell = 0x00000010


class AllowIntersectionFlags:
    Walls = 0x00000002
    Objects = 0x00000004
    Sims = 0x00000008
    Roofs = 0x00000010
    Fences = 0x00000020
    ModularStairs = 0x00000040
    ObjectsOfSameType = 0x00000080


class SurfaceTypeFlags:
    Terrain = 0x00000001
    Floor = 0x00000002
    Pool = 0x00000004
    Pond = 0x00000008
    Fence = 0x00000010
    AnySurface = 0x00000020
    Air = 0x00000040
    Roof = 0x00000080


class SurfaceAttributeFlags:
    Inside = 0x00000001
    Outside = 0x00000002
    Slope = 0x00000004


class VisualProxy(RCOL):
    ID = 0x736884F1
    TAG = 'VPXY'

    class VERSION:
        DEFAULT = 0x00000004

    def __init__(self, key=None, stream=None):
        self.version = self.VERSION.DEFAULT
        self.bounds = BoundingBox()
        self.flags = 0x00000000
        self.routing_footprint = ExternalResource(ResourceKey())
        self.entries = []
        RCOL.__init__(self, key, stream)

    def read_rcol(self, stream, rcol):
        self.read_tag(stream)
        s = StreamReader(stream)
        tgi = TGIList()
        self.version = s.u32()
        tgi.begin_read(stream)
        cEntries = s.u8()
        for entry_index in range(cEntries):
            type = s.u8()
            entry = self.Entry.create_instance(type)
            entry.read(stream, tgi)
            self.entries.append(entry)
        assert s.u8() == 0x02
        self.bounds.read(stream)
        self.flags = s.u32()
        if s.u8():
            self.routing_footprint = tgi.get_resource(s.u32())
        tgi.end_read(stream)

    def write_rcol(self, stream, rcol):
        self.write_tag(stream)
        s = StreamWriter(stream)
        s.u32(self.version)
        tgi = TGIList()
        tgi.begin_write(stream)
        s.u8(len(self.entries))
        for entry in self.entries:
            s.u8(entry.TYPE)
            entry.write(stream, tgi)
        s.u8(2)
        self.bounds.write(stream)
        s.u32(self.flags)
        if self.routing_footprint.key != ResourceKey():
            s.u8(1)
            s.tgi(self.routing_footprint.key, 'TGI')
        else: s.u8(0)
        tgi.end_write(stream)

    class Entry(Serializable):
        def __init__(self, stream=None, resources=None):
            Serializable.__init__(self, stream, resources)

        @staticmethod
        def create_instance(type):
            if type == VisualProxy.MiscEntry.TYPE: return VisualProxy.MiscEntry()
            elif type == VisualProxy.LodEntry.TYPE: return VisualProxy.LodEntry()

    class MiscEntry(Entry):
        TYPE = 0x00000001

        def __init__(self, stream=None, resources=None, parent=None):
            self.index = 0
            self.resource = ExternalResource(ResourceKey())
            VisualProxy.Entry.__init__(self, stream, resources)

        def read(self, stream, tgi):
            s = StreamReader(stream)
            self.index = s.u32()
            self.resource = tgi.get_resource(self.index)

        def write(self, stream, tgi):
            s = StreamWriter(stream)
            s.u32(self.index)
            s.u32(tgi.get_resource_index(self.resource))

        def __str__(self):
            return "%s" % self.resource

    class LodEntry(Entry):
        TYPE = 0x00000000

        def __init__(self, stream=None, resources=None):
            self.index = 0
            self.resources = []
            VisualProxy.Entry.__init__(self, stream, resources)

        def read(self, stream, resources):
            s = StreamReader(stream)
            self.index = s.u8()
            self.resources = [resources.get_resource(s.u32()) for i in range(s.u8())]

        def write(self, stream, resources):
            s = StreamWriter(stream)
            s.u8(self.index)
            s.u8(len(self.resources))
            for resource in self.resources: s.u32(resources.get_resource_index(resource))

        def __str__(self):
            return "%s" % self.resources


class Footprint(RCOL):
    TAG = 'FTPT'
    ID = 0xD382BF57

    class VERSION:
        DEFAULT = 0x00000006
        EXTENDED = 0x00000007

    def __init__(self, key=None, stream=None):
        self.version = self.VERSION.DEFAULT
        self.footprint_polygons = []
        self.routing_slot_footprint_polygons = []
        RCOL.__init__(self, key, stream)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.footprint_polygons = [self.Area(stream) for i in range(s.i8())]
        self.routing_slot_footprint_polygons = [self.Area(stream) for i in range(s.i8())]

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        cFootprints = len(self.footprint_polygons)
        s.i8(cFootprints)
        for footprint_index in range(cFootprints): self.footprint_polygons[footprint_index].write_rcol(stream)
        cSlots = len(self.routing_slot_footprint_polygons)
        s.i8(cSlots)
        for slot_index in range(cSlots): self.routing_slot_footprint_polygons[slot_index].write_rcol(stream)

    class Area(Serializable):
        def __init__(self, ftpt, stream=None):
            self.ftpt = ftpt
            self.name = None
            self.priority = 0
            self.footprint_type_flags = 0
            self.points = []
            self.allow_intersection_flags = 0
            self.surface_type_flags = 0
            self.surface_attribute_flags = 0
            self.level_offset = 0
            self.elevation_offset = 0
            self.bounds = BoundingBox(dimensions=2)
            Serializable.__init__(self, stream)

        def read(self, stream, resource=None):
            s = StreamReader(stream)
            self.name = s.u32()
            self.priority = s.i8()
            self.footprint_type_flags = s.u32()
            self.points = [(s.f32(), s.f32()) for i in range(s.i32())]
            self.allow_intersection_flags = s.u32()
            self.surface_type_flags = s.u32()
            self.surface_attribute_flags = s.u32()
            self.level_offset = s.i32()
            if self.ftpt.version >= Footprint.VERSION.EXTENDED: self.elevation_offset = s.f32()
            self.bounds.read(stream)

        def write(self, stream, resource=None):
            s = StreamWriter(stream)
            self.bounds.clear()
            s.hash(self.name)
            s.i8(self.priority)
            s.u32(self.footprint_type_flags)
            s.i32(len(self.points))
            for point in self.points:
                self.bounds.add(point)
                s.f32(point[0])
                s.f32(point[1])
            s.u32(self.allow_intersection_flags)
            s.u32(self.surface_type_flags)
            s.u32(self.surface_attribute_flags)
            s.i8(self.level_offset)
            if self.ftpt.version >= Footprint.VERSION.EXTENDED: s.f32(self.elevation_offset)
            self.bounds.write(stream)


class SlotOffset:
    def __init__(self, position=None, rotation=None):
        if position == None:
            position = [0.0, 0.0, 0.0]
        if rotation == None:
            rotation = [0.0, 0.0, 0.0]
        self.position = position
        self.rotation = rotation


class Slot:
    NAMES = {
        0x31229A44: "RoutingSlot_8",
        0x31229A45: "RoutingSlot_9",
        0x31229A48: "RoutingSlot_4",
        0x31229A49: "RoutingSlot_5",
        0x31229A4A: "RoutingSlot_6",
        0x31229A4B: "RoutingSlot_7",
        0x31229A4C: "RoutingSlot_0",
        0x31229A4D: "RoutingSlot_1",
        0x31229A4E: "RoutingSlot_2",
        0x31229A4F: "RoutingSlot_3",
        0x318A2824: "FXJoint_8",
        0x318A2825: "FXJoint_9",
        0x318A2828: "FXJoint_4",
        0x318A2829: "FXJoint_5",
        0x318A282A: "FXJoint_6",
        0x318A282B: "FXJoint_7",
        0x318A282C: "FXJoint_0",
        0x318A282D: "FXJoint_1",
        0x318A282E: "FXJoint_2",
        0x318A282F: "FXJoint_3",
        0x4BE763D2: "ContainmentSlot_62",
        0x4BE763D3: "ContainmentSlot_Sentinel",
        0x4DE76734: "ContainmentSlot_42",
        0x4DE76735: "ContainmentSlot_43",
        0x4DE76737: "ContainmentSlot_41",
        0x4FE76A1C: "ContainmentSlot_20",
        0x4FE76A1D: "ContainmentSlot_21",
        0x4FE76A1E: "ContainmentSlot_22",
        0x4FE76A1F: "ContainmentSlot_23",
        0x52E76ED0: "ContainmentSlot_15",
        0x52E76ED1: "ContainmentSlot_14",
        0x52E76ED2: "ContainmentSlot_17",
        0x52E76ED3: "ContainmentSlot_16",
        0x52E76ED4: "ContainmentSlot_11",
        0x52E76ED5: "ContainmentSlot_10",
        0x52E76ED6: "ContainmentSlot_13",
        0x52E76ED7: "ContainmentSlot_12",
        0x52E76EDC: "ContainmentSlot_19",
        0x52E76EDD: "ContainmentSlot_18",
        0x5CB037A1: "PlacementSlot_N",
        0x5CB037AA: "PlacementSlot_E",
        0x5CB037B8: "PlacementSlot_W",
        0x5CB037B9: "PlacementSlot_Sentinel",
        0x5CB037BC: "PlacementSlot_S",
        0x5D33660A: "FXJoint_SandsOfUnderstanding",
        0x5D33660B: "FXJoint_Sentinel",
        0x9CEC4DC8: "FXJoint_Science_3",
        0x9CEC4DC9: "FXJoint_Science_2",
        0x9CEC4DCA: "FXJoint_Science_1",
        0x9CEC4DCB: "FXJoint_Science_0",
        0xA2C0A4B0: "IKTarget_3",
        0xA2C0A4B1: "IKTarget_2",
        0xA2C0A4B2: "IKTarget_1",
        0xA2C0A4B3: "IKTarget_0",
        0xA2C0A4B4: "IKTarget_7",
        0xA2C0A4B5: "IKTarget_6",
        0xA2C0A4B6: "IKTarget_5",
        0xA2C0A4B7: "IKTarget_4",
        0xA2C0A4BA: "IKTarget_9",
        0xA2C0A4BB: "IKTarget_8",
        0xA678E700: "RoutingSlot_17",
        0xA678E701: "RoutingSlot_16",
        0xA678E702: "RoutingSlot_15",
        0xA678E703: "RoutingSlot_14",
        0xA678E704: "RoutingSlot_13",
        0xA678E705: "RoutingSlot_12",
        0xA678E706: "RoutingSlot_11",
        0xA678E707: "RoutingSlot_10",
        0xA678E70E: "RoutingSlot_19",
        0xA678E70F: "RoutingSlot_18",
        0xA820F8A0: "ContainmentSlot_6",
        0xA820F8A1: "ContainmentSlot_7",
        0xA820F8A2: "ContainmentSlot_4",
        0xA820F8A3: "ContainmentSlot_5",
        0xA820F8A4: "ContainmentSlot_2",
        0xA820F8A5: "ContainmentSlot_3",
        0xA820F8A6: "ContainmentSlot_0",
        0xA820F8A7: "ContainmentSlot_1",
        0xA820F8AE: "ContainmentSlot_8",
        0xA820F8AF: "ContainmentSlot_9",
        0xCD68F001: "TransformBone"
    }

    def __init__(self):
        self.name = None
        self.bone_name = None
        self.transform =\
        [
            [1.0, 0.0, 0.0, 0.0],
            [0.0, 1.0, 0.0, 0.0],
            [0.0, 0.0, 1.0, 0.0]
        ]
        self.offset = None


class RoutingSlot(Slot):
    def __init__(self):
        Slot.__init__(self)


class ContainerSlot(Slot):
    def __init__(self):
        Slot.__init__(self)
        self.flags = 0x00000000


class EffectSlot(Slot):
    def __init__(self):
        Slot.__init__(self)


class TargetSlot(Slot):
    def __init__(self):
        Slot.__init__(self)


class ConeSlot(Slot):
    def __init__(self):
        Slot.__init__(self)
        self.cone_radius = 1.0
        self.cone_angle = math.pi / 4.0


class SlotRig(RCOL):
    ID = 0xD3044521
    TAG = 'RSLT'

    def __init__(self, key=None, stream=None):
        self.version = 0
        self.container_slots = []
        self.routing_slots = []
        self.effect_slots = []
        self.target_slots = []
        self.cone_slots = []
        RCOL.__init__(self, key, stream)


    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()

        self.routing_slots = [RoutingSlot() for i in range(s.i32())]
        self.container_slots = [ContainerSlot() for i in range(s.i32())]
        self.effect_slots = [EffectSlot() for i in range(s.i32())]
        self.target_slots = [TargetSlot() for i in range(s.i32())]
        self.cone_slots = [ConeSlot() for i in range(s.i32())]

        def read_names(slots):
            for slot in slots: slot.name = s.hash(Slot.NAMES)

        def read_bones(slots):
            for slot in slots: slot.bone_name = s.u32()

        def read_transforms(slots):
            for slot in slots: slot.transform = s.m43()

        def read_offsets(slots):
            if any(slots):
                for i in range(s.i32()):
                    slots[i].offset = SlotOffset([s.f32(), s.f32(), s.f32()], [s.f32(), s.f32(), s.f32()])
                    pass
                pass

        read_names(self.routing_slots)
        read_bones(self.routing_slots)
        read_transforms(self.routing_slots)
        read_offsets(self.routing_slots)

        read_names(self.container_slots)
        read_bones(self.container_slots)
        for slot in self.container_slots:
            slot.flags = s.u32()
        read_transforms(self.container_slots)
        read_offsets(self.container_slots)

        read_names(self.effect_slots)
        read_bones(self.effect_slots)
        read_transforms(self.effect_slots)
        read_offsets(self.effect_slots)

        read_names(self.target_slots)
        read_bones(self.target_slots)
        read_transforms(self.target_slots)
        read_offsets(self.target_slots)

        read_names(self.cone_slots)
        read_bones(self.cone_slots)
        read_transforms(self.cone_slots)
        for cone_slot in self.cone_slots:
            cone_slot.radius = s.f32()
            cone_slot.angle = s.f32()
        read_offsets(self.cone_slots)
        pass

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)

        def write_names(slots):
            for slot in slots: s.hash(slot.name)

        def write_bones(slots):
            for slot in slots: s.hash(slot.bone_name)

        def write_transforms(slots):
            for slot in slots:
                s.m43(slot.transform)

        def write_offsets(slots):
            if any(slots):
                s.i32(len(list(filter(lambda slot: slot.offset != None, slots))))
                for i, slot in enumerate(slots):
                    if slot.offset != None:
                        s.i32(i)
                        for i in range(3): s.i32(slot.offset.position)
                        for i in range(3): s.i32(slot.offset.rotation)

        self.write_tag(stream)
        s.u32(self.version)
        s.i32(len(self.routing_slots))
        s.i32(len(self.container_slots))
        s.i32(len(self.effect_slots))
        s.i32(len(self.target_slots))
        s.i32(len(self.cone_slots))

        write_names(self.routing_slots)
        write_bones(self.routing_slots)
        write_transforms(self.routing_slots)
        write_offsets(self.routing_slots)

        write_names(self.container_slots)
        write_bones(self.container_slots)
        for slot in self.container_slots:
            s.u32(slot.flags)
        write_transforms(self.container_slots)
        write_offsets(self.container_slots)

        write_names(self.effect_slots)
        write_bones(self.effect_slots)
        write_transforms(self.effect_slots)
        write_offsets(self.effect_slots)

        write_names(self.target_slots)
        write_bones(self.target_slots)
        write_transforms(self.target_slots)
        write_offsets(self.target_slots)

        write_names(self.cone_slots)
        write_bones(self.cone_slots)
        write_transforms(self.cone_slots)
        for slot in self.cone_slots:
            s.f32(slot.radius)
            s.f32(slot.angle)
        write_offsets(self.cone_slots)


