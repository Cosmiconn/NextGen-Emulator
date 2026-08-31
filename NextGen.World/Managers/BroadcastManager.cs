using System;
using NextGen.World.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Util;

namespace NextGen.World.Managers
{
    [ServerModule(InitializationStage.Clients)]
    public class BroadcastManager
    {
        public static BroadcastManager Instance { get; set; }

        [InitializerMethod]
        public static bool init()
        {
            Instance = new BroadcastManager();
            return true;
        }
        public void BroadcastInRange(WorldCharacter pChar, Packet pPacket, bool ToAll)
        {
            pChar.BroucastPacket(pPacket);
        }
    }
}
