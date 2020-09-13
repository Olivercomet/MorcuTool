
from s3py.core import Serializable, PackedResource, ExternalResource, ChildElement, ResourceKey
from s3py.cas import Age, Species
from s3py.helpers import Flag
from s3py.io import StreamReader, StreamWriter, TGIList
from s3py.cas.geometry import BlendGeometry
from s3py.material import Preset


class ShoeMaterial:
    BARE = 'bare'
    HEEL = 'heel'
    LEATHER = 'leath'
    RUBBER = 'rub'
    SANDAL = 'sand'
    SLIPPER = 'slip'


class CASPart(PackedResource):
    ID = 0x034AEECB

    class VERSION:
        DEFAULT = 0x00000012

    class CasPreset(Preset,Serializable):
        def __init__(self, stream=None, resources=None):
            self.unknown = 0
            Preset.__init__(self)
            Serializable.__init__(self,stream,resources)

        def read(self, stream, resources=None):
            s = StreamReader(stream)
            length = s.u32()
            complateStr = s.chars(length, 16)
            self.unknown = s.u32()
            self.read_xml(complateStr, resources)

        def write(self, stream, resources=None):
            s = StreamWriter(stream)
            xml = self.write_xml(resources)
            s.u32(len(xml))
            s.u32(self.unknown)
            stream.write(xml)

    def __init__(self, key=None, stream=None):
        self.version = self.VERSION.DEFAULT
        self.presets = []
        self.part_name = ''
        self.display_index = 0
        self.has_unique_texture_space = 0
        self.body_type = 0
        self.part_flags = 0
        self.age_gender_flags = 0
        self.clothing_category = 0
        self.naked_cas_part = None
        self.base_cas_part = None
        self.blend_fat = None
        self.blend_fit = None
        self.blend_thin = None
        self.blend_special = None
        self.draw_layer = 0
        self.sources = []
        self.lod_infos = []
        self.diffuse_refs = []
        self.specular_refs = []
        self.secondary_diffuse_refs = []
        self.secondary_specular_refs = []
        self.slot_poses = []
        self.shoe_material = ShoeMaterial.BARE
        PackedResource.__init__(self, key, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.version = s.u32()
        tgi = TGIList(order='igt', count_size=8, package=resource, use_length=False)
        tgi.begin_read(stream)
        self.presets = [self.CasPreset(stream, tgi) for i in range(s.u32())]
        self.part_name = s.s7(16, '>')
        self.display_index = s.f32()
        self.has_unique_texture_space = s.u8()
        self.body_type = s.u32()
        self.part_flags = s.u32()
        self.age_gender_flags = s.u32()
        self.clothing_category = s.u32()
        self.naked_cas_part = tgi.get_resource(s.i8())
        self.base_cas_part = tgi.get_resource(s.i8())
        self.blend_fat = tgi.get_resource(s.i8())
        self.blend_fit = tgi.get_resource(s.i8())
        self.blend_thin = tgi.get_resource(s.i8())
        self.blend_special = tgi.get_resource(s.i8())
        self.draw_layer = s.u32()
        self.sources = [tgi.get_resource(s.i8()) for i in range(s.u8())]
        self.lod_infos = [CASLodInfo(stream) for i in range(s.u8())]
        self.diffuse_refs = [tgi.get_resource(s.i8()) for i in range(s.u8())]
        self.specular_refs = [tgi.get_resource(s.i8()) for i in range(s.u8())]
        self.secondary_diffuse_refs = [tgi.get_resource(s.i8()) for i in range(s.u8())]
        self.secondary_specular_refs = [tgi.get_resource(s.i8()) for i in range(s.u8())]
        self.slot_poses = [tgi.get_resource(s.i8()) for i in range(s.u8())]
        self.shoe_material = s.s7(16, '>')
        tgi.end_read(stream)

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.version)
        tgi = TGIList('igt', 8, False)
        tgi.begin_write(stream)
        for preset in self.presets: preset.write(stream)
        s.s7(self.part_name, 16, '>')
        s.f32(self.display_index)
        s.u8(self.has_unique_texture_space)
        s.u32(self.body_type)
        s.u32(self.part_flags)
        s.u32(self.age_gender_flags)
        s.u32(self.clothing_category)
        s.i8(tgi.get_resource_index(self.naked_cas_part))
        s.i8(tgi.get_resource_index(self.base_cas_part))
        s.i8(tgi.get_resource_index(self.blend_fat))
        s.i8(tgi.get_resource_index(self.blend_fit))
        s.i8(tgi.get_resource_index(self.blend_thin))
        s.i8(tgi.get_resource_index(self.blend_special))
        s.u32(self.draw_layer)
        s.i8(len(self.sources))
        for source in self.sources: s.i8(tgi.get_resource_index(source))
        s.i8(len(self.lod_infos))
        for lod_info in self.lod_infos: lod_info.write(stream)
        s.i8(len(self.diffuse_refs))
        for diffuse_ref in self.diffuse_refs: s.i8(tgi.get_resource_index(diffuse_ref))
        s.i8(len(self.specular_refs))
        for specular_ref in self.specular_refs: s.i8(tgi.get_resource_index(specular_ref))
        s.u8(len(self.secondary_diffuse_refs))
        for block in self.secondary_diffuse_refs: s.u8(tgi.get_resource_index(block))
        s.u8(len(self.secondary_specular_refs))
        for block in self.secondary_specular_refs: s.u8(tgi.get_resource_index(block))
        s.u8(len(self.slot_poses))
        for block in self.slot_poses: s.u8(tgi.get_resource_index(block))
        s.s7(self.shoe_material, 16, '>')
        tgi.end_write(stream)

    def get_rig(self):
        age_char = ''
        if Flag.is_set(self.age_gender_flags, Age.ADULT): age_char = 'a'
        elif Flag.is_set(self.age_gender_flags, Age.BABY): age_char = 'b'
        elif Flag.is_set(self.age_gender_flags, Age.CHILD): age_char = 'c'
        elif Flag.is_set(self.age_gender_flags, Age.ELDER): age_char = 'e'
        elif Flag.is_set(self.age_gender_flags, Age.TEEN): age_char = 't'
        elif Flag.is_set(self.age_gender_flags, Age.TODDLER): age_char = 'p'
        elif Flag.is_set(self.age_gender_flags, Age.YOUNG_ADULT): age_char = 'a'
        else:
            raise Exception('Unable to determine age')
        species_id = (self.age_gender_flags & 0x00000F00)
        print(hex(species_id))
        species_char = 'u'
        if species_id == Species.HUMAN: species_char = 'u'
        elif species_id == Species.CAT: species_char = 'c'
        elif species_id == Species.DOG: species_char = 'd'
        elif species_id == Species.HORSE: species_char = 'h'
        elif species_id == Species.RACCOON: species_char = 'r'
        elif species_id == Species.DEER: species_char = 'h'
        elif species_id == Species.LITTLE_DOG: species_char = 'd'
        rig_name = "%s%sRig" % (age_char, species_char)
        return rig_name


class CASLodInfo(Serializable):
    class Asset(Serializable):
        def __init__(self, stream=None):
            self.sorting = 0
            self.specular_level = 0
            self.cast_shadow = 0
            Serializable.__init__(self, stream)

        def read(self, stream, resource=None):
            s = StreamReader(stream)
            self.sorting = s.u32()
            self.specular_level = s.u32()
            self.cast_shadow = s.u32()

        def write(self, stream, resource=None):
            s = StreamWriter(stream)
            s.u32(self.sorting)
            s.u32(self.specular_level)
            s.u32(self.cast_shadow)

    def __init__(self, stream=None):
        self.level = 0
        self.dest_texture = 0
        self.assets = []
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.level = s.u8()
        self.dest_texture = s.u32()
        self.assets = [self.Asset(stream) for i in range(s.i8())]

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u8(self.level)
        s.u32(self.dest_texture)
        s.u8(len(self.assets))
        for asset in self.assets: asset.write(stream)


class FacialRegionFlags:
    EYES = 0x0000001
    NOSE = 0x0000002
    MOUTH = 0x0000004
    TRANSLATE_MOUTH = 0x0000008
    EARS = 0x0000010
    TRANSLATE_EYES = 0x0000020
    FACE = 0x0000040
    HEAD = 0x0000080
    BROW = 0x0000100
    JAW = 0x0000200
    BODY = 0x0000400
    EYELASHES = 0x0000800


class BlendData(PackedResource):
    ID = 0x062C8204

    class VERSION:
        DEFAULT = 0x00000007
        EXTENDED = 0x00000008

    class BoneEntry:
        def __init__(self, stream=None, tgi=None):
            self.age_gender_flags = 0
            self.amount = 0.0
            self.bone = ExternalResource()
            if not stream == None:
                self.read(stream, tgi)

        def read(self, stream, tgi):
            s = StreamReader(stream)
            self.age_gender_flags = s.u32()
            self.amount = s.f32()
            self.blend = tgi.get_resource(s.u32())

        def write(self, stream, tgi):
            s = StreamWriter(stream)
            s.u32(self.age_gender_flags)
            s.f32(self.amount)
            s.u32(tgi.get_resource_index(self.blend))

    class GeomEntry:
        def __init__(self, stream=None, tgi=None):
            self.age_gender_flags = 0
            self.amount = 0.0
            self.geom = ExternalResource()
            if not stream == None:
                self.read(stream, tgi)

        def read(self, stream, tgi):
            s = StreamReader(stream)
            self.age_gender_flags = s.u32()
            self.amount = s.f32()
            self.geom = tgi.get_resource(s.u32())

        def write(self, stream, tgi):
            s = StreamWriter(stream)
            s.u32(self.age_gender_flags)
            s.f32(self.amount)
            s.u32(tgi.get_resource_index(self.geom))

    class RegionEntry:
        def __init__(self):
            self.region_flags = 0
            self.geom_entries = []
            self.bone_entries = []


    def __init__(self, key=None, stream=None):
        self.version = self.VERSION.EXTENDED
        self.part_name = ''
        self.blend_type = 0
        self.entries = []
        self.blend_geometry = ExternalResource(BlendGeometry.ID)
        PackedResource.__init__(self, key, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.version = s.u32()
        tgi = TGIList(use_length=self.version >= 7, add_eight=True)
        tgi.begin_read(stream)
        self.part_name = s.s7(16, '>')
        self.blend_type = s.u32()
        if self.version >= 8:
            key = s.tgi()
            bgeo = ExternalResource(key=key)
            self.blend_geometry = bgeo
        cEntries = s.i32()
        for i in range(cEntries):
            entry = self.RegionEntry()
            entry.region_flags = s.u32()
            entry.geom_entries = [self.GeomEntry(stream, tgi) for i in range(s.i32())]
            entry.bone_entries = [self.BoneEntry(stream, tgi) for i in range(s.i32())]
            self.entries.append(entry)
        tgi.end_read(stream)

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.version)
        tgi = TGIList(use_length=self.version >= 7, add_eight=True)
        tgi.begin_write(stream)
        s.s7(self.part_name, 16, '>')
        s.u32(self.blend_type)
        s.i32(len(self.entries))
        for entry in self.entries:
            s.u32(entry.region_flags)
            s.i32(len(entry.geom_entries))
            for geom_entry in entry.geom_entries: geom_entry.write_rcol(stream, tgi)
            s.i32(len(entry.bone_entries))
            for bone_entry in entry.bone_entries: bone_entry.write_rcol(stream, tgi)
        tgi.end_write(stream)


class BlendUnit(PackedResource):
    ID = 0xB52F5055

    def __init__(self, key=None, stream=None, resources=None, name=None):
        self.version = 0
        self.locale_key = 0
        self.indexers = []
        self.is_bi_directional = True
        self.cas_panel_group = 0
        self.sort_index = 0
        PackedResource.__init__(self, key, stream, resources, name)

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        self.version = s.u32()
        tgi = TGIList(package=resources)
        tgi.begin_read(stream)
        self.locale_key = s.u64()
        self.indexers = [tgi.get_resource(s.i32()) for i in range(s.i32())]
        self.is_bi_directional = bool(s.u32())
        self.cas_panel_group = s.u32()
        self.sort_index = s.i32()
        assert s.u32() == 0
        tgi.end_read(stream)

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        s.u32(self.version)
        tgi = TGIList(package=resources)
        tgi.begin_write(stream)
        s.u64(self.locale_key)
        for indexer in self.indexers:
            s.u32(tgi.get_resource_index(indexer))
        s.u32(0 if not self.is_bi_directional else 1)
        s.u32(self.cas_panel_group)
        s.i32(self.sort_index)
        s.u32(0)
        tgi.end_read(stream)


class BodyBlendData(BlendData):
    ID = 0x062C8204


class FacialBlendData(BlendData):
    ID = 0x0358B08A


class FurBlendData(BlendData):
    ID = 0x93D84841


class SkinTone(PackedResource):
    class ShaderKey(ChildElement, Serializable):
        def __init__(self, parent, stream=None, resources=None):
            self.age_gender = 0
            self.edge_color = 0
            self.specular_color = 0
            self.specular_power = 0.0
            self.is_genetic = True
            ChildElement.__init__(parent)
            Serializable.__init__(self, stream, resources)

        def read(self, stream, resources):
            s = StreamReader(stream)
            self.age_gender = s.u32()
            self.edge_color = s.u32()
            self.specular_color = s.u32()
            self.specular_power = s.f32()
            self.is_genetic = bool(s.u8())

        def write(self, stream, resources):
            s = StreamWriter(stream)
            s.u32(self.age_gender)
            s.u32(self.edge_color)
            s.u32(self.specular_color)
            s.u8(self.is_genetic)

    class TextureKey(ChildElement, Serializable):
        def __init__(self, parent, stream=None, resources=None):
            self.age_gender = 0
            self.type_flags = 0
            self.specular = None
            self.detail_dark = None
            self.detail_light = None
            self.normal = None
            self.overlay = None
            ChildElement.__init__(parent)
            Serializable.__init__(self, stream, resources)

        def read(self, stream, resources):
            s = StreamReader(stream)
            self.age_gender = s.u32()
            self.type_flags = s.u32()
            self.specular = resources.get_resource(s.u32())
            self.detail_dark = resources.get_resource(s.u32())
            self.detail_light = resources.get_resource(s.u32())
            self.normal = resources.get_resource(s.u32())
            self.overlay = resources.get_resource(s.u32())

        def write(self, stream, resources):
            s = StreamWriter(stream)
            s.u32(self.age_gender)
            s.u32(self.type_flags)
            s.u32(resources.get_resource_index(self.specular))
            s.u32(resources.get_resource_index(self.detail_dark))
            s.u32(resources.get_resource_index(self.detail_light))
            s.u32(resources.get_resource_index(self.normal))
            s.u32(resources.get_resource_index(self.overlay))


        pass

    def __init__(self, key=None, stream=None, resources=None, name=None):
        self.version = 0
        self.shader_keys = []
        self.sub_skin_ramp = ExternalResource()
        self.tone_ramp = ExternalResource()
        self.texture_keys = []
        self.is_dominant = True
        PackedResource.__init__(self, key, stream, resources, name)

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        self.version = s.u32()
        tgi = TGIList()
        tgi.begin_read(stream)
        self.shader_keys = [self.ShaderKey(self, stream, tgi) for i in range(s.i32())]
        self.sub_skin_ramp = tgi.get_resource(s.u32())
        self.tone_ramp = tgi.get_resource(s.u32())
        self.texture_keys = [self.TextureKey(self, stream, tgi) for i in range(s.i32())]
        self.dominant = bool(s.u8())

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        s.u32(self.version)
        tgi = TGIList()
        tgi.begin_write(stream)
        for shader_key in self.shader_keys: shader_key.write(stream, tgi)
        s.u32(tgi.get_resource_index(self.sub_skin_ramp))
        s.u32(tgi.get_resource_index(self.tone_ramp))
        for texture_key in self.texture_keys: texture_key.write(stream, tgi)
        s.u8(self.is_dominant)


