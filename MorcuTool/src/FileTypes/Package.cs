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

        uint MSKindexversion = 0x00;

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

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)))
            {
                uint magic = 0;

                magic = Utility.ReverseEndian(reader.ReadUInt32());

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
                    majorversion = Utility.ReverseEndian(reader.ReadUInt32());
                    minorversion = Utility.ReverseEndian(reader.ReadUInt32());
                    unknown1 = Utility.ReverseEndian(reader.ReadUInt32());
                    unknown2 = Utility.ReverseEndian(reader.ReadUInt32());
                    unknown3 = Utility.ReverseEndian(reader.ReadUInt32());
                    date = DateTime.FromBinary(Utility.ReverseEndianLong(reader.ReadInt64()));
                    indexmajorversion = Utility.ReverseEndian(reader.ReadUInt32());

                    filecount = Utility.ReverseEndian(reader.ReadUInt32());
                    indexoffsetdeprecated = Utility.ReverseEndian(reader.ReadUInt32());
                    indexsize = Utility.ReverseEndian(reader.ReadUInt32());
                    holeentrycount = Utility.ReverseEndian(reader.ReadUInt32());
                    holeoffset = Utility.ReverseEndian(reader.ReadUInt32());
                    holesize = Utility.ReverseEndian(reader.ReadUInt32());
                    indexminorversion = Utility.ReverseEndian(reader.ReadUInt32());
                    unknown4 = Utility.ReverseEndian(reader.ReadUInt32());
                    indexoffset = Utility.ReverseEndian(reader.ReadUInt32());
                    unknown5 = Utility.ReverseEndian(reader.ReadUInt32());
                    unknown6 = Utility.ReverseEndian(reader.ReadUInt32());
                    reserved1 = Utility.ReverseEndian(reader.ReadUInt32());
                    reserved2 = Utility.ReverseEndian(reader.ReadUInt32());
                }
                else
                {
                    majorversion = reader.ReadUInt32();
                    minorversion = reader.ReadUInt32();
                    unknown1 = reader.ReadUInt32();
                    unknown2 = reader.ReadUInt32();
                    unknown3 = reader.ReadUInt32();
                    date = DateTime.FromBinary(Utility.ReverseEndianLong(reader.ReadInt64()));
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
                    indexnumberofentries = Utility.ReverseEndianULong(reader.ReadUInt64());

                    for (uint i = 0; i < indexnumberofentries; i++)  //a bunch of entries that describe how many files there are of each type
                    {
                        IndexEntry newEntry = new IndexEntry();

                        newEntry.typeID = Utility.ReverseEndian(reader.ReadUInt32());
                        newEntry.groupID = Utility.ReverseEndian(reader.ReadUInt32());
                        newEntry.typeNumberOfInstances = Utility.ReverseEndian(reader.ReadUInt32());
                        newEntry.indexnulls = Utility.ReverseEndian(reader.ReadUInt32());

                        IndexEntries.Add(newEntry);
                    }

                    currenttypeindexbeingprocessed = 0;
                    instancesprocessedofcurrenttype = 0;

                    for (uint i = 0; i < filecount; i++)     //go through the files, they are organised by type, one type after the other. (So X number of type A, as described above, then Y number of type B...)
                    {
                        Subfile newSubfile = new Subfile();
                        newSubfile.hash = Utility.ReverseEndianULong(reader.ReadUInt64());
                        newSubfile.fileoffset = Utility.ReverseEndian(reader.ReadUInt32());
                        newSubfile.filesize = Utility.ReverseEndian(reader.ReadUInt32());
                        newSubfile.typeID = IndexEntries[currenttypeindexbeingprocessed].typeID;
                        newSubfile.groupID = IndexEntries[currenttypeindexbeingprocessed].groupID;

                        if (IndexEntries[currenttypeindexbeingprocessed].groupID != 0)   //it's compressed
                        {
                            newSubfile.should_be_compressed_when_in_package = true;
                            newSubfile.uncompressedsize = Utility.ReverseEndian(reader.ReadUInt32());
                            Console.WriteLine("Found a compressed file");
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
                else     //MySims and MySims Kingdom use these
                {
                    MSKindexversion = reader.ReadUInt32();

                    if (MSKindexversion == 0)  
                    {
                        Console.WriteLine("index version 0");
                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                            newSubfile.typeID = reader.ReadUInt32();
                            newSubfile.groupID = reader.ReadUInt32();
                            newSubfile.hash = (ulong)(reader.ReadUInt32()) << 32;
                            newSubfile.hash |= (ulong)(reader.ReadUInt32());

                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                            newSubfile.uncompressedsize = reader.ReadUInt32();
                            reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }
                            else {
                                newSubfile.should_be_compressed_when_in_package = true;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                    else if (MSKindexversion == 1)
                    {
                        MessageBox.Show("Index version 1 not implemented!");

                    }
                    else if (MSKindexversion == 2)
                    {
                        Console.WriteLine("index version 2");

                        reader.BaseStream.Position += 4;

                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                            newSubfile.typeID = reader.ReadUInt32();
                            newSubfile.hash = (ulong)(reader.ReadUInt32()) << 32;
                            newSubfile.hash |= (ulong)(reader.ReadUInt32());

                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                            newSubfile.uncompressedsize = reader.ReadUInt32();
                            reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }
                            else
                            {
                                newSubfile.should_be_compressed_when_in_package = true;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                    else if (MSKindexversion == 3)
                    {
                        Console.WriteLine("index version 3");
                        uint allFilesTypeID = reader.ReadUInt32();
                        reader.BaseStream.Position += 4;

                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                            newSubfile.typeID = allFilesTypeID;
                            newSubfile.hash = (ulong)(reader.ReadUInt32()) << 32;
                            newSubfile.hash |= (ulong)(reader.ReadUInt32());

                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                            newSubfile.uncompressedsize = reader.ReadUInt32();
                            reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }
                            else
                            {
                                newSubfile.should_be_compressed_when_in_package = true;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                    else if (MSKindexversion == 4)  
                    {
                        Console.WriteLine("index version 4");
                        reader.BaseStream.Position += 4;
                       
                        for (uint i = 0; i < filecount; i++)
                        {
                            Subfile newSubfile = new Subfile();

                            newSubfile.typeID = reader.ReadUInt32();
                            newSubfile.hash = (ulong)(reader.ReadUInt32()) << 32;
                            newSubfile.hash |= (ulong)(reader.ReadUInt32());

                            newSubfile.fileoffset = reader.ReadUInt32();
                            newSubfile.filesize = reader.ReadUInt32();
                            newSubfile.uncompressedsize = reader.ReadUInt32();

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }
                            else
                            {
                                newSubfile.should_be_compressed_when_in_package = true;
                            }

                            subfiles.Add(newSubfile);
                        }
                    }
                    else if (MSKindexversion == 7)
                        {
                            Console.WriteLine("index version 7");
                            for (uint i = 0; i < filecount; i++)
                            {
                                Subfile newSubfile = new Subfile();

                                newSubfile.typeID = reader.ReadUInt32();
                                newSubfile.groupID = reader.ReadUInt32();
                                newSubfile.hash = (ulong)(reader.ReadUInt32()) << 32;
                                newSubfile.hash |= (ulong)(reader.ReadUInt32());

                                newSubfile.fileoffset = reader.ReadUInt32();
                                newSubfile.filesize = reader.ReadUInt32() & 0x7FFFFFFF;
                                newSubfile.uncompressedsize = reader.ReadUInt32();
                                reader.BaseStream.Position += 0x04; //flags

                            if (newSubfile.filesize == newSubfile.uncompressedsize)
                            {
                                newSubfile.uncompressedsize = 0;
                            }
                            else
                            {
                                newSubfile.should_be_compressed_when_in_package = true;
                            }

                            subfiles.Add(newSubfile);
                            }
                        }
                        else
                        {
                        MessageBox.Show("Unknown index version: "+ MSKindexversion);
                        return;
                        }
                }


                //extract files

                Console.WriteLine("filecount " + filecount);

                int filesprocessed = 0;


                List<string> luaFilenamesForDict = new List<string>();

                bool containslua = false;

                for (int i = 0; i < filecount; i++)
                {

                    if (subfiles[i].should_be_compressed_when_in_package)
                        {
                        subfiles[i].has_been_decompressed = false;    //set default state of a compressed file to compressed
                        }

                    {
                        string fileextension = null;

                        switch ((global.TypeID)subfiles[i].typeID)
                        {
                            case global.TypeID.RMDL_MSA:          //RMDL MSA     
                                fileextension = ".rmdl";        //TYPE ID 29 54 E7 34
                                break;

                            case global.TypeID.RMDL_MSK:          //RMDL MSK     
                                fileextension = ".rmdl";        //          "ModelData"
                                break;

                            case global.TypeID.MATD_MSA:          //MATD MSA    
                                fileextension = ".matd";        //TYPE ID E6 64 05 42
                                break;

                            case global.TypeID.MATD_MSK:          //MATD MSK            "MaterialData"
                                fileextension = ".matd";
                                break;

                            case global.TypeID.TPL_MSA:         //altered TPL MSA
                                fileextension = ".tpl";         //TYPE ID 92 AA 4D 6A
                                break;

                            case global.TypeID.TPL_MSK:         //altered TPL MSK      "TextureData"
                                fileextension = ".tpl";
                                break;

                            case global.TypeID.MTST_MSK:      //MTST Material Set MSK       "MaterialSetData"
                                fileextension = ".mtst";
                                break;

                            case global.TypeID.MTST_MSA:         //MTST Material Set MSA
                                fileextension = ".mtst";         //TYPE ID 78 7E 84 2A
                                break;

                            case global.TypeID.FPST_MSK:         //FPST   MySims Kingdom footprint set.    "FootprintData"    contains a model footprint (ftpt) which is possibly documented at http://simswiki.info/wiki.php?title=Sims_3:PackedFileTypes
                            case global.TypeID.FPST_MSA:         //FPST   MySims Agents footprint set.                        contains a model footprint (ftpt) which is possibly documented at http://simswiki.info/wiki.php?title=Sims_3:PackedFileTypes
                                fileextension = ".fpst";        //It's like a heatmap of where sims can walk? Or perhaps which surfaces should generate which kind of footprint (but wouldn't that mean that overhangs wouldn't work?)
                                break;

                            case global.TypeID.BNK_MSA:        //BNKb    big endian BNK    MSA                             vgmstream can decode these.           https://github.com/losnoco/vgmstream/blob/master/src/meta/ea_schl.c  
                                fileextension = ".bnk";        //TYPE ID 21 99 BB 60
                                break;

                            case global.TypeID.BNK_MSK:        //BNKb    BNK    MSK (idk which endian, not tested)       "AudioData"             vgmstream can decode these.           https://github.com/losnoco/vgmstream/blob/master/src/meta/ea_schl.c  
                                fileextension = ".bnk";                                         
                                break;

                            case global.TypeID.BIG_MSK:    //big   
                                fileextension = ".big";         //"AptData" (but it is, of course, a familiar EA .BIG archive)
                                break;

                            case global.TypeID.BIG_MSA:        //BIGF
                                fileextension = ".big";
                                break;

                            case global.TypeID.COLLISION_MSA:       //00 00 00 02             There's another, separate filetype that also begins with the 2 magic, but that one doesn't appear as frequently, so this one here is probably the collision type ID
                                                   //TYPE ID 1A 8F EB 14.  
                                fileextension = ".collision";               //mesh collision
                                break;                                  //there are mentions of 'rwphysics' in MSA's main.dol; could this mean Renderware Physics, which was indeed a product available at the time?

                            case global.TypeID.FX:       //FX
                                fileextension = ".fx";
                                break;

                            case global.TypeID.LUAC_MSA:        //LUAC MSA
                                fileextension = ".luac";
                                containslua = true;
                                break;

                            case global.TypeID.LUAC_MSK:         //LUAC MSK  "LuaObjectData"
                                fileextension = ".luac";
                                containslua = true;
                                break;

                            case global.TypeID.SLOT_MSA:     //SLOT MSA
                                fileextension = ".slot";
                                break;

                            case global.TypeID.SLOT_MSK:     //SLOT MSK
                                fileextension = ".slot";                //"SlotData"
                                break;

                            case global.TypeID.PARTICLES_MSA:       //particles file
                                fileextension = ".particles";           //TYPE ID 28 70 78 64
                                break;

                            case global.TypeID.BUILDABLEREGION_MSA:       //00 00 00 03        buildable region MSA
                                fileextension = ".buildableregion";
                                break;

                            case global.TypeID.BUILDABLEREGION_MSK:       //buildable region MSK        "BuildableRegionData"
                                fileextension = ".buildableregion";        
                                break;

                            case global.TypeID.LLMF_MSA:                     //LLMF level bin MSA
                                fileextension = ".llmf";
                                break;

                            case global.TypeID.LLMF_MSK:                     //LLMF level bin MSK   "LevelData"
                                fileextension = ".llmf";
                                break;

                            case global.TypeID.RIG_MSA:    //RIG MSA                   //Interesting granny struct info at 0x49CFDD in MSA's main.dol
                                fileextension = ".grannyrig";             //TYPE ID 46 72 E5 BD
                                break;

                            case global.TypeID.RIG_MSK:    //RIG MSK
                                fileextension = ".grannyrig";           // "RigData"
                                break;

                            case global.TypeID.ANIMCLIP_MSA:    //ANIMATION MSA
                                fileextension = ".clip";             //TYPE ID D6 BE DA 43
                                break;

                            case global.TypeID.ANIMCLIP_MSK:    //ANIMATION MSK         "ClipData"
                                fileextension = ".clip";
                                break;

                            case global.TypeID.LTST_MSA:
                                fileextension = ".ltst";    //possibly lighting set?
                                break;

                            case global.TypeID.TTF_MSK:         //TrueType font MySims Kingdom
                            case global.TypeID.TTF_MSA:         //TrueType font MySims Agents
                                fileextension = ".ttf";                      //TYPE ID 27 6C A4 B9
                                break;

                            case global.TypeID.HKX_MSK:    //MSK HKX havok collision file
                                fileextension = ".hkx";              // "PhysicsData" 
                                break;

                            case global.TypeID.OGVD_MSK:
                                fileextension = ".objectGridVolumeData";   //MSK "ObjectGridVolumeData"
                                break;

                            case global.TypeID.OGVD_MSA:       //00 00 00 02           MSA ObjectGridVolumeData bounding box collision (for very simple objects)
                                fileextension = ".objectGridVolumeData";
                                break;

                            case global.TypeID.SPD_MSK:     
                                fileextension = ".snapPointData";   //MSK "SnapPointData"
                                break;

                            case global.TypeID.SPD_MSA:       //00 00 00 03          MSA SnapPointData, most likely
                                fileextension = ".snapPointData";
                                break;

                            case global.TypeID.VGD_MSK:
                                fileextension = ".voxelGridData";   //MSK "VoxelGridData"
                                break;

                            case global.TypeID.VGD_MSA:       //00 00 00 01      MSA VoxelGridData, most likely, most likely
                                fileextension = ".voxelGridData";
                                break;

                            case global.TypeID.MODEL_MS:   //model          used by MySims, not the same as rmdl
                                fileextension = ".model";
                                break;

                            case global.TypeID.KEYNAMEMAP_MS:
                                fileextension = ".KeyNameMap";
                                break;

                            case global.TypeID.GEOMETRY_MS:
                                fileextension = ".geometry";
                                break;

                            case global.TypeID.OLDSPEEDTREE_MS:
                                fileextension = ".oldSpeedTree";
                                break;

                            case global.TypeID.SPEEDTREE_MS:
                                fileextension = ".speedTree";
                                break;

                            case global.TypeID.COMPOSITETEXTURE_MS:
                                fileextension = ".compositeTexture";
                                break;

                            case global.TypeID.SIMOUTFIT_MS:
                                fileextension = ".simOutfit";
                                break;

                            case global.TypeID.LEVELXML_MS:
                                fileextension = ".levelXml";
                                break;

                            case global.TypeID.LUA_MSK:    //uncompiled lua script "LuaTextData"
                                fileextension = ".lua";     
                                break;

                            case global.TypeID.LIGHTSETXML_MS:    //Light set XML MySims
                                fileextension = ".lightSetXml";
                                break;

                            case global.TypeID.LIGHTSETBIN_MSK:    //Light set bin MySims
                                fileextension = ".lightSetBin";                 //Named "LightSetData" in MSK's executable
                                break;

                            case global.TypeID.XML_MS:   //xml
                                fileextension = ".xml";
                                break;

                            case global.TypeID.FPST_MS:    //footprint set MySims
                                fileextension = ".footprintSet";
                                break;

                            case global.TypeID.OBJECTCONSTRUCTIONXML_MS:    //object construction xml
                                fileextension = ".objectConstructionXml";
                                break;

                            case global.TypeID.OBJECTCONSTRUCTIONBIN_MS:    //object construction bin
                                fileextension = ".objectConstructionBin";
                                break;

                            case global.TypeID.SLOTXML_MS:    //slot xml
                                fileextension = ".slotXml";
                                break;

                            case global.TypeID.SWARM_MSK:    //swm
                                fileextension = ".swm";         //MSK           "SwarmData" 
                                break;

                            case global.TypeID.SWARM_MS:    //SwarmBin
                                fileextension = ".SwarmBin";
                                break;

                            case global.TypeID.XMLBIN_MS:    //XmlBin
                                fileextension = ".XmlBin";
                                break;

                            case global.TypeID.CABXML_MS:    //CABXml
                                fileextension = ".CABXml";
                                break;

                            case global.TypeID.CABBIN_MS:    //CABBin
                                fileextension = ".CABBin";
                                break;

                            case global.TypeID.LIGHTBOXXML_MS:  //LightBoxXml
                                fileextension = ".lightBoxXml";
                                break;

                            case global.TypeID.LIGHTBOXBIN_MS:  //LightBoxBin
                                fileextension = ".lightBoxBin";     //MSK LightBoxData (Named in MSK's executable, though I think I found it in MySims?)
                                break;

                            case global.TypeID.XMB_MS:  //xmb
                                fileextension = ".xmb";
                                break;

                            default:
                                Console.WriteLine("Unknown type ID " + subfiles[i].typeID);
                                Console.WriteLine("and this type ID appears " + GetNumOccurrencesOfTypeID(subfiles[i].typeID) + " times in total.");
                                Console.WriteLine("index of file was " + filesprocessed);
                                fileextension = "."+subfiles[i].typeID.ToString();
                                break;
                        }

                        subfiles[i].fileextension = fileextension;



                        Vault.luaString typeIDRealString = global.activeVault.GetLuaStringWithHash(subfiles[i].typeID);

                        //if (typeIDRealString != null)
                        //   {
                        //   fileextension = "." + typeIDRealString.name;
                        //   }



                        byte[] newfilenameasbytes = BitConverter.GetBytes(Utility.ReverseEndianULong(subfiles[i].hash));

                        subfiles[i].filename = "0x";

                        ulong newfilenameasulong = Utility.ReverseEndianULong(subfiles[i].hash);

                        subfiles[i].hashString = "0x" + BitConverter.ToString(newfilenameasbytes).Replace("-", "");

                        if (global.activeVault.VaultHashesAndFileNames.Keys.Contains(subfiles[i].hash))
                        {
                            subfiles[i].filename = global.activeVault.VaultHashesAndFileNames[subfiles[i].hash]; 
                        }
                        else
                        {
                            subfiles[i].filename = subfiles[i].hashString;
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
                        luaStringsForExport[i] = BitConverter.ToString(BitConverter.GetBytes(Utility.ReverseEndian(global.activeVault.luaStrings[i].hash))).Replace("-", ""); ;
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

                uint sig = Utility.ReverseEndian(reader.ReadUInt32());

                if (sig != 0x030502ED && sig != 0x03051771)
                {
                    Console.WriteLine("invalid signature");
                    return;
                }

                uint fileversion = Utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position += 0x04;

                ushort unknown1 = Utility.ReverseEndianShort(reader.ReadUInt16());
                ushort numberoffiles = Utility.ReverseEndianShort(reader.ReadUInt16());     //maybe???

                reader.BaseStream.Position += 0x30;

                Dictionary<uint, uint> chunklist = new Dictionary<uint, uint>();  //counts and offsets. count is how many 8 byte entries there are. offset is where the first one starts from.


                for (uint i = 0; i < 110; i++)
                {
                    uint count = Utility.ReverseEndian(reader.ReadUInt32());
                    uint offset = Utility.ReverseEndian(reader.ReadUInt32());

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
                        uint fileID = Utility.ReverseEndian(reader.ReadUInt32());
                        uint fileoffset = Utility.ReverseEndian(reader.ReadUInt32());

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
                                newfile = imageTools.ConvertToNintendoTPL(filename + ID.ToString(), newfile.ToArray());
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


        public Subfile FindFileByHashAndTypeID(ulong hash, uint typeID) {

            foreach (Subfile s in global.activePackage.subfiles)
            {
                if (s.hash == hash && s.typeID == typeID){
                    return s;
                }
            }

            Console.WriteLine("File with hash " + hash + " and type ID " + typeID + " not found...");
            return null;
        }



        public uint ReverseEndianIfNeeded(uint input) { 
        if (packageType == PackageType.Agents)
            {
            input = Utility.ReverseEndian(input);
            }
            return input;
        }


        public void RebuildPackage()
        {
            List<byte> output = new List<byte>();

            uint packageVersion = 0;

            if (packageType == PackageType.Kingdom)
                {
                output.Add((byte)'D');
                output.Add((byte)'B');
                output.Add((byte)'P');
                output.Add((byte)'F');
                packageVersion = 2;
                }
            else if(packageType == PackageType.Agents)
                {
                output.Add((byte)'F');
                output.Add((byte)'P');
                output.Add((byte)'B');
                output.Add((byte)'D');
                packageVersion = 3;
                }

            //offset 0x04

            Utility.AddUIntToList(output, ReverseEndianIfNeeded(packageVersion));

            //offset 0x08

            for (int i = 0; i < 0x10; i++)
                {
                output.Add(0x00);
                }

            //offset 0x18

            if (packageType == PackageType.Kingdom)
                {
                for (int i = 0; i < 8; i++)
                    {
                    output.Add(0x00);   //pad
                    }
                }
            else if (packageType == PackageType.Agents)
                {
                Utility.AddLongToList(output, Utility.ReverseEndianLong(DateTime.Now.ToBinary()));
                }

            Utility.AddUIntToList(output, ReverseEndianIfNeeded(indexmajorversion));

            Utility.AddUIntToList(output, ReverseEndianIfNeeded((uint)subfiles.Count));

            //offset 0x28

            if (packageType == PackageType.Kingdom)
                {
                for (int i = 0; i < 4; i++)
                    {
                    output.Add(0x00);   //pad
                    }
                }
            else if (packageType == PackageType.Agents)
                {
                Utility.AddUIntToList(output, ReverseEndianIfNeeded(indexoffset));       //this will be returned to later once we know what it is
                }

            Utility.AddUIntToList(output, ReverseEndianIfNeeded(indexsize));        //this will be returned to later once we know what it is
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(holeentrycount));
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(holeoffset));
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(holesize));
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(indexminorversion));

            if (packageType == PackageType.Agents)
                {
                Utility.AddUIntToList(output, ReverseEndianIfNeeded(unknown4));
                }

            Utility.AddUIntToList(output, ReverseEndianIfNeeded(indexoffset));   //this will be returned to later once we know what it is
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(unknown5));
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(unknown6));
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(reserved1));
            Utility.AddUIntToList(output, ReverseEndianIfNeeded(reserved2));

            while (output.Count < 0x60)
                {
                output.Add(0x00);
                }

            //sort by type ID, and within a type ID, by hash
            subfiles = subfiles.OrderBy(s => s.typeID).ThenBy(s => s.hash).ToList();

            List <IndexEntry> indexEntriesForWriting = new List<IndexEntry>();
            List<uint> TypeIDsThatRequireCompression = new List<uint>();

            int[] subfileOffsets = new int[subfiles.Count];

            for(int f = 0; f < subfiles.Count; f++)
                {
                subfileOffsets[f] = output.Count;

                bool typeIDhasIndexEntry = false;

                foreach (IndexEntry entry in indexEntriesForWriting)
                    {
                    if(entry.typeID == subfiles[f].typeID)
                        {
                        entry.typeNumberOfInstances++;
                        typeIDhasIndexEntry = true;
                        break;
                        }
                    }

                if (!typeIDhasIndexEntry)
                    {
                    IndexEntry newIndexEntry = new IndexEntry();
                    newIndexEntry.typeID = subfiles[f].typeID;

                    if (subfiles[f].should_be_compressed_when_in_package)
                        {
                        newIndexEntry.groupID = 2;
                    }
                    else
                        {
                        newIndexEntry.groupID = 0;
                        }

                    newIndexEntry.indexnulls = 0;
                    newIndexEntry.typeNumberOfInstances = 1;

                    if(newIndexEntry.groupID != 0)
                        {
                        TypeIDsThatRequireCompression.Add(newIndexEntry.typeID);
                        }

                    indexEntriesForWriting.Add(newIndexEntry);
                    }


                if (subfiles[f].should_be_compressed_when_in_package)
                    {
                    if (subfiles[f].has_been_decompressed)  //if it was decompressed by the user then compress it
                        {                        
                        subfiles[f].filebytes = Utility.Compress_QFS(subfiles[f].filebytes);
                        }
                    }

                if (subfiles[f].filebytes == null || subfiles[f].filebytes.Length == 0) //then the file was not modified or read, so transfer it directly from the old package
                    {
                    for (int i = 0; i < subfiles[f].filesize; i++)
                        {
                        output.Add(filebytes[subfiles[f].fileoffset + i]);
                        }
                    }
                else //if it was modified or read, use the bytes from its filebytes array
                    {
                    subfiles[f].filesize = (uint)subfiles[f].filebytes.Length;
                    Console.WriteLine("LENGTH BEING ADDED IS: "+subfiles[f].filesize);
                    for (int i = 0; i < subfiles[f].filebytes.Length; i++)
                        {
                        output.Add(subfiles[f].filebytes[i]);
                        }
                    }

                while(output.Count % 0x20 != 0)
                    {
                    output.Add(0x00);   //pad to multiple of 0x20
                    }
                }

            //that should bring us up to the start of the index table

            uint newIndexOffset = (uint)output.Count;

            if (packageType == PackageType.Agents)
                {
                Utility.AddULongToList(output,Utility.ReverseEndianULong((ulong)indexEntriesForWriting.Count));

                for (int i = 0; i < indexEntriesForWriting.Count; i++)  //a bunch of entries that describe how many files there are of each type
                    {
                    Utility.AddUIntToList(output, Utility.ReverseEndian(indexEntriesForWriting[i].typeID));
                    Utility.AddUIntToList(output, Utility.ReverseEndian(indexEntriesForWriting[i].groupID));
                    Utility.AddUIntToList(output, Utility.ReverseEndian(indexEntriesForWriting[i].typeNumberOfInstances));
                    Utility.AddUIntToList(output, Utility.ReverseEndian(indexEntriesForWriting[i].indexnulls));
                    }

                for (int i = 0; i < subfiles.Count; i++)     //go through the files and add them to the index list. They are organised by type, one type after the other. (So X number of type A, as described above, then Y number of type B...) Within a type, they are organised by hash
                    {
                    Utility.AddULongToList(output, Utility.ReverseEndianULong(subfiles[i].hash));
                    Utility.AddIntToList(output, Utility.ReverseEndianSigned(subfileOffsets[i]));
                    Utility.AddUIntToList(output, Utility.ReverseEndian(subfiles[i].filesize));

                    // this line is probably only required in MSK     utility.AddUIntToList(output, utility.ReverseEndian(subfiles[i].typeID));
                    // this line is probably only required in MSK     utility.AddUIntToList(output, utility.ReverseEndian(subfiles[i].groupID));

                    if (TypeIDsThatRequireCompression.Contains(subfiles[i].typeID))
                        {
                        Utility.AddUIntToList(output, Utility.ReverseEndian(subfiles[i].uncompressedsize));
                        }
                    }

                Utility.OverWriteUIntInList(output, 0x28, ReverseEndianIfNeeded(newIndexOffset));
                Utility.OverWriteUIntInList(output, 0x2C, ReverseEndianIfNeeded((uint)(output.Count - newIndexOffset)));     
                Utility.OverWriteUIntInList(output, 0x44, ReverseEndianIfNeeded(newIndexOffset));      
                }
            else 
                {
                MessageBox.Show("only Agents works at the moment");
                
                }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Title = "Save new package";
            saveFileDialog1.Filter = ".package files (*.package)|*.package";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                File.WriteAllBytes(saveFileDialog1.FileName, output.ToArray());
            }
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
