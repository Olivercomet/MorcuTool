from s3py.core import Serializable, ResourceKey, PackedResource, ExternalResource
from xml.dom.minidom import Document, parseString
from s3py.io import TGIList

class Preset(object):
    class Element(object):
        def __init__(self):
            self.name = ""
            self.resource = ExternalResource(ResourceKey())
            self.values = {}
            self.patterns = []
            self.variable = None
    def __init__(self):
        self.complate = self.Element()

    def read_xml(self, xml_string, resources):
        def parse_value(value_string,resources):
            items = []
            strings = value_string.split(',')
            for string in strings:
                if string[:4] == 'key:':
                    keyVals = string[4:].split(':')
                    key = ResourceKey(int(keyVals[0], 16), int(keyVals[1], 16), int(keyVals[2], 16))
                    items.append(key)
                elif string in ('true', 'false'):
                    items.append(string == 'true')
                else:
                    items.append(string)
            return items if len(items) > 1 else items[0]

        doc = parseString(xml_string)
        presetElement = doc.documentElement
        complateElement = presetElement.getElementsByTagName('complate')[0]
        if not resources:
            resources = TGIList()

        self.complate = self.Element()
        self.complate.name = complateElement.nodeName
        self.complate.resource = parse_value(complateElement.attributes['reskey'].nodeValue,resources)
        for keyElement in complateElement.getElementsByTagName('value'):
            key = keyElement.attributes['key'].nodeValue
            value = keyElement.attributes['value'].nodeValue
            self.complate.values[key] = parse_value(value,resources)
        for patternElement in complateElement.getElementsByTagName('pattern'):
            pattern = self.Element()
            pattern.name = patternElement.nodeName
            pattern.resource = parse_value(patternElement.attributes['reskey'].nodeValue,resources)
            pattern.variable = parse_value(patternElement.attributes['variable'].nodeValue,resources)
            for keyElement in patternElement.getElementsByTagName('value'):
                key = keyElement.attributes['key'].nodeValue
                value = keyElement.attributes['value'].nodeValue
                pattern.values[key] = parse_value(value,resources)
            self.complate.patterns.append(pattern)

    def write_xml(self, resources):
        def unparse_value(value):
            pass

        doc = Document()
        presetElement = doc.createElement("preset")

        doc.appendChild(presetElement)
        complateElement = doc.createElement('complate')
        complateElement.setAttribute('reskey', unparse_value(self.complate.resource))
        presetElement.appendChild(complateElement)
        for key in self.complate.values.keys():
            keyElement = doc.createElement('value')
            keyElement.setAttribute('key', key)
            keyElement.setAttribute('value', unparse_value(self.complate.values[key]))
            complateElement.appendChild(keyElement)
        for pattern in self.complate.patterns:
            patternElement = doc.createElement('pattern')
            patternElement.setAttribute('reskey', unparse_value(pattern.resource))
            complateElement.appendChild(patternElement)
            for key in pattern.values.keys():
                keyElement = doc.createElement('value')
                keyElement.setAttribute('key', key)
                keyElement.setAttribute('value', unparse_value(self.complate.values[key]))
                patternElement.appendChild(keyElement)
        return doc.toxml('utf-16')

    def read(self, stream, resources=None):
        raise NotImplementedError()

    def write(self, stream, resources=None):
        raise NotImplementedError()
class PackedPreset(PackedResource,Preset):
    ID = 0x0333406C
    def __init__(self, key=None, stream=None, resources=None, name=None):
        PackedResource.__init__(self,key,stream,resources,name)
    def read(self, stream, resources=None):
        self.read_xml(stream.read(),resources)

