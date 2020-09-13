import s3py,bpy,bmesh
from s3py.animation.blender import load_rig
from s3py.animation.rig import SkeletonRig
from s3py.blender import  swizzle_uv, invalid_face, create_marker_node
from s3py.buybuild.catalog import BuildBuyProduct
from s3py.buybuild.geometry import Model, ModelLod, VertexFormat
from s3py.core import ResourceKey
from s3py.helpers import first, FNV32
from s3py.material import  PackedPreset, Preset
from s3py.material.blender import MaterialLoader
from s3py.model import VisualProxy
import s3py.material.blender
from mathutils import Vector
from s3py.model.material import MaterialDefinition, MaterialSet

def load_mesh(armature_rig,model_mesh,materials):
    assert isinstance(model_mesh,s3py.buybuild.geometry.ObjectMesh)




    bone_name_map = {}
    if armature_rig.type == 'ARMATURE':
        for bone_hash in armature_rig.data.bones: bone_name_map[FNV32.hash(bone_hash.name)] = bone_hash.name




    vertices = model_mesh.get_vertices()
    faces = model_mesh.get_triangles()
    mesh_name = '%08X' % int(model_mesh.name)
    print(mesh_name)


    print('Creating Mesh %s'%mesh_name)
    mesh = bpy.data.meshes.new(mesh_name)

    mesh_obj = bpy.data.objects.new(mesh_name, mesh)
    bpy.context.scene.objects.link(mesh_obj)
    bpy.context.scene.objects.active = mesh_obj
    mesh_obj.parent = armature_rig

    mesh_obj.show_transparent = True

    matd = model_mesh.material
    if isinstance(matd,MaterialSet):
        matd= matd.default_material

    if isinstance(matd,MaterialDefinition):
        mesh_material = materials.generate(mesh_name,matd.material_block)
        mesh_obj.data.materials.append(mesh_material)


    vertex_groups = []
    if model_mesh.skin_controller:
        print('Adding Vertex groups from skin controller')

        for bone_hash in model_mesh.bone_references:
            bone_name = '%0X8'%bone_hash
            if bone_hash in bone_name_map:
                bone_name = bone_name_map[bone_hash]
            vertex_groups.append(mesh_obj.vertex_groups.new(bone_name))



    print('Adding armature modifier and attach to rig')
    mesh_skin = mesh_obj.modifiers.new(type='ARMATURE', name="%s_skin" % mesh_name)
    mesh_skin.use_bone_envelopes = False
    mesh_skin.object = armature_rig



    bm = bmesh.new()
    bm.from_mesh(mesh)

    for vertex in vertices: bm.verts.new(vertex.position)
    for face_index,face in enumerate(faces):
        if invalid_face(face): print('[%s]Face[%04i] %s has duplicate points, skipped'%(mesh_name,face_index,face)); continue
        bm.faces.new([bm.verts[face_point] for face_point in face])


    for vertex_index,vertex in enumerate(vertices): bm.verts[vertex_index].normal = Vector(vertex.normal[:-1])

    bm.to_mesh(mesh)

    print('Adding weights')
    for vertex_index,vertex in enumerate(vertices):
        if vertex.blend_indices and vertex.blend_weights:
            for blend_index, blend_bone_index in enumerate(vertex.blend_indices):
                if blend_bone_index >= 0:
                    weight = vertex.blend_weights[blend_index]
                    if weight > 0.0:
                        blend_vertex_group = vertex_groups[int(blend_bone_index)]
                        blend_vertex_group.add((vertex_index,), weight, 'ADD')


    print('Adding UV Groups')
    for declaration in model_mesh.get_vertex_format().declarations:
        if declaration.usage == VertexFormat.USAGE.UV:
            mesh.uv_textures.new(name='uv_%i' %  declaration.usage_index)

    faces_skipped = 0
    for face_index, face in enumerate(faces):
        if invalid_face(face): faces_skipped+=1; continue
        for face_point_index, face_point_vertex_index in enumerate(face):
            vertex = vertices[face_point_vertex_index]
            if vertex.uv:
                for uv_channel_index, uv_coord in enumerate(vertex.uv):
                    mesh.uv_layers[uv_channel_index].data[ face_point_index + ((face_index-faces_skipped)* 3)].uv = swizzle_uv(uv_coord)

    mesh_obj.select = True
    bpy.ops.object.shade_smooth()
    mesh_obj.select = False
    return mesh_obj

def load_lod(armature_rig,lod,material):
    for model_mesh in lod.meshes:
        print(model_mesh)
        if model_mesh.is_dropshadow():
            continue
        mesh = load_mesh(armature_rig,model_mesh,material)
        mesh.parent = armature_rig

def load_model(package,modl,armature_rig,material):
    lod_entry = modl.lods[0]
    lod = lod_entry.model if isinstance(lod_entry.model,ModelLod) else  package.find_key(lod_entry.model.key).fetch(ModelLod)
    load_lod(armature_rig,lod,material)

def load_object(objd,package):
    print('Loading object!')
    assert isinstance(objd, BuildBuyProduct)
    vpxy = first(package.find_all_type(VisualProxy.ID)).fetch(VisualProxy)

    armature_rig = None
    rig = package.find_key(first(vpxy.entries, lambda e: isinstance(e,VisualProxy.MiscEntry) and e.resource.key.t == SkeletonRig.ID).resource.key)
    if rig:
        try:
            rig = rig.fetch(SkeletonRig)
            armature_rig = load_rig(rig)
        except:
            print('Unable to load rig, please patch your game...')
    if not armature_rig:
        print('No rig found')
        armature_rig = create_marker_node(objd.resource_name,True)


    print('Loading Model...')
    modl = package.find_key(first(vpxy.entries, lambda e: isinstance(e,VisualProxy.MiscEntry) and e.resource.key.t == Model.ID).resource.key).fetch(Model)

    if any(objd.presets):
        preset = objd.presets[0]
    else:
        preset = package.find_key(ResourceKey(t=PackedPreset.ID,g=1,i=modl.key.i))
        if preset:
            preset = preset.fetch(PackedPreset)
        else:
            preset = Preset()
    ml = MaterialLoader(package,preset)



    load_model(package,modl,armature_rig,ml)

    return armature_rig


