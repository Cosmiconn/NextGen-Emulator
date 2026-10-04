using System;
using System.Globalization;
using System.IO;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// The three original Honeying doorbuild statements. The Normal operand
    /// is evaluated by os_ObjectRegen but NOT forwarded on its type-7 branch.
    /// Nonzero source coordinates do not enter the random-position branch.
    /// </summary>
    public sealed class KingdomQuestHoneyingDoorBuildNativePlan
    {
        public const uint NativeStepAddress = 0x004F9440;
        public const uint NativeObjectRegenAddress = 0x004F8C40;
        public const uint NativeBuildAddress = 0x0043F7B0;
        public const int NativeBuildVtableOffset = 0x6DC;
        public const byte ObjectType = 7;
        public const string MobIndex = "KQ_SlimeGate";
        public const ushort SourceMobId = 1091;
        public const int Scale = 1000;
        public const ulong RegistrationNumber = 0;
        public const string EvaluatedButUnusedAction = "Normal";
        public int CanonicalLine { get; }
        public string DestinationIdentifier { get; }
        public int X { get; }
        public int Y { get; }
        public int DirectionDegrees { get; }

        private KingdomQuestHoneyingDoorBuildNativePlan(int line, int x, int y, int direction)
        {
            CanonicalLine = line;
            DestinationIdentifier = "Door" + (line - 11);
            X = x;
            Y = y;
            DirectionDegrees = direction;
        }

        public static bool TryBuild(KingdomQuestHoneyingExternalPlan source,
            out KingdomQuestHoneyingDoorBuildNativePlan plan)
        {
            plan = null;
            if (source == null || source.Kind != KingdomQuestHoneyingExternalKind.DoorBuild ||
                source.TopLevelBlock != "main") return false;
            KingdomQuestHoneyingExternalSourceSite original;
            if (!KingdomQuestHoneyingSourceFlow.TryResolve("KQ/Honeying", source.CanonicalLine,
                source.CommandText, out original)) return false;
            switch (source.CanonicalLine)
            {
                case 12: plan = new KingdomQuestHoneyingDoorBuildNativePlan(12, 9860, 6094, 272); break;
                case 13: plan = new KingdomQuestHoneyingDoorBuildNativePlan(13, 6692, 3944, 6); break;
                case 14: plan = new KingdomQuestHoneyingDoorBuildNativePlan(14, 5894, 6098, 88); break;
                default: return false;
            }
            return true;
        }
    }

    public sealed class KingdomQuestPineDoorBuildRequest
    {
        private readonly byte[] mapName12;
        public KingdomQuestHoneyingDoorBuildNativePlan Source { get; }
        public ushort Handle { get; }
        public ushort MobId { get; }
        public byte[] SnapshotMapName12() => (byte[])mapName12.Clone();

        internal KingdomQuestPineDoorBuildRequest(KingdomQuestHoneyingDoorBuildNativePlan source,
            byte[] mapName12, ushort handle, ushort mobId)
        {
            Source = source;
            this.mapName12 = (byte[])mapName12.Clone();
            Handle = handle;
            MobId = mobId;
        }

        public byte[] CreateInitialBriefWire() => KingdomQuestPineDoorBrief.CreateNativeWire(
            Handle, MobId, Source.X, Source.Y, Source.DirectionDegrees, 0, new byte[32],
            KingdomQuestHoneyingDoorBuildNativePlan.Scale);
    }

    public enum KingdomQuestPineDoorBuildFailure
    {
        Allocation, Destination, MobIndex, MapName, Marking
    }

    public interface IKingdomQuestPineDoorBuildOwner
    {
        // All native services must exist before this returns true. Absence
        // is not the native allocation failure or native map-lookup miss.
        bool IsAvailable { get; }
        object Allocate(byte nativeObjectType, out ushort handle);
        // Implement Identify::i_GetVariable/ps_GetVariable, including its
        // native shared fallback token on an unknown variable. Do not push a
        // new variable or synthesize a null result for an ordinary name miss.
        KingdomQuestPineTokenValue ResolveDestination(
            KingdomQuestPineVariableStack variables, string identifier);
        ushort FindMobId(string index); // native lookup miss = 0xFFFF
        byte[] GetCurrentMapName12(); // null = Theater::t_MapNameServer failure
        // Performs so_door_Build, marking and BuildComplete before returning.
        // Return the original FM_MarkingError: zero is success.
        int Build(object allocated, KingdomQuestPineDoorBuildRequest request);
        bool Free(ushort handle, int removeWhen, int reason);
        void ReportFailure(KingdomQuestPineDoorBuildFailure failure, int nativeError);
    }

    public static class KingdomQuestPineDoorBuildRuntime
    {
        public static bool TryStep(KingdomQuestHoneyingDoorBuildNativePlan plan,
            KingdomQuestPineVariableStack variables, IKingdomQuestPineDoorBuildOwner owner,
            out bool completed)
        {
            completed = false;
            if (plan == null || variables == null || owner == null || !owner.IsAvailable) return false;
            ushort handle;
            object allocated = owner.Allocate(KingdomQuestHoneyingDoorBuildNativePlan.ObjectType, out handle);
            if (allocated == null)
            {
                owner.ReportFailure(KingdomQuestPineDoorBuildFailure.Allocation, 0);
                completed = true;
                return true;
            }

            bool built = false;
            try
            {
                var destination = owner.ResolveDestination(variables, plan.DestinationIdentifier);
                if (destination == null)
                    owner.ReportFailure(KingdomQuestPineDoorBuildFailure.Destination, 0);
                else
                {
                    ushort mobId = owner.FindMobId(KingdomQuestHoneyingDoorBuildNativePlan.MobIndex);
                    if (mobId == ushort.MaxValue)
                        owner.ReportFailure(KingdomQuestPineDoorBuildFailure.MobIndex, 0);
                    else
                    {
                        byte[] mapName = owner.GetCurrentMapName12();
                        if (mapName == null)
                            owner.ReportFailure(KingdomQuestPineDoorBuildFailure.MapName, 0);
                        else
                        {
                            if (mapName.Length != 12) return false; // invalid owner contract
                            var request = new KingdomQuestPineDoorBuildRequest(plan, mapName, handle, mobId);
                            int error = owner.Build(allocated, request);
                            if (error != 0)
                            {
                                owner.ReportFailure(KingdomQuestPineDoorBuildFailure.Marking, error);
                                owner.Free(handle, 0, 22); // os_ObjectRegen failure cleanup
                            }
                            else
                            {
                                // pst_Initialize followed by pst_MergeNumber("%d").
                                built = destination.TrySetAscii(handle.ToString(CultureInfo.InvariantCulture));
                                if (!built) return false;
                            }
                        }
                    }
                }
                completed = true;
                return true;
            }
            finally
            {
                // sa_Step performs this even after the inner reason-22 call.
                // It ignores the Free return; the owner must preserve manager
                // lookup semantics rather than double-dispose an object.
                if (!built) owner.Free(handle, 0, 23);
            }
        }
    }

    /// <summary>
    /// BriefInformationDoor 0x00549260: opcode 0x1C0F + 48-byte payload.
    /// Field order is fixed by so_door_Build and the copy at 0x00555AF0.
    /// These are native bytes before transport framing/encryption.
    /// </summary>
    public static class KingdomQuestPineDoorBrief
    {
        public const ushort Opcode = 0x1C0F;
        public const int NativeWireSize = 50;

        // ShineDoor::so_RemakeHandle 0x00555BA0. This translates a native
        // pool index only; it does not allocate a slot or consume a MapObjectID.
        public static ushort RemakeNativeHandle(ushort poolIndex) =>
            KingdomQuestPineNativeObjectHandles.Encode(7, poolIndex);

        public static byte EncodeDirection(int degrees)
        {
            int half = degrees / 2;
            if (half < 0) half += 180;
            return unchecked((byte)half);
        }

        public static byte[] CreateNativeWire(ushort handle, ushort mobId, int x, int y,
            int degrees, byte action, byte[] name32, int scale)
        {
            if (name32 == null || name32.Length != 32) throw new ArgumentException("Native door Name32 required.", nameof(name32));
            using (var stream = new MemoryStream(NativeWireSize))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Opcode);
                writer.Write(handle);
                writer.Write(mobId);
                writer.Write(x);
                writer.Write(y);
                writer.Write(EncodeDirection(degrees));
                writer.Write(action);
                writer.Write(name32);
                writer.Write(unchecked((ushort)scale));
                return stream.ToArray();
            }
        }
    }
}
