using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.InterLib.Networking;
using NextGen.Util;
using NextGen.World.Data;
using NextGen.World.Handlers;

namespace NextGen.World.InterServer
{
    public sealed class ZoneConnection : InterClient
    {
        public bool IsAZone { get; set; }
        public int Load { get; private set; }

        public byte ID { get; set; }
        public ushort Port { get; set; }
        public string IP { get; set; }
        public List<MapInfo> Maps { get; set; }

        public ZoneConnection(Socket sock) : base(sock)
        {
            IsAZone = false;
            this.OnPacket += new EventHandler<InterPacketReceivedEventArgs>(WorldConnection_OnPacket);
            this.OnDisconnect += new EventHandler<InterLib.Networking.SessionCloseEventArgs>(WorldConnection_OnDisconnect);
        }

        void WorldConnection_OnDisconnect(object sender, InterLib.Networking.SessionCloseEventArgs e)
        {
            if (IsAZone)
            {
                this.OnPacket -= new EventHandler<InterPacketReceivedEventArgs>(WorldConnection_OnPacket);
                this.OnDisconnect -= new EventHandler<InterLib.Networking.SessionCloseEventArgs>(WorldConnection_OnDisconnect);

                ZoneConnection derp;
                if (Program.Zones.TryRemove(ID, out derp))
                {
                    Log.WriteLine(LogLevel.Info, "Zone {0} disconnected.", ID);
                    InterHandler.SendZoneStopped(ID);
                }
                else
                {
                    Log.WriteLine(LogLevel.Info, "Could not remove zone {0}!?", ID);
                }
            }
        }

        void WorldConnection_OnPacket(object sender, InterPacketReceivedEventArgs e)
        {
#if DEBUG
            // so the startup works
          System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));
#endif

            if (e.Client.Assigned == false)
            {
                if (Program.Zones.Count >= 3)
                {
                    Log.WriteLine(LogLevel.Warn, "We can't load more than 3 zones atm.");
                    e.Client.Disconnect();
                    return;
                }

                if (e.Packet.OpCode == InterHeader.Auth)
                {
                    string pass;
                    if (!e.Packet.TryReadString(out pass))
                    {
                        e.Client.Disconnect();
                        return;
                    }

                    if (!pass.Equals(Settings.Instance.InterPassword))
                    {
                        e.Client.Disconnect();
                        return;
                    }
                    else
                    {
                        try
                        {
                            e.Client.Assigned = true;

                            ID = Program.GetFreeZoneID();
                            this.Port = (ushort)(Settings.Instance.ZoneBasePort + ID);

                            var l = DataProvider.Instance.GetMapsForZone(ID);
                            Maps = new List<MapInfo>();
                            foreach (var mapid in l)
                            {
                                MapInfo map;
                                if (DataProvider.Instance.Maps.TryGetValue(mapid, out map))
                                {
                                    Maps.Add(map);
                                }
                                else
                                    Log.WriteLine(LogLevel.Warn, "Zone is loading map {0} which could not be found.", mapid);
                            }

                            if (Program.Zones.TryAdd(ID, this))
                            {
                                IsAZone = true;
                               SendData();
                                Log.WriteLine(LogLevel.Info, "Added zone {0} with {1} maps.", ID, Maps.Count);
                            }
                            else
                            {
                                Log.WriteLine(LogLevel.Error, "Failed to add zone. Terminating connection.");
                                Disconnect();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(LogLevel.Exception, ex.ToString());
                            Disconnect();
                        }
                    }
                }
                else
                {
                    Log.WriteLine(LogLevel.Info, "Not authenticated and no auth packet first.");
                    e.Client.Disconnect();
                    return;
                }
            }
            else
            {
                MethodInfo method = InterHandlerStore.GetHandler(e.Packet.OpCode);
                if (method != null)
                {
                    Action action = InterHandlerStore.GetCallback(method, this, e.Packet);
                    if (Worker.Instance == null)
                    {
                        action();
                    }
                    else
                    {
                        Worker.Instance.AddCallback(action);
                    }
                }
                else
                {
                    Log.WriteLine(LogLevel.Debug, "Unhandled interpacket: {0}", e.Packet);
                }
            }
        }
        public void SendTransferClientFromWorld(int accountID, string userName, byte admin, string hostIP, string hash)
        {
            using (var packet = new InterPacket(InterHeader.Clienttransfer))
            {
                packet.WriteByte(0);
                packet.WriteInt(accountID);
                packet.WriteStringLen(userName);
                packet.WriteStringLen(hash);
                packet.WriteByte(admin);
                packet.WriteStringLen(hostIP);
                this.SendPacket(packet);
            }
        }
        public bool SendKingdomQuestMake(uint handle)
        {
            KingdomQuestSessionTarget target;
            if (!KingdomQuestSessionTargetRegistry.TryGet(handle, out target))
                return false;

            Packet native;
            if (!KingdomQuestServerProtocol.TryCreateMakeRequest(handle, out native))
                return false;

            using (native)
            using (var packet = new InterPacket(InterHeader.KingdomQuestMake))
            {
                byte[] body = native.ToNormalArray();
                packet.WriteUShort(target.MapID);
                packet.WriteShort(target.MapInstance);
                packet.WriteInt(body.Length);
                packet.WriteBytes(body);
                SendPacket(packet);
                return true;
            }
        }

        public bool SendKingdomQuestStart(uint handle)
        {
            Packet native;
            if (!KingdomQuestServerProtocol.TryCreateStart(handle, out native))
                return false;

            using (native)
            using (var packet = new InterPacket(InterHeader.KingdomQuestStart))
            {
                byte[] body = native.ToNormalArray();
                packet.WriteInt(body.Length);
                packet.WriteBytes(body);
                SendPacket(packet);
                return true;
            }
        }

        public void SendKingdomQuestDestroy(uint handle)
        {
            using (Packet native = KingdomQuestServerProtocol.CreateDestroy(handle))
            using (var packet = new InterPacket(InterHeader.KingdomQuestDestroy))
            {
                byte[] body = native.ToNormalArray();
                packet.WriteInt(body.Length);
                packet.WriteBytes(body);
                SendPacket(packet);
            }
        }

        public void SendKingdomQuestTransferRequest(string characterName,
            ushort mapId, short mapInstance, int x, int y)
        {
            using (var packet = new InterPacket(InterHeader.KingdomQuestTransfer))
            {
                packet.WriteStringLen(characterName);
                packet.WriteUShort(mapId);
                packet.WriteShort(mapInstance);
                packet.WriteInt(x);
                packet.WriteInt(y);
                SendPacket(packet);
            }
        }

        public void SendTransferClientFromZone(int accountID, string userName, string charName,int CharID, ushort randid, byte admin, string hostIP, short mapInstance = 0,
            ushort? mapOverrideId = null, int mapOverrideX = 0, int mapOverrideY = 0)
        {
            using (var packet = new InterPacket(InterHeader.Clienttransfer))
            {
                packet.WriteByte(1);
                packet.WriteInt(accountID);
                packet.WriteStringLen(userName);
                packet.WriteStringLen(charName);
                packet.WriteInt(CharID);
                packet.WriteShort(mapInstance);
                packet.WriteBool(mapOverrideId.HasValue);
                if (mapOverrideId.HasValue)
                {
                    packet.WriteUShort(mapOverrideId.Value);
                    packet.WriteInt(mapOverrideX);
                    packet.WriteInt(mapOverrideY);
                }
                packet.WriteUShort(randid);
                packet.WriteByte(admin);
                packet.WriteStringLen(hostIP);
                this.SendPacket(packet);
            }
        }

        public void SendData()
        {
            using (var packet = new InterPacket(InterHeader.Assigned))
            {
                packet.WriteByte(ID);
                packet.WriteStringLen(String.Format("{0}-{1}", Settings.Instance.GameServiceUri, ID));
                packet.WriteUShort((ushort)(Settings.Instance.ZoneBasePort + ID));

                packet.WriteInt(Maps.Count);
                foreach (var m in Maps)
                {
                    packet.WriteUShort(m.ID);
                    packet.WriteStringLen(m.ShortName);
                    packet.WriteStringLen(m.FullName);
                    packet.WriteInt(m.RegenX);
                    packet.WriteInt(m.RegenY);
                    packet.WriteByte(m.Kingdom);
                    packet.WriteUShort(m.ViewRange);
                }
                this.SendPacket(packet);
            }

        }
    }
}
