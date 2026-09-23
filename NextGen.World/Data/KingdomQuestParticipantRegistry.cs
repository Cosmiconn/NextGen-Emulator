using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact native KQ_JOIN_CHAR_INFO roster keyed by KQ Handle.
    /// Admission/team assignment is performed elsewhere; this registry only
    /// stores already-resolved participant wire data.
    /// </summary>
    public static class KingdomQuestParticipantRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, List<KingdomQuestJoinCharacterInfo>> ByHandle =
            new Dictionary<uint, List<KingdomQuestJoinCharacterInfo>>();

        public static void Set(uint handle, IEnumerable<KingdomQuestJoinCharacterInfo> participants)
        {
            if (participants == null) throw new ArgumentNullException("participants");

            List<KingdomQuestJoinCharacterInfo> copy =
                participants.Select(Clone).ToList();
            if (copy.Count > byte.MaxValue)
                throw new ArgumentOutOfRangeException("participants");

            lock (Sync)
                ByHandle[handle] = copy;
        }

        public static bool TryGet(uint handle,
            out IReadOnlyList<KingdomQuestJoinCharacterInfo> participants)
        {
            lock (Sync)
            {
                List<KingdomQuestJoinCharacterInfo> current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    participants = null;
                    return false;
                }

                participants = current.Select(Clone).ToList().AsReadOnly();
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

        private static KingdomQuestJoinCharacterInfo Clone(
            KingdomQuestJoinCharacterInfo source)
        {
            if (source == null)
                throw new ArgumentException("KQ participant entry is null.", "participants");

            return new KingdomQuestJoinCharacterInfo
            {
                Level = source.Level,
                Class = source.Class,
                Name = source.Name,
                Team = source.Team,
            };
        }
    }
}
