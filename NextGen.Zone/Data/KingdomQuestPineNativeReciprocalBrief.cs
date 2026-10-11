using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineReciprocalEvent
    { InvalidModeFlags, CharacterPacketLengthRejected, AbstateObjectCapacity, AbstateEntryCapacity, AbstatePacketLengthRejected }

    public interface IKingdomQuestPineNativeReciprocalServices
    {
        bool IsAvailable { get; }
        byte ModeFlags(IKingdomQuestPineNativeAxialObject obj);
        ushort NativeHandle(IKingdomQuestPineNativeAxialObject obj);
        // These methods must execute the real +0x4C8 / +0x4C4 / +0xA94
        // virtuals. Unknown character/mobile state must not be synthesized.
        bool TrySendMyBrief(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject recipient);
        bool TryFillCharacterBrief235(IKingdomQuestPineNativeAxialObject obj, Span<byte> destination);
        bool TryFillAbstates(IKingdomQuestPineNativeAxialObject obj, Span<byte> entries336, ref byte count);
        void Report(KingdomQuestPineReciprocalEvent reason, IKingdomQuestPineNativeAxialObject obj);
    }

    /// <summary>
    /// Character exchange 0x0043FD00 and abstate exchange 0x00440510.
    /// One shared owner on the native Zone thread, including the singleton
    /// immediate abstate packet, two 28-object queues and character batch.
    /// This closes exchange algorithms, not the live character/abstate owners.
    /// </summary>
    public sealed class KingdomQuestPineNativeReciprocalBrief
    {
        private sealed class Packet
        {
            internal readonly byte[] Wire;
            internal int Length;
            internal Packet(int capacity, ushort opcode)
            { Wire = new byte[capacity]; Wire[0] = (byte)opcode; Wire[1] = (byte)(opcode >> 8); }
            internal bool SetLength(int length)
            { if (length >= Wire.Length) return false; Length = length; return true; }
            internal byte[] Snapshot() { var copy = new byte[Length]; Array.Copy(Wire, copy, Length); return copy; }
        }
        private sealed class Abstates
        {
            internal readonly Packet[] Packets = new Packet[28];
            internal int Count, CurrentIndex;
            internal Abstates()
            {
                for (int i = 0; i < 28; i++)
                { Packets[i] = new Packet(0x155, 0x1C19); Packets[i].Wire[2] = Packets[i].Wire[3] = 255; }
            }
        }
        private readonly IKingdomQuestPineNativeBriefServices transport;
        private readonly IKingdomQuestPineNativeReciprocalServices services;
        private readonly KingdomQuestPineNativeSubLayer layers;
        // Initialized .data at 0x74E1A0 / 0x74E030. The abstate arrays live at
        // 0x8B8010 (character and mover) and 0x8BA710 (mob).
        private readonly Packet characters = new Packet(0x1A9F, 0x1C07);
        private readonly Packet immediateAbstates = new Packet(0x155, 0x1C19);
        private readonly Abstates characterAbstates = new Abstates(), mobAbstates = new Abstates();
        public byte CharacterCount => characters.Wire[2];
        public int CharacterPacketLength => characters.Length;
        public int ImmediateAbstatePacketLength => immediateAbstates.Length;
        public int CharacterAbstateObjectCount => characterAbstates.Count;
        public int MobAbstateObjectCount => mobAbstates.Count;
        public int CharacterAbstateCurrentIndex => characterAbstates.CurrentIndex;
        public bool IsAvailable => transport.IsAvailable && services.IsAvailable;

        public KingdomQuestPineNativeReciprocalBrief(IKingdomQuestPineNativeBriefServices transport,
            IKingdomQuestPineNativeReciprocalServices services, KingdomQuestPineNativeSubLayer layers)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.layers = layers ?? throw new ArgumentNullException(nameof(layers));
        }
        private byte Type(IKingdomQuestPineNativeAxialObject obj) => obj is KingdomQuestPineNativeSceneObject scene
            ? (byte)scene.NativeObjectType : obj is KingdomQuestPineNativeAxialFlag ? (byte)0 : transport.ObjectType(obj);
        private byte Flags(IKingdomQuestPineNativeAxialObject obj) => obj is KingdomQuestPineNativeSceneObject scene
            ? scene.ModeFlags : services.ModeFlags(obj);
        private ushort Handle(IKingdomQuestPineNativeAxialObject obj) => obj is KingdomQuestPineNativeSceneObject scene
            ? scene.NativeHandle : services.NativeHandle(obj);
        private static bool SameMode(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate)
        {
            uint candidateMode = candidate.Location.Mode, sourceMode = source.Location.Mode;
            return sourceMode == candidateMode || sourceMode == uint.MaxValue || candidateMode == uint.MaxValue;
        }
        private bool TryDirection(IKingdomQuestPineNativeAxialObject sender, IKingdomQuestPineNativeAxialObject receiver, out bool allowed)
        {
            allowed = false;
            if (!layers.TrySendMyBrief(Flags(sender), Flags(receiver), out byte result))
            { services.Report(KingdomQuestPineReciprocalEvent.InvalidModeFlags, sender); return false; }
            allowed = result != 0; return true;
        }
        private bool SendMyBrief(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject recipient)
        {
            // Concrete +0x4C8 targets 0x555B60 / 0x555280 have no recipient
            // type/mobile/sight test. Forward the complete individual brief.
            if (source is KingdomQuestPineNativeSceneObject scene)
            { transport.SendPacket(recipient, scene.SnapshotBriefWire()); return true; }
            return services.TrySendMyBrief(source, recipient);
        }
        public void InitializeCharacters() => characters.Wire[2] = 0;
        // bixa_Init 0x43C6B0 is a NO-OP once the current pointer is non-null.
        // Both constructed arrays already have that pointer, even with count>0.
        public void InitializeAbstates() { }

        public bool TrySendCharacters(IKingdomQuestPineNativeAxialObject source)
        {
            if (source == null || !IsAvailable) return false;
            if (CharacterCount == 0 || Type(source) != 2 || transport.MobileMode(source) == 0) return true;
            if (!characters.SetLength(3 + 235 * CharacterCount))
            { services.Report(KingdomQuestPineReciprocalEvent.CharacterPacketLengthRejected, source); return true; }
            transport.SendPacket(source, characters.Snapshot()); return true; // count retained
        }
        public bool TryExchangeCharacter(IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate)
        {
            if (source == null || candidate == null || !IsAvailable) return false;
            if (!SameMode(source, candidate)) return true;
            if (!TryDirection(source, candidate, out bool forward)) return false;
            if (forward && !SendMyBrief(source, candidate)) return false;
            // Re-read mode flags AFTER the immediate send callback. Native
            // mode equality is not repeated, and the reverse rule is distinct.
            if (!TryDirection(candidate, source, out bool reverse)) return false;
            if (!reverse || Type(source) != 2) return true;
            if (CharacterCount >= 28)
            { if (!TrySendCharacters(source)) return false; characters.Wire[2] = 0; }
            if (!services.TryFillCharacterBrief235(candidate, characters.Wire.AsSpan(3 + CharacterCount * 235, 235))) return false;
            characters.Wire[2]++; return true;
        }

        private bool FillAbstates(Packet packet, IKingdomQuestPineNativeAxialObject obj)
        {
            ushort handle = Handle(obj); packet.Wire[2] = (byte)handle; packet.Wire[3] = (byte)(handle >> 8);
            packet.Wire[4] = 0;
            // Effect +0xA94 is the literal ret 8 at 0x509900. Door uses the
            // full ShineMobileObject::so_FillBriefABState, NEVER a guessed zero.
            if (!(obj is KingdomQuestPineNativeEffectObject) &&
                !services.TryFillAbstates(obj, packet.Wire.AsSpan(5, 336), ref packet.Wire[4])) return false;
            if (packet.Wire[4] > 28)
            { services.Report(KingdomQuestPineReciprocalEvent.AbstateEntryCapacity, obj); return false; }
            return true;
        }
        public bool TryExchangeAbstates(bool mob, IKingdomQuestPineNativeAxialObject source, IKingdomQuestPineNativeAxialObject candidate)
        {
            if (source == null || candidate == null || !IsAvailable) return false;
            if (!SameMode(source, candidate)) return true;
            // Unlike character exchange, reverse visibility gates BOTH paths.
            if (!TryDirection(candidate, source, out bool reverse)) return false;
            if (!reverse) return true;
            if (!TryDirection(source, candidate, out bool forward)) return false;
            if (forward)
            {
                if (!FillAbstates(immediateAbstates, source)) return false;
                // 0x43CC00 ignores pp_SetPacketLen failure: count 28 requests
                // 341 == capacity, retaining the PREVIOUS length, even zero.
                immediateAbstates.SetLength(5 + 12 * immediateAbstates.Wire[4]);
                if (immediateAbstates.Wire[4] != 0) transport.SendPacket(candidate, immediateAbstates.Snapshot());
            }
            if (Type(source) != 2) return true;
            var queue = mob ? mobAbstates : characterAbstates;
            if (queue.Count > 28) // native uses >, NOT >=; do not repair it
            { if (!SendAbstates(queue, source)) return false; queue.Count = 0; }
            if (queue.Count < 0 || queue.Count >= 28)
            {
                // Source attempts slot 28 past its 28 constructed packets.
                // Preserve earlier effects, expose unavailable native memory.
                services.Report(KingdomQuestPineReciprocalEvent.AbstateObjectCapacity, candidate); return false;
            }
            queue.CurrentIndex = queue.Count;
            if (!FillAbstates(queue.Packets[queue.CurrentIndex], candidate)) return false;
            queue.Count++; return true; // read shared count AFTER fill callback
        }
        public bool TrySendAbstates(bool mob, IKingdomQuestPineNativeAxialObject source)
        {
            if (source == null || !IsAvailable) return false;
            return SendAbstates(mob ? mobAbstates : characterAbstates, source);
        }
        private bool SendAbstates(Abstates queue, IKingdomQuestPineNativeAxialObject source)
        {
            if (queue.Count <= 0 || queue.Count > 28 || Type(source) != 2 || transport.MobileMode(source) == 0) return true;
            for (int index = 0; index < queue.Count; index++) // shared count re-read after each send
            {
                if (index >= 28)
                { services.Report(KingdomQuestPineReciprocalEvent.AbstateObjectCapacity, source); return false; }
                queue.CurrentIndex = index;
                var packet = queue.Packets[index]; int count = packet.Wire[4];
                if (count == 0) continue;
                if (!packet.SetLength(5 + count * 12))
                { services.Report(KingdomQuestPineReciprocalEvent.AbstatePacketLengthRejected, source); continue; }
                transport.SendPacket(source, packet.Snapshot());
            }
            queue.Count = 0; queue.CurrentIndex = 0; return true;
        }
    }
}
