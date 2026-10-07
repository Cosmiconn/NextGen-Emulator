using System;

namespace NextGen.Zone.Data
{
    public interface IKingdomQuestPineNativeAxialObject
    {
        KingdomQuestPineNativeAxes Axes { get; }
        // so_AllOfRange_Getthis: ordinary objects return this; boundary flags
        // return null and intermediate flags return the configured flag object.
        object RangeObject { get; }
        KingdomQuestPineEffectLocation Location { get; }
        ulong LayerRegistrationNumber { get; }
        byte LayerObjectViewType { get; }
        bool HasNativeAxisCounters { get; }
        KingdomQuestPineNativeAxisCounters AxisCounters { get; }
        void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters counters);
    }

    public enum KingdomQuestPineTraversalEvent
    {
        NestingLimit, RepeatedObject, VisitLimit, BrokenCandidate,
        SaveFieldInformation, DumpMap,
        // Managed unresolved boundaries, not invented native return codes.
        UnknownAxisOwner, UninitializedCounters, InvalidCounterDepth
    }

    /// <summary>
    /// Zone-thread state shared by ALL map traversals and marking counters:
    /// depth 0x0074D71C, four DWORDs 0x132728E4, loop count 0x13272C30.
    /// Counter seeds must come from the owning native state. Not per map/timer.
    /// </summary>
    public sealed class KingdomQuestPineNativeListCheckState
    {
        private KingdomQuestPineNativeAxisCounters counters;
        public int Depth { get; private set; } = -1; // original initialized data
        public int Steps { get; internal set; }
        public KingdomQuestPineNativeListCheckState(KingdomQuestPineNativeAxisCounters initialCounters)
        { counters = initialCounters; }
        public KingdomQuestPineNativeAxisCounters SnapshotCounters() => counters;
        internal bool Enter()
        {
            if (Depth >= 3) return false;
            Depth++; return true;
        }
        internal void Leave()
        {
            // BroadcastEventPopper dtor 0x005497C0 decrements even when the
            // constructor rejected the fifth level. Do not "repair" this.
            if (Depth > -1) Depth--;
        }
        internal void BeginVisit() => counters = counters.With(Depth, unchecked(counters.At(Depth) + 1));
        internal uint CurrentCounter => counters.At(Depth);
    }

    /// <summary>
    /// so_AllInMapNomal member-function overload 0x0054BE00. The spelling is
    /// native. This is the normal-map algorithm used by Build/Blast completion;
    /// MiniHouse dispatch, iterator overload and packet exchange remain separate.
    /// </summary>
    public sealed class KingdomQuestPineNativeMapTraversal
    {
        private readonly KingdomQuestPineNativeListCheckState state;
        private readonly Action<KingdomQuestPineTraversalEvent, IKingdomQuestPineNativeAxialObject> report;
        private readonly Action<IKingdomQuestPineNativeAxialObject, int> relink;
        public KingdomQuestPineNativeMapTraversal(KingdomQuestPineNativeListCheckState state,
            Action<KingdomQuestPineTraversalEvent, IKingdomQuestPineNativeAxialObject> report,
            Action<IKingdomQuestPineNativeAxialObject, int> relink)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
            this.relink = relink ?? throw new ArgumentNullException(nameof(relink));
        }
        private bool TryRangeObject(KingdomQuestPineNativeAxisNode node,
            IKingdomQuestPineNativeAxialObject context, out IKingdomQuestPineNativeAxialObject obj)
        {
            obj = null;
            if (node.Owner is IKingdomQuestPineNativeAxialObject owner)
            {
                object range = owner.RangeObject;
                if (range == null) return true;
                obj = range as IKingdomQuestPineNativeAxialObject;
                if (obj != null) return true;
            }
            report(KingdomQuestPineTraversalEvent.UnknownAxisOwner, context); return false;
        }
        // Returns false only for an unresolved managed dependency. On true,
        // nativeResult preserves the source's traversal/visitor return value.
        // Calls and diagnostics may already have occurred before a failure.
        public bool TryAllInMapNormal(IKingdomQuestPineNativeAxialObject source, byte includeSelf,
            Func<IKingdomQuestPineNativeAxialObject, IKingdomQuestPineNativeAxialObject, uint, bool> visit,
            out bool nativeResult)
        {
            nativeResult = false;
            if (source == null || visit == null) return false;
            bool entered = state.Enter();
            try
            {
                if (!entered) { report(KingdomQuestPineTraversalEvent.NestingLimit, source); return true; }
                if (!TryRangeObject(source.Axes.X.Previous, source, out var previous)) return false;
                if (ReferenceEquals(previous, source))
                {
                    if (!TryRangeObject(source.Axes.X.Next, source, out var next)) return false;
                    if (ReferenceEquals(next, source))
                    { report(KingdomQuestPineTraversalEvent.SaveFieldInformation, source); return true; }
                }
                state.BeginVisit();
                for (int direction = 0; direction < 2; direction++)
                {
                    // Y predecessors first, then Y successors. The global loop
                    // counter is reset for EACH pass; nested visits also reset it.
                    var node = direction == 0 ? source.Axes.Y.Previous : source.Axes.Y.Next;
                    state.Steps = 0;
                    if (!TryRangeObject(node, source, out var candidate)) return false;
                    while (candidate != null)
                    {
                        state.Steps = unchecked(state.Steps + 1);
                        if (state.Steps > 10000)
                        { report(KingdomQuestPineTraversalEvent.VisitLimit, source); return true; }
                        // so_SlantedListCheck stamps BEFORE layer filtering.
                        // Repeated objects abort; they are not skipped.
                        if (state.Depth < 0 || state.Depth > 3)
                        { report(KingdomQuestPineTraversalEvent.InvalidCounterDepth, candidate); return false; }
                        if (!candidate.HasNativeAxisCounters)
                        { report(KingdomQuestPineTraversalEvent.UninitializedCounters, candidate); return false; }
                        var counters = candidate.AxisCounters;
                        if (counters.At(state.Depth) == state.CurrentCounter)
                        { report(KingdomQuestPineTraversalEvent.RepeatedObject, candidate); return true; }
                        candidate.SetNativeAxisCounters(counters.With(state.Depth, state.CurrentCounter));
                        if (!TryRangeObject(candidate.Axes.X.Previous, candidate, out previous)) return false;
                        if (ReferenceEquals(previous, candidate))
                        {
                            report(KingdomQuestPineTraversalEvent.BrokenCandidate, candidate);
                            relink(candidate, 1);
                            report(KingdomQuestPineTraversalEvent.SaveFieldInformation, candidate);
                            report(KingdomQuestPineTraversalEvent.DumpMap, candidate);
                            return true;
                        }
                        if (source.LayerRegistrationNumber == candidate.LayerRegistrationNumber ||
                            source.LayerObjectViewType != 0 || candidate.LayerObjectViewType != 0)
                        {
                            uint distance = KingdomQuestPineEffectRoutine.DistanceSquared(source.Location, candidate.Location);
                            // Native calls the member function ON the candidate,
                            // passing the initiating source and squared distance.
                            if (!visit(candidate, source, distance)) return true;
                        }
                        // Deliberately read AFTER the callback, not from a list
                        // snapshot or a cached successor. Callbacks can mutate it.
                        node = direction == 0 ? candidate.Axes.Y.Previous : candidate.Axes.Y.Next;
                        if (!TryRangeObject(node, candidate, out candidate)) return false;
                    }
                }
                nativeResult = includeSelf == 0 || visit(source, source, 0);
                return true;
            }
            finally { state.Leave(); }
        }
    }
}
