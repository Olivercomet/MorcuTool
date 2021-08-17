using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class MsaAnimation
    {

        public class Table1Entry {      //setting table 1's count to 1 turns the animation into a T-pose in-game

            public uint unk1;   //start frame, possibly?
            public uint unk2;   //flags? padding? usually 0
            public uint unk3;   //u32 start of this frame's data, relative to the data section
            public FrameData frmData;

            public Table1Entry(byte[] filebytes, int offset, MsaAnimation parent) {
                unk1 = Utility.ReadUInt32BigEndian(filebytes, offset);
                unk2 = Utility.ReadUInt32BigEndian(filebytes, offset + 4);
                unk3 = Utility.ReadUInt32BigEndian(filebytes, offset + 8);
                frmData = new FrameData(filebytes, parent.dataSectionOffset + unk3);
            }
        }


        public class FrameData {

        public FrameData(byte[] filebytes, uint offset) {

            }
        } 

        public class Table2Entry            //setting table 2's count to 1 doesn't seem to change anything in-game
        { 
            public uint unk1;       // u32 flags? padding? usually 0x00000001
            public uint unk2;       // u32 offset to something (relative to data section)
            public uint unk3;       // u32 usually 0 ?
            public uint unk4;       // u32 offset to float data for bone movement

            public Table2Entry(byte[] filebytes, int offset) {

                unk1 = Utility.ReadUInt32BigEndian(filebytes, offset);
                unk2 = Utility.ReadUInt32BigEndian(filebytes, offset + 4);
                unk3 = Utility.ReadUInt32BigEndian(filebytes, offset + 8);
                unk4 = Utility.ReadUInt32BigEndian(filebytes, offset + 12);
            }
        }

        public class BoneTableEntry
        {
            public uint unk1;
            public uint unk2;
            public uint unk3;
            public uint boneNameHash;
            public uint unk4; //usually 1?
            public uint unk5;
            public uint unk6;
            public uint unk7;

            public BoneTableEntry(byte[] filebytes, int offset)
            {
                unk1 = Utility.ReadUInt32BigEndian(filebytes, offset);
                unk2 = Utility.ReadUInt32BigEndian(filebytes, offset+4);
                unk3 = Utility.ReadUInt32BigEndian(filebytes, offset+8);
                boneNameHash = Utility.ReadUInt32BigEndian(filebytes, offset+12);
                unk4 = Utility.ReadUInt32BigEndian(filebytes, offset+16);
                unk5 = Utility.ReadUInt32BigEndian(filebytes, offset+20);
                unk6 = Utility.ReadUInt32BigEndian(filebytes, offset+24);
                unk7 = Utility.ReadUInt32BigEndian(filebytes, offset+28);
                Console.WriteLine("Found bone: " + boneNameHash);
            }
        }

        public uint dataSectionOffset;
        public uint boneTableRelativeOffset;
        public uint firstTableOffset;
        public uint firstTableCount;
        public uint secondTableOffset;
        public uint secondTableCount;

        public List<Table1Entry> table1 = new List<Table1Entry>();
        public List<Table2Entry> table2 = new List<Table2Entry>();
        public List<BoneTableEntry> boneTable = new List<BoneTableEntry>();

        public MsaAnimation(Subfile basis) {

            dataSectionOffset = Utility.ReadUInt32BigEndian(basis.filebytes,0x6C);
            boneTableRelativeOffset = Utility.ReadUInt32BigEndian(basis.filebytes, 0x40);

            firstTableOffset = Utility.ReadUInt32BigEndian(basis.filebytes, 0x84);
            firstTableCount = Utility.ReadUInt32BigEndian(basis.filebytes, 0x88);
            secondTableOffset = Utility.ReadUInt32BigEndian(basis.filebytes, 0x8C);
            secondTableCount = Utility.ReadUInt32BigEndian(basis.filebytes, 0x90);

            for (int i = 0; i < firstTableCount; i++){
                table1.Add(new Table1Entry(basis.filebytes, (int)(firstTableOffset + (i * 0x0C)),this));
              }

            for (int i = 0; i < secondTableCount; i++){
                table2.Add(new Table2Entry(basis.filebytes, (int)(secondTableOffset + (i * 0x0C))));
              }

            uint boneTableCount = Utility.ReadUInt32BigEndian(basis.filebytes, (int)(dataSectionOffset + boneTableRelativeOffset + 0x80));

            for (int i = 0; i < boneTableCount; i++) {
                boneTable.Add(new BoneTableEntry(basis.filebytes, (int)(dataSectionOffset + boneTableRelativeOffset + 0xF0 + (i * 0x20))));           
            }
        }
    }
}
