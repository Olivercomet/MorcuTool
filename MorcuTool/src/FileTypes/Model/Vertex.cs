using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MorcuTool.MorcuMath;

namespace MorcuTool
{
    public class Vertex
    {
        uint startingVertexID;

        public Vector3 position = new Vector3();
        public float W; //when needed

        public Vector2[] UVchannels = new Vector2[8];

        public Vector3 normal = new Vector3();

        public Vector3 binormal = new Vector3();

        public Vector3 tangent = new Vector3();

        public Color color;

        public Vertex() { 
        }
        public Vertex(float x, float y, float z) {
            position = new Vector3(x, y, z);
        }
    }
}
