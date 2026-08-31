using System;
using NextGen.InterLib;
using NextGen.InterLib.Networking;
using NextGen.FiestaLib.Networking;
using NextGen.Util;

namespace NextGen.World.Managers
{
    [ServerModule(InitializationStage.Clients)]
    public class ZoneManager
    {
        public static ZoneManager Instance { get; set; }

        [InitializerMethod]
        public static bool init()
        {
            Instance = new ZoneManager();
            return true;
        }
        public void Broadcast(InterPacket pPacket)
        {
            foreach (var zone in Program.Zones.Values)
            {
                zone.SendPacket(pPacket);
            }
        }
    }
}
