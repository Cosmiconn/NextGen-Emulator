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

        // WorldManager.pdb names the native KQ_JOINER_BF vote fields.
        // They are stored after CharInfo at exact struct offsets:
        // +0x20 bInVote (DWORD), +0x24 bBan (DWORD), +0x28 nVotingCount (BYTE).
        // Preserve the DWORDs raw because native code uses both ==1 and
        // nonzero tests on different paths.
        public uint InVoteRaw { get; set; }
        public uint BanRaw { get; set; }
        public byte VotingCount { get; set; }

        public bool IsNativeInVote
        {
            get { return InVoteRaw != 0; }
        }

        public bool HasNativeLoginBanMarker
        {
            get { return BanRaw == 1; }
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
                InVoteRaw = InVoteRaw,
                BanRaw = BanRaw,
                VotingCount = VotingCount,
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

        public static bool TrySetNativeVoteFields(
            uint handle, uint characterNumber,
            uint inVoteRaw, uint banRaw, byte votingCount)
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

                current[index].InVoteRaw = inVoteRaw;
                current[index].BanRaw = banRaw;
                current[index].VotingCount = votingCount;
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
