using System;

namespace NextGen.Zone.Data
{
    public interface IKingdomQuestPineNativeEffectParent
    {
        ushort NativeHandle { get; }
        KingdomQuestPineEffectLocation Location { get; }
        // Native LoginLocation is Name3 + i32 X/Y + u8 direction (21 bytes).
        // This is independent of the current map pointer and current position.
        byte[] SnapshotLoginLocation21();
    }

    public interface IKingdomQuestPineNativeSceneServices
    {
        bool IsAvailable { get; }
        // Entire so_MapMarking(1), including map lookup, axes and native errors.
        // Update the object's actual map pointer at the original write site,
        // including a failing fm_Marking. Do not infer it from the login name.
        int Mark(KingdomQuestPineNativeSceneObject obj, byte ignoreBlock);
        void FreeFromLists(KingdomQuestPineNativeSceneObject obj);
    }

    public sealed class KingdomQuestPineNativeDoorMobData
    {
        // Original MobDataBoxIndex's row DWORD +0x46. No emulator alias inferred.
        public uint Field46 { get; }
        public KingdomQuestPineNativeDoorMobData(uint field46) { Field46 = field46; }
    }

    public interface IKingdomQuestPineNativeDoorServices : IKingdomQuestPineNativeSceneServices
    {
        KingdomQuestPineNativeDoorMobData FindMobData(ushort mobId);
        void ReportMissingMobData();
        // Full 0x004422A0, including AI initialization, mobile fields and the
        // original visibility traversal. A global broadcast is not equivalent.
        void BuildComplete(KingdomQuestPineNativeDoorObject obj, ushort handle);
        // 0x0043F9B0..0x0043F9FD: preserve state 3; otherwise reset the native
        // mobile movement state and capture current XY. Required dependency.
        void FinishBuildMovement(KingdomQuestPineNativeDoorObject obj);
        KingdomQuestMapCollision GetCollision(KingdomQuestPineNativeDoorObject obj);
        void BroadcastDoorAction(KingdomQuestPineNativeDoorObject obj, byte[] wire);
    }

    public interface IKingdomQuestPineNativeEffectServices : IKingdomQuestPineNativeSceneServices
    {
        uint CurrentNativeTick { get; }
        // Full 0x004423E0, including AI initialization, mode reset and native
        // visibility traversal. It runs only after successful marking.
        void BlastComplete(KingdomQuestPineNativeEffectObject obj, ushort handle);
        void ReportOutOfBounds(KingdomQuestPineNativeEffectObject obj);
        void MoveTo(KingdomQuestPineNativeEffectObject obj, int x, int y, int argument);
        void Unmark(KingdomQuestPineNativeEffectObject obj, int when, int broadcast, int reason);
        bool HasRoutineScript(KingdomQuestPineNativeEffectObject obj);
        void RunRoutineScript(KingdomQuestPineNativeEffectObject obj, ushort handle,
            KingdomQuestPineEffectMapIdentity map);
    }

    /// <summary>
    /// Persistent native slot state, separate from emulator MapObjectID. Original
    /// brief constructors set scale but do NOT zero their packet buffers. The
    /// initial 48 bytes must therefore be supplied explicitly, never guessed.
    /// Map/axis/visibility services remain mandatory before live attachment.
    /// </summary>
    public abstract class KingdomQuestPineNativeSceneObject : IKingdomQuestPineNativePoolObject,
        IKingdomQuestPineNativeEffectParent
    {
        protected readonly byte[] payload;
        protected readonly byte[] login = new byte[21];
        protected readonly IKingdomQuestPineNativeSceneServices scene;
        private readonly int positionOffset, directionOffset;
        private readonly ushort opcode;
        private bool hasLogin;
        public KingdomQuestPineNativeObjectType NativeObjectType { get; }
        public ushort NativeHandle { get; }
        public KingdomQuestPineEffectMapIdentity Map { get; private set; }
        public uint Mode { get; private set; }
        public byte ModeFlags { get; private set; }
        public bool IsMarked { get; private set; }
        public KingdomQuestPineEffectLocation Location => new KingdomQuestPineEffectLocation(
            Map, Mode, ReadInt32(positionOffset), ReadInt32(positionOffset + 4), payload[directionOffset]);

        protected KingdomQuestPineNativeSceneObject(ushort type, ushort poolIndex,
            byte[] initialPayload48, IKingdomQuestPineNativeSceneServices scene,
            ushort opcode, int positionOffset, int directionOffset, int scaleOffset)
        {
            ushort handle = KingdomQuestPineNativeObjectHandles.Encode(type, poolIndex);
            if (handle == ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(poolIndex));
            if (initialPayload48 == null || initialPayload48.Length != 48)
                throw new ArgumentException("Explicit native 48-byte payload seed required.", nameof(initialPayload48));
            this.scene = scene ?? throw new ArgumentNullException(nameof(scene));
            NativeObjectType = (KingdomQuestPineNativeObjectType)type; NativeHandle = handle;
            payload = (byte[])initialPayload48.Clone();
            this.opcode = opcode; this.positionOffset = positionOffset; this.directionOffset = directionOffset;
            WriteUInt16(scaleOffset, 1000); // BriefInformationDoor/Effect constructors
            // ShineObject ctor initializes map, mode, mode flags and marked flag
            // to zero; derived ctors clear only the Name3 portion of LoginLocation.
            // Its remaining bytes are unavailable until Build/Blast assigns them.
        }

        public byte[] SnapshotBriefWire()
        {
            var wire = new byte[50]; wire[0] = (byte)opcode; wire[1] = (byte)(opcode >> 8);
            payload.CopyTo(wire, 2); return wire;
        }
        public byte[] SnapshotLoginLocation21() => hasLogin ? (byte[])login.Clone() : null;
        protected void SetLogin(byte[] value)
        {
            if (value == null || value.Length != 21)
                throw new InvalidOperationException("Native parent LoginLocation is unavailable.");
            value.CopyTo(login, 0); hasLogin = true;
        }
        protected void SetLogin(byte[] name12, int x, int y, byte direction)
        {
            name12.CopyTo(login, 0); WriteInt32(login, 12, x); WriteInt32(login, 16, y);
            login[20] = direction; hasLogin = true;
        }
        // Explicit write operations for the source-backed map/completion owners.
        // Free itself must not clear the map pointer, payload, parent or login.
        public void SetNativeMap(KingdomQuestPineEffectMapIdentity map) { Map = map; }
        public void SetNativeMode(uint mode, byte flags) { Mode = mode; ModeFlags = flags; }
        public void SetNativeMarked(bool marked) { IsMarked = marked; }
        public void SetNativePosition(int x, int y)
        { WriteInt32(payload, positionOffset, x); WriteInt32(payload, positionOffset + 4, y); }
        public void SetNativeDirection(byte direction) { payload[directionOffset] = direction; }
        public abstract void InitializeForAllocation();
        public void FreeFromLists() => scene.FreeFromLists(this);
        // Both concrete vtables point +0x2F0 to 0x00549070 (ret 4).
        public void InvokeManagerFreeCallback() { }
        protected int Mark()
        {
            int result = scene.Mark(this, 1);
            if (result == 0) IsMarked = true; // so_MapMarking 0x00466599
            return result;
        }
        protected void WriteUInt16(int offset, ushort value)
        { payload[offset] = (byte)value; payload[offset + 1] = (byte)(value >> 8); }
        protected int ReadInt32(int offset) => unchecked((int)((uint)payload[offset] |
            ((uint)payload[offset + 1] << 8) | ((uint)payload[offset + 2] << 16) | ((uint)payload[offset + 3] << 24)));
        protected static void WriteInt32(byte[] target, int offset, int value)
        { for (int i = 0; i < 4; i++) target[offset + i] = unchecked((byte)(value >> (i * 8))); }
    }

    /// <summary>ShineDoor::so_door_Build 0x0043F7B0 and persistent brief state.</summary>
    public sealed class KingdomQuestPineNativeDoorObject : KingdomQuestPineNativeSceneObject,
        IKingdomQuestPineDoorObject
    {
        private readonly IKingdomQuestPineNativeDoorServices services;
        byte IKingdomQuestPineDoorObject.NativeObjectType => 7;
        public byte StaticWalkEnabled { get; private set; }
        public ushort StaticWalkSpeed { get; private set; }
        public byte StaticRunEnabled { get; private set; }
        public ushort StaticRunSpeed { get; private set; }
        public ushort NativeWord168 { get; set; }
        public KingdomQuestPineNativeDoorMobData MobData { get; private set; }
        public uint NativeMobField46 { get; private set; }
        public ulong RegistrationNumber { get; private set; }
        public KingdomQuestPineNativeDoorObject(ushort index, byte[] initialPayload48,
            IKingdomQuestPineNativeDoorServices services)
            : base(7, index, initialPayload48, services, 0x1C0F, 4, 12, 46) { this.services = services; }
        public void SetStaticWalkSpeed(byte enabled, ushort speed)
        { StaticWalkEnabled = enabled; StaticWalkSpeed = speed; }
        public void SetStaticRunSpeed(byte enabled, ushort speed)
        { StaticRunEnabled = enabled; StaticRunSpeed = speed; }
        public override void InitializeForAllocation()
        {
            // so_Init -> +E7C 0x005558F0, then +E80 0x00555910. No blanket reset.
            SetStaticWalkSpeed(0, 0); SetStaticRunSpeed(0, 0);
        }
        public bool TryBuild(KingdomQuestPineDoorBuildRequest request, out int nativeError)
        {
            nativeError = 0;
            if (request == null || request.Handle != NativeHandle || !services.IsAvailable) return false;
            var source = request.Source;
            NativeWord168 = 0;
            byte direction = KingdomQuestPineDoorBrief.EncodeDirection(source.DirectionDegrees);
            SetLogin(request.SnapshotMapName12(), source.X, source.Y, direction);
            SetNativePosition(source.X, source.Y); SetNativeDirection(direction);
            payload[13] = 0; Array.Clear(payload, 14, 32);
            MobData = services.FindMobData(request.MobId);
            if (MobData == null)
            { services.ReportMissingMobData(); nativeError = 3; return true; }
            nativeError = Mark();
            if (nativeError != 0) return true;
            NativeMobField46 = MobData.Field46;
            // These fields are written AFTER successful marking, unlike effect.
            WriteUInt16(0, request.Handle); WriteUInt16(2, request.MobId);
            WriteUInt16(46, KingdomQuestHoneyingDoorBuildNativePlan.Scale);
            RegistrationNumber = KingdomQuestHoneyingDoorBuildNativePlan.RegistrationNumber;
            services.BuildComplete(this, request.Handle);
            services.FinishBuildMovement(this);
            NativeWord168 = 0;
            return true;
        }
        public bool TryApplyDoorAction(KingdomQuestHoneyingDoorNativePlan plan)
        {
            if (plan == null || !services.IsAvailable) return false;
            var collision = services.GetCollision(this);
            return new KingdomQuestPineDoorActionOwner(collision, NativeHandle,
                (action, name) => { payload[13] = action; name.CopyTo(payload, 14); },
                wire => services.BroadcastDoorAction(this, wire)).TryApplyDoorAction(plan);
        }
    }

    /// <summary>ShineEffectObject::EffectBlast 0x004B1540, bound to a real pool slot.</summary>
    public sealed class KingdomQuestPineNativeEffectObject : KingdomQuestPineNativeSceneObject,
        IKingdomQuestPineRetreatObject, IKingdomQuestPineEffectRoutineOwner
    {
        private readonly IKingdomQuestPineNativeEffectServices services;
        private readonly KingdomQuestPineNativeObjectManager manager;
        private bool hasBlastState;
        public IKingdomQuestPineNativeEffectParent Parent { get; private set; }
        public KingdomQuestPineEffectRoutine Routine { get; } = new KingdomQuestPineEffectRoutine(0, 0, 0);
        public KingdomQuestPineNativeEffectObject(ushort index, byte[] initialPayload48,
            IKingdomQuestPineNativeEffectServices services, KingdomQuestPineNativeObjectManager manager)
            : base(9, index, initialPayload48, services, 0x1C11, 34, 42, 45)
        { this.services = services; this.manager = manager ?? throw new ArgumentNullException(nameof(manager)); }
        // Both +E7C/+E80 target 0x00509900 (ret 8): allocation changes NO fields.
        public override void InitializeForAllocation() { }
        public bool TryBlast(KingdomQuestPineEffectBlastRequest request, out int nativeError)
        {
            nativeError = 0;
            if (request == null || request.Handle != NativeHandle || !services.IsAvailable ||
                !(request.Parent is IKingdomQuestPineNativeEffectParent parent)) return false;
            Parent = parent;
            Routine.ResetForBlast(() => services.CurrentNativeTick,
                KingdomQuestHoneyingEffectNativePlan.DurationMilliseconds);
            ushort parentHandle = parent.NativeHandle;
            WriteUInt16(0, request.Handle); request.Source.CreateEffectName32().CopyTo(payload, 2);
            WriteUInt16(43, parentHandle); WriteUInt16(45, KingdomQuestHoneyingEffectNativePlan.Scale);
            payload[47] = KingdomQuestPineEffectBrief.ApplyBlastFlag(payload[47],
                KingdomQuestHoneyingEffectNativePlan.TrailingArgument);
            var position = parent.Location;
            SetNativePosition(position.X, position.Y);
            SetNativeDirection(parent.Location.Direction);
            SetLogin(parent.SnapshotLoginLocation21());
            // Name3 comes from LoginLocation, XY/direction from CURRENT parent
            // state; the theater map argument is unused by this overload.
            var own = Location;
            WriteInt32(login, 12, own.X); WriteInt32(login, 16, own.Y); login[20] = own.Direction;
            hasBlastState = true;
            nativeError = Mark();
            if (nativeError == 0) services.BlastComplete(this, request.Handle);
            return true;
        }
        public void RetreatFromMap() => Routine.RetreatFromMap();
        public bool TryRunRoutine() => hasBlastState && Routine.TryStep(this);
        bool IKingdomQuestPineEffectRoutineOwner.IsAvailable => hasBlastState && services.IsAvailable;
        uint IKingdomQuestPineEffectRoutineOwner.CurrentNativeTick => services.CurrentNativeTick;
        ushort IKingdomQuestPineEffectRoutineOwner.Handle => NativeHandle;
        KingdomQuestPineEffectLocation IKingdomQuestPineEffectRoutineOwner.ParentLocation => Parent?.Location;
        void IKingdomQuestPineEffectRoutineOwner.ReportOutOfBounds() => services.ReportOutOfBounds(this);
        void IKingdomQuestPineEffectRoutineOwner.MoveTo(int x, int y, int argument) => services.MoveTo(this, x, y, argument);
        void IKingdomQuestPineEffectRoutineOwner.Unmark(int when, int broadcast, int reason) => services.Unmark(this, when, broadcast, reason);
        bool IKingdomQuestPineEffectRoutineOwner.Free(ushort handle, int when, int reason)
        {
            bool result;
            if (!manager.TryFree(handle, when, reason, out result))
                throw new InvalidOperationException("Effect's native pool is not bound.");
            return result;
        }
        bool IKingdomQuestPineEffectRoutineOwner.HasRoutineScript => services.HasRoutineScript(this);
        void IKingdomQuestPineEffectRoutineOwner.RunRoutineScript(ushort handle, KingdomQuestPineEffectMapIdentity map) =>
            services.RunRoutineScript(this, handle, map);
    }
}
