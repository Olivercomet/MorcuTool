import bpy
from io import BytesIO, SEEK_SET
import io,os,tempfile
from s3py.core import  DefaultResource, Resource, ResourceKey
from s3py.model.material import TextureCompositor,AnimatedTexture

def load_image(resource):
    unique_name= str(resource.key).replace(':','_')
    if resource.resource_name:
        unique_name = resource.resource_name + '_' + unique_name
    if unique_name in bpy.data.images:
        return bpy.data.images[unique_name]
    temp_name = '%s%s%s'% (tempfile.gettempdir(),os.sep,unique_name)
    with io.open(temp_name,'wb') as tmp:
        strm = BytesIO()
        resource.write(strm)
        strm.seek(0,SEEK_SET)
        tmp.write(strm.read())
    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.render.image_settings.color_mode = 'RGBA'
    bpy.ops.image.open(filepath=temp_name)
    img = bpy.data.images.new(unique_name,512,512)
    img.source = 'FILE'
    img.filepath = temp_name
    img.save_render(temp_name)
    img.filepath = temp_name
    return img



class MaterialLoader():
#    Complates = (
#    )
    Maps = (
        'NormalMap',
#        'Mask',
        'Multiplier',
        'DiffuseMap',
        'SpecularMap',
        'Specular',
        'Overlay'
#        'DiffuseMap',
#        'DetailMap',
#        'MultiplyMap',
#        'SelfIlluminationMap',
#        'AmbientOcclusionMap'
        )
    def __init__(self,package,preset):
        self.texture_index = 0
        self.package = package
        self.preset = preset
    def generate(self,material_name, material_block=None):
        self.texture_index = 0
        self.material = bpy.data.materials.new(material_name)
        self.material.specular_intensity = .08
        def create_texture(map_name,material_block = None):
            if map_name in self.preset.complate.values:
                tex_map_key =  self.preset.complate.values[map_name]
            elif material_block and map_name in material_block:
                tex_map_key = material_block[map_name]
            else:
                return
            if isinstance(tex_map_key,Resource):
                tex_map_key = tex_map_key.key
            if not isinstance(tex_map_key,ResourceKey):
                return
            if tex_map_key.t == TextureCompositor.ID or tex_map_key.t == AnimatedTexture.ID:
                return
            map_resource = self.package.find_key(tex_map_key)

            if not map_resource:
                return
            else:
                map_resource = map_resource.fetch()
            map_img = load_image(map_resource)
            self.material.texture_slots.create(self.texture_index)
            texture_slot = self.material.texture_slots[self.texture_index]
            self.texture_index+=1
            texture = bpy.data.textures.new(name=map_name+'_'+self.material.name,type='IMAGE')
            texture_slot.texture = texture
            texture_slot.texture_coords = 'UV'

            if map_name in ('DiffuseMap','Multiplier'):
                texture_slot.use_stencil = True
            elif map_name == 'Mask':
                texture_slot.use = False
                texture_slot.blend_type = 'OVERLAY'
                texture_slot.use_rgb_to_intensity = True
            elif map_name in ('Specular','SpecularMap'):
                texture_slot.use_map_specular = True
                texture_slot.use_map_color_spec = True
                texture_slot.use_map_hardness = True
                texture_slot.use_map_color_diffuse = False
            elif map_name == 'NormalMap':
                texture_slot.use_map_normal = True
                texture_slot.normal_factor = 0.01
                texture_slot.use_map_color_diffuse = False
            texture.type = 'IMAGE'
            texture.image = map_img


        for map_name in self.Maps:
            try:
                create_texture(map_name,material_block)
            except Exception as ex:
                print("Failed to load map %!"%map_name)
                print(ex)

        return self.material