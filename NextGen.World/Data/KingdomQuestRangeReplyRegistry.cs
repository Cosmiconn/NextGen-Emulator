using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.World.Data
{
    public sealed class KingdomQuestRangeReply
    {
        private readonly List<uint> handles;

        public uint NewStartHandle { get; private set; }
        public uint NewEndHandle { get; private set; }
        public IReadOnlyList<uint> Handles { get { return handles.AsReadOnly(); } }

        internal KingdomQuestRangeReply(uint newStartHandle, uint newEndHandle,
            IEnumerable<uint> sourceHandles)
        {
            if (sourceHandles == null) throw new ArgumentNullException("sourceHandles");
            handles = sourceHandles.ToList();
            if (handles.Count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("sourceHandles");

            NewStartHandle = newStartHandle;
            NewEndHandle = newEndHandle;
        }

        internal KingdomQuestRangeReply Clone()
        {
            return new KingdomQuestRangeReply(NewStartHandle, NewEndHandle, handles);
        }
    }

    /// <summary>
    /// Explicit response windows for NC_KQ_LIST_REQ / NC_KQ_SCHEDULE_REQ.
    ///
    /// The PDB proves both requests contain StartHandle/EndHandle and both
    /// ACKs return NewStartHandle/NewEndHandle plus client KQ entries. The
    /// selection/range algorithm is not proven, so this registry performs no
    /// comparisons on handles: a scheduler must register the exact response
    /// for an exact requested pair.
    /// </summary>
    public static class KingdomQuestRangeReplyRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Tuple<uint, uint>, KingdomQuestRangeReply> ListReplies =
            new Dictionary<Tuple<uint, uint>, KingdomQuestRangeReply>();
        private static readonly Dictionary<Tuple<uint, uint>, KingdomQuestRangeReply> ScheduleReplies =
            new Dictionary<Tuple<uint, uint>, KingdomQuestRangeReply>();

        public static void SetList(uint requestStartHandle, uint requestEndHandle,
            uint newStartHandle, uint newEndHandle, IEnumerable<uint> handles)
        {
            Set(ListReplies, requestStartHandle, requestEndHandle,
                newStartHandle, newEndHandle, handles);
        }

        public static void SetSchedule(uint requestStartHandle, uint requestEndHandle,
            uint newStartHandle, uint newEndHandle, IEnumerable<uint> handles)
        {
            Set(ScheduleReplies, requestStartHandle, requestEndHandle,
                newStartHandle, newEndHandle, handles);
        }

        public static bool TryGetList(uint requestStartHandle, uint requestEndHandle,
            out KingdomQuestRangeReply reply)
        {
            return TryGet(ListReplies, requestStartHandle, requestEndHandle, out reply);
        }

        public static bool TryGetSchedule(uint requestStartHandle, uint requestEndHandle,
            out KingdomQuestRangeReply reply)
        {
            return TryGet(ScheduleReplies, requestStartHandle, requestEndHandle, out reply);
        }

        public static void Clear()
        {
            lock (Sync)
            {
                ListReplies.Clear();
                ScheduleReplies.Clear();
            }
        }

        private static void Set(
            Dictionary<Tuple<uint, uint>, KingdomQuestRangeReply> target,
            uint requestStartHandle, uint requestEndHandle,
            uint newStartHandle, uint newEndHandle, IEnumerable<uint> handles)
        {
            var reply = new KingdomQuestRangeReply(newStartHandle, newEndHandle, handles);
            lock (Sync)
                target[Tuple.Create(requestStartHandle, requestEndHandle)] = reply;
        }

        private static bool TryGet(
            Dictionary<Tuple<uint, uint>, KingdomQuestRangeReply> target,
            uint requestStartHandle, uint requestEndHandle,
            out KingdomQuestRangeReply reply)
        {
            lock (Sync)
            {
                KingdomQuestRangeReply current;
                if (!target.TryGetValue(Tuple.Create(requestStartHandle, requestEndHandle),
                    out current))
                {
                    reply = null;
                    return false;
                }

                reply = current.Clone();
                return true;
            }
        }
    }
}
