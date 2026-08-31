using NextGen.FiestaLib;
using NextGen.InterLib.Networking;
using NextGen.FiestaLib.Networking;
using NextGen.Zone.Networking;

namespace NextGen.Zone.Handlers
{
    public sealed class Handler3
    {
        [PacketHandler(CH3Type.BackToCharSelect)]
        public static void BackTo(ZoneClient client, Packet packet)
        {
            using(var iacket = new InterPacket(InterHeader.ClientDisconect))
            {
                iacket.WriteString(client.Character.Character.Name, 16);
                InterServer.WorldConnector.Instance.SendPacket(iacket);
            }
        }
        public static void SendError(ZoneClient client, ServerError error)
        {
            using (Packet pack = new Packet(SH3Type.Error))
            {
                pack.WriteShort((byte)error);
                client.SendPacket(pack);
            }
        }
    }
}
