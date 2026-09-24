using System;
using NextGen.FiestaLib.Networking;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Original PROTO_NC_CHARSAVE_LOCATION_CMD, exactly 48 bytes.
    ///
    /// Zone.pdb names the fields chrregnum, coord, kqhandle, map_kq and
    /// coord_kq. ShinePlayer::so_SaveLocation writes them at offsets
    /// 0x00, 0x04, 0x18, 0x1C and 0x28 respectively.
    ///
    /// This is a wire/source boundary only. It does not decide the normal
    /// return coordinate used while a character is inside special maps.
    /// </summary>
    public sealed class CharacterSaveLocationProtocolInfo
    {
        public const int WireSize = 48;
        public const int MapNameSize = 12;

        public uint CharacterNumber { get; set; }

        public string MapName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }

        public uint KingdomQuestHandle { get; set; } = uint.MaxValue;
        public string KingdomQuestMapName { get; set; } = string.Empty;
        public int KingdomQuestX { get; set; }
        public int KingdomQuestY { get; set; }

        public void Write(Packet packet)
        {
            if (packet == null) throw new ArgumentNullException("packet");

            packet.WriteUInt(CharacterNumber);
            packet.WriteString(MapName ?? string.Empty, MapNameSize);
            packet.WriteInt(X);
            packet.WriteInt(Y);
            packet.WriteUInt(KingdomQuestHandle);
            packet.WriteString(
                KingdomQuestMapName ?? string.Empty, MapNameSize);
            packet.WriteInt(KingdomQuestX);
            packet.WriteInt(KingdomQuestY);
        }

        public static bool TryRead(
            Packet packet, out CharacterSaveLocationProtocolInfo value)
        {
            value = null;
            if (packet == null || packet.Remaining < WireSize)
                return false;

            uint characterNumber;
            string mapName;
            int x, y;
            uint kqHandle;
            string kqMapName;
            int kqX, kqY;
            if (!packet.TryReadUInt(out characterNumber) ||
                !packet.TryReadString(out mapName, MapNameSize) ||
                !packet.TryReadInt(out x) ||
                !packet.TryReadInt(out y) ||
                !packet.TryReadUInt(out kqHandle) ||
                !packet.TryReadString(out kqMapName, MapNameSize) ||
                !packet.TryReadInt(out kqX) ||
                !packet.TryReadInt(out kqY))
                return false;

            value = new CharacterSaveLocationProtocolInfo
            {
                CharacterNumber = characterNumber,
                MapName = mapName,
                X = x,
                Y = y,
                KingdomQuestHandle = kqHandle,
                KingdomQuestMapName = kqMapName,
                KingdomQuestX = kqX,
                KingdomQuestY = kqY,
            };
            return true;
        }
    }
}
