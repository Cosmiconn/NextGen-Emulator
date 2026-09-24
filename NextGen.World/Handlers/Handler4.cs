using System;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.Util;
using NextGen.World.Data;
using NextGen.World.InterServer;
using NextGen.World.Networking;

namespace NextGen.World.Handlers
{
    public sealed class Handler4
    {
        [PacketHandler(CH4Type.CharSelect)]
        public static void CharacterSelectHandler(WorldClient client, Packet packet)
        {
            byte slot;
            if (!packet.TryReadByte(out slot) || slot > 10 || !client.Characters.ContainsKey(slot))
            {
                Log.WriteLine(LogLevel.Warn, "{0} selected an invalid character.", client.Username);
                return;
            }

            WorldCharacter character;
            if (client.Characters.TryGetValue(slot, out character))
            {
                KingdomQuestSessionTarget reconnectTarget;
                int reconnectX;
                int reconnectY;
                bool kingdomQuestReconnect =
                    KingdomQuestReconnectService.TryRestore(
                        client, character, DateTime.Now,
                        out reconnectTarget, out reconnectX, out reconnectY);

                ushort targetMap = kingdomQuestReconnect
                    ? reconnectTarget.MapID
                    : character.Character.PositionInfo.Map;
                short targetInstance = kingdomQuestReconnect
                    ? reconnectTarget.MapInstance
                    : (short)0;

                ZoneConnection zone = Program.GetZoneByMap(targetMap);
                if (zone != null)
                {
                    client.Characters.Clear(); //we clear the other ones from memory
                    client.Character = character; //only keep the one selecte

                    if (kingdomQuestReconnect)
                    {
                        // Original fc_NC_CHAR_CHARDATA_ACK overwrites the live
                        // login map/X/Y with p_Char_GetKQMap values after
                        // IsExisted + JoinerInfoUpdateByLogin succeed.
                        character.Character.PositionInfo.Map = targetMap;
                        character.Character.PositionInfo.XPos = reconnectX;
                        character.Character.PositionInfo.YPos = reconnectY;
                    }

                    //Database.Storage.Characters.AddChars(character.Character);
                    zone.SendTransferClientFromZone(
                        client.AccountID, client.Username,
                        client.Character.Character.Name, client.Character.ID,
                        client.RandomID, client.Admin, client.Host,
                        targetInstance,
                        kingdomQuestReconnect ? (ushort?)targetMap : null,
                        reconnectX, reconnectY);
                    ClientManager.Instance.AddClientByName(client); //so we can look them up fast using charname later.
                    SendZoneServerIP(client, zone);
                }
                else
                {
                    if (kingdomQuestReconnect)
                        client.KingdomQuestHandle = null;

                    Log.WriteLine(LogLevel.Warn,
                        "Character tried to join unloaded map: {0}", targetMap);
                    SendConnectError(client, ConnectErrors.MapUnderMaintenance);
                }
            }
        }

        public static void SendZoneServerIP(WorldClient client, ZoneConnection info)
        {
            using (var packet = new Packet(SH4Type.ServerIP))
            {
                packet.WriteString(info.IP, 16);
                packet.WriteUShort(info.Port);
                client.SendPacket(packet);
            }
        }

        public static void SendConnectError(WorldClient client, ConnectErrors error)
        {
            using (var packet = new Packet(SH4Type.ConnectError))
            {
                packet.WriteUShort((ushort)error);
                client.SendPacket(packet);
            }
        }
    }
}
