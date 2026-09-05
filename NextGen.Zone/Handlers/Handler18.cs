
using NextGen.FiestaLib;
using NextGen.FiestaLib.Networking;
using NextGen.Zone.Game;
namespace NextGen.Zone.Handlers
{
    public sealed class Handler18
    {
        public static void SendSkillLearnt(ZoneCharacter character, ushort skillid)
        {
            using (var packet = new Packet(SH18Type.LearnSkill))
            {
                packet.WriteUShort(skillid);
                packet.WriteByte(0); //unk
                character.Client.SendPacket(packet);
            }
        }
    }
}
