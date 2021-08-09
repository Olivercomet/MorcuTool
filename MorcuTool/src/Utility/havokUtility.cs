using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public static class havokUtility
    {

        public static List<Vertex> ParseHkVertexArray(Subfile basis, int offset) {

            int length = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 4));
            int capacityAndFlags = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 8));

            if (length == 0) {
                return null;
            }

            Console.WriteLine("Going to look for offset "+offset+" in the local fixup table");

            offset = basis.hkx.getLocalFixUpOffset(offset - basis.hkx.dataSection.sectionOffset) + basis.hkx.dataSection.sectionOffset; //go to the offset of the actual array, using the local fixup table

            //now begins the unique bit  

            List<Vertex> output = new List<Vertex>();

            for (int i = 0; i < length; i++) {

                Vertex v = new Vertex();

                v.position.x = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, offset));
                v.position.y = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, offset+4));
                v.position.z = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, offset+8));
                v.W = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, offset+12));

                output.Add(v);
                //Console.WriteLine("vertex: "+v.X+","+v.Y+","+v.Z);
                offset += 16;
            
            }
            return output;
        }

        public static List<hkSimpleMeshShapeTriangle> ParseHkTriangleArray(Subfile basis, int offset) {
            int length = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 4));
            int capacityAndFlags = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 8));

            if (length == 0)
            {
                return null;
            }

            Console.WriteLine("Going to look for offset " + offset + " in the local fixup table");

            offset = basis.hkx.getLocalFixUpOffset(offset - basis.hkx.dataSection.sectionOffset) + basis.hkx.dataSection.sectionOffset; //go to the offset of the actual array, using the local fixup table

            //now begins the unique bit  

            List<hkSimpleMeshShapeTriangle> output = new List<hkSimpleMeshShapeTriangle>();

            for (int i = 0; i < length; i++)
            {

                hkSimpleMeshShapeTriangle t = new hkSimpleMeshShapeTriangle();

                t.v1 = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset));
                t.v2 = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 4));
                t.v3 = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 8));

                output.Add(t);
                //Console.WriteLine("triangle: " + t.v1 + "," + t.v2 + "," + t.v3);
                offset += 12;
            }

            return output;
        }

        public static List<int> ParseHkIntArray(Subfile basis, int offset)
        {
            int length = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 4));
            int capacityAndFlags = Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset + 8));

            if (length == 0)
            {
                return null;
            }

            Console.WriteLine("Going to look for offset " + offset + " in the local fixup table");

            offset = basis.hkx.getLocalFixUpOffset(offset - basis.hkx.dataSection.sectionOffset) + basis.hkx.dataSection.sectionOffset; //go to the offset of the actual array, using the local fixup table

            //now begins the unique bit  

            List<int> output = new List<int>();

            for (int i = 0; i < length; i++)
            {
                output.Add(Utility.ReverseEndianSigned(BitConverter.ToInt32(basis.filebytes, offset)));
                Console.WriteLine("int: " + output[output.Count-1]);
                offset += 4;
            }

            return output;
        }
    }
}