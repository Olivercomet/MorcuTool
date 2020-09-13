import bpy,io,os
from bpy_extras.io_utils import ExportHelper
from genericpath import exists
from mathutils import Vector, Quaternion, Matrix
from shutil import copyfile
import s3py.animation
from s3py.animation.blender import *
from imp import reload
from s3py.blender import set_context
from s3py.buybuild.blender import load_object
from s3py.buybuild.catalog import BuildBuyProduct
from s3py.cas.blender import load_caspart
from s3py.cas.catalog import CASPart
from s3py.cas.geometry import BodyGeometry
from s3py.core import ResourceKey
from s3py.data.package import Package
from s3py.helpers import first
from s3py.animation.rig import  BoneDelta
reload(s3py.cas.geometry)
reload(s3py.animation.blender)
reload(s3py.buybuild.blender)
reload(s3py.cas.blender)
reload(s3py.material.blender)

bl_info = {
    'name': 'S3PY Animation Tools',
    'version': (1, 6,0),
    'blender': (2, 67, 0),
    'category': 'Import-Export',
    'location': 'File > Import/Export'
}

def find_armature(context,check_proxy=False):
    """
    Searches for an armature, starting with active object, its modifiers, or first one in the scene if nothing valid is selected
    """
    ao = context.active_object
    skel = None

    if not skel and ao:
        if ao.type == 'ARMATURE':
            skel = ao
        if ao.type == 'MESH':
            for mod in ao.modifiers:
                if mod.type == 'ARMATURE' and mod.object:
                    skel = mod.object

    if not skel:
        for o in context.scene.objects:
            if o.type== 'ARMATURE':
                skel = None
    if check_proxy:
        if skel and skel.proxy and skel.proxy.type == 'ARMATURE':
            skel = skel.proxy
    return skel
class LoadRigOp(bpy.types.Operator):
    bl_idname = 's3py.import_rig'
    bl_label = 'Import Sims 3 Rig'
    bl_options = {'REGISTER'}
    bl_description = 'Import Sims 3 Rig'
    filepath = bpy.props.StringProperty(name="File path", maxlen=4096, default="")
    filter_folder = bpy.props.BoolProperty(name="Filter folders", description="", default=True, options={'HIDDEN'})
    filter_glob = bpy.props.StringProperty(default="*.grannyrig", options={'HIDDEN'})

    def execute(self, context):
        rig = SkeletonRig()
        with io.open(self.filepath,'rb') as stream: rig.read(stream)
        load_rig(rig,.0015)
        return {'FINISHED'}
    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

class LoadCASOp(bpy.types.Operator):
    bl_idname = 's3py.import_cas'
    bl_label = 'Import Sims 3 Prop'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Import Sims 3 CAS part from a Package'
    filepath = bpy.props.StringProperty(name="File path", maxlen=4096, default="")
    filter_folder = bpy.props.BoolProperty(name="Filter folders", description="", default=True, options={'HIDDEN'})
    filter_glob = bpy.props.StringProperty(default="*.package", options={'HIDDEN'})

    def execute(self, context):
        package = Package(self.filepath)
        rig = find_armature(context)

        for caspart_index in package.find_all_type(CASPart.ID):
            caspart = caspart_index.fetch(CASPart)
            meshes = load_caspart(caspart,package,rig,True,True)

        return {'FINISHED'}
    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

class LoadPropOp(bpy.types.Operator):
    bl_idname = 's3py.import_prop'
    bl_label = 'Import Sims 3 Prop'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Import Sims 3 Prop from a Package'
    filepath = bpy.props.StringProperty(name="File path", maxlen=4096, default="")
    filter_folder = bpy.props.BoolProperty(name="Filter folders", description="", default=True, options={'HIDDEN'})
    filter_glob = bpy.props.StringProperty(default="*.package", options={'HIDDEN'})

    def execute(self, context):
        package = Package(self.filepath)
        for objd_index in package.find_all_type(BuildBuyProduct.ID):
            objd = objd_index.fetch(BuildBuyProduct)
            obj = load_object(objd,package)
        return {'FINISHED'}
    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}
class FilterBoneOp(bpy.types.Operator):
    bl_idname = 's3py.filter_bones'
    bl_label = 'Filter Bones (Sims 3)'
    bl_options = {'REGISTER', 'UNDO'}
    pet_bones = [0x2AF0A8AE,0x57884BB9,0x6FA96266,0xAFAC05CF,0x6FAF7238,0x6FB1B0A1,0x6FA06782,0xAFA30B6B,0x77F97B14,0x37FB557D,0x37F49902,0x77F73CEB,0x0F97B21B,0x9C4E5197,0xDC4C77AE,0xDC5333E9,0x9C509000,0xDC442973,0x3684EF66,0x768792CF,0x368AFF38,0x368D3DA1,0x76627909,0xEE7454F8,0xEE769361,0xEE6FD726,0x2E727A8F,0x34D7126B,0x58B9EB01,0x9BD5E550,0x9BB1DFAB,0x7E55A02E,0x1BEBC0AD,0x515F56D0,0xB282AC4C,0x36932030,0x1B459202,0xA303CE83,0x0C9E57D0,0x15AF037E,0x7CCD6D29,0x27BFBAD0,0x27C1F979,0xB1F747D3,0xF386366C,0x02908372,0xE25DA4A8,0x3BD15657,0x7EDE491B,0x1FAB5A56,0x3E3705BC,0x1C60CE02,0x90448227,0xF426F821,0xF55DFF6C,0xACD31E86,0xA75D2A97,0xB76F3C30,0x199C1122,0x68AEB8BD,0xB378FC90,0x2EE11E90,0xF3540AA8,0x2765E2AC,0x646EA315,0xA92B596E,0xF0143B40,0xCEB0355B,0xBD2CE17E,0xBD2F1FE7,0xEB22BFAD,0xA8B3A8AA,0xD04B46FC,0x2018F586,0x1E8EDCA5,0xEA51AA75,0xA691D790,0x2AFC0D02,0x6A5BBDF8,0xD0F50715,0x1BB37863,0x44A845BE,0x403317F4,0xFC4DB305,0xBFBCC3DA,0x2CE2E778,0x9BCB4D93,0x73456A1E,0x7242B766,0x96C59F6A,0x55213FCE,0x8DFBA064,0x556B181A,0x8F94DBCD,0x8F929CA4,0x4F8FF9FB,0x4F8DBB52,0x4F9F0471,0x8F9B9708,0x4F98F45F,0x4F96B536,0x30A7CEAC,0x82B6570D,0x8E75826E,0xA97C8377,0xE966FE20,0xCC993C11,0x28AE8B62,0x82BD203B,0xA5A146FA,0x406337AC,0x46818FF6,0x35A21598,0x34098492,0xCD971DB2,0xC6035E5E,0x85E195D0,0x23B79422,0x985BCF62,0x585DA94B,0x53758218,0xE1989FD9,0xC1C132E6,0x5B422292,0x952698E4,0x5A502A0A,0x080DE20B,0x8899D4D8,0x83B1355C,0x81B330DE,0x37BB051C,0x4A707954,0x0A73E5BD,0xCAECE55A,0xB7725F2B,0x72526690,0x0B40A974,0xAEEC56C6,0x17416D20,0xA8053FB1,0x405EC0BD,0xBF7D091C,0xDB898525,0x46879DCE,0xAFF979DB,0x282D0C45,0x2A72D486,0xBBEC6A45,0xC944B701,0x4AA91010,0xD6F37864,0xF96E5CED,0xA613836F,0x5BD83C6E,0x5A286D7A,0x7BAEE4EB,0x559F6DCD,0xAE28EB24,0x46034CBC,0xBF9966BD,0xC7AD1731,0xF2EE99A0,0xE7FD33E0,0x46722252,0x14B429B0,0x3D405C51,0xB4F6CE51,0x9E727D9A,0x10744D1C,0x5E58ECB5,0xB6609F20,0xB65E60B7,0x7A33531C,0xF4D46E82,0xF4DB2AFD,0xDDCF40A9,0x316E2F04,0x27DED11F,0x8884DF07,0x344AF043,0xDF1D341A,0x6509E081,0x5C0DF160,0xA924A104,0xD1CABD8F,0x0BD7CE1E,0xD0DAFCDB,0xCDC92586,0xD4685ECF,0xBFEE3853,0x438F1372,0x0410C881,0x37E413CE,0x8EC5268F,0x312E6F96,0x08E5E1CC,0xCE442223,0x32E75AD1,0x965DD13C,0xDF8E2A73,0x2EAAF0AD,0x8E35BD53,0x6EA499B2,0xCE38603C,0x6EA6D8DB,0xCE5D923A,0xD460062F,0x4ACD5424,0x34EB05C1,0xB7E6C645,0xD75244F5,0x9C42FE37,0x67BBA27E,0xF1F9EABC,0xF383F7C3,0xD3D3F5D9,0xF3E842B5,0xB622A80A,0x8B11C394,0x68116969,0x8DE694E4,0x1DCB011F,0x13C8B886,0xF4414C5B,0x47BD75CB,0xB2B5E0BA,0x755A88E1,0x3D3A788E,0x1F708664,0x121CBF01,0x30B2F032,0x316256A5,0x7D532228,0x37417626,0xA786B8F9,0x7D30F9D0,0x2B1C68B2,0xA8BA64E5,0xA18A197B,0x04E555DB,0x3796FC60,0x6A9C0196,0x12D7445F,0xE00D45CE,0x083917A5,0x08996364,0x8FE298B5,0x53996EAD,0xDF0A6BB0,0x750B1453,0x7F516F84,0x7D5EE66A,0x23490FDB,0xCC5AEC8C,0xB0316CB7,0xC1B1B8BE,0x19CDC510,0xCA6FC27C,0x9C344585,0x42C954A8,0xB88199D8,0x4FA7FED5,0x2A8FBA36,0xA149C9AD,0xB331345C,0xBE3B3F23,0x06E17332,0x2BB756F9,0x0E6E8308,0x545B502F,0x53659CE4,0x61FB92DE,0x4149968C,0x585FE7F4,0xD2B451B1,0xD045D92D,0x87BBFC91,0x3CDA4B0A,0xCDD6995A,0x513B90D7,0x749CB5E2,0x0A6B9742,0x46A2C45B,0x4512D0A7,0x313A98F3,0xC7434B6C,0x9A00AA78,0x9477B175,0x2BBACA21,0x94BA544C,0xBD7F1D47,0xAD6BBCBD,0x1B7E4A0A,0x33A14B27,0x821B5273,0x7144B88A,0x0737113E,0x31E29005,0x4A024BF2,0xA411711C,0x7899B037,0x7B59A932,0x7A8022E6,0x2CE4F9DB,0xD9F7958F,0x1B50E46B,0x4554BBCA,0xF7F2BC99,0x3410AA73,0xB9D63125,0x05EE826B,0xA59A520B,0x5B0FF02E,0x987BDE59,0xFBB3F393,0x99B5A4D7,0xE9880786,0x40953EF0,0x0055D98A,0xDF22D9C1,0x3FE7E5BC,0x46D8B68E,0xA8D228E4,0x55F7AF13,0x240DD4D5,0x567B2648,0xCB7F785D,0xDF1AA8BD,0x7A9532CC,0x2BB4EFB3,0xEE1B21F0,0x45D8DAA3,0x2A01DF92,0xCD68F001,0xA105A2D4,0x1978A98C,0x82A862AD,0xEBA1337D,0xA884CC22,0xDF76829B,0x5D2145D9,0x9D23E97D,0x01DCADCF,0x362070E9,0x5B0F1E2F,0x36F3D263,0x9D1B9A3A,0x9D1DD88E]
    sim_bones = [0x05AB4F06,0x07DDF1DE,0x08E5E1CC,0x0C9E57D0,0x0CD0D7C0,0x0EB4037B,0x0EBDA930,0x0EBDA935,0x0EBDA936,0x0EBDA937,0x0F26BE95,0x0F585BC9,0x0F97B21B,0x13A6779A,0x15AF037E,0x17411B46,0x17472B18,0x17496981,0x1CA23C66,0x1CA84C38,0x1E8EDCA5,0x2010CA87,0x23B79422,0x27BFBAD0,0x27C1F979,0x2AF0A8AE,0x2B0CFBFB,0x2B1C68B2,0x312E6F96,0x32E75AD1,0x37664E95,0x37BB051C,0x3BD15657,0x405EC0BD,0x4C4A702A,0x556B181A,0x57884BB9,0x58B9EB01,0x5BDC542C,0x5C0DF160,0x5CA4DFCF,0x646EA315,0x64D8444D,0x6509E081,0x67BBA27E,0x6804DEDB,0x6E7C259D,0x6FA96266,0x6FAF7238,0x750AF470,0x7CCD6D29,0x7D30F9D0,0x7F6C538B,0x812806D9,0x81B330DE,0x83B1355C,0x84794242,0x85E195D0,0x88B96968,0x88B96969,0x88B9696B,0x88B9696E,0x88B9696F,0x92A505CE,0x92ABC109,0x9BD5E550,0x9F266CA6,0xA0FCA895,0xA303CE83,0xA550CC9C,0xA6F3B078,0xA8B3A8AA,0xA8B5E7D3,0xA8BA64E5,0xA92B596E,0xA9F33BB2,0xAF9E9A53,0xAFAC05CF,0xAFF79517,0xAFF9D380,0xB1DE1AE5,0xB1F508AA,0xB1F747D3,0xB3881055,0xBA821CD6,0xBC81D5B8,0xBD2CE17E,0xBD2F1FE7,0xBDF5C2C1,0xBF7D091C,0xC183EF50,0xC2D0302A,0xC6035E5E,0xC619C029,0xC7AD1731,0xCE442223,0xCEB0355B,0xD0C4D27B,0xD2A9E720,0xD5E15C10,0xDC4C77AE,0xDC5333E9,0xE5331C30,0xE723D915,0xEB208104,0xEB22BFAD,0xEC2DDEBB,0xECA95D4C,0xEFF5BB2E,0xF0143B40,0xF1F9EABC,0xF2EE99A0,0xF383F7C3,0xF386366C,0xF6DFCA81,0xFBCF5C32]
    modes = [ ('SIM','SIM','SIM'), ('PET','PET','PET') ]
    mode = bpy.props.EnumProperty(items=modes,name='Mode',description='Bone filter mode')
    bl_options = {'REGISTER', 'UNDO'}
    def execute(self,context):
        rig = find_armature(context)
        bones = None
        if self.mode == 'PET':
            bones = self.pet_bones
        elif self.mode == 'SIM':
            bones = self.sim_bones
        else:
            print('Invalid mode')
            return {'CANCELLED'}
        bones_hidden = 0
        for bone in rig.data.bones:
            bone.hide = not FNV32.hash(bone.name) in bones
            if bone.hide:
                print('Hiding %s'%bone.name)
                bones_hidden += 1
        print('%i Bones hidden.'%bones_hidden)
        return {'FINISHED'}
    def invoke(self, context, event):
        wm = context.window_manager
        return wm.invoke_props_dialog(self)


class LoadClipOp(bpy.types.Operator):
    bl_idname = 's3py.import_clip'
    bl_label = 'Import Sims 3 Animation'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Import Sims 3 Animation Clip file'
    filepath = bpy.props.StringProperty(name="File path", description="Filepath used for importing the CLIP file",
        maxlen=4096, default="")
    filter_folder = bpy.props.BoolProperty(name="Filter folders", description="", default=True, options={'HIDDEN'})
    filter_glob = bpy.props.StringProperty(default="*.animation", options={'HIDDEN'})

    def execute(self, context):
        clip_resource = None
        with io.open(self.filepath, 'rb') as clip_stream:
            clip_resource = s3py.animation.ClipResource()
            clip_resource.read(clip_stream)
        skeleton = find_armature(context)

        if not skeleton or not skeleton.type == 'ARMATURE':
            self.report({'WARNING'}, "Please load a rig file first, thanks.")
            return{'CANCELLED'}
#        scale = 1.0 if clip_resource.ik_info or clip_resource.clip.name[0] != 't' else .9
#        print('Animation scale detected: %s'%scale)

        action = load_clip(clip_resource,skeleton,1.0)
        action.s3py.basis = self.filepath
        bpy.context.scene.frame_start = 0
        return {'FINISHED'}

    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

class UnloadClipOp(bpy.types.Operator):
    bl_idname='s3py.unload_clip'
    bl_label = 'Unload CLIP'
    bl_description = 'Remove animation data from an armature and reset to bind pose.'

    def execute(self,context):
        obj = find_armature(context)
        if obj.animation_data and obj.animation_data.action:
            obj.animation_data.action = None
        bpy.ops.pose.user_transforms_clear()
        for bone in obj.pose.bones: bone.morph_value = 0
        return {'FINISHED'}
class ExportClipOp(bpy.types.Operator):
    bl_idname = 's3py.export_clip'
    bl_label = 'Export Sims 3 Animation'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Export Sims 3 Animation file'

    name = bpy.props.StringProperty(name='Name')
    def execute(self, context):
        try:
            s3py.animation.ClipResource.create_key(self.name)
        except e as Exception:
            self.report({'ERROR_INVALID_INPUT'}, "Animation must have an name i.e. 'a_myAnimation'")
            return{'CANCELLED'}
        return bpy.ops.s3py.save_clip('INVOKE_DEFAULT',clip_name = self.name)

    def invoke(self, context, event):
        skeleton =  find_armature(context)
        if not skeleton or not skeleton.type == 'ARMATURE':
            self.report({'WARNING'}, "Please select an Armature with animation data to export, thanks.")
            return{'CANCELLED'}
        if not skeleton.animation_data:
            self.report({'WARNING'}, "Armature has no animation data to export.")
            return{'CANCELLED'}
        action =  skeleton.animation_data.action
        if not action:
            self.report({'WARNING'}, "Armature has no action selected.")
            return{'CANCELLED'}
        if not action.name == skeleton.name+'Action':
            self.name = action.name
        wm = context.window_manager
        return wm.invoke_props_dialog(self)

class SaveClipOp(bpy.types.Operator):
    bl_idname = 's3py.save_clip'
    bl_label = 'Save Sims 3 Animation'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Export Sims 3 Animation file'
    filepath = bpy.props.StringProperty(name="Filepath", description="Filepath to save the new animation to.",
        maxlen=4096, default="")
    filter_folder = bpy.props.BoolProperty(name="Filter folders", default=True, options={'HIDDEN'})
    filter_glob = bpy.props.StringProperty(default="*.animation", options={'HIDDEN'})
    filename=bpy.props.StringProperty(name="Filename")
    remove_ik = bpy.props.BoolProperty(default=True, name="Remove IK Targets?",
        description="Checking this option will remove any IK targets that the base clip may have had.  These are unsupported and leaving them there can have unexpected results.  If you are unsure, leave this checked.")

    skeleton = None
    clip_name = bpy.props.StringProperty(name="Clip Name")
    source_name = bpy.props.StringProperty(name="Source Name")
    actor_name = bpy.props.StringProperty(name="Actor Name")

    def execute(self, context):

        skeleton =  find_armature(context)
        action = skeleton.animation_data.action
        action.s3py.actor_name = self.actor_name
        action.s3py.source_name = self.source_name
        action.s3py.name = self.clip_name

        if not action.s3py.name:
            self.report({'ERROR_INVALID_INPUT'}, "Animation must have an name i.e. 'a_myAnimation'")
            return{'CANCELLED'}


        if not action.s3py.actor_name:
            self.report({'ERROR_INVALID_INPUT'}, "Animation must have an actor name i.e. 'x'")
            return{'CANCELLED'}

        if not action.s3py.source_name:
            self.report({'ERROR_INVALID_INPUT'}, "Animation must have a source name i.e. 'myAnimation.blend'")
            return{'CANCELLED'}

        # if saving over an existing, make a backup
        if exists(self.filepath):
            ext = os.path.splitext(self.filepath)[1]
            i = 1
            backup =  str(self.filepath).replace(ext,'_backup_%i_'%i +ext)
            while exists(backup):
                i+=1
                backup =  str(self.filepath).replace(ext,'_backup_%i_'%i +ext)
            copyfile(self.filepath,backup)


        clip_resource = s3py.animation.ClipResource()
        # if not starting from scratch, find original clip to preserve events
        if exists(action.s3py.basis):
            with io.open(action.s3py.basis, 'rb') as clip_stream:
                clip_resource.read(clip_stream)
            clip_resource.clip.frame_duration = 1.0/30.0
        else:
            print('No basis clip found, creating new one from scratch.')

        # Remove IK chains unless user specifies to keep them
        if self.remove_ik and clip_resource.ik_info:
            clip_resource.ik_info.chains = []
        #rig_scale = 0.9 if self.clip_name[0] == 't' else 1.0
        #print('Animation scale detected: %s'%rig_scale)
        # Write animation data
        save_clip(clip_resource,skeleton,1.0)
        with io.open(self.filepath, 'wb') as output_stream:
            clip_resource.write(output_stream)
        return {'FINISHED'}

    def invoke(self, context, event):

        skeleton =  find_armature(context)

        if not skeleton or not skeleton.type == 'ARMATURE':
            self.report({'WARNING'}, "Please select an Armature with animation data to export, thanks.")
            return{'CANCELLED'}
        if not skeleton.animation_data or not skeleton.animation_data.action:

            self.report({'WARNING'}, "Armature has no animation data to export.")
            return{'CANCELLED'}
        action = skeleton.animation_data.action

        if self.clip_name:
            action.name = self.clip_name
            action.s3py.source_name = self.clip_name +'.blend'
            try:
                clip_key = s3py.animation.ClipResource.create_key(self.clip_name)
                assert isinstance(clip_key,ResourceKey)
                self.filename = clip_key.s3pi_name('CLIP','animation',self.clip_name)
            except Exception as ex:
                self.report({'WARNING'}, "Invalid animation name.  Name must be in this format 'a_myanimation'")
                return{'CANCELLED'}


        self.actor_name = action.s3py.actor_name
        self.source_name = action.s3py.source_name
        self.clip_name = action.name


        if not self.actor_name:
            self.actor_name = 'x'
        if not self.clip_name:
            if len(action.name) < 3 or action.name[1] != '_':
                action.name = skeleton.name[0]+'_'+action.name
            self.clip_name = action.name
        if not self.source_name:
            self.source_name = self.clip_name+'.blend'
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

class ResetRigOp(bpy.types.Operator):
    bl_idname='s3py.unload_clip'
    bl_label = 'Reset Rig'
    bl_description = 'Remove animation data from an armature and reset to bind pose.'

    def execute(self,context):
        obj = find_armature(context)
        if not obj:
            return
        if obj.animation_data and obj.animation_data.action:
            obj.animation_data.action = None
        set_context('POSE',obj)
        bpy.ops.pose.user_transforms_clear()
        for bone in obj.pose.bones: bone.morph_value = 0
        bpy.context.scene.frame_set(0)
        return {'FINISHED'}


class DeleteClipOp(bpy.types.Operator):
    bl_idname='s3py.delete_clip'
    bl_label = 'Delete CLIP'
    bl_description = 'Delete an animation from the scene'

    def execute(self,context):
        obj = find_armature(context)
        if not obj:
            return

        if obj.animation_data and obj.animation_data.action:
            action =obj.animation_data.action
            obj.animation_data.action = None
            action.user_clear()
            bpy.data.actions.remove(action)

        set_context('POSE',obj)
        bpy.ops.pose.user_transforms_clear()
        bpy.context.scene.frame_set(0)
        return {'FINISHED'}

class RigPanel(bpy.types.Panel):
    bl_label = 'S3PY Rig Tools'
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = 'scene'


    def draw(self,context):
        l = self.layout
        row = l.row()
        row.operator('s3py.import_prop', text='Load Prop',icon='OBJECT_DATA')
        row = l.row()
        row.operator('s3py.import_rig', text='Load Skeleton',icon='BONE_DATA')
        obj = find_armature(context)
        if obj:
            armature = obj.data
            row = l.row()
            row.operator('s3py.import_cas', text='Load CAS',icon='POSE_HLT')
            row = l.row()
            row.operator('s3py.set_morphs',text='Set Morphs', icon = 'POSE_HLT')
            if DEBUG:
                row = l.row()
                row.operator('s3py.set_bone_scale', text='Set Bone Scale',icon='BONE_DATA')
                row = l.row()
                row.operator('s3py.filter_bones', text='Filter Bones',icon='BONE_DATA')

class PetsFacePanel(bpy.types.Panel):
    bl_label = 'S3PY Pet Face Sliders'
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_context = 'scene'

    @classmethod
    def poll(cls,context):
        obj = find_armature(context)
        return obj != None and ROOT_MORPH in obj.data.bones
    def draw_header(self,context):
        l = self.layout
        r =l.row()
        r.label('',icon='MONKEY')

    def draw(self,context):
        l = self.layout
        a = find_armature(context)
        p = a.pose.bones

        row = l.row()
        row.label('Eye')

        if 'RightEyeUpperLidOpen' in p:
            row = l.row()
            row.label('Open')
            row = l.row()
            row.prop(p["RightEyeUpperLidOpen"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftEyeUpperLidOpen"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.prop(p["RightEyeLowerLidOpen"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftEyeLowerLidOpen"], 'morph_value', slider=True,text='L')

        if 'RightEyeUpperLidClosed' in p:
            row = l.row()
            row.label('Closed')
            row = l.row()
            row.prop(p["RightEyeUpperLidClosed"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftEyeUpperLidClosed"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.prop(p["RightEyeLowerLidClosed"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftEyeLowerLidClosed"], 'morph_value', slider=True,text='L')
            l.separator()


        if 'RightEyeUnderLidSquint' in p:
            row = l.row()
            row.label('Squint')
            row = l.row()
            row.prop(p["RightEyeUnderLidSquint"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftEyeUnderLidSquint"], 'morph_value', slider=True,text='L')
        l.separator()

        if 'RightBrowOuterUp' in p:
            row = l.row()
            row.label('Brow')
            row = l.row()
            row.label('Up')
            row = l.row()
            row.prop(p["RightBrowOuterUp"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftBrowOuterUp"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.prop(p["RightBrowInnerUp"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftBrowInnerUp"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.label('Down')
            row = l.row()
            row.prop(p["RightBrowOuterDown"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftBrowOuterDown"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.prop(p["RightBrowInnerDown"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftBrowInnerDown"], 'morph_value', slider=True,text='L')
            l.separator()



        if 'RightMouthUpperLipUp' in p:
            row = l.row()
            row.label('Mouth')
            row = l.row()
            row.label('UpperLip')
            row = l.row()
            row.prop(p["RightMouthUpperLipUp"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftMouthUpperLipUp"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.label('Corner')
            row = l.row()
            row.prop(p["RightMouthCornerUp"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftMouthCornerUp"], 'morph_value', slider=True,text='L')
            row = l.row()
            row.prop(p["RightMouthCornerBack"], 'morph_value', slider=True,text='R')
            row.prop(p["LeftMouthCornerBack"], 'morph_value', slider=True,text='L')
            l.separator()

        if 'NoseCrinkleBack' in p:
            row = l.row()
            row.label('Nose')
            if 'NoseCrinkleBack' in p:
                row = l.row()
                row.prop(p["NoseCrinkleBack"], 'morph_value', slider=True,text='Crinkle')
            if 'NoseTwitchOut' in p:
                row = l.row()
                row.prop(p["NoseTwitchOut"], 'morph_value', slider=True,text='Twitch Out')
            if 'RightNoseSide' in p:
                row = l.row()
                row.label('Side')
                row = l.row()
                row.prop(p["RightNoseSide"], 'morph_value', slider=True,text='R')
                row.prop(p["LeftNoseSide"], 'morph_value', slider=True,text='L')


class AnimationPanel(bpy.types.Panel):
    bl_label = 'S3PY Animation Tools'
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = 'scene'

    @classmethod
    def poll(cls,context):
        obj = find_armature(context)
        return obj != None

    def draw(self, context):
        l = self.layout
        scene = context.scene
        obj = find_armature(context)
        if obj:
            anim = obj.animation_data
            action = None
            if anim:
                row = l.row()
                row.prop_search(anim, 'action', bpy.data, 'actions', text="Active Animation", icon='NONE')

                action = anim.action
                if action:
                    row = l.row()
                    row.prop(action, 'name',text="Animation Name")
                    row = l.row()
                    row.prop(action.s3py, 'basis',text="Base Clip")

            row = l.row()
            row.operator('s3py.import_clip', text='Load CLIP',icon='POSE_HLT')
            row = l.row()
            row.operator('s3py.unload_clip', text='Reset Rig',icon='FILE_REFRESH')
            if action:
                row = l.row()
                row.operator('s3py.export_clip', text='Save CLIP',icon='FILE_FOLDER')
                row = l.row()
                row.operator('s3py.delete_clip', text='Delete CLIP',icon='CANCEL')


            row = l.row()
            row.prop(obj,'hide',text='Hide Bones')

class ClipProps(bpy.types.PropertyGroup):
    actor_name = bpy.props.StringProperty(name="Actor Name",default='x')
    name = bpy.props.StringProperty(name="Clip Name",default='')
    source_name = bpy.props.StringProperty(name="Source Name",default='')
    basis = bpy.props.StringProperty(name='Basis',description='Animation to base this clip upon.  Events and optionally IK chains will be preserved.')




class SetMorphsOp(bpy.types.Operator):
    bl_idname = 's3py.set_morphs'
    bl_label = 'Set Sims 3 Morphs'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Set Sims 3 Morphs'

    thin = bpy.props.FloatProperty(name="Thin",default=0.0,min=-2.0,max=2.0,step=3, precision=2)
    fit = bpy.props.FloatProperty(name="Fit",default=0.0,min=-2.0,max=2.0,step=3, precision=2)
    fat = bpy.props.FloatProperty(name="Fat",default=0.0,min=-2.0,max=2.0,step=3, precision=2)
    pregnant = bpy.props.FloatProperty(name="Pregnant",default=-2.0,min=0.0,max=2.0,step=3, precision=2)

    def execute(self, context):
        obj = find_armature(context,True)
        def update(slider_name,value):
            for mesh in filter(lambda m:m.data.shape_keys and slider_name in m.data.shape_keys.key_blocks ,obj.children):
                mesh.data.shape_keys.key_blocks[slider_name].value = value
        update('Fat',self.fat)
        update('Fit',self.fit)
        update('Thin',self.thin)
        update('Pregnant',self.pregnant)
        return {'FINISHED'}

    def invoke(self, context, event):
        skeleton =  find_armature(context,True)
        if not skeleton or not skeleton.type == 'ARMATURE':
            self.report({'WARNING'}, "Please select an Armature.")
            return{'CANCELLED'}
        wm = context.window_manager
        return wm.invoke_props_dialog(self)




class SetBoneScale(bpy.types.Operator):
    bl_idname = 's3py.set_bone_scale'
    bl_label = 'Set Sims 3 Bone Scale'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Set Sims 3 Bone Scale'

    scale = bpy.props.FloatProperty(name="Scale",default=1.0,min=0.0,max=10)

    def execute(self, context):
        obj = find_armature(context)
        set_context('POSE',obj)
        scaled = []
        for pose_bone in obj.pose.bones:
            if pose_bone.custom_shape and not pose_bone.custom_shape in scaled:
                pose_bone.custom_shape.empty_draw_size *= self.scale
                scaled.append(pose_bone.custom_shape)
        return {'FINISHED'}

    def invoke(self, context, event):
        skeleton =  find_armature(context)
        if not skeleton or not skeleton.type == 'ARMATURE':
            self.report({'WARNING'}, "Please select an Armature.")
            return{'CANCELLED'}
        wm = context.window_manager
        return wm.invoke_props_dialog(self)

def menu_func_import(self, context):
    self.layout.operator(LoadClipOp.bl_idname, text="Sims 3 Animation (.animation)")

def menu_func_export(self, context):
    self.layout.operator(ExportClipOp.bl_idname, text="Sims 3 Animation (.animation)")

DEBUG = True
def register():

    bpy.utils.register_class(ClipProps)
    bpy.utils.register_class(SetMorphsOp)
    bpy.utils.register_class(SetBoneScale)
    bpy.types.Action.s3py = bpy.props.PointerProperty(type=ClipProps)
    bpy.types.PoseBone.morph_value = bpy.props.FloatProperty(name="Morph",default=0.0,min=0.0,max=1.0,step=3, precision=2, options={'ANIMATABLE'})

    bpy.utils.register_class(ResetRigOp)
    bpy.utils.register_class(DeleteClipOp)
    bpy.utils.register_class(AnimationPanel)
    bpy.utils.register_class(LoadClipOp)
    bpy.utils.register_class(SaveClipOp)
    bpy.utils.register_class(ExportClipOp)

    bpy.types.INFO_MT_file_import.append(menu_func_import)
    bpy.types.INFO_MT_file_export.append(menu_func_export)


    bpy.utils.register_class(LoadPropOp)
    bpy.utils.register_class(LoadCASOp)
    bpy.utils.register_class(LoadRigOp)

    bpy.utils.register_class(RigPanel)
    bpy.utils.register_class(PetsFacePanel)

    if DEBUG:
        bpy.utils.register_class(FilterBoneOp)

def unregister():
    bpy.utils.unregister_class(ClipProps)
    bpy.utils.unregister_class(SetMorphsOp)
    bpy.utils.unregister_class(SetBoneScale)
    del bpy.types.Action.s3py
    del bpy.types.PoseBone.morph_value


    bpy.utils.unregister_class(ResetRigOp)
    bpy.utils.unregister_class(DeleteClipOp)
    bpy.utils.unregister_class(AnimationPanel)
    bpy.utils.unregister_class(LoadClipOp)
    bpy.utils.unregister_class(SaveClipOp)
    bpy.utils.unregister_class(ExportClipOp)

    bpy.types.INFO_MT_file_import.remove(menu_func_import)
    bpy.types.INFO_MT_file_export.remove(menu_func_export)

    bpy.utils.unregister_class(LoadPropOp)
    bpy.utils.unregister_class(LoadCASOp)
    bpy.utils.unregister_class(LoadRigOp)

    bpy.utils.unregister_class(RigPanel)
    bpy.utils.unregister_class(PetsFacePanel)

    if DEBUG:
        bpy.utils.unregister_class(FilterBoneOp)

if __name__ == "__main__":
    register()