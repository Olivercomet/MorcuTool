using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MorcuTool
{
    public class Subfile
    {
        public byte[] filebytes = new byte[0]; //will only contain bytes if the file has been exported or modified by the user.

        public ulong hash;
        public uint fileoffset;
        public uint filesize;
        public string filename;

        public string hashString;

        public bool has_been_decompressed = true;
        public bool should_be_compressed_when_in_package = false;

        public uint uncompressedsize; //only used by compressed files

        public uint typeID;
        public uint groupID;

        public string fileextension = "";

        public TreeNode treeNode;

        public hkxFile hkx; //if needed
        public MsaCollision msaCol; //if needed
        public LLMF llmf; //if needed
        public RevoModel rmdl; //if needed

        public void Load()
        {
            using (BinaryReader reader = new BinaryReader(File.Open(global.activePackage.filename, FileMode.Open)))
            {
                reader.BaseStream.Position = fileoffset;

                filebytes = reader.ReadBytes((int)filesize);

                if (uncompressedsize > 0) //if it's a compressed file
                {
                    filebytes = utility.Decompress_QFS(filebytes);
                    has_been_decompressed = true;
                }

                if (fileextension == ".tpl")
                {
                    filebytes = imageTools.ConvertToTPL(filename, filebytes).ToArray();
                }


                switch (typeID)
                {
                    case 0xD5988020:  //MySims Kingdom HKX 
                        hkx = new hkxFile(this);
                        break;
                    case 0x1A8FEB14:  //MySims Agents mesh collision
                        msaCol = new MsaCollision(this);
                        File.WriteAllLines(filename+".obj",msaCol.obj);
                        break;
                    case 0xA5DCD485:                     //LLMF level bin MSA
                    case 0x58969018:                     //LLMF level bin MSK   "LevelData"
                        llmf = new LLMF(this);
                        llmf.GenerateReport();
                        break;
                    case 0x2954E734:          //RMDL MSA     
                    case 0xF9E50586:          //RMDL MSK   
                        rmdl = new RevoModel(this);
                        rmdl.GenerateObj();
                        break;
                }
            }
        }

        public void Unload() {
            filebytes = new byte[0];
        }

        public void ExportFile(bool silent, string silentPath)
        {

            if (filebytes == null || filebytes.Length == 0)
            {
                Load();
            }

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            if (!silent)
            {
                saveFileDialog1.FileName = Path.GetFileName(filename);

                saveFileDialog1.Title = "Export " + Path.GetFileName(filename);
                saveFileDialog1.CheckPathExists = true;
                saveFileDialog1.Filter = fileextension.ToUpper() + " file (*" + fileextension + ")|*" + fileextension + "|All files (*.*)|*.*";

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    silentPath = saveFileDialog1.FileName;
                }
            }

            if (silentPath != null && silentPath != "")
            {
                File.WriteAllBytes(silentPath, filebytes);
                if (global.activePackage.date.Year > 1)
                {
                    File.SetLastWriteTime(silentPath, global.activePackage.date);
                }
            }
        }
    }
}
