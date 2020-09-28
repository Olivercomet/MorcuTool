using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MorcuTool
{
    public class Vault
    {
        public string filename = "";

        public Dictionary<ulong, ulong> VaultHashesAndIndexes = new Dictionary<ulong, ulong>();
        public Dictionary<ulong, string> VaultHashesAndFileNames = new Dictionary<ulong, string>();

        public List<luaString> luaStrings = new List<luaString>();

        public class luaString
        {
            public uint hash = 0;
            public string name = "";
            public uint nameOffset = 0;
        }


        public void LoadVault() {
            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                //first, for lua strings (i.e. variable names etc)

                reader.BaseStream.Position = 0x2D6FF3;

                while (reader.BaseStream.Position < 0x32EBEB)   //I don't know if this is quite the right address
                {
                    luaString newLuaString = new luaString();
                    newLuaString.hash = utility.ReverseEndian(reader.ReadUInt32());
                    newLuaString.nameOffset = utility.ReverseEndian(reader.ReadUInt32());

                    luaStrings.Add(newLuaString);
                }

                reader.BaseStream.Position = 0x32EBEB;

                for (int i = 0; i < luaStrings.Count; i++)
                {
                    reader.BaseStream.Position = 0x32EBEB + luaStrings[i].nameOffset;

                    if (reader.BaseStream.Position > reader.BaseStream.Length)
                    {
                        break;
                    }

                    while (reader.ReadByte() != 0x00)
                    {
                        reader.BaseStream.Position--;
                        luaStrings[i].name += reader.ReadChar() + "";
                    }
                }


                //and now for filenames
                reader.BaseStream.Position = 0x556241;

                for (uint i = 0; i < 0xEDB4; i++)
                {
                    ulong hash = utility.ReverseEndianULong(reader.ReadUInt64());
                    ulong index = utility.ReverseEndianULong(reader.ReadUInt64());

                    VaultHashesAndIndexes.Add(hash, index);
                }

                foreach (ulong hash in VaultHashesAndIndexes.Keys)
                {
                    reader.BaseStream.Position = (0x643D90 + (long)VaultHashesAndIndexes[hash]) + 1;

                    List<Byte> bytes = new List<Byte>();

                    byte newbyte = 0x00;

                    readanotherbyte:


                    newbyte = reader.ReadByte();

                    if (reader.BaseStream.Position == (0x643D90 + (long)VaultHashesAndIndexes[hash]) + 2 && newbyte == 0x00)
                    {
                        goto readanotherbyte;
                    }

                    if (reader.BaseStream.Position == reader.BaseStream.Length - 1)
                    {

                    }
                    else
                    {
                        if (newbyte != 0x00)
                        {
                            bytes.Add(newbyte);
                            if (reader.BaseStream.Position < reader.BaseStream.Length - 1)
                            {
                                goto readanotherbyte;
                            }
                        }
                    }


                    VaultHashesAndFileNames.Add(hash, System.Text.Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.ToArray().Length));
                }

                if (global.activePackage.packageType == Package.PackageType.Agents) //then add the lua filenames that aren't in the vaults
                {
                    VaultHashesAndFileNames.Add(0x001935F4D51D2A44, "Tent_Interaction_TakeNap");
                    VaultHashesAndFileNames.Add(0x00456145F62C640D, "EmbeddedBuildableRegion");
                    VaultHashesAndFileNames.Add(0x00AE7406DF8B142C, "PerFrameSingleton");
                    VaultHashesAndFileNames.Add(0x0158B1BB6F7429AD, "DanceFloor_Interaction_Dance");
                    VaultHashesAndFileNames.Add(0x0184FCB3A64CA6C5, "Couch");
                    VaultHashesAndFileNames.Add(0x01DB4D1FA64057B4, "Job_RequestCharacterControl");
                    VaultHashesAndFileNames.Add(0x05836ACC88FCD2F6, "NPC_Declarations");
                    VaultHashesAndFileNames.Add(0x05A1B707325B3E05, "Television_Interaction_TurnOn");
                    VaultHashesAndFileNames.Add(0x0651778A074A45AB, "Job_WorldChange");
                    VaultHashesAndFileNames.Add(0x06974C8C833586B2, "Forensics_Interaction_Forensify");
                    VaultHashesAndFileNames.Add(0x07A052273141CD7B, "UINunchuk");
                    VaultHashesAndFileNames.Add(0x08325307B4EB15B7, "UI");
                    VaultHashesAndFileNames.Add(0x08326307B4EB316F, "EA");
                    VaultHashesAndFileNames.Add(0x088A1290BBC886FC, "SushiStool_Interaction_Eat");
                    VaultHashesAndFileNames.Add(0x09725B165CF2EF07, "ModalUtils");
                    VaultHashesAndFileNames.Add(0x0A82DBD165BE2493, "Refrigerator");
                    VaultHashesAndFileNames.Add(0x0A8F16764DDF9B30, "Job_TeleportNonBlocking");
                    VaultHashesAndFileNames.Add(0x0AAA13B8932AC848, "DJBooth_Interaction_Dance");
                    VaultHashesAndFileNames.Add(0x0BB037190D3631FD, "Hottub_Interaction_Relax");
                    VaultHashesAndFileNames.Add(0x0BFED339C3B72C01, "Boat_Interaction_LeaveIsland");
                    VaultHashesAndFileNames.Add(0x0C4B91AC8CC30B34, "Job_InteractionBase");
                    VaultHashesAndFileNames.Add(0x0C7DFFB3AD2B63E0, "Chair");
                    VaultHashesAndFileNames.Add(0x0D441381588FE08C, "Television_Interaction_Watch");
                    VaultHashesAndFileNames.Add(0x0DC07132436EE700, "Chair_Interaction_Sit");
                    VaultHashesAndFileNames.Add(0x0E53D829104F2A52, "UnlockSystem");
                    VaultHashesAndFileNames.Add(0x0F3FB1BA6607A639, "Television");
                    VaultHashesAndFileNames.Add(0x10EFE2F46E3D2368, "UIModalTutorialDialog");
                    VaultHashesAndFileNames.Add(0x119A250AE12C6C36, "ScriptObjectBase");
                    VaultHashesAndFileNames.Add(0x1492A669F07EADB9, "TutorialScriptBase");
                    VaultHashesAndFileNames.Add(0x14FA8F8CF1EF9FDE, "Job_ConstructionController");
                    VaultHashesAndFileNames.Add(0x15D7B3E8F633A6EF, "Job_SpawnObject");
                    VaultHashesAndFileNames.Add(0x161A6C8522DD72E5, "TattooChair_Interaction_Sit");
                    VaultHashesAndFileNames.Add(0x165983D4AD97F695, "Job_GadgetController");
                    VaultHashesAndFileNames.Add(0x16D96016901459FB, "Trigger");
                    VaultHashesAndFileNames.Add(0x17431E11AE60F054, "TutorialTriggerObject");
                    VaultHashesAndFileNames.Add(0x17BF60E8D8248E3C, "UIBackStoryEnd");
                    VaultHashesAndFileNames.Add(0x17FBD7132AB74597, "Guitar_Interaction_Play");
                    VaultHashesAndFileNames.Add(0x1A3E56FDE819136F, "UIModalPopUpCinematic");
                    VaultHashesAndFileNames.Add(0x1AE142F645D30955, "TriggerPortal");
                    VaultHashesAndFileNames.Add(0x1CB6F87EF10442B1, "Case");
                    VaultHashesAndFileNames.Add(0x1D45CB68F9E14DC0, "MinigamePlaneVsEye");
                    VaultHashesAndFileNames.Add(0x1DFFF58F26FD23EA, "UIModalDialog");
                    VaultHashesAndFileNames.Add(0x1E07FBDAD0DAFDC0, "TurkeyCart_Interaction_Serve");
                    VaultHashesAndFileNames.Add(0x20514D852BBB7D91, "Job_ReplaceModelAndRig");
                    VaultHashesAndFileNames.Add(0x24A13F7AF74591A8, "MinigameLockPicking");
                    VaultHashesAndFileNames.Add(0x252A169ADAD95B74, "SummoningCircle");
                    VaultHashesAndFileNames.Add(0x25CA38BA7AD970CE, "StateObjectBase");
                    VaultHashesAndFileNames.Add(0x2695D47A9999E2BB, "Job_PlayAnimMachine");
                    VaultHashesAndFileNames.Add(0x2713277EF75D54AF, "Boat");
                    VaultHashesAndFileNames.Add(0x28CD396A105300DF, "Bed_Interaction_Sleep");
                    VaultHashesAndFileNames.Add(0x29239FE06DBB4557, "UIConstructionBlock");
                    VaultHashesAndFileNames.Add(0x2A5F8DCC1CB45052, "Job_Wander");
                    VaultHashesAndFileNames.Add(0x2B380CA6D46A76D8, "UIModalPopUpDialog");
                    VaultHashesAndFileNames.Add(0x2B9EE0A00BE7D7EC, "Job_UIBaseUnloader");
                    VaultHashesAndFileNames.Add(0x2D87F8701C374D67, "JobManager");
                    VaultHashesAndFileNames.Add(0x2EC0930DC697F472, "TutorialScriptStoveEBR");
                    VaultHashesAndFileNames.Add(0x2F45401EDF5021F7, "LuaDebuggerUtils");
                    VaultHashesAndFileNames.Add(0x2FC909D73838D42D, "Job_RouteToWorld");
                    VaultHashesAndFileNames.Add(0x2FE1ACEA022CE90A, "AutoTest_AllLevelLocations");
                    VaultHashesAndFileNames.Add(0x32315EE62083614B, "ObjectDeclarations");
                    VaultHashesAndFileNames.Add(0x35E8CD82108B5AFD, "Treadmill");
                    VaultHashesAndFileNames.Add(0x36FFBBDFF810FCD5, "UIRecruiting");
                    VaultHashesAndFileNames.Add(0x37649ECF542C3376, "Fireplace");
                    VaultHashesAndFileNames.Add(0x38EB36D6912C8E86, "Job_EnterEBR");
                    VaultHashesAndFileNames.Add(0x3AFF336D79171B02, "Job_RouteToPosition");
                    VaultHashesAndFileNames.Add(0x3C18C0DE65EC32C9, "Stereo");
                    VaultHashesAndFileNames.Add(0x3E4057F8852E3547, "UICredits");
                    VaultHashesAndFileNames.Add(0x3F7A9AE3503F62CB, "UICasTransitionScreen");
                    VaultHashesAndFileNames.Add(0x405B5573970BF881, "DJBooth");
                    VaultHashesAndFileNames.Add(0x406A6DCAA6F7D115, "UIInfoCard");
                    VaultHashesAndFileNames.Add(0x40862A76CB14D450, "MinigameForensics");
                    VaultHashesAndFileNames.Add(0x413CCC340CD843AD, "FloorPlan_Interaction");
                    VaultHashesAndFileNames.Add(0x420966178735F819, "Job_TeleportThroughPortal");
                    VaultHashesAndFileNames.Add(0x424DECB8E2B3C6C4, "UIPaintMode");
                    VaultHashesAndFileNames.Add(0x42A1A1749D531A83, "Job_PlayAnimation");
                    VaultHashesAndFileNames.Add(0x43EB22BC396B9C02, "ArcadeMachine_Interaction_Play");
                    VaultHashesAndFileNames.Add(0x455AAF6B065BB545, "UIDispatchSimBioPage");
                    VaultHashesAndFileNames.Add(0x457D7A599E63780B, "NPC_Kraken");
                    VaultHashesAndFileNames.Add(0x45E25E16512101BD, "Job_EnterMetaState");
                    VaultHashesAndFileNames.Add(0x45E8D16D562A4D9C, "Chair_Interaction_Jenny");
                    VaultHashesAndFileNames.Add(0x4633CEC1F5A89622, "GiantWheel_Interaction_Turn");
                    VaultHashesAndFileNames.Add(0x46AA3195900EBF27, "DryingChair_Interaction_Sit");
                    VaultHashesAndFileNames.Add(0x471FB630202C3195, "DJBooth_Interaction_DJ");
                    VaultHashesAndFileNames.Add(0x4769A20780D60437, "PlumbobTutorialScript");
                    VaultHashesAndFileNames.Add(0x47A446DBF9AACD67, "KeyItem");
                    VaultHashesAndFileNames.Add(0x48F5EE1C606090C2, "TrophyCase_Interaction_View");
                    VaultHashesAndFileNames.Add(0x4A3DEC52F5AC6FC9, "CharacterBase");
                    VaultHashesAndFileNames.Add(0x4B34AF062CE78D7A, "AutoTest_StressRoute");
                    VaultHashesAndFileNames.Add(0x4B4200FE4539BFA6, "ToolChest");
                    VaultHashesAndFileNames.Add(0x4CFCF86ED13C1DF0, "CharacterBase_Interaction_PlayerMoment");
                    VaultHashesAndFileNames.Add(0x4DFB71AD73C37997, "MechanicalBull_Interaction_Ride");
                    VaultHashesAndFileNames.Add(0x4FADDA9E7AB4BDDC, "UIClueMoment");
                    VaultHashesAndFileNames.Add(0x4FE7F07716945B01, "GameplayLoad");
                    VaultHashesAndFileNames.Add(0x51CCECD7B6427D95, "TattooChair");
                    VaultHashesAndFileNames.Add(0x52B3559810A90C55, "UIDispatchSelection");
                    VaultHashesAndFileNames.Add(0x52DB7CECCAF59FBF, "SandPile");
                    VaultHashesAndFileNames.Add(0x52FD64B7929DEA23, "Job_BehaviorController");
                    VaultHashesAndFileNames.Add(0x5307C9F1BB780E08, "UIDispatchHistoryQueue");
                    VaultHashesAndFileNames.Add(0x530BDBAD39F211BD, "UIBackStory");
                    VaultHashesAndFileNames.Add(0x53B487A8A67AE00A, "WaterCooler_Interaction_Drink");
                    VaultHashesAndFileNames.Add(0x53C73D1A54AD932D, "Campfire_Interaction_WarmHands");
                    VaultHashesAndFileNames.Add(0x53CFD1CA41F71C03, "WasteBasket_Interaction_ThrowPaper");
                    VaultHashesAndFileNames.Add(0x5400EE4FE3308A4C, "UITalkDialogCinematic");
                    VaultHashesAndFileNames.Add(0x54695476B52C31DE, "PicnicBlanket_Interaction_Eat");
                    VaultHashesAndFileNames.Add(0x54F184877C4240A7, "DryingChair");
                    VaultHashesAndFileNames.Add(0x55103C021A854533, "SalonChair");
                    VaultHashesAndFileNames.Add(0x5533D624C9D7A306, "Settings");
                    VaultHashesAndFileNames.Add(0x55638A1F3DC1276F, "TrophyCase");
                    VaultHashesAndFileNames.Add(0x566F4E120650FCDE, "Job_RouteCloseToPosition");
                    VaultHashesAndFileNames.Add(0x5768257FC715E045, "UIElevatorPanel");
                    VaultHashesAndFileNames.Add(0x592C9119D134BF75, "Lockpicking_Interaction_PickLock");
                    VaultHashesAndFileNames.Add(0x592CDD42838AC10B, "UICASMenu");
                    VaultHashesAndFileNames.Add(0x593E47BD29393447, "Dresser");
                    VaultHashesAndFileNames.Add(0x5A07962F9797D324, "Stove");
                    VaultHashesAndFileNames.Add(0x5A7980A6B671026F, "Event");
                    VaultHashesAndFileNames.Add(0x5AAD4D65D6C7E18C, "Job_PerFrameFunctionCallback");
                    VaultHashesAndFileNames.Add(0x5B8A6BC603831B58, "Constants");
                    VaultHashesAndFileNames.Add(0x5D00016438F55408, "Couch_Interaction_Sleep");
                    VaultHashesAndFileNames.Add(0x5D6B42026E15C485, "ManaVent");
                    VaultHashesAndFileNames.Add(0x5DC7D2FA72588124, "TattooChair_Interaction_Tattoo");
                    VaultHashesAndFileNames.Add(0x605B081C628E0C93, "SkiLift");
                    VaultHashesAndFileNames.Add(0x605D99A196DE0D75, "Couch_Interaction_Sit");
                    VaultHashesAndFileNames.Add(0x6150A8603C24C113, "DispatchMission");
                    VaultHashesAndFileNames.Add(0x63870C41BAAF4261, "UITutorialScreen");
                    VaultHashesAndFileNames.Add(0x6418A437180B4C92, "Job_InteractionPeeking");
                    VaultHashesAndFileNames.Add(0x672F6ADC3B486455, "Job_PlayIdleAnimation");
                    VaultHashesAndFileNames.Add(0x676721A9E7966307, "Job_RouteToSlot");
                    VaultHashesAndFileNames.Add(0x678CD19FD89B467B, "Job_SocialBase");
                    VaultHashesAndFileNames.Add(0x68D70BC24AFE550E, "Mirror_CAS");
                    VaultHashesAndFileNames.Add(0x69072AC8459380D7, "CharacterBase_Interaction_Interrupted");
                    VaultHashesAndFileNames.Add(0x6939169BFDB6397C, "DanceFloor");
                    VaultHashesAndFileNames.Add(0x6976FC2FA0E6EEEB, "UIModalDialogNoAudio");
                    VaultHashesAndFileNames.Add(0x6A0AFD06EEBC9003, "Mirror_Interaction_CAS");
                    VaultHashesAndFileNames.Add(0x6A0BB92CD97E85D9, "UISlideShow");
                    VaultHashesAndFileNames.Add(0x6A7854C82C35F630, "WorldBase");
                    VaultHashesAndFileNames.Add(0x6AA940DA96C2AF5B, "SalonChair_Interaction_Sit");
                    VaultHashesAndFileNames.Add(0x6C6A780DD6AE3468, "Job_FadeColor");
                    VaultHashesAndFileNames.Add(0x6F037B44B6E2BF6A, "SystemLoad");
                    VaultHashesAndFileNames.Add(0x73B5847F29E11944, "CASInitialWorld");
                    VaultHashesAndFileNames.Add(0x73F42DA6A6FA6CE7, "Examine_Interaction");
                    VaultHashesAndFileNames.Add(0x74F6952C89F5B74D, "ClimbingObject");
                    VaultHashesAndFileNames.Add(0x75D03549D3496728, "UIDebugTextTester");
                    VaultHashesAndFileNames.Add(0x77265349B43D53CA, "System");
                    VaultHashesAndFileNames.Add(0x7758290B3FC3582A, "Job_RouteToObject");
                    VaultHashesAndFileNames.Add(0x784975BB99BFBD6E, "UICitySelect");
                    VaultHashesAndFileNames.Add(0x78CC1F95BBECECB8, "UIModalDialogError");
                    VaultHashesAndFileNames.Add(0x791977EAE1DC563D, "UIDispatchMissionCardHistory");
                    VaultHashesAndFileNames.Add(0x79C230851BD66F75, "CharacterBase_Interaction_Idle");
                    VaultHashesAndFileNames.Add(0x7A7A3060C70852C3, "Hottub");
                    VaultHashesAndFileNames.Add(0x7AECB9E645F1549E, "DoorBase");
                    VaultHashesAndFileNames.Add(0x7B2E23540F9F4F35, "Debug_Interaction_ForceNPCUse");
                    VaultHashesAndFileNames.Add(0x7BB7BBA0C67CD1B5, "Stereo_Interaction_TurnOn");
                    VaultHashesAndFileNames.Add(0x7E7FEA04E18483A3, "Trophy");
                    VaultHashesAndFileNames.Add(0x7E9AFF520101ADAF, "Debug_Interaction_DebugEBRs");
                    VaultHashesAndFileNames.Add(0x822E234831977CE4, "Piano");
                    VaultHashesAndFileNames.Add(0x8353D53CE41FAB13, "Guitar");
                    VaultHashesAndFileNames.Add(0x837FCCA91E3854B3, "UISpinningFish");
                    VaultHashesAndFileNames.Add(0x8425FF218A0E5038, "Boat_Interaction_ChangeOutfit");
                    VaultHashesAndFileNames.Add(0x8470314E8A018AF2, "CharacterBase_Interaction_Social");
                    VaultHashesAndFileNames.Add(0x8670986E962AEC4C, "Job_RouteToBehaviorBlock");
                    VaultHashesAndFileNames.Add(0x874B80870A7FC57E, "Job_InteractionMachine");
                    VaultHashesAndFileNames.Add(0x8765BBF67881E255, "WolfCage");
                    VaultHashesAndFileNames.Add(0x878E33002ECA1035, "UITutorialPopup");
                    VaultHashesAndFileNames.Add(0x87C9727C6E467AC1, "DebugDisplayManager");
                    VaultHashesAndFileNames.Add(0x87F022C4E5E494F0, "TutorialScriptHQConstruction");
                    VaultHashesAndFileNames.Add(0x88823CBB1377EB98, "UIMinigame");
                    VaultHashesAndFileNames.Add(0x8925450BCA53DA03, "Refrigerator_Interaction_GetSnack");
                    VaultHashesAndFileNames.Add(0x89DA304BE0E3903C, "UICaseBook");
                    VaultHashesAndFileNames.Add(0x8AFD98453DBC279F, "UILangSelect");
                    VaultHashesAndFileNames.Add(0x8C09427E9FCCC442, "Tent");
                    VaultHashesAndFileNames.Add(0x8C544A4837C2F1EF, "Phone");
                    VaultHashesAndFileNames.Add(0x8CFAB0EA977D2328, "WaterCooler_Interaction_HangOut");
                    VaultHashesAndFileNames.Add(0x8EA580475A689AE9, "JobBase");
                    VaultHashesAndFileNames.Add(0x8EA88480D7B9249D, "PizzaOven_Interaction_Cook");
                    VaultHashesAndFileNames.Add(0x8EF406C8B880F9E8, "Job_RouteToFootprint");
                    VaultHashesAndFileNames.Add(0x8F8F5800ED8CAAB6, "Treadmill_Interaction_Run");
                    VaultHashesAndFileNames.Add(0x905CA6F2ADAAF9C3, "Bathtub");
                    VaultHashesAndFileNames.Add(0x91D2BCFB0621A865, "UISavingDialog");
                    VaultHashesAndFileNames.Add(0x9304F604998FC578, "CharacterBase_Interaction_React");
                    VaultHashesAndFileNames.Add(0x94189C1CB2FFC090, "Stereo_Interaction_Dance");
                    VaultHashesAndFileNames.Add(0x94C0041CA209C6DD, "GameObjectBase");
                    VaultHashesAndFileNames.Add(0x94CCE9E1515BD522, "Newspaper");
                    VaultHashesAndFileNames.Add(0x97744E0364705485, "UIFloorSelectCard");
                    VaultHashesAndFileNames.Add(0x97CD0F76BF8CB00A, "BackstoryLocation");
                    VaultHashesAndFileNames.Add(0x98D784AD8A538171, "Campfire_Interaction_RoastMarshmallows");
                    VaultHashesAndFileNames.Add(0x98FE22770ADB2E8B, "UserSettings");
                    VaultHashesAndFileNames.Add(0x9926E7DE0A17E7EE, "Strict");
                    VaultHashesAndFileNames.Add(0x9978E1B45CF6667A, "UITransitionScreen");
                    VaultHashesAndFileNames.Add(0x9A11C60F104A99D3, "TurkeyCart_Interaction_Eat");
                    VaultHashesAndFileNames.Add(0x9A2AFE385ECA4BEE, "DragonHead");
                    VaultHashesAndFileNames.Add(0x9AAF85102A858102, "AudioScriptObjectBase");
                    VaultHashesAndFileNames.Add(0x9C5ADF436D04C6C2, "Common");
                    VaultHashesAndFileNames.Add(0x9CBB20F2FEA0D244, "Job_ClimbingController");
                    VaultHashesAndFileNames.Add(0x9DEED08530C24248, "CharacterBase_Interaction_Move");
                    VaultHashesAndFileNames.Add(0x9EA0B784920B1A96, "Job_BalancingController");
                    VaultHashesAndFileNames.Add(0xA07642BA7F5CE77F, "TutorialController");
                    VaultHashesAndFileNames.Add(0xA10CE5A3B3D96953, "Balancing_Interaction_Balancing");
                    VaultHashesAndFileNames.Add(0xA11879910E61F6D7, "Job_PackageLoad");
                    VaultHashesAndFileNames.Add(0xA22FF8BD624704A4, "Job_ShowTextMessage");
                    VaultHashesAndFileNames.Add(0xA348C7921596FD59, "UIEBRReset");
                    VaultHashesAndFileNames.Add(0xA3B871C555CDA63B, "Inventory");
                    VaultHashesAndFileNames.Add(0xA449964119DED5FF, "ElectroDanceSphere");
                    VaultHashesAndFileNames.Add(0xA4D6B9AB9A5BA0DC, "EventManager");
                    VaultHashesAndFileNames.Add(0xA5E8A13E0D940096, "AutoTest_HQ");
                    VaultHashesAndFileNames.Add(0xA662B1E22228F7BA, "Pedestal_Interaction_NPC");
                    VaultHashesAndFileNames.Add(0xA6F95D969293D81E, "UIFloorplan");
                    VaultHashesAndFileNames.Add(0xA73D89BFAFE19E67, "BalancingObject");
                    VaultHashesAndFileNames.Add(0xA86C3D3C315C3D35, "Job_RotateToFaceObject");
                    VaultHashesAndFileNames.Add(0xA984CA9289595512, "InteractionUtils");
                    VaultHashesAndFileNames.Add(0xA98902AB3294EF38, "Dresser_Interaction_RifleThroughClothes");
                    VaultHashesAndFileNames.Add(0xA9F90C9F3FF1540C, "LeadTriggerObject");
                    VaultHashesAndFileNames.Add(0xAA1CFC1617A9FC0B, "Job_RouteToPosition3D");
                    VaultHashesAndFileNames.Add(0xAAE8AA656E569306, "Job_RotateToFacePos");
                    VaultHashesAndFileNames.Add(0xAB87C2E749E00778, "UIBase");
                    VaultHashesAndFileNames.Add(0xAC52691AF3EFAA84, "Icicle");
                    VaultHashesAndFileNames.Add(0xAD0119BA4AE8109B, "UIOptionsScreen");
                    VaultHashesAndFileNames.Add(0xAD0A26C035FA388E, "BlockObjectBase");
                    VaultHashesAndFileNames.Add(0xAD75A4A5134B7A63, "MechanicalBull");
                    VaultHashesAndFileNames.Add(0xADC085B703810B09, "Phases");
                    VaultHashesAndFileNames.Add(0xAE0B61B86E7DE954, "InterestingObject");
                    VaultHashesAndFileNames.Add(0xAE106955C78D1765, "Barrel");
                    VaultHashesAndFileNames.Add(0xAE2D26432FF1110A, "UIUtils");
                    VaultHashesAndFileNames.Add(0xAE6BAFFA2920CF39, "UIMinigameHelp");
                    VaultHashesAndFileNames.Add(0xAF959ADAF05FB0D5, "CharacterBase_Interaction_Dispatch");
                    VaultHashesAndFileNames.Add(0xB04BA87DB63C78CD, "AutoTest_LevelLocations");
                    VaultHashesAndFileNames.Add(0xB04FC46F0911FD25, "FlourMill");
                    VaultHashesAndFileNames.Add(0xB190B0D0710DD54C, "lowBatteryDialog");
                    VaultHashesAndFileNames.Add(0xB3197A38FB3C7BDE, "UIGenericCounter");
                    VaultHashesAndFileNames.Add(0xB3EDA6BE2974CC19, "Wardrobe");
                    VaultHashesAndFileNames.Add(0xB50FAD13B80687EA, "DayNTime");
                    VaultHashesAndFileNames.Add(0xB6058F3146186441, "Stereo_Interaction_TurnOff");
                    VaultHashesAndFileNames.Add(0xB6FB405F1EBCD79C, "Job_Teleport");
                    VaultHashesAndFileNames.Add(0xB8B72BAAA00FB3D9, "Crystal");
                    VaultHashesAndFileNames.Add(0xB90ED50F0AC3E451, "TurkeyCart");
                    VaultHashesAndFileNames.Add(0xB9F7CA8EF00D12CA, "Job_Sleep");
                    VaultHashesAndFileNames.Add(0xBAD07A1F0A9C79E7, "FlourMill_Interaction_MillFlour");
                    VaultHashesAndFileNames.Add(0xBBC0F24EC6C1BB9D, "UIControllerDisconnect");
                    VaultHashesAndFileNames.Add(0xBC0A24075E09390E, "UILetterbox");
                    VaultHashesAndFileNames.Add(0xBC2C596CB17CF394, "AmbientCritter");
                    VaultHashesAndFileNames.Add(0xBCA1FF16BB403BAD, "AutoTestManager");
                    VaultHashesAndFileNames.Add(0xBEB09623102178BC, "Speaker");
                    VaultHashesAndFileNames.Add(0xBF265AF557D3A298, "UIComputerDialog");
                    VaultHashesAndFileNames.Add(0xC02237148FA15DD4, "Stove_Interaction_Cook");
                    VaultHashesAndFileNames.Add(0xC1B2A0C74CD5ED9E, "EffectDummy");
                    VaultHashesAndFileNames.Add(0xC1C9DE6BA67C55DC, "NotFinalLoad");
                    VaultHashesAndFileNames.Add(0xC1FC4FD2714EF068, "GumballMachine_Interaction_Buy");
                    VaultHashesAndFileNames.Add(0xC2FEC01D3CBC3F30, "UIMovieMenu");
                    VaultHashesAndFileNames.Add(0xC4161046A63EC9CD, "SummoningCircle_Interaction_Seance");
                    VaultHashesAndFileNames.Add(0xC68A5917B1D68666, "JamJar");
                    VaultHashesAndFileNames.Add(0xC7E55B48075B3517, "MinigameHacking");
                    VaultHashesAndFileNames.Add(0xC9FF2979ADD842C2, "ClueTracker");
                    VaultHashesAndFileNames.Add(0xCAA324845C1B7F4C, "UIKeyboard");
                    VaultHashesAndFileNames.Add(0xCC39ED71ECA7D167, "Job_TriggerWaitUntilSafe");
                    VaultHashesAndFileNames.Add(0xCC6AECA558D82EA0, "UICallouts");
                    VaultHashesAndFileNames.Add(0xCCFA19CF9CCA890B, "Pedestal");
                    VaultHashesAndFileNames.Add(0xCD788DBBCFCB13EF, "UIDispatchMissionCard");
                    VaultHashesAndFileNames.Add(0xCDEC7F89F6CDE78F, "Dumbwaiter");
                    VaultHashesAndFileNames.Add(0xCEE153B282742719, "Social_TalkPlusPlusPlus");
                    VaultHashesAndFileNames.Add(0xCFBCCB7934564563, "UILoadingGameError");
                    VaultHashesAndFileNames.Add(0xCFC92BF8D245D381, "UIModalPopUpDialogNoAudio");
                    VaultHashesAndFileNames.Add(0xD04C6560DBE90DC9, "Job_CutsceneController");
                    VaultHashesAndFileNames.Add(0xD07B657EC653955B, "Lead");
                    VaultHashesAndFileNames.Add(0xD0E15C2ED6A73E0D, "EffectBase");
                    VaultHashesAndFileNames.Add(0xD1E71C7891ED7D9F, "SoundDummyScript");
                    VaultHashesAndFileNames.Add(0xD252DA4569FE32BB, "PhaseManager");
                    VaultHashesAndFileNames.Add(0xD36BFD55F0BF3B0A, "Guitar_Interaction_Watch");
                    VaultHashesAndFileNames.Add(0xD408F366D951F6F8, "Couch_Interaction_JumpOn");
                    VaultHashesAndFileNames.Add(0xD452D0115264055D, "Shaking_Interaction");
                    VaultHashesAndFileNames.Add(0xD58052BF5715833E, "UIRewardDialog");
                    VaultHashesAndFileNames.Add(0xD583CB89D5A5A9B8, "UIConstructionInventory");
                    VaultHashesAndFileNames.Add(0xD86EF4B3B74171F7, "SoundTrack");
                    VaultHashesAndFileNames.Add(0xD87B6670AA89C490, "Harrier");
                    VaultHashesAndFileNames.Add(0xD87CCD93E54841C9, "Paragraph");
                    VaultHashesAndFileNames.Add(0xD8D9A1186BAD3242, "Bed");
                    VaultHashesAndFileNames.Add(0xDA1489933781A64A, "Player");
                    VaultHashesAndFileNames.Add(0xDAD842C3675F0915, "CharacterBase_Debug_PushSim");
                    VaultHashesAndFileNames.Add(0xDAE4F98F4C7F47EF, "BuildableRegion");
                    VaultHashesAndFileNames.Add(0xDB08E135E5ED71E8, "SalonChair_Interaction_Style");
                    VaultHashesAndFileNames.Add(0xDB23EE15785C22FB, "Debug_Interaction_DebugCutscene");
                    VaultHashesAndFileNames.Add(0xDDDC3AC692FE9511, "UIDispatchMsgQueue");
                    VaultHashesAndFileNames.Add(0xDE1119717F3EDEE9, "UITalkDialog");
                    VaultHashesAndFileNames.Add(0xDEAE555161A08A81, "PauseScreen");
                    VaultHashesAndFileNames.Add(0xE1E2E12BA5B265D8, "CharacterBase_Interaction_Wander");
                    VaultHashesAndFileNames.Add(0xE1FBF68380887D0B, "EffectScript");
                    VaultHashesAndFileNames.Add(0xE21D7A93923E4662, "DummyScript");
                    VaultHashesAndFileNames.Add(0xE21F75E5FD5A2C5A, "UILoadMenu");
                    VaultHashesAndFileNames.Add(0xE3090F30C490073F, "AutoTest_StressAnimation");
                    VaultHashesAndFileNames.Add(0xE58660DB7E93C9CA, "Computer");
                    VaultHashesAndFileNames.Add(0xE5B7D0AF413CAAF9, "TreasureChest");
                    VaultHashesAndFileNames.Add(0xE6B76EE2AD555A9A, "Pedestal_Interaction_Player");
                    VaultHashesAndFileNames.Add(0xE75FE22F1086F2B0, "UIMinigameLoadScreen");
                    VaultHashesAndFileNames.Add(0xE7BCD0C9CD97B53F, "Job_InputListener");
                    VaultHashesAndFileNames.Add(0xE7E5F8C9DBAC393F, "Bathtub_Interaction_TakeBath");
                    VaultHashesAndFileNames.Add(0xE82569B3981A66FB, "Class");
                    VaultHashesAndFileNames.Add(0xEA0D7EA36AEDB848, "PortalBase");
                    VaultHashesAndFileNames.Add(0xEA1D821A57896B9C, "TransitionWorld");
                    VaultHashesAndFileNames.Add(0xEA5E1D1ACA3567D2, "Campfire");
                    VaultHashesAndFileNames.Add(0xEAC820CF65FA2AD8, "UIKeyboardJapan");
                    VaultHashesAndFileNames.Add(0xEBFBF666D2D29F42, "Hearth_Interaction_WarmHands");
                    VaultHashesAndFileNames.Add(0xEC1D2911EDCACD8F, "Elevator");
                    VaultHashesAndFileNames.Add(0xECA00CEF680ADA7D, "EDS_Interaction_Ride");
                    VaultHashesAndFileNames.Add(0xED080E3A910A7071, "Television_Interaction_TurnOff");
                    VaultHashesAndFileNames.Add(0xEE073A320273981D, "UIInitCASMenu");
                    VaultHashesAndFileNames.Add(0xEF4C90B3C36D213E, "Interest_CommonCode");
                    VaultHashesAndFileNames.Add(0xEFAAC58ABCC50171, "Hacking_Interaction_Hack");
                    VaultHashesAndFileNames.Add(0xF1ECEDF87C894647, "Job_Fade");
                    VaultHashesAndFileNames.Add(0xF227D008EB797C43, "NPC_IdleData");
                    VaultHashesAndFileNames.Add(0xF275EE09AABE0FF4, "Bookshelf_Interaction_Browse");
                    VaultHashesAndFileNames.Add(0xF33DD730713C28B6, "UIDispatchMsgCard");
                    VaultHashesAndFileNames.Add(0xF359EA9E2A564021, "Job_RunInteractionQueue");
                    VaultHashesAndFileNames.Add(0xF509915399678DCB, "DebugMenu");
                    VaultHashesAndFileNames.Add(0xF62F9E6DA123FD14, "UISocialize");
                    VaultHashesAndFileNames.Add(0xF6DBCBA96310EDF6, "Job_InteractionState");
                    VaultHashesAndFileNames.Add(0xF721886571B33B19, "UICASContextPicker");
                    VaultHashesAndFileNames.Add(0xFA9736F16BCBE321, "DebugOnScreenLogger");
                    VaultHashesAndFileNames.Add(0xFBA2B1B8625A5AE5, "Phone_Interaction_Recruit");
                    VaultHashesAndFileNames.Add(0xFC35A5FD0CD0F5D5, "Job_SpawnForInventory");
                    VaultHashesAndFileNames.Add(0xFC790A8D32C82BA5, "UIMainMenu");
                    VaultHashesAndFileNames.Add(0xFD556827143AC4ED, "Job_ProcessInteractionQueue");
                    VaultHashesAndFileNames.Add(0xFDE613259FFF01F2, "AmbientObject");
                    VaultHashesAndFileNames.Add(0xFF4D669DDA41037E, "Piano_Interaction_Play");
                    VaultHashesAndFileNames.Add(0xFFB305AD564F71DE, "UIArcadeMachine");
                }
            }
        }

        public luaString GetLuaStringWithHash(uint hash)
        {
            foreach (luaString l in luaStrings)
            {
                if (l.hash == hash)
                {
                    return l;
                }
            }
            return null;
        }
    }
}
