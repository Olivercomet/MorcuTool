from s3py.core import Serializable, ExternalResource
from s3py.animation import ClipResource, TrackMask
from s3py.helpers import get_subclasses, Enum
from s3py.io import StreamWriter, StreamReader, RCOL


class AnimationOverlay(Enum):
    ThoughtBubble = 0
    OverlayFace = 1
    OverlayHead = 2
    OverlayBothArms = 3
    OverlayUpperBody = 4
    OverlayNone = 5
    Unset = 6

DEADBEEF = 0xDEADBEEF
DGN = '/DGN'

class DecisionGraph(RCOL):
    ID = 0x02EEDB18
    TAG = 'S_DG'

    class VERSION:
        DEFAULT = 0x00000101

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.children = []
        self.parents = []
        RCOL.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        assert s.u32() == 0x00000000
        self.children = [rcol.get_block(s.u32(), DecisionGraphNode.get_node_types()) for i in range(s.i32())]
        self.parents = [rcol.get_block(s.u32(), DecisionGraphNode.get_node_types()) for i in range(s.i32())]
        assert s.u32() == DEADBEEF

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.u32(0)
        s.i32(len(self.children))
        for child in self.children: s.u32(rcol.get_block_index(child))
        s.i32(len(self.parents))
        for parent in self.parents: s.u32(rcol.get_block_index(parent))
        s.u32(DEADBEEF)


class State(RCOL):
    ID = 0x02EEDAFE
    TAG = 'S_St'

    class VERSION:
        DEFAULT = 0x00000101

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.name = None
        self.properties = 0
        self.decision_graph = DecisionGraph()
        self.transitions = []
        self.awareness_overlay_level = 0
        RCOL.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.name = s.u32()
        self.properties = s.u32()
        self.decision_graph = rcol.get_block(s.u32(), DecisionGraph)
        self.transitions = [rcol.get_block(s.u32(), State) for i in range(s.i32())]
        self.awareness_overlay_level = s.u32()


    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.hash(self.name)
        s.u32(self.properties)
        s.u32(rcol.get_block_index(self.decision_graph, DecisionGraph))
        s.i32(len(self.transitions))
        for transition in self.transitions: s.u32(rcol.get_block_index(transition, State))
        s.u32(self.awareness_overlay_level)


class ActorDefinition(RCOL):
    ID = 0x02EEDB2F
    TAG = 'S_AD'

    class VERSION:
        DEFAULT = 0x00000100

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.name = None
        RCOL.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.name = s.u32()
        assert s.u32() == 0

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.hash(self.name)
        s.u32(0)


class ParameterDefinition(RCOL):
    ID = 0x02EEDB46
    TAG = 'S_PD'

    class VERSION:
        DEFAULT = 0x00000100

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.name = None
        self.default = None
        RCOL.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.name = s.u32()
        self.default = s.u32()

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.hash(self.name)
        s.hash(self.default)


class DecisionGraphNode(RCOL):
    NODE_TYPES = None

    def __init__(self, stream=None, resources=None, parent=None):
        RCOL.__init__(self, stream, resources, parent)

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        assert s.chars(4) == DGN

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        s.u32(DGN)

    @staticmethod
    def get_node_types():
        if DecisionGraphNode.NODE_TYPES == None:
            DecisionGraphNode.NODE_TYPES = tuple(filter(lambda c: c.ID > 0, get_subclasses(DecisionGraphNode)))
        return DecisionGraphNode.NODE_TYPES


class MulticastDecisionGraphNode(DecisionGraphNode):
    def __init__(self, stream=None, resources=None, parent=None):
        self.on_complete = []
        DecisionGraphNode.__init__(self, stream, resources, parent)

    def read(self, stream, resources=None):
        s = StreamReader(stream)
        self.on_complete = [resources.get_block(s.u32(), DecisionGraphNode.get_node_types()) for i in range(s.i32())]
        DecisionGraphNode.read(self, stream, resources)

    def write(self, stream, resources=None):
        s = StreamWriter(stream)
        s.i32(len(self.on_complete))
        for node in self.on_complete: s.u32(resources.get_block_index(node))
        DecisionGraphNode.write(self, stream, resources)


class AnimationNode(MulticastDecisionGraphNode):
    def __init__(self, stream=None, resources=None, parent=None):
        self.flags = 0
        self.priority = 0
        self.blend_in_time = 0.0
        self.blend_out_time = 0.0
        self.speed = 1.0
        self.actor = None
        timing_priority = 0
        MulticastDecisionGraphNode.__init__(self, stream, resources, parent)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.flags = s.u32()
        self.priority = s.u32()
        assert s.u32() == 0
        self.blend_in_time = s.f32()
        self.blend_out_time = s.f32()
        assert s.u32() == 0
        self.speed = s.f32()
        self.actor = rcol.get_block(s.u32(), ActorDefinition)
        self.timing_priority = s.u32()
        assert s.u32() == 0x00000010
        for i in range(5):
            assert s.u32() == 0
        assert s.u32() == DEADBEEF
        MulticastDecisionGraphNode.read(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        s.u32(self.flags)
        s.u32(self.priority)
        s.u32(0)
        s.f32(self.blend_in_time)
        s.f32(self.blend_out_time)
        s.u32(0)
        s.f32(self.speed)
        s.u32(rcol.get_block_index(self.actor, ActorDefinition))
        s.u32(self.timing_priority)
        s.u32(0x00000010)
        for i in range(5): s.u32(0)
        s.u32(DEADBEEF)
        MulticastDecisionGraphNode.write(self, stream, rcol)


class PlayAnimationNode(AnimationNode):
    ID = 0x02EEDB5F
    TAG = 'Play'

    class VERSION:
        DEFAULT = 0x00000101

    class SlotAssignment(Serializable):
        def __init__(self, stream=None):
            self.chain_id = 0
            self.slot_id = 0
            self.target_namespace = None
            self.target_slot = None
            Serializable.__init__(self, stream)

        def read(self, stream, resources=None):
            s = StreamReader(stream)
            self.chain_id = s.u32()
            self.slot_id = s.u32()
            self.target_namespace = s.u32()
            self.target_slot = s.u32()

        def write(self, stream, resources=None):
            s = StreamWriter(stream)
            s.u32(self.chain_id)
            s.u32(self.slot_id)
            s.hash(self.target_namespace)
            s.hash(self.target_slot)

    class NamespaceSlotSuffix(Serializable):
        def __init__(self, stream=None):
            self.target_namespace = None
            self.ik_suffix = None
            Serializable.__init__(self, stream)

        def read(self, stream, resources=None):
            s = StreamReader(stream)
            self.target_namespace = s.u32()
            self.ik_suffix = s.u32()

        def write(self, stream, resources=None):
            s = StreamWriter(stream)
            s.hash(self.target_namespace)
            s.hash(self.ik_suffix)

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.clip = ExternalResource(ClipResource.ID)
        self.track_mask = ExternalResource(TrackMask.ID)
        self.actor_slots = []
        self.actor_iks = []
        self.additive_clip = ExternalResource(ClipResource.ID)
        self.clip_pattern = ''
        self.additive_clip_pattern = ''
        AnimationNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.clip = ExternalResource(s.tgi('ITG'))
        self.track_mask = ExternalResource(s.tgi('ITG'))

        cActorSlots = s.i32()
        assert s.u32() == 0
        assert s.u32() == 0
        assert s.u32() == 0
        self.actor_slots = [self.SlotAssignment(stream) for i in range(cActorSlots)]
        self.actor_iks = [self.NamespaceSlotSuffix(stream) for i in range(s.i32())]

        assert s.u32() == DEADBEEF
        self.additive_clip = ExternalResource(s.tgi('ITG'))

        self.clip_pattern = s.p32(size=16)
        s.align()
        self.additive_clip_pattern = s.p32(size=16)
        s.align()
        assert s.u32() == DEADBEEF
        AnimationNode.read_rcol(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.tgi(self.clip, 'ITG')
        s.tgi(self.track_mask, 'ITG')

        s.i32(len(self.actor_slots))
        s.u32(0)
        s.u32(0)
        s.u32(0)
        for actor_slot in self.actor_slots: actor_slot.write(stream)
        s.i32(len(self.actor_iks))
        for actor_ik in self.actor_iks: actor_ik.write(stream)

        s.u32(DEADBEEF)
        s.tgi(self.additive_clip)
        s.p32(self.clip_pattern, size=16)
        s.align()
        s.p32(self.additive_clip_pattern, size=16)
        s.align()
        s.u32(DEADBEEF)
        AnimationNode.write_rcol(self, stream, rcol)


class RandomNode(DecisionGraphNode):
    ID = 0x02EEDB70
    TAG = 'Rand'

    class Outcome(Serializable):
        def __init__(self, stream=None, resources=None):
            self.weight = 1.0
            self.decision_graph_nodes = []
            Serializable.__init__(self, stream, resources)

        def read(self, stream, resources=None):
            s = StreamReader(stream)
            self.weight = s.f32()
            self.decision_graph_nodes = [resources.get_block(s.u32(), DecisionGraphNode.get_node_types())]

        def write(self, stream, resources=None):
            s = StreamWriter(stream)
            s.f32(self.weight)
            s.i32(len(self.decision_graph_nodes))
            for dgn in self.decision_graph_nodes: s.u32(resources.get_block_index(dgn))

    class VERSION:
        DEFAULT = 0x00000101

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.outcomes = []
        self.flags = 0x00000000
        DecisionGraphNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.outcomes = [self.Outcome(stream, rcol) for i in range(s.i32())]
        assert s.u32() == DEADBEEF
        self.flags = s.u32()
        DecisionGraphNode.read(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.i32(len(self.outcomes))
        for outcome in self.outcomes: outcome.write(stream, rcol)
        s.u32(DEADBEEF)
        s.u32(self.flags)
        DecisionGraphNode.write(self, stream, rcol)


class SelectOnParameterNode(DecisionGraphNode):
    ID = 0x02EEDB92
    TAG = 'SoPn'

    class VERSION:
        DEFAULT = 0x00000101

    class Item(object):
        def __init__(self):
            self.value = 0
            self.actions = []

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.parameter_index = None
        self.items = []
        DecisionGraphNode.__init__(self, key, stream, resources)


    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.parameter_index = s.i32()
        for item_index in range(s.i32()):
            item = self.Item()
            item.value = s.u32()
            for action_index in range(s.i32()):
                item.actions.append(rcol.get_block(s.u32(), DecisionGraphNode.get_node_types()))
            self.items.append(item)
        assert s.u32() == DEADBEEF
        DecisionGraphNode.read(self, stream, rcol)


    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.i32(self.parameter_index)
        s.i32(len(self.items))
        for item in self.items:
            s.hash(item.value)
            s.i32(len(item.actions))
            for action in item.actions:
                s.u32(rcol.get_block_index(action, DecisionGraphNode.get_node_types()))
        s.u32(DEADBEEF)
        DecisionGraphNode.write(self, stream, rcol)


class SelectOnDestinationNode(DecisionGraphNode):
    ID = 0x02EEDBA5
    TAG = 'SoDn'

    class VERSION:
        DEFAULT = 0x00000101

    def __init__(self, key=None, stream=None, resources=None):
        DecisionGraphNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        raise NotImplementedError()

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        raise NotImplementedError()


class SelectNextStateNode(DecisionGraphNode):
    ID = 0x02EEEBDC
    TAG = 'SNSN'

    class VERSION:
        DEFAULT = 0x00000101

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.next_state = None
        DecisionGraphNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.next_state = rcol.get_block(s.u32(), DecisionGraphNode.get_node_types())
        DecisionGraphNode.read(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.u32(rcol.get_block_index(self.next_state))
        DecisionGraphNode.read(self, stream, rcol)


class CreatePropNode(MulticastDecisionGraphNode):
    ID = 0x02EEEBDD
    TAG = 'Prop'

    class VERSION:
        DEFAULT = 0x00000100

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.actor = None
        self.parameter = None
        self.prop = ExternalResource()
        MulticastDecisionGraphNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.actor = rcol.get_block(s.u32(), ActorDefinition)
        self.parameter = s.u32()
        self.prop = s.tgi('ITG')
        for i in range(4):
            assert s.u32() == 0
        MulticastDecisionGraphNode.read(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.u32(rcol.get_block_index(self.actor))
        s.u32(self.parameter)
        s.tgi(self.prop, 'ITG')
        for i in range(4):
            s.u32(0)
        MulticastDecisionGraphNode.write(self, stream, rcol)


class ActorOperationNode(MulticastDecisionGraphNode):
    ID = 0x02EEEBDE
    TAG = 'AcOp'

    class VERSION:
        DEFAULT = 0x00000100

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.target = None
        self.operation = 0
        self.operand = 0
        MulticastDecisionGraphNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.target = rcol.get_block(s.u32(), ActorDefinition)
        self.operation = s.u32()
        self.operand = s.u32()
        for i in range(3): assert s.u32() == 0
        MulticastDecisionGraphNode.read(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.u32(rcol.get_block_index(self.target))
        s.u32(self.operation)
        s.u32(self.operand)
        for i in range(3): s.u32(0)
        MulticastDecisionGraphNode.write(self, stream, rcol)


class StopAnimationNode(AnimationNode):
    ID = 0x0344D438
    TAG = 'Stop'

    def __init__(self, key=None, stream=None, resources=None):
        AnimationNode.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32(self.version)
        AnimationNode.read_rcol(self, stream, rcol)

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        AnimationNode.write_rcol(self, stream, rcol)


class StateMachine(RCOL):
    ID = 0x02D5DF13
    TAG = 'S_SM'

    class ActorPair(Serializable):
        def __init__(self, stream, resources=None):
            self.filename = None
            self.clip_actor = None
            self.jazz_actor = None
            Serializable.__init__(self, stream, resources)

        def read(self, stream, resources=None):
            s = StreamReader(stream)
            self.filename = s.u32()
            self.clip_actor = s.u32()
            self.jazz_actor = s.u32()

        def write(self, stream, resources=None):
            s = StreamWriter(stream)
            s.hash(self.filename)
            s.hash(self.clip_actor)
            s.hash(self.jazz_actor)

    class VERSION:
        DEFAULT = 0x00000202

    def __init__(self, key=None, stream=None, resources=None):
        self.version = self.VERSION.DEFAULT
        self.name = None
        self.actors = []
        self.parameters = []
        self.states = []
        self.namespace_map = []
        self.flags = 0
        self.default_priority = 0
        self.awareness_overlay_level = AnimationOverlay.OverlayNone
        RCOL.__init__(self, key, stream, resources)

    def read_rcol(self, stream, rcol):
        s = StreamReader(stream)
        self.read_tag(stream)
        self.version = s.u32()
        self.name = s.u32()
        self.actors = [rcol.get_block(s.u32(), ActorDefinition) for i in range(s.i32())]
        self.parameters = [rcol.get_block(s.u32(), ParameterDefinition) for i in range(s.i32())]
        self.states = [rcol.get_block(s.u32(), State) for i in range(s.i32())]
        self.namespace_map = [self.ActorPair(stream) for i in range(s.i32())]
        assert s.u32() == DEADBEEF
        self.flags = s.u32()
        self.default_priority = s.i32()
        self.awareness_overlay_level =AnimationOverlay(s.i32())
        for i in range(4): assert s.u32() == 0

    def write_rcol(self, stream, rcol):
        s = StreamWriter(stream)
        self.write_tag(stream)
        s.u32(self.version)
        s.hash(self.name)
        s.i32(len(self.actors))
        for actor in self.actors: s.u32(rcol.get_block_index(actor, ActorDefinition))
        s.i32(len(self.parameters))
        for parameter in self.parameters: s.u32(rcol.get_block_index(parameter, ParameterDefinition))
        s.i32(len(self.states))
        for state in self.states: s.u32(rcol.get_block_index(state, State))
        s.i32(len(self.namespace_map))
        for actor_pair in self.namespace_map: actor_pair.write(stream)
        s.u32(DEADBEEF)
        s.u32(self.flags)
        s.u32(self.default_priority)
        s.u32(self.awareness_overlay_level)
        for i in range(4): s.u32(0)
