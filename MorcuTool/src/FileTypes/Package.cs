using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace MorcuTool
{
    public class Package
    {
        public Form1 form1;
        public byte[] filebytes = new byte[0];
        public string filename = "";

        public PackageType packageType = PackageType.Agents;    //defaults to agents


        public uint majorversion = 0x00;
        public uint minorversion = 0x00;
        public uint unknown1 = 0x00;
        public uint unknown2 = 0x00;
        public uint unknown3 = 0x00;
        public DateTime date = new DateTime();
        public uint indexmajorversion = 0x00;

        public uint filecount = 0x00;
        public uint indexoffsetdeprecated = 0x00;
        public uint indexsize = 0x00;
        public uint holeentrycount = 0x00;
        public uint holeoffset = 0x00;
        public uint holesize = 0x00;
        public uint indexminorversion = 0x00;
        public uint unknown4 = 0x00;
        public uint indexoffset = 0x00; //offset of the index table
        public uint unknown5 = 0x00;
        public uint unknown6 = 0x00;
        public uint reserved1 = 0x00;
        public uint reserved2 = 0x00;

        public List<IndexEntry> IndexEntries = new List<IndexEntry>();
        public ulong indexnumberofentries = 0x00;

        public List<Subfile> subfiles = new List<Subfile>();


        public enum PackageType { 
            Kingdom = 0x00,
            Agents = 0x01,
            SkyHeroes = 0x02
        }


        public uint GetNumOccurrencesOfTypeID(uint typeID)
            {
            foreach (IndexEntry entry in IndexEntries)
                {
                if (entry.typeID == typeID)
                    {
                    return entry.typeNumberOfInstances;
                    }
                }
            return 0;
            }


        public void LoadPackage()
        {

            int currenttypeindexbeingprocessed = 0;
            int instancesprocessedofcurrenttype = 0;

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                uint magic = 0;

                magic = utility.ReverseEndian(reader.ReadUInt32());

                switch (magic)
                    {
                    case 0x44425046:    //DBPF
                        packageType = PackageType.Kingdom;
                        break;

                    case 0x46504244:    //FPBD
                        packageType = PackageType.Agents;
                        break;

                    case 0x030502ED:    //Skyheroes
                    case 0x03051771:    //Skyheroes
                        packageType = PackageType.SkyHeroes;
                        reader.Close();
                        LoadSkyHeroesPackage();
                        return;

                    default:
                        MessageBox.Show("This is not a valid package!", "Package not supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Console.WriteLine("invalid file magic");
                        break;
                    }


                if (packageType == PackageType.Agents)
                {
                    majorversion = utility.ReverseEndian(reader.ReadUInt32());
                    minorversion = utility.ReverseEndian(reader.ReadUInt32());
                    unknown1 = utility.ReverseEndian(reader.ReadUInt32());
                    unknown2 = utility.ReverseEndian(reader.ReadUInt32());
                    unknown3 = utility.ReverseEndian(reader.ReadUInt32());
                    date = DateTime.FromBinary(utility.ReverseEndianLong(reader.ReadInt64()));
                    indexmajorversion = utility.ReverseEndian(reader.ReadUInt32());

                    filecount = utility.ReverseEndian(reader.ReadUInt32());
                    indexoffsetdeprecated = utility.ReverseEndian(reader.ReadUInt32());
                    indexsize = utility.ReverseEndian(reader.ReadUInt32());
                    holeentrycount = utility.ReverseEndian(reader.ReadUInt32());
                    holeoffset = utility.ReverseEndian(reader.ReadUInt32());
                    holesize = utility.ReverseEndian(reader.ReadUInt32());
                    indexminorversion = utility.ReverseEndian(reader.ReadUInt32());
                    unknown4 = utility.ReverseEndian(reader.ReadUInt32());
                    indexoffset = utility.ReverseEndian(reader.ReadUInt32());
                    unknown5 = utility.ReverseEndian(reader.ReadUInt32());
                    unknown6 = utility.ReverseEndian(reader.ReadUInt32());
                    reserved1 = utility.ReverseEndian(reader.ReadUInt32());
                    reserved2 = utility.ReverseEndian(reader.ReadUInt32());
                }
                else
                {
                    majorversion = reader.ReadUInt32();
                    minorversion = reader.ReadUInt32();
                    unknown1 = reader.ReadUInt32();
                    unknown2 = reader.ReadUInt32();
                    unknown3 = reader.ReadUInt32();
                    date = DateTime.FromBinary(utility.ReverseEndianLong(reader.ReadInt64()));
                    indexmajorversion = reader.ReadUInt32();

                    filecount = reader.ReadUInt32();
                    indexoffsetdeprecated = reader.ReadUInt32();
                    indexsize = reader.ReadUInt32();
                    holeentrycount = reader.ReadUInt32();
                    holeoffset = reader.ReadUInt32();
                    holesize = reader.ReadUInt32();
                    indexminorversion = reader.ReadUInt32();
                    indexoffset = reader.ReadUInt32();
                    unknown5 = reader.ReadUInt32();
                    unknown6 = reader.ReadUInt32();
                    reserved1 = reader.ReadUInt32();
                    reserved2 = reader.ReadUInt32();
                }

                Console.WriteLine("Package date: " + date.ToString());


                reader.BaseStream.Position = indexoffset;

                //index header

                if (packageType == PackageType.Agents)
                {
                    indexnumberofentries = utility.ReverseEndianULong(reader.ReadUInt64());

                    for (uint i = 0; i < indexnumberofentries; i++)  //a bunch of entries that describe how many files there are of each type
                    {
                        IndexEntry newEntry = new IndexEntry();

                        newEntry.typeID = utility.ReverseEndian(reader.ReadUInt32());
                        newEntry.groupID = utility.ReverseEndian(reader.ReadUInt32());
                        newEntry.typeNumberOfInstances = utility.ReverseEndian(reader.ReadUInt32());
                        newEntry.indexnulls = utility.ReverseEndian(reader.ReadUInt32());

                        IndexEntries.Add(newEntry);
                    }

                    currenttypeindexbeingprocessed = 0;
                    instancesprocessedofcurrenttype = 0;

                    for (uint i = 0; i < filecount; i++)     //go through the files, they are organised by type, one type after the other. (So X number of type A, as described above, then Y number of type B...)
                    {
                        Subfile newSubfile = new Subfile();
                        newSubfile.hash = utility.ReverseEndianULong(reader.ReadUInt64());
                        newSubfile.fileoffset = utility.ReverseEndian(reader.ReadUInt32());
                        newSubfile.filesize = utility.ReverseEndian(reader.ReadUInt32());
                        newSubfile.typeID = IndexEntries[currenttypeindexbeingprocessed].typeID;
                        newSubfile.groupID = IndexEntries[currenttypeindexbeingprocessed].groupID;

                        if (IndexEntries[currenttypeindexbeingprocessed].groupID != 0)   //it's compressed
                        {
                            newSubfile.uncompressedsize = utility.ReverseEndian(reader.ReadUInt32());
                        }

                        instancesprocessedofcurrenttype++;

                        if (instancesprocessedofcurrenttype == IndexEntries[currenttypeindexbeingprocessed].typeNumberOfInstances)
                        {
                            Console.WriteLine("Processed " + instancesprocessedofcurrenttype + " instances of " + currenttypeindexbeingprocessed);
                            currenttypeindexbeingprocessed++;
                            instancesprocessedofcurrenttype = 0;
                        }

                        subfiles.Add(newSubfile);
                    }
                }
                else     //MySims, MySims Kingdom etc, don't know if all the index versions work
                {
                    uint indexversion = reader.ReadUInt32();

                    if (indexversion == 0)  //MYSIMS USES THIS, SO YOU NEED TO IMPLEMENT IT
                    {
                        Console.WriteLine("index version 0");
                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                            newSubfile.typeID = reader.ReadUInt32();
                            newSubfile.groupID = reader.ReadUInt32();
                            newSubfile.hash = reader.ReadUInt64();
                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                            newSubfile.uncompressedsize = reader.ReadUInt32();
                            reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                    else if (indexversion == 1)
                    {
                        MessageBox.Show("Index version 1 not implemented!");

                    }
                    else if (indexversion == 2)
                    {
                        Console.WriteLine("index version 2");

                        reader.BaseStream.Position += 4;

                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                           
                            newSubfile.typeID = reader.ReadUInt32();
                            newSubfile.hash = reader.ReadUInt64();   //or might be hash
                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                            newSubfile.uncompressedsize = reader.ReadUInt32();
                            reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                    else if (indexversion == 3)
                    {
                        Console.WriteLine("index version 3");
                        uint allFilesTypeID = reader.ReadUInt32();
                        reader.BaseStream.Position += 4;

                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                            newSubfile.typeID = allFilesTypeID;
                            newSubfile.hash = reader.ReadUInt64();   //or might be hash
                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                            newSubfile.uncompressedsize = reader.ReadUInt32();
                            reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                }

                //extract files

                Console.WriteLine("filecount " + filecount);

                int filesprocessed = 0;


                List<string> luaFilenamesForDict = new List<string>();

                bool containslua = false;

                for (int i = 0; i < filecount; i++)
                {
                    {
                        string fileextension = null;

                        switch (subfiles[i].typeID)
                        {
                            case 0x2954E734:          //RMDL MSA     
                                fileextension = ".rmdl";        //TYPE ID 29 54 E7 34
                                break;

                            case 0xF9E50586:          //RMDL MSK     
                                fileextension = ".rmdl";        //TYPE ID 29 54 E7 34
                                break;

                            case 0xE6640542:          //MATD MSA    
                                fileextension = ".matd";        //TYPE ID E6 64 05 42
                                break;

                            case 0x01D0E75D:          //MATD MSK    
                                fileextension = ".matd";
                                break;

                            case 0x92AA4D6A:         //altered TPL MSA
                                fileextension = ".tpl";         //TYPE ID 92 AA 4D 6A
                                break;

                            case 0x00B2D882:         //altered TPL MSK
                                fileextension = ".tpl";
                                break;

                            case 0x787E842A:         //MTST Material Set MSA
                                fileextension = ".mtst";         //TYPE ID 78 7E 84 2A
                                break;

                            case 0x02019972:      //MTST MSK
                                fileextension = ".mtst";
                                break;

                            case 0x0EFC1A82:         //FPST   footprint set.      contains a model footprint (ftpt) which is documented at http://simswiki.info/wiki.php?title=Sims_3:PackedFileTypes
                                fileextension = ".fpst";
                                break;

                            case 0x2199BB60:        //BNKb    big endian BNK    MSA                             vgmstream can decode these.           https://github.com/losnoco/vgmstream/blob/master/src/meta/ea_schl.c  
                                fileextension = ".bnk";        //TYPE ID 21 99 BB 60
                                break;

                            case 0xB6B5C271:        //BNKb    BNK    MSK (idk which endian, not tested)                             vgmstream can decode these.           https://github.com/losnoco/vgmstream/blob/master/src/meta/ea_schl.c  
                                fileextension = ".bnk";
                                break;

                            case 0x2699C28D:        //BIGF
                                fileextension = ".big";
                                break;

                            case 0x1A8FEB14:       //00 00 00 02             There's another, separate filetype that also begins with the 2 magic, but that one doesn't appear as frequently, so this one here is probably the collision type ID
                                                   //TYPE ID 1A 8F EB 14.  
                                fileextension = ".collision";               //mesh collision
                                break;

                            case 0x6B772503:       //FX
                                fileextension = ".fx";
                                break;

                            case 0x3681D75B:        //LUA MSA
                                fileextension = ".lua";
                                containslua = true;
                                break;

                            case 0x2B8E2411:         //LUA MSK
                                fileextension = ".lua";
                                containslua = true;
                                break;

                            case 0x2EF1E401:     //SLOT MSA
                                fileextension = ".slot";
                                break;

                            case 0x487BF9E4:     //SLOT MSK
                                fileextension = ".slot";
                                break;

                            case 0x28707864:       //particles file
                                fileextension = ".particles";           //TYPE ID 28 70 78 64
                                break;

                            case 0x9614D3C0:       //00 00 00 01      
                                fileextension = ".1";
                                break;

                            case 0x8FC0DE5A:       //00 00 00 02            bounding box collision (for very simple objects)
                                fileextension = ".2";
                                break;

                            case 0x5027B4EC:       //00 00 00 03            slightly more complex bounding box collision (includes position and rotation?)
                                fileextension = ".3";
                                break;

                            case 0x41C4A8EF:       //00 00 00 03        buildable region
                                fileextension = ".buildableregion";
                                break;

                            case 0xA5DCD485:                     //LLMF MSA
                                fileextension = ".llmf";
                                break;

                            case 0x58969018:                     //LLMF MSK
                                fileextension = ".llmf";
                                break;

                            case 0x4672E5BD:    //RIG MSA  
                                fileextension = ".grannyrig";             //TYPE ID 46 72 E5 BD
                                break;

                            case 0x8EAF13DE:    //RIG MSK
                                fileextension = ".grannyrig";
                                break;

                            case 0xD6BEDA43:    //ANIMATION MSA
                                fileextension = ".animation";             //TYPE ID D6 BE DA 43
                                break;

                            case 0x6B20C4F3:    //ANIMATION MSK
                                fileextension = ".animation";
                                break;

                            case 0xE55D5715:
                                fileextension = ".ltst";    //possibly lighting set?
                                break;

                            case 0x276CA4B9:         //TrueType font 
                                fileextension = ".ttf";                      //TYPE ID 27 6C A4 B9
                                break;

                            case 0xD5988020:    //MSK PHYS
                                fileextension = ".phys";
                                break;

                            default:
                                Console.WriteLine("Unknown type ID " + subfiles[i].typeID);
                                Console.WriteLine("and this type ID appears " + GetNumOccurrencesOfTypeID(subfiles[i].typeID) + " times in total.");
                                Console.WriteLine("index of file was " + filesprocessed);
                                fileextension = subfiles[i].typeID.ToString();
                                break;
                        }

                        subfiles[i].fileextension = fileextension;


                        Vault.luaString typeIDRealString = global.activeVault.GetLuaStringWithHash(subfiles[i].typeID);

                        //if (typeIDRealString != null)
                        //   {
                        //   fileextension = "." + typeIDRealString.name;
                        //   }



                        byte[] newfilenameasbytes = BitConverter.GetBytes(utility.ReverseEndianULong(subfiles[i].hash));

                        subfiles[i].filename = "0x";

                        ulong newfilenameasulong = utility.ReverseEndianULong(subfiles[i].hash);


                        if (global.activeVault.VaultHashesAndFileNames.Keys.Contains(subfiles[i].hash))
                        {
                            subfiles[i].filename = global.activeVault.VaultHashesAndFileNames[subfiles[i].hash]; 
                        }
                        else
                        {
                            subfiles[i].filename += BitConverter.ToString(newfilenameasbytes).Replace("-", "");
                        }

                        subfiles[i].filename += fileextension;

                        /*
                        if (fileextension == ".lua") //temp
                            {
                            string luaName = "";

                            int currentOffset = 0x10;

                            while (newfile[currentOffset] != 0x00)
                                {
                                currentOffset++;
                                }

                            while (newfile[currentOffset] != 0x2F)
                            {
                                currentOffset--;
                            }

                            currentOffset++;

                            while (newfile[currentOffset] != 0x2E)
                                {
                                luaName += ((char)newfile[currentOffset]) + "";
                                currentOffset++;
                                }

                            luaFilenamesForDict.Add("VaultHashesAndFileNames.Add("+newfilename+",\""+luaName+"\");");
                            }*/

                        filesprocessed++;
                    }
                }
                form1.MakeFileTree();

                if (containslua && global.activeVault.luaStrings.Count > 0)
                {
                    string[] luaStringsForExport = new string[global.activeVault.luaStrings.Count];

                    for (int i = 0; i < global.activeVault.luaStrings.Count; i++)
                    {
                        luaStringsForExport[i] = BitConverter.ToString(BitConverter.GetBytes(utility.ReverseEndian(global.activeVault.luaStrings[i].hash))).Replace("-", ""); ;
                        luaStringsForExport[i] += " " + global.activeVault.luaStrings[i].name;
                    }

                    File.WriteAllLines("global.activeVault.luaStrings.lua", luaStringsForExport);
                }

                File.WriteAllLines("dict.txt", luaFilenamesForDict);
                MessageBox.Show("Processed " + filesprocessed + " files (out of a total " + filecount + ").", "Task complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

    


    public void LoadSkyHeroesPackage()
    {
            DialogResult result = MessageBox.Show("This operation will dump the files to the same directory as the input file. Proceed?","Extract files?",MessageBoxButtons.YesNo,MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                {
                return;
                }

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {

                uint sig = utility.ReverseEndian(reader.ReadUInt32());

                if (sig != 0x030502ED && sig != 0x03051771)
                {
                    Console.WriteLine("invalid signature");
                    return;
                }

                uint fileversion = utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position += 0x04;

                ushort unknown1 = utility.ReverseEndianShort(reader.ReadUInt16());
                ushort numberoffiles = utility.ReverseEndianShort(reader.ReadUInt16());     //maybe???

                reader.BaseStream.Position += 0x30;

                Dictionary<uint, uint> chunklist = new Dictionary<uint, uint>();  //counts and offsets. count is how many 8 byte entries there are. offset is where the first one starts from.


                for (uint i = 0; i < 110; i++)
                {
                    uint count = utility.ReverseEndian(reader.ReadUInt32());
                    uint offset = utility.ReverseEndian(reader.ReadUInt32());

                    if (offset != 0xFFFFFFFF)
                    {
                        chunklist.Add(offset, count);

                    }
                }


                Dictionary<uint, uint> IDsandoffsets = new Dictionary<uint, uint>();
                List<uint> offsets = new List<uint>();

                foreach (uint offset in chunklist.Keys)
                {
                    reader.BaseStream.Position = offset;  //go to offset where chunk begins

                    for (int i = 0; i < chunklist[offset]; i++)     //for each item in the chunk
                    {
                        uint fileID = utility.ReverseEndian(reader.ReadUInt32());
                        uint fileoffset = utility.ReverseEndian(reader.ReadUInt32());

                        IDsandoffsets.Add(fileID, fileoffset + 0x10);  //and the ID and offset of the file to the dictionary
                        offsets.Add(fileoffset + 0x10); // just so that we can reference which one comes after etc.
                    }
                }

                //extract files

                foreach (uint ID in IDsandoffsets.Keys)
                {
                    List<Byte> newfile = new List<Byte>();


                    if (offsets.IndexOf(IDsandoffsets[ID]) != offsets.Count - 1)   //if it's not the last file
                    {
                        uint numberofbytestoread = (offsets[offsets.IndexOf(IDsandoffsets[ID]) + 1] - IDsandoffsets[ID]) - 0x10;

                        reader.BaseStream.Position = IDsandoffsets[ID];

                        for (int i = 0; i < numberofbytestoread; i++)     //add bytes to the file
                        {
                            newfile.Add(reader.ReadByte());
                        }

                        Byte[] filetype = new Byte[2];

                        Byte[] IDasbytearray = BitConverter.GetBytes(ID);

                        Array.Reverse(IDasbytearray, 0, IDasbytearray.Length);

                        Array.Copy(IDasbytearray, 0, filetype, 0, 2);


                        switch (filetype[0])
                        {
                            case 0x22:
                                newfile = imageTools.ConvertToTPL(filename + ID.ToString(), newfile.ToArray());
                                File.WriteAllBytes(filename + ID.ToString() + ".tpl", newfile.ToArray());
                                break;
                            case 0x28:
                                File.WriteAllBytes(filename + ID.ToString() + ".proxy", newfile.ToArray());
                                break;
                            case 0x29:
                                File.WriteAllBytes(filename + ID.ToString() + ".animdata", newfile.ToArray());
                                break;
                            case 0x45:
                                File.WriteAllBytes(filename + ID.ToString() + ".fx", newfile.ToArray());
                                break;
                            case 0x5A:
                                File.WriteAllBytes(filename + ID.ToString() + ".snddata", newfile.ToArray());
                                break;
                            case 0x5B:
                                //newfile = ConvertSkyHeroesModel(file + ID.ToString(), newfile.ToArray());
                                File.WriteAllBytes(filename + ID.ToString() + ".mdl", newfile.ToArray());
                                break;
                            case 0x63:
                                File.WriteAllBytes(filename + ID.ToString() + ".godinfo", newfile.ToArray());
                                break;
                            default:
                                File.WriteAllBytes(filename + ID.ToString() + "_" + filetype, newfile.ToArray());
                                break;
                        }
                    }
                }

                //0x2246  = texture type?
                //0x2846  = proxies
                //0x2946  = animation data/pointers?
                //0x4546  = effect file
                //0x5A46  = sound effect data/pointers
                //0x5B46  = model???
                //0x6346  = godinfo
            }
            
            MessageBox.Show("Files extracted. They are in the same folder as the original archive.", "Extraction complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class IndexEntry
    {

        public uint typeID = 0;
        public uint groupID = 0;
        public uint typeNumberOfInstances = 0;
        public uint indexnulls = 0;
    }
}
