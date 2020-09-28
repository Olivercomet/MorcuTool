using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MorcuTool
{
    public static class imageTools
    {
        public static List<byte> ConvertToTPL(string filename, byte[] file)
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

                if (global.activePackage.packageType == Package.PackageType.SkyHeroes && utility.ReverseEndian(reader.ReadUInt32()) != 0)        //skip those annoying extra headers from MySims SkyHeroes
                {
                    startoffile = 0x50;
                }

                reader.BaseStream.Position = startoffile;

                if (global.activePackage.packageType == Package.PackageType.Agents || global.activePackage.packageType == Package.PackageType.Kingdom)
                {
                    magic = utility.ReverseEndian(reader.ReadUInt32());
                    flags = utility.ReverseEndian(reader.ReadUInt32());
                    imagesize = utility.ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x0C;

                    width = utility.ReverseEndianShort(reader.ReadUInt16());
                    height = utility.ReverseEndianShort(reader.ReadUInt16());

                    imageformat = utility.ReverseEndian(reader.ReadUInt32());
                    imagecount = utility.ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x14;

                    imageoffset = utility.ReverseEndian(reader.ReadUInt32());
                }
                else if (global.activePackage.packageType == Package.PackageType.SkyHeroes)
                {
                    magic = utility.ReverseEndian(reader.ReadUInt32());
                    flags = utility.ReverseEndian(reader.ReadUInt32());

                    reader.BaseStream.Position += 0x06;
                    width = utility.ReverseEndianShort(reader.ReadUInt16());

                    reader.BaseStream.Position += 0x02;
                    height = utility.ReverseEndianShort(reader.ReadUInt16());

                    reader.BaseStream.Position += 0x28;

                    imageformat = utility.ReverseEndian(reader.ReadUInt32());

                    imageoffset = ((uint)reader.BaseStream.Position + 0x20) - startoffile;
                }


                reader.BaseStream.Position = startoffile + imageoffset;

                if (global.activePackage.packageType == Package.PackageType.SkyHeroes)
                {
                    imagesize = (uint)((reader.BaseStream.Length - startoffile) - (reader.BaseStream.Position - startoffile));
                }

                List<Byte[]> imagesubblocks = new List<Byte[]>();


                for (int i = 0; i < imagesize / 8; i++)
                {
                    imagesubblocks.Add(BitConverter.GetBytes(reader.ReadUInt64()));
                }

                //create tpl file byte array

                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x0020AF30)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000001)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x0000000C)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000014)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(0x00000000)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndianShort(height)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndianShort(width)));
                output.AddRange(BitConverter.GetBytes(utility.ReverseEndian(imageformat)));
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

                for (int i = 0; i < imagesize / 8; i++)
                {
                    output.AddRange(imagesubblocks[i]);
                }
            }

            File.Delete(filename + ".tpltemp");
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
