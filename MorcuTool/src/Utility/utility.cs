using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public static class utility
    {

        public static bool send_unfinished_notification = true;

        public static ushort ReverseEndianShort(ushort input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if (isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //   }

            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReverseEndian(uint input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if (isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //    }

            return BitConverter.ToUInt32(bytes, 0);
        }

        public static int ReverseEndianSigned(int input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if (isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //    }

            return BitConverter.ToInt32(bytes, 0);
        }


        public static long ReverseEndianLong(long input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if(isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //   }


            return BitConverter.ToInt64(bytes, 0);
        }

        public static ulong ReverseEndianULong(ulong input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            // if(isMSA.Checked || isSkyHeroes.Checked)
            //    {
            Array.Reverse(bytes, 0, bytes.Length);
            //   }


            return BitConverter.ToUInt64(bytes, 0);
        }

        public static float ReverseEndianSingle(float input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            Array.Reverse(bytes, 0, bytes.Length);

            return BitConverter.ToSingle(bytes, 0);
        }


        public static void AddULongToList(List<byte> list, ulong input)
        {
            list.Add((byte)input);
            list.Add((byte)(input >> 8));
            list.Add((byte)(input >> 16));
            list.Add((byte)(input >> 24));
            list.Add((byte)(input >> 32));
            list.Add((byte)(input >> 40));
            list.Add((byte)(input >> 48));
            list.Add((byte)(input >> 56));
        }

        public static void AddLongToList(List<byte> list, long input)
        {
            list.Add((byte)input);
            list.Add((byte)(input >> 8));
            list.Add((byte)(input >> 16));
            list.Add((byte)(input >> 24));
            list.Add((byte)(input >> 32));
            list.Add((byte)(input >> 40));
            list.Add((byte)(input >> 48));
            list.Add((byte)(input >> 56));
        }
        public static void AddUIntToList(List<byte> list, uint integer) {
            list.Add((byte)integer);
            list.Add((byte)(integer >> 8));
            list.Add((byte)(integer >> 16));
            list.Add((byte)(integer >> 24));
        }


        public static void OverWriteUIntInList(List<byte> list, int pos, uint integer)
        {
            list[pos] = ((byte)integer);
            list[pos + 1] = (byte)(integer >> 8);
            list[pos + 2] = (byte)(integer >> 16);
            list[pos + 3] = (byte)(integer >> 24);
        }

        public static void AddIntToList(List<byte> list, int integer)
        {
            list.Add((byte)integer);
            list.Add((byte)(integer >> 8));
            list.Add((byte)(integer >> 16));
            list.Add((byte)(integer >> 24));
        }

        public static void AddUShortToList(List<byte> list, ushort input)
        {
            list.Add((byte)input);
            list.Add((byte)(input >> 8));
        }
        public static void AddShortToList(List<byte> list, short input)
        {
            list.Add((byte)input);
            list.Add((byte)(input >> 8));
        }

        public static byte[] Compress_QFS(byte[] filebytes)
        { 
            if(send_unfinished_notification)
                {
                System.Windows.Forms.MessageBox.Show("NEED TO ADD QFS COMPRESSION FUNCTION HERE");
                send_unfinished_notification = false;
                }
            return filebytes;
        }



        public static byte[] Decompress_QFS(byte[] filebytes)
        {

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
                for (int i = 0; i < numcopy; i++)     //copy bytes from earlier in the output
                {
                    output[output_pos] = output[fromoffset + i];
                    output_pos++;
                }
            }
            return output;
        }
    }
}
