using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// One native KQ_JOINER_BF row projected into both client-visible
    /// KQ_JOIN_CHAR_INFO and World->Zone PROTO_NC_KQ_JOINER fields.
    ///
    /// This is the first registry that deliberately owns CharacterNumber and
    /// Name/Level/Class together. It never infers one identity from the other.
    /// </summary>
    public sealed class KingdomQuestMembershipEntry
    {
        public uint CharacterNumber { get; set; }
        public byte Level { get; set; }
        public byte Class { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte TeamType { get; set; }

        // Raw DWORD at native KQ_JOINER_BF +0x20. PlayerJoin initializes it
        // to 0 and CheckCharBannedInLogin compares it with 1. The original PDB
        // does not give this field a trustworthy semantic name, so preserve the
        // raw value instead of collapsing it to a bool or inventing vote state.
        public uint LoginBanStateRaw { get; set; }

        public bool HasNativeLoginBanMarker
        {
            get { return LoginBanStateRaw == 1; }
        }

        public KingdomQuestJoinCharacterInfo ToClientInfo()
        {
            return new KingdomQuestJoinCharacterInfo
            {
                Level = Level,
                Class = Class,
                Name = Name,
                Team = TeamType,
            };
        }

        public KingdomQuestZoneJoinerInfo ToZoneInfo()
        {
            return new KingdomQuestZoneJoinerInfo
            {
                CharacterNumber = CharacterNumber,
                TeamType = TeamType,
            };
        }

        public KingdomQuestMembershipEntry Clone()
        {
            return new KingdomQuestMembershipEntry
            {
                CharacterNumber = CharacterNumber,
                Level = Level,
                Class = Class,
                Name = Name,
                TeamType = TeamType,
                LoginBanStateRaw = LoginBanStateRaw,
            };
        }
    }

    public static class KingdomQuestMembershipRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, List<KingdomQuestMembershipEntry>>
            ByHandle = new Dictionary<uint, List<KingdomQuestMembershipEntry>>();

        public static void Set(uint handle,
            IEnumerable<KingdomQuestMembershipEntry> members)
        {
            if (members == null) throw new ArgumentNullException("members");

            List<KingdomQuestMembershipEntry> copy =
                members.Select(CloneAndValidate).ToList();
            if (copy.Count > byte.MaxValue)
                throw new ArgumentOutOfRangeException("members");

            lock (Sync)
                ByHandle[handle] = copy;
        }

        public static bool TryGet(uint handle,
            out IReadOnlyList<KingdomQuestMembershipEntry> members)
        {
            lock (Sync)
            {
                List<KingdomQuestMembershipEntry> current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    members = null;
                    return false;
                }

                members = current.Select(v => v.Clone()).ToList().AsReadOnly();
                return true;
            }
        }

        public static bool TrySetLoginBanStateRaw(
            uint handle, uint characterNumber, uint rawValue)
        {
            lock (Sync)
            {
                List<KingdomQuestMembershipEntry> current;
                if (!ByHandle.TryGetValue(handle, out current))
                    return false;

                int index = current.FindIndex(
                    v => v.CharacterNumber == characterNumber);
                if (index < 0)
                    return false;

                // Preserve the original raw DWORD. Only the separately proven
                // login check currently interprets value 1 specially.
                current[index].LoginBanStateRaw = rawValue;
                return true;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ByHandle.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                ByHandle.Clear();
        }

        private static KingdomQuestMembershipEntry CloneAndValidate(
            KingdomQuestMembershipEntry source)
        {
            if (source == null || source.Name == null)
                throw new ArgumentException(
                    "KQ membership entry/name is null.", "members");
            return source.Clone();
        }
    }
}
