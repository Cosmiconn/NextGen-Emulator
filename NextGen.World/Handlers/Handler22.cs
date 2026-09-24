using System;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.World.Networking;
using NextGen.World.Data;
using NextGen.Util;

namespace NextGen.World.Handlers
{
    public class Handler22
    {
        [PacketHandler(CH22Type.KingdomQuestStatusReq)]
        public static void KingdomQuestStatus(WorldClient client, Packet packet)
        {
            uint handle;
            if (!packet.TryReadUInt(out handle))
                return;

            KingdomQuestInstanceWireState state;
            if (!KingdomQuestInstanceRegistry.TryGet(handle, out state))
            {
                Log.WriteLine(LogLevel.Debug,
                    "KQ status requested for unresolved handle {0}.", handle);
                return;
            }

            using (Packet response = KingdomQuestProtocol.CreateStatusAck(state))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestJoinListReq)]
        public static void KingdomQuestJoinList(WorldClient client, Packet packet)
        {
            uint handle;
            if (!packet.TryReadUInt(out handle))
                return;

            ushort error;
            System.Collections.Generic.IReadOnlyList<
                NextGen.FiestaLib.Data.KingdomQuestJoinCharacterInfo> participants;
            if (!KingdomQuestJoinListReplyRegistry.TryGet(handle, out error) ||
                !KingdomQuestParticipantRegistry.TryGet(handle, out participants))
            {
                Log.WriteLine(LogLevel.Debug,
                    "KQ join-list requested for unresolved handle/error {0}.", handle);
                return;
            }

            using (Packet response =
                KingdomQuestProtocol.CreateJoinListAck(error, participants))
                client.SendPacket(response);
        }

        [PacketHandler(CH22Type.KingdomQuestListRefreshReq)]
        public static void KingdomQuestListRefresh(WorldClient client, Packet packet)
        {
            using (Packet time = KingdomQuestProtocol.CreateListTime(DateTimeOffset.Now))
                client.SendPacket(time);

            var definitions = KingdomQuestDefinitionRegistry.Snapshot();
            using (Packet add = KingdomQuestProtocol.CreateListAdd(definitions))
                client.SendPacket(add);

            // The client also emits LIST_REFRESH during initial World entry.
            // Keep unrelated bootstrap work one-time; later refreshes must not
            // replay login callbacks.
            if (!client.Character.IsIngame)
            {
                using (var friends = new Packet(21, 7))
                {
                    friends.WriteByte((byte)client.Character.Friends.Count);
                    client.Character.WriteFriendData(friends);
                    client.SendPacket(friends);
                }
                using (var timePacket = new Packet(SH2Type.UnkTimePacket))
                {
                    timePacket.WriteShort(256);
                    client.SendPacket(timePacket);
                }

                client.Character.IsIngame = true;
                client.Character.OneIngameLoginLoad();
                MasterManager.Instance.SendMasterList(client);
                Managers.CharacterManager.InvokdeIngame(client.Character);
                client.Character.OnGotIngame();
            }
        }
    }
}
