using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class hkBoxShape : havokObject
    {
        public float x;
        public float y;
        public float z;
        public float unk;

        public hkBoxShape(Subfile basis, int objectOffset) {
            x = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, objectOffset + 0x10));
            y = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, objectOffset + 0x14));
            z = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, objectOffset + 0x18));
            unk = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, objectOffset + 0x1C));
        }
    }
}
