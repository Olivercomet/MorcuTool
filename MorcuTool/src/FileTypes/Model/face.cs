using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class Face
    {
        public int v1;
        public int v2;
        public int v3;
        public int v4;

        public int vn1;
        public int vn2;
        public int vn3;
        public int vn4;

        public int vt1;
        public int vt2;
        public int vt3;
        public int vt4;

        public int vc1;
        public int vc2;
        public int vc3;
        public int vc4;

        public int v1BoneIndex;
        public int v2BoneIndex;
        public int v3BoneIndex;
        public int v4BoneIndex;

        public int temp; //hacky temporary variable for storing new pos after reading


        public bool is_quad;
        public Face() { 
        
        }
        public Face(int v_1, int v_2, int v_3) {
            v1 = v_1;
            v2 = v_2;
            v3 = v_3;
        } 
    }
}
