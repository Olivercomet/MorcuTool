using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class Vertex
    {
        uint startingVertexID;

        public float X;
        public float Y;
        public float Z;
        public float W; //when needed

        public float U;
        public float V;


        public float normalX;
        public float normalY;
        public float normalZ;

        public float binormalX;
        public float binormalY;
        public float binormalZ;

        public float tangentX;
        public float tangentY;
        public float tangentZ;

        public Color color;

        public Vertex() { 
        }
        public Vertex(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
