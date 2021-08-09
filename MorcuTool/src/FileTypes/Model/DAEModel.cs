using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MorcuTool.MorcuMath;

namespace MorcuTool
{
    public class DAEModel
    {
        public List<Mesh> meshes = new List<Mesh>();

        public class Mesh {
            public List<Vertex> vertices = new List<Vertex>();
            public List<Face> faces = new List<Face>();
        }

        public DAEModel(string daePath)
        {
            FromDAE(daePath);
        }

        public class DAEMesh {

            public string name;
            public Mesh actualMesh;

            public Dictionary<int, DAEVertexData> offsetsAndLists = new Dictionary<int, DAEVertexData>();

            public class DAEVertexData {
                public string dataType;
                public Object list;
                public int set; //for UVMaps
                public DAEVertexData(string _dataType, Object _list) {
                    dataType = _dataType;
                    list = _list;
                }

                public DAEVertexData(string _dataType, Object _list, int _set)
                {
                    dataType = _dataType;
                    list = _list;
                    set = _set;
                }
            }

            public DAEMesh(XML.XMLtag geometryXMLtag)
            {
                name = geometryXMLtag.GetParamValue("id");

                XML.XMLtag meshXMLtag = geometryXMLtag.GetFirstChildWithName("mesh");
                XML.XMLtag trianglesXMLtag = meshXMLtag.GetFirstChildWithName("triangles");

                //the "triangles" XML tag contains, among other things, the names of the arrays within "mesh" that contain important model data (position, normals, etc)

                List<XML.XMLtag> sources = meshXMLtag.GetChildrenWithName("source");

                foreach (XML.XMLtag input in trianglesXMLtag.GetChildrenWithName("input")) {
                    string source = input.GetParamValue("source");
                    source = source.Substring(1, source.Length-1); //removes hashtag at start

                    int offset = int.Parse(input.GetParamValue("offset"));

                    string dataType = input.GetParamValue("semantic");

                    switch (dataType) {

                        case "VERTEX": //This one is not a direct reference like the others, so it has an extra two lines first
                            { 
                                source = meshXMLtag.GetFirstChildWithName("vertices").GetFirstChildWithName("input").GetParamValue("source");
                                source = source.Substring(1, source.Length - 1); //removes hashtag at start

                                XML.XMLtag positionXMLtag = meshXMLtag.GetFirstChildWithParamAndValue("id", "\"" + source + "\"");
                                string arrayName = GetDAEInputArrayName(positionXMLtag);
                                XML.XMLtag floatArray = positionXMLtag.GetFirstChildWithParamAndValue("id", "\"" + arrayName + "\"");
                                string[] floatArrayString = floatArray.data.Replace('\n', ' ').Split(' ');
                                int count = int.Parse(floatArray.GetParamValue("count"));

                                List<MorcuMath.Vector3> positions = new List<MorcuMath.Vector3>();
            
                                for (int i = 0; i < count; i += 3) {
                                    positions.Add(new Vector3(float.Parse(floatArrayString[i]), float.Parse(floatArrayString[i + 1]), float.Parse(floatArrayString[i + 2])));
                                    }
                                offsetsAndLists.Add(offset, new DAEVertexData(dataType,positions));
                            }
                            break;
                        case "NORMAL":
                            {
                                XML.XMLtag normalXMLtag = meshXMLtag.GetFirstChildWithParamAndValue("id", "\"" + source + "\"");
                                string arrayName = GetDAEInputArrayName(normalXMLtag);
                                XML.XMLtag floatArray = normalXMLtag.GetFirstChildWithParamAndValue("id", "\"" + arrayName + "\"");
                                string[] floatArrayString = floatArray.data.Replace('\n', ' ').Split(' ');
                                int count = int.Parse(floatArray.GetParamValue("count"));
                                List<MorcuMath.Vector3> normals = new List<MorcuMath.Vector3>();
                                for (int i = 0; i < count; i += 3)
                                {
                                    normals.Add(new MorcuMath.Vector3(float.Parse(floatArrayString[i]), float.Parse(floatArrayString[i + 1]), float.Parse(floatArrayString[i + 2])));
                                }
                                offsetsAndLists.Add(offset, new DAEVertexData(dataType,normals));
                            }
                            break;
                             
                        case "TEXCOORD":
                            {
                                int UVchannel = 0;
                                int.TryParse(input.GetParamValue("set"), out UVchannel);

                                XML.XMLtag texcoordXMLtag = meshXMLtag.GetFirstChildWithParamAndValue("id", "\"" + source + "\"");
                                string arrayName = GetDAEInputArrayName(texcoordXMLtag);
                                XML.XMLtag floatArray = texcoordXMLtag.GetFirstChildWithParamAndValue("id", "\"" + arrayName + "\"");
                                string[] floatArrayString = floatArray.data.Replace('\n', ' ').Split(' ');
                                int count = int.Parse(floatArray.GetParamValue("count"));

                                List<MorcuMath.Vector2> newUVMap = new List<MorcuMath.Vector2>();
                                for (int i = 0; i < count; i += 2){
                                    newUVMap.Add(new Vector2(float.Parse(floatArrayString[i]), float.Parse(floatArrayString[i + 1])));
                                }
                                offsetsAndLists.Add(offset, new DAEVertexData(dataType,newUVMap,UVchannel));
                            }

                            break;
                        case "COLOR": // Color coordinate vector
                            {
                                XML.XMLtag colorXMLtag = meshXMLtag.GetFirstChildWithParamAndValue("id", "\"" + source + "\"");
                                string arrayName = GetDAEInputArrayName(colorXMLtag);
                                XML.XMLtag accessor = colorXMLtag.GetFirstChildWithName("technique_common").GetFirstChildWithName("accessor");

                                List<XML.XMLtag> colorComponents = accessor.GetChildrenWithName("param");
                                string[] components = new string[colorComponents.Count];
                                for (int i = 0; i < colorComponents.Count; i++) {
                                    components[i] = colorComponents[i].GetParamValue("name"); //will return R, G, B, or A. We do this to find out which order the components are in and how many there are (i.e. whether A is included or not)
                                }

                                XML.XMLtag floatArray = colorXMLtag.GetFirstChildWithParamAndValue("id", "\"" + arrayName + "\"");
                                string[] floatArrayString = floatArray.data.Replace('\n', ' ').Split(' ');
                                int count = int.Parse(floatArray.GetParamValue("count"));
                                
                                List<Color> colors = new List<Color>();

                                for (int i = 0; i < count; i += components.Length)
                                {
                                    double R = 255;
                                    double G = 255;
                                    double B = 255;
                                    double A = 255;

                                    for (int j = 0; j < components.Length; j++) {
                                        switch (components[j]) {
                                            case "R":
                                                R *= double.Parse(floatArrayString[i+j]);
                                                break;
                                            case "G":
                                                G *= double.Parse(floatArrayString[i + j]);
                                                break;
                                            case "B":
                                                B *= double.Parse(floatArrayString[i + j]);
                                                break;
                                            case "A":
                                                A *= double.Parse(floatArrayString[i + j]);
                                                break;
                                            default:
                                                Console.WriteLine("Unknown colour component: "+components[j]);
                                                break;
                                        }
                                    }
                                    colors.Add(Color.FromArgb((int)Math.Round(A), (int)Math.Round(R), (int)Math.Round(G), (int)Math.Round(B)));
                                }
                                offsetsAndLists.Add(offset, new DAEVertexData(dataType, colors));
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown array type in mesh: "+input.GetParamValue("semantic"));
                            break;
                    }
                }

                string[] indicesArrayString = trianglesXMLtag.GetFirstChildWithName("p").data.Replace('\n', ' ').Split(' ');

                int[] vertexDataOffsets = offsetsAndLists.Keys.ToList<int>().OrderBy(o => o).ToArray();

                actualMesh = new Mesh();

                Face newFace = null;

                for (int i = 0; i < indicesArrayString.Length; i+= vertexDataOffsets.Length) {

                    Vertex newVertex = new Vertex();

                    if ((i / vertexDataOffsets.Length % 3) == 0){
                        if (newFace != null) {
                            actualMesh.faces.Add(newFace);
                        }
                        newFace = new Face();
                        newFace.v1 = i / vertexDataOffsets.Length;
                    }
                    else if (((i - vertexDataOffsets.Length) / vertexDataOffsets.Length % 3) == 0){
                        newFace.v2 = i / vertexDataOffsets.Length;
                    }
                    else if (((i + vertexDataOffsets.Length) / vertexDataOffsets.Length % 3) == 0) {
                        newFace.v3 = i / vertexDataOffsets.Length;
                    }

                    for (int j = 0; j < vertexDataOffsets.Length; j++) {

                        switch (offsetsAndLists[vertexDataOffsets[j]].dataType) {
                            case "VERTEX":
                            case "POSITION":
                                newVertex.position = ((List<MorcuMath.Vector3>)offsetsAndLists[vertexDataOffsets[j]].list)[int.Parse(indicesArrayString[i+j])];
                                break;
                            case "NORMAL":
                                newVertex.normal = ((List<MorcuMath.Vector3>)offsetsAndLists[vertexDataOffsets[j]].list)[int.Parse(indicesArrayString[i+j])];
                                break;                           
                            case "TEXCOORD":
                                DAEVertexData daeVertexData = offsetsAndLists[vertexDataOffsets[j]]; // just a shortcut because we have to access it twice
                                newVertex.UVchannels[daeVertexData.set] = ((List<MorcuMath.Vector2>)daeVertexData.list)[int.Parse(indicesArrayString[i+j])];
                                break;
                            case "COLOR":
                                newVertex.color = ((List<Color>)offsetsAndLists[vertexDataOffsets[j]].list)[int.Parse(indicesArrayString[i+j])];
                                break;
                        }
                    }
                    actualMesh.vertices.Add(newVertex);
                }

                actualMesh.faces.Add(newFace); //add the last face (because otherwise it doesn't get processed at the start of the next loop like all the others do)

                Console.WriteLine("DAE mesh parsed successfully");
            }
            public string GetDAEInputArrayName(XML.XMLtag tag){
                string output = tag.GetFirstChildWithName("technique_common").GetFirstChildWithName("accessor").GetParamValue("source");
                return output.Substring(1, output.Length - 1); //remove the hashtag from the beginning
            }
        }


        public void FromDAE(string daePath) {

            XML.XMLtag dae = new XML(File.ReadAllText(daePath)).GetFirstRootTagWithName("COLLADA");

            XML.XMLtag library_images = dae.GetFirstChildWithName("library_images");
            XML.XMLtag library_materials = dae.GetFirstChildWithName("library_materials");
            XML.XMLtag library_effects = dae.GetFirstChildWithName("library_effects");
            XML.XMLtag library_geometries = dae.GetFirstChildWithName("library_geometries");
            XML.XMLtag library_visual_scenes = dae.GetFirstChildWithName("library_visual_scenes");

            Console.WriteLine("ready to parse dae");

            if (library_geometries != null) { 
                foreach (XML.XMLtag geometry in library_geometries.children) {
                    meshes.Add(new DAEMesh(geometry).actualMesh);
                }
            }

            if (library_visual_scenes != null){
                foreach (XML.XMLtag node in library_visual_scenes.GetFirstChildWithName("visual_scene").GetChildrenWithName("node"))
                {
                    XML.XMLtag instance_geometry = node.GetFirstChildWithName("instance_geometry");

                    if (instance_geometry != null) {
                        string meshID = instance_geometry.GetParamValue("url");
                        meshID = meshID.Substring(1, meshID.Length - 1); //remove hashtag at start

                        Console.WriteLine("TODO: Now we can look up the mesh in the DAE virtual scene and apply its scene translation etc to it. ");
                    }
                }
            }


            //temp
            File.WriteAllLines("test.obj",MakeOBJ());
        }


        public string[] MakeOBJ() {   //wavefront OBJ

            List<string> output = new List<string>();

            for (int m = 0; m < meshes.Count; m++) {

                output.Add("o object"+m);
                output.Add("");

                foreach (Vertex vertex in meshes[m].vertices) {
                    output.Add("v " + vertex.position.x + " " + vertex.position.y + " " + vertex.position.z);
                    output.Add("vn " + vertex.normal.x + " " + vertex.normal.y + " " + vertex.normal.z);
                    output.Add("vt " + vertex.UVchannels[0].x + " " + vertex.UVchannels[0].y);
                }
                output.Add("");
                foreach (Face f in meshes[m].faces)
                {
                    output.Add("f " + (f.v1+1) + "/" + (f.v1 + 1) + "/" + (f.v1 + 1) +
                                " " + (f.v2+1) + "/" + (f.v2 + 1) + "/" + (f.v2 + 1) +
                                " " + (f.v3+1) + "/" + (f.v3 + 1) + "/" + (f.v3 + 1));
                }
                output.Add("");
            }

            Console.WriteLine("Created OBJ successfully");
            return output.ToArray();
        }
    }
}
