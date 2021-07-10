using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class RevoModel  //RMDL
    {
        uint magic;
        uint flags;
        public List<Mesh> meshes = new List<Mesh>();

        public class Mesh {

            public List<Vertex> vertices = new List<Vertex>();
            public List<Vertex> normals = new List<Vertex>();
            public List<Vertex> texCoords = new List<Vertex>();
            public List<face> faces = new List<face>();

            public List<MaterialData> materials = new List<MaterialData>();

            public ulong hash_of_material = 0;

            public List<VertexAttributeArrayType> presentAttributes = new List<VertexAttributeArrayType>();
        }

        public enum DisplayListCommand {

        kGXCmdNOP = 0x00,
        kGXCmdLoadIndxA = 0x20,
        kGXCmdLoadIndxB = 0x28,
        kGXCmdLoadIndxC = 0x30,
        kGXCmdLoadIndxD = 0x38

        }

        public enum DrawType
        {
            quads,
            quads2,
            triangles,
            triangleStrip,
            triangleFan,
            lines,
            lineStrip,
            points
        }

        public enum VertexAttributeArrayType {

        GX_LIGHTARRAY = 24,
        GX_NRMMTXARRAY = 22,
        GX_POSMTXARRAY = 21
        , GX_TEXMTXARRAY = 23
        , GX_VA_CLR0 = 11
        , GX_VA_CLR1 = 12
        , GX_VA_MAXATTR = 26
        , GX_VA_NBT = 25
        , GX_VA_NRM = 10
        , GX_VA_NULL = 0xff
        , GX_VA_POS = 9
        , GX_VA_PTNMTXIDX = 0
        , GX_VA_TEX0 = 13
        , GX_VA_TEX0MTXIDX = 1
        , GX_VA_TEX1 = 14
        , GX_VA_TEX1MTXIDX = 2
        , GX_VA_TEX2 = 15
        , GX_VA_TEX2MTXIDX = 3
        , GX_VA_TEX3 = 16, 
        GX_VA_TEX3MTXIDX = 4, 
        GX_VA_TEX4 = 17
        , GX_VA_TEX4MTXIDX = 5
        , GX_VA_TEX5 = 18
        , GX_VA_TEX5MTXIDX = 6
        , GX_VA_TEX6 = 19
        , GX_VA_TEX6MTXIDX = 7
        , GX_VA_TEX7 = 20
        , GX_VA_TEX7MTXIDX = 8
        }

        public enum VertexAttributeComponentType {

            GX_CLR_RGB = 0,
	        GX_CLR_RGBA = 1,
 	        GX_NRM_NBT = 1,
	        GX_NRM_NBT3 = 2,
	        GX_NRM_XYZ = 0,
	        GX_POS_XY = 0,
 	        GX_POS_XYZ = 1,
 	        GX_TEX_S = 0,
	        GX_TEX_ST = 1
        
        }

        public enum VertexAttributeComponentSize
        {

         GX_F32 = 4,
         GX_RGB565 = 0,
         GX_RGB8 = 1,
         GX_RGBA4 = 3,
         GX_RGBA6 = 4,
         GX_RGBA8 = 5,
         GX_RGBX8 = 2,
         GX_S16 = 3,
         GX_S8 = 1,
         GX_U16 = 2,
         GX_U8 = 0

        }

        public RevoModel(Subfile basis) {

            int pos = 0;

            magic = utility.ReadUInt32BigEndian(basis.filebytes,pos); pos += 4;
            flags = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;

            pos += 0x18;

            int meshcount = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
            int meshtableoffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;

            for (int m = 0; m < meshcount; m++)
            {
                Mesh newMesh = new Mesh();

                pos = meshtableoffset + (m * 4);
                int meshInfoTableOffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;

                pos = meshInfoTableOffset;

                int displayListSize = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
                int displayListOffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
                int numVertAttributes = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
                int vertDataOffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;

                newMesh.hash_of_material = utility.ReadUInt64BigEndian(basis.filebytes, pos); pos += 8;
                uint typeID_of_material = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;
                uint unk2 = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;

                float boundsMinX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                float boundsMinY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                float boundsMinZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;

                float boundsMaxX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                float boundsMaxY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                float boundsMaxZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;

                uint unk3 = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;
                uint unk4 = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;

                uint unk5 = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4; //FNV-1 hash of "none" in MySims Kingdom. Different in Agents.

                pos += 12;

                int boneToMatrixBindingInfoOffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
                int boneNamesOffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
                int boneInfoOffset = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;

                //now read vertex data

                for (int v = 0; v < numVertAttributes; v++) {

                    pos = vertDataOffset + (v * 8);
                    
                    VertexAttributeArrayType vertexArrayType = (VertexAttributeArrayType)basis.filebytes[pos]; pos++;
                    //what kind of data is stored in the array: positions? lights? etc.
                    newMesh.presentAttributes.Add(vertexArrayType);

                    VertexAttributeComponentType vertexComponentType = (VertexAttributeComponentType)(basis.filebytes[pos] >> 3);
                    //how many components are there to the data, e.g. is it XYZ, or just XY? etc.

                    VertexAttributeComponentSize vertexComponentSize = (VertexAttributeComponentSize)(basis.filebytes[pos] & 0x07); pos++;
                    //what format is the data in, e.g. 32 bit float, u8, RGB565, etc...

                    pos += 2; //skip padding

                    pos = utility.ReadInt32BigEndian(basis.filebytes, pos); //now go to the start of the actual entries



                    int stopPoint = boneToMatrixBindingInfoOffset;  //absolute endpoint is the beginning of the next section
                    if (stopPoint == 0) {
                        if (m == meshcount - 1){
                            stopPoint = basis.filebytes.Length;
                        }
                        else {
                            stopPoint = utility.ReadInt32BigEndian(basis.filebytes, meshtableoffset + ((m + 1) * 4));
                        }
                        
                    }

                    if (v < numVertAttributes - 1) { //but if we're not at the last vertex array yet, then the endpoint is the start of the next array
                        stopPoint = utility.ReadInt32BigEndian(basis.filebytes, vertDataOffset + ((v+1) * 8) + 4);
                    }

                    int checkAheadAmount = 12;

                    if (vertexArrayType == VertexAttributeArrayType.GX_LIGHTARRAY) { //it will probably ask for RGB values etc
                        switch (vertexComponentSize)
                        {
                            case VertexAttributeComponentSize.GX_RGBA4:
                                checkAheadAmount = 1;
                                break;
                            case VertexAttributeComponentSize.GX_RGB565:
                                checkAheadAmount = 2;
                                break;
                            case VertexAttributeComponentSize.GX_RGB8:  //more accurately described as RGB24
                            case VertexAttributeComponentSize.GX_RGBA6:  //more accurately described as RGBA24
                                checkAheadAmount = 3;
                                break;
                             case VertexAttributeComponentSize.GX_RGBA8:
                             case VertexAttributeComponentSize.GX_RGBX8:
                                checkAheadAmount = 4;
                                break;
                        }

                    }
                    else {
                        switch (vertexComponentSize) {

                            case VertexAttributeComponentSize.GX_S8:
                            case VertexAttributeComponentSize.GX_U8:
                                checkAheadAmount = 1;
                                break;
                            case VertexAttributeComponentSize.GX_S16:
                            case VertexAttributeComponentSize.GX_U16:
                                checkAheadAmount = 2;
                                break;
                            case VertexAttributeComponentSize.GX_F32:
                                checkAheadAmount = 4;
                                break;
                        }
                    }

                    switch (vertexComponentType) {

                        case VertexAttributeComponentType.GX_POS_XY:
                            checkAheadAmount *= 2;
                            break;
                        case VertexAttributeComponentType.GX_POS_XYZ:
                            checkAheadAmount *= 3;
                            break;
                        default:
                            System.Windows.Forms.MessageBox.Show("Unhandled component type, and therefore couldn't find an appopriate multiplier: "+ vertexComponentType);
                            break;

                    }

                    //now actually go and read those vertices

                    List<Vertex> vertexAttributes = new List<Vertex>();

                    do
                    {
                        Vertex newVertex = new Vertex();

                        if (vertexArrayType == VertexAttributeArrayType.GX_VA_POS)
                        {
                            switch (vertexComponentType)
                            {
                                case VertexAttributeComponentType.GX_POS_XY:

                                    if (vertexComponentSize == VertexAttributeComponentSize.GX_F32)
                                    {
                                        checkAheadAmount = 8;
                                        newVertex.X = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.Y = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("Argh! The vertex coordinates are not floats! How annoying.");
                                    }
                                    break;
                                case VertexAttributeComponentType.GX_POS_XYZ:

                                    if (vertexComponentSize == VertexAttributeComponentSize.GX_F32)
                                    {
                                        checkAheadAmount = 12;
                                        newVertex.X = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.Y = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.Z = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("Argh! The vertex coordinates are not floats! How annoying.");
                                    }
                                    break;
                                default:
                                    Console.WriteLine("Unhandled vertex component type: " + vertexComponentType);
                                    break;
                            }
                        }
                        else if (vertexArrayType == VertexAttributeArrayType.GX_VA_NRM || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX0 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX1 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX2 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX3 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX4 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX5 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX6 || vertexArrayType == VertexAttributeArrayType.GX_VA_TEX7)
                        {
                            switch (vertexComponentType)
                            {
                                case VertexAttributeComponentType.GX_NRM_XYZ:

                                    if (vertexComponentSize == VertexAttributeComponentSize.GX_F32)
                                    {
                                        checkAheadAmount = 12;
                                        newVertex.normalX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.normalY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.normalZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("Argh! The normal coordinates are not floats! How annoying.");
                                    }
                                    break;
                                case VertexAttributeComponentType.GX_NRM_NBT3:

                                    if (vertexComponentSize == VertexAttributeComponentSize.GX_F32)
                                    {
                                        checkAheadAmount = 36;
                                        newVertex.normalX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.normalY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.normalZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.binormalX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.binormalY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.binormalZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.tangentX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.tangentY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.tangentZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("Argh! The normal coordinates are not floats! How annoying.");
                                    }
                                    break;
                                case VertexAttributeComponentType.GX_NRM_NBT:

                                    if (vertexComponentSize == VertexAttributeComponentSize.GX_F32)
                                    {
                                        checkAheadAmount = 12;
                                        newVertex.normalX = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.normalY = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.normalZ = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                    }
                                    else if (vertexComponentSize == VertexAttributeComponentSize.GX_U8)
                                    {
                                        checkAheadAmount = 8;  //APPARENTLY IT IGNORES THE U8 AND READS FLOATS INSTEAD.
                                        newVertex.U = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                        newVertex.V = 1 - utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("Argh! The normal coordinates are not floats! How annoying.");
                                    }
                                    break;
                                default:
                                    Console.WriteLine("Unhandled tex or normal component type: " + vertexComponentType);
                                    break;
                            }
                        }
                        else if (vertexArrayType == VertexAttributeArrayType.GX_VA_CLR0 || vertexArrayType == VertexAttributeArrayType.GX_VA_CLR1)
                        {
                            switch (vertexComponentType)
                            {
                                case VertexAttributeComponentType.GX_CLR_RGB:
                                    if (vertexComponentSize == VertexAttributeComponentSize.GX_RGBA6) //RGBA24 basically
                                    {
                                        checkAheadAmount = 3;
                                        newVertex.color = imageTools.ReadRGBA24(basis.filebytes, pos);
                                        pos += 3;
                                    }
                                    else
                                    {
                                        System.Windows.Forms.MessageBox.Show("Argh! The rgb component size was unhandled! It was: "+vertexComponentSize);
                                    }
                                    break;
                                default:
                                    Console.WriteLine("Unhandled colour component type: " + vertexComponentType);
                                    break;
                                }
                            }
                        else
                        {

                            Console.WriteLine("Don't know how to read a vertex array of type: " + vertexArrayType + "! Component type is: " + vertexComponentType);
                            break;

                        }

                        vertexAttributes.Add(newVertex);

                    } while (pos + checkAheadAmount <= stopPoint);


                    switch (vertexArrayType) {

                        case VertexAttributeArrayType.GX_VA_POS:
                            newMesh.vertices = vertexAttributes;
                            break;
                        case VertexAttributeArrayType.GX_VA_NRM:
                            foreach (Vertex vn in vertexAttributes) {
                                newMesh.normals.Add(vn);
                            }
                            break;
                        case VertexAttributeArrayType.GX_VA_TEX0:
                            newMesh.texCoords = vertexAttributes;
                            break;

                    }
                }

                //now read display list data

                pos = displayListOffset;

                while (pos < displayListOffset + displayListSize) {

                    byte cmd = basis.filebytes[pos]; pos++;

                    switch ((DisplayListCommand)cmd)
                    {
                        case DisplayListCommand.kGXCmdNOP:
                            break;
                        case DisplayListCommand.kGXCmdLoadIndxA:
                        case DisplayListCommand.kGXCmdLoadIndxB:
                        case DisplayListCommand.kGXCmdLoadIndxC:
                        case DisplayListCommand.kGXCmdLoadIndxD:
                            pos += 4;
                            break;
                        default:
                            { 
                            byte drawCmd = (byte)((cmd & 0x78) >> 3);
                            byte vat = (byte)(cmd & 0x07);

                            if (drawCmd > 7)
                            {
                                Console.WriteLine("Invalid Display List command "+cmd+" at offset "+pos);
                            }
                            else
                            {
                                DrawType drawType = (DrawType)drawCmd;
                                
                                //Console.WriteLine("Found drawtype at "+pos+": " + drawType);

                                // Read element amount
                                int elemCount = utility.ReadUInt16BigEndian(basis.filebytes,pos); pos += 2;

                                    switch (drawType) {

                                        case DrawType.quads:
                                        case DrawType.quads2:
                                        case DrawType.triangles:
                                            int vertsMax = 4;
                                            if (drawType == DrawType.triangles) { vertsMax = 3; }
                                            
                                            for (int f = 0; f < elemCount / vertsMax; f++)
                                            {
                                                face newFace = new face();
                                                for (int i = 0; i < vertsMax; i++)
                                                {
                                                    if (boneNamesOffset != 0)
                                                    {
                                                        switch (i) {
                                                            case 0: newFace.v1BoneIndex = basis.filebytes[pos];  break;
                                                            case 1: newFace.v2BoneIndex = basis.filebytes[pos]; break;
                                                            case 2: newFace.v3BoneIndex = basis.filebytes[pos]; break;
                                                            case 3: newFace.v4BoneIndex = basis.filebytes[pos]; newFace.is_quad = true; break;
                                                        }
                                                        pos++;
                                                    }

                                                    for (int attr = 0; attr < 20; attr++)
                                                    {  //for each possible vertex attribute, if it exists, process it in order

                                                        if (newMesh.presentAttributes.Contains((VertexAttributeArrayType)attr))
                                                        {

                                                            newFace = AddAttrToFace(basis.filebytes, pos, (VertexAttributeArrayType)attr, newFace, i);
                                                            pos = newFace.temp;
                                                        }
                                                    }

                                                    newMesh.faces.Add(newFace);

                                                }
                                            }
                                            break;
                                        case DrawType.triangleStrip:
                                            {
                                                face newFace = new face();
                                                int currentV = 0;
                                                int backupPos = 0;
                                                bool winding = false;

                                                for (int i = 0; i < elemCount; i++)
                                                {
                                                    if (currentV == 1)
                                                    {   //if we're reading the penultimate vertex in a triangle, remember our pos because we'll need to come back to it when we process the next triangle (which overlaps slightly)
                                                        backupPos = pos;
                                                    }

                                                    if (boneNamesOffset != 0)
                                                    {
                                                        switch (currentV)
                                                        {
                                                            case 0: newFace.v1BoneIndex = basis.filebytes[pos]; break;
                                                            case 1: newFace.v2BoneIndex = basis.filebytes[pos]; break;
                                                            case 2: newFace.v3BoneIndex = basis.filebytes[pos]; break;
                                                            case 3: newFace.v4BoneIndex = basis.filebytes[pos]; newFace.is_quad = true; break;  //this won't get triggered by a triangle, but might as well keep it here so I don't forget for quads
                                                        }
                                                        pos++;
                                                    }

                                                    if (winding && currentV == 0)
                                                    {
                                                        currentV = 1;
                                                    }
                                                    else if (winding && currentV == 1) { 
                                                        currentV = 0;
                                                    }

                                                    for (int attr = 0; attr < 20; attr++)
                                                    {  //for each possible vertex attribute, if it exists, process it in order

                                                        if (newMesh.presentAttributes.Contains((VertexAttributeArrayType)attr))
                                                        {
                                                            newFace = AddAttrToFace(basis.filebytes, pos, (VertexAttributeArrayType)attr, newFace, currentV);
                                                            pos = newFace.temp;
                                                        }
                                                    }

                                                    if (winding && currentV == 1)
                                                    {
                                                        currentV = 0;
                                                    }
                                                    else if (winding && currentV == 0)
                                                    {
                                                        currentV = 1;
                                                    }

                                                    currentV++;

                                                    if (currentV == 3)
                                                    { //if we just finished making a triangle
                                                        newMesh.faces.Add(newFace);
                                                        newFace = new face();
                                                        currentV = 0;
                                                        if (i < elemCount - 1)
                                                        { //as long as we're not on the very last one, there's still work to do so back up a bit and keep reading the strip
                                                            i-=2;
                                                            pos = backupPos;
                                                            winding = !winding;
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                        default:
                                            Console.WriteLine("Unhandled draw type at offset "+pos+": "+drawType);
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }

                //now get the materials ready

                newMesh.materials = new List<MaterialData>();

                foreach (Subfile s in global.activePackage.subfiles) {

                    if (s.hash == newMesh.hash_of_material && s.typeID == typeID_of_material) {

                        switch ((global.TypeID)s.typeID){
                            case global.TypeID.MATD_MSK:
                            case global.TypeID.MATD_MSA:
                                if (s.filebytes == null || s.filebytes.Length == 0) {
                                    s.Load();
                                }
                                newMesh.materials.Add(s.matd);
                                break;
                            case global.TypeID.MTST_MSK:
                            case global.TypeID.MTST_MSA:
                                if (s.filebytes == null || s.filebytes.Length == 0){
                                    s.Load();
                                }

                                foreach (MaterialData mat in s.mtst.mats) {
                                    newMesh.materials.Add(mat);
                                }

                                break;
                            default:
                                System.Windows.Forms.MessageBox.Show("RMDL tried to use a material of unexpected type ID: "+ (global.TypeID)s.typeID);
                                break;
                        }
                        break;
                    }
                }

                meshes.Add(newMesh);
            }
        }

        public void GenerateObj(string outputPath) {

            List<string> obj = new List<string>();

            int cumuVertices = 0;
            int cumuNormals = 0;
            int cumuTexCoords = 0;

            string mtl_folder_path = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath)) + "_materials";

            foreach (Mesh m in meshes) {

                obj.Add("object "+meshes.IndexOf(m));

                for (int v = 0; v < m.vertices.Count; v++) {
                    obj.Add("v "+m.vertices[v].X + " " + m.vertices[v].Y + " " + m.vertices[v].Z);
                }
                
                for (int v = 0; v < m.normals.Count; v++)
                {
                    obj.Add("vn " + m.normals[v].normalX + " " + m.normals[v].normalY + " " +m.normals[v].normalZ);
                }

                for (int v = 0; v < m.texCoords.Count; v++)
                {
                    obj.Add("vt " + m.texCoords[v].U + " " + m.texCoords[v].V);
                }

                for (int f = 0; f < m.faces.Count; f++)
                {
                    if (m.faces[f].is_quad)
                    {
                        obj.Add("f " + (m.faces[f].v1 + 1 + cumuVertices) + "/" + (m.faces[f].vt1 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn1 + 1 + cumuNormals) + " " +
                                       (m.faces[f].v2 + 1 + cumuVertices) + "/" + (m.faces[f].vt2 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn2 + 1 + cumuNormals) + " " +
                                       (m.faces[f].v3 + 1 + cumuVertices) + "/" + (m.faces[f].vt3 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn3 + 1 + cumuNormals) + " " +
                                       (m.faces[f].v4 + 1 + cumuVertices) + "/" + (m.faces[f].vt4 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn4 + 1 + cumuNormals));
                    }
                    else {
                        obj.Add("f " + (m.faces[f].v1 + 1 + cumuVertices) + "/" + (m.faces[f].vt1 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn1 + 1 + cumuNormals) + " " + 
                                       (m.faces[f].v2 + 1 + cumuVertices) + "/" + (m.faces[f].vt2 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn2 + 1 + cumuNormals) + " " +
                                       (m.faces[f].v3 + 1 + cumuVertices) + "/" + (m.faces[f].vt3 + 1 + cumuTexCoords) + "/" + (m.faces[f].vn3 + 1 + cumuNormals));
                    }
                }

                cumuVertices += m.vertices.Count;
                cumuNormals += m.normals.Count;
                cumuTexCoords += m.texCoords.Count;

                foreach (MaterialData mat in m.materials) {

                    if (!Directory.Exists(mtl_folder_path)) {
                        Directory.CreateDirectory(mtl_folder_path);
                    }

                    foreach (MaterialData.Param param in mat.parameters) {

                        if (param.paramType == MaterialData.MaterialParameter.diffuseMap) {
                            param.diffuse_texture.ExportFile(true, Path.Combine(mtl_folder_path,param.diffuse_texture.filename.Replace(".tpl",".png")));
                        }
                    }
                }
            }
            File.WriteAllLines(outputPath,obj.ToArray());
        }

        public face AddAttrToFace(byte[] filebytes, int pos, VertexAttributeArrayType attr, face f, int vertIndex) {

            switch (attr) {

                case VertexAttributeArrayType.GX_VA_POS:

                    switch (vertIndex) {        //I'm aware that this is an awful way to do it, but I don't want to go back and refactor the skyheroes code right now
                        case 0: f.v1 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 1: f.v2 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 2: f.v3 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 3: f.v4 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                    }
                    break;
                case VertexAttributeArrayType.GX_VA_NRM:
                    switch (vertIndex)
                    {        //I'm aware that this is an awful way to do it, but I don't want to go back and refactor the skyheroes code right now
                        case 0: f.vn1 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 1: f.vn2 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 2: f.vn3 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 3: f.vn4 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                    }
                    break;
                case VertexAttributeArrayType.GX_VA_TEX0:
                case VertexAttributeArrayType.GX_VA_TEX1:
                case VertexAttributeArrayType.GX_VA_TEX2:
                case VertexAttributeArrayType.GX_VA_TEX3:
                case VertexAttributeArrayType.GX_VA_TEX4:
                case VertexAttributeArrayType.GX_VA_TEX5:
                case VertexAttributeArrayType.GX_VA_TEX6:
                case VertexAttributeArrayType.GX_VA_TEX7:
                    switch (vertIndex)
                    {        //I'm aware that this is an awful way to do it, but I don't want to go back and refactor the skyheroes code right now
                        case 0: f.vt1 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 1: f.vt2 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 2: f.vt3 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 3: f.vt4 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                    }
                    break;
                case VertexAttributeArrayType.GX_VA_CLR0:
                case VertexAttributeArrayType.GX_VA_CLR1:
                    switch (vertIndex)
                    {        //I'm aware that this is an awful way to do it, but I don't want to go back and refactor the skyheroes code right now
                        case 0: f.vc1 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 1: f.vc2 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 2: f.vc3 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                        case 3: f.vc4 = utility.ReadUInt16BigEndian(filebytes, pos); pos += 2; break;
                    }
                    break;
                default:
                    System.Windows.Forms.MessageBox.Show("Unhandled vertex array type at "+pos+ ": "+attr);
                    break;
            }

            f.temp = pos;

            return f;
        }
    }
}