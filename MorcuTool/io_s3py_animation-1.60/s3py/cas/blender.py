import bpy, bmesh
from mathutils import Vector
from s3py.animation.blender import find_bone
from s3py.blender import  swizzle_uv, invalid_face, swizzle_v3, set_context
from s3py.cas.catalog import CASPart, BlendData
from s3py.cas.geometry import BlendGeometry, BodyGeometry
from s3py.core import ResourceKey
from s3py.data.package import Package
from s3py.helpers import FNV32, first
from s3py.material import PackedPreset, Preset
from s3py.material.blender import MaterialLoader
from s3py.model import VisualProxy


def load_geom(name, geom, blend_vertex_lod_map, armature_rig, material):
    bone_name_map = {}
    for bone in armature_rig.data.bones:
        bone_name_map[FNV32.hash(bone.name)] = bone.name

    mesh_name = name
    mesh = bpy.data.meshes.new(mesh_name)

    mesh_obj = bpy.data.objects.new(mesh_name, mesh)
    if material:
        mesh_obj.data.materials.append(material)

    bpy.context.scene.objects.link(mesh_obj)
    bpy.context.scene.objects.active = mesh_obj
    mesh_obj.parent = armature_rig

    print('processing mesh: %s' %mesh_name)
    shape_key_basis = mesh_obj.shape_key_add(from_mix=False, name='Basis')
    for blend_name in blend_vertex_lod_map:
        shape_key_morph = mesh_obj.shape_key_add(from_mix=False, name=blend_name)
        shape_key_morph.relative_key = shape_key_basis
        print('making driver %s for %s'%(blend_name,mesh_name))
        shape_key_fcurve = shape_key_morph.driver_add('value')
        shape_key_fcurve.driver.type = 'AVERAGE'
        shape_key_var = shape_key_fcurve.driver.variables.new()
        shape_key_var.type = 'SINGLE_PROP'
        shape_key_var.targets[0].id_type = 'OBJECT'
        shape_key_var.targets[0].id = armature_rig
        shape_key_var.targets[0].data_path = 'pose.bones["%s"].morph_value' % blend_name

    mesh_uvs = []
    mesh_skin = mesh_obj.modifiers.new(type='ARMATURE', name="%s_skin" % mesh_name)
    mesh_skin.use_bone_envelopes = False
    mesh_skin.object = armature_rig
    vertex_groups = []
    for geom_bone in geom.bones:
        if not geom_bone in bone_name_map:
            raise Exception('0x%08X not found' % geom_bone)
        vertex_groups.append(mesh_obj.vertex_groups.new(bone_name_map[geom_bone]))

    bm = bmesh.new()
    bm.from_mesh(mesh)

    for vertex in geom.vertices:
        bvert = bm.verts.new(vertex.position)
        bvert.normal = Vector(swizzle_v3(vertex.normal[:3]))
    faces_skipped = 0
    for face_index, face in enumerate(geom.indices):
        if invalid_face(face): print(
            '[%s]Face[%04i] %s has duplicate points, skipped' % (mesh_name, face_index, face)); continue
        try:
            bm.faces.new([bm.verts[face_point] for face_point in face])
        except ValueError as e:
            faces_skipped += 1
            print('Unable to load face %s'%face)
            print(e)
    bm.to_mesh(mesh)

    for vertex_index, vertex in enumerate(geom.vertices):
        # Positions
        blender_vertex = mesh.vertices[vertex_index]
        pos = blender_vertex.co.copy()
        shape_key_basis.data[vertex_index].co = pos
        # Add Shape Keys
        for blend_name in sorted(blend_vertex_lod_map):
            morph_blend_vertices = blend_vertex_lod_map[blend_name]
            morph_pos = pos.copy()
            if vertex.id in morph_blend_vertices:
                morph_blend_vertex = morph_blend_vertices[vertex.id]
                if morph_blend_vertex.position:
                    morph_pos += Vector(morph_blend_vertex.position)
            mesh.shape_keys.key_blocks[blend_name].data[vertex_index].co = morph_pos
            # Add Vertex Weights

        if vertex.blend_indices:
            for blend_index, blend_bone_index in enumerate(vertex.blend_indices):
                if blend_bone_index <= len(vertex_groups):
                    blend_vertex_group = vertex_groups[blend_bone_index]
                    weight = vertex.blend_weights[blend_index]
                    if weight > 0.000:
                        blend_vertex_group.add((vertex_index,), vertex.blend_weights[blend_index], 'REPLACE')

    for face_index, face in enumerate(geom.indices):
        if invalid_face(face): faces_skipped += 1; continue
        for face_point_index, face_point_vertex_index in enumerate(face):
            vertex = geom.vertices[face_point_vertex_index]
            if vertex.uv:
                for uv_channel_index, uv_coord in enumerate(vertex.uv):
                    if (uv_channel_index + 1) > len(mesh_uvs):
                        mesh_uvs.append(mesh.uv_textures.new(name='uv_%i' % uv_channel_index))
                    mesh.uv_layers[uv_channel_index].data[
                    face_point_index + ((face_index - faces_skipped) * 3)].uv = swizzle_uv(uv_coord)

    mesh_obj.select = True
    bpy.ops.object.shade_smooth()
    mesh_obj.select = False
    mesh_obj.active_shape_key_index = 0

    return mesh_obj

def load_slider(armature_rig):
    for mesh in filter(lambda x: x.type == 'MESH',armature_rig.children):
        pass
    pass

def load_cas(package, armature_rig, morphs=False, expressions=False):
    cas_parts = list(package.find_all_type(CASPart.ID))
    print(list(cas_parts))
    if len(cas_parts):
        print('found casparts')
        for caspart_index in cas_parts:
            caspart = caspart_index.fetch(CASPart)
            print('loading caspart %s'%caspart)
            load_caspart(caspart,package,armature_rig,morphs,expressions)
    else:
        print('no casparts')
        load_caspart(None,package,armature_rig,morphs,expressions)

    pass

def load_caspart(caspart, package, armature_rig, morphs=False, expressions=False):
    print('Loading CASPart %s...' %caspart.resource_name)
    bgeo = {}
    preset = None
    loaded_morphs = []
    # Loads a BlendGeometry and add it to the dictionary.  If a key was already loaded, skip it.  Either key or index must be specified.
    # If a name is provided, it will be used, otherwise it will default to the package name or the instance id
    def load_bgeo(key=None,name=None,index=None):
        if index:
            assert isinstance(index,Package.IndexEntry)
            key = index.key
        print('Loading %s %s' %(key,index))
        if not key.i :
            print('Skipping invalid BlendGeometry %s:  Instance must not be 0.'% key)
            return
        if key in loaded_morphs:
            print ('Skipping BlendGeometry %s: Already loaded.'%key)
            return
        try:
            if not index:
                if key.t == BlendData.ID:
                    blend_data = package.find_key(key).fetch(BlendData)
                    assert isinstance(blend_data,BlendData)
                    key = blend_data.blend_geometry.key
                index = package.find_key(key)
            if not index:
                print('Skipping BlendGeometry %s: Resource not found in package'%key)
                return

            assert isinstance(index,Package.IndexEntry)
            resource = index.fetch(BlendGeometry)
            assert isinstance(resource,BlendGeometry)
            if not name:
                name = resource.resource_name
            if not name:
                name= '%16X' %key.i
            bgeo[name] = resource
            loaded_morphs.append(key)

        except Exception as ex:
            print('Skipping BlendGeometry %s: Error loading'%key)
            print(ex)
            pass
        pass

    # Maps blend LOD vertex by it's id
    def map_blend_vertex(blend_lod):
        bvmap = {}
        for v in blend_lod.vertices:
            bvmap[v.id] = v
        return bvmap

    # Adds a bone to the skeleton
    def create_armature_bone(bone_name, parent_bone=None, min_bone=.001):
        set_context('EDIT', armature_rig)
        armature_bone = find_bone(armature_rig.data.edit_bones, bone_name)
        if not armature_bone:
            armature_bone = armature_rig.data.edit_bones.new(bone_name)
            armature_bone.use_connect = False
            armature_bone.tail = [0, min_bone, 0]
        if parent_bone:
            armature_bone.parent = armature_rig.data.edit_bones[parent_bone]
        set_context('POSE', armature_rig)
        return armature_bone

    if caspart:
        print('CASP found...')
        preset = package.find_key(ResourceKey(t=PackedPreset.ID, g=caspart.key.g, i=caspart.key.i))
        if preset:
            preset = preset.fetch(PackedPreset)
        elif any(caspart.presets):
            preset = caspart.presets[0]
        part_name = caspart.part_name
        assert isinstance(caspart, CASPart)
        vpxy = package.get_resource(key=caspart.sources[0].key, wrapper=VisualProxy)

        # Load standard morphs if specified with user friendly name
        if morphs:
            if caspart.blend_fat.key.i:
                load_bgeo(name='Fat',key=caspart.blend_fat.key)
            if caspart.blend_fit.key.i:
                load_bgeo(name='Fit',key=caspart.blend_fit.key)
            if caspart.blend_thin.key.i:
                load_bgeo(name='Thin',key=caspart.blend_thin.key)
            if caspart.blend_special.key.i:
                load_bgeo(name='Pregnant',key=caspart.blend_special.key)

    else:
        print('No CASP found, defaulting to first VPXY')
        vpxy =first(package.find_all_type(VisualProxy.ID))
        if vpxy:
            vpxy = vpxy.fetch(VisualProxy)
        assert isinstance(vpxy,VisualProxy)
        part_name=vpxy.resource_name

    print('Loading morphs...')
    for bgeo_index in package.find_all_type(BlendGeometry.ID):
        try:
            load_bgeo(index=bgeo_index)
        except Exception as ex:
            print("Unable to load morph %s"%bgeo_index)
            print(ex)
    if not preset:
        preset = Preset()
    ml = MaterialLoader(package, preset)
    lod_hi = first(vpxy.entries, lambda e: e.TYPE == VisualProxy.LodEntry.TYPE)
    assert isinstance(lod_hi, VisualProxy.LodEntry)

    # Arrange morph data for processing
    blend_vertex_lod_map = {}
    for blend_name in bgeo:
        cur_bgeo = bgeo[blend_name]
        for blend in cur_bgeo.blends:
            blend_vertex_lod_map[blend_name] = map_blend_vertex(blend.lods[lod_hi.index])

    # Load face morphs for animal meshes.  Loads any BodyGeometry matching the name 'Expression' as a morph
    if expressions:
        driver_root = 'b__DRIVERS__'
        create_armature_bone(driver_root)
        print('creating root: %s' % driver_root)
        for index in package.find_all_type(BodyGeometry.ID):
            assert isinstance(index, Package.IndexEntry)
            geom = index.fetch(BodyGeometry)
            assert isinstance(geom, BodyGeometry)
            if not 'Expressions' in geom.resource_name:
                continue
            blend_name = geom.resource_name[17:-2]
            create_armature_bone(blend_name, parent_bone=driver_root)
            blend_vertex_lod_map[blend_name] = map_blend_vertex(geom)
    meshes = []
    for lod_sub_index, geom in enumerate(package.find_key(item.key).fetch(BodyGeometry)for item in lod_hi.resources):
        material = ml.generate('%s_%i' % (part_name, lod_sub_index), geom.material)
        meshes.append(
            load_geom(part_name + '_' + str(lod_sub_index), geom, blend_vertex_lod_map, armature_rig, material))
    return meshes




