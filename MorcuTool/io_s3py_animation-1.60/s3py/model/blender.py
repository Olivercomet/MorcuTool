import bpy
import s3py
from s3py.blender import find_sims3_content_root, create_sims3_content_node
from s3py.animation.blender import find_bone, SkeletonRig
from s3py.model import SlotRig, Slot
from mathutils import Vector,Matrix,Quaternion



def generate_tangents(triangles,vertices,normals,uvs):
    indices = []
    for triangle in triangles:
        for i in range(3):
            indices.append(triangle.vertices[i])
    tan1 = [Vector()] * len(triangles)
    for i in range(len(indices)):
        i1 = indices[i+0]
        i2 = indices[i+1]
        i3 = indices[i+2]

        v1 = vertices[i1]
        v2 = vertices[i2]
        v3 = vertices[i3]

        w1 = uvs[i1]
        w2 = uvs[i2]
        w3 = uvs[i3]

        x1 = v2.x - v1.x
        x2 = v3.x - v1.x
        y1 = v2.y - v1.Y
        y2 = v3.Y - v1.Y
        z1 = v2.z - v1.z
        z2 = v3.z - v1.z

        s1 = w2.x - w1.x
        s2 = w3.x - w1.x
        t1 = w2.Y - w1.Y
        t2 = w3.Y - w1.Y

        r = 1.0/(s1*t2 - s2*t1)

        sdir = Vector((t2*x1 - t1*x2)*r, (t2*y1 - t1*y2)*r, (t2*z1 - t1*z2)*r)
        tan1[i1] += sdir
        tan1[i2] += sdir
        tan1[i3] += sdir
    for i in range(len(vertices)):
        n = normals[i]
        t = tan1[i]
        tmp = (t- n * n.dot(t))
        tmp.normalize()
        print(tmp)

def load_ftpt(ftpt):
    pass

def load_rslt(rslt,armature,names):
    if isinstance(rslt,SlotRig):
        slot_root = create_sims3_content_node('slot','SLOT',rslt.key)
        def create_slot_node(root,slot,slot_type,set_fields=None):
            if isinstance(slot,Slot):
                bone_name = '%0X8'%int(slot.bone_name)
                if slot.bone_name in names:
                    bone_name = names[slot.bone_name]
                slot_name = '%0X8'%int(slot.name)
                if slot.name in Slot.NAMES:
                    slot_name = Slot.NAMES[slot.name]
                slot_node = create_sims3_content_node(bone_name,slot_type)
                slot_node.hide = False
                slot_node.empty_draw_type = 'ARROWS'
                slot_node.parent = root
                if set_fields:
                    set_fields(slot_node)
                slot_matrix = s3py.blender.s3_4x3_to_Matrix(slot.transform)
                slot_node.rotation_quaternion = slot_matrix.to_quaternion()
                slot_node.location = slot_matrix.to_translation()
                bone_bind = slot_node.constraints.new(type='CHILD_OF')
                armature_bone = find_bone(armature.data.edit_bones,bone_name)
                if armature_bone:
                    bone_bind.target = armature
                    bone_bind.subtarget = armature_bone.name
                return slot_node
            else:
                raise Exception("Expected an instance of s3py.model.Slot")
        for slot in rslt.container_slots: create_slot_node(slot_root,slot,'CONTAINER')
        for slot in rslt.routing_slots: create_slot_node(slot_root,slot,'ROUTE')
        for slot in rslt.target_slots: create_slot_node(slot_root,slot,'TARGET')
        for slot in rslt.effect_slots: create_slot_node(slot_root,slot,'EFFECT')
        for slot in rslt.cone_slots: create_slot_node(slot_root,slot,'CONE')
        return slot_root
