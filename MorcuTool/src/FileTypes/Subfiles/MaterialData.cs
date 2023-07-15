using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class MaterialData
    {
        public string filename;
        

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


            public Param(byte[] bytes, int pos, bool useBigEndian) {

                if (useBigEndian)
                {
                    paramType = (MaterialParameter)Utility.ReadInt32BigEndian(bytes, pos); pos += 4;
                    unk = Utility.ReadInt32BigEndian(bytes, pos); pos += 4;
                    dataSizeDividedBy4 = Utility.ReadInt32BigEndian(bytes, pos); pos += 4;
                    dataOffset = Utility.ReadInt32BigEndian(bytes, pos);
                }
                else {
                    paramType = (MaterialParameter)BitConverter.ToInt32(bytes, pos); pos += 4;
                    unk = BitConverter.ToInt32(bytes, pos); pos += 4;
                    dataSizeDividedBy4 = BitConverter.ToInt32(bytes, pos); pos += 4;
                    dataOffset = BitConverter.ToInt32(bytes, pos);
                }
               
            }
        }

        public MaterialData(Subfile basis)
        {
            parameters = new List<Param>();
            filename = basis.filename;

            int pos = 0x04;

            bool useBigEndian = false;

            if (Utility.ReadInt32BigEndian(basis.filebytes, pos) == 0x01)
            {
                //msk and msa; subsequent data is big-endian
                pos = 0x10;
                useBigEndian = true;
            }
            else {
                //mysims; subsequent data is little-endian
                pos = 0x40; //while it doesn't look like it, MySims MATD files start with 0x2C of extra data, even before the MATD magic. So in total there are 0x40 bytes before the actual data start (including 4 extra ones just after the MATD magic)
                useBigEndian = false;
            }

            // a lot of the offsets are measured from 0x10

            //now read the parameters!

            pos += 0x08;
            int lengthOfDataSection;
            int numberOfParams;

            if (useBigEndian)
            {
                lengthOfDataSection = Utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
                numberOfParams = Utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;
            }
            else {
                lengthOfDataSection = BitConverter.ToInt32(basis.filebytes, pos); pos += 4;
                numberOfParams = BitConverter.ToInt32(basis.filebytes, pos); pos += 4;
            }

            //now the param list begins. Each param has 0x10 bytes describing it, with an offset to later in the file (falling within the data section, but measured from 0x10)

            for (int i = 0; i < numberOfParams; i++) {
                parameters.Add(new Param(basis.filebytes,pos,useBigEndian));
                pos += 0x10;
            }

            //now process the data for each parameter

            foreach (Param parameter in parameters) {

                pos = 0x10 + parameter.dataOffset;

                switch (parameter.paramType) {
                    case MaterialParameter.diffuseMap:
                        ulong hash_of_diffuse_texture;
                        uint typeID_of_diffuse_texture;

                        if (useBigEndian)
                        {
                            hash_of_diffuse_texture = (ulong)Utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;
                            hash_of_diffuse_texture |= ((ulong)Utility.ReadUInt32BigEndian(basis.filebytes, pos)) << 32; pos += 4;
                            typeID_of_diffuse_texture = Utility.ReadUInt32BigEndian(basis.filebytes, pos); pos += 4;
                        }
                        else {
                            hash_of_diffuse_texture = (ulong)BitConverter.ToUInt32(basis.filebytes, pos); pos += 4;
                            hash_of_diffuse_texture |= ((ulong)BitConverter.ToUInt32(basis.filebytes, pos)) << 32; pos += 4;
                            typeID_of_diffuse_texture = BitConverter.ToUInt32(basis.filebytes, pos); pos += 4;
                        }

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
