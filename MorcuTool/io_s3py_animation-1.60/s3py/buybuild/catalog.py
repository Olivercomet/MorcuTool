import io
from s3py.core import Serializable, ExternalResource, PackedResource
from s3py.io import StreamWrapper, StreamReader, TGIList
from s3py.material import Preset
class ComplateEncoder(object):
    complate_strings = (
        "",
        "filename",
        "X:",
        "-1",

        "assetRoot",
        "daeFileName",
        "daeFilePath",
        "Color",

        "ObjectRgbMask",
        "rgbmask",
        "specmap",
        "Background Image",

        "HSVShift Bg",
        "H Bg",
        "V Bg",
        "S Bg",


        "Base H Bg",
        "Base V Bg",
        "Base S Bg",
        "Mask",

        "Multiplier",
        "Dirt Layer",
        "1X Multiplier",
        "Specular",

        "Overlay",
        "Face",
        "partType",
        "gender",

        "bodyType",
        "age",
        "A",
        "M",


        "Stencil A",
        "Stencil B",
        "Stencil C",
        "Stencil D",

        "Stencil A Enabled",
        "Stencil B Enabled",
        "Stencil C Enabled",
        "Stencil D Enabled",

        "Stencil A Tiling",
        "Stencil B Tiling",
        "Stencil C Tiling",
        "Stencil D Tiling",

        "Stencil A Rotation",
        "Stencil B Rotation",
        "Stencil C Rotation",
        "Stencil D Rotation",


        "Pattern A",
        "Pattern B",
        "Pattern C",
        "Pattern A Enabled",

        "Pattern B Enabled",
        "Pattern C Enabled",
        "Pattern A Linked",
        "Pattern B Linked",

        "Pattern C Linked",
        "Pattern A Rotation",
        "Pattern B Rotation",
        "Pattern C Rotation",

        "Pattern A Tiling",
        "Pattern B Tiling",
        "Pattern C Tiling",
        "?0x3F",


        "",
        "MaskWidth",
        "MaskHeight",
        "ObjectRgbaMask",

        "RndColors",
        "Flat Color",
        "Alpha",
        "Color 0",

        "Color 1",
        "Color 2",
        "Color 3",
        "Color 4",

        "Channel 1",
        "Channel 2",
        "Channel 3",
        "Pattern D",


        "Pattern D Tiling",
        "Pattern D Enabled",
        "Pattern D Linked",
        "Pattern D Rotation",

        "HSVShift 1",
        "HSVShift 2",
        "HSVShift 3",
        "Channel 1 Enabled",

        "Channel 2 Enabled",
        "Channel 3 Enabled",
        "Base H 1",
        "Base V 1",

        "Base S 1",
        "Base H 2",
        "Base V 2",
        "Base S 2",


        "Base H 3",
        "Base V 3",
        "Base S 3",
        "H 1",

        "S 1",
        "V 1",
        "H 2",
        "S 2",

        "V 2",
        "H 3",
        "V 3",
        "S 3",

        "true",
        "1,0,0,0",
        "defaultFlatColor",
        "solidColor_1"

        )
    complate_string_lookup = {}

    for string_index,string in enumerate(complate_strings):
        complate_string_lookup[string_index] = string
        complate_string_lookup[string] = string_index



    def deserialize(self,stream,parent_tgi):

        def read_element(s,tgi_list):
            def read_complate_string(s):
                a = s.i8()
                if not a: return None
                if a & 0x80: return s.chars(s.i8() if a & 0x40 else a &0x3F)
                if a & 0x40: a = (a & 0x3F) + s.i8()
                return self.complate_string_lookup[a]
            def read_typecode(s,tgi_list):
                tc = s.u8()
                if   tc == 1: return read_complate_string(s)
                elif tc == 0x02: return [s.u8() for i in range(4)]
                elif tc == 0x03: return tgi_list.get_resource(s.i8())
                elif tc == 0x04: return s.f32()
                elif tc == 0x05: return [s.f32() for i in range(2)]
                elif tc == 0x06: return [s.f32() for i in range(3)]
                elif tc == 0x07: return bool(s.i8())
                else: raise Exception("Unknown typecode %02X"%tc)
            element = Preset.Element()
            element.resource = tgi_list.get_resource(s.u8())
            element.name = read_complate_string(s)
            element.variable = read_complate_string(s)

            for i in range(s.i32()):
                name = read_complate_string(s)
                value = read_typecode(s,tgi_list)
                element.values[name] =value
            element.patterns = [read_element(s,tgi_list) for i in range(s.i32())]
            return element
        s = StreamReader(stream)
        unk = s.i16()
        preset_tgi = TGIList(use_length=True)
        preset_tgi.begin_read(stream)
        element = read_element(s,preset_tgi)
        preset_tgi.end_read(stream)
        return element

    def serialize(self,complate,stream,resources):
        raise NotImplementedError()

class ProductInfo(Serializable):
    def __init__(self, stream=None, resources=None):
        self.version = 0x0000000C
        self.name_guid = 0
        self.desc_guid = 0
        self.name_key = ''
        self.desc_key = ''
        self.price = 0.0
        self.niceness_multiplier = 1.0
        self.crap_score = 0.0
        self.status_flags = 0
        self.icon = 0x0000000000000000
        self.environment_score = 0.0
        self.fire_type = 0
        self.is_stealable = False
        self.is_reposessable = False
        self.ui_sort_index = 0
        self.is_placeable_on_roof = False       #0x0000000D
        self.is_visible_in_worldbuilder = False #0x0000000E
        self.product_name = 0                   #0x0000000F
        Serializable.__init__(self,stream,None)
    def read(self, stream, resources=None):
        s = StreamReader(stream)
        self.version = s.u32()
        self.name_guid = s.u64()
        self.desc_guid = s.u64()
        self.name_key = s.s7()
        self.desc_key = s.s7()
        self.price = s.f32()
        self.niceness_multiplier = s.f32()
        self.crap_score = s.f32()
        self.status_flags = s.u8()
        self.icon = s.u64()
        self.environment_score = s.f32()
        self.fire_type = s.u32()
        self.is_stealable = bool(s.i8())
        self.is_reposessable = bool(s.i8())
        self.ui_sort_index = s.u32()
        if self.version >= 0x0000000D: self.is_placeable_on_roof = bool(s.u8())
        if self.version >= 0x0000000E: self.is_visible_in_worldbuilder = bool(s.u8())
        if self.version >= 0x0000000F: self.product_name = s.u32()
class ProductPreset(Preset):
    def __init__(self):
        self.type = 0
        self.unk1 = 0
        self.unk2 = 0

        Preset.__init__(self)


class ProductBase(PackedResource):
    def __init__(self, key=None, stream=None, resources=None, name=None):
        self.product_info = ProductInfo()
        PackedResource.__init__(self,key,stream,resources,name)

        pass
class BuildBuyProduct(ProductBase):
    ID = 0x319E4F1D

    class BuildBuyPreset(Preset):
        def __init__(self):
            self.id = 0
            Preset.__init__(self)

    def __init__(self, key=None, stream=None, resources=None, name=None):
        self.version = 0x00000001B
        self.presets = []

        ProductBase.__init__(self,key,stream,resources,name)
        pass
    def read(self, stream, resources=None):
        s = StreamReader(stream)
        s.seek(12,io.SEEK_SET)
        c = s.i32()
        ce = ComplateEncoder()
        for i in range(c):
            if not s.i8() == 1: s.i32()
            preset_len = s.u32()
            expected_end = s.tell() + preset_len
            preset = self.BuildBuyPreset()
            preset.complate = ce.deserialize(stream,resources)
            assert s.tell() == expected_end
            preset.id = s.u32()
            self.presets.append(preset)



