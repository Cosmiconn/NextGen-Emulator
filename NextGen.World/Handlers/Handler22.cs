using System;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.World.Networking;
using NextGen.World.Data;

namespace NextGen.World.Handlers
{
   public class Handler22
    {
       [PacketHandler(CH22Type.GetKQInstanceInfo)]
       public static void GetKQInstanceInfo(WorldClient client, Packet packet)
       {
           uint instanceId;
           if (!packet.TryReadUInt(out instanceId))
               return;

           KingdomQuestInstanceWireState state;
           if (!KingdomQuestInstanceRegistry.TryGet(instanceId, out state) ||
               !state.InstanceInfoValue.HasValue)
           {
               Log.WriteLine(LogLevel.Debug,
                   "KQ instance detail requested for unresolved instance {0}.", instanceId);
               return;
           }

           using (Packet response = KingdomQuestProtocol.CreateInstanceInfo(
               instanceId, state.InstanceInfoValue.Value))
               client.SendPacket(response);
       }

       [PacketHandler(CH22Type.GotIngame)]
       public static void GotIngame(WorldClient client, Packet packet)
       {
          /* using (var p1 = new Packet(SH4Type.CharacterGuildacademyinfo))
           {
           if(client.Character.GuildAcademy != null)
           {
    
              
           }
           else
           {
               p1.Fill(5, 0);
           }
           client.SendPacket(p1);
           }
          using (var p2 = new Packet(SH4Type.CharacterGuildinfo))
           { 
                  if (client.Character.Guild != null)
                  {
                      client.Character.Guild.Details.WriteMessageAsGuildMember(p2, client.Character.Guild);

                  }
                  else
                  {
                      p2.WriteInt(0);
                  }
              client.SendPacket(p2);
           }*/
           // dafuq no op code..
           using (var p = new Packet(0x581C))
           {
             //p.WriteShort();
               p.WriteUInt(0x4d0bc167);   // 21h
               client.SendPacket(p);
           }
           // SH22/29 is the captured World-side Kingdom Quest list family.
           // Until authoritative KingdomQuest definition/schedule rows are loaded,
           // keep the legacy empty list behavior rather than inventing entries.
           using (var p3 = new Packet(SH22Type.KingdomQuestList))
           {
               p3.WriteUShort(0);
               client.SendPacket(p3);
           }
           
           using (var p4 = new Packet(21, 7))
           {
               p4.WriteByte((byte)client.Character.Friends.Count);
               client.Character.WriteFriendData(p4);
               client.SendPacket(p4);
           }
           using (var p5 = new Packet(SH2Type.UnkTimePacket))
           {
               p5.WriteShort(256);
               client.SendPacket(p5);
           }
           if (!client.Character.IsIngame)
           {
               client.Character.IsIngame = true;

               client.Character.OneIngameLoginLoad();
               MasterManager.Instance.SendMasterList(client);
               //SendMasterList(pClient);
           }

           Managers.CharacterManager.InvokdeIngame(client.Character);
           client.Character.OnGotIngame();
       }
    }
}
