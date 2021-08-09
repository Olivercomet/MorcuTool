using System;
using System.Collections.Generic;
using System.IO;
using UnluacNET;
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
        public MaterialSet mtst; //if needed
        public MaterialData matd; //if needed
        public TPLtexture tpl; //if needed

        public void Load()
        {
            using (BinaryReader reader = new BinaryReader(File.Open(global.activePackage.filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)))
            {
                reader.BaseStream.Position = fileoffset;

                filebytes = reader.ReadBytes((int)filesize);

                if (uncompressedsize > 0) //if it's a compressed file
                {
                    filebytes = Utility.Decompress_QFS(filebytes);
                    has_been_decompressed = true;
                }


                switch ((global.TypeID)typeID)
                {
                    case global.TypeID.HKX_MSK:  //MySims Kingdom HKX 
                        hkx = new hkxFile(this);
                        break;
                    case global.TypeID.COLLISION_MSA:  //MySims Agents mesh collision
                        msaCol = new MsaCollision(this);
                        File.WriteAllLines(filename+".obj",msaCol.obj);
                        break;
                    case global.TypeID.LLMF_MSK:                     //LLMF level bin MSK   "LevelData"
                    case global.TypeID.LLMF_MSA:                     //LLMF level bin MSA
                        llmf = new LLMF(this);
                        llmf.GenerateReport();
                        break;
                    case global.TypeID.RMDL_MSK:          //RMDL MSK   
                    case global.TypeID.RMDL_MSA:          //RMDL MSA     
                        rmdl = new RevoModel(this);
                        break;
                    case global.TypeID.MTST_MSK:          
                    case global.TypeID.MTST_MSA:          
                        mtst = new MaterialSet(this);
                        break;
                    case global.TypeID.MATD_MSK:          //MATD MSK            "MaterialData"
                    case global.TypeID.MATD_MSA:          //MATD MSA    
                        matd = new MaterialData(this);
                        break;
                    case global.TypeID.TPL_MSK:
                    case global.TypeID.TPL_MSA:
                        tpl = new TPLtexture(this);
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

                if (rmdl != null)
                {
                    saveFileDialog1.Filter = "Wavefront OBJ (*.obj)|*.obj|MySims RevoModel (*.rmdl)|*.rmdl";
                    saveFileDialog1.FileName = saveFileDialog1.FileName.Replace(".rmdl", ".obj");
                }
                else if (tpl != null)
                {
                    saveFileDialog1.Filter = "PNG image (*.PNG)|*.png|TPL image (*.tpl)|*.tpl";
                    saveFileDialog1.FileName = saveFileDialog1.FileName.Replace(".tpl", ".png");
                }
                else if (typeID == (uint)global.TypeID.LUAC_MSK || typeID == (uint)global.TypeID.LUAC_MSA)
                {
                    saveFileDialog1.Filter = "Decompiled lua script (*.lua)|*.lua|Compiled lua script (*.luac)|*.luac";
                    saveFileDialog1.FileName = saveFileDialog1.FileName.Replace(".luac", ".lua");
                }
                else
                {
                    saveFileDialog1.Filter = fileextension.ToUpper() + " file (*" + fileextension + ")|*" + fileextension + "|All files (*.*)|*.*";
                }

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    silentPath = saveFileDialog1.FileName;
                }
            }
            else {
                if (tpl != null)
                {
                    silentPath = silentPath.Replace(".tpl", ".png");
                }
                else if (typeID == (uint)global.TypeID.LUAC_MSK || typeID == (uint)global.TypeID.LUAC_MSA)
                {
                    //these MSK hashes are some of the ones that crash the decompiler
                    if (hash != 0x13A985F3E3E05FD1 && hash != 0x1DCDDD2C8D672B03 && hash != 0x225C6F76F6DFE215 || hash != 0x2B19149AE76336EA || hash != 0x13A985F3E3E05FD1)
                    {
                        silentPath = silentPath.Replace(".luac", ".lua");
                    }
                }
            }

            if (silentPath != null && silentPath != "")
            {
                if (Path.GetExtension(silentPath) == ".obj" && (rmdl != null))
                {
                    rmdl.GenerateObj(silentPath);
                }
                else if (Path.GetExtension(silentPath) == ".png" && (tpl != null))
                {
                    if (tpl.images.Count > 1) {
                        for (int i = 0; i < tpl.images.Count; i++)
                        {
                            tpl.images[i].Save(silentPath.Replace(".png", "_" + i + ".png"));
                        }
                    }
                    else {
                        tpl.images[0].Save(silentPath);
                    }
                }
                else if (tpl != null) {
                    File.WriteAllBytes(silentPath, imageTools.ConvertToNintendoTPL(filename, filebytes).ToArray());
                }
                else if ((typeID == (uint)global.TypeID.LUAC_MSK || typeID == (uint)global.TypeID.LUAC_MSA) && Path.GetExtension(silentPath) == ".lua")
                {
                    Console.WriteLine("Trying to decompile " + filename);
                    DecompileLuc(filebytes, silentPath);
                }
                else
                {
                    File.WriteAllBytes(silentPath, filebytes);
                }
                
                if (global.activePackage.date.Year > 1)
                {
                    File.SetLastWriteTime(silentPath, global.activePackage.date);
                }
            }
        }

        public string DecompileLuc(byte[] input, string destfile)
        {
            Stream stream = new MemoryStream(input);

            var header = new BHeader(stream);

            LFunction lmain = header.Function.Parse(stream, header);

            Decompiler d = new Decompiler(lmain);
            d.Decompile();

            using (var writer = new StreamWriter(destfile, false, new UTF8Encoding(false)))
            {
                d.Print(new Output(writer));

                writer.Flush();
            }

            return (null);
        }
    }
}