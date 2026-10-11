using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    // String fields at +0x82 + n*0x1C. Assign("",0) keeps capacity,
    // backing storage and the adjacent +0x18 DWORD, clearing length/byte 0.
    public sealed class KingdomQuestPineNativeAiString
    {
        private readonly byte[] storage;
        public int Length { get; private set; }
        public int Capacity => storage.Length - 1;
        public uint Field18 { get; set; }
        public KingdomQuestPineNativeAiString(byte[] initialStorage, int length, uint field18)
        {
            if (initialStorage == null || initialStorage.Length < 16 || length < 0 || length >= initialStorage.Length)
                throw new ArgumentException("Explicit valid native string storage and length required.");
            storage = (byte[])initialStorage.Clone(); Length = length; Field18 = field18;
        }
        public byte[] SnapshotStorage() => (byte[])storage.Clone();
        internal void Clear() { Length = 0; storage[0] = 0; }
    }

    public sealed class KingdomQuestPineNativeAiState
    {
        private readonly KingdomQuestPineNativeAiString[] slots;
        public object Script { get; set; } // +0x7E
        public KingdomQuestPineNativeAiString Slot(int index) => slots[index];
        public KingdomQuestPineNativeAiState(object script, KingdomQuestPineNativeAiString[] initialSlots)
        {
            if (initialSlots == null || initialSlots.Length != 8) throw new ArgumentException("Eight native AI strings required.");
            slots = (KingdomQuestPineNativeAiString[])initialSlots.Clone();
            for (int i = 0; i < 8; i++) if (slots[i] == null) throw new ArgumentException("Unbound AI string.");
            Script = script;
        }
        public void Initialize()
        {
            // so_InitAIScript 0x558C80: pointer first, then eight strings.
            Script = null;
            for (int i = 0; i < 8; i++) slots[i].Clear();
        }
    }

    /// <summary>
    /// MoveManager storage +0x18C..+0x1CB. Its constructor target 0x450350
    /// does not initialize these bytes. +4 is the opaque native owner pointer,
    /// assigned to the door by ShineMobileObject ctor; it is not dereferenced
    /// as a managed address. Other bytes require an explicit native seed.
    /// </summary>
    public sealed class KingdomQuestPineNativeDoorMovement
    {
        private readonly byte[] data;
        public uint State => Read(0);
        public KingdomQuestPineNativeDoorMovement(byte[] initial64)
        {
            if (initial64 == null || initial64.Length != 64) throw new ArgumentException("Explicit 64-byte MoveManager seed required.");
            data = (byte[])initial64.Clone();
        }
        public uint Read(int offset)
        {
            if (offset < 0 || offset > 60 || offset % 4 != 0) throw new ArgumentOutOfRangeException(nameof(offset));
            return (uint)data[offset] | ((uint)data[offset + 1] << 8) | ((uint)data[offset + 2] << 16) | ((uint)data[offset + 3] << 24);
        }
        public void Write(int offset, uint value)
        {
            if (offset < 0 || offset > 60 || offset % 4 != 0) throw new ArgumentOutOfRangeException(nameof(offset));
            for (int i = 0; i < 4; i++) data[offset + i] = (byte)(value >> (i * 8));
        }
        public byte[] Snapshot() => (byte[])data.Clone();
        internal void FinishBuild(KingdomQuestPineNativeDoorObject owner)
        {
            // 0x43F9B0: preserve EVERYTHING in state 3, including old XY.
            if (State == 3) return;
            Write(0, 0);
            for (int offset = 0x1C; offset <= 0x2C; offset += 4) Write(offset, 0);
            Write(8, 0);
            var position = owner.Location; // after the resets, current XY, not LoginLocation
            Write(0x14, unchecked((uint)position.X)); Write(0x18, unchecked((uint)position.Y));
        }
    }

    // Explicit map binding; no inference from emulator IDs or field size.
    // The original member-dispatch pointer is map+0x58 and adjustment +0x5C.
    public sealed class KingdomQuestPineNativeCompletionMap
    {
        public KingdomQuestPineEffectMapIdentity Identity { get; }
        public bool HasMapData { get; set; }
        public uint MapSightState { get; set; }
        public uint SightDistance { get; set; }
        public uint MemberFunctionAddress { get; set; }
        public int ThisAdjustment { get; set; }
        public KingdomQuestPineNativeCompletionMap(KingdomQuestPineEffectMapIdentity identity,
            bool hasMapData, uint mapSightState, uint sightDistance, uint memberFunctionAddress, int thisAdjustment)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            HasMapData = hasMapData; MapSightState = mapSightState; SightDistance = sightDistance;
            MemberFunctionAddress = memberFunctionAddress; ThisAdjustment = thisAdjustment;
        }
    }

    /// <summary>
    /// Concrete door BuildComplete 0x4422A0 / effect BlastComplete 0x4423E0.
    /// Native traversal false is ignored; an UNRESOLVED managed dependency is
    /// not. Already-completed writes survive unresolved/exceptional exits.
    /// </summary>
    public sealed class KingdomQuestPineNativeSceneCompletion
    {
        private readonly Dictionary<KingdomQuestPineEffectMapIdentity, KingdomQuestPineNativeCompletionMap> maps =
            new Dictionary<KingdomQuestPineEffectMapIdentity, KingdomQuestPineNativeCompletionMap>();
        private readonly KingdomQuestPineNativeBriefExchange exchange;
        private readonly KingdomQuestPineNativeMapTraversal traversal;
        public KingdomQuestPineNativeSceneCompletion(KingdomQuestPineNativeBriefExchange exchange,
            KingdomQuestPineNativeMapTraversal traversal)
        {
            this.exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            this.traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
        }
        public bool TryBindMap(KingdomQuestPineNativeCompletionMap map)
        {
            if (map == null || maps.ContainsKey(map.Identity)) return false;
            maps.Add(map.Identity, map); return true;
        }
        public bool TryFieldSightSquared(KingdomQuestPineNativeSceneObject obj, out uint sight)
        {
            sight = 0;
            if (obj == null) return false;
            if (obj.Map == null) return true;
            if (!maps.TryGetValue(obj.Map, out var map)) return false;
            if (!map.HasMapData) return true;
            if (!obj.NativeSightState.HasValue) return false;
            sight = KingdomQuestPineNativeBriefExchange.FieldSightSquared(true, true,
                obj.NativeSightState.Value, map.MapSightState, map.SightDistance); return true;
        }
        public bool TryVisitMap(KingdomQuestPineNativeSceneObject obj, out bool nativeResult)
        {
            nativeResult = false;
            if (obj == null) return false;
            if (obj.Map == null) return true; // so_AllInMap 0x54B8C0
            if (!maps.TryGetValue(obj.Map, out var map)) return false;
            if (map.MemberFunctionAddress == 0) return true;
            if (map.MemberFunctionAddress != 0x0054BE00 || map.ThisAdjustment != 0) return false;
            return exchange.TryVisitNormalMap(traversal, obj, 0, out nativeResult);
        }
        private bool VisitCompletion(KingdomQuestPineNativeSceneObject obj)
        {
            if (!TryFieldSightSquared(obj, out uint sight)) return false;
            exchange.SourceSightSquared = sight;
            return TryVisitMap(obj, out _); // a KNOWN native false is deliberately ignored
        }
        public bool TryDoorBuildComplete(KingdomQuestPineNativeDoorObject obj, ushort unusedHandle)
        {
            if (obj == null || obj.NativeAi == null) return false;
            obj.NativeAi.Initialize();
            obj.NativeWord168 = 0; obj.NativeMobileMode = 1; obj.NativeByte1F0C = 0;
            obj.SetNativeMode(0, 0);
            exchange.InitializeDoors(); // count pointer 0x7512D0 belongs to door buffer
            if (!VisitCompletion(obj)) return false;
            obj.NativeFlags1E78 &= 0xFD; obj.NativeWord168 = 0;
            return true;
        }
        public bool TryEffectBlastComplete(KingdomQuestPineNativeEffectObject obj, ushort unusedHandle)
        {
            if (obj == null || obj.NativeAi == null) return false;
            obj.NativeAi.Initialize(); obj.SetNativeMode(0, 0);
            exchange.InitializeEffects(); // count pointer 0x751858 belongs to effect buffer
            return VisitCompletion(obj);
        }
        public static bool TryFinishDoorBuildMovement(KingdomQuestPineNativeDoorObject obj)
        {
            if (obj == null || obj.NativeMovement == null) return false;
            obj.NativeMovement.FinishBuild(obj); return true;
        }
    }
}
