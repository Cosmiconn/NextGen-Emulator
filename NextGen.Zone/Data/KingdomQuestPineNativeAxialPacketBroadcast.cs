using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Shared Zone ProtocolPacket at 0x0084D908: pointer 0x0084D914,
    /// capacity 0x2000, initial length/storage zero in the original .data.
    /// All users must share this owner; nested sends may overwrite its bytes
    /// and length. This is distinct from the BriefInfo batch accumulators.
    /// </summary>
    public sealed class KingdomQuestPineNativeSharedPacket
    {
        public const int Capacity = 0x2000;
        private readonly byte[] data = new byte[Capacity];
        public int Length { get; private set; }
        public void Write(int offset, ReadOnlySpan<byte> bytes) => bytes.CopyTo(data.AsSpan(offset, bytes.Length));
        public bool TrySetLength(int length)
        {
            // Negative native lengths expose invalid memory, not a packet.
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (length >= Capacity) return false; // 0x004C8CE0, retains old length
            Length = length; return true;
        }
        public byte[] Snapshot() => data.AsSpan(0, Length).ToArray();
    }

    public enum KingdomQuestPineBroadcastEvent { PacketLengthRejected, InvalidModeFlags }

    public interface IKingdomQuestPineNativeBroadcastServices
    {
        bool IsAvailable { get; }
        byte ObjectType(IKingdomQuestPineNativeAxialObject obj);
        byte MobileMode(IKingdomQuestPineNativeAxialObject obj);
        byte ModeFlags(IKingdomQuestPineNativeAxialObject obj);
        byte IsEmpty(IKingdomQuestPineNativeAxialObject obj); // +0x300
        byte SoloingMode(IKingdomQuestPineNativeAxialObject obj); // +0x79C
        uint MoverOwnerCharacterNumber(IKingdomQuestPineNativeAxialObject obj); // +0xCA8
        uint CharacterNumber(IKingdomQuestPineNativeAxialObject obj); // +0x344
        // Execute +0x2FC GetGameSocketStream and its +0x0C Send. The packet
        // pointer is shared, so snapshot only at the actual transport boundary.
        bool TrySendPacket(IKingdomQuestPineNativeAxialObject recipient, KingdomQuestPineNativeSharedPacket packet);
        void Report(KingdomQuestPineBroadcastEvent reason, IKingdomQuestPineNativeAxialObject obj);
    }

    /// <summary>AxialListPacketBroadcast constructor 0x00428A40 / ali_Work 0x00428B20.</summary>
    public sealed class KingdomQuestPineNativeAxialPacketBroadcast : IKingdomQuestPineNativeAxialIterator
    {
        private readonly KingdomQuestPineNativeSharedPacket packet;
        private readonly KingdomQuestPineNativeSubLayer layers;
        private readonly IKingdomQuestPineNativeBroadcastServices services;
        // Iterator +8 is retained separately from the shared packet's length.
        public int RequestedLength { get; }
        public KingdomQuestPineNativeAxialPacketBroadcast(KingdomQuestPineNativeSharedPacket packet, int length,
            KingdomQuestPineNativeSubLayer layers, IKingdomQuestPineNativeBroadcastServices services)
        {
            this.packet = packet ?? throw new ArgumentNullException(nameof(packet));
            this.layers = layers ?? throw new ArgumentNullException(nameof(layers));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            if (packet.TrySetLength(length)) RequestedLength = length;
            else services.Report(KingdomQuestPineBroadcastEvent.PacketLengthRejected, null);
        }
        private byte Type(IKingdomQuestPineNativeAxialObject obj) => obj is KingdomQuestPineNativeSceneObject scene
            ? (byte)scene.NativeObjectType : obj is KingdomQuestPineNativeAxialFlag ? (byte)0 : services.ObjectType(obj);
        private byte Flags(IKingdomQuestPineNativeAxialObject obj) => obj is KingdomQuestPineNativeSceneObject scene
            ? scene.ModeFlags : services.ModeFlags(obj);
        public bool TryWork(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate,
            uint unusedDistanceSquared, out bool nativeResult)
        {
            nativeResult = false;
            if (RequestedLength == 0) return true; // native false, before any virtual
            if (source == null || candidate == null || !services.IsAvailable) return false;
            nativeResult = true;
            if (Type(candidate) != 2 || services.MobileMode(candidate) == 0) return true;
            uint sourceMode = source.Location.Mode, candidateMode = candidate.Location.Mode;
            if (sourceMode != candidateMode && sourceMode != uint.MaxValue && candidateMode != uint.MaxValue) return true;
            if (!layers.TrySendMyBrief(Flags(source), Flags(candidate), out byte canSee))
            { services.Report(KingdomQuestPineBroadcastEvent.InvalidModeFlags, source); nativeResult = false; return false; }
            if (canSee == 0 || services.IsEmpty(candidate) != 0) return true;
            if (services.SoloingMode(candidate) != 0)
            {
                if (Type(source) == 11)
                {
                    // Two independent reads are in the original; callbacks can
                    // change the owner between the sentinel and equality tests.
                    if (services.MoverOwnerCharacterNumber(source) == uint.MaxValue) return true;
                    uint owner = services.MoverOwnerCharacterNumber(source);
                    if (owner != services.CharacterNumber(candidate)) return true;
                }
                else if (!ReferenceEquals(candidate, source)) return true;
            }
            if (!services.TrySendPacket(candidate, packet)) { nativeResult = false; return false; }
            return true; // native ignores distance and the transport's own result
        }
    }
}
