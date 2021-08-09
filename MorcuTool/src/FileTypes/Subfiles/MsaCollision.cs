using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class MsaCollision
    {

        //seems to control some of the placement of collision within the scene, but not the actual collisions themselves?
        //and not even the full placement, either - just nudging it around from a location that has already been determined elsewhere, seemingly.


        //taking LVL-Industrial_Cargo.collision as an example:

        //first section: ??? describing which type each section is, 4 bytes per section, including one that describes this section? if you null it out or put it out of sequence, you can fall through the floor

        //second section: nulling it out removes the collision for the topmost cardboard box
        //third section: nulling it out removes the collision for the cardboard box on the far right

        //fourth section: nulling it out has an unknown effect (maybe it would have an effect if the former two hadn't been nulled?)
        //fifth section (0x280 to 0x300): nulling it out has an unknown effect (maybe it would have an effect if the former two hadn't been nulled?)

        //sixth section: not sure what nulling this out did

        //thirteenth section: nulling it out removes the collision for the back wall


        //penultimate-but-one section: nulling it out removes the collision for the rightmost box in the pile




        //so I think the values in the first section are two shorts per section (including this very section we're in right now), describing the purpose of each section

        //possible values for second short:

        //01 box
        //06 mesh? it's used for the walls in the cargo container, three walls of which are together rather than separate.





        public List<string> obj = new List<string>();

        List<Vertex> vertices = new List<Vertex>();

        int cumulativeVerts = 0;

        public MsaCollision (Subfile basis) {

            obj = new List<string>();
            cumulativeVerts = 0;

            if (basis.filebytes.Length == 0) {
                Console.WriteLine("Couldn't process the MSA collision file because the input subfile was empty.");
                return;
            }

            int meshDataOffset = Utility.ReadInt32BigEndian(basis.filebytes, 0x14);

            int pos = meshDataOffset + 0x08;
            int offsetOfEndOfMeshData = meshDataOffset + Utility.ReadInt32BigEndian(basis.filebytes, pos);

            pos = meshDataOffset + 0x10;
            int sectionCount = Utility.ReadInt32BigEndian(basis.filebytes, pos);
            pos += 4;

            int[] sectionOffsets = new int[sectionCount];

            for (int i = 0; i < sectionCount; i++) { 
            sectionOffsets[i] = meshDataOffset + Utility.ReadInt32BigEndian(basis.filebytes, pos + (i * 4));
            }

            //section 0 is some other data, the rest are vertex data seemingly

            pos = sectionOffsets[0];

            //deal with section 0 here

            //now deal with other sections

            for (int i = 1; i < sectionCount; i++) {

                //seems to be triangle coordinates where each one overlaps at the end, then the section is terminated with 0x00000001

                obj.Add("o object" + i);
                pos = sectionOffsets[i];

                float UnkMultiplier = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4; 
                float xUnk = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float yUnk = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float zUnk = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                

                float Unk2Multiplier = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float xUnk2 = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float yUnk2 = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float zUnk2 = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                

                float nudgeMultiplier = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float xNudge = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float yNudge = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;
                float zNudge = Utility.ReadSingleBigEndian(basis.filebytes, pos);
                pos += 4;

                pos += 12;


                while (pos < basis.filebytes.Length && Utility.ReadInt32BigEndian(basis.filebytes, pos) != 1) {

                    int chunkType = Utility.ReadInt32BigEndian(basis.filebytes, pos);
                    pos += 4;

                    Console.WriteLine("MSA collision chunk " + chunkType + " at "+ pos);
                    switch (chunkType) {

                        case 0x04:
                            //extents X Y Z of collision box
                            vertices.Add(new Vertex(Utility.ReadSingleBigEndian(basis.filebytes, pos), Utility.ReadSingleBigEndian(basis.filebytes, pos + 4), Utility.ReadSingleBigEndian(basis.filebytes, pos + 8)));  
                            pos += 12;
                            pos += 12; //seems to be a hash of a lua attribute or two? Possibly giving a name to this particular bit of collision. No obvious effects when nulled out (although there might be if the object is required by a lua script)
                            break;
                        default:
                            Console.WriteLine("Unhandled MSA collision chunk type: "+ chunkType);
                            obj.Add("#dodgy");
                            break;
                    }
                }

                foreach (Vertex v in vertices) {

                    obj.Add("v "+v.position.x+" "+v.position.y+" "+v.position.z);
                }

                vertices.Clear();

                cumulativeVerts += vertices.Count;
            }
        }
    }
}