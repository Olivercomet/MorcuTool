using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    class mdlObject
    {
        public uint startOffset;
        public uint vertexCount;
        public uint vertexListOffset;

        public uint StartingVertexID;

        public uint faceListOffset;

        public uint facesToRemoveOffset;
        public List<ushort> facesToRemove = new List<ushort>();

        public List<Vertex> vertices = new List<Vertex>();
        public List<Face> faces = new List<Face>();
    }
}
