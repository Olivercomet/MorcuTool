using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorcuTool
{
    public static class global
    {
        public static Package activePackage = new Package();

        public static Vault activeVault = new Vault();

        public enum TypeID : uint
        { 
        
        RMDL_MSK = 0xF9E50586,  //"ModelData"      very accomodating of the gamecube/wii gpu and uses many of its display list conventions
        RMDL_MSA = 0x2954E734,  
        MATD_MSK = 0x01D0E75D,  //"MaterialData"
        MATD_MSA = 0xE6640542,
        TPL_MSK = 0x00B2D882,   //"TextureData"     very similar to Nintendo tpl (just has a different header)
        TPL_MSA = 0x92AA4D6A,
        MTST_MSK = 0x02019972,  //"MaterialSetData"
        MTST_MSA = 0x787E842A,
        FPST_MS = 0x2c81b60a,   //Footprint set MySims
        FPST_MSK = 0x8101A6EA,  //"FootprintData"
        FPST_MSA = 0x0EFC1A82,
        BNK_MSK = 0xB6B5C271,   //"AudioData"       vgmstream can decode these
        BNK_MSA = 0x2199BB60,
        BIG_MSK = 0x5bca8c06,       //"AptData" Standard EA .BIG archive
        BIG_MSA = 0x2699C28D,       //Standard EA .BIG archive
        COLLISION_MSA = 0x1A8FEB14,
        FX = 0x6B772503,
        LUAC_MSA = 0x3681D75B,
        LUAC_MSK = 0x2B8E2411,   //"LuaObjectData"
        SLOT_MSK = 0x487BF9E4,  //"SlotData"
        SLOT_MSA = 0x2EF1E401,     //SLOT MSA
        PARTICLES_MSA = 0x28707864,
        BUILDABLEREGION_MSA = 0x41C4A8EF,
        BUILDABLEREGION_MSK = 0xC84ACD30,
        LLMF_MSK = 0x58969018,      //"LevelData"
        LLMF_MSA = 0xA5DCD485,      //(FNV-1 32bit hash of "level")
        RIG_MSK = 0x8EAF13DE,     //"RigData"   based on granny3D seemingly
        RIG_MSA = 0x4672E5BD,     //Interesting granny struct info at 0x49CFDD in MSA's main.dol
        ANIMCLIP_MSK =0x6B20C4F3,   //"ClipData"
        ANIMCLIP_MSA = 0xD6BEDA43,
        LTST_MSA = 0xE55D5715,  //possibly lighting set?
        TTF_MSK = 0x89AF85AD,         //TrueType font MySims Kingdom
        TTF_MSA = 0x276CA4B9,         //TrueType font MySims Agents
        HKX_MSK = 0xD5988020,    //"PhysicsData"    MSK HKX havok collision file
        OGVD_MSK = 0xD00DECF5,   // "ObjectGridVolumeData"
        OGVD_MSA = 0x8FC0DE5A,       //MSA ObjectGridVolumeData bounding box collision (for very simple objects)
        SPD_MSK = 0xB70F1CEA,    //"SnapPointData"
        SPD_MSA = 0x5027B4EC,
        VGD_MSK = 0x614ED283,   //"VoxelGridData"
        VGD_MSA = 0x9614D3C0,
        MODEL_MS = 0x01661233,   //model          used by MySims, not the same as rmdl
        KEYNAMEMAP_MS = 0x0166038c,
        GEOMETRY_MS = 0x015A1849,
        OLDSPEEDTREE_MS = 0x00b552ea,
        SPEEDTREE_MS = 0x021d7e8c,
        COMPOSITETEXTURE_MS = 0x8e342417,
        SIMOUTFIT_MS = 0x025ed6f4,
        LEVELXML_MS = 0x585ee310,
        LUA_MSK = 0x474999b4,   //"LuaTextData"
        LIGHTSETXML_MS = 0x50182640,    //Light set XML MySims
        LIGHTSETBIN_MSK = 0x50002128,    //"LightSetData"
        XML_MS = 0xdc37e964,
        OBJECTCONSTRUCTIONXML_MS = 0xc876c85e,
        OBJECTCONSTRUCTIONBIN_MS = 0xc08ec0ee,
        SLOTXML_MS = 0x4045d294,
        SWARM_MS = 0x9752e396,
        SWARM_MSK = 0xcf60795e,     //"SwarmData"
        XMLBIN_MS = 0xe0d83029,
        CABXML_MS = 0xa6856948,
        CABBIN_MS = 0xc644f440,
        LIGHTBOXXML_MS = 0xb61215e9,
        LIGHTBOXBIN_MS = 0xd6215201,  //LightBoxBin
        XMB_MS = 0x1e1e6516
        }
    }
}
