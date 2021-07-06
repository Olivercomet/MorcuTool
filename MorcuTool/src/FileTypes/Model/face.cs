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

        public face() { 
        
        }
        public face(ushort v_1, ushort v_2, ushort v_3) {
            v1 = v_1;
            v2 = v_2;
            v3 = v_3;
        } 
    }
}
