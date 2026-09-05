using System;
using System.Collections.Generic;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.Util;
using NextGen.World.Networking;
using NextGen.World.Data;
using NextGen.World.Managers;
using NextGen.World.Data.Guilds.Academy;
namespace NextGen.World.Handlers
{
    public sealed class Handler38
    {
             [PacketHandler(CH38Type.GetAcademyGoldRewardList)]
        public static void GetAcademyGoldRewardList(WorldClient client, Packet packet)
        {
            using (var pack = new Packet(SH38Type.SendAcademyGoldRewardList))
            {
                pack.WriteHexAsBytes("80 18");//responsecode
                pack.WriteByte(1);//stat count
                pack.WriteHexAsBytes("0A 0A CA 9A 3B 00 00 00 00");//unk

                pack.WriteByte(10);//levelbreich
                pack.WriteLong(1000);
                pack.WriteByte(15);//level bereich
                pack.WriteLong(1000);
                pack.WriteByte(26);//level bereich
                pack.WriteLong(1000);
                pack.WriteByte(31);//level bereich
                pack.WriteLong(1000);
                pack.WriteByte(36);//levelbereich
                pack.WriteLong(1000);
                pack.WriteByte(41);//level bereich
                pack.WriteLong(9000);
                pack.WriteByte(46);//level bereich
                pack.WriteLong(1000);
                pack.WriteByte(51);//level bereich
                pack.WriteLong(1000);
                pack.WriteByte(56);//level bereich
                pack.WriteLong(1000);
                client.SendPacket(pack);
            }
        }
        public static void SendAcademyResponse(WorldClient pClient,string GuildName, GuildAcademyResponse Response)
        {

            using (var packet = new Packet(SH38Type.AcademyResponse))
            {
                packet.WriteString(GuildName, 16);
                packet.WriteUShort((ushort)Response);
                pClient.SendPacket(packet);
            }
        }
    }
        
}
