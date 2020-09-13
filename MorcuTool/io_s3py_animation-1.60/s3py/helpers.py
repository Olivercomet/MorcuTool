def first(iterable, predicate=None):
    if predicate:
        iterable = filter(predicate, iterable)
    return next(iterable, None)


def get_subclasses(t):
    for g in filter(lambda x: isinstance(t, object), globals().values()):
        try:
            if issubclass(g, t):
                yield g
        except TypeError:
            continue


class FNV32(object):
    prime = 0x01000193
    offset = 0x811C9DC5
    mask = 0xFFFFFFFF

    @classmethod
    def hash(cls,string):
        h = cls.offset
        byteArray = str(string).lower().encode(encoding='ascii')
        for b in list(byteArray):
            h *= cls.prime
            h ^= b
            h &= cls.mask
        return h

class FNV64(FNV32):
    prime = 0x00000100000001B3
    offset = 0xCBF29CE484222325
    mask = 0xFFFFFFFFFFFFFFFF

class Flag(object):
    @staticmethod
    def is_set(field, flag): return (field & flag) != 0

    @staticmethod
    def set(field, flag): return field | flag

    @staticmethod
    def unset(field, flag): return field & (0xFFFFFFFF ^ flag)


class HashedString(object):

    def __init__(self, value):
        self.__string = None
        self.__hash = None
        if isinstance(value, int):
            self.hash = value
        elif isinstance(value, str):
            self.string = value
        elif isinstance(value, HashedString):
            self.string = value.string
            self.hash = value.hash

    def get_hash(self):
        return self.__hash

    def set_hash(self, value):
        assert isinstance(value, int)
        self.__hash = value
        self.__string = ''

    hash = property(get_hash, set_hash)

    def get_string(self):
        return self.__string

    def set_string(self, value):
        assert isinstance(value, str)
        self.__string = value
        self.__hash = FNV32.hash(value) if any(value) else 0

    string = property(get_string, set_string)

    def __contains__(self, item):
        item = set(item)
        for e in item:
            if self.__eq__(e):
                return True
        return False
    def __str__(self):
        return self.string if self.string else "%08X" % self.hash

    def __int__(self):
        return self.hash

    def __hash__(self):
        return self.hash

    def __eq__(self, other):
        if isinstance(other, int):
            return self.hash == other
        elif isinstance(other, str):
            return FNV32.hash(other) == self.hash
        elif isinstance(other, HashedString):
            return self.hash == other.hash
        else:
            raise Exception("Unable to compare %s and %s" % (type(self), type(other) ))

class Enum(object):
    __enum_dict = None
    @classmethod
    def __enum(cls):
        if not cls.__enum_dict:
            cls.__enum_dict = {}
            for key in cls.__dict__:
                if key[0] == '_':
                    continue
                val = cls.__dict__[key]
                if isinstance(val,int):
                    cls.__enum_dict[key] = val
                    cls.__enum_dict[val] = key
        return cls.__enum_dict

    def __init__(self,value):
        if isinstance(value,str):
            if value in self.__enum():
                value = self.__enum()[value]
            else:
                value = int(value)
        if isinstance(value,int):
            self.__value = value
        else:
            raise TypeError('Value incompatible with enum')


    def __int__(self):
        return self.__value
    def __str__(self):
        return self.__enum()[self.__value] if self.__value in self.__enum() else 'Unknown(%08X)'%int(self.__value)
    def __eq__(self, other):
        return int(self) == int(other)