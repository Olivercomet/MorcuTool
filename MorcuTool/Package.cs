using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class Package
    {
        public uint majorversion = 0x00;
        public uint minorversion = 0x00;
        public uint unknown1 = 0x00;
        public uint unknown2 = 0x00;
        public uint unknown3 = 0x00;
        public uint created = 0x00;
        public uint modified = 0x00;
        public uint indexmajorversion = 0x00;

        public uint filecount = 0x00;
        public uint indexoffsetdeprecated = 0x00;
        public uint indexsize = 0x00;
        public uint holeentrycount = 0x00;
        public uint holeoffset = 0x00;
        public uint holesize = 0x00;
        public uint indexminorversion = 0x00;
        public uint unknown4 = 0x00;
        public uint indexoffset = 0x00; //offset of the index table
        public uint unknown5 = 0x00;
        public uint unknown6 = 0x00;
        public uint reserved1 = 0x00;
        public uint reserved2 = 0x00;

        public List<IndexEntry> IndexEntries = new List<IndexEntry>();
        public ulong indexnumberofentries = 0x00;

        public List<Subfile> subfiles = new List<Subfile>();


        public uint GetNumOccurrencesOfTypeID(uint typeID)
            {
            foreach (IndexEntry entry in IndexEntries)
                {
                if (entry.typeID == typeID)
                    {
                    return entry.typeNumberOfInstances;
                    }
                }
            return 0;
            }
    }


    public class IndexEntry
    {

        public uint typeID = 0;
        public uint groupID = 0;
        public uint typeNumberOfInstances = 0;
        public uint indexnulls = 0;

       

       
    }

    public class Subfile
    {
        public ulong hash;
        public uint fileoffset;
        public uint filesize;

        public uint uncompressedsize; //only used by compresed files

        public uint typeID;
        public uint groupID;
    }



}
