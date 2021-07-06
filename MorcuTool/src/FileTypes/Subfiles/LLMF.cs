using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public class LLMF           //LLMF level layout data
    {

        public List<LevelEntry> levelModels = new List<LevelEntry>();
        public List<ObjectEntry> objects = new List<ObjectEntry>();
        public class LevelEntry
        {
            public ulong modelHash;
        }

        public class ObjectEntry {

            public ulong modelHash;

            public float X;
            public float Y;
            public float Z;

            public float xRot;
            public float yRot;
            public float zRot;

            public float xScale;
            public float yScale;
            public float zScale;

            public int materialCount;
        }

        public LLMF (Subfile basis) {

            int pos = 0x28;

            int numLevelModels = utility.ReadInt32BigEndian(basis.filebytes,pos);
            pos += 4;
            int numObjects = utility.ReadInt32BigEndian(basis.filebytes, pos);
            pos += 4;

            pos = 0x48;
            
            for (int i = 0; i < numLevelModels; i++) {
                levelModels.Add(new LevelEntry() { modelHash = utility.ReverseEndianULong(BitConverter.ToUInt64(basis.filebytes,pos))});
                pos += 8;
            }

            for (int i = 0; i < numObjects; i++)
            {
                ObjectEntry newObject = new ObjectEntry();
                newObject.modelHash = utility.ReverseEndianULong(BitConverter.ToUInt64(basis.filebytes, pos)); pos += 8;
                newObject.X = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.Y = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.Z = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.xRot = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.yRot = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.zRot = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.xScale = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.yScale = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;
                newObject.zScale = utility.ReadSingleBigEndian(basis.filebytes, pos); pos += 4;

                newObject.materialCount = utility.ReadInt32BigEndian(basis.filebytes, pos); pos += 4;

                for (int j = 0; j < newObject.materialCount; j++) {
                    pos += 16;
                }

                objects.Add(newObject);
            }
        }


        public void GenerateReport() {

            List<string> report = new List<string>();

            report.Add("**** LLMF REPORT ****\n\n");
            report.Add("Main level models:\n");

            foreach (LevelEntry levelModel in levelModels) {

                if (global.activeVault != null && global.activeVault.VaultHashesAndFileNames.Keys.Contains(levelModel.modelHash))
                {
                    report.Add("Level model with name " + global.activeVault.VaultHashesAndFileNames[levelModel.modelHash]);

                }
                else {
                    report.Add("Level model with hash " + levelModel.modelHash);
                }
                report.Add("");
            }

            report.Add("Objects:\n");

            foreach (ObjectEntry o in objects)
            {

                if (global.activeVault != null && global.activeVault.VaultHashesAndFileNames.Keys.Contains(o.modelHash))
                {
                    report.Add("Object with name " + global.activeVault.VaultHashesAndFileNames[o.modelHash]);
                }
                else
                {
                    report.Add("Object with hash " + o.modelHash);
                }

                report.Add("Position: " + o.X + " " + o.Y + " " + o.Z);
                report.Add("Rotation: " + o.xRot + " " + o.yRot + " " + o.zRot);
                report.Add("Scale: " + o.xScale + " " + o.yScale + " " + o.zScale);

                report.Add("");
            }

            foreach (string s in report) {
                Console.WriteLine(s);
            }

        }
    }
}
