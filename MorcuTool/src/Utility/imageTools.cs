using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MorcuTool
{
    public static class imageTools
    {

        public static Color ReadRGBA24(byte[] bytes, int pos) {

            int R = bytes[pos] & 0xFC;
            int G = ((bytes[pos] & 0x03) << 6) | ((bytes[pos + 1] & 0xF0) >> 2);
            int B = ((bytes[pos+1] & 0x0F) << 4) | ((bytes[pos + 2] & 0xC0) >> 6);
            int A = bytes[pos+2] & 0x3F;

            return Color.FromArgb(A, R, G, B);
        }

        public static Color ToRGB565(ushort input)
        {
            int R = ((input >> 11) & 0x1f) << 3;
            int G = ((input >> 5) & 0x3F) << 2;
            int B = (input & 0x1F) << 3;
            int A = 255;

            return Color.FromArgb(A, R, G, B);
        }

        public static List<byte> ConvertToTPL(string filename, byte[] file)
        {
            List<byte> output = new List<byte>();

                uint magic = 0;
                uint flags = 0;

                byte version = file[5];

                int startoffile = 0;
                
                uint imagesize = 0;

                ushort width = 0;
                ushort height = 0;

                byte imageformat = 0;
                uint imagecount = 0;

                int imageoffset = 0;

                int pos = 0;

                if (global.activePackage.packageType == Package.PackageType.SkyHeroes && utility.ReverseEndian(BitConverter.ToUInt32(file,pos)) != 0)        //skip those annoying extra headers from MySims SkyHeroes
                {
                    startoffile = 0x50;
                }

                pos = startoffile;

            if (global.activePackage.packageType == Package.PackageType.Agents || global.activePackage.packageType == Package.PackageType.Kingdom)
                {
                    magic = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                    pos+=4;
                    flags = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                    pos+=4;

                    if (version == 1)   //MYSIMS
                        {
                        pos = 0x1C;

                        width = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                        pos += 2;
                        height = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                        pos += 5;

                        imageformat = file[pos];
                        pos++;
                        imagecount = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                        pos += 4;

                        pos += 0x14;

                        imageoffset = 0x4C;
                        pos += 4;

                        imagesize = (uint)(height * width * 4);
                        }
                     else if (version == 2)  //MYSIMS
                        {
                        pos = 0x1C;

                        width = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                        pos += 2;
                        height = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                        pos += 5;

                        imageformat = file[pos];
                        pos++;
                        imagecount = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                        pos += 4;

                        pos += 0x14;

                        imageoffset = 0x4C;
                        pos += 4;

                        imagesize = (uint)(height * width * 4);
                        }
                    else if (version == 3)   //MYSIMS KINGDOM AND AGENTS
                        {
                        imagesize = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                        pos += 4;

                        pos += 0x0C;

                        width = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                        pos += 2;
                        height = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                        pos += 5;

                        imageformat = file[pos];
                        pos ++;
                        imagecount = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                        pos += 4;

                        pos += 0x14;

                        imageoffset = utility.ReverseEndianSigned(BitConverter.ToInt32(file, pos));
                        pos += 4;
                        }
                }
                else if (global.activePackage.packageType == Package.PackageType.SkyHeroes)
                {
                    magic = utility.ReverseEndian(BitConverter.ToUInt32(file,pos));
                    pos += 4;
                    flags = utility.ReverseEndian(BitConverter.ToUInt32(file, pos));
                    pos += 4;

                    pos += 6;
                    width = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));
                    pos += 2;

                    pos += 2;
                    height = utility.ReverseEndianShort(BitConverter.ToUInt16(file, pos));

                    pos += 0x2D;

                    imageformat = file[pos];
                    pos += 1;

                    imageoffset = (pos + 0x20) - startoffile;
                }   


                pos = startoffile + imageoffset;

                if (global.activePackage.packageType == Package.PackageType.SkyHeroes)
                {
                    imagesize = (uint)((file.Length - startoffile) - (pos - startoffile));
                }

                byte[] imageData = new byte[file.Length - pos];

                Array.Copy(file,pos,imageData,0,file.Length-pos);

                //create tpl file byte array

                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x0020AF30)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000001)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x0000000C)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000014)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndianShort(height)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndianShort(width)));
                output.Add(0x00);
                output.Add(0x00);
                output.Add(0x00);
                output.Add(imageformat);
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000060)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000001)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000001)));

                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));

                output.AddRange(imageData);

            return output;
        }


        public static void TPLToMySimsTPL(string filename)
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

                magic = utility.ReverseEndian(reader.ReadUInt32());
                numberofimages = utility.ReverseEndian(reader.ReadUInt32());
                imagetableoffset = utility.ReverseEndian(reader.ReadUInt32());

                imageheaderoffset = utility.ReverseEndian(reader.ReadUInt32());
                paletteheaderoffset = utility.ReverseEndian(reader.ReadUInt32());

                reader.BaseStream.Position = startoffile + imageheaderoffset;

                height = utility.ReverseEndianShort(reader.ReadUInt16());
                width = utility.ReverseEndianShort(reader.ReadUInt16());
                imageformat = utility.ReverseEndian(reader.ReadUInt32());
                imagedataoffset = utility.ReverseEndian(reader.ReadUInt32());
                wrapS = utility.ReverseEndian(reader.ReadUInt32());
                wrapT = utility.ReverseEndian(reader.ReadUInt32());
                minfilter = utility.ReverseEndian(reader.ReadUInt32());
                magfilter = utility.ReverseEndian(reader.ReadUInt32());




                reader.BaseStream.Position = startoffile + imagedataoffset;

                List<Byte[]> imagesubblocks = new List<Byte[]>();

                for (int i = 0; i < ((reader.BaseStream.Length - imagedataoffset) / 8); i++)
                {
                    imagesubblocks.Add(BitConverter.GetBytes(reader.ReadUInt64()));
                }

                using (BinaryWriter writer = new BinaryWriter(File.Open(filename + "2.tpl", FileMode.Create)))
                {

                    writer.Write(utility.ReverseEndian(0x14FE0149));
                    writer.Write(utility.ReverseEndian(0x00030000));
                    writer.Write(utility.ReverseEndian((uint)(reader.BaseStream.Length - imagedataoffset)));

                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);

                    writer.Write(utility.ReverseEndianShort(width));
                    writer.Write(utility.ReverseEndianShort(height));

                    writer.Write(utility.ReverseEndian(imageformat));


                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(utility.ReverseEndian(1));
                    writer.Write(utility.ReverseEndian(1));
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(utility.ReverseEndian(0x40));
                    writer.Write(utility.ReverseEndian((uint)(reader.BaseStream.Length - imagedataoffset)));


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

    }
}
