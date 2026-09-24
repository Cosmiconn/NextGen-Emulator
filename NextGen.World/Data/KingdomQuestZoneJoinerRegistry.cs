using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Explicit native PROTO_NC_KQ_JOINER roster for World -> Zone start.
    ///
    /// CharacterNumber and TeamType must be supplied by an authoritative
    /// admission/team owner. This registry never resolves names to character
    /// numbers and never chooses teams.
    /// </summary>
    public static class KingdomQuestZoneJoinerRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, List<KingdomQuestZoneJoinerInfo>> ByHandle =
            new Dictionary<uint, List<KingdomQuestZoneJoinerInfo>>();

        public static void Set(uint handle,
            IEnumerable<KingdomQuestZoneJoinerInfo> joiners)
        {
            if (joiners == null) throw new ArgumentNullException("joiners");

            List<KingdomQuestZoneJoinerInfo> copy =
                joiners.Select(Clone).ToList();
            if (copy.Count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("joiners");

            lock (Sync)
                ByHandle[handle] = copy;
        }

        public static bool TryGet(uint handle,
            out IReadOnlyList<KingdomQuestZoneJoinerInfo> joiners)
        {
            lock (Sync)
            {
                List<KingdomQuestZoneJoinerInfo> current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    joiners = null;
                    return false;
                }

                joiners = current.Select(Clone).ToList().AsReadOnly();
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

        private static KingdomQuestZoneJoinerInfo Clone(
            KingdomQuestZoneJoinerInfo source)
        {
            if (source == null)
                throw new ArgumentException(
                    "KQ Zone joiner entry is null.", "joiners");

            return new KingdomQuestZoneJoinerInfo
            {
                CharacterNumber = source.CharacterNumber,
                TeamType = source.TeamType,
            };
        }
    }
}
