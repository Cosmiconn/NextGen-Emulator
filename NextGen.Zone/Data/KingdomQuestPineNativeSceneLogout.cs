using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public interface IKingdomQuestPineNativeLogoutBooths
    {
        // Actual BoothManager at map+0x1F0. Unknown state must return false,
        // never an invented empty manager. Close includes all native side effects.
        bool TryCheckStoreIsOpening(ushort nativeHandle, out byte nativeResult);
        bool TryCloseStore(ushort nativeHandle, out bool nativeResult);
    }

    public sealed class KingdomQuestPineNativeLogoutMap
    {
        public KingdomQuestPineEffectMapIdentity Identity { get; }
        public uint Field0C { get; set; } // nonzero gates packet traversal
        public uint IteratorFunctionAddress { get; set; } // +0x68, not member callback +0x58
        public int IteratorThisAdjustment { get; set; } // +0x6C
        public IKingdomQuestPineNativeLogoutBooths Booths { get; set; }
        public KingdomQuestPineNativeLogoutMap(KingdomQuestPineEffectMapIdentity identity, uint field0C,
            uint iteratorFunctionAddress, int iteratorThisAdjustment, IKingdomQuestPineNativeLogoutBooths booths)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Field0C = field0C; IteratorFunctionAddress = iteratorFunctionAddress;
            IteratorThisAdjustment = iteratorThisAdjustment; Booths = booths;
        }
    }

    public enum KingdomQuestPineLogoutEvent { StoreCloseFailed, UnknownAxisOwner, NullPacketMap }

    /// <summary>
    /// Door/effect so_BroadcastLogout 0x0054F290 -> so_Logout 0x0054B780 ->
    /// so_LogoutPacketBroadcast 0x00553A40. Does not unlink/free objects itself.
    /// Call through the map registry's Unmark to retain broadcast-before-unlink.
    /// </summary>
    public sealed class KingdomQuestPineNativeSceneLogout
    {
        private readonly Dictionary<KingdomQuestPineEffectMapIdentity, KingdomQuestPineNativeLogoutMap> maps =
            new Dictionary<KingdomQuestPineEffectMapIdentity, KingdomQuestPineNativeLogoutMap>();
        private readonly KingdomQuestPineNativeMapTraversal traversal;
        private readonly KingdomQuestPineNativeSubLayer layers;
        private readonly IKingdomQuestPineNativeBroadcastServices services;
        private readonly Action<KingdomQuestPineLogoutEvent, KingdomQuestPineNativeSceneObject> report;
        public KingdomQuestPineNativeSharedPacket Packet { get; }
        public KingdomQuestPineNativeSceneLogout(KingdomQuestPineNativeMapTraversal traversal,
            KingdomQuestPineNativeSubLayer layers, IKingdomQuestPineNativeBroadcastServices services,
            KingdomQuestPineNativeSharedPacket packet,
            Action<KingdomQuestPineLogoutEvent, KingdomQuestPineNativeSceneObject> report)
        {
            this.traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
            this.layers = layers ?? throw new ArgumentNullException(nameof(layers));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
        }
        public bool TryBindMap(KingdomQuestPineNativeLogoutMap map)
        {
            if (map == null || maps.ContainsKey(map.Identity)) return false;
            maps.Add(map.Identity, map); return true;
        }
        private bool TryIsSelf(KingdomQuestPineNativeAxisNode node, KingdomQuestPineNativeSceneObject obj, out bool isSelf)
        {
            isSelf = false;
            if (!(node.Owner is IKingdomQuestPineNativeAxialObject owner))
            { report(KingdomQuestPineLogoutEvent.UnknownAxisOwner, obj); return false; }
            isSelf = ReferenceEquals(owner.RangeObject, obj); return true;
        }
        public bool TryBroadcastLogout(KingdomQuestPineNativeSceneObject obj)
        {
            if (obj == null) return false;
            if (obj.Map == null) return true; // before axis checks
            if (!TryIsSelf(obj.Axes.X.Previous, obj, out bool self)) return false;
            if (self) return true;
            if (!TryIsSelf(obj.Axes.X.Next, obj, out self)) return false;
            if (self) return true; // either self-link skips booth/packet work
            // Range-object virtuals above may have changed the map. so_Logout
            // skips booth work for null, but still calls packet broadcast.
            if (obj.Map != null)
            {
                if (!maps.TryGetValue(obj.Map, out var map) || map.Booths == null) return false;
                var booths = map.Booths; // native stores this pointer before calls
                if (!booths.TryCheckStoreIsOpening(obj.NativeHandle, out byte open)) return false;
                if (open == 1)
                {
                    if (!booths.TryCloseStore(obj.NativeHandle, out bool closed)) return false;
                    if (!closed) report(KingdomQuestPineLogoutEvent.StoreCloseFailed, obj);
                }
            }
            return TryLogoutPacketBroadcast(obj); // reads possibly changed map AFTER booth calls
        }
        // so_AllInMap(iterator) 0x0054B9E0. Known native false and unresolved
        // managed bindings are distinct; only the former authorizes the retry.
        public bool TryVisitMap(KingdomQuestPineNativeSceneObject obj,
            IKingdomQuestPineNativeAxialIterator iterator, out bool nativeResult)
        {
            nativeResult = false;
            if (obj == null || iterator == null) return false;
            if (obj.Map == null) return true;
            if (!maps.TryGetValue(obj.Map, out var map)) return false;
            if (map.IteratorFunctionAddress == 0) return true;
            if (map.IteratorFunctionAddress != 0x0054C370 || map.IteratorThisAdjustment != 0) return false;
            return traversal.TryAllInMapNormalIterator(obj, 0, iterator, out nativeResult);
        }
        public bool TryLogoutPacketBroadcast(KingdomQuestPineNativeSceneObject obj)
        {
            if (obj == null) return false;
            // Opcode 0x1805, WORD source handle. Shared bytes and
            // length are written even when the map's +0x0C gate is zero.
            ushort handle = obj.NativeHandle;
            Packet.Write(0, new byte[] { 0x05, 0x18, (byte)handle, (byte)(handle >> 8) });
            var iterator = new KingdomQuestPineNativeAxialPacketBroadcast(Packet, 4, layers, services);
            if (obj.Map == null) { report(KingdomQuestPineLogoutEvent.NullPacketMap, obj); return false; }
            if (!maps.TryGetValue(obj.Map, out var map)) return false;
            if (map.Field0C == 0) return true;
            if (!TryVisitMap(obj, iterator, out bool result)) return false;
            if (!result && !TryVisitMap(obj, iterator, out _)) return false;
            // Ignore second native false. Do not reset bytes/length, recheck
            // Field0C, restore nested writes, or suppress duplicate recipients.
            return true;
        }
    }
}
