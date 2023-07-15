using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class MaterialSet
    {
        public List<MaterialData> mats = new List<MaterialData>();

        public MaterialSet(Subfile basis) {

            bool useBigEndian = true;
            int start = 0x00;

            if (basis.filebytes[0] == 0x01) {
                useBigEndian = false;
                return; //Processing these MTSTs not yet implemented
            }

            int number_of_materials = Utility.ReadInt32BigEndian(basis.filebytes, start + 0x0C);
      
            List<ulong> hashes = new List<ulong>();

            for (int i = 0; i < number_of_materials; i++) {
                hashes.Add(Utility.ReadUInt64BigEndian(basis.filebytes, start + 0x18 + (i * 8)));
            }

            foreach (Subfile s in global.activePackage.subfiles) {
                if (!(s.typeID == (uint)global.TypeID.MATD_MSK || s.typeID == (uint)global.TypeID.MATD_MSA)){
                    continue;
                    }
                if (hashes.Contains(s.hash)) {
                    if (s.filebytes == null || s.filebytes.Length == 0) {
                        s.Load();
                    }

                    mats.Add(s.matd);
                }
            }
        }
    }
}