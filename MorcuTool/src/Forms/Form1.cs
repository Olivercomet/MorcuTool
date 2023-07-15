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


        //bone names that have had their FNV-1 32bit hashes identified in model/animation files
        public string[] commonBoneNames = new string[] { "pelvis", "root", "spine0","spine1","spine2", "neck", "head", "l_ankle","r_ankle"};

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
                else if (FileTree.SelectedNode == FileTree.Nodes[0])
                {
                    packageRootContextMenu.Show(Cursor.Position);
                }
            }

            if (treeNodesAndSubfiles.Keys.Contains(FileTree.SelectedNode))
            {
                hashLabel.Text = "Hash: " + treeNodesAndSubfiles[FileTree.SelectedNode].hashString;
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


                uint meshcount = Utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = 0x30; //start of first object

                reader.BaseStream.Position += 0x04;

                reader.BaseStream.Position += 0x0C; //skip weird thing (coords of object?)

                uint unk1offset = 0x30 + Utility.ReverseEndian(reader.ReadUInt32());
                uint unk1size = 0x30 + Utility.ReverseEndian(reader.ReadUInt32());

                uint ObjectListOffset = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                uint ObjectListSize = 0x30 + Utility.ReverseEndian(reader.ReadUInt32());    //this is not actually the size

                reader.BaseStream.Position = unk1offset;

                List<string> outputFile = new List<string>();

                uint unk1count = 0;

                while (reader.BaseStream.Position < unk1offset + unk1size)
                    {
                    reader.BaseStream.Position += 0x04;
                    ushort ID = Utility.ReverseEndianShort(reader.ReadUInt16());
                   // Console.WriteLine("break");
                    reader.BaseStream.Position += 0x0A;

                    float U = Utility.ReverseEndianSingle(reader.ReadSingle());
                    float X = Utility.ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x0C;

                    reader.BaseStream.Position += 0x1C;

                    float Y = Utility.ReverseEndianSingle(reader.ReadSingle());

                    reader.BaseStream.Position += 0x04;

                    float Z = Utility.ReverseEndianSingle(reader.ReadSingle());

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
                
                reader.BaseStream.Position = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                reader.BaseStream.Position += 0x04;
                reader.BaseStream.Position = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;

                if (MorcubusMode)
                {
                    reader.BaseStream.Position += 0x04;
                    reader.BaseStream.Position = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                else
                {
                    reader.BaseStream.Position = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                }
                
                
                while (reader.ReadByte() != 0xEF)
                    {
                    
                    }

                while (reader.ReadByte() == 0xEF)
                    {

                    }

                reader.BaseStream.Position += 0x03;

                uint unk2sectioncount = Utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = Utility.ReverseEndian(reader.ReadUInt32()) -0x20;

                uint VertexCountForStartVertexCalculation = 0;

                for (int i = 0; i < unk2sectioncount; i++)
                    {
                    mdlObject newObject = new mdlObject();
                    reader.BaseStream.Position += 0x1C;
                    newObject.vertexCount = Utility.ReverseEndian(reader.ReadUInt32());
                    newObject.StartingVertexID = VertexCountForStartVertexCalculation;
                    VertexCountForStartVertexCalculation += newObject.vertexCount;
                    newObject.vertexListOffset = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x08;
                    newObject.facesToRemoveOffset = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
                    reader.BaseStream.Position += 0x04;
                    newObject.faceListOffset = Utility.ReverseEndian(reader.ReadUInt32()) - 0x10;
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

                        newVertex.position.x = Utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.position.y = Utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.position.z = Utility.ReverseEndianSingle(reader.ReadSingle());

                        o.vertices.Add(newVertex);

                        reader.BaseStream.Position += 0x10;

                        if (newVertex.UVchannels[0] == null) {
                            newVertex.UVchannels[0] = new MorcuMath.Vector2();
                        }
                        
                        newVertex.UVchannels[0].x = Utility.ReverseEndianSingle(reader.ReadSingle());
                        newVertex.UVchannels[0].y = Utility.ReverseEndianSingle(reader.ReadSingle()) * -1;

                        reader.BaseStream.Position += 0x14;
                    }
                }

                foreach (mdlObject o in objects)
                {
                    reader.BaseStream.Position = o.facesToRemoveOffset;
                    while (reader.ReadByte() != 0xEF)
                    {
                        reader.BaseStream.Position -= 0x01;
                        o.facesToRemove.Add(Utility.ReverseEndianShort(reader.ReadUInt16()));
                    }

                    reader.BaseStream.Position = o.faceListOffset;

                    int count = 0;

                    int facesToRemovePos = 0;

                    uint facesToRemoveCountdown = o.facesToRemove[0];

                    reader.BaseStream.Position += 4; //to prepare for the first 0xEF scouting
                    while (facesToRemoveCountdown != 0 && facesToRemovePos != o.facesToRemove.Count)
                    {
                        reader.BaseStream.Position -= 0x04;
                        
                        Face newface = new Face();
                        newface.v1 = (ushort)(Utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v2 = (ushort)(Utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
                        newface.v3 = (ushort)(Utility.ReverseEndianShort(reader.ReadUInt16()) + 1);
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
                        outputFile.Add("v " + v.position.x + " " + v.position.y + " " + v.position.z);
                        }

                    foreach (Vertex v in o.vertices)
                    {
                        outputFile.Add("vt " + v.UVchannels[0].x + " " + v.UVchannels[0].y);
                    }

                    foreach (Face f in o.faces)
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

            openFileDialog1.Filter = "MySims Series TPL file or fake DDS file (*.tpl, *.dds)|*.tpl;*.dds";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in openFileDialog1.FileNames)
                {
                    File.WriteAllBytes(filename + "new.tpl", imageTools.ConvertToNintendoTPL(filename, File.ReadAllBytes(filename)).ToArray());
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
                OpenFileDialog openFileDialog2 = new OpenFileDialog();

                openFileDialog2.Title = "Select mdl file";
                openFileDialog2.DefaultExt = "mdl";
                openFileDialog2.Filter = "MySims 3d models (*.mdl, *.rmdl)|*.mdl;*.rmdl";
                openFileDialog2.CheckFileExists = true;
                openFileDialog2.CheckPathExists = true;
                openFileDialog2.Multiselect = true;

                bool createdAtLeastOneOBJ = false;

                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filename in openFileDialog2.FileNames)
                    {
                        if (Path.GetExtension(filename) == ".mdl")
                        {
                            ConvertSkyHeroesModel(filename, File.ReadAllBytes(filename));
                        }
                        else {
                            Subfile s = new Subfile();
                            s.filebytes = File.ReadAllBytes(filename);
                            s.rmdl = new RevoModel(s);

                            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                            saveFileDialog1.Title = "Save model";
                            saveFileDialog1.FileName = "";
                            saveFileDialog1.CheckPathExists = true;
                            saveFileDialog1.Filter = "Wavefront OBJ (*.obj)|*.obj";

                            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                                {
                                s.rmdl.GenerateObj(saveFileDialog1.FileName);
                                createdAtLeastOneOBJ = true;
                                }
                        }
                    }
                MessageBox.Show("Created .OBJ model(s) in same directory as original file.");
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
                    File.WriteAllBytes(filename+"_decomp" , Compression.Decompress_QFS(File.ReadAllBytes(filename)).ToArray());
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

            FileTree.Sort();
            FileTree.CollapseAll();
            FileTree.EndUpdate();
        }

        private void exportSubfile_Click(object sender, EventArgs e)
        {
            treeNodesAndSubfiles[FileTree.SelectedNode].ExportFile(false, "");
        }

        private void exportAllContextMenuStripButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Title = "Select destination folder";
            saveFileDialog1.FileName = "Save here";
            saveFileDialog1.CheckPathExists = false;
            saveFileDialog1.CheckFileExists = false;
            saveFileDialog1.Filter = "Directory |directory";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

                foreach (TreeNode node in FileTree.Nodes[0].Nodes) {

                    bool was_loaded_already = false;
                    Subfile target = treeNodesAndSubfiles[node];

                    if (target.filebytes == null || target.filebytes.Length == 0) {
                        target.Load();
                    } else
                    {
                        was_loaded_already = true;
                    }

                    string newname = target.filename;

                    if (target.rmdl != null) {
                        newname = target.filename.Replace(".rmdl", ".obj");
                    }

                    target.ExportFile(true,Path.Combine(Path.GetDirectoryName(saveFileDialog1.FileName),newname));

                    if (!was_loaded_already) {
                        target.Unload();
                    }
                }
            }

            MessageBox.Show("Export complete.");
        }

        private void savePackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Work in progress!");
            SavePackageForm savePackageForm = new SavePackageForm();
            savePackageForm.Show();
        }

        private void findByHashButton_Click(object sender, EventArgs e)
        {
            List<string> found = new List<string>();
            List<Subfile> actualsubfiles = new List<Subfile>();

            found.Add("Found the following files with similar hashes:");

            foreach (Subfile s in global.activePackage.subfiles) {
                if (s.hashString.Contains(findByHashTextBox.Text)) {
                    found.Add(s.filename);
                    actualsubfiles.Add(s);
                }
            }

            if (found.Count > 1) {
                string message = "";

                foreach (string s in found) {
                    message += s + "\n";
                }

                MessageBox.Show(message + "\n\nTaking you to the first result.");

                    foreach (TreeNode node in FileTree.Nodes[0].Nodes) {

                        if (treeNodesAndSubfiles[node] == actualsubfiles[0]) {
                            FileTree.SelectedNode = node;
                            FileTree.SelectedNode.EnsureVisible();
                            break;
                        }
                    }

                return;
            }


            MessageBox.Show("Not found");
        }

        private void vaultSearchButton_Click(object sender, EventArgs e)
        {
            if (global.activeVault == null){
                return;
            }

            foreach (ulong hash in global.activeVault.VaultHashesAndFileNames.Keys) {

                if (hash == ulong.Parse(vaultSearchTextBox.Text))
                    {
                    MessageBox.Show("Match found: "+ global.activeVault.VaultHashesAndFileNames[hash]);
                    break;
                    }
            }
        }

        private void hashLabel_Click(object sender, EventArgs e)
        {

        }



        public void backtrackSubfile(Subfile s) {

            Console.WriteLine("Backtracking " + s.filename);

            switch ((global.TypeID)s.typeID)
            {
                case global.TypeID.TPL_MSK:
                case global.TypeID.TPL_MSA:
                    foreach (Subfile superior in global.activePackage.subfiles)
                    {
                        //look for MATDs that reference this texture

                            if ((global.TypeID)superior.typeID == global.TypeID.MATD_MSK || (global.TypeID)superior.typeID == global.TypeID.MATD_MSA)
                            {
                            superior.Load();
                            foreach (MaterialData.Param param in superior.matd.parameters) {
                                if (param.paramType == MaterialData.MaterialParameter.diffuseMap && param.diffuse_texture == s)
                                {
                                    backtrackSubfile(superior);
                                    return;
                                }
                            }
                            superior.Unload();
                        }  
                    }
                    break;
                case global.TypeID.MTST_MSK:
                case global.TypeID.MTST_MSA:
                    foreach (Subfile superior in global.activePackage.subfiles)
                    {
                        //look for RMDLs that reference this materialset

                        if ((global.TypeID)superior.typeID == global.TypeID.RMDL_MSK || (global.TypeID)superior.typeID == global.TypeID.RMDL_MSA)
                        {
                            superior.Load();

                                foreach (RevoModel.Mesh m in superior.rmdl.meshes)
                                {
                                    if (m.hash_of_material == s.hash)
                                    {
                                        MessageBox.Show("Succesfully backtracked to model: " + superior.filename);

                                        foreach (TreeNode node in FileTree.Nodes[0].Nodes)
                                        {
                                            if (treeNodesAndSubfiles[node] == superior)
                                            {
                                                FileTree.SelectedNode = node;
                                                break;
                                            }
                                        }
                                        FileTree.SelectedNode.EnsureVisible();
                                        return;
                                    }
                                }

                            superior.Unload();
                        }
                    }
                    break;
                case global.TypeID.MATD_MSK:
                case global.TypeID.MATD_MSA:
                    foreach (Subfile superior in global.activePackage.subfiles)
                    {
                        //look for RMDLs and MTSTs that reference this material

                       if ((global.TypeID)superior.typeID == global.TypeID.RMDL_MSK || (global.TypeID)superior.typeID == global.TypeID.RMDL_MSA 
                           || (global.TypeID)superior.typeID == global.TypeID.MTST_MSK || (global.TypeID)superior.typeID == global.TypeID.MTST_MSA)
                        {
                            superior.Load();

                            if (superior.rmdl != null)
                            {
                                foreach (RevoModel.Mesh m in superior.rmdl.meshes)
                                {
                                    if (m.materials.Contains(s.matd))
                                    {
                                        MessageBox.Show("Succesfully backtracked to model: " + superior.filename);

                                        foreach (TreeNode node in FileTree.Nodes[0].Nodes)
                                        {
                                            if (treeNodesAndSubfiles[node] == superior)
                                            {
                                                FileTree.SelectedNode = node;
                                                break;
                                            }
                                        }
                                        FileTree.SelectedNode.EnsureVisible();
                                        return;
                                    }
                                }
                            }
                            else if (superior.mtst != null){

                                if (superior.mtst.mats.Contains(s.matd)){
                                    backtrackSubfile(superior);
                                    return;
                                }
                            }

                            superior.Unload();
                        }
                    }
                    break;
                default:
                    MessageBox.Show("Sorry, that type of file is not applicable for backtracking.");
                    break;
            }

        }

        private void compressToQFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog2 = new OpenFileDialog();

            openFileDialog2.Title = "Select file to QFS compress";
            openFileDialog2.CheckFileExists = true;
            openFileDialog2.CheckPathExists = true;

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in openFileDialog2.FileNames)
                {
                    File.WriteAllBytes(filename + "_comp", Compression.Compress_QFS(File.ReadAllBytes(filename)));
                }
            }
        }

        private void backtrackToModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Subfile s = treeNodesAndSubfiles[FileTree.SelectedNode];
            s.Load();
            backtrackSubfile(s);
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            Subfile s = treeNodesAndSubfiles[FileTree.SelectedNode];
            s.Load();

            openFileDialog1.Title = "Replace "+ s.filename;
            openFileDialog1.Filter = "All files (*.*)|*.*";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                s.filebytes = File.ReadAllBytes(openFileDialog1.FileName);
            }
        }

        private void mSGTextEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MSGtexteditor textEdit = new MSGtexteditor();
            textEdit.Show();

        }
    }
}