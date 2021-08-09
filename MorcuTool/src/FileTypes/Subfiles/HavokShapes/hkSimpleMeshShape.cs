using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class hkSimpleMeshShape : havokObject
    {
        public bool disableWelding;

        public List<Vertex> vertices = new List<Vertex>();
        public List<hkSimpleMeshShapeTriangle> triangles = new List<hkSimpleMeshShapeTriangle>();
        public List<int> materialIndices;
        public double radius;

        public hkSimpleMeshShape(Subfile basis, int objectOffset) {

            Console.WriteLine("Exporting a hkSimpleMeshShape");

            if (basis.filebytes[objectOffset + 16] == 0)
                    {
                disableWelding = false;
            }
            else {
                disableWelding = true;
            }

            vertices = havokUtility.ParseHkVertexArray(basis, objectOffset + 20);
            triangles = havokUtility.ParseHkTriangleArray(basis, objectOffset + 32);
            materialIndices = havokUtility.ParseHkIntArray(basis, objectOffset + 44);

            radius = Utility.ReverseEndianSingle(BitConverter.ToSingle(basis.filebytes, objectOffset + 56));
        }
    }
}