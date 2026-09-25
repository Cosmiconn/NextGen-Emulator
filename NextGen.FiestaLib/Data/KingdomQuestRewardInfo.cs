using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Networking;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Original Zone.pdb SHINE_REWARD_TYPE values. The native GURAD spelling
    /// is normalized only in the managed member name; numeric values are exact.
    /// </summary>
    public enum ShineRewardType : byte
    {
        None = 0,
        Item = 1,
        Experience = 2,
        Money = 3,
        Honor = 4,
        HpSoulStone = 5,
        SpSoulStone = 6,
        GuardSoulStone = 7,
        AttackSoulStone = 8,
        ClassChange = 9,
        Pet = 10,
        Max = 11,
    }

    /// <summary>
    /// Source-correlation result for a ShineReward ITEM Argument.
    /// Every native ITEM branch still enters TreasureChestMaker. This enum
    /// only records whether the raw Argument also happens to be an exact
    /// ItemInfo.inxname in the supplied source snapshot; it never turns an
    /// opaque TreasureChest token into an item alias.
    /// </summary>
    public enum ShineRewardItemArgumentKind : byte
    {
        NotItemReward = 0,
        ExactItemInfoName = 1,
        OpaqueTreasureChestArgument = 2,
    }

    /// <summary>
    /// Native 128-byte KINGDOM_QUEST_REW projection used by
    /// ShinePlayer::sp_KQReward. Raw KingdomQuestRew.shn columns are signed
    /// Int16; the executable consumes their exact 16-bit patterns as u16.
    /// </summary>
    public sealed class KingdomQuestNativeRewardInfo
    {
        public const int EntryCount = 15;
        public const int NativeStructSize = 128;

        public string KQBoxItemIndex { get; private set; }
        public IReadOnlyList<ushort> RewardHandles { get; private set; }
        public IReadOnlyList<ushort> RewardRates { get; private set; }

        private KingdomQuestNativeRewardInfo(
            string kqBoxItemIndex,
            ushort[] rewardHandles,
            ushort[] rewardRates)
        {
            KQBoxItemIndex = kqBoxItemIndex ?? string.Empty;
            RewardHandles = Array.AsReadOnly(rewardHandles);
            RewardRates = Array.AsReadOnly(rewardRates);
        }

        public static bool TryCreate(
            string kqBoxItemIndex,
            IReadOnlyList<short> rewardColumns,
            IReadOnlyList<short> rewardRateColumns,
            out KingdomQuestNativeRewardInfo value)
        {
            value = null;
            if (rewardColumns == null || rewardRateColumns == null ||
                rewardColumns.Count != EntryCount ||
                rewardRateColumns.Count != EntryCount)
                return false;

            var handles = new ushort[EntryCount];
            var rates = new ushort[EntryCount];
            for (int i = 0; i < EntryCount; i++)
            {
                handles[i] = unchecked((ushort)rewardColumns[i]);
                rates[i] = unchecked((ushort)rewardRateColumns[i]);
            }

            value = new KingdomQuestNativeRewardInfo(
                kqBoxItemIndex, handles, rates);
            return true;
        }

        /// <summary>
        /// Reproduces only the recovered per-slot dice predicate. The caller
        /// supplies exactly 15 native 0..999 samples because RNG ownership is
        /// Zone-global in the original runtime and must not be invented here.
        /// This does not resolve ShineReward handles or grant rewards.
        /// </summary>
        public IReadOnlyList<KingdomQuestRewardDiceEntry> EvaluateDice(
            IReadOnlyList<ushort> randomSamples)
        {
            if (randomSamples == null ||
                randomSamples.Count != EntryCount)
                throw new ArgumentException(
                    "Exactly 15 native reward random samples are required.",
                    "randomSamples");

            var result = new List<KingdomQuestRewardDiceEntry>(EntryCount);
            for (int i = 0; i < EntryCount; i++)
            {
                if (randomSamples[i] >= 1000)
                    throw new ArgumentOutOfRangeException(
                        "randomSamples",
                        "Native reward samples must be in range 0..999.");

                result.Add(new KingdomQuestRewardDiceEntry(
                    i,
                    RewardHandles[i],
                    RewardRates[i],
                    randomSamples[i],
                    randomSamples[i] < RewardRates[i]));
            }

            return result.AsReadOnly();
        }
    }

    public sealed class KingdomQuestRewardDiceEntry
    {
        public int Slot { get; private set; }
        public ushort RewardHandle { get; private set; }
        public ushort RewardRate { get; private set; }
        public ushort RandomSample { get; private set; }
        public bool Selected { get; private set; }

        internal KingdomQuestRewardDiceEntry(
            int slot,
            ushort rewardHandle,
            ushort rewardRate,
            ushort randomSample,
            bool selected)
        {
            Slot = slot;
            RewardHandle = rewardHandle;
            RewardRate = rewardRate;
            RandomSample = randomSample;
            Selected = selected;
        }
    }

    /// <summary>
    /// Source/native ShineReward record shape correlated from ShineReward.shn
    /// and Zone.pdb. Runtime loading is deliberately separate.
    /// </summary>
    public sealed class ShineRewardNativeInfo
    {
        public const int ShineRewardNativeStructSize = 66;
        public const int UnknownShortCount = 9;

        public ushort RewardHandle { get; set; }
        public ShineRewardType RewardType { get; set; }
        public string Argument { get; set; } = string.Empty;
        public uint Quantity { get; set; }
        public short Upgrade { get; set; }
        public IReadOnlyList<short> UnknownShorts { get; set; }
        public ushort OptionDegree { get; set; }
        public uint TitleDegree { get; set; }

        /// <summary>
        /// Cross-correlates an ITEM Argument against source-backed ItemInfo
        /// names with an ordinal comparison. A miss is deliberately returned
        /// as an opaque TreasureChest argument: original sp_KQReward passes
        /// every ITEM reward through TreasureChestMaker, and many valid KQ
        /// source arguments are not ItemInfo.inxname values.
        ///
        /// This helper performs no item generation, random selection,
        /// inventory mutation or persistence.
        /// </summary>
        public ShineRewardItemArgumentKind ClassifyItemArgument(
            IEnumerable<ItemInfo> itemInfos,
            out ItemInfo exactItem)
        {
            exactItem = null;
            if (RewardType != ShineRewardType.Item)
                return ShineRewardItemArgumentKind.NotItemReward;

            if (itemInfos != null)
            {
                string argument = Argument ?? string.Empty;
                foreach (ItemInfo candidate in itemInfos)
                {
                    if (candidate != null &&
                        string.Equals(
                            candidate.InxName, argument,
                            StringComparison.Ordinal))
                    {
                        exactItem = candidate;
                        return ShineRewardItemArgumentKind.ExactItemInfoName;
                    }
                }
            }

            return ShineRewardItemArgumentKind.OpaqueTreasureChestArgument;
        }
    }

    /// <summary>
    /// PDB/EXE-proven KQ reward protocol sizes. The item-create request has a
    /// variable tail, so these constants do not fabricate a complete serializer.
    /// </summary>
    public static class KingdomQuestRewardProtocolConstants
    {
        public const ushort RewardRequestOpCode = 0x5815;
        public const ushort RewardSuccessAckOpCode = 0x5816;
        public const ushort RewardFailAckOpCode = 0x5817;

        public const int NetPacketZoneHeaderSize = 6;
        public const int ItemCreateRequestBaseSize = 23;
        public const int RewardRequestPayloadBaseSize = 35;
        public const int RewardSuccessAckSize = 8;
        public const int RewardFailAckSize = 10;
    }

    /// <summary>
    /// Original PROTO_NC_KQ_REWARDSUC_ACK payload. GameDBSession validates
    /// ClientHandle -> player and CharacterNumber before forwarding LockIndex
    /// to the player's item-store transaction path.
    /// </summary>
    public sealed class KingdomQuestRewardSuccessAckInfo
    {
        public const int WireSize = 8;

        public ushort ClientHandle { get; private set; }
        public uint CharacterNumber { get; private set; }
        public ushort LockIndex { get; private set; }

        public void Write(Packet packet)
        {
            if (packet == null) throw new ArgumentNullException("packet");
            packet.WriteUShort(ClientHandle);
            packet.WriteUInt(CharacterNumber);
            packet.WriteUShort(LockIndex);
        }

        public static bool TryRead(
            Packet packet, out KingdomQuestRewardSuccessAckInfo value)
        {
            value = null;
            if (packet == null || packet.Remaining < WireSize)
                return false;

            ushort clientHandle;
            uint characterNumber;
            ushort lockIndex;
            if (!packet.TryReadUShort(out clientHandle) ||
                !packet.TryReadUInt(out characterNumber) ||
                !packet.TryReadUShort(out lockIndex))
                return false;

            value = new KingdomQuestRewardSuccessAckInfo
            {
                ClientHandle = clientHandle,
                CharacterNumber = characterNumber,
                LockIndex = lockIndex,
            };
            return true;
        }
    }

    /// <summary>
    /// Original PROTO_NC_KQ_REWARDFAIL_ACK payload. The Error field is
    /// preserved byte-for-byte, although the recovered Zone
    /// GameDBSession::gds_NC_KQ_REWARDFAIL_ACK handler does not read it; after
    /// validating player identity it forwards only LockIndex to the distinct
    /// item-store failure/rollback transaction path.
    /// </summary>
    public sealed class KingdomQuestRewardFailAckInfo
    {
        public const int WireSize = 10;

        public ushort ClientHandle { get; private set; }
        public uint CharacterNumber { get; private set; }
        public ushort LockIndex { get; private set; }
        public ushort Error { get; private set; }

        public void Write(Packet packet)
        {
            if (packet == null) throw new ArgumentNullException("packet");
            packet.WriteUShort(ClientHandle);
            packet.WriteUInt(CharacterNumber);
            packet.WriteUShort(LockIndex);
            packet.WriteUShort(Error);
        }

        public static bool TryRead(
            Packet packet, out KingdomQuestRewardFailAckInfo value)
        {
            value = null;
            if (packet == null || packet.Remaining < WireSize)
                return false;

            ushort clientHandle;
            uint characterNumber;
            ushort lockIndex;
            ushort error;
            if (!packet.TryReadUShort(out clientHandle) ||
                !packet.TryReadUInt(out characterNumber) ||
                !packet.TryReadUShort(out lockIndex) ||
                !packet.TryReadUShort(out error))
                return false;

            value = new KingdomQuestRewardFailAckInfo
            {
                ClientHandle = clientHandle,
                CharacterNumber = characterNumber,
                LockIndex = lockIndex,
                Error = error,
            };
            return true;
        }
    }
}
