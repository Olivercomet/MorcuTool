import os
import winreg

GUIDS = [
    '{C05D8CDB-417D-4335-A38C-A0659EDFD6B8}',
    '{BA26FFA5-6D47-47DB-BE56-34C357B5F8CC}',
    '{71828142-5A24-4BD0-97E7-976DA08CE6CF}',
    '{910F4A29-1134-49E0-AD8B-56E4A3152BD1}',
    '{ED436EA8-4145-4703-AE5D-4D09DD24AF5A}',
    '{45057FCE-5784-48BE-8176-D9D00AF56C3C}',
    '{117B6BF6-82C3-420C-B284-9247C8568E53}',
    '{E6B88BD6-E4B2-4701-A648-B6DAC6E491CC}',
    '{7B11296A-F894-449C-8DF6-6AAAA7D4D118}',
    '{C12631C6-804D-4B32-B0DD-8A496462F106}'
]

class Filetable(object):
    class Entry(object):
        def __init__(self,key):
            self.product_key = winreg.QueryValueEx(key, 'ProductKey')[0]
            self.display_name = winreg.QueryValueEx(key, 'DisplayName')[0]
            self.install_location = winreg.QueryValueEx(key, 'InstallLocation')[0]
        def __str__(self):
            return self.product_key
        def __hash__(self):
            return hash(self.product_key)
        def __eq__(self, other):
            return isinstance(other, Entry) and other.product_key == self.product_key

    package_subdir = '\\GameData\\Shared\\Packages\\'
    delta_subdir = '\\GameData\\Shared\\DeltaPackages\\'
    thumb_subdir = '\\Thumbnails\\'
    entries = None

    @classmethod
    def load_paths(cls):
        cls.entries = []
        regNodePath = "SOFTWARE%s\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" %  ("\\Wow6432Node" if 'PROGRAMFILES(X86)' in os.environ else '')
        for productKey in GUIDS:
            keyName = regNodePath + productKey
            try:
                with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE,keyName,0,winreg.KEY_WOW64_64KEY|winreg.KEY_READ) as key:
                    entry = cls.Entry(key)
                    cls.entries.append(entry)
                    print("Found %s"%entry)
            except Exception as ex:
                pass
    def __init__(self):
        if not self.entries:
            self.load_paths()