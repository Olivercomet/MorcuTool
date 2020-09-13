import bpy
import logging
from mathutils import Quaternion,Matrix,Vector,Euler
import math
import traceback
import sys

AXIS_FIXER = Quaternion((0.7071067690849304, 0.7071067690849304, 0.0, 0.0))
def s3_4x3_to_Matrix(s3_matrix):
    """
    Arranges a sequence of floats into a mathutils.Matrix
    """
    args = [tuple(m) for m in s3_matrix]
    args.append((0.0, 0.0, 0.0, 1.0))
    return Matrix(args)

def swizzle_uv(uv):
    return uv[0], 1 - uv[1]
def swizzle_v3(v3):
    return Vector([ v3[0],v3[1],v3[2]])

def quat_wxyz(quaternion):
    """
    Swap xyzw (order used by The Sims 3) to wxyz(order used by Blender).
    """
    return quaternion[3], quaternion[0], quaternion[1], quaternion[2]

def argb_rgb(argb):
    return argb[1:3]

def quat_xyzw(quaternion):
    return [quaternion[1], quaternion[2], quaternion[3], quaternion[0]]
def invalid_face(face):
    if not face: return  True
    f2 = face[:]
    a = None
    while len(f2):
        a = f2.pop()
        if a in f2:
            return True
    return False

def create_marker_node(name,rotate=False):
    set_context('OBJECT')
    bpy.ops.object.add(type='EMPTY')
    marker = bpy.context.active_object
    marker.name = name
    marker.show_x_ray = True
    marker.empty_draw_size = .1
    marker.empty_draw_type = 'CUBE'
    if rotate:
        marker.rotation_euler = Euler([math.pi/2,0,0])
        #bpy.ops.transform.rotate(value=(math.pi / 2), axis=(1, 0, 0))
    return marker
def set_context(mode=None,select=None):
    for obj in bpy.data.objects: obj.select = obj == select
    bpy.context.scene.objects.active = select
    if bpy.context.mode != mode: bpy.ops.object.mode_set(mode=mode)