using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MorcuTool
{
    public partial class Form1 : Form
    {
        Byte[] sourcepackage;
        Byte[] newpackage;


        public Dictionary<ulong, ulong> VaultHashesAndIndexes = new Dictionary<ulong, ulong>();
        public Dictionary<ulong, string> VaultHashesAndFileNames = new Dictionary<ulong, string>();

        public List<luaString> luaStrings = new List<luaString>();

        public Form1()
        {

            InitializeComponent();
        }














        public void SelectPackage()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (isMSA.Checked || isMSK.Checked)
            {
                openFileDialog1.Title = "Select MySims Agents package";
                openFileDialog1.DefaultExt = "package";
                openFileDialog1.Filter = "DataBase Packed File (*.package)|*.package";
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;
                openFileDialog1.Multiselect = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    sourcepackage = File.ReadAllBytes(openFileDialog1.FileName);
                    newpackage = sourcepackage;
                    List<string> filenames = new List<string>();
                    foreach (string filename in openFileDialog1.FileNames)
                    {
                        filenames.Add(filename);
                    }
                    ExtractPackage(filenames);
                }
            }
            else if (isSkyHeroes.Checked)
            {
                openFileDialog1.Title = "Select MySims SkyHeroes Wii archive";
                openFileDialog1.DefaultExt = "wii";
                openFileDialog1.Filter = "Wii archive (*.wii)|*.wii";
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;
                openFileDialog1.Multiselect = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    sourcepackage = File.ReadAllBytes(openFileDialog1.FileName);
                    newpackage = sourcepackage;
                    List<string> filenames = new List<string>();
                    foreach (string filename in openFileDialog1.FileNames)
                    {
                        filenames.Add(filename);
                    }
                    ExtractSkyHeroesPackage(filenames);
                }
            }
        }

        public UInt16 ReverseEndianShort(ushort input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if (isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //   }

            return BitConverter.ToUInt16(bytes, 0);
        }

        public UInt32 ReverseEndian(uint input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if (isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //    }

            return BitConverter.ToUInt32(bytes, 0);
        }

        public UInt64 ReverseEndianLong(ulong input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if(isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //   }


            return BitConverter.ToUInt64(bytes, 0);
        }

        public float ReverseEndianSingle(float input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            Array.Reverse(bytes, 0, bytes.Length);

            return BitConverter.ToSingle(bytes, 0);
        }

        public void ExtractSkyHeroesPackage(List<string> filenames)
        {

            foreach (string file in filenames)
            {
                using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
                {

                    uint sig = ReverseEndian(reader.ReadUInt32());

                    if (sig != 0x030502ED && sig != 0x03051771)
                    {
                        Console.WriteLine("invalid signature");
                        return;
                    }

                    uint fileversion = ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x04;

                    ushort unknown1 = ReverseEndianShort(reader.ReadUInt16());
                    ushort numberoffiles = ReverseEndianShort(reader.ReadUInt16());     //maybe???

                    reader.BaseStream.Position += 0x30;

                    Dictionary<uint, uint> chunklist = new Dictionary<uint, uint>();  //counts and offsets. count is how many 8 byte entries there are. offset is where the first one starts from.


                    for (uint i = 0; i < 110; i++)
                    {
                        uint count = ReverseEndian(reader.ReadUInt32());
                        uint offset = ReverseEndian(reader.ReadUInt32());

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
                            uint fileID = ReverseEndian(reader.ReadUInt32());
                            uint fileoffset = ReverseEndian(reader.ReadUInt32());

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
                                    newfile = ConvertToTPL(file + ID.ToString(), newfile.ToArray());
                                    File.WriteAllBytes(file + ID.ToString() + ".tpl", newfile.ToArray());
                                    break;
                                case 0x28:
                                    File.WriteAllBytes(file + ID.ToString() + ".proxy", newfile.ToArray());
                                    break;
                                case 0x29:
                                    File.WriteAllBytes(file + ID.ToString() + ".animdata", newfile.ToArray());
                                    break;
                                case 0x45:
                                    File.WriteAllBytes(file + ID.ToString() + ".fx", newfile.ToArray());
                                    break;
                                case 0x5A:
                                    File.WriteAllBytes(file + ID.ToString() + ".snddata", newfile.ToArray());
                                    break;
                                case 0x5B:
                                    //newfile = ConvertSkyHeroesModel(file + ID.ToString(), newfile.ToArray());
                                    File.WriteAllBytes(file + ID.ToString() + ".mdl", newfile.ToArray());
                                    break;
                                case 0x63:
                                    File.WriteAllBytes(file + ID.ToString() + ".godinfo", newfile.ToArray());
                                    break;
                                default:
                                    File.WriteAllBytes(file + ID.ToString() +"_"+filetype, newfile.ToArray());
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

            }

            MessageBox.Show("Files extracted. They are in the same folder as the original archive.", "Extraction complete", MessageBoxButtons.OK, MessageBoxIcon.Information);




        }

        public Byte[] Decompress_QFS(Byte[] filebytes) {

            int currentoffset = 0;

            currentoffset += 0x02; //skip 10FB header

            int uncompressedsize = (filebytes[currentoffset] * 0x10000) + (filebytes[currentoffset + 1] * 0x100) + filebytes[currentoffset + 2];
            
            byte[] output = new Byte[uncompressedsize];

            currentoffset += 0x03;

            byte cc = 0; //control byte
            int len = filebytes.Length;
            int numplain = 0; ;
            int numcopy = 0;
            int offset = 0;
            byte byte1 = 0;
            byte byte2 = 0;
            byte byte3 = 0;

            int output_pos = 0;

            while (output_pos < uncompressedsize)
                {
                cc = filebytes[currentoffset];

                len--;

                if (cc >= 0xFC)
                {
                    numplain = cc & 0x03;
                    if (numplain > len)
                    { numplain = len; }
                    numcopy = 0;
                    offset = 0;
                    currentoffset++;
                }
                else if (cc >= 0xE0)
                {
                    numplain = (cc - 0xdf) << 2;
                    numcopy = 0;
                    offset = 0;
                    currentoffset++;
                }
                else if (cc >= 0xC0)
                {
                    len -= 3;
                    byte1 = filebytes[currentoffset + 1];
                    byte2 = filebytes[currentoffset + 2];
                    byte3 = filebytes[currentoffset + 3];
                    numplain = cc & 0x03;
                    numcopy = ((cc & 0x0c) << 6) + 5 + byte3;
                    offset = ((cc & 0x10) << 12) + (byte1 << 8) + byte2;
                    currentoffset += 4;
                }
                else if (cc >= 0x80)
                {
                    len -= 2;
                    byte1 = filebytes[currentoffset + 1];
                    byte2 = filebytes[currentoffset + 2];
                    numplain = (byte1 & 0xc0) >> 6;
                    numcopy = (cc & 0x3f) + 4;
                    offset = ((byte1 & 0x3f) << 8) + byte2;
                    currentoffset += 3;
                }
                else
                {
                    len -= 1;
                    byte1 = filebytes[currentoffset + 1];
                    numplain = (cc & 0x03);
                    numcopy = ((cc & 0x1c) >> 2) + 3;
                    offset = ((cc & 0x60) << 3) + byte1;
                    currentoffset += 2;
                }
                len -= numplain;

                // This section basically copies the parts of the string to the end of the buffer:
                if (numplain > 0) 
                    {
                    for (int i = 0; i < numplain; i++)
                        {
                        output[output_pos] = filebytes[currentoffset];
                        currentoffset++;
                        output_pos++;
                        }
                    }

                       int fromoffset = output_pos - (offset + 1); // 0 == last char
                       for (int i = 0; i <numcopy; i++)     //copy bytes from earlier in the output
                            {
                              output[output_pos] = output[fromoffset +i];
                                output_pos++;
                }
            }
                return output;
        }

      

        public void ExtractPackage(List<string> filenames)
        {

            foreach (string file in filenames)
            {
                Package newPackage = new Package();
                
                int currenttypeindexbeingprocessed = 0;
                int instancesprocessedofcurrenttype = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
                {
                    uint sig = 0;

                    if (isMSA.Checked)
                    {
                        sig = ReverseEndian(reader.ReadUInt32());
                    }
                    else
                    {
                        sig = reader.ReadUInt32();
                    }



                    if (isMSA.Checked)
                    {
                        if (sig != 0x46504244)
                        {
                            MessageBox.Show("This is not a MySims Agents package!", "Package not supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Console.WriteLine("invalid signature");
                            return;
                        }
                    }

                    if (isMSK.Checked)
                    {
                        if (sig != 0x46504244)
                        {
                            MessageBox.Show("This is not a MySims Kingdom package!", "Package not supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Console.WriteLine("invalid signature");
                            Console.WriteLine(sig);
                            return;
                        }
                    }


                    DialogResult askIfSure = MessageBox.Show("Files will be extracted to the Morcutool directory.\n\nWARNING: There may be a large number of files.\nContinue?", "Begin extraction?", MessageBoxButtons.YesNo);
                    
                    if (askIfSure != DialogResult.Yes)
                        {
                        return;
                        }


                    if (isMSA.Checked)
                    {
                        newPackage.majorversion = ReverseEndian(reader.ReadUInt32());
                        newPackage.minorversion = ReverseEndian(reader.ReadUInt32());
                        newPackage.unknown1 = ReverseEndian(reader.ReadUInt32());
                        newPackage.unknown2 = ReverseEndian(reader.ReadUInt32());
                        newPackage.unknown3 = ReverseEndian(reader.ReadUInt32());
                        newPackage.created = ReverseEndian(reader.ReadUInt32());
                        newPackage.modified = ReverseEndian(reader.ReadUInt32());
                        newPackage.indexmajorversion = ReverseEndian(reader.ReadUInt32());

                        newPackage.filecount = ReverseEndian(reader.ReadUInt32());
                        newPackage.indexoffsetdeprecated = ReverseEndian(reader.ReadUInt32());
                        newPackage.indexsize = ReverseEndian(reader.ReadUInt32());
                        newPackage.holeentrycount = ReverseEndian(reader.ReadUInt32());
                        newPackage.holeoffset = ReverseEndian(reader.ReadUInt32());
                        newPackage.holesize = ReverseEndian(reader.ReadUInt32());
                        newPackage.indexminorversion = ReverseEndian(reader.ReadUInt32());
                        newPackage.unknown4 = ReverseEndian(reader.ReadUInt32());
                        newPackage.indexoffset = ReverseEndian(reader.ReadUInt32());
                        newPackage.unknown5 = ReverseEndian(reader.ReadUInt32());
                        newPackage.unknown6 = ReverseEndian(reader.ReadUInt32());
                        newPackage.reserved1 = ReverseEndian(reader.ReadUInt32());
                        newPackage.reserved2 = ReverseEndian(reader.ReadUInt32());
                    }
                    else
                    {
                        newPackage.majorversion = reader.ReadUInt32();
                        newPackage.minorversion = reader.ReadUInt32();
                        newPackage.unknown1 = reader.ReadUInt32();
                        newPackage.unknown2 = reader.ReadUInt32();
                        newPackage.unknown3 = reader.ReadUInt32();
                        newPackage.created = reader.ReadUInt32();
                        newPackage.modified = reader.ReadUInt32();
                        newPackage.indexmajorversion = reader.ReadUInt32();

                        newPackage.filecount = reader.ReadUInt32();
                        newPackage.indexoffsetdeprecated = reader.ReadUInt32();
                        newPackage.indexsize = reader.ReadUInt32();
                        newPackage.holeentrycount = reader.ReadUInt32();
                        newPackage.holeoffset = reader.ReadUInt32();
                        newPackage.holesize = reader.ReadUInt32();
                        newPackage.indexminorversion = reader.ReadUInt32();
                        newPackage.indexoffset = reader.ReadUInt32();
                        newPackage.unknown5 = reader.ReadUInt32();
                        newPackage.unknown6 = reader.ReadUInt32();
                        newPackage.reserved1 = reader.ReadUInt32();
                        newPackage.reserved2 = reader.ReadUInt32();
                    }




                    reader.BaseStream.Position = newPackage.indexoffset;

                    //index header

                    if (isMSA.Checked)
                    {
                        newPackage.indexnumberofentries = ReverseEndianLong(reader.ReadUInt64());

                        for (uint i = 0; i < newPackage.indexnumberofentries; i++)  //a bunch of entries that describe how many files there are of each type
                        {
                            IndexEntry newEntry = new IndexEntry();

                            newEntry.typeID = ReverseEndian(reader.ReadUInt32());
                            newEntry.groupID = ReverseEndian(reader.ReadUInt32());
                            newEntry.typeNumberOfInstances = ReverseEndian(reader.ReadUInt32());
                            newEntry.indexnulls = ReverseEndian(reader.ReadUInt32());

                            newPackage.IndexEntries.Add(newEntry);
                        }

                        currenttypeindexbeingprocessed = 0;
                        instancesprocessedofcurrenttype = 0;

                        for (uint i = 0; i < newPackage.filecount; i++)     //go through the files, they are organised by type, one type after the other. (So X number of type A, as described above, then Y number of type B...)
                        {
                            Subfile newSubfile = new Subfile();
                            newSubfile.hash = ReverseEndianLong(reader.ReadUInt64());
                            newSubfile.fileoffset = ReverseEndian(reader.ReadUInt32());
                            newSubfile.filesize = ReverseEndian(reader.ReadUInt32());
                            newSubfile.typeID = newPackage.IndexEntries[currenttypeindexbeingprocessed].typeID;
                            newSubfile.groupID = newPackage.IndexEntries[currenttypeindexbeingprocessed].groupID;

                            if (newPackage.IndexEntries[currenttypeindexbeingprocessed].groupID != 0)   //it's compressed
                            {
                                newSubfile.uncompressedsize = ReverseEndian(reader.ReadUInt32());  
                            }

                            instancesprocessedofcurrenttype++;

                            if (instancesprocessedofcurrenttype == newPackage.IndexEntries[currenttypeindexbeingprocessed].typeNumberOfInstances)
                            {
                                Console.WriteLine("Processed " + instancesprocessedofcurrenttype + " instances of " + currenttypeindexbeingprocessed);
                                currenttypeindexbeingprocessed++;
                                instancesprocessedofcurrenttype = 0;
                            }

                            newPackage.subfiles.Add(newSubfile);
                        }
                    }
                    else     //MSK etc, don't know if it works
                    {
                        uint indexversion = reader.ReadUInt32();

                        if (indexversion == 0)
                        {
                            for (uint i = 0; i < newPackage.filecount; i++)
                            {
                                Subfile newSubfile = new Subfile();

                                newSubfile.typeID = reader.ReadUInt32();
                                reader.BaseStream.Position += 4;
                                newSubfile.groupID = reader.ReadUInt32();
                                newSubfile.hash = reader.ReadUInt32();
                                newSubfile.fileoffset = reader.ReadUInt32();

                                reader.BaseStream.Position += 0x04;
                                newSubfile.filesize = reader.ReadUInt32();
                                reader.BaseStream.Position += 0x04; //flags

                                newPackage.subfiles.Add(newSubfile);
                            }
                        }
                        else if (indexversion == 1)
                            {
                            MessageBox.Show("Index version 1 not implemented!");
                        
                            }
                        else if (indexversion == 2)
                        {
                            for (uint i = 0; i < newPackage.filecount; i++)
                            {
                                Subfile newSubfile = new Subfile();

                                reader.BaseStream.Position += 4;
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

                                newPackage.subfiles.Add(newSubfile);
                            }
                        }
                        else if (indexversion == 3)
                        {
                            uint allFilesTypeID = reader.ReadUInt32();
                            reader.BaseStream.Position += 4;

                            for (uint i = 0; i < newPackage.filecount; i++)
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

                                newPackage.subfiles.Add(newSubfile);
                            }
                        }
                    }


                    MessageBox.Show("Ready to extract files... this may take a while", "Please wait", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    //extract files

                    Console.WriteLine("filecount " + newPackage.filecount);

                    int filesprocessed = 0;



                    List<string> luaFilenamesForDict = new List<string>();

                    bool containslua = false;

                    for (int i = 0; i < newPackage.filecount; i++)
                    {
                        Byte[] newfile = new byte[newPackage.subfiles[i].filesize];


                        
                    
                        {
                            reader.BaseStream.Position = newPackage.subfiles[i].fileoffset;

                            for (int seekerpos = 0; seekerpos < newPackage.subfiles[i].filesize; seekerpos++)
                            {
                                newfile[seekerpos] = reader.ReadByte();
                            }

                            string fileextension = null;

                            switch (newPackage.subfiles[i].typeID)
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

                                case 0xD5988020:    //MSA PHYS
                                    fileextension = ".phys";
                                    break;

                                default:
                                    Console.WriteLine("Unknown type ID " + newPackage.subfiles[i].typeID +", the file magic was:");
                                    Console.WriteLine((char)newfile[0] + "" + (char)newfile[1] + "" + (char)newfile[2] + "" + (char)newfile[3]);
                                    Console.WriteLine("and this type ID appears " + newPackage.GetNumOccurrencesOfTypeID(newPackage.subfiles[i].typeID) + " times in total.");
                                    Console.WriteLine("index of file was " + filesprocessed);
                                    fileextension = newPackage.subfiles[i].typeID.ToString();
                                    break;
                            }


                            luaString typeIDRealString = GetLuaStringWithHash(newPackage.subfiles[i].typeID);

                            //if (typeIDRealString != null)
                             //   {
                             //   fileextension = "." + typeIDRealString.name;
                             //   }



                            byte[] newfilenameasbytes = BitConverter.GetBytes(ReverseEndianLong(newPackage.subfiles[i].hash));

                            string newfilename = "0x";

                            ulong newfilenameasulong = ReverseEndianLong(newPackage.subfiles[i].hash);



                            if (VaultHashesAndFileNames.Keys.Contains(newPackage.subfiles[i].hash))
                            {
                                newfilename = VaultHashesAndFileNames[newPackage.subfiles[i].hash];
                            }
                            else
                            {
                                newfilename += BitConverter.ToString(newfilenameasbytes).Replace("-", "");
                            }



                            if (newPackage.subfiles[i].uncompressedsize > 0 ) //if it's a compressed file
                                {
                                newfile = Decompress_QFS(newfile);
                                }


                            if (fileextension == ".tpl")
                            {
                                newfile = ConvertToTPL(newfilename, newfile).ToArray();
                            }





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
                            File.WriteAllBytes(newfilename + fileextension, newfile);

                        }
                    }

                    if (containslua && luaStrings.Count > 0)
                    {
                        string[] luaStringsForExport = new string[luaStrings.Count];

                        for (int i = 0; i < luaStrings.Count; i++)
                        {
                            luaStringsForExport[i] = BitConverter.ToString(BitConverter.GetBytes(ReverseEndian(luaStrings[i].hash))).Replace("-", ""); ;
                            luaStringsForExport[i] += " " + luaStrings[i].name;
                        }

                        File.WriteAllLines("luaStrings.lua", luaStringsForExport);
                        }

                    File.WriteAllLines("D:\\THINGS TO BE SAVED\\Visual Studio Source\\Projects\\MorcuTool\\MorcuTool\\bin\\Debug\\dict.txt", luaFilenamesForDict);
                    MessageBox.Show("Processed " + filesprocessed + " files (out of a total " + newPackage.filecount + ").", "Extract complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }
        }





        public List<byte> ConvertModel(string filename, byte[] file)
        {

            List<byte> output = new List<byte>();

            File.WriteAllBytes(filename + ".daetemp", file);

            using (BinaryReader reader = new BinaryReader(File.Open(filename + ".daetemp", FileMode.Open)))
            {
                uint startoffile = 0;

                uint magic = 0;
                uint flags = 0;
                uint meshcount = 0;
                uint meshtableoffset = 0;


                if (isMSA.Checked || isMSK.Checked)
                {
                    magic = ReverseEndian(reader.ReadUInt32());
                    flags = ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x18;

                    meshcount = ReverseEndian(reader.ReadUInt32());
                    meshtableoffset = ReverseEndian(reader.ReadUInt32());



                    for (int i = 0; i < meshcount; i++)
                    {
                        reader.BaseStream.Position = meshtableoffset + (i * 4);
                        uint meshInfoTableOffset = ReverseEndian(reader.ReadUInt32());

                        reader.BaseStream.Position = meshInfoTableOffset;

                        uint primBankSize = ReverseEndian(reader.ReadUInt32());
                        uint primBankOffset = ReverseEndian(reader.ReadUInt32());
                        uint numVertDescriptors = ReverseEndian(reader.ReadUInt32());
                        uint vertDescriptorStartOffset = ReverseEndian(reader.ReadUInt32());
                    }
                }
                else if (isSkyHeroes.Checked)
                {
                    if (reader.ReadByte() != 0x00)      //skip the extra header if it exists
                    {
                        reader.BaseStream.Position = 0x20;
                        startoffile = 0x20;
                    }

                    meshcount = ReverseEndian(reader.ReadUInt32());
                    flags = ReverseEndian(reader.ReadUInt32());
                }
            }


            File.Delete(filename + ".daetemp");
            return output;
        }





        public List<byte> ConvertToTPL(string filename, byte[] file)
        {

            List<byte> output = new List<byte>();


            File.WriteAllBytes(filename + ".tpltemp", file);

            using (BinaryReader reader = new BinaryReader(File.Open(filename + ".tpltemp", FileMode.Open)))
            {
                uint startoffile = 0;

                uint magic = 0;
                uint flags = 0;
                uint imagesize = 0;

                ushort width = 0;
                ushort height = 0;

                uint imageformat = 0;
                uint imagecount = 0;

                uint imageoffset = 0;

                if (isSkyHeroes.Checked && ReverseEndian(reader.ReadUInt32()) != 0)        //skip those annoying extra headers from MySims SkyHeroes
                {
                    startoffile = 0x50;
                }

                reader.BaseStream.Position = startoffile;

                if (isMSA.Checked || isMSK.Checked)
                {
                    magic = ReverseEndian(reader.ReadUInt32());
                    flags = ReverseEndian(reader.ReadUInt32());
                    imagesize = ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x0C;

                    width = ReverseEndianShort(reader.ReadUInt16());
                    height = ReverseEndianShort(reader.ReadUInt16());

                    imageformat = ReverseEndian(reader.ReadUInt32());
                    imagecount = ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x14;

                    imageoffset = ReverseEndian(reader.ReadUInt32());
                }
                else if (isSkyHeroes.Checked)
                {


                    magic = ReverseEndian(reader.ReadUInt32());
                    flags = ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x06;
                    width = ReverseEndianShort(reader.ReadUInt16());

                    reader.BaseStream.Position += 0x02;
                    height = ReverseEndianShort(reader.ReadUInt16());

                    reader.BaseStream.Position += 0x28;

                    imageformat = ReverseEndian(reader.ReadUInt32());

                    imageoffset = ((uint)reader.BaseStream.Position + 0x20) - startoffile;
                }


                reader.BaseStream.Position = startoffile + imageoffset;

                if (isSkyHeroes.Checked)
                {
                    imagesize = (uint)((reader.BaseStream.Length - startoffile) - (reader.BaseStream.Position - startoffile));
                }

                List<Byte[]> imagesubblocks = new List<Byte[]>();


                for (int i = 0; i < imagesize / 8; i++)
                {
                    imagesubblocks.Add(BitConverter.GetBytes(reader.ReadUInt64()));
                }

                //create tpl file byte array

                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x0020AF30)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000001)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x0000000C)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000014)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndianShort(height)));
                output.AddRange(BitConverter.GetBytes(ReverseEndianShort(width)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(imageformat)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000060)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000001)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000001)));

                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(ReverseEndian(0x00000000)));

                for (int i = 0; i < imagesize / 8; i++)
                {
                    output.AddRange(imagesubblocks[i]);
                }

            }

            File.Delete(filename + ".tpltemp");
            return output;

        }



        public void TPLToMySimsTPL(string filename)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                uint startoffile = 0;

                uint magic = 0;
                uint numberofimages = 0;
                uint imagetableoffset = 0;

                uint imageheaderoffset = 0;
                uint paletteheaderoffset = 0;

                ushort height = 0;
                ushort width = 0;
                uint imageformat = 0;
                uint imagedataoffset = 0;
                uint wrapS = 0;
                uint wrapT = 0;
                uint minfilter = 0;
                uint magfilter = 0;




                reader.BaseStream.Position = startoffile;

                magic = ReverseEndian(reader.ReadUInt32());
                numberofimages = ReverseEndian(reader.ReadUInt32());
                imagetableoffset = ReverseEndian(reader.ReadUInt32());

                imageheaderoffset = ReverseEndian(reader.ReadUInt32());
                paletteheaderoffset = ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = startoffile + imageheaderoffset;

                height = ReverseEndianShort(reader.ReadUInt16());
                width = ReverseEndianShort(reader.ReadUInt16());
                imageformat = ReverseEndian(reader.ReadUInt32());
                imagedataoffset = ReverseEndian(reader.ReadUInt32());
                wrapS = ReverseEndian(reader.ReadUInt32());
                wrapT = ReverseEndian(reader.ReadUInt32());
                minfilter = ReverseEndian(reader.ReadUInt32());
                magfilter = ReverseEndian(reader.ReadUInt32());




                reader.BaseStream.Position = startoffile + imagedataoffset;

                List<Byte[]> imagesubblocks = new List<Byte[]>();

                for (int i = 0; i < ((reader.BaseStream.Length - imagedataoffset) / 8); i++)
                {
                    imagesubblocks.Add(BitConverter.GetBytes(reader.ReadUInt64()));
                }

                using (BinaryWriter writer = new BinaryWriter(File.Open(filename + "2.tpl", FileMode.Create)))
                {

                    writer.Write(ReverseEndian(0x14FE0149));
                    writer.Write(ReverseEndian(0x00030000));
                    writer.Write(ReverseEndian((uint)(reader.BaseStream.Length - imagedataoffset)));

                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);

                    writer.Write(ReverseEndianShort(width));
                    writer.Write(ReverseEndianShort(height));

                    writer.Write(ReverseEndian(imageformat));


                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(ReverseEndian(1));
                    writer.Write(ReverseEndian(1));
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(ReverseEndian(0x40));
                    writer.Write(ReverseEndian((uint)(reader.BaseStream.Length - imagedataoffset)));


                    for (int i = 0; i < ((reader.BaseStream.Length - imagedataoffset) / 8); i++)
                    {
                        foreach (Byte b in imagesubblocks[i])
                        {
                            writer.Write(b);
                        }
                    }

                }

            }



        }


        public List<byte> ConvertAgentsModel(string filename, byte[] file)
        {
            List<byte> output = new List<byte>();

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {

                bool MorcubusMode = morcubusModeBox.Checked;  //If true, this will export the second object in the file instead, which was necessary for Morcubus because he was not the first object in his file


                uint startoffile = 0;

                List<mdlObject> objects = new List<mdlObject>();

                uint RMDLMagic = ReverseEndian(reader.ReadUInt32());

                if (RMDLMagic != 0x524D444C)
                    {
                    Console.Write("Not a RMDL model!");
                    return output;    
                    }

                uint flags = ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position += 0x18;

                uint meshcount = ReverseEndian(reader.ReadUInt32());

                uint ObjectListOffset = ReverseEndian(reader.ReadUInt32());

                List<string> outputFile = new List<string>();

                for (int i = 0; i < meshcount; i++)
                {
                    reader.BaseStream.Position = ObjectListOffset + (i * 4);
                    uint ObjectInfoTableOffset = ReverseEndian(reader.ReadUInt32());
                    reader.BaseStream.Position = ObjectInfoTableOffset;

                    uint faceCount = ReverseEndian(reader.ReadUInt32());
                    uint faceListOffset = ReverseEndian(reader.ReadUInt32());
                    uint vertexDescCount = ReverseEndian(reader.ReadUInt32());
                    uint vertexDescListOffset = ReverseEndian(reader.ReadUInt32());

                    //skip an array of 16 unknown floats

                    reader.BaseStream.Position += 0x40;

                    uint weightInfoOffset = ReverseEndian(reader.ReadUInt32());
                    uint unkInfoOffset = ReverseEndian(reader.ReadUInt32());
                    uint unk2InfoOffset = ReverseEndian(reader.ReadUInt32());

                    List<uint> boneMapWeightsArray = new List<uint>();
                    List<uint> boneMapWeightIdsArray = new List<uint>();

                    List<Vertex> vertices = new List<Vertex>();

                    for (int j = 0; j < vertexDescCount; j++)
                        {
                        reader.BaseStream.Position = vertexDescListOffset + (j * 8);

                        }

                    List<face> faceList = new List<face>();

                    reader.BaseStream.Position = faceListOffset;

                    while (reader.BaseStream.Position < faceListOffset + faceCount)
                        {
                        Byte CommandByte = reader.ReadByte();

                        switch (CommandByte)
                            {
                            case 0x00:
                                Console.WriteLine("?");
                                break;
                            case 0x08:
                                reader.BaseStream.Position += 0x05;
                                break;
                            case 0x20:
                                reader.BaseStream.Position += 0x04;
                                break;
                            case 0x28:
                                reader.BaseStream.Position += 0x04;
                                break;
                            case 0x90:
                                ushort vtxCount = ReverseEndianShort(reader.ReadUInt16());

                                List<ushort> prims = new List<ushort>();
                                List<ushort> triPrims = new List<ushort>();

                                for (int k = 0; k < vtxCount; k++)
                                    {
                                    for (int l = 0; l < vertexDescCount; l++)
                                        {
                                       // if (VertexDescs[l].gxAttr != 0)
                                         //   {
                                           // prims[l][k] = ReverseEndianShort(reader.ReadUInt16()) + 1; //possibly these +1s can be removed if an error is being thrown
                                         //   }
                                      //  else if (VertexDescs[l].gxAttr != 0)
                                        //    {   
                                            //prims[l][k] = ReverseEndianShort(reader.ReadUInt16()) + 1; //possibly these +1s can be removed if an error is being thrown
                                        //    }


                                    }


                                    }


                                break;
                            case 0x98:
                                
                                break;
                            default:
                                Console.WriteLine("Unknown face command byte");
                                break;
                            }
                        }



                    reader.BaseStream.Position += 0x04;
                    ushort ID = ReverseEndianShort(reader.ReadUInt16());
                    // Console.WriteLine("break");
                    reader.BaseStream.Position += 0x0A;

                    float U = ReverseEndianSingle(reader.ReadSingle());
                    float X = ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x0C;

                    reader.BaseStream.Position += 0x1C;

                    float Y = ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x04;

                    float Z = ReverseEndianSingle(reader.ReadSingle());

                    //outputFile.Add("v " + X + " " + Y + " " + Z + " //"+ID);
                    reader.BaseStream.Position += 0x04;

                    //unk1count++;

                    ///if (reader.BaseStream.Position > unk1offset + unk1size)
                    //{
                    //    break;
                    //}
                    Console.WriteLine("breakpoint");
                }

                reader.BaseStream.Position += 0x40;

               // for (int i = 0; i < unk1count; i++)
                {
                    reader.BaseStream.Position += 0x30;
                    // float X = ReverseEndianSingle(reader.ReadSingle());
                    //float Y = ReverseEndianSingle(reader.ReadSingle());
                    //float Z = ReverseEndianSingle(reader.ReadSingle());
                    reader.BaseStream.Position += 0x04;
                    // outputFile.Add("v " + X + " " + Y + " " + Z);
                }

                reader.BaseStream.Position = ObjectListOffset;

                reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;
                reader.BaseStream.Position += 0x04;
                reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;

                if (MorcubusMode)
                {
                    reader.BaseStream.Position += 0x04;
                    reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                else
                {
                    reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;
                }


                while (reader.ReadByte() != 0xEF)
                {

                }

                while (reader.ReadByte() == 0xEF)
                {

                }

                reader.BaseStream.Position += 0x03;

                uint unk2sectioncount = ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x20;

                uint VertexCountForStartVertexCalculation = 0;

                for (int i = 0; i < unk2sectioncount; i++)
                {
                    mdlObject newObject = new mdlObject();
                    reader.BaseStream.Position += 0x1C;
                    newObject.vertexCount = ReverseEndian(reader.ReadUInt32());
                    newObject.StartingVertexID = VertexCountForStartVertexCalculation;
                    VertexCountForStartVertexCalculation += newObject.vertexCount;
                    newObject.vertexListOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x08;
                    newObject.facesToRemoveOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x04;
                    newObject.faceListOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x0C;
                    Console.WriteLine("break");
                    objects.Add(newObject);
                }

                reader.BaseStream.Position += 0x10;

                uint padtest = 0;

                testforpad:

                padtest = reader.ReadUInt32();
                if (padtest != 0xEFEFEFEF)
                {
                    reader.BaseStream.Position -= 0x04;
                }
                else
                {
                    goto testforpad;
                }

                Console.WriteLine("break");

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.vertexListOffset;
                    for (int i = 0; i < o.vertexCount; i++)
                    {
                        uint padtest2 = 0;

                        testforpad2:

                        padtest2 = reader.ReadByte();


                        if (padtest2 != 0xEF)
                        {
                            reader.BaseStream.Position -= 0x01;
                        }
                        else
                        {
                            goto testforpad2;
                        }

                        Vertex newVertex = new Vertex();

                        newVertex.X = ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Y = ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Z = ReverseEndianSingle(reader.ReadSingle());

                        o.vertices.Add(newVertex);

                        reader.BaseStream.Position += 0x10;

                        newVertex.U = ReverseEndianSingle(reader.ReadSingle());
                        newVertex.V = ReverseEndianSingle(reader.ReadSingle()) * -1;

                        reader.BaseStream.Position += 0x14;

                    }


                }

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.facesToRemoveOffset;
                    while (reader.ReadByte() != 0xEF)
                    {
                        reader.BaseStream.Position -= 0x01;
                        o.facesToRemove.Add(ReverseEndianShort(reader.ReadUInt16()));
                    }

                    reader.BaseStream.Position = o.faceListOffset;

                    int count = 0;


                    int facesToRemovePos = 0;

                    uint facesToRemoveCountdown = o.facesToRemove[0];

                    reader.BaseStream.Position += 4; //to prepare for the first 0xEF scouting
                    while (facesToRemoveCountdown != 0 && facesToRemovePos != o.facesToRemove.Count)
                    {
                        reader.BaseStream.Position -= 0x04;

                        face newface = new face();
                        newface.v1 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v2 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v3 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        //newface.v4 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);




                        if (newface.v3 > o.vertexCount)
                        {
                            Console.WriteLine("break");
                        }

                        facesToRemoveCountdown--;

                        if (facesToRemoveCountdown > 1)
                        {

                            o.faces.Add(newface);
                        }
                        else
                        {
                            Console.WriteLine("face with ID " + count + " was omitted");
                        }


                        if (facesToRemoveCountdown == 0)
                        {
                            facesToRemovePos++;
                            if (facesToRemovePos < o.facesToRemove.Count)
                            {
                                facesToRemoveCountdown = o.facesToRemove[facesToRemovePos];
                            }

                        }

                        Console.WriteLine("break");

                        count++;



                    }


                }

                foreach (mdlObject o in objects)
                {
                    //if (objects.IndexOf(o) > 0)
                    //   {
                    //   break;
                    //   }
                    outputFile.Add("o Object" + objects.IndexOf(o));

                    foreach (Vertex v in o.vertices)
                    {
                        outputFile.Add("v " + v.X + " " + v.Y + " " + v.Z);
                    }

                    foreach (Vertex v in o.vertices)
                    {
                        outputFile.Add("vt " + v.U + " " + v.V);
                    }

                    foreach (face f in o.faces)
                    {
                        outputFile.Add("f " + (o.StartingVertexID + f.v1) + "/" + (o.StartingVertexID + f.v1) + " " + (o.StartingVertexID + f.v2) + "/" + (o.StartingVertexID + f.v2) + " " + (o.StartingVertexID + f.v3) + "/" + (o.StartingVertexID + f.v3));


                    }
                }

               // Console.WriteLine(Path.GetDirectoryName(filename) + realFileName + ".obj");
               // File.WriteAllLines(Path.Combine(Path.GetDirectoryName(filename), realFileName + ".obj"), outputFile);

            }

            File.Delete(filename + ".mdltemp");
            return output;

        }

        public List<byte> ConvertSkyHeroesModel(string filename, byte[] file)
        {
            List<byte> output = new List<byte>();

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {

                bool MorcubusMode = morcubusModeBox.Checked;  //If true, this will export the second object in the file instead, which was necessary for Morcubus because he was not the first object in his file


                uint startoffile = 0;

                List<mdlObject> objects = new List<mdlObject>();

                string realFileName = null;
                for (int i = 0; i < 0x20; i++)
                    {
                    Char newchar = reader.ReadChar();

                    if ((Byte)newchar == 0x00)
                        {
                        break;
                        }
                        
                    realFileName += newchar;
                    }


                uint meshcount = ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = 0x30; //start of first object

                reader.BaseStream.Position += 0x04;

                reader.BaseStream.Position += 0x0C; //skip weird thing (coords of object?)

                uint unk1offset = 0x30 + ReverseEndian(reader.ReadUInt32());
                uint unk1size = 0x30 + ReverseEndian(reader.ReadUInt32());

                uint ObjectListOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                uint ObjectListSize = 0x30 + ReverseEndian(reader.ReadUInt32());    //this is not actually the size

                reader.BaseStream.Position = unk1offset;

                List<string> outputFile = new List<string>();

                uint unk1count = 0;

                while (reader.BaseStream.Position < unk1offset + unk1size)
                    {
                    reader.BaseStream.Position += 0x04;
                    ushort ID = ReverseEndianShort(reader.ReadUInt16());
                   // Console.WriteLine("break");
                    reader.BaseStream.Position += 0x0A;

                    float U = ReverseEndianSingle(reader.ReadSingle());
                    float X = ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x0C;

                    reader.BaseStream.Position += 0x1C;

                    float Y = ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x04;

                    float Z = ReverseEndianSingle(reader.ReadSingle());

                    //outputFile.Add("v " + X + " " + Y + " " + Z + " //"+ID);
                    reader.BaseStream.Position += 0x04;

                    unk1count++;

                    if (reader.BaseStream.Position > unk1offset + unk1size)
                        {
                        break;
                        }
                    Console.WriteLine("breakpoint");
                    }

                reader.BaseStream.Position += 0x40;

                for (int i = 0; i < unk1count; i++)
                    {
                    reader.BaseStream.Position += 0x30;
                   // float X = ReverseEndianSingle(reader.ReadSingle());
                    //float Y = ReverseEndianSingle(reader.ReadSingle());
                    //float Z = ReverseEndianSingle(reader.ReadSingle());
                    reader.BaseStream.Position += 0x04;
                   // outputFile.Add("v " + X + " " + Y + " " + Z);
                }

                reader.BaseStream.Position = ObjectListOffset;
                
                reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;
                reader.BaseStream.Position += 0x04;
                reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;

                if (MorcubusMode)
                {
                    reader.BaseStream.Position += 0x04;
                    reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                else
                {
                    reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                
                
                while (reader.ReadByte() != 0xEF)
                    {
                    
                    }

                while (reader.ReadByte() == 0xEF)
                {

                }

                reader.BaseStream.Position += 0x03;

                uint unk2sectioncount = ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = ReverseEndian(reader.ReadUInt32()) -0x20;

                uint VertexCountForStartVertexCalculation = 0;

                for (int i = 0; i < unk2sectioncount; i++)
                    {
                    mdlObject newObject = new mdlObject();
                    reader.BaseStream.Position += 0x1C;
                    newObject.vertexCount = ReverseEndian(reader.ReadUInt32());
                    newObject.StartingVertexID = VertexCountForStartVertexCalculation;
                    VertexCountForStartVertexCalculation += newObject.vertexCount;
                    newObject.vertexListOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x08;
                    newObject.facesToRemoveOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x04;
                    newObject.faceListOffset = ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x0C;
                    Console.WriteLine("break");
                    objects.Add(newObject);
                }

                reader.BaseStream.Position += 0x10;

                uint padtest = 0;

                testforpad:

                padtest = reader.ReadUInt32();
                if (padtest != 0xEFEFEFEF)
                {
                    reader.BaseStream.Position -= 0x04;
                }
                else
                {
                    goto testforpad;
                }

                Console.WriteLine("break");

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.vertexListOffset;
                    for (int i = 0; i < o.vertexCount; i++)
                    {
                        uint padtest2 = 0;

                        testforpad2:

                        padtest2 = reader.ReadByte();


                        if (padtest2 != 0xEF)
                        {
                            reader.BaseStream.Position -= 0x01;
                        }
                        else
                        {
                            goto testforpad2;
                        }

                        Vertex newVertex = new Vertex();

                        newVertex.X = ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Y = ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Z = ReverseEndianSingle(reader.ReadSingle());

                        o.vertices.Add(newVertex);

                        reader.BaseStream.Position += 0x10;

                        newVertex.U = ReverseEndianSingle(reader.ReadSingle());
                        newVertex.V = ReverseEndianSingle(reader.ReadSingle()) * -1;

                        reader.BaseStream.Position += 0x14;
                       
                    }

                   
                }

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.facesToRemoveOffset;
                    while (reader.ReadByte() != 0xEF)
                    {
                        reader.BaseStream.Position -= 0x01;
                        o.facesToRemove.Add(ReverseEndianShort(reader.ReadUInt16()));
                    }

                        reader.BaseStream.Position = o.faceListOffset;

                    int count = 0;

                    
                    int facesToRemovePos = 0;

                    uint facesToRemoveCountdown = o.facesToRemove[0];

                    reader.BaseStream.Position += 4; //to prepare for the first 0xEF scouting
                    while (facesToRemoveCountdown != 0 && facesToRemovePos != o.facesToRemove.Count)
                    {
                        reader.BaseStream.Position -= 0x04;
                        
                        face newface = new face();
                        newface.v1 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v2 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v3 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        //newface.v4 = (ushort)(ReverseEndianShort(reader.ReadUInt16()) + 1);
                        

                        

                        if (newface.v3 > o.vertexCount)
                        {
                            Console.WriteLine("break");
                        }

                        facesToRemoveCountdown--;

                            if (facesToRemoveCountdown > 1)
                            {
                                
                            o.faces.Add(newface);
                            }   
                            else
                            {
                            Console.WriteLine("face with ID " + count + " was omitted");
                            }
                        

                        if (facesToRemoveCountdown == 0)
                        {
                            facesToRemovePos++;
                            if (facesToRemovePos < o.facesToRemove.Count)
                                {
                                facesToRemoveCountdown = o.facesToRemove[facesToRemovePos];
                            }
                            
                            }

                        Console.WriteLine("break");
                        
                        count++;

              
                       
                    }


                }

                    foreach (mdlObject o in objects)
                    {
                    //if (objects.IndexOf(o) > 0)
                     //   {
                     //   break;
                     //   }
                    outputFile.Add("o Object" + objects.IndexOf(o));

                    foreach (Vertex v in o.vertices)
                        {
                        outputFile.Add("v " + v.X + " " + v.Y + " " + v.Z);
                        }

                    foreach (Vertex v in o.vertices)
                    {
                        outputFile.Add("vt " + v.U + " " + v.V);
                    }

                    foreach (face f in o.faces)
                    {
                        outputFile.Add("f " + (o.StartingVertexID + f.v1) + "/" + (o.StartingVertexID + f.v1) + " " + (o.StartingVertexID + f.v2) + "/" + (o.StartingVertexID + f.v2) + " " + (o.StartingVertexID + f.v3) + "/" + (o.StartingVertexID + f.v3));
                        
                        
                    }
                }

                Console.WriteLine(Path.GetDirectoryName(filename) + realFileName + ".obj");
                File.WriteAllLines(Path.Combine(Path.GetDirectoryName(filename),realFileName + ".obj"), outputFile);

            }

            File.Delete(filename + ".mdltemp");
            return output;

        }

        private void simsTPLToTPLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Title = "Select MySims Series TPL";
            openFileDialog1.DefaultExt = "tpl";

            openFileDialog1.Filter = "MySims Series TPL file (*.tpl)|*.tpl";

            if (isMSK.Checked)
            {
                openFileDialog1.Filter = "S3PE fake DDS file (*.dds)|*.dds";
            }

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in openFileDialog1.FileNames)
                {
                    File.WriteAllBytes(filename + "new.tpl", ConvertToTPL(filename, File.ReadAllBytes(filename)).ToArray());
                }
            }

            MessageBox.Show("Done", "Conversion complete", MessageBoxButtons.OK, MessageBoxIcon.Information);


        }

        private void tPLToMSATPLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Title = "Select TPL";
            openFileDialog1.DefaultExt = "tpl";
            openFileDialog1.Filter = "Texture Palette Library (*.tpl)|*.tpl|All files (*.*)|*.*";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in openFileDialog1.FileNames)
                {
                    TPLToMySimsTPL(filename);
                }
            }

            MessageBox.Show("Done", "Conversion complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        public luaString GetLuaStringWithHash(uint hash)
            {
            foreach(luaString l in luaStrings)
                {
                if (l.hash == hash)
                    {
                    return l;
                    }
                }
            return null;
            }




        public class luaString {

            public uint hash = 0;
            public string name = "";
            public uint nameOffset = 0;
        }
            private void loadVaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Title = "Select attribdbd.bin (in the 'Vaults' folder)";
            openFileDialog1.DefaultExt = "bin";
            openFileDialog1.Filter = "Binary file (*.bin)|*.bin";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;


            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (BinaryReader reader = new BinaryReader(File.Open(openFileDialog1.FileName, FileMode.Open)))
                {
                    //first, for lua strings (i.e. variable names etc)

                    reader.BaseStream.Position = 0x2D6FF3;

                    while (reader.BaseStream.Position < 0x32EBEB)   //I don't know if this is quite the right address
                        {
                        luaString newLuaString = new luaString();
                        newLuaString.hash = ReverseEndian(reader.ReadUInt32());
                        newLuaString.nameOffset = ReverseEndian(reader.ReadUInt32());
                       
                        luaStrings.Add(newLuaString);
                        }

                    reader.BaseStream.Position = 0x32EBEB;

                    for (int i = 0; i < luaStrings.Count; i++)
                        {
                        reader.BaseStream.Position = 0x32EBEB + luaStrings[i].nameOffset;

                        if (reader.BaseStream.Position > reader.BaseStream.Length)
                            {
                            break;
                            }

                        while(reader.ReadByte() != 0x00)
                            {
                            reader.BaseStream.Position--;
                            luaStrings[i].name += reader.ReadChar() + "";
                            }
                        }


                    //and now for filenames
                    reader.BaseStream.Position = 0x556241;

                    for (uint i = 0; i < 0xEDB4; i++)
                    {
                        ulong hash = ReverseEndianLong(reader.ReadUInt64());
                        ulong index = ReverseEndianLong(reader.ReadUInt64());

                        VaultHashesAndIndexes.Add(hash, index);
                    }

                    foreach (ulong hash in VaultHashesAndIndexes.Keys)
                    {
                        reader.BaseStream.Position = (0x643D90 + (long)VaultHashesAndIndexes[hash]) + 1;

                        List<Byte> bytes = new List<Byte>();

                        byte newbyte = 0x00;

                        readanotherbyte:


                        newbyte = reader.ReadByte();

                        if (reader.BaseStream.Position == (0x643D90 + (long)VaultHashesAndIndexes[hash]) + 2 && newbyte == 0x00)
                        {
                            goto readanotherbyte;
                        }

                        if (reader.BaseStream.Position == reader.BaseStream.Length - 1)
                        {

                        }
                        else
                        {
                            if (newbyte != 0x00)
                            {
                                bytes.Add(newbyte);
                                if (reader.BaseStream.Position < reader.BaseStream.Length - 1)
                                {
                                    goto readanotherbyte;
                                }
                            }
                        }


                        VaultHashesAndFileNames.Add(hash, System.Text.Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.ToArray().Length));


                    }

                    if (isMSA.Checked) //then add the lua filenames that aren't in the vaults
                    {
                        VaultHashesAndFileNames.Add(0x001935F4D51D2A44, "Tent_Interaction_TakeNap");
                        VaultHashesAndFileNames.Add(0x00456145F62C640D, "EmbeddedBuildableRegion");
                        VaultHashesAndFileNames.Add(0x00AE7406DF8B142C, "PerFrameSingleton");
                        VaultHashesAndFileNames.Add(0x0158B1BB6F7429AD, "DanceFloor_Interaction_Dance");
                        VaultHashesAndFileNames.Add(0x0184FCB3A64CA6C5, "Couch");
                        VaultHashesAndFileNames.Add(0x01DB4D1FA64057B4, "Job_RequestCharacterControl");
                        VaultHashesAndFileNames.Add(0x05836ACC88FCD2F6, "NPC_Declarations");
                        VaultHashesAndFileNames.Add(0x05A1B707325B3E05, "Television_Interaction_TurnOn");
                        VaultHashesAndFileNames.Add(0x0651778A074A45AB, "Job_WorldChange");
                        VaultHashesAndFileNames.Add(0x06974C8C833586B2, "Forensics_Interaction_Forensify");
                        VaultHashesAndFileNames.Add(0x07A052273141CD7B, "UINunchuk");
                        VaultHashesAndFileNames.Add(0x08325307B4EB15B7, "UI");
                        VaultHashesAndFileNames.Add(0x08326307B4EB316F, "EA");
                        VaultHashesAndFileNames.Add(0x088A1290BBC886FC, "SushiStool_Interaction_Eat");
                        VaultHashesAndFileNames.Add(0x09725B165CF2EF07, "ModalUtils");
                        VaultHashesAndFileNames.Add(0x0A82DBD165BE2493, "Refrigerator");
                        VaultHashesAndFileNames.Add(0x0A8F16764DDF9B30, "Job_TeleportNonBlocking");
                        VaultHashesAndFileNames.Add(0x0AAA13B8932AC848, "DJBooth_Interaction_Dance");
                        VaultHashesAndFileNames.Add(0x0BB037190D3631FD, "Hottub_Interaction_Relax");
                        VaultHashesAndFileNames.Add(0x0BFED339C3B72C01, "Boat_Interaction_LeaveIsland");
                        VaultHashesAndFileNames.Add(0x0C4B91AC8CC30B34, "Job_InteractionBase");
                        VaultHashesAndFileNames.Add(0x0C7DFFB3AD2B63E0, "Chair");
                        VaultHashesAndFileNames.Add(0x0D441381588FE08C, "Television_Interaction_Watch");
                        VaultHashesAndFileNames.Add(0x0DC07132436EE700, "Chair_Interaction_Sit");
                        VaultHashesAndFileNames.Add(0x0E53D829104F2A52, "UnlockSystem");
                        VaultHashesAndFileNames.Add(0x0F3FB1BA6607A639, "Television");
                        VaultHashesAndFileNames.Add(0x10EFE2F46E3D2368, "UIModalTutorialDialog");
                        VaultHashesAndFileNames.Add(0x119A250AE12C6C36, "ScriptObjectBase");
                        VaultHashesAndFileNames.Add(0x1492A669F07EADB9, "TutorialScriptBase");
                        VaultHashesAndFileNames.Add(0x14FA8F8CF1EF9FDE, "Job_ConstructionController");
                        VaultHashesAndFileNames.Add(0x15D7B3E8F633A6EF, "Job_SpawnObject");
                        VaultHashesAndFileNames.Add(0x161A6C8522DD72E5, "TattooChair_Interaction_Sit");
                        VaultHashesAndFileNames.Add(0x165983D4AD97F695, "Job_GadgetController");
                        VaultHashesAndFileNames.Add(0x16D96016901459FB, "Trigger");
                        VaultHashesAndFileNames.Add(0x17431E11AE60F054, "TutorialTriggerObject");
                        VaultHashesAndFileNames.Add(0x17BF60E8D8248E3C, "UIBackStoryEnd");
                        VaultHashesAndFileNames.Add(0x17FBD7132AB74597, "Guitar_Interaction_Play");
                        VaultHashesAndFileNames.Add(0x1A3E56FDE819136F, "UIModalPopUpCinematic");
                        VaultHashesAndFileNames.Add(0x1AE142F645D30955, "TriggerPortal");
                        VaultHashesAndFileNames.Add(0x1CB6F87EF10442B1, "Case");
                        VaultHashesAndFileNames.Add(0x1D45CB68F9E14DC0, "MinigamePlaneVsEye");
                        VaultHashesAndFileNames.Add(0x1DFFF58F26FD23EA, "UIModalDialog");
                        VaultHashesAndFileNames.Add(0x1E07FBDAD0DAFDC0, "TurkeyCart_Interaction_Serve");
                        VaultHashesAndFileNames.Add(0x20514D852BBB7D91, "Job_ReplaceModelAndRig");
                        VaultHashesAndFileNames.Add(0x24A13F7AF74591A8, "MinigameLockPicking");
                        VaultHashesAndFileNames.Add(0x252A169ADAD95B74, "SummoningCircle");
                        VaultHashesAndFileNames.Add(0x25CA38BA7AD970CE, "StateObjectBase");
                        VaultHashesAndFileNames.Add(0x2695D47A9999E2BB, "Job_PlayAnimMachine");
                        VaultHashesAndFileNames.Add(0x2713277EF75D54AF, "Boat");
                        VaultHashesAndFileNames.Add(0x28CD396A105300DF, "Bed_Interaction_Sleep");
                        VaultHashesAndFileNames.Add(0x29239FE06DBB4557, "UIConstructionBlock");
                        VaultHashesAndFileNames.Add(0x2A5F8DCC1CB45052, "Job_Wander");
                        VaultHashesAndFileNames.Add(0x2B380CA6D46A76D8, "UIModalPopUpDialog");
                        VaultHashesAndFileNames.Add(0x2B9EE0A00BE7D7EC, "Job_UIBaseUnloader");
                        VaultHashesAndFileNames.Add(0x2D87F8701C374D67, "JobManager");
                        VaultHashesAndFileNames.Add(0x2EC0930DC697F472, "TutorialScriptStoveEBR");
                        VaultHashesAndFileNames.Add(0x2F45401EDF5021F7, "LuaDebuggerUtils");
                        VaultHashesAndFileNames.Add(0x2FC909D73838D42D, "Job_RouteToWorld");
                        VaultHashesAndFileNames.Add(0x2FE1ACEA022CE90A, "AutoTest_AllLevelLocations");
                        VaultHashesAndFileNames.Add(0x32315EE62083614B, "ObjectDeclarations");
                        VaultHashesAndFileNames.Add(0x35E8CD82108B5AFD, "Treadmill");
                        VaultHashesAndFileNames.Add(0x36FFBBDFF810FCD5, "UIRecruiting");
                        VaultHashesAndFileNames.Add(0x37649ECF542C3376, "Fireplace");
                        VaultHashesAndFileNames.Add(0x38EB36D6912C8E86, "Job_EnterEBR");
                        VaultHashesAndFileNames.Add(0x3AFF336D79171B02, "Job_RouteToPosition");
                        VaultHashesAndFileNames.Add(0x3C18C0DE65EC32C9, "Stereo");
                        VaultHashesAndFileNames.Add(0x3E4057F8852E3547, "UICredits");
                        VaultHashesAndFileNames.Add(0x3F7A9AE3503F62CB, "UICasTransitionScreen");
                        VaultHashesAndFileNames.Add(0x405B5573970BF881, "DJBooth");
                        VaultHashesAndFileNames.Add(0x406A6DCAA6F7D115, "UIInfoCard");
                        VaultHashesAndFileNames.Add(0x40862A76CB14D450, "MinigameForensics");
                        VaultHashesAndFileNames.Add(0x413CCC340CD843AD, "FloorPlan_Interaction");
                        VaultHashesAndFileNames.Add(0x420966178735F819, "Job_TeleportThroughPortal");
                        VaultHashesAndFileNames.Add(0x424DECB8E2B3C6C4, "UIPaintMode");
                        VaultHashesAndFileNames.Add(0x42A1A1749D531A83, "Job_PlayAnimation");
                        VaultHashesAndFileNames.Add(0x43EB22BC396B9C02, "ArcadeMachine_Interaction_Play");
                        VaultHashesAndFileNames.Add(0x455AAF6B065BB545, "UIDispatchSimBioPage");
                        VaultHashesAndFileNames.Add(0x457D7A599E63780B, "NPC_Kraken");
                        VaultHashesAndFileNames.Add(0x45E25E16512101BD, "Job_EnterMetaState");
                        VaultHashesAndFileNames.Add(0x45E8D16D562A4D9C, "Chair_Interaction_Jenny");
                        VaultHashesAndFileNames.Add(0x4633CEC1F5A89622, "GiantWheel_Interaction_Turn");
                        VaultHashesAndFileNames.Add(0x46AA3195900EBF27, "DryingChair_Interaction_Sit");
                        VaultHashesAndFileNames.Add(0x471FB630202C3195, "DJBooth_Interaction_DJ");
                        VaultHashesAndFileNames.Add(0x4769A20780D60437, "PlumbobTutorialScript");
                        VaultHashesAndFileNames.Add(0x47A446DBF9AACD67, "KeyItem");
                        VaultHashesAndFileNames.Add(0x48F5EE1C606090C2, "TrophyCase_Interaction_View");
                        VaultHashesAndFileNames.Add(0x4A3DEC52F5AC6FC9, "CharacterBase");
                        VaultHashesAndFileNames.Add(0x4B34AF062CE78D7A, "AutoTest_StressRoute");
                        VaultHashesAndFileNames.Add(0x4B4200FE4539BFA6, "ToolChest");
                        VaultHashesAndFileNames.Add(0x4CFCF86ED13C1DF0, "CharacterBase_Interaction_PlayerMoment");
                        VaultHashesAndFileNames.Add(0x4DFB71AD73C37997, "MechanicalBull_Interaction_Ride");
                        VaultHashesAndFileNames.Add(0x4FADDA9E7AB4BDDC, "UIClueMoment");
                        VaultHashesAndFileNames.Add(0x4FE7F07716945B01, "GameplayLoad");
                        VaultHashesAndFileNames.Add(0x51CCECD7B6427D95, "TattooChair");
                        VaultHashesAndFileNames.Add(0x52B3559810A90C55, "UIDispatchSelection");
                        VaultHashesAndFileNames.Add(0x52DB7CECCAF59FBF, "SandPile");
                        VaultHashesAndFileNames.Add(0x52FD64B7929DEA23, "Job_BehaviorController");
                        VaultHashesAndFileNames.Add(0x5307C9F1BB780E08, "UIDispatchHistoryQueue");
                        VaultHashesAndFileNames.Add(0x530BDBAD39F211BD, "UIBackStory");
                        VaultHashesAndFileNames.Add(0x53B487A8A67AE00A, "WaterCooler_Interaction_Drink");
                        VaultHashesAndFileNames.Add(0x53C73D1A54AD932D, "Campfire_Interaction_WarmHands");
                        VaultHashesAndFileNames.Add(0x53CFD1CA41F71C03, "WasteBasket_Interaction_ThrowPaper");
                        VaultHashesAndFileNames.Add(0x5400EE4FE3308A4C, "UITalkDialogCinematic");
                        VaultHashesAndFileNames.Add(0x54695476B52C31DE, "PicnicBlanket_Interaction_Eat");
                        VaultHashesAndFileNames.Add(0x54F184877C4240A7, "DryingChair");
                        VaultHashesAndFileNames.Add(0x55103C021A854533, "SalonChair");
                        VaultHashesAndFileNames.Add(0x5533D624C9D7A306, "Settings");
                        VaultHashesAndFileNames.Add(0x55638A1F3DC1276F, "TrophyCase");
                        VaultHashesAndFileNames.Add(0x566F4E120650FCDE, "Job_RouteCloseToPosition");
                        VaultHashesAndFileNames.Add(0x5768257FC715E045, "UIElevatorPanel");
                        VaultHashesAndFileNames.Add(0x592C9119D134BF75, "Lockpicking_Interaction_PickLock");
                        VaultHashesAndFileNames.Add(0x592CDD42838AC10B, "UICASMenu");
                        VaultHashesAndFileNames.Add(0x593E47BD29393447, "Dresser");
                        VaultHashesAndFileNames.Add(0x5A07962F9797D324, "Stove");
                        VaultHashesAndFileNames.Add(0x5A7980A6B671026F, "Event");
                        VaultHashesAndFileNames.Add(0x5AAD4D65D6C7E18C, "Job_PerFrameFunctionCallback");
                        VaultHashesAndFileNames.Add(0x5B8A6BC603831B58, "Constants");
                        VaultHashesAndFileNames.Add(0x5D00016438F55408, "Couch_Interaction_Sleep");
                        VaultHashesAndFileNames.Add(0x5D6B42026E15C485, "ManaVent");
                        VaultHashesAndFileNames.Add(0x5DC7D2FA72588124, "TattooChair_Interaction_Tattoo");
                        VaultHashesAndFileNames.Add(0x605B081C628E0C93, "SkiLift");
                        VaultHashesAndFileNames.Add(0x605D99A196DE0D75, "Couch_Interaction_Sit");
                        VaultHashesAndFileNames.Add(0x6150A8603C24C113, "DispatchMission");
                        VaultHashesAndFileNames.Add(0x63870C41BAAF4261, "UITutorialScreen");
                        VaultHashesAndFileNames.Add(0x6418A437180B4C92, "Job_InteractionPeeking");
                        VaultHashesAndFileNames.Add(0x672F6ADC3B486455, "Job_PlayIdleAnimation");
                        VaultHashesAndFileNames.Add(0x676721A9E7966307, "Job_RouteToSlot");
                        VaultHashesAndFileNames.Add(0x678CD19FD89B467B, "Job_SocialBase");
                        VaultHashesAndFileNames.Add(0x68D70BC24AFE550E, "Mirror_CAS");
                        VaultHashesAndFileNames.Add(0x69072AC8459380D7, "CharacterBase_Interaction_Interrupted");
                        VaultHashesAndFileNames.Add(0x6939169BFDB6397C, "DanceFloor");
                        VaultHashesAndFileNames.Add(0x6976FC2FA0E6EEEB, "UIModalDialogNoAudio");
                        VaultHashesAndFileNames.Add(0x6A0AFD06EEBC9003, "Mirror_Interaction_CAS");
                        VaultHashesAndFileNames.Add(0x6A0BB92CD97E85D9, "UISlideShow");
                        VaultHashesAndFileNames.Add(0x6A7854C82C35F630, "WorldBase");
                        VaultHashesAndFileNames.Add(0x6AA940DA96C2AF5B, "SalonChair_Interaction_Sit");
                        VaultHashesAndFileNames.Add(0x6C6A780DD6AE3468, "Job_FadeColor");
                        VaultHashesAndFileNames.Add(0x6F037B44B6E2BF6A, "SystemLoad");
                        VaultHashesAndFileNames.Add(0x73B5847F29E11944, "CASInitialWorld");
                        VaultHashesAndFileNames.Add(0x73F42DA6A6FA6CE7, "Examine_Interaction");
                        VaultHashesAndFileNames.Add(0x74F6952C89F5B74D, "ClimbingObject");
                        VaultHashesAndFileNames.Add(0x75D03549D3496728, "UIDebugTextTester");
                        VaultHashesAndFileNames.Add(0x77265349B43D53CA, "System");
                        VaultHashesAndFileNames.Add(0x7758290B3FC3582A, "Job_RouteToObject");
                        VaultHashesAndFileNames.Add(0x784975BB99BFBD6E, "UICitySelect");
                        VaultHashesAndFileNames.Add(0x78CC1F95BBECECB8, "UIModalDialogError");
                        VaultHashesAndFileNames.Add(0x791977EAE1DC563D, "UIDispatchMissionCardHistory");
                        VaultHashesAndFileNames.Add(0x79C230851BD66F75, "CharacterBase_Interaction_Idle");
                        VaultHashesAndFileNames.Add(0x7A7A3060C70852C3, "Hottub");
                        VaultHashesAndFileNames.Add(0x7AECB9E645F1549E, "DoorBase");
                        VaultHashesAndFileNames.Add(0x7B2E23540F9F4F35, "Debug_Interaction_ForceNPCUse");
                        VaultHashesAndFileNames.Add(0x7BB7BBA0C67CD1B5, "Stereo_Interaction_TurnOn");
                        VaultHashesAndFileNames.Add(0x7E7FEA04E18483A3, "Trophy");
                        VaultHashesAndFileNames.Add(0x7E9AFF520101ADAF, "Debug_Interaction_DebugEBRs");
                        VaultHashesAndFileNames.Add(0x822E234831977CE4, "Piano");
                        VaultHashesAndFileNames.Add(0x8353D53CE41FAB13, "Guitar");
                        VaultHashesAndFileNames.Add(0x837FCCA91E3854B3, "UISpinningFish");
                        VaultHashesAndFileNames.Add(0x8425FF218A0E5038, "Boat_Interaction_ChangeOutfit");
                        VaultHashesAndFileNames.Add(0x8470314E8A018AF2, "CharacterBase_Interaction_Social");
                        VaultHashesAndFileNames.Add(0x8670986E962AEC4C, "Job_RouteToBehaviorBlock");
                        VaultHashesAndFileNames.Add(0x874B80870A7FC57E, "Job_InteractionMachine");
                        VaultHashesAndFileNames.Add(0x8765BBF67881E255, "WolfCage");
                        VaultHashesAndFileNames.Add(0x878E33002ECA1035, "UITutorialPopup");
                        VaultHashesAndFileNames.Add(0x87C9727C6E467AC1, "DebugDisplayManager");
                        VaultHashesAndFileNames.Add(0x87F022C4E5E494F0, "TutorialScriptHQConstruction");
                        VaultHashesAndFileNames.Add(0x88823CBB1377EB98, "UIMinigame");
                        VaultHashesAndFileNames.Add(0x8925450BCA53DA03, "Refrigerator_Interaction_GetSnack");
                        VaultHashesAndFileNames.Add(0x89DA304BE0E3903C, "UICaseBook");
                        VaultHashesAndFileNames.Add(0x8AFD98453DBC279F, "UILangSelect");
                        VaultHashesAndFileNames.Add(0x8C09427E9FCCC442, "Tent");
                        VaultHashesAndFileNames.Add(0x8C544A4837C2F1EF, "Phone");
                        VaultHashesAndFileNames.Add(0x8CFAB0EA977D2328, "WaterCooler_Interaction_HangOut");
                        VaultHashesAndFileNames.Add(0x8EA580475A689AE9, "JobBase");
                        VaultHashesAndFileNames.Add(0x8EA88480D7B9249D, "PizzaOven_Interaction_Cook");
                        VaultHashesAndFileNames.Add(0x8EF406C8B880F9E8, "Job_RouteToFootprint");
                        VaultHashesAndFileNames.Add(0x8F8F5800ED8CAAB6, "Treadmill_Interaction_Run");
                        VaultHashesAndFileNames.Add(0x905CA6F2ADAAF9C3, "Bathtub");
                        VaultHashesAndFileNames.Add(0x91D2BCFB0621A865, "UISavingDialog");
                        VaultHashesAndFileNames.Add(0x9304F604998FC578, "CharacterBase_Interaction_React");
                        VaultHashesAndFileNames.Add(0x94189C1CB2FFC090, "Stereo_Interaction_Dance");
                        VaultHashesAndFileNames.Add(0x94C0041CA209C6DD, "GameObjectBase");
                        VaultHashesAndFileNames.Add(0x94CCE9E1515BD522, "Newspaper");
                        VaultHashesAndFileNames.Add(0x97744E0364705485, "UIFloorSelectCard");
                        VaultHashesAndFileNames.Add(0x97CD0F76BF8CB00A, "BackstoryLocation");
                        VaultHashesAndFileNames.Add(0x98D784AD8A538171, "Campfire_Interaction_RoastMarshmallows");
                        VaultHashesAndFileNames.Add(0x98FE22770ADB2E8B, "UserSettings");
                        VaultHashesAndFileNames.Add(0x9926E7DE0A17E7EE, "Strict");
                        VaultHashesAndFileNames.Add(0x9978E1B45CF6667A, "UITransitionScreen");
                        VaultHashesAndFileNames.Add(0x9A11C60F104A99D3, "TurkeyCart_Interaction_Eat");
                        VaultHashesAndFileNames.Add(0x9A2AFE385ECA4BEE, "DragonHead");
                        VaultHashesAndFileNames.Add(0x9AAF85102A858102, "AudioScriptObjectBase");
                        VaultHashesAndFileNames.Add(0x9C5ADF436D04C6C2, "Common");
                        VaultHashesAndFileNames.Add(0x9CBB20F2FEA0D244, "Job_ClimbingController");
                        VaultHashesAndFileNames.Add(0x9DEED08530C24248, "CharacterBase_Interaction_Move");
                        VaultHashesAndFileNames.Add(0x9EA0B784920B1A96, "Job_BalancingController");
                        VaultHashesAndFileNames.Add(0xA07642BA7F5CE77F, "TutorialController");
                        VaultHashesAndFileNames.Add(0xA10CE5A3B3D96953, "Balancing_Interaction_Balancing");
                        VaultHashesAndFileNames.Add(0xA11879910E61F6D7, "Job_PackageLoad");
                        VaultHashesAndFileNames.Add(0xA22FF8BD624704A4, "Job_ShowTextMessage");
                        VaultHashesAndFileNames.Add(0xA348C7921596FD59, "UIEBRReset");
                        VaultHashesAndFileNames.Add(0xA3B871C555CDA63B, "Inventory");
                        VaultHashesAndFileNames.Add(0xA449964119DED5FF, "ElectroDanceSphere");
                        VaultHashesAndFileNames.Add(0xA4D6B9AB9A5BA0DC, "EventManager");
                        VaultHashesAndFileNames.Add(0xA5E8A13E0D940096, "AutoTest_HQ");
                        VaultHashesAndFileNames.Add(0xA662B1E22228F7BA, "Pedestal_Interaction_NPC");
                        VaultHashesAndFileNames.Add(0xA6F95D969293D81E, "UIFloorplan");
                        VaultHashesAndFileNames.Add(0xA73D89BFAFE19E67, "BalancingObject");
                        VaultHashesAndFileNames.Add(0xA86C3D3C315C3D35, "Job_RotateToFaceObject");
                        VaultHashesAndFileNames.Add(0xA984CA9289595512, "InteractionUtils");
                        VaultHashesAndFileNames.Add(0xA98902AB3294EF38, "Dresser_Interaction_RifleThroughClothes");
                        VaultHashesAndFileNames.Add(0xA9F90C9F3FF1540C, "LeadTriggerObject");
                        VaultHashesAndFileNames.Add(0xAA1CFC1617A9FC0B, "Job_RouteToPosition3D");
                        VaultHashesAndFileNames.Add(0xAAE8AA656E569306, "Job_RotateToFacePos");
                        VaultHashesAndFileNames.Add(0xAB87C2E749E00778, "UIBase");
                        VaultHashesAndFileNames.Add(0xAC52691AF3EFAA84, "Icicle");
                        VaultHashesAndFileNames.Add(0xAD0119BA4AE8109B, "UIOptionsScreen");
                        VaultHashesAndFileNames.Add(0xAD0A26C035FA388E, "BlockObjectBase");
                        VaultHashesAndFileNames.Add(0xAD75A4A5134B7A63, "MechanicalBull");
                        VaultHashesAndFileNames.Add(0xADC085B703810B09, "Phases");
                        VaultHashesAndFileNames.Add(0xAE0B61B86E7DE954, "InterestingObject");
                        VaultHashesAndFileNames.Add(0xAE106955C78D1765, "Barrel");
                        VaultHashesAndFileNames.Add(0xAE2D26432FF1110A, "UIUtils");
                        VaultHashesAndFileNames.Add(0xAE6BAFFA2920CF39, "UIMinigameHelp");
                        VaultHashesAndFileNames.Add(0xAF959ADAF05FB0D5, "CharacterBase_Interaction_Dispatch");
                        VaultHashesAndFileNames.Add(0xB04BA87DB63C78CD, "AutoTest_LevelLocations");
                        VaultHashesAndFileNames.Add(0xB04FC46F0911FD25, "FlourMill");
                        VaultHashesAndFileNames.Add(0xB190B0D0710DD54C, "lowBatteryDialog");
                        VaultHashesAndFileNames.Add(0xB3197A38FB3C7BDE, "UIGenericCounter");
                        VaultHashesAndFileNames.Add(0xB3EDA6BE2974CC19, "Wardrobe");
                        VaultHashesAndFileNames.Add(0xB50FAD13B80687EA, "DayNTime");
                        VaultHashesAndFileNames.Add(0xB6058F3146186441, "Stereo_Interaction_TurnOff");
                        VaultHashesAndFileNames.Add(0xB6FB405F1EBCD79C, "Job_Teleport");
                        VaultHashesAndFileNames.Add(0xB8B72BAAA00FB3D9, "Crystal");
                        VaultHashesAndFileNames.Add(0xB90ED50F0AC3E451, "TurkeyCart");
                        VaultHashesAndFileNames.Add(0xB9F7CA8EF00D12CA, "Job_Sleep");
                        VaultHashesAndFileNames.Add(0xBAD07A1F0A9C79E7, "FlourMill_Interaction_MillFlour");
                        VaultHashesAndFileNames.Add(0xBBC0F24EC6C1BB9D, "UIControllerDisconnect");
                        VaultHashesAndFileNames.Add(0xBC0A24075E09390E, "UILetterbox");
                        VaultHashesAndFileNames.Add(0xBC2C596CB17CF394, "AmbientCritter");
                        VaultHashesAndFileNames.Add(0xBCA1FF16BB403BAD, "AutoTestManager");
                        VaultHashesAndFileNames.Add(0xBEB09623102178BC, "Speaker");
                        VaultHashesAndFileNames.Add(0xBF265AF557D3A298, "UIComputerDialog");
                        VaultHashesAndFileNames.Add(0xC02237148FA15DD4, "Stove_Interaction_Cook");
                        VaultHashesAndFileNames.Add(0xC1B2A0C74CD5ED9E, "EffectDummy");
                        VaultHashesAndFileNames.Add(0xC1C9DE6BA67C55DC, "NotFinalLoad");
                        VaultHashesAndFileNames.Add(0xC1FC4FD2714EF068, "GumballMachine_Interaction_Buy");
                        VaultHashesAndFileNames.Add(0xC2FEC01D3CBC3F30, "UIMovieMenu");
                        VaultHashesAndFileNames.Add(0xC4161046A63EC9CD, "SummoningCircle_Interaction_Seance");
                        VaultHashesAndFileNames.Add(0xC68A5917B1D68666, "JamJar");
                        VaultHashesAndFileNames.Add(0xC7E55B48075B3517, "MinigameHacking");
                        VaultHashesAndFileNames.Add(0xC9FF2979ADD842C2, "ClueTracker");
                        VaultHashesAndFileNames.Add(0xCAA324845C1B7F4C, "UIKeyboard");
                        VaultHashesAndFileNames.Add(0xCC39ED71ECA7D167, "Job_TriggerWaitUntilSafe");
                        VaultHashesAndFileNames.Add(0xCC6AECA558D82EA0, "UICallouts");
                        VaultHashesAndFileNames.Add(0xCCFA19CF9CCA890B, "Pedestal");
                        VaultHashesAndFileNames.Add(0xCD788DBBCFCB13EF, "UIDispatchMissionCard");
                        VaultHashesAndFileNames.Add(0xCDEC7F89F6CDE78F, "Dumbwaiter");
                        VaultHashesAndFileNames.Add(0xCEE153B282742719, "Social_TalkPlusPlusPlus");
                        VaultHashesAndFileNames.Add(0xCFBCCB7934564563, "UILoadingGameError");
                        VaultHashesAndFileNames.Add(0xCFC92BF8D245D381, "UIModalPopUpDialogNoAudio");
                        VaultHashesAndFileNames.Add(0xD04C6560DBE90DC9, "Job_CutsceneController");
                        VaultHashesAndFileNames.Add(0xD07B657EC653955B, "Lead");
                        VaultHashesAndFileNames.Add(0xD0E15C2ED6A73E0D, "EffectBase");
                        VaultHashesAndFileNames.Add(0xD1E71C7891ED7D9F, "SoundDummyScript");
                        VaultHashesAndFileNames.Add(0xD252DA4569FE32BB, "PhaseManager");
                        VaultHashesAndFileNames.Add(0xD36BFD55F0BF3B0A, "Guitar_Interaction_Watch");
                        VaultHashesAndFileNames.Add(0xD408F366D951F6F8, "Couch_Interaction_JumpOn");
                        VaultHashesAndFileNames.Add(0xD452D0115264055D, "Shaking_Interaction");
                        VaultHashesAndFileNames.Add(0xD58052BF5715833E, "UIRewardDialog");
                        VaultHashesAndFileNames.Add(0xD583CB89D5A5A9B8, "UIConstructionInventory");
                        VaultHashesAndFileNames.Add(0xD86EF4B3B74171F7, "SoundTrack");
                        VaultHashesAndFileNames.Add(0xD87B6670AA89C490, "Harrier");
                        VaultHashesAndFileNames.Add(0xD87CCD93E54841C9, "Paragraph");
                        VaultHashesAndFileNames.Add(0xD8D9A1186BAD3242, "Bed");
                        VaultHashesAndFileNames.Add(0xDA1489933781A64A, "Player");
                        VaultHashesAndFileNames.Add(0xDAD842C3675F0915, "CharacterBase_Debug_PushSim");
                        VaultHashesAndFileNames.Add(0xDAE4F98F4C7F47EF, "BuildableRegion");
                        VaultHashesAndFileNames.Add(0xDB08E135E5ED71E8, "SalonChair_Interaction_Style");
                        VaultHashesAndFileNames.Add(0xDB23EE15785C22FB, "Debug_Interaction_DebugCutscene");
                        VaultHashesAndFileNames.Add(0xDDDC3AC692FE9511, "UIDispatchMsgQueue");
                        VaultHashesAndFileNames.Add(0xDE1119717F3EDEE9, "UITalkDialog");
                        VaultHashesAndFileNames.Add(0xDEAE555161A08A81, "PauseScreen");
                        VaultHashesAndFileNames.Add(0xE1E2E12BA5B265D8, "CharacterBase_Interaction_Wander");
                        VaultHashesAndFileNames.Add(0xE1FBF68380887D0B, "EffectScript");
                        VaultHashesAndFileNames.Add(0xE21D7A93923E4662, "DummyScript");
                        VaultHashesAndFileNames.Add(0xE21F75E5FD5A2C5A, "UILoadMenu");
                        VaultHashesAndFileNames.Add(0xE3090F30C490073F, "AutoTest_StressAnimation");
                        VaultHashesAndFileNames.Add(0xE58660DB7E93C9CA, "Computer");
                        VaultHashesAndFileNames.Add(0xE5B7D0AF413CAAF9, "TreasureChest");
                        VaultHashesAndFileNames.Add(0xE6B76EE2AD555A9A, "Pedestal_Interaction_Player");
                        VaultHashesAndFileNames.Add(0xE75FE22F1086F2B0, "UIMinigameLoadScreen");
                        VaultHashesAndFileNames.Add(0xE7BCD0C9CD97B53F, "Job_InputListener");
                        VaultHashesAndFileNames.Add(0xE7E5F8C9DBAC393F, "Bathtub_Interaction_TakeBath");
                        VaultHashesAndFileNames.Add(0xE82569B3981A66FB, "Class");
                        VaultHashesAndFileNames.Add(0xEA0D7EA36AEDB848, "PortalBase");
                        VaultHashesAndFileNames.Add(0xEA1D821A57896B9C, "TransitionWorld");
                        VaultHashesAndFileNames.Add(0xEA5E1D1ACA3567D2, "Campfire");
                        VaultHashesAndFileNames.Add(0xEAC820CF65FA2AD8, "UIKeyboardJapan");
                        VaultHashesAndFileNames.Add(0xEBFBF666D2D29F42, "Hearth_Interaction_WarmHands");
                        VaultHashesAndFileNames.Add(0xEC1D2911EDCACD8F, "Elevator");
                        VaultHashesAndFileNames.Add(0xECA00CEF680ADA7D, "EDS_Interaction_Ride");
                        VaultHashesAndFileNames.Add(0xED080E3A910A7071, "Television_Interaction_TurnOff");
                        VaultHashesAndFileNames.Add(0xEE073A320273981D, "UIInitCASMenu");
                        VaultHashesAndFileNames.Add(0xEF4C90B3C36D213E, "Interest_CommonCode");
                        VaultHashesAndFileNames.Add(0xEFAAC58ABCC50171, "Hacking_Interaction_Hack");
                        VaultHashesAndFileNames.Add(0xF1ECEDF87C894647, "Job_Fade");
                        VaultHashesAndFileNames.Add(0xF227D008EB797C43, "NPC_IdleData");
                        VaultHashesAndFileNames.Add(0xF275EE09AABE0FF4, "Bookshelf_Interaction_Browse");
                        VaultHashesAndFileNames.Add(0xF33DD730713C28B6, "UIDispatchMsgCard");
                        VaultHashesAndFileNames.Add(0xF359EA9E2A564021, "Job_RunInteractionQueue");
                        VaultHashesAndFileNames.Add(0xF509915399678DCB, "DebugMenu");
                        VaultHashesAndFileNames.Add(0xF62F9E6DA123FD14, "UISocialize");
                        VaultHashesAndFileNames.Add(0xF6DBCBA96310EDF6, "Job_InteractionState");
                        VaultHashesAndFileNames.Add(0xF721886571B33B19, "UICASContextPicker");
                        VaultHashesAndFileNames.Add(0xFA9736F16BCBE321, "DebugOnScreenLogger");
                        VaultHashesAndFileNames.Add(0xFBA2B1B8625A5AE5, "Phone_Interaction_Recruit");
                        VaultHashesAndFileNames.Add(0xFC35A5FD0CD0F5D5, "Job_SpawnForInventory");
                        VaultHashesAndFileNames.Add(0xFC790A8D32C82BA5, "UIMainMenu");
                        VaultHashesAndFileNames.Add(0xFD556827143AC4ED, "Job_ProcessInteractionQueue");
                        VaultHashesAndFileNames.Add(0xFDE613259FFF01F2, "AmbientObject");
                        VaultHashesAndFileNames.Add(0xFF4D669DDA41037E, "Piano_Interaction_Play");
                        VaultHashesAndFileNames.Add(0xFFB305AD564F71DE, "UIArcadeMachine");

                    }
                }
            }

            MessageBox.Show("Loaded vault. Many filenames will now be correct!", "Loaded vault", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectPackage();
        }

        private void skyHeroesPlanetReplacerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Title = "Select mainmenu.wii";
            openFileDialog1.DefaultExt = "wii";
            openFileDialog1.Filter = "Wii archive (*.wii)|*.wii";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Byte[] mainmenuskyheroes = File.ReadAllBytes(openFileDialog1.FileName);

                for (int i = 2241104; i < 2924896; i++)
                {
                    mainmenuskyheroes[i] = 0x00;
                }

                OpenFileDialog openFileDialog2 = new OpenFileDialog();

                openFileDialog2.Title = "Select mdl chunk";
                openFileDialog2.DefaultExt = "mdl";
                openFileDialog2.Filter = "MySims SkyHeroes animated model (*.mdl)|*.mdl";
                openFileDialog2.CheckFileExists = true;
                openFileDialog2.CheckPathExists = true;

                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {

                    Byte[] model = File.ReadAllBytes(openFileDialog2.FileName);

                    for (int i = 0; i < model.Length; i++)
                    {
                        mainmenuskyheroes[2241104 + i] = model[i];
                    }

                    File.WriteAllBytes(openFileDialog1.FileName + "new.wii", mainmenuskyheroes);

                    MessageBox.Show("The planet model has been replaced.", "Replacement complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }



        }

        private void isMSA_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void convertModelToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (isSkyHeroes.Checked)
            {
                OpenFileDialog openFileDialog2 = new OpenFileDialog();

                openFileDialog2.Title = "Select mdl file";
                openFileDialog2.DefaultExt = "mdl";
                openFileDialog2.Filter = "MySims SkyHeroes 3d models (*.mdl)|*.mdl";
                openFileDialog2.CheckFileExists = true;
                openFileDialog2.CheckPathExists = true;
                openFileDialog2.Multiselect = true;

                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filename in openFileDialog2.FileNames)
                    {
                        ConvertSkyHeroesModel(filename, File.ReadAllBytes(filename));
                    }
                }
            }
            else
            {
                MessageBox.Show("Model conversion is only supported for MySims Skyheroes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);

                /*
                OpenFileDialog openFileDialog2 = new OpenFileDialog();

                openFileDialog2.Title = "Select rmdl file";
                openFileDialog2.DefaultExt = "rmdl";
                openFileDialog2.Filter = "MySims Agents 3d models (*.rmdl)|*.rmdl";
                openFileDialog2.CheckFileExists = true;
                openFileDialog2.CheckPathExists = true;
                openFileDialog2.Multiselect = true;

                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filename in openFileDialog2.FileNames)
                    {
                        ConvertAgentsModel(filename, File.ReadAllBytes(filename));
                    }
                }*/
            }

        }

        private void decompressQFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void decompressQFSToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog2 = new OpenFileDialog();

            openFileDialog2.Title = "Select QFS compressed file";
            openFileDialog2.CheckFileExists = true;
            openFileDialog2.CheckPathExists = true;

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in openFileDialog2.FileNames)
                {
                    File.WriteAllBytes(filename+"decomp" , Decompress_QFS(File.ReadAllBytes(filename)).ToArray());
                }


            }
        }

        private void hiddenButton_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog openFileDialog2 = new OpenFileDialog();

            openFileDialog2.Title = "Select LEGO .DAT file and I'll see if my checksum method is correct";
            openFileDialog2.CheckFileExists = true;
            openFileDialog2.CheckPathExists = true;

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                int checksum = 0x12345678;

                using (BinaryReader reader = new BinaryReader(File.Open(openFileDialog2.FileName, FileMode.Open)))
                {

                    reader.BaseStream.Position = 0x08;

                    for(long i = 0; i < reader.BaseStream.Length - 0x08; i++)
                        {
                        checksum += reader.ReadByte();
                        }

                    Console.WriteLine(checksum);

                }






            }


        }
    }
}
