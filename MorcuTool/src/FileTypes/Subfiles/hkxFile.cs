using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class hkxFile
    {

        public hkxClassTable classTable;
        public hkxDataSection dataSection;

        public List<havokObject> havokObjects = new List<havokObject>();

        List<string> obj = new List<string>();
        int cumulativeVertCount = 0;

        //Havok collision file

        //You can find these in MySims Kingdom

        //I believe MySims Agents might use a custom version that is truncated to just the __data__ section

        //(all big endian)

        //0x20 bytes of EA header, which is removed during processing here, and then:

        //0x00:      0x57E0E057 hkx magic
        //0x04:      0x10C0C010 hkx magic
        //0x08:      0x00000000 hkx magic
        //0x0C:      0x00000004 hkx version
        //0x10:      0x04000101 hkx build number

        //0x14:      0x00000003 constant    (num sections?)
        //0x18:      0x00000001 constant
        //0x1C:      0x00000000 constant
        //0x20:      0x00000000 constant
        //0x24:      0x000000CB constant

        //0x28:     char[0x18] null-terminated havok version name string

        //then for each section header:

        //  char[16] null-terminated section name
        //  0x000000FF magic
        //  u32 section offset
        //  u32 data
        //  u32 data
        //  u32 data
        //  u32 data
        //  u32 data
        //  u32 data


        public class hkxClassTable {

            public string name = "";
            public int sectionOffset;
            public int endOfClassTableOffset;
            public int val2;
            public int val3;
            public int val4;
            public int val5;
            public int val6;

            public List<hkxClassTableEntry> entries = new List<hkxClassTableEntry>();

            public hkxClassTable(Subfile basis, int offset)
            {
                int pos = offset;

                while (basis.filebytes[pos] != 0x00)
                {
                    name += (char)basis.filebytes[pos];
                    pos++;
                }

                pos = offset + 0x14;    //skips over 4-byte magic 0x000000FF

                sectionOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                endOfClassTableOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val2 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val3 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val4 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val5 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val6 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;

                //now actually read the entries

                pos = sectionOffset;

                while (pos < endOfClassTableOffset)
                {
                    hkxClassTableEntry newEntry = new hkxClassTableEntry(this, basis, pos);

                    if (newEntry.ID != 0xFFFFFFFF)
                    {
                        entries.Add(newEntry);
                    }
                    pos = newEntry.endOffset;
                    Console.WriteLine("Added hkx class entry: " + newEntry.name);
                }
                Console.WriteLine(entries.Count);
            }

            public class hkxClassTableEntry
            {
                public uint ID;
                public byte unk;
                public int offset; //this offset goes to the beginning of the name, it's what the virtual fixup table talks to
                public string name = "";
                public int endOffset;

                public hkxClassTableEntry(hkxClassTable parent, Subfile basis, int pos)
                {
                    ID = BitConverter.ToUInt32(basis.filebytes, pos);
                    pos += 4;
                    unk = basis.filebytes[pos];
                    pos++;
                    offset = pos - parent.sectionOffset;

                    while (basis.filebytes[pos] != 0x00) {
                        name += (char)basis.filebytes[pos];
                        pos++;
                    }

                    pos++;

                    endOffset = pos;
                }
            }
        }

        public class hkxDataSection
        {

            public string name = "";
            public int sectionOffset;
            public int localFixupTableOffset; //relative to sectionOffset
            public int globalFixupTableOffset; //relative to sectionOffset
            public int virtualFixupTableOffset; //relative to sectionOffset
            public int val4;
            public int val5;
            public int val6;

            public List<hkxLocalFixupEntry> localFixupTable = new List<hkxLocalFixupEntry>();
            public List<hkxVirtualFixupEntry> virtualFixupTable = new List<hkxVirtualFixupEntry>();

            public hkxDataSection(Subfile basis, int offset)
            {
                int pos = offset;

                while (basis.filebytes[pos] != 0x00)
                {
                    name += (char)basis.filebytes[pos];
                    pos++;
                }

                pos = offset + 0x14;    //skips over 4-byte magic 0x000000FF

                sectionOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                localFixupTableOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                globalFixupTableOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                virtualFixupTableOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val4 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val5 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;
                val6 = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                pos += 4;

                //now actually read the sections

                pos = sectionOffset + localFixupTableOffset;

                while (pos + 0x08 <= sectionOffset + globalFixupTableOffset)
                {
                    localFixupTable.Add(new hkxLocalFixupEntry(basis, pos));
                    pos += 0x08;
                }

                pos = sectionOffset + virtualFixupTableOffset;

                while (pos + 0x0C <= sectionOffset + val4) {
                    virtualFixupTable.Add(new hkxVirtualFixupEntry(basis, pos));
                    pos += 0x0C;
                }
            }

            public class hkxVirtualFixupEntry
            {
                public int objectOffset; //offset to the object from the beginning of the data section
                public int classOffset;  //offset to the type of object in the class table

                public hkxVirtualFixupEntry(Subfile basis, int pos)
                {
                    objectOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                    pos += 4;
                    pos += 4; //skip unk, always 0?
                    classOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                }
            }

            public class hkxLocalFixupEntry
            {
                public int destOffset; 
                public int srcOffset;  

                public hkxLocalFixupEntry(Subfile basis, int pos)
                {
                    destOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                    pos += 4;
                    srcOffset = utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, pos));
                }
            }
        }

        public hkxFile (Subfile basis) {

            basis.hkx = this;
            obj = new List<string>();
            cumulativeVertCount = 0;

            byte[] withoutHeader = new byte[basis.filebytes.Length - 0x20];
            Array.Copy(basis.filebytes, 0x20, withoutHeader, 0x00, basis.filebytes.Length - 0x20);
            basis.filebytes = withoutHeader;

            classTable = new hkxClassTable(basis, 0x40);
            dataSection = new hkxDataSection(basis, 0x70);

            foreach (hkxDataSection.hkxVirtualFixupEntry virtualFixupEntry in dataSection.virtualFixupTable) {

                ParseHavokObject(basis, virtualFixupEntry);
            
            }

            System.IO.File.WriteAllLines(basis.filename + ".obj", obj.ToArray());
        }
        public void ParseHavokObject(Subfile basis, hkxDataSection.hkxVirtualFixupEntry entry) {

            for (int i = 0; i < classTable.entries.Count; i++) {

                if (classTable.entries[i].offset == entry.classOffset) {

                    hkxClassTable.hkxClassTableEntry target = classTable.entries[i];

                    switch (target.name) {

                        case "hkBoxShape":
                            hkBoxShape newBoxShape = new hkBoxShape(basis, dataSection.sectionOffset + entry.objectOffset);
                            havokObjects.Add(newBoxShape);
                            break;
                        case "hkSimpleMeshShape":
                            hkSimpleMeshShape newSimpleMeshShape = new hkSimpleMeshShape(basis, dataSection.sectionOffset + entry.objectOffset);
                            havokObjects.Add(newSimpleMeshShape);

                            obj.Add("o object" + entry.objectOffset);

                            Console.WriteLine("vert count: " + newSimpleMeshShape.vertices.Count);

                            foreach (Vertex v in newSimpleMeshShape.vertices) {
                                obj.Add("v " + v.X + " " + v.Y + " " + v.Z);
                            }

                            foreach (hkSimpleMeshShapeTriangle f in newSimpleMeshShape.triangles)
                            {
                                obj.Add("f " + (f.v1+1 + cumulativeVertCount) + " " + (f.v2+ 1 + cumulativeVertCount) + " " + (f.v3+ 1 + cumulativeVertCount));
                            }

                            cumulativeVertCount += newSimpleMeshShape.vertices.Count;

                            break;
                        default:
                            Console.WriteLine("Unhandled havok object type: " + target.name);
                            break;
                    }
                    break;
                }
            }
        }

        public int getLocalFixUpOffset(int offset) {

            foreach (hkxDataSection.hkxLocalFixupEntry entry in dataSection.localFixupTable) {

                if (entry.destOffset == offset) {
                    return entry.srcOffset;
                }
            }

            System.Windows.Forms.MessageBox.Show("Warning: failed to find offset " + offset + " in te local fixup table");
            return -1;
        }
    }
}
