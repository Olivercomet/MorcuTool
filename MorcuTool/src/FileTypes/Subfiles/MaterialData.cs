using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class MaterialData
    {
        public List<Param> parameters = new List<Param>();
        public enum MaterialParameter : uint {

            diffuseColor = 0x7FEE2D1A,
            useLights = 0x76F88689,
            highlightMultiplier = 0x2616B09A,
            diffuseMap = 0x6CC0FD85,
            ambientMap = 0x20CB22B7,
            specularMap = 0xAD528A60,   
            alphaMap = 0x2A20E51B,
            shadowReceiver = 0xF46B90AE, 
            blendmode = 0xB2649C2F,     
            unk = 0x988403F9   

        }

        public class Param {
            public MaterialParameter paramType;
            public int unk;
            public int dataSizeDividedBy4;
            public int dataOffset;

            //DiffuseMap stuff

            public Subfile diffuse_texture;


            public Param(byte[] bytes, int pos) {

                paramType = (MaterialParameter)utility.ReadInt32BigEndian(bytes, pos); pos += 4;
                unk = utility.ReadInt32BigEndian(bytes, pos); pos += 4;
                dataSizeDividedBy4 = utility.ReadInt32BigEndian(bytes, pos); pos += 4;
                dataOffset = utility.ReadInt32BigEndian(bytes, pos);
            }
        }

        public MaterialData(Subfile basis)
        {
            parameters = new List<Param>();
            int pos = 0x10;

            // a lot of the offsets are measured from 0x10

            //now read the parameters!

            pos += 0x08;

            int lengthOfDataSection = utility.ReadInt32BigEndian(basis.filebytes,pos); pos += 4;
            int numberOfParams = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;

            //now the param list begins. Each param has 0x10 bytes describing it, with an offset to later in the file (falling within the data section, but measured from 0x10)

            for (int i = 0; i < numberOfParams; i++) {
                parameters.Add(new Param(basis.filebytes,pos));
                pos += 0x10;
            }

            //now process the data for each parameter

            foreach (Param parameter in parameters) {

                pos = 0x10 + parameter.dataOffset;

                switch (parameter.paramType) {
                    case MaterialParameter.diffuseMap:
                        ulong hash_of_diffuse_texture = (ulong)utility.ReadUInt32BigEndian(basis.filebytes,pos); pos += 4;
                        hash_of_diffuse_texture |= ((ulong)utility.ReadUInt32BigEndian(basis.filebytes, pos)) << 32; pos += 4;
                        if (hash_of_diffuse_texture == 0x58CC31A1AC11E901) {
                            Console.WriteLine("Here it is!!");
                        }

                        uint typeID_of_diffuse_texture = utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;

                        parameter.diffuse_texture = global.activePackage.FindFileByHashAndTypeID(hash_of_diffuse_texture,typeID_of_diffuse_texture);
                        break;
                    default:
                        Console.WriteLine("Unhandled material parameter: " + parameter.paramType);
                        break;
                }
            }
        }
    }
}
