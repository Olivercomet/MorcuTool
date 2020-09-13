import bpy, time
from s3py.animation import Track, Frame, Curve
from s3py.animation.rig import BoneDelta
from s3py.blender import quat_wxyz, quat_xyzw, set_context, swizzle_v3
from s3py.helpers import FNV32, FNV64
from mathutils import Vector, Matrix, Quaternion

ROOT_BONE = 'b__ROOT__'
ROOT_BIND = 'b__ROOT_bind__'
ROOT_MORPH = 'b__DRIVERS__'

def load_clip(clip_resource, skeleton, scale=1.0):
    print('Loading clip...')

    #clear existing animations
    skeleton.animation_data_clear()

    clip = clip_resource.clip
    skeleton.hide = False
    bpy.context.scene.objects.active = skeleton
    skeleton.animation_data_clear()
    skeleton.animation_data_create()
    action = skeleton.animation_data.action = bpy.data.actions.new(clip.name)

    #set custom props
    action.s3py.actor_name = clip_resource.actor_name
    action.s3py.name = clip.name
    action.s3py.source_name = clip.source_file_name

    bone_transforms = {}
    pose_bone_map = {}
    for pose_bone in skeleton.pose.bones:
        hash = FNV32.hash(pose_bone.name)
        pose_bone_map[hash] = pose_bone
        bone_transforms[hash] = pose_bone.bone.matrix_local
    bpy.ops.object.mode_set(mode='POSE')

    #reset to bind pose
    bpy.ops.pose.user_transforms_clear()

    frame_map = {}
    for track in clip.tracks:
        if not track.track_key in frame_map:
            frame_map[track.track_key] = {}
        if track.orientations:
            for orientation_key in track.orientations.frames:
                if not orientation_key.frame_index in frame_map[track.track_key].keys():
                    frame_map[track.track_key][orientation_key.frame_index] = {
                        'orientation': None,
                        'position': None,
                        'morph': None
                    }
                frame_map[track.track_key][orientation_key.frame_index]['orientation'] = Quaternion(
                    quat_wxyz(orientation_key.data))
        if track.positions:
            for position_key in track.positions.frames:
                if not position_key.frame_index in frame_map[track.track_key].keys():
                    frame_map[track.track_key][position_key.frame_index] = {
                        'orientation': None,
                        'position': None,
                        'morph': None
                    }
                position = Vector(position_key.data)
#                if track.track_key == FNV32.hash(ROOT_BIND):
#                    position[1] /= scale
                frame_map[track.track_key][position_key.frame_index]['position'] = position

        if track.morphs:
            for morph_key in track.morphs.frames:
                if not morph_key.frame_index in frame_map[track.track_key].keys():
                    frame_map[track.track_key][morph_key.frame_index] = {
                        'orientation': None,
                        'position': None,
                        'morph': None
                    }
                frame_map[track.track_key][morph_key.frame_index]['morph'] = morph_key.data

    def animate_driver_bones(driver_root):
        for shape_key in driver_root.children:
            track_key = FNV64.hash(shape_key.name) & 0xFFFFFFFF
            print('shape_key %s' % shape_key)
            print('track_key: %08X' % track_key)
            if track_key in frame_map:
                group = action.groups.new(name=shape_key.name)
                data_path = 'pose.bones["%s"].morph_value' % shape_key.name
                fcurve = action.fcurves.new(data_path=data_path, index=0)
                fcurve.group = group
                frame_indices = sorted(frame_map[track_key].keys())

                for frame_index in frame_indices:
                    print('[%i]: %s' % ( frame_index, frame_map[track_key][frame_index]['morph'][0]))
                    val = frame_map[track_key][frame_index]['morph'][0]
                    fcurve.keyframe_points.add(1)
                    fcurve.keyframe_points[-1].co = [frame_index, val]

    def animate_bone(root_bone):
        track_key = FNV32.hash(root_bone.name)
        if track_key in frame_map:
            group = action.groups.new(name=root_bone.name)
            data_path = 'pose.bones["%s"].' % root_bone.name
            location_path = '%slocation' % data_path
            rotation_path = '%srotation_quaternion' % data_path
            fcurves_translate = []
            fcurves_rotate = []
            for i in range(3):
                fcurve = action.fcurves.new(data_path=location_path, index=i)
                fcurve.group = group
                fcurves_translate.append(fcurve)
            for i in range(4):
                fcurve = action.fcurves.new(data_path=rotation_path, index=i)
                fcurve.group = group
                fcurves_rotate.append(fcurve)
            frame_indices = sorted(frame_map[track_key].keys())
            current_matrix = root_bone.bone.matrix_local
            rotation_matrix = current_matrix.to_3x3().to_4x4()
            translation_matrix = Matrix.Translation(current_matrix.to_translation())
            for frame_index in frame_indices:
                frame_data = frame_map[track_key][frame_index]
                if frame_data['orientation']:
                    rotation_matrix = (
                        Quaternion() if not root_bone.parent else frame_data['orientation']).to_matrix().to_4x4()
                if frame_data['position']:
                    translation_matrix = Matrix.Translation(frame_data['position'])
                transform_matrix = translation_matrix * rotation_matrix
                if root_bone.parent:
                    transform_matrix = root_bone.parent.matrix * transform_matrix
                root_bone.matrix = transform_matrix
                if frame_data['position']:
                    for i in range(3):
                        fcurves_translate[i].keyframe_points.add(1)
                        fcurves_translate[i].keyframe_points[-1].co = [frame_index, root_bone.location[i]]
                if frame_data['orientation']:
                    for i in range(4):
                        fcurves_rotate[i].keyframe_points.add(1)
                        fcurves_rotate[i].keyframe_points[-1].co = [frame_index, root_bone.rotation_quaternion[i]]
            root_bone.matrix = root_bone.bone.matrix_local
        for child in root_bone.children:
            animate_bone(child)


    animate_bone(skeleton.pose.bones[ROOT_BONE])
    if ROOT_MORPH in skeleton.pose.bones:
        animate_driver_bones(skeleton.pose.bones[ROOT_MORPH])

    bpy.context.scene.frame_set(0)
    print('Done.')
    return action


def save_clip(clip_resource, skeleton, scale=1.0):
    print(skeleton.name, skeleton.type)
    clip = clip_resource.clip
    clip.tracks = []
    action = skeleton.animation_data.action
    clip_resource.actor_name = action.s3py.actor_name
    clip.name = action.s3py.name
    clip.source_file_name = action.s3py.source_name
    clip.max_frame_count = int(action.frame_range[1])

    start_time = time.clock()
    print('Saving CLIP...')
    track_map = {}
    used_bones = []
    for fcurve in action.fcurves:
        s = str(fcurve.data_path).split('.')
        if s[0] != 'pose' or s[1][:5] != 'bones':
            continue
        cname = s[1][7:-2]
        if cname == ROOT_MORPH:
            continue
        if cname in skeleton.pose.bones:
            pose_bone = skeleton.pose.bones[cname]
            track_key = FNV32.hash(cname)
            clip_track_key = FNV64.hash(
                cname) & 0xFFFFFFFF if pose_bone.parent and pose_bone.parent.name == ROOT_MORPH else  track_key
            if not track_key in track_map:
                track = Track(clip_track_key)
                used_bones.append(pose_bone)
                clip.tracks.append(track)
                track_map[track_key] = track

    print(list(ub.name for ub in used_bones))
    print('%i Frames found in %s' % (int(action.frame_range[1]), action.name))

    def write_frame(current_value, track, frame_index):
        write = False
        if not any(track.frames):
            write = True
        else:
            last_value = track.frames[-1].data
            for i in range(len(current_value)):
                difference = math.fabs(current_value[i] - last_value[i])
                if difference > 0.0001:
                    write = True
        if write:
            f = Frame()
            f.frame_index = frame_index
            f.data = current_value
            track.frames.append(f)

    set_context('POSE', skeleton)

    for frame_index in range(int(action.frame_range[0]), int(action.frame_range[1])):
        bpy.context.scene.frame_set(frame_index)
        for pose_bone in used_bones:
            track_key = FNV32.hash(pose_bone.name)
            if not track_key in track_map:
                continue
            track = track_map[track_key]
            if pose_bone.parent and pose_bone.parent.name == ROOT_MORPH:
                if not track.morphs:
                    track.morphs = Curve.create_morph()
                cur_morph = [pose_bone.morph_value]
                write_frame(cur_morph, track.morphs, frame_index)
                continue

            matrix_parent = Matrix() if not pose_bone.parent else pose_bone.parent.matrix
            matrix_delta = matrix_parent.inverted() * pose_bone.matrix

            if not track.orientations:
                track.orientations = Curve.create_orientation()
            rotation = quat_xyzw(matrix_delta.to_quaternion())
            write_frame(rotation, track.orientations, frame_index)

            if not track.positions:
                track.positions = Curve.create_position()
            translation = matrix_delta.to_translation()
#            if pose_bone.name == ROOT_BIND:
#                translation[1] *= scale
            write_frame(translation, track.positions, frame_index)

    print('Finished in %.4f sec.' % (time.clock() - start_time))

import math
import bpy
from s3py.animation.rig import SkeletonRig, Bone
from s3py.blender import quat_wxyz
from mathutils import Quaternion, Matrix, Vector

def find_bone(iterable, real_name):
    for bone in iterable:
        if bone.name == real_name:
            return bone


def load_rig(rig, min_bone=0.01):
    """
    Creates a bpy.types.Armature from a s3py_animation.SkeletonRig (Sims 3 Rig)
    """

    def make_bone_shape(draw_type, name, size, unlink=True):
        if name in bpy.data.objects:
            return bpy.data.objects[name]
        set_context('OBJECT')
        bpy.ops.object.add(type='EMPTY')
        shape = bpy.context.active_object
        shape.empty_draw_type = draw_type
        shape.name = '%s_bone_shape' % name
        shape.empty_draw_size = size
        if unlink:
            bpy.context.scene.objects.unlink(shape)
        return shape

    def walk_hierarchy(bone_source, func):
        children = []
        if isinstance(bone_source, Bone):
            func(bone_source)
            children = bone_source.children
        elif isinstance(bone_source, SkeletonRig):
            children = filter(lambda bone: not bone.parent, bone_source.bones)
        for child in children:
            walk_hierarchy(child, func)

    def create_armature_bone(bone):
        bone_name = bone.name
        original = find_bone(armature_data.edit_bones, bone_name)
        if original:
            return

        print('Creating %s (%i)' % (bone_name, len(armature_data.edit_bones)))
        armature_bone = armature_data.edit_bones.new(bone_name)
        armature_bone.tail = [0, min_bone, 0]
        if bone.parent:
            armature_parent = find_bone(armature_data.edit_bones, bone.parent.name)
            armature_bone.parent = armature_parent

    def pose_armature_bone(bone):
        pose_bone = armature_rig.pose.bones[bone.name]
        pose_bone.rotation_quaternion = Quaternion(quat_wxyz(bone.orientation))
        pose_bone.location = bone.position
        if bone.is_root():
            pose_bone.custom_shape = root_shape
        elif bone.is_slot():
            pose_bone.bone.hide = True
        else:
            pose_bone.custom_shape = bone_shape

    assert isinstance(rig, SkeletonRig)
    root_shape = make_bone_shape('PLAIN_AXES', 'root', 50)
    bone_shape = make_bone_shape('SPHERE', 'bone', 2.5)

    set_context('OBJECT')
    armature_name = "%s_Armature" % rig.name
    armature_data = bpy.data.armatures.new(name=armature_name)
    armature_data.draw_type = 'STICK'
    armature_rig = bpy.data.objects.new(armature_name, armature_data)
    armature_rig.show_x_ray = True
    armature_rig.name = rig.name
    armature_rig.data.name = '%s_Armature_Data' % rig.name
    bpy.context.scene.objects.link(armature_rig)

    set_context('EDIT', armature_rig)
    walk_hierarchy(rig, create_armature_bone)

    set_context('POSE', armature_rig)
    walk_hierarchy(rig, pose_armature_bone)
    bpy.ops.pose.armature_apply()

    set_context('OBJECT', armature_rig)
    bpy.ops.transform.rotate(value=math.pi / 2.0, axis=(1, 0, 0))

    return armature_rig 


def select_bone(armature_rig, names):
    if isinstance(names, str):
        names = [names]
    for b in armature_rig.pose.bones:
        b.bone.select = b.name in names
        if b.bone.select:
            armature_rig.data.bones.active = b.bone


def add_constraint_target(armature_rig, bone_name, offset, draw_type, draw_size):
    target = None
    pose_bones = armature_rig.pose.bones
    set_context('POSE', armature_rig)
    bone = pose_bones[bone_name]
    select_bone(armature_rig, bone_name)
    bpy.ops.pose.constraint_add_with_targets(type='DAMPED_TRACK')
    constraint = bone.constraints[-1]
    constraint.track_axis = 'TRACK_Z'
    target = constraint.target
    target.name = 'ik_%s_%s' % (armature_rig.name, bone_name)
    target.empty_draw_type = draw_type
    target.empty_draw_size = draw_size
    set_context('OBJECT', target)
    bpy.ops.transform.translate(value=(0, offset, 0), constraint_axis=(False, True, False))
    return target


def set_parent(child_obj, parent_obj):
    if len(bpy.context.selected_objects) > 0:
        bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.object.select_pattern(pattern=child_obj.name, extend=True)
    bpy.ops.object.select_pattern(pattern=parent_obj.name, extend=True)
    bpy.context.scene.objects.active = parent_obj
    print('setting parent: %s' % bpy.context.selected_objects)
    bpy.ops.object.parent_set(type='OBJECT')


def add_lookats(armature_rig):
    head = 'b__Head__'
    eyes = ('b__LeftEye__', 'b__RightEye__')
    head_target = add_constraint_target(armature_rig, head, -.2, 'CUBE', .015)
    for eye in eyes:
        eye_target = add_constraint_target(armature_rig, eye, -.15, 'SPHERE', .01)
        set_parent(eye_target, head_target)
    bpy.ops.object.select_all(action="DESELECT")
    #head_target.parent = armature_rig


def setup_ik(rig, armature_rig):
    armature_data = armature_rig.data
    bpy.ops.object.mode_set(mode='OBJECT')
    for ik in rig.ik_chains:
        start = ik.bones[-1]
        armature_start = armature_rig.data.bones[start.name]
        ik_helper_name = 'IK_%s' % armature_start.name[3:][:-2]
        ik_helper = None
        if ik.pole:
            bpy.ops.object.add(type='EMPTY')
            ik_pole_helper = bpy.context.active_object
            ik_pole_helper.empty_draw_type = 'SPHERE'
            ik_pole_helper.name = '%s_Pole' % ik_helper_name
            ik_pole_helper.empty_draw_size = .01

            ik_pole_constraint = ik_pole_helper.constraints.new('COPY_LOCATION')
            ik_pole_constraint.target = armature_rig
            ik_pole_constraint.subtarget = ik.pole.name
            ik_pole_helper.select = False

            bpy.context.scene.objects.active = armature_rig
            bpy.ops.object.mode_set(mode='POSE')
            armature_data.bones.active = armature_start
            bpy.ops.pose.constraint_add_with_targets(type='IK')
            armature_data.bones.active.select = False

            ik_constraint = bpy.context.active_pose_bone.constraints[-1]
            ik_helper = ik_constraint.target

            armature_rig.select = False
            bpy.ops.object.mode_set(mode='OBJECT')
            bpy.context.scene.objects.active = ik_helper
            ik_helper.empty_draw_type = 'CIRCLE'
            ik_helper.name = ik_helper_name
            ik_helper.empty_draw_size = .1
            ik_helper.select = False

            bpy.context.scene.objects.active = armature_rig
            ik_constraint.chain_count = len(ik.bones)
            ik_constraint.influence = 0.0
            ik_constraint.pole_target = ik_pole_helper
            #                ik_constraint.pole_subtarget = ik.pole.name
            ik_constraint.pole_angle = -math.pi / 2

            for info in ik.info_nodes:
                if not info:
                    continue
                info_constraint = ik_helper.constraints.new(type='COPY_TRANSFORMS')
                info_constraint.influence = 0.0
                info_constraint.name = info.name


def load_bone_delta(bond, skeleton):
    assert isinstance(bond, BoneDelta)

    for pose_bone in skeleton.pose.bones:
        hash = FNV32.hash(pose_bone.name)
        if hash in bond.deltas:
            delta = bond.deltas[hash]
            assert isinstance(delta, BoneDelta.Delta)
            pose_bone.scale += Vector(swizzle_v3(delta.scale))
            pose_bone.location += Vector(swizzle_v3(delta.position))
            pose_bone.rotation_quaternion += Quaternion(quat_wxyz(delta.orientation))


def save_bone_delta(skeleton):
    bond = BoneDelta()
    for pose_bone in skeleton.pose.bones:
        if pose_bone.location != Vector(0, 0, 0) or pose_bone.scale != Vector(1, 1,
            1) or pose_bone.rotation_quaternion != Quaternion(1, 0, 0, 0):
            delta = BoneDelta.Delta()
            delta.position = list(pose_bone.location)
            delta.scale = list(pose_bone.scale)
            delta.orientation = list(quat_xyzw(pose_bone.rotation_quaternion))
            bond.deltas[FNV32.hash(pose_bone.name)] = delta
    return bond


def find_closest(some_mesh, some_point):
    closest = None
    min_distance = None
    for vertex in some_mesh.vertices:
        distance = math.fabs((some_point - vertex.co).magnitude)
        if not closest or distance < min_distance:
            min_distance = distance
            closest = vertex
    return closest



