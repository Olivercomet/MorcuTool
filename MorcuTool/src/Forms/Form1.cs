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

        Dictionary<string, TreeNode> foldersProcessed = new Dictionary<string, TreeNode>();
        public Dictionary<TreeNode, Subfile> treeNodesAndSubfiles = new Dictionary<TreeNode, Subfile>();


        public Form1()
        {
            InitializeComponent();

            FileTree.NodeMouseClick += (sender, args) => FileTree.SelectedNode = args.Node;
            FileTree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.FileTree_NodeMouseClick);
        }

        private void FileTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (treeNodesAndSubfiles.Keys.Contains(FileTree.SelectedNode))
                    {
                    subfileContextMenu.Show(Cursor.Position);
                    }
            }
        }


        public void SelectPackage()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

                openFileDialog1.Title = "Select MySims package";
                openFileDialog1.DefaultExt = "package";
                openFileDialog1.Filter = "MySims package (*.package; *.wii)|*.package;*.wii";
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                   global.activePackage = new Package();
                   global.activePackage.filebytes = File.ReadAllBytes(openFileDialog1.FileName);
                   global.activePackage.filename = openFileDialog1.FileName;
                   global.activePackage.form1 = this;

                   global.activePackage.LoadPackage();
                }
        }



        public List<byte> ConvertModel(string filename, byte[] file)    //for MSA, not finished
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
                    magic = utility.ReverseEndian(reader.ReadUInt32());
                    flags = utility.ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x18;

                    meshcount = utility.ReverseEndian(reader.ReadUInt32());
                    meshtableoffset = utility.ReverseEndian(reader.ReadUInt32());



                    for (int i = 0; i < meshcount; i++)
                    {
                        reader.BaseStream.Position = meshtableoffset + (i * 4);
                        uint meshInfoTableOffset = utility.ReverseEndian(reader.ReadUInt32());

                        reader.BaseStream.Position = meshInfoTableOffset;

                        uint primBankSize = utility.ReverseEndian(reader.ReadUInt32());
                        uint primBankOffset = utility.ReverseEndian(reader.ReadUInt32());
                        uint numVertDescriptors = utility.ReverseEndian(reader.ReadUInt32());
                        uint vertDescriptorStartOffset = utility.ReverseEndian(reader.ReadUInt32());
                    }
                }
                else if (isSkyHeroes.Checked)
                {
                    if (reader.ReadByte() != 0x00)      //skip the extra header if it exists
                    {
                        reader.BaseStream.Position = 0x20;
                        startoffile = 0x20;
                    }

                    meshcount = utility.ReverseEndian(reader.ReadUInt32());
                    flags = utility.ReverseEndian(reader.ReadUInt32());
                }
            }


            File.Delete(filename + ".daetemp");
            return output;
        }





        


        public List<byte> ConvertAgentsModel(string filename, byte[] file)
        {
            List<byte> output = new List<byte>();

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {

                bool MorcubusMode = morcubusModeBox.Checked;  //If true, this will export the second object in the file instead, which was necessary for Morcubus because he was not the first object in his file


                uint startoffile = 0;

                List<mdlObject> objects = new List<mdlObject>();

                uint RMDLMagic = utility.ReverseEndian(reader.ReadUInt32());

                if (RMDLMagic != 0x524D444C)
                    {
                    Console.Write("Not a RMDL model!");
                    return output;    
                    }

                uint flags = utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position += 0x18;

                uint meshcount = utility.ReverseEndian(reader.ReadUInt32());

                uint ObjectListOffset = utility.ReverseEndian(reader.ReadUInt32());

                List<string> outputFile = new List<string>();

                for (int i = 0; i < meshcount; i++)
                {
                    reader.BaseStream.Position = ObjectListOffset + (i * 4);
                    uint ObjectInfoTableOffset = utility.ReverseEndian(reader.ReadUInt32());
                    reader.BaseStream.Position = ObjectInfoTableOffset;

                    uint faceCount = utility.ReverseEndian(reader.ReadUInt32());
                    uint faceListOffset = utility.ReverseEndian(reader.ReadUInt32());
                    uint vertexDescCount = utility.ReverseEndian(reader.ReadUInt32());
                    uint vertexDescListOffset = utility.ReverseEndian(reader.ReadUInt32());

                    //skip an array of 16 unknown floats

                    reader.BaseStream.Position += 0x40;

                    uint weightInfoOffset = utility.ReverseEndian(reader.ReadUInt32());
                    uint unkInfoOffset = utility.ReverseEndian(reader.ReadUInt32());
                    uint unk2InfoOffset = utility.ReverseEndian(reader.ReadUInt32());

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
                                ushort vtxCount = utility.ReverseEndianShort(reader.ReadUInt16());

                                List<ushort> prims = new List<ushort>();
                                List<ushort> triPrims = new List<ushort>();

                                for (int k = 0; k < vtxCount; k++)
                                    {
                                    for (int l = 0; l < vertexDescCount; l++)
                                        {
                                       // if (VertexDescs[l].gxAttr != 0)
                                         //   {
                                           // prims[l][k] = utility.ReverseEndianShort(reader.ReadUInt16()) + 1; //possibly these +1s can be removed if an error is being thrown
                                         //   }
                                      //  else if (VertexDescs[l].gxAttr != 0)
                                        //    {   
                                            //prims[l][k] = utility.ReverseEndianShort(reader.ReadUInt16()) + 1; //possibly these +1s can be removed if an error is being thrown
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
                    ushort ID = utility.ReverseEndianShort(reader.ReadUInt16());
                    // Console.WriteLine("break");
                    reader.BaseStream.Position += 0x0A;

                    float U = utility.ReverseEndianSingle(reader.ReadSingle());
                    float X = utility.ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x0C;

                    reader.BaseStream.Position += 0x1C;

                    float Y = utility.ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x04;

                    float Z = utility.ReverseEndianSingle(reader.ReadSingle());

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
                    // float X = utility.ReverseEndianSingle(reader.ReadSingle());
                    //float Y = utility.ReverseEndianSingle(reader.ReadSingle());
                    //float Z = utility.ReverseEndianSingle(reader.ReadSingle());
                    reader.BaseStream.Position += 0x04;
                    // outputFile.Add("v " + X + " " + Y + " " + Z);
                }

                reader.BaseStream.Position = ObjectListOffset;

                reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                reader.BaseStream.Position += 0x04;
                reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;

                if (MorcubusMode)
                {
                    reader.BaseStream.Position += 0x04;
                    reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                else
                {
                    reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                }


                while (reader.ReadByte() != 0xEF)
                {

                }

                while (reader.ReadByte() == 0xEF)
                {

                }

                reader.BaseStream.Position += 0x03;

                uint unk2sectioncount = utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x20;

                uint VertexCountForStartVertexCalculation = 0;

                for (int i = 0; i < unk2sectioncount; i++)
                {
                    mdlObject newObject = new mdlObject();
                    reader.BaseStream.Position += 0x1C;
                    newObject.vertexCount = utility.ReverseEndian(reader.ReadUInt32());
                    newObject.StartingVertexID = VertexCountForStartVertexCalculation;
                    VertexCountForStartVertexCalculation += newObject.vertexCount;
                    newObject.vertexListOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x08;
                    newObject.facesToRemoveOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x04;
                    newObject.faceListOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
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

                        newVertex.X = utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Y = utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Z = utility.ReverseEndianSingle(reader.ReadSingle());

                        o.vertices.Add(newVertex);

                        reader.BaseStream.Position += 0x10;

                        newVertex.U = utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.V = utility.ReverseEndianSingle(reader.ReadSingle()) * -1;

                        reader.BaseStream.Position += 0x14;

                    }


                }

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.facesToRemoveOffset;
                    while (reader.ReadByte() != 0xEF)
                    {
                        reader.BaseStream.Position -= 0x01;
                        o.facesToRemove.Add(utility.ReverseEndianShort(reader.ReadUInt16()));
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
                        newface.v1 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v2 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v3 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        //newface.v4 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);




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


                uint meshcount = utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = 0x30; //start of first object

                reader.BaseStream.Position += 0x04;

                reader.BaseStream.Position += 0x0C; //skip weird thing (coords of object?)

                uint unk1offset = 0x30 + utility.ReverseEndian(reader.ReadUInt32());
                uint unk1size = 0x30 + utility.ReverseEndian(reader.ReadUInt32());

                uint ObjectListOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                uint ObjectListSize = 0x30 + utility.ReverseEndian(reader.ReadUInt32());    //this is not actually the size

                reader.BaseStream.Position = unk1offset;

                List<string> outputFile = new List<string>();

                uint unk1count = 0;

                while (reader.BaseStream.Position < unk1offset + unk1size)
                    {
                    reader.BaseStream.Position += 0x04;
                    ushort ID = utility.ReverseEndianShort(reader.ReadUInt16());
                   // Console.WriteLine("break");
                    reader.BaseStream.Position += 0x0A;

                    float U = utility.ReverseEndianSingle(reader.ReadSingle());
                    float X = utility.ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x0C;

                    reader.BaseStream.Position += 0x1C;

                    float Y = utility.ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x04;

                    float Z = utility.ReverseEndianSingle(reader.ReadSingle());

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
                   // float X = utility.ReverseEndianSingle(reader.ReadSingle());
                    //float Y = utility.ReverseEndianSingle(reader.ReadSingle());
                    //float Z = utility.ReverseEndianSingle(reader.ReadSingle());
                    reader.BaseStream.Position += 0x04;
                   // outputFile.Add("v " + X + " " + Y + " " + Z);
                }

                reader.BaseStream.Position = ObjectListOffset;
                
                reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                reader.BaseStream.Position += 0x04;
                reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;

                if (MorcubusMode)
                {
                    reader.BaseStream.Position += 0x04;
                    reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                else
                {
                    reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                
                
                while (reader.ReadByte() != 0xEF)
                    {
                    
                    }

                while (reader.ReadByte() == 0xEF)
                    {

                    }

                reader.BaseStream.Position += 0x03;

                uint unk2sectioncount = utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = utility.ReverseEndian(reader.ReadUInt32()) -0x20;

                uint VertexCountForStartVertexCalculation = 0;

                for (int i = 0; i < unk2sectioncount; i++)
                    {
                    mdlObject newObject = new mdlObject();
                    reader.BaseStream.Position += 0x1C;
                    newObject.vertexCount = utility.ReverseEndian(reader.ReadUInt32());
                    newObject.StartingVertexID = VertexCountForStartVertexCalculation;
                    VertexCountForStartVertexCalculation += newObject.vertexCount;
                    newObject.vertexListOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x08;
                    newObject.facesToRemoveOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x04;
                    newObject.faceListOffset = utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
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

                        newVertex.X = utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Y = utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.Z = utility.ReverseEndianSingle(reader.ReadSingle());

                        o.vertices.Add(newVertex);

                        reader.BaseStream.Position += 0x10;

                        newVertex.U = utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.V = utility.ReverseEndianSingle(reader.ReadSingle()) * -1;

                        reader.BaseStream.Position += 0x14;
                    }
                }

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.facesToRemoveOffset;
                    while (reader.ReadByte() != 0xEF)
                    {
                        reader.BaseStream.Position -= 0x01;
                        o.facesToRemove.Add(utility.ReverseEndianShort(reader.ReadUInt16()));
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
                        newface.v1 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v2 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v3 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        //newface.v4 = (ushort)(utility.ReverseEndianShort(reader.ReadUInt16()) + 1);

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
                    File.WriteAllBytes(filename + "new.tpl", imageTools.ConvertToTPL(filename, File.ReadAllBytes(filename)).ToArray());
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
                    imageTools.TPLToMySimsTPL(filename);
                }
            }

            MessageBox.Show("Done", "Conversion complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                global.activeVault = new Vault();
                global.activeVault.filename = openFileDialog1.FileName;
                global.activeVault.LoadVault();
                MessageBox.Show("Loaded vault. Many filenames will now be correct!", "Loaded vault", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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
                    File.WriteAllBytes(filename+"decomp" , utility.Decompress_QFS(File.ReadAllBytes(filename)).ToArray());
                }
            }
        }

        public void MakeFileTree()
        {
            FileTree.Nodes.Clear();

            treeNodesAndSubfiles = new Dictionary<TreeNode, Subfile>();
            foldersProcessed = new Dictionary<string, TreeNode>();


            FileTree.BeginUpdate();

            List<Subfile> subfiles = global.activePackage.subfiles;


            foreach (Subfile file in subfiles)
            {
                //now add entries to the file tree

                List<string> dirs = new List<string>();

                string tempfilepath = Path.GetFileName(global.activePackage.filename).Replace("/", ""); ;


                if (file.filename[0] != '/')
                {
                    tempfilepath += "/";
                }
                tempfilepath += file.filename;

                if (tempfilepath[tempfilepath.Length - 1] == '/')
                {
                    tempfilepath.Remove(tempfilepath.Length - 1);
                }

                if (tempfilepath[0] == '/')
                {
                    tempfilepath = tempfilepath.Substring(1, tempfilepath.Length - 1);
                }

                int number_of_dir_levels = tempfilepath.Split('/').Length;

                for (int d = 0; d < number_of_dir_levels - 1; d++)  //store a string for each level of the directory, so that we can check each folder individually (by this I mean checking whether or not it already exists in the tree)
                {
                    dirs.Add(Path.GetDirectoryName(tempfilepath));
                    tempfilepath = Path.GetDirectoryName(tempfilepath);

                    if (tempfilepath[tempfilepath.Length - 1] == '/')
                    {
                        tempfilepath.Remove(tempfilepath.Length - 1);
                    }
                }

                bool isRoot = true;
                TreeNode newestNode = new TreeNode();

                for (int f = dirs.Count - 1; f >= 0; f--)
                {
                    if (!foldersProcessed.Keys.Contains(dirs[f].ToLower()))    //if the folder isn't in the tree yet
                    {
                        if (!isRoot)
                        {   //add to the chain of nodes
                            FileTree.SelectedNode = newestNode;
                            newestNode = new TreeNode(Path.GetFileName(dirs[f]));
                            newestNode.ImageIndex = 0;
                            newestNode.SelectedImageIndex = 0;
                            FileTree.SelectedNode.Nodes.Add(newestNode);
                        }
                        else
                        { //create a root node first
                            newestNode = new TreeNode(Path.GetFileName(dirs[f]));
                            newestNode.ImageIndex = 0;
                            newestNode.SelectedImageIndex = 0;
                            FileTree.Nodes.Add(newestNode);
                            isRoot = false;
                        }

                        foldersProcessed.Add(dirs[f].ToLower(), newestNode);  //add it to the list of folders we've put in the tree   
                    }
                    else
                    {
                        newestNode = foldersProcessed[dirs[f].ToLower()]; //set the parent node of the next folder to the existing node
                        newestNode.ImageIndex = 0;
                        newestNode.SelectedImageIndex = 0;
                        FileTree.SelectedNode = newestNode;
                        isRoot = false;
                    }
                }
                FileTree.SelectedNode = newestNode;
                newestNode = new TreeNode(Path.GetFileName(file.filename));
                newestNode.ImageIndex = 1;
                newestNode.SelectedImageIndex = 1;
                FileTree.SelectedNode.Nodes.Add(newestNode);
                file.treeNode = newestNode;
                treeNodesAndSubfiles.Add(newestNode, file);
            }

            //FileTree.Sort();
            FileTree.CollapseAll();
            FileTree.EndUpdate();
        }

        private void exportSubfile_Click(object sender, EventArgs e)
        {
            treeNodesAndSubfiles[FileTree.SelectedNode].ExportFile();
        }

        private void savePackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Work in progress!");
            SavePackageForm savePackageForm = new SavePackageForm();
            savePackageForm.Show();
        }
    }
}
