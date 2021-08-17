using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public static class Utility
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

        public static ushort ReadUInt16BigEndian(byte[] input, int pos)
        {
            return (ushort)(input[pos + 1] + (input[pos] << 8));
        }

        public static int ReadInt32BigEndian(byte[] input, int pos)
        {
            return (input[pos + 3]) + (input[pos + 2] << 8) + (input[pos + 1] << 16) + (input[pos] << 24);
        }

        public static uint ReadUInt32BigEndian(byte[] input, int pos)
        {
            return ((uint)input[pos + 3]) + ((uint)input[pos + 2] << 8) + ((uint)input[pos + 1] << 16) + ((uint)input[pos] << 24);
        }

        public static ulong ReadUInt64BigEndian(byte[] input, int pos)
        {
            return ((ulong)input[pos + 7]) + ((ulong)input[pos + 6] << 8) + ((ulong)input[pos + 5] << 16) + ((ulong)input[pos + 4] << 24) + ((ulong)input[pos + 3] << 32) + ((ulong)input[pos + 2] << 40) + ((ulong)input[pos + 1] << 48) + ((ulong)input[pos] << 56);
        }

        public static float ReadSingleBigEndian(byte[] input, int pos)
        {
            byte[] bytes = new byte[] { input[pos + 3], input[pos + 2], input[pos + 1], input[pos] };
            return BitConverter.ToSingle(bytes, 0);
        }

        public static long ReverseEndianLong(long input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            Array.Reverse(bytes, 0, bytes.Length);

            return BitConverter.ToInt64(bytes, 0);
        }

        public static ulong ReverseEndianULong(ulong input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            Array.Reverse(bytes, 0, bytes.Length);

            return BitConverter.ToUInt64(bytes, 0);
        }

        public static float ReverseEndianSingle(float input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            Array.Reverse(bytes, 0, bytes.Length);

            return BitConverter.ToSingle(bytes, 0);
        }

        public static float ReadFloat(byte[] source, int offset, bool isBigEndian)
        {

            if (!BitConverter.IsLittleEndian)
            {
                if (isBigEndian)
                {
                    return BitConverter.ToSingle(source, offset);
                }
                else
                {
                    byte[] bytes = new byte[] { source[offset + 3], source[offset + 2], source[offset + 1], source[offset] };
                    return BitConverter.ToSingle(bytes, 0);
                }
            }
            else
            {
                if (isBigEndian)
                {
                    byte[] bytes = new byte[] { source[offset + 3], source[offset + 2], source[offset + 1], source[offset] };
                    return BitConverter.ToSingle(bytes, 0);
                }
                else
                {
                    return BitConverter.ToSingle(source, offset);
                }
            }
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
        public static void AddUIntToList(List<byte> list, uint integer)
        {
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

        public static uint Get_FNV_1_Hash(string input)
        {

            uint hash = 2166136261;

            char[] chars = input.ToCharArray();

            foreach (char c in chars)
            {
                hash = hash * 16777619;
                hash = hash ^ (byte)c;
            }
            return hash;
        }

       

        public static char CharFromMSADSChar(byte b)
        {
            switch (b)
            {
                case 0x00: return ' ';
                case 0x01: return 'e';
                case 0x02: return (char)0x00;
                case 0x03: return 'a';
                case 0x04: return 'n';
                case 0x05: return 't';
                case 0x06: return 'i';
                case 0x07: return 'r';
                case 0x08: return 'o';
                case 0x09: return 's';
                case 0x0A: return '\n';
                case 0x0B: return 'l';
                case 0x0C: return 'd';
                case 0x0D: return 'u';
                case 0x0E: return 'm';
                case 0x0F: return 'g';
                case 0x10: return 'c';
                case 0x11: return '.';
                case 0x12: return 'h';
                case 0x13: return 'p';

                case 0x16: return 'k';
                case 0x17: return 'v';
                case 0x18: return 'b';
                case 0x19: return 'f';

                case 0x1B: return '!';

                case 0x1D: return 'y';
                case 0x1E: return 'j';

                case 0x21: return 'z';

                case 0x23: return 'w';
                case 0x24: return 'D';
                case 0x25: return 'S';
                case 0x26: return 'A';
                case 0x27: return 'T';
                case 0x28: return 'E';
                case 0x29: return '?';
                case 0x2A: return 'H';
                case 0x2B: return 'M';
                case 0x2C: return 'I';
                case 0x2D: return 'q';
                case 0x2E: return '\'';
                case 0x2F: return 'C';
                case 0x30: return 'L';
                case 0x31: return 'J';

                case 0x34: return 'P';

                case 0x36: return 'O';

                case 0x38: return 'N';

                case 0x3A: return 'R';
                case 0x3B: return 'V';
                case 0x3C: return 'B';
                case 0x3D: return '-';

                case 0x3F: return 'G';

                case 0x42: return 'F';

                case 0x44: return 'ö';

                case 0x48: return 'K';
                case 0x49: return 'W';

                case 0x4B: return 'x';

                case 0x61: return 'U';

                case 0x6C: return 'Q';
                case 0x6D: return 'Y';

                case 0x7B: return ':';

                case 0x90: return 'Z';

                case 0xA5: return 'ᴹ';

                case 0xC2: return 'X';

                case 0xC4: return 'ᵀ';

                default: Console.WriteLine("Couldn't translate MSADS char: "+b);  return (char)b;
            }
        }
    }
}
