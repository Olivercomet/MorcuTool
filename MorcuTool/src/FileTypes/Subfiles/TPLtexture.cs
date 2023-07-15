using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MorcuTool
{
    public class TPLtexture
    {
        uint magic;
        uint flags;
        byte version;

        ImageFormat imageformat;
        uint imagecount = 0;

        int startoffile = 0;

        public List<Image> images = new List<Image>();

        public enum ImageFormat {        
        I4 = 0x00,
        I8 = 0x01,
        IA4 = 0x02,
        IA8 = 0x03,
        RGB565 = 0x04,
        RGB5A3 = 0x05,
        RGBA32 = 0x06,
        CI4 = 0x08,
        CI8 = 0x09,
        C14X2 = 0x0A,
        CMPR = 0x0E
        }

        public TPLtexture(Subfile basis) {

            uint imagesize = 0;

            ushort width = 0;
            ushort height = 0;
            version = basis.filebytes[5];

            int imageoffset = 0;

            int pos = 0;

            if (global.activePackage.packageType == Package.PackageType.SkyHeroes && Utility.ReadUInt32BigEndian(basis.filebytes,pos) != 0)        //skip those annoying extra headers from MySims SkyHeroes
            {
                startoffile = 0x50;
            }

            pos = startoffile;

            if (global.activePackage.packageType == Package.PackageType.Agents || global.activePackage.packageType == Package.PackageType.Kingdom)
            {
                magic = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                pos += 4;

                if (magic != 0x14FE0149) {
                    // That's not actually a TPL image!! It's probably a .DDS, and will be detected and exported as such if the user exports the file.
                    return;
                }

                flags = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                pos += 4;

                if (version == 1)   //MYSIMS
                {
                    pos = 0x1C;

                    width = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                    pos += 2;
                    height = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                    pos += 5;

                    imageformat = (ImageFormat)basis.filebytes[pos];
                    pos++;
                    imagecount = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                    pos += 4;

                    pos += 0x14;

                    imageoffset = 0x4C;
                    pos += 4;

                    imagesize = (uint)(height * width * 4);
                }
                else if (version == 2)  //MYSIMS
                {
                    pos = 0x1C;

                    width = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                    pos += 2;
                    height = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                    pos += 5;

                    imageformat = (ImageFormat)basis.filebytes[pos];
                    pos++;
                    imagecount = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                    pos += 4;

                    pos += 0x14;

                    imageoffset = 0x4C;
                    pos += 4;

                    imagesize = (uint)(height * width * 4);
                }
                else if (version == 3)   //MYSIMS KINGDOM AND AGENTS
                {
                    imagesize = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                    pos += 4;

                    pos += 0x0C;

                    width = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                    pos += 2;
                    height = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                    pos += 5;

                    imageformat = (ImageFormat)basis.filebytes[pos];
                    pos++;
                    imagecount = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                    pos += 4;

                    pos += 0x14;

                    imageoffset = Utility.ReadInt32BigEndian(basis.filebytes, pos);
                    pos += 4;
                }
            }
            else if (global.activePackage.packageType == Package.PackageType.SkyHeroes)
            {
                magic = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                pos += 4;
                flags = Utility.ReadUInt32BigEndian(basis.filebytes,pos);
                pos += 4;

                pos += 6;
                width = Utility.ReadUInt16BigEndian(basis.filebytes, pos);
                pos += 2;

                pos += 2;
                height = Utility.ReadUInt16BigEndian(basis.filebytes, pos);

                pos += 0x2D;

                imageformat = (ImageFormat)basis.filebytes[pos];
                pos += 1;

                imageoffset = (pos + 0x20) - startoffile;
            }

            if (imagecount == 0) {
                imagecount = 1;
            }

            pos = startoffile + imageoffset;

            if (global.activePackage.packageType == Package.PackageType.SkyHeroes)
            {
                imagesize = (uint)((basis.filebytes.Length - startoffile) - (pos - startoffile));
            }

            while ((width % 8) != 0)
            {
                width++;
            }

            while ((height % 8) != 0) {
                height++;
            }

            for (int img = 0; img < imagecount; img++)
            {
                Bitmap newImage = new Bitmap(width, height);

                switch (imageformat) {

                    case ImageFormat.CMPR:
                        for (int y = 0; y < height; y += 8)
                        {
                            for (int x = 0; x < width; x += 8)
                            {
                                for (int j = 0; j < 4; j++) {

                                    if ((j == 1 || j == 3) && ((x + 4) >= width)) {
                                        continue;
                                    }
                                    if ((j == 2 || j == 3) && ((y + 4) >= height)) {
                                        continue;
                                    }

                                    Color[] cols = new Color[4];

                                    ushort col0ushort = Utility.ReadUInt16BigEndian(basis.filebytes, pos); pos += 2;
                                    ushort col1ushort = Utility.ReadUInt16BigEndian(basis.filebytes, pos); pos += 2;
                                    cols[0] = imageTools.ToRGB565(col0ushort);
                                    cols[1] = imageTools.ToRGB565(col1ushort);

                                    cols[2] = new Color();
                                    cols[3] = new Color();

                                    if (col0ushort > col1ushort)
                                    {
                                        cols[2] = Color.FromArgb(255, ((2 * cols[0].R) + (cols[1].R)) / 3, ((2 * cols[0].G) + (cols[1].G)) / 3, ((2 * cols[0].B) + (cols[1].B)) / 3);
                                        cols[3] = Color.FromArgb(255, ((2 * cols[1].R) + (cols[0].R)) / 3, ((2 * cols[1].G) + (cols[0].G)) / 3, ((2 * cols[1].B) + (cols[0].B)) / 3);
                                    }
                                    else
                                    {
                                        cols[2] = Color.FromArgb(255, (cols[0].R + cols[1].R) / 2, (cols[0].G + cols[1].G) / 2, (cols[0].B + cols[1].B) / 2);
                                        cols[3] = Color.FromArgb(0, 0, 0, 0);
                                    }

                                    uint indices = Utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;

                                    int xOffset = 0;
                                    int yOffset = 0;

                                    switch (j) {
                                        case 0: xOffset = 0; yOffset = 0; break;
                                        case 1: xOffset = 4; yOffset = 0; break;
                                        case 2: xOffset = 0; yOffset = 4; break;
                                        case 3: xOffset = 4; yOffset = 4; break;
                                    }

                                    int xOffsetLimit = xOffset + 3;


                                    for (int i = 30; i >= 0; i -= 2)
                                    {
                                        newImage.SetPixel(x + xOffset, y + yOffset, cols[(indices >> i) & 0x03]);
                                        xOffset++;
                                        if (xOffset > xOffsetLimit)
                                        {
                                            xOffset -= 4;
                                            yOffset++;
                                        }
                                    }
                                }

                            }
                        }
                        break;
                    case ImageFormat.RGBA32:
                        for (int y = 0; y < height; y+=4)
                        {
                            for (int x = 0; x < width; x+=4)
                            {
                                int xOffset = 0;
                                int yOffset = 0;
                                for (int j = 0; j < 16; j++) {
     
                                    newImage.SetPixel(x+xOffset, y+yOffset, imageTools.ReadNintendoRGBA32(basis.filebytes, pos));
                                    pos += 2;
                                    xOffset++;
                                    
                                    if (xOffset > 3)
                                    {
                                        xOffset = 0;
                                        yOffset++;
                                    }
                                }

                                pos += 0x20;
                            }
                        }
                        break;
                    default:
                        Console.WriteLine("That image format was not handled by the switch statement! Image format was: " + imageformat);
                        break;
                    }
                images.Add(newImage);
            }
            }
    }
}
