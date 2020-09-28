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

        public uint uncompressedsize; //only used by compressed files

        public uint typeID;
        public uint groupID;

        public string fileextension = "";

        public TreeNode treeNode;


        public void ExportFile() {

            using (BinaryReader reader = new BinaryReader(File.Open(global.activePackage.filename, FileMode.Open)))
            {
                reader.BaseStream.Position = fileoffset;

                filebytes = reader.ReadBytes((int)filesize);

                if (uncompressedsize > 0) //if it's a compressed file
                {
                    filebytes = utility.Decompress_QFS(filebytes);
                }

                if (fileextension == ".tpl")
                {
                    filebytes = imageTools.ConvertToTPL(filename, filebytes).ToArray();
                }

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.FileName = Path.GetFileName(filename);

                saveFileDialog1.Title = "Export "+ Path.GetFileName(filename);
                saveFileDialog1.CheckPathExists = true;
                saveFileDialog1.Filter = fileextension.ToUpper() +" file (*" + fileextension+")|*"+ fileextension+"|All files (*.*)|*.*";

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(saveFileDialog1.FileName, filebytes);
                    if (global.activePackage.date.Year > 0)
                        {
                        File.SetLastWriteTime(saveFileDialog1.FileName, global.activePackage.date);
                        }
                   
                }
            }
        }

    }
}
