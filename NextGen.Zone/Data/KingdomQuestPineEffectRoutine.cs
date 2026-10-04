using System;
using System.IO;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Actual FieldMap Name3 and unsigned coordinate limits (+0x18/+0x1C).
    /// These are not a client map ID, an emulator instance ID or bitmap dimensions.
    /// </summary>
    public sealed class KingdomQuestPineEffectMapIdentity
    {
        private readonly byte[] name12;
        public uint XLimit { get; }
        public uint YLimit { get; }
        public KingdomQuestPineEffectMapIdentity(byte[] nativeName12, uint xLimit, uint yLimit)
        {
            if (nativeName12 == null || nativeName12.Length != 12)
                throw new ArgumentException("Native FieldMap Name3 required.", nameof(nativeName12));
            name12 = (byte[])nativeName12.Clone();
            XLimit = xLimit; YLimit = yLimit;
        }
        public byte[] SnapshotName12() => (byte[])name12.Clone();
        internal bool SameName(KingdomQuestPineEffectMapIdentity other)
        {
            if (other == null) return false;
            for (int i = 0; i < 12; i++) if (name12[i] != other.name12[i]) return false;
            return true;
        }
    }

    public sealed class KingdomQuestPineEffectLocation
    {
        // Null Map is the native null map pointer, not an unavailable service.
        public KingdomQuestPineEffectMapIdentity Map { get; }
        public uint Mode { get; }
        public int X { get; }
        public int Y { get; }
        public byte Direction { get; }
        public KingdomQuestPineEffectLocation(KingdomQuestPineEffectMapIdentity map,
            uint mode, int x, int y, byte direction)
        { Map = map; Mode = mode; X = x; Y = y; Direction = direction; }
    }

    public interface IKingdomQuestPineEffectRoutineOwner
    {
        // All callbacks must be available and run on the owning native thread.
        // Do not attach this routine to a second, independent timer.
        bool IsAvailable { get; }
        uint CurrentNativeTick { get; }
        ushort Handle { get; }
        // Non-null throughout dispatch, including after Free. Its Map may be null.
        KingdomQuestPineEffectLocation Location { get; }
        KingdomQuestPineEffectLocation ParentLocation { get; }
        void ReportOutOfBounds();
        // Execute so_MoveTo, including its native map/axis/update operations.
        // This is not a direct emulator Position assignment or map transfer.
        void MoveTo(int x, int y, int nativeArgument);
        void Unmark(int argument0, int argument1, int reason);
        bool Free(ushort handle, int removeWhen, int reason);
        // Read AFTER Free: Unmark/Free may have changed the script/map/handle.
        bool HasRoutineScript { get; }
        void RunRoutineScript(ushort handle, KingdomQuestPineEffectMapIdentity map);
    }

    /// <summary>
    /// ShineEffectObject::so_Routine 0x00569EF0. Owns effect expiry and the
    /// strict follow timer; live map, object-pool and Lua services are explicit.
    /// A successful call means native routine dispatch, not successful Free.
    /// </summary>
    public sealed class KingdomQuestPineEffectRoutine : IKingdomQuestPineRetreatObject
    {
        public const uint NativeRoutineAddress = 0x00569EF0;
        public const uint NativeDistanceSquaredAddress = 0x004028F0;
        public const uint FollowInterval = 50;
        public const uint DifferentSpaceDistanceSquared = 999999999;
        private readonly KingdomQuestPineEffectLifetime lifetime;
        public uint Deadline => lifetime.Deadline;
        public uint NextFollowTick { get; private set; }

        // EffectBlast reads the native clock separately for expiry and follow.
        public KingdomQuestPineEffectRoutine(uint expiryClock, uint followClock, uint milliseconds)
        {
            lifetime = new KingdomQuestPineEffectLifetime(expiryClock, milliseconds);
            NextFollowTick = unchecked(followClock + FollowInterval);
        }
        public void RetreatFromMap() => lifetime.RetreatFromMap();

        public static uint DistanceSquared(KingdomQuestPineEffectLocation from,
            KingdomQuestPineEffectLocation to)
        {
            if (from == null || to == null || from.Map == null || to.Map == null ||
                !from.Map.SameName(to.Map) || from.Mode != to.Mode)
                return DifferentSpaceDistanceSquared;
            unchecked
            {
                int dx = from.X - to.X, dy = from.Y - to.Y;
                return (uint)(dx * dx + dy * dy);
            }
        }

        public bool TryStep(IKingdomQuestPineEffectRoutineOwner owner)
        {
            if (owner == null || !owner.IsAvailable) return false;
            var location = owner.Location;
            if (location == null) return false;
            if (location.Map != null &&
                (unchecked((uint)location.X) >= location.Map.XLimit ||
                 unchecked((uint)location.Y) >= location.Map.YLimit))
            {
                owner.ReportOutOfBounds();
                lifetime.RetreatFromMap();
            }

            // Native JBE skips at equality. Advance the OLD deadline exactly
            // once per call, even with no parent; no catch-up loop or now+50.
            if (owner.CurrentNativeTick > NextFollowTick)
            {
                NextFollowTick = unchecked(NextFollowTick + FollowInterval);
                var parent = owner.ParentLocation;
                if (parent != null && DistanceSquared(owner.Location, parent) > 100)
                    owner.MoveTo(parent.X, parent.Y, 5);
            }

            // Follow precedes expiry, including after vanish/out-of-bounds.
            if (lifetime.IsExpired(owner.CurrentNativeTick))
            {
                owner.Unmark(0, 1, 3);
                owner.Free(owner.Handle, 0, 5); // native return is ignored
            }
            if (owner.HasRoutineScript)
                owner.RunRoutineScript(owner.Handle, owner.Location.Map);
            return true;
        }
    }

    /// <summary>
    /// BriefInformationEffect 0x005491F0 / field copy 0x005553D0:
    /// 0x1C11 + 48 payload bytes, before transport framing and encryption.
    /// Direction is copied from the parent byte, not converted from degrees.
    /// </summary>
    public static class KingdomQuestPineEffectBrief
    {
        public const ushort Opcode = 0x1C11;
        public const int NativeWireSize = 50;
        // so_RemakeHandle 0x00555450. This does not allocate a pool slot.
        public static ushort RemakeNativeHandle(ushort poolIndex) =>
            KingdomQuestPineNativeObjectHandles.Encode(9, poolIndex);
        // EffectBlast changes only bit zero; retain all other stored bits.
        public static byte ApplyBlastFlag(byte previousFlags, int argument) =>
            (byte)((previousFlags & 0xFE) | (argument & 1));

        public static byte[] CreateNativeWire(ushort handle, byte[] name32,
            int x, int y, byte direction, ushort parentHandle, int scale, byte flags)
        {
            if (name32 == null || name32.Length != 32)
                throw new ArgumentException("Native effect Name32 required.", nameof(name32));
            using (var stream = new MemoryStream(NativeWireSize))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Opcode);
                writer.Write(handle);
                writer.Write(name32);
                writer.Write(x);
                writer.Write(y);
                writer.Write(direction);
                writer.Write(parentHandle);
                writer.Write(unchecked((ushort)scale));
                writer.Write(flags);
                return stream.ToArray();
            }
        }
    }
}
