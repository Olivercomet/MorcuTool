using System;
using System.Collections.Generic;
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

        public Vertex() { 
        }
        public Vertex(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
