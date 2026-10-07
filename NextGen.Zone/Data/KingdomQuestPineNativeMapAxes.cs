using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineAxisError
    {
        AxisType, AlreadyLinked, DetachedAnchor, SearchBoundary, LinkMismatch,
        BothSearchDirections, InvalidBoundary, InvalidOrder, InvalidObjectLinks,
        UnequalAxisRemoval, CoordinatesOutsideMap, ReservedCoordinate,
        RelocateDetachFailure, RelocateAppendFailure,
        SaveFieldInformation, DumpMap
    }

    /// <summary>CoordedNode 0x00587D50/60, cn_IsValid 0x0054B4C0.</summary>
    public sealed class KingdomQuestPineNativeAxisNode
    {
        public const int LastCoordinate = 0x00FFFFFF;
        private readonly Func<int> coordinate;
        private readonly bool realObject;
        public object Owner { get; }
        public byte Axis { get; } // native ocn_type: ASCII X=0x58, Y=0x59
        public int Coordinate => coordinate();
        public KingdomQuestPineNativeAxisNode Previous { get; private set; }
        public KingdomQuestPineNativeAxisNode Next { get; private set; }
        public bool IsDetached => Previous == this && Next == this;
        public KingdomQuestPineNativeAxisNode(object owner, byte axis, bool realObject, Func<int> coordinate)
        {
            if (axis != 0x58 && axis != 0x59) throw new ArgumentOutOfRangeException(nameof(axis));
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.coordinate = coordinate ?? throw new ArgumentNullException(nameof(coordinate));
            Axis = axis; this.realObject = realObject; Previous = Next = this;
        }
        private bool Error(Action<KingdomQuestPineAxisError, object> report, KingdomQuestPineAxisError error)
        { report(error, Owner); return false; }
        private static int Difference(int a, int b) => unchecked(a - b);
        public bool IsValid(Action<KingdomQuestPineAxisError, object> report)
        {
            if (realObject)
            {
                if (Previous == this || Next == this)
                    return Error(report, KingdomQuestPineAxisError.InvalidObjectLinks);
                if (Previous.Next != this || Next.Previous != this)
                    return Error(report, KingdomQuestPineAxisError.LinkMismatch);
            }
            else
            {
                if (Coordinate == 0)
                {
                    if (Previous != this || Next == this)
                        return Error(report, KingdomQuestPineAxisError.InvalidBoundary);
                    if (Next.Previous != this) return Error(report, KingdomQuestPineAxisError.LinkMismatch);
                }
                if (Coordinate == LastCoordinate)
                {
                    if (Previous == this || Next != this)
                        return Error(report, KingdomQuestPineAxisError.InvalidBoundary);
                    if (Previous.Next != this) return Error(report, KingdomQuestPineAxisError.LinkMismatch);
                }
            }
            if ((Previous != this && Difference(Previous.Coordinate, Coordinate) > 0) ||
                (Next != this && Difference(Coordinate, Next.Coordinate) > 0))
                return Error(report, KingdomQuestPineAxisError.InvalidOrder);
            return true;
        }
        // cn_MakeLink 0x00588060. Bad inputs violate native assertions;
        // reject them before mutation instead of modeling corrupt pointers.
        public static bool TryLinkBoundaries(KingdomQuestPineNativeAxisNode low, KingdomQuestPineNativeAxisNode high)
        {
            if (low == null || high == null || low == high || low.Axis != high.Axis ||
                !low.IsDetached || !high.IsDetached || low.realObject || high.realObject ||
                low.Coordinate != 0 || high.Coordinate != LastCoordinate) return false;
            low.Next = high; high.Previous = low; return true;
        }
        public bool TryAppend(KingdomQuestPineNativeAxisNode anchor,
            Action<KingdomQuestPineAxisError, object> report)
        {
            if (anchor == null || anchor.Axis != Axis) return Error(report, KingdomQuestPineAxisError.AxisType);
            if (!IsDetached) return Error(report, KingdomQuestPineAxisError.AlreadyLinked);
            if (anchor.IsDetached) return Error(report, KingdomQuestPineAxisError.DetachedAnchor);
            var left = anchor;
            int leftSteps = 0, rightSteps = 0, remaining = 10000;
            while (Difference(left.Coordinate, Coordinate) > 0)
            {
                if (--remaining < 0) return false;
                if (left.Previous == left) return Error(report, KingdomQuestPineAxisError.SearchBoundary);
                left = left.Previous; leftSteps++;
            }
            var right = left.Next; remaining = 10000;
            while (Difference(Coordinate, right.Coordinate) > 0)
            {
                if (--remaining < 0) return false;
                if (right.Next == right) return Error(report, KingdomQuestPineAxisError.SearchBoundary);
                left = left.Next; right = right.Next;
                if (left.Next != right || right.Previous != left)
                    report(KingdomQuestPineAxisError.LinkMismatch, Owner); // native reports, then continues
                rightSteps++;
            }
            if (leftSteps != 0 && rightSteps != 0) return Error(report, KingdomQuestPineAxisError.BothSearchDirections);
            if (!left.IsValid(report)) return false;
            if (left.Next != right || right.Previous != left) return Error(report, KingdomQuestPineAxisError.LinkMismatch);
            if (!right.IsValid(report)) return false;
            Previous = left; Next = right; right.Previous = this; left.Next = this;
            // Failed post-insertion validation does NOT roll the writes back.
            return left.IsValid(report) && IsValid(report) && right.IsValid(report);
        }
        // cn_Delink 0x00553900 validates neighbours after unlinking, ignores
        // their results and self-links this node. Its object is never destroyed.
        public bool Unlink(Action<KingdomQuestPineAxisError, object> report)
        {
            if (IsDetached) return false;
            Previous.Next = Next; Next.Previous = Previous;
            Previous.IsValid(report); Next.IsValid(report);
            Previous = Next = this; return true;
        }
        // cn_Relocate 0x005883A0: equality also requires reinsertion. Keep the
        // old successor before unlinking, including in native error branches.
        internal KingdomQuestPineAxisError? Relocate(Action<KingdomQuestPineAxisError, object> report)
        {
            if (Difference(Previous.Coordinate, Coordinate) < 0 && Difference(Coordinate, Next.Coordinate) < 0)
                return null;
            var anchor = Next;
            if (!Unlink(report)) return KingdomQuestPineAxisError.RelocateDetachFailure;
            if (!TryAppend(anchor, report)) return KingdomQuestPineAxisError.RelocateAppendFailure;
            return null;
        }
    }

    public sealed class KingdomQuestPineNativeAxes
    {
        public KingdomQuestPineNativeAxisNode X { get; }
        public KingdomQuestPineNativeAxisNode Y { get; }
        public KingdomQuestPineNativeAxes(object owner, bool realObject, Func<int> x, Func<int> y)
        { X = new KingdomQuestPineNativeAxisNode(owner, 0x58, realObject, x); Y = new KingdomQuestPineNativeAxisNode(owner, 0x59, realObject, y); }
        public void FreeFromLists(Action<KingdomQuestPineAxisError, object> report)
        {
            bool x = X.Previous != X.Next, y;
            if (x) X.Unlink(report);
            y = Y.Previous != Y.Next;
            if (y) Y.Unlink(report);
            if (x != y) report(KingdomQuestPineAxisError.UnequalAxisRemoval, X.Owner);
        }
    }

    public sealed class KingdomQuestPineNativeAxialFlag : IKingdomQuestPineNativePoolObject, IKingdomQuestPineNativeAxialObject
    {
        private readonly Action<KingdomQuestPineAxisError, object> report;
        private int x, y;
        private bool configured;
        private KingdomQuestPineNativeAxisCounters? axisCounters;
        public KingdomQuestPineNativeObjectType NativeObjectType => (KingdomQuestPineNativeObjectType)0;
        public ushort NativeHandle { get; }
        public KingdomQuestPineNativeAxes Axes { get; }
        public KingdomQuestPineEffectMapIdentity Map { get; private set; }
        public object RangeObject { get; private set; } // +0x167: endpoints null, intermediate flag itself
        public KingdomQuestPineEffectLocation Location => new KingdomQuestPineEffectLocation(Map, 0, X, Y, Direction);
        public ulong LayerRegistrationNumber { get; private set; }
        public byte LayerObjectViewType { get; private set; }
        public bool HasNativeAxisCounters => axisCounters.HasValue;
        public KingdomQuestPineNativeAxisCounters AxisCounters => axisCounters ??
            throw new InvalidOperationException("Explicit native flag counter seed required.");
        public void SetNativeAxisCounters(KingdomQuestPineNativeAxisCounters counters) { axisCounters = counters; }
        public void SetNativeLayer(ulong registrationNumber, byte viewType)
        { LayerRegistrationNumber = registrationNumber; LayerObjectViewType = viewType; }
        public int X => configured ? x : throw new InvalidOperationException("Uninitialized native axial coordinate.");
        public int Y => configured ? y : throw new InvalidOperationException("Uninitialized native axial coordinate.");
        public byte Direction => configured ? (byte)0 :
            throw new InvalidOperationException("Uninitialized native axial direction."); // so_SlantedFlag 0x00556270
        public KingdomQuestPineNativeAxialFlag(ushort index, Action<KingdomQuestPineAxisError, object> report)
        {
            NativeHandle = KingdomQuestPineNativeObjectHandles.Encode(0, index);
            if (NativeHandle == ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(index));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
            Axes = new KingdomQuestPineNativeAxes(this, false, () => X, () => Y);
        }
        internal void Configure(int x, int y, KingdomQuestPineEffectMapIdentity map, object rangeObject)
        { this.x = x; this.y = y; Map = map; RangeObject = rangeObject; configured = true; }
        public void InitializeForAllocation() { } // both Init virtual targets are ret 8
        public void FreeFromLists() => Axes.FreeFromLists(report);
        public void InvokeManagerFreeCallback() { } // +0x2F0 ret 4
    }

    public readonly struct KingdomQuestPineNativeAxisCounters
    {
        public uint A { get; }
        public uint B { get; }
        public uint C { get; }
        public uint D { get; }
        public KingdomQuestPineNativeAxisCounters(uint a, uint b, uint c, uint d)
        { A = a; B = b; C = c; D = d; }
        public uint At(int index)
        {
            switch (index) { case 0: return A; case 1: return B; case 2: return C; case 3: return D;
                default: throw new ArgumentOutOfRangeException(nameof(index)); }
        }
        public KingdomQuestPineNativeAxisCounters With(int index, uint value)
        {
            switch (index)
            {
                case 0: return new KingdomQuestPineNativeAxisCounters(value, B, C, D);
                case 1: return new KingdomQuestPineNativeAxisCounters(A, value, C, D);
                case 2: return new KingdomQuestPineNativeAxisCounters(A, B, value, D);
                case 3: return new KingdomQuestPineNativeAxisCounters(A, B, C, value);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        public KingdomQuestPineNativeAxisCounters Previous() => new KingdomQuestPineNativeAxisCounters(
            unchecked(A - 1), unchecked(B - 1), unchecked(C - 1), unchecked(D - 1));
    }

    /// <summary>
    /// fm_Init's axial segment 0x00464D7A..0x00464E93, with seven ALREADY
    /// allocated native type-0 slots. This is not the entire FieldMap initializer.
    /// </summary>
    public sealed class KingdomQuestPineNativeFieldMapAxes
    {
        private readonly KingdomQuestPineNativeAxialFlag[] flags;
        private readonly Action<KingdomQuestPineAxisError, object> report;
        public KingdomQuestPineEffectMapIdentity Identity { get; }
        public bool IsReady { get; }
        public KingdomQuestPineNativeFieldMapAxes(KingdomQuestPineEffectMapIdentity identity,
            IReadOnlyList<KingdomQuestPineNativeAxialFlag> allocatedFlags,
            Action<KingdomQuestPineNativeAxialFlag> setTitleZoneObject,
            Action<KingdomQuestPineAxisError, object> report)
        {
            if (identity == null || identity.XLimit == 0 || identity.YLimit == 0)
                throw new ArgumentException("Native nonzero FieldMap limits required.", nameof(identity));
            if (allocatedFlags == null || allocatedFlags.Count != 7 || setTitleZoneObject == null || report == null)
                throw new ArgumentException("Seven native flag slots and source services required.");
            flags = new KingdomQuestPineNativeAxialFlag[7]; var unique = new HashSet<KingdomQuestPineNativeAxialFlag>();
            for (int i = 0; i < 7; i++)
            {
                var f = allocatedFlags[i];
                if (f == null || !unique.Add(f) || !f.Axes.X.IsDetached || !f.Axes.Y.IsDetached)
                    throw new ArgumentException("Distinct detached native flag slots required.", nameof(allocatedFlags));
                flags[i] = f;
            }
            Identity = identity; this.report = report;
            flags[0].Configure(0, 0, identity, null);
            flags[1].Configure(0xFFFFFF, 0xFFFFFF, identity, null);
            setTitleZoneObject(flags[0]); // CCharacterTitleZone::ctz_SetObject 0x005CA610
            KingdomQuestPineNativeAxisNode.TryLinkBoundaries(flags[0].Axes.X, flags[1].Axes.X);
            KingdomQuestPineNativeAxisNode.TryLinkBoundaries(flags[0].Axes.Y, flags[1].Axes.Y);
            uint step = identity.XLimit / 5, halfX = step / 2, halfY = (identity.YLimit / 5) / 2;
            bool ready = true;
            for (int i = 0; i < 5; i++)
            {
                // Original adds the X step to BOTH axes after their distinct
                // half-step starts. Preserve the rectangular-map asymmetry.
                flags[i + 2].Configure(unchecked((int)(halfX + i * step)),
                    unchecked((int)(halfY + i * step)), identity, flags[i + 2]);
                ready &= flags[i + 2].Axes.X.TryAppend(flags[0].Axes.X, report);
                ready &= flags[i + 2].Axes.Y.TryAppend(flags[0].Axes.Y, report);
            }
            IsReady = ready;
        }
        public IReadOnlyList<object> SnapshotAxis(bool y)
        {
            var result = new List<object>(); var seen = new HashSet<KingdomQuestPineNativeAxisNode>();
            var node = y ? flags[0].Axes.Y : flags[0].Axes.X;
            while (seen.Add(node))
            { result.Add(node.Owner); if (node.Next == node) return result.AsReadOnly(); node = node.Next; }
            throw new InvalidOperationException("Corrupt native axis links.");
        }
        internal int Mark(KingdomQuestPineNativeSceneObject obj, Func<KingdomQuestPineNativeAxisCounters> counters)
        {
            obj.SetNativeMap(Identity); obj.SetNativeAxisCounters(counters().Previous());
            var p = obj.Location;
            // Native JA allows equality here. MoveTo and effect Routine have
            // DIFFERENT exclusive bounds and must not share this predicate.
            if (unchecked((uint)p.X) > Identity.XLimit || unchecked((uint)p.Y) > Identity.YLimit)
            { report(KingdomQuestPineAxisError.CoordinatesOutsideMap, obj); return 3; }
            obj.SetNativeMap(Identity); obj.SetNativeAxisCounters(counters().Previous());
            p = obj.Location;
            if (unchecked((uint)p.X) >= 0xFFFFFF || unchecked((uint)p.Y) >= 0xFFFFFF)
            { report(KingdomQuestPineAxisError.ReservedCoordinate, obj); return 3; }
            int x = (int)Math.Min(4u, unchecked((uint)p.X * 5u) / Identity.XLimit);
            int y = (int)Math.Min(4u, unchecked((uint)p.Y * 5u) / Identity.YLimit);
            // Native short-circuit: a failed X append skips Y, relinks/logs,
            // and nevertheless returns success on the ignoreBlock=1 branch.
            if (!obj.Axes.X.TryAppend(flags[x + 2].Axes.X, report) ||
                !obj.Axes.Y.TryAppend(flags[y + 2].Axes.Y, report))
            {
                Relink(obj);
                report(KingdomQuestPineAxisError.SaveFieldInformation, obj);
                report(KingdomQuestPineAxisError.DumpMap, obj);
            }
            obj.SetNativeMarked(true); return 0;
        }
        private void Relink(KingdomQuestPineNativeSceneObject obj)
        {
            // Effect +8A0 is a no-op. Door 0x00566E80 calls fm_GetCenter
            // 0x00461D60: coordinate writes only, no axis repair or new marking.
            if (obj is KingdomQuestPineNativeDoorObject && obj.Map != null)
                obj.SetNativePosition(unchecked((int)(obj.Map.XLimit / 2)), unchecked((int)(obj.Map.YLimit / 2)));
        }
        internal void Move(KingdomQuestPineNativeSceneObject obj, int x, int y)
        {
            // fm_InMapCoord 0x00461D80 uses signed exclusive upper bounds.
            if (x < 0 || x >= unchecked((int)Identity.XLimit) || y < 0 || y >= unchecked((int)Identity.YLimit)) return;
            obj.SetNativePosition(x, y);
            Relocate(obj, obj.Axes.X); Relocate(obj, obj.Axes.Y);
            // +A38(7,0,0) is ret 12 for both concrete types. No packet here.
        }
        private void Relocate(KingdomQuestPineNativeSceneObject obj, KingdomQuestPineNativeAxisNode node)
        {
            var error = node.Relocate(report);
            if (!error.HasValue) return;
            report(error.Value, obj); Relink(obj);
            report(KingdomQuestPineAxisError.SaveFieldInformation, obj);
            report(KingdomQuestPineAxisError.DumpMap, obj);
        }
    }

    /// <summary>Source-owned map-list order, exact Name3 lookup and concrete door/effect axes.</summary>
    public sealed class KingdomQuestPineNativeMapRegistry
    {
        private readonly KingdomQuestPineNativeFieldMapAxes[] maps;
        private readonly Func<KingdomQuestPineNativeAxisCounters> counters;
        private readonly Action<KingdomQuestPineAxisError, object> report;
        public KingdomQuestPineNativeMapRegistry(IReadOnlyList<KingdomQuestPineNativeFieldMapAxes> maps,
            Func<KingdomQuestPineNativeAxisCounters> counters, Action<KingdomQuestPineAxisError, object> report)
        {
            if (maps == null || counters == null || report == null) throw new ArgumentNullException();
            this.maps = new KingdomQuestPineNativeFieldMapAxes[maps.Count];
            for (int i = 0; i < maps.Count; i++)
            { if (maps[i] == null || !maps[i].IsReady) throw new ArgumentException("Initialized native axes required."); this.maps[i] = maps[i]; }
            this.counters = counters; this.report = report;
        }
        public bool TryMark(KingdomQuestPineNativeSceneObject obj, byte ignoreBlock, out int nativeError)
        {
            nativeError = 0;
            // Only original door/effect so_MapMarking(1) is covered here.
            // Mob collision/random relocation and other object types stay unresolved.
            if (!(obj is KingdomQuestPineNativeDoorObject || obj is KingdomQuestPineNativeEffectObject) || ignoreBlock != 1) return false;
            byte[] login = obj.SnapshotLoginLocation21(); if (login == null) return false;
            foreach (var map in maps)
            {
                var name = map.Identity.SnapshotName12(); bool equal = true;
                for (int i = 0; i < 12; i++) if (login[i] != name[i]) { equal = false; break; }
                if (equal) { nativeError = map.Mark(obj, counters); return true; }
            }
            nativeError = 1; return true; // so_MapMarking no-map error: no mutations
        }
        public void FreeFromLists(KingdomQuestPineNativeSceneObject obj) => obj.Axes.FreeFromLists(report);
        public bool TryUnmark(KingdomQuestPineNativeSceneObject obj, int when, byte broadcast, byte reason,
            Action<KingdomQuestPineNativeSceneObject> broadcastLogout)
        {
            if (!(obj is KingdomQuestPineNativeDoorObject || obj is KingdomQuestPineNativeEffectObject) ||
                (broadcast != 0 && broadcastLogout == null)) return false;
            // Both +310 targets are ret. RemoveWhen/reason are unused here.
            if (broadcast != 0) broadcastLogout(obj);
            obj.Axes.X.Unlink(report); obj.Axes.Y.Unlink(report); obj.SetNativeMarked(false);
            return true;
        }
        public bool TryMoveTo(KingdomQuestPineNativeSceneObject obj, int x, int y, int argument)
        {
            if (!(obj is KingdomQuestPineNativeDoorObject || obj is KingdomQuestPineNativeEffectObject)) return false;
            if (obj.Map == null) return true;
            foreach (var map in maps)
                if (ReferenceEquals(map.Identity, obj.Map)) { map.Move(obj, x, y); return true; }
            return false; // an unbound native map pointer is not a movement no-op
        }
    }
}
