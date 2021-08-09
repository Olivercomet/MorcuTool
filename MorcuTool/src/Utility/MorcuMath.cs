using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool.MorcuMath
{
        public class Vector2
        {
            public float x = 0;
            public float y = 0;

            public Vector2()
            {
            }

            public Vector2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            public Vector2 ScaleBy(double scale)
            {

                return new Vector2((float)(x * scale), (float)(y * scale));

            }

            public void From32BitFloats(byte[] bytes, int offset, bool isBigEndian)
            {
                x = Utility.ReadFloat(bytes, offset, isBigEndian);
                y = Utility.ReadFloat(bytes, offset + 4, isBigEndian);
            }
        }

        public class Vector3 {

            public float x = 0;
            public float y = 0;
            public float z = 0;

            public Vector3() { 
            }

            public Vector3(float x, float y, float z) {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3 ScaleBy(double scale) {

                return new Vector3((float)(x * scale), (float)(y*scale), (float)(z*scale));
            
            }

            public void From32BitFloats(byte[] bytes, int offset, bool isBigEndian)
            {
                x = Utility.ReadFloat(bytes, offset, isBigEndian);
                y = Utility.ReadFloat(bytes, offset + 4, isBigEndian);
                z = Utility.ReadFloat(bytes, offset + 8, isBigEndian);
            }
        }
}
