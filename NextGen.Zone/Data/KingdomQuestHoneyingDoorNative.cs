using System;
using System.Text;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Source-locked Honeying dooropen/doorclose calls. sa_Step resolves the
    /// object before evaluating its name; type 7 dispatches vtable +0x318.
    /// Null/wrong-type objects log and pop, without touching collision/wire.
    /// </summary>
    public sealed class KingdomQuestHoneyingDoorNativePlan
    {
        public const uint NativeOpenStepAddress = 0x004ED970;
        public const uint NativeCloseStepAddress = 0x004EDB30;
        public const uint NativeDoorActionAddress = 0x00550770;
        public const byte NativeDoorObjectType = 7;
        public const int NativeActionVtableOffset = 0x318;
        public const ushort NativeActionOpcode = 0x6C09;
        private readonly byte[] objectToken;
        private readonly byte[] name32;
        public int CanonicalLine { get; }
        public string ObjectIdentifier { get; }
        public string CollisionName { get; }
        public byte Action { get; }

        private KingdomQuestHoneyingDoorNativePlan(int line, string identifier,
            byte[] token, string name, byte action)
        {
            CanonicalLine = line;
            ObjectIdentifier = identifier;
            CollisionName = name;
            Action = action;
            objectToken = (byte[])token.Clone();
            name32 = new byte[32];
            Encoding.ASCII.GetBytes(name).CopyTo(name32, 0);
        }

        public byte[] SnapshotSourceObjectToken() => (byte[])objectToken.Clone();
        public byte[] SnapshotCollisionName32() => (byte[])name32.Clone();

        // Caller supplies the actual resolved door's u16 wire handle. Never
        // truncate/reinterpret the opaque script token as a network handle.
        public byte[] CreateNativeWire(ushort doorHandle) => new byte[] {
            0x09, 0x6C, (byte)doorHandle, (byte)(doorHandle >> 8), Action };

        public static bool TryBuild(KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestHoneyingDoorNativePlan plan)
        {
            plan = null;
            if (source == null || variables == null) return false;
            int door;
            string block;
            bool open;
            switch (source.CanonicalLine)
            {
                case 15: door = 1; block = "main"; open = false; break;
                case 16: door = 2; block = "main"; open = false; break;
                case 17: door = 3; block = "main"; open = false; break;
                case 61: door = 1; block = "FirstMobEleminate"; open = true; break;
                case 97: door = 2; block = "SecondMobEleminate"; open = true; break;
                case 130: door = 3; block = "ThirdMobEleminate"; open = true; break;
                default: return false;
            }
            string identifier = "Door" + door;
            string name = "CloseGate0" + door;
            var kind = open ? KingdomQuestHoneyingExternalKind.DoorOpen :
                KingdomQuestHoneyingExternalKind.DoorClose;
            string command = (open ? "dooropen " : "doorclose ") + identifier + " \"" + name + "\".";
            KingdomQuestPineTokenValue token;
            if (source.Kind != kind || source.TopLevelBlock != block || source.CommandText != command ||
                !variables.TryFind(identifier, out token) || token == null) return false;
            plan = new KingdomQuestHoneyingDoorNativePlan(source.CanonicalLine,
                identifier, token.SnapshotNativeBytes(), name, open ? (byte)1 : (byte)0);
            return true;
        }
    }

    public interface IKingdomQuestPineDoorObject
    {
        byte NativeObjectType { get; }
        // Execute native 0x00550770: state/name write, collision attempt,
        // then five-byte 0x6C09 broadcast. A collision miss is NOT failure.
        // False means that this live execution dependency is unavailable.
        bool TryApplyDoorAction(KingdomQuestHoneyingDoorNativePlan plan);
    }

    public interface IKingdomQuestPineDoorObjectResolver
    {
        // True + null is the native null lookup. False is an unavailable
        // resolver and must never be disguised as a successful native no-op.
        bool TryResolve(byte[] sourceObjectToken, out IKingdomQuestPineDoorObject obj);
        void ReportInvalidDoorObject();
    }

    /// <summary>
    /// Applies 0x00550770 to explicitly supplied live dependencies. Object
    /// creation, identity resolution and broadcast recipient selection remain
    /// with the owning map/object lifecycle; no implicit door is fabricated.
    /// </summary>
    public sealed class KingdomQuestPineDoorActionOwner : IKingdomQuestPineDoorObject
    {
        private readonly KingdomQuestMapCollision collision;
        private readonly ushort wireHandle;
        private readonly Action<byte, byte[]> storeObjectState;
        private readonly Action<byte[]> broadcast;
        public byte NativeObjectType => KingdomQuestHoneyingDoorNativePlan.NativeDoorObjectType;

        public KingdomQuestPineDoorActionOwner(KingdomQuestMapCollision collision,
            ushort wireHandle, Action<byte, byte[]> storeObjectState, Action<byte[]> broadcast)
        {
            this.collision = collision;
            this.wireHandle = wireHandle;
            this.storeObjectState = storeObjectState;
            this.broadcast = broadcast;
        }

        public bool TryApplyDoorAction(KingdomQuestHoneyingDoorNativePlan plan)
        {
            if (plan == null || collision == null || storeObjectState == null || broadcast == null)
                return false;
            storeObjectState(plan.Action, plan.SnapshotCollisionName32());
            // The native call's return at 0x0055082A is deliberately ignored.
            collision.TryDoorAction(plan.SnapshotCollisionName32(), plan.Action == 1);
            broadcast(plan.CreateNativeWire(wireHandle));
            return true;
        }
    }

    public static class KingdomQuestPineDoorActionRuntime
    {
        public static bool TryStep(KingdomQuestHoneyingDoorNativePlan plan,
            IKingdomQuestPineDoorObjectResolver resolver, out bool completed)
        {
            completed = false;
            IKingdomQuestPineDoorObject obj;
            if (plan == null || resolver == null ||
                !resolver.TryResolve(plan.SnapshotSourceObjectToken(), out obj)) return false;
            if (obj == null || obj.NativeObjectType != KingdomQuestHoneyingDoorNativePlan.NativeDoorObjectType)
            {
                resolver.ReportInvalidDoorObject();
                completed = true;
                return true;
            }
            if (!obj.TryApplyDoorAction(plan)) return false;
            completed = true;
            return true;
        }
    }
}
