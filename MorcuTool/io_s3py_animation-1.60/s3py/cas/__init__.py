class Age(object):
    BABY = 0x00000001
    TODDLER = 0x00000002
    CHILD = 0x00000004
    TEEN = 0x00000008
    YOUNG_ADULT = 0x00000010
    ADULT = 0x00000020
    ELDER = 0x00000040


class Gender(object):
    MALE = 0x00001000
    FEMALE = 0x00002000


class Species(object):
    HUMAN = 0x00000000
    HORSE = 0x00000200
    CAT = 0x00000300
    DOG = 0x00000400
    LITTLE_DOG = 0x00000500
    DEER = 0x00000600
    RACCOON = 0x00000700


class Handedness(object):
    LEFT = 0x00100000
    RIGHT = 0x00200000


class BlendType(object):
    ARCHETYPE = 1
    MODIFIER = 2