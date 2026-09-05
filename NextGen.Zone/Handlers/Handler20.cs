
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.Zone.Networking;

namespace NextGen.Zone.Handlers
{
    public sealed class Handler20
    {
        [PacketHandler(CH20Type.ByHPStone)]
        public static void ByHPStoneHandler(ZoneClient client, Packet packet)
        {
            short Amount;
            if (packet.TryReadShort(out Amount))
            {
                client.Character.ChangeMoney(client.Character.Character.Money -= client.Character.BaseStats.PriceHPStone);
                // War: StonesSP statt StonesHP gelesen/geschrieben - der
                // HP-Stein-Kauf-Handler aenderte faelschlich die SP-Steine.
                // Siehe DOCUMENTATION.md Abschnitt 36.
                short am = Amount += client.Character.StonesHP;
                client.Character.StonesHP = am;
                using (var p = new Packet(SH20Type.ChangeHPStones))
                {
                    p.WriteShort(am);
                    client.SendPacket(p);
                }
            }
        }

        [PacketHandler(CH20Type.BySPStone)]
        public static void BySPStoneHandler(ZoneClient client, Packet packet)
        {
            short Amount;
            if (packet.TryReadShort(out Amount))
            {
                client.Character.ChangeMoney(client.Character.Character.Money -= client.Character.BaseStats.PriceSPStone);
                // War: das Ergebnis wurde faelschlich in StonesHP statt
                // StonesSP geschrieben. Siehe DOCUMENTATION.md Abschnitt 36.
                short Am = Amount += client.Character.StonesSP;
                client.Character.StonesSP = Am;
                using (var p = new Packet(SH20Type.ChangeSPStones))
                {
                    p.WriteShort(Am);
                    client.SendPacket(p);
                }
            }
        }
        [PacketHandler(CH20Type.UseHPStone)]
        public static void UseHPStoneHandler(ZoneClient client, Packet packet)
        {
            if (client.Character.StonesHP == 0)
            {
                using (var p = new Packet(SH20Type.ErrorUseStone))
                {
                    client.SendPacket(p);
                }
            }
            else
            {
                client.Character.HealHP((uint)client.Character.BaseStats.SoulHP);

                using (var p = new Packet(SH20Type.StartHPStoneCooldown))
                {
                    client.SendPacket(p);
                }
            }
        }
        [PacketHandler(CH20Type.UseSPStone)]
        public static void UseSPStoneHandler(ZoneClient client, Packet packet)
        {
            if (client.Character.StonesSP == 0)
            {
                using (var p = new Packet(SH20Type.ErrorUseStone))
                {
                    client.SendPacket(p);
                }
            }
            else
            {
                client.Character.HealSP((uint)client.Character.BaseStats.SoulSP);

                using (var p = new Packet(SH20Type.StartSPStoneCooldown))
                {
                    client.SendPacket(p);
                }
            }
        }
    }
}
