using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineBriefFamily
    { Character, Mob, Item, MagicField, MiniHouse, Mover, Pet, CharacterAbstate, MobAbstate }

    public enum KingdomQuestPineBriefEvent { InvalidViewKind, InvalidObjectType }

    /// <summary>
    /// Required native virtual calls and thread-owned services. These are NOT
    /// emulator player/mob aliases. Unknown concrete owners must remain unbound.
    /// Door/effect payload copies, their constant virtuals and flag filtering
    /// are implemented below; mobile state, other families and transport are not.
    /// </summary>
    public interface IKingdomQuestPineNativeBriefServices
    {
        bool IsAvailable { get; }
        byte ObjectType(IKingdomQuestPineNativeAxialObject obj);
        int ViewKind(IKingdomQuestPineNativeAxialObject obj);
        byte MobileMode(IKingdomQuestPineNativeAxialObject obj);
        int StateA18(IKingdomQuestPineNativeAxialObject obj);
        int MovementState(IKingdomQuestPineNativeAxialObject obj);
        uint FieldSightSquared(IKingdomQuestPineNativeAxialObject obj);
        void QueueMovement(IKingdomQuestPineNativeAxialObject obj);
        // Door exchanger reads the SOURCE name (+0x56C) into the original
        // diagnostic ring before a possible flush; effect does not do this.
        void TraceDoorSourceName(IKingdomQuestPineNativeAxialObject source);
        void SendPacket(IKingdomQuestPineNativeAxialObject source, byte[] wire);
        bool TryExchangeOther(KingdomQuestPineBriefFamily family,
            IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate);
        void Report(KingdomQuestPineBriefEvent reason, IKingdomQuestPineNativeAxialObject candidate);
    }

    /// <summary>
    /// so_ExchageBriefInfo 0x00441460, door/effect exchange 0x0043FF60 /
    /// 0x004400A0 and send 0x0043D7D0 / 0x0043D900. One owner per native Zone
    /// thread: the original accumulators and sight limit are global, not per
    /// recipient or traversal. Nested callbacks observe the same mutable state.
    /// </summary>
    public sealed class KingdomQuestPineNativeBriefExchange
    {
        private sealed class Batch
        {
            // Original initialized .data: capacity 0x570, length/count zero,
            // door opcode 0x1C10 at 0x750D5C, effect 0x1C12 at 0x7512E4.
            internal readonly byte[] Wire = new byte[0x570];
            internal int Length;
            internal byte Count { get => Wire[2]; set => Wire[2] = value; }
            internal Batch(ushort opcode) { Wire[0] = (byte)opcode; Wire[1] = (byte)(opcode >> 8); }
        }

        private readonly IKingdomQuestPineNativeBriefServices services;
        private readonly Batch doors = new Batch(0x1C10), effects = new Batch(0x1C12);
        public byte DoorCount => doors.Count;
        public byte EffectCount => effects.Count;
        public int DoorPacketLength => doors.Length;
        public int EffectPacketLength => effects.Length;
        // 0x008B7834. Caller sets this at the source's completion/login site.
        public uint SourceSightSquared { get; set; }
        public KingdomQuestPineNativeReciprocalBrief Reciprocal { get; }

        public KingdomQuestPineNativeBriefExchange(IKingdomQuestPineNativeBriefServices services)
        { this.services = services ?? throw new ArgumentNullException(nameof(services)); }
        public KingdomQuestPineNativeBriefExchange(IKingdomQuestPineNativeBriefServices services,
            IKingdomQuestPineNativeReciprocalServices reciprocal, KingdomQuestPineNativeSubLayer layers) : this(services)
        { Reciprocal = new KingdomQuestPineNativeReciprocalBrief(services, reciprocal, layers); }

        // 0x0043C330. Missing map/data is checked BEFORE either unlimited flag.
        // Sight state +0x163 and map +0x124 compare exactly to 1; DWORD square
        // wraps. These values cannot be inferred from emulator map dimensions.
        public static uint FieldSightSquared(bool hasMap, bool hasMapData,
            uint sightState, uint mapState, uint sightDistance)
        {
            if (!hasMap || !hasMapData) return 0;
            if (sightState == 1 || mapState == 1) return uint.MaxValue;
            return unchecked(sightDistance * sightDistance);
        }

        public void InitializeDoors() => doors.Count = 0;
        public void InitializeEffects() => effects.Count = 0;

        private byte Type(IKingdomQuestPineNativeAxialObject obj) =>
            obj is KingdomQuestPineNativeSceneObject scene ? (byte)scene.NativeObjectType :
            obj is KingdomQuestPineNativeAxialFlag ? (byte)0 : services.ObjectType(obj);
        private int View(IKingdomQuestPineNativeAxialObject obj) =>
            obj is KingdomQuestPineNativeDoorObject ? 1 :
            obj is KingdomQuestPineNativeEffectObject ? 2 :
            obj is KingdomQuestPineNativeAxialFlag ? 0 : services.ViewKind(obj);
        private byte MobileMode(IKingdomQuestPineNativeAxialObject obj) =>
            obj is KingdomQuestPineNativeEffectObject || obj is KingdomQuestPineNativeAxialFlag
                ? (byte)1 : obj is KingdomQuestPineNativeDoorObject door && door.NativeMobileMode.HasValue
                    ? door.NativeMobileMode.Value : services.MobileMode(obj);
        private int StateA18(IKingdomQuestPineNativeAxialObject obj) =>
            obj is KingdomQuestPineNativeSceneObject || obj is KingdomQuestPineNativeAxialFlag
                ? 0 : services.StateA18(obj);
        private int MovementState(IKingdomQuestPineNativeAxialObject obj) =>
            obj is KingdomQuestPineNativeEffectObject || obj is KingdomQuestPineNativeAxialFlag
                ? 0 : obj is KingdomQuestPineNativeDoorObject door && door.NativeMovement != null
                    ? unchecked((int)door.NativeMovement.State) : services.MovementState(obj);

        private void Send(Batch batch, IKingdomQuestPineNativeAxialObject source)
        {
            if (batch.Count == 0 || Type(source) != 2 || MobileMode(source) == 0) return;
            // pp_SetPacketLen 0x004C8CE0 accepts length strictly BELOW capacity.
            // Reachable count <=28 gives at most 1347 bytes, below 1392.
            batch.Length = 3 + 48 * batch.Count;
            var wire = new byte[batch.Length]; Array.Copy(batch.Wire, wire, wire.Length);
            services.SendPacket(source, wire);
            // Send does NOT clear count, even after successful delivery.
        }
        public bool TrySendDoors(IKingdomQuestPineNativeAxialObject source)
        {
            if (source == null || !services.IsAvailable) return false;
            Send(doors, source); return true;
        }
        public bool TrySendEffects(IKingdomQuestPineNativeAxialObject source)
        {
            if (source == null || !services.IsAvailable) return false;
            Send(effects, source); return true;
        }

        private bool Exchange(Batch batch, IKingdomQuestPineNativeAxialObject source,
            IKingdomQuestPineNativeAxialObject candidate, bool door)
        {
            // Type 8 uses character dispatch elsewhere, but these two
            // exchangers accept ONLY initiating type 2, even if invisible.
            if (Type(source) != 2) return true;
            if (door) services.TraceDoorSourceName(source);
            if (batch.Count >= 28)
            {
                Send(batch, source);
                batch.Count = 0; // also after Send skipped an invisible source
            }
            // Unknown type-7/9 implementations must not invent payload bytes.
            var scene = candidate as KingdomQuestPineNativeSceneObject;
            if (scene == null || (door ? !(scene is KingdomQuestPineNativeDoorObject) :
                !(scene is KingdomQuestPineNativeEffectObject))) return false;
            scene.CopyNativeBriefPayload(batch.Wire.AsSpan(3 + batch.Count * 48, 48));
            batch.Count++; return true;
        }

        private bool Other(KingdomQuestPineBriefFamily family, IKingdomQuestPineNativeAxialObject source,
            IKingdomQuestPineNativeAxialObject candidate)
        {
            if (Reciprocal != null)
            {
                if (family == KingdomQuestPineBriefFamily.Character) return Reciprocal.TryExchangeCharacter(source, candidate);
                if (family == KingdomQuestPineBriefFamily.CharacterAbstate) return Reciprocal.TryExchangeAbstates(false, source, candidate);
                if (family == KingdomQuestPineBriefFamily.MobAbstate) return Reciprocal.TryExchangeAbstates(true, source, candidate);
            }
            return services.TryExchangeOther(family, source, candidate);
        }

        // False means UNRESOLVED, not a native visitor false: every original
        // branch returns true. Side effects before an unresolved call remain.
        public bool TryVisit(IKingdomQuestPineNativeAxialObject candidate,
            IKingdomQuestPineNativeAxialObject source, uint distanceSquared)
        {
            if (candidate == null || source == null || !services.IsAvailable) return false;
            Type(candidate); // original diagnostic virtual reads are retained
            switch (View(candidate))
            {
                case 0: return true;
                case 1: break;
                case 2: if (distanceSquared > services.FieldSightSquared(candidate)) return true; break;
                case 3: if (distanceSquared > SourceSightSquared) return true; break;
                default:
                    Type(candidate); services.Report(KingdomQuestPineBriefEvent.InvalidViewKind, candidate); break;
            }
            Type(candidate);
            if (MobileMode(candidate) == 0) return true;
            Type(candidate);
            if (StateA18(candidate) != 0 || MovementState(candidate) != 0)
                services.QueueMovement(candidate);
            switch (Type(candidate))
            {
                case 0: return true;
                case 1: return Other(KingdomQuestPineBriefFamily.Item, source, candidate);
                case 2: case 8:
                    return Other(KingdomQuestPineBriefFamily.Character, source, candidate) &&
                        Other(KingdomQuestPineBriefFamily.CharacterAbstate, source, candidate);
                case 3: return Other(KingdomQuestPineBriefFamily.MiniHouse, source, candidate);
                case 4: case 5: case 10:
                    return Other(KingdomQuestPineBriefFamily.Mob, source, candidate) &&
                        Other(KingdomQuestPineBriefFamily.MobAbstate, source, candidate);
                case 6: return Other(KingdomQuestPineBriefFamily.MagicField, source, candidate);
                case 7: return Exchange(doors, source, candidate, true);
                case 9: return Exchange(effects, source, candidate, false);
                case 11:
                    return Other(KingdomQuestPineBriefFamily.Mover, source, candidate) &&
                        Other(KingdomQuestPineBriefFamily.CharacterAbstate, source, candidate);
                case 12: return Other(KingdomQuestPineBriefFamily.Pet, source, candidate);
                default:
                    Type(candidate); services.Report(KingdomQuestPineBriefEvent.InvalidObjectType, candidate); return true;
            }
        }

        // Explicit normal-map binding only. Does not assume the map's virtual
        // dispatch, reset globals, flush buffers or run AI/mobile completion.
        public bool TryVisitNormalMap(KingdomQuestPineNativeMapTraversal traversal,
            IKingdomQuestPineNativeAxialObject source, byte includeSelf, out bool nativeResult)
        {
            nativeResult = false;
            if (traversal == null || source == null || !services.IsAvailable) return false;
            bool resolved = true;
            bool walked = traversal.TryAllInMapNormal(source, includeSelf, (candidate, origin, distance) =>
            {
                resolved = TryVisit(candidate, origin, distance); return resolved;
            }, out nativeResult);
            return walked && resolved;
        }
    }
}
