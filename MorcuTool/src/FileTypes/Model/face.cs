using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class face
    {
        public ushort v1;
        public ushort v2;
        public ushort v3;
        public ushort v4;

        public ushort vn1;
        public ushort vn2;
        public ushort vn3;
        public ushort vn4;

        public ushort vt1;
        public ushort vt2;
        public ushort vt3;
        public ushort vt4;

        public ushort vc1;
        public ushort vc2;
        public ushort vc3;
        public ushort vc4;

        public ushort v1BoneIndex;
        public ushort v2BoneIndex;
        public ushort v3BoneIndex;
        public ushort v4BoneIndex;

        public int temp; //hacky temporary variable for storing new pos after reading


        public bool is_quad;
        public face() { 
        
        }
        public face(ushort v_1, ushort v_2, ushort v_3) {
            v1 = v_1;
            v2 = v_2;
            v3 = v_3;
        } 
    }
}
