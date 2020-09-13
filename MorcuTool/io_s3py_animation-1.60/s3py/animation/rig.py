from s3py.core import Serializable, ChildElement, PackedResource
from s3py.helpers import FNV32,HashedString
from s3py.io import StreamWriter, StreamReader, RCOL

class Bone:
    def __init__(self, skeleton):
        self.skeleton = skeleton
        self.position = [0.0, 0.0, 0.0]
        self.orientation = [0.0, 0.0, 0.0, 1.0]
        self.scale = [1.0, 1.0, 1.0]
        self.flags = 0
        self.__children = []
        self.__name = None
        self.__parent = None
        self.__opposite = None

    container_slots = {
        'cntm',
        'container',
        'carry'
    }
    target_slots = {
        'target',
        'surface',
        'grip'
    }
    route_slots = {
        'route'
    }
    effect_slots = {
        'fx'
    }
    slots = [
        'slot'
    ]
    slots.extend(route_slots)
    slots.extend(target_slots)
    slots.extend(container_slots)
    slots.extend(effect_slots)

    ik_bones = {
        'world',
        'world_offset',
        'info',
        'export',
        'rootoffset',
        'slotoffset',
        'footoffset'
    }
    slider_bones = {
        'compress',
        'twist',
        'back',
        'chest',
        'shoulder',
        'wrist',
        'ulna',
        'radius',
        'hip',
        'bicep',
        'quadricep',
        'backcalf',
        'skirt',
        'sleeve'
    }
    misc_bones = {
        'breast',
        'belly',
        'stomach',
        'rump'
    }


    def get_children(self):
        return self.__children

    children = property(get_children)

    def set_name(self, name):
        if name == None:
            raise Exception("Bone name cannot be None")
        for bone in self.skeleton.bones:
            if bone.__name == name:
                print("WARNING: You cannot have duplicate names in a skeleton")
        self.__name = name

    def get_name(self):
        return self.__name

    name = property(get_name, set_name)

    #checks for certain naming conventions to determine function
    def is_a(self, name_set):
        if not isinstance(name_set,set) and isinstance(name_set,str):
            name_set = {name_set}
        return any(filter(lambda n: n.lower() in self.name.lower(), name_set))

    def is_root(self):
        return self.is_a('root')

    def is_export_root(self):
        return self.is_a('export_root')

    def is_route(self):
        return self.is_a(self.route_slots)

    def is_container(self):
        return self.is_a(self.container_slots)

    def is_slot(self):
        return self.is_a(self.slots)

    def is_ik(self):
        return self.is_a(self.ik_bones)

    def is_slider(self):
        return self.is_a(self.slider_bones)

    def is_target(self):
        return self.is_a(self.target_slots)

    def is_misc(self):
        return self.is_a(self.misc_bones)

    def is_skeletal(self):
        return not (self.is_root() or self.is_ik() or self.is_slider() or self.is_slot() or self.is_misc())

    def is_static(self):
        return self.orientation[3] == 1.0 and 0.0 == self.position[0] == self.position[1] == self.position[2] == self.orientation[0] == self.orientation[1] == self.orientation[2]


    def set_opposite(self, opposite):
        if opposite == None:
            opposite = self
        if not opposite in self.skeleton.bones:
            raise Exception("Opposite bone must be from the same skeleton")
        if not self.__opposite == opposite:
            self.__opposite = opposite
        if not opposite.opposite == self:
            opposite.opposite = self

    def get_opposite(self):
        return self.__opposite

    opposite = property(get_opposite, set_opposite)

    def set_parent(self, parent):
        if not parent is None and not parent in self.skeleton.bones:
            raise Exception("Parent bone must be from the same skeleton")
        if not self.__parent == None:
            self.__parent.__children.remove(self)
        self.__parent = parent
        if not self.__parent == None:
            self.__parent.__children.append(self)

    def get_parent(self):
        return self.__parent

    parent = property(get_parent, set_parent)

    def get_siblings(self):
        if not self.__parent:
            return []
        return filter(lambda bone: bone != self, self.__parent.__children)

    siblings = property(get_siblings)

    def __eq__(self, other):
        return isinstance(other, Bone) and other.name == self.name

    def __hash__(self):
        return hash(self.name)

    def __str__(self):
        return "%s: Position: %s Orientation: %s" % (self.name, self.position, self.orientation)


class IKChain(Serializable, ChildElement):
    def __init__(self, parent, stream=None):
        self.bones = []
        self.info_nodes = [None] * 11
        self.pole = None
        self.slot_info = None
        self.slot_offset = None
        self.root = None
        ChildElement.__init__(self, parent)
        Serializable.__init__(self, stream)

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.bones = [self.parent.get_bone(s.i32()) for i in range(s.i32())]
        self.info_nodes = [self.parent.get_bone(s.i32()) for i in range(11)]
        self.pole = self.parent.get_bone(s.i32())
        self.slot_info = self.parent.get_bone(s.i32())
        self.slot_offset = self.parent.get_bone(s.i32())
        self.root = self.parent.get_bone(s.i32())

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        for bone_index in self.bones: s.i32(self.parent.get_bone_index(bone_index))
        for info_node_index in self.info_nodes: s.i32(self.parent.get_bone_index(info_node_index))
        s.i32(self.parent.get_bone_index(self.pole))
        s.i32(self.parent.get_bone_index(self.slot_info))
        s.i32(self.parent.get_bone_index(self.slot_offset))
        s.i32(self.parent.get_bone_index(self.root))


class IKTrackData(object):
    def __index__(self):
        self.slot_info = None
        self.offset = None
        self.pole = None
        self.targets = []


class SkeletonRig(PackedResource):
    ID = 0x8EAF13DE

    def __init__(self, key=None):
        PackedResource.__init__(self, key)
        self.version_major = 4
        self.version_minor = 2
        self.bones = []
        self.ik_chains = []
        self.resource_name = None
        self.parent = None
        self.opposite = None
        self.name = None
        self.__hashes = {}

    def get_bone(self, index):
        return None if index < 0 or index > len(self.bones) else self.bones[index]

    def get_bone_index(self, bone):
        return -1 if not bone else self.bones.index(bone)

    def __getitem__(self, item):
        if isinstance(item,int) and item < len(self.bones):
            return self.bones[item]
        elif isinstance(item,str) and FNV32.hash(item) in self.__hashes:
            return self.__hashes[FNV32.hash(item)]
        return None

    def __str__(self):
        return "%s (%i)" % (PackedResource.__str__(self),len(self.bones))

    def read(self, stream, resource=None):
        s = StreamReader(stream)
        self.version_major = s.u32()
        self.version_minor = s.u32()
        cBones = s.i32()
        self.bones = []
        opposites = []
        parents = []
        self.__hashes = {}
        for i in range(cBones):
            bone = Bone(self)
            bone.position = [s.f32() for i in range(3)]
            bone.orientation = [s.f32() for i in range(4)]
            bone.scale = [s.f32() for i in range(3)]
            bone.name = s.p32()
            opposites.append(s.i32())
            parents.append(s.i32())
            hash_name = s.u32()
            if not hash_name == FNV32.hash(bone.name):
                print("WARNING: Bone %s should have matching hash 0x%08X, but has 0x%08X",bone.name, FNV32.hash(bone.name),hash_name)
            self.__hashes[hash] = bone
            bone.flags = s.u32()
            self.bones.append(bone)
        for bone_index, opposite_index in enumerate(opposites):
            if opposite_index >= 0:
                self.bones[bone_index].opposite = self.bones[opposite_index]
        for bone_index, parent_index in enumerate(parents):
            if parent_index >= 0:
                self.bones[bone_index].parent = self.bones[parent_index]
        if self.version_major >= 4: self.name = s.p32()
        self.ik_chains = []
        cChains = s.i32()
        for i in range(cChains):
            chain = IKChain(self)
            chain.read(stream)
            self.ik_chains.append(chain)

    def write(self, stream, resource=None):
        s = StreamWriter(stream)
        s.u32(self.version_major)
        s.u32(self.version_minor)
        s.i32(len(self.bones))
        for bone in self.bones:
            for i in range(3): s.f32(bone.position[i])
            for i in range(4): s.f32(bone.orientation[i])
            for i in range(3): s.f32(bone.scale[i])
            assert bone.name != None, "Bone %s must have a name"
            s.p32(bone.name)
            s.i32(-1 if not bone.opposite in self.bones else self.bones.index(bone.opposite))
            s.i32(-1 if not bone.parent in self.bones else self.bones.index(bone.parent))
            s.u32(FNV32.hash(bone.name))
            s.u32(bone.flags)
        if self.version_major >= 4: s.p32(self.name)
        s.i32(len(self.ik_chains))
        for chain in self.ik_chains:
            chain.write(stream)


class BoneDelta(RCOL):
    ID = 0x0355E0A6
    class VERSION:
        DEFAULT = 0x00000003

    def __init__(self,key=None,stream=None,resources=None):
        self.version = self.VERSION.DEFAULT
        self.deltas = {}
        RCOL.__init__(self,key,stream,resources)

    class Delta(Serializable):
        def __init__(self, stream=None, resources=None):
            self.position = [0.0,0.0,0.0]
            self.scale = [1.0,1.0,1.0]
            self.orientation = [0.0,0.0,0.0,1.0]
            Serializable.__init__(self,stream,resources)

        def read(self, stream, resources=None):
            s = StreamReader(stream)
            self.position = [s.f32() for i in range(3)]
            self.scale = [s.f32() for i in range(3)]
            self.orientation = [s.f32() for i in range(4)]

        def write(self, stream, resources=None):
            s = StreamWriter(stream)
            for i in range(3): s.f32(self.position[i])
            for i in range(3): s.f32(self.scale[i])
            for i in range(4): s.f32(self.orientation[i])

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.version = s.u32()
        self.deltas = {}
        c = s.i32()
        for i in range(c):
            hsh = s.hash()
            self.deltas[hsh] = self.Delta(stream)
        pass


    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        s.u32(self.version)
        s.i32(len(self.deltas))

        for hash in self.deltas:
            delta = self.deltas[hash]
            s.u32(hash)
            delta.write(stream)
    def print(self):
        s= str(vars(self))
        s += 'Version %08X\r\n' % self.version
        s+= 'Deltas:\r\n'
        for key in self.deltas:
            delta = self.deltas[key]
            s+= '[%08X]\r\n'%key
            s+= '%s\r\n%s\r\n%s\r\n' %(delta.position,delta.orientation,delta.scale)
            pass
        return s




