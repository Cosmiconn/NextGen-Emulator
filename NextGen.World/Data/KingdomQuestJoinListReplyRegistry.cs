using System.Collections.Generic;

namespace NextGen.World.Data
{
    /// <summary>
    /// Explicit per-Handle error value for NC_KQ_JOIN_LIST_ACK.
    ///
    /// The original structure proves this is a ushort Error field, but the
    /// supplied capture does not expose its raw value. Runtime therefore never
    /// invents a success/error constant; an authoritative admission/session
    /// owner must set it before JOIN_LIST_REQ can be answered.
    /// </summary>
    public static class KingdomQuestJoinListReplyRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, ushort> ErrorByHandle =
            new Dictionary<uint, ushort>();

        public static void Set(uint handle, ushort error)
        {
            lock (Sync)
                ErrorByHandle[handle] = error;
        }

        public static bool TryGet(uint handle, out ushort error)
        {
            lock (Sync)
                return ErrorByHandle.TryGetValue(handle, out error);
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ErrorByHandle.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                ErrorByHandle.Clear();
        }
    }
}
