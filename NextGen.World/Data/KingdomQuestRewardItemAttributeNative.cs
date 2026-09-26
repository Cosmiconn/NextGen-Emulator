using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    public enum KingdomQuestRewardItemAttributeKind : byte
    {
        ByteLot = 1,
        QuestLot = 2,
        Amulet = 3,
        Weapon = 4,
        Armor = 5,
        Shield = 6,
        Boot = 7,
        Decoration = 8,
        SkillScrollNoOp = 9,
        UpSourceByteLot = 10,
    }

    /// <summary>
    /// Mutation-free projection of the reward-specific ItemAttributeClass
    /// virtual used by TreasureChestMaker for KQ ITEM rewards.
    ///
    /// This represents only bytes proven by the supplied Zone.exe/PDB. Normal
    /// per-class itemcreate initialization, registration numbers, later
    /// RandomOptionTable filling and the weapon socket-rate lookup remain
    /// separate native stages.
    /// </summary>
    public sealed class KingdomQuestRewardItemAttributePlan
    {
        public byte NativeItemClass { get; private set; }
        public KingdomQuestRewardItemAttributeKind Kind { get; private set; }
        public uint RewardItemCreateAddress { get; private set; }

        public int QuantityWriteBytes { get; private set; }
        public uint QuantityWriteValue { get; private set; }

        public bool ClearsByte0C { get; private set; }
        public bool UsesWeightedGrade { get; private set; }
        public byte WeightedGrade { get; private set; }

        public int OptionCardStorageOffset { get; private set; }
        public ushort OptionDegree { get; private set; }

        public bool ClearsDecorationPayload { get; private set; }
        public bool RewardOverloadIsNoOp { get; private set; }
        public bool RequiresWeaponSocketRate { get; private set; }

        internal KingdomQuestRewardItemAttributePlan(
            byte nativeItemClass,
            KingdomQuestRewardItemAttributeKind kind,
            uint rewardItemCreateAddress,
            int quantityWriteBytes,
            uint quantityWriteValue,
            bool clearsByte0C,
            bool usesWeightedGrade,
            byte weightedGrade,
            int optionCardStorageOffset,
            ushort optionDegree,
            bool clearsDecorationPayload,
            bool rewardOverloadIsNoOp,
            bool requiresWeaponSocketRate)
        {
            NativeItemClass = nativeItemClass;
            Kind = kind;
            RewardItemCreateAddress = rewardItemCreateAddress;
            QuantityWriteBytes = quantityWriteBytes;
            QuantityWriteValue = quantityWriteValue;
            ClearsByte0C = clearsByte0C;
            UsesWeightedGrade = usesWeightedGrade;
            WeightedGrade = weightedGrade;
            OptionCardStorageOffset = optionCardStorageOffset;
            OptionDegree = optionDegree;
            ClearsDecorationPayload = clearsDecorationPayload;
            RewardOverloadIsNoOp = rewardOverloadIsNoOp;
            RequiresWeaponSocketRate = requiresWeaponSocketRate;
        }
    }

    /// <summary>
    /// Exact KQ-reachable reward ItemAttributeClass projection.
    ///
    /// Original ItemInfo/ItemInfoServer + KQ reward correlation reaches only
    /// native item classes 0,3,4,5,6,7,8,10,11,14 (884 unique candidate item
    /// IDs). Their reward-overload vtable targets were recovered from Zone.pdb.
    ///
    /// Weapon/Armor/Shield/Boot call well512_GetRandom(1000) and select one of
    /// ten u16 weights: ShineReward.Upgrade followed by Undefined0..8. Native
    /// subtracts each rejected weight and stores the first matching index
    /// 0..9 at ITI +0x0A; exhausting all ten stores zero.
    ///
    /// Every one of the 247 used ITEM ShineReward rows has Upgrade=0,
    /// OptionDegree=0 and TitleDegree=0. ItemOptions.shn contains only degrees
    /// 1 and 2, so the reward OptionCardStack degree-zero path has no
    /// source-defined cards. The native ocs_make call/storage normalization is
    /// still preserved as a distinct stage.
    /// </summary>
    public static class KingdomQuestRewardItemAttributeNative
    {
        public const int UsedItemRewardHandleCount = 247;
        public const int ReachableUniqueItemIdCount = 884;
        public const int ReachableNativeItemClassCount = 10;

        public const int RewardValueOffset = 0x0A;
        public const int RewardAuxiliaryByteOffset = 0x0C;
        public const int ArmorOptionStorageOffset = 0x16;
        public const int AmuletOptionStorageOffset = 0x2F;
        public const int WeaponSocketCountOffset = 0x43;
        public const int WeaponOptionStorageOffset = 0x4A;

        public const int GradeWeightCount = 10;
        public const int NativeGradeRandomUpperExclusive = 1000;

        public const uint ByteLotRewardItemCreateAddress = 0x0063F5B0u;
        public const uint QuestLotRewardItemCreateAddress = 0x0063F5D0u;
        public const uint AmuletRewardItemCreateAddress = 0x0063F610u;
        public const uint WeaponRewardItemCreateAddress = 0x0063F650u;
        public const uint ArmorRewardItemCreateAddress = 0x0063F700u;
        public const uint DecorationRewardItemCreateAddress = 0x0063F780u;
        public const uint SkillScrollRewardItemCreateAddress = 0x004C2450u;
        public const uint OptionCardMakeAddress = 0x0064C8A0u;
        public const uint Well512GetRandomAddress = 0x0063CB10u;
        public const uint WeaponSocketRateAddress = 0x0064C5C0u;

        private static readonly byte[] ReachableClasses =
        {
            0, 3, 4, 5, 6, 7, 8, 10, 11, 14,
        };

        public static IReadOnlyList<byte> GetReachableNativeItemClasses()
        {
            return Array.AsReadOnly(ReachableClasses);
        }

        public static bool IsSuppliedKqItemRewardShape(
            ShineRewardSourceRow reward)
        {
            return reward != null &&
                reward.RewardType == ShineRewardType.Item &&
                reward.Upgrade == 0 &&
                reward.OptionDegree == 0 &&
                reward.TitleDegree == 0 &&
                reward.UnknownShorts != null &&
                reward.UnknownShorts.Count == 9;
        }

        /// <summary>
        /// Builds the reward-specific part of native iac_itemcreate.
        ///
        /// well512Sample is mandatory only for classes 5..8 and must already be
        /// the exact native well512_GetRandom(1000) result. This helper neither
        /// owns nor advances WELL512 state.
        /// </summary>
        public static bool TryBuild(
            byte nativeItemClass,
            ShineRewardSourceRow reward,
            ushort? well512Sample,
            out KingdomQuestRewardItemAttributePlan plan)
        {
            plan = null;
            if (reward == null ||
                reward.RewardType != ShineRewardType.Item ||
                reward.UnknownShorts == null ||
                reward.UnknownShorts.Count != 9)
                return false;

            switch (nativeItemClass)
            {
                case 0:
                    plan = Build(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.ByteLot,
                        ByteLotRewardItemCreateAddress,
                        1,
                        reward.Quantity & 0xFFu,
                        false, false, 0, -1,
                        reward.OptionDegree,
                        false, false, false);
                    return true;

                case 3:
                    plan = Build(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.QuestLot,
                        QuestLotRewardItemCreateAddress,
                        2,
                        reward.Quantity & 0xFFFFu,
                        false, false, 0, -1,
                        reward.OptionDegree,
                        false, false, false);
                    return true;

                case 4:
                    plan = Build(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.Amulet,
                        AmuletRewardItemCreateAddress,
                        0, 0,
                        false, false, 0,
                        AmuletOptionStorageOffset,
                        reward.OptionDegree,
                        false, false, false);
                    return true;

                case 5:
                    return TryBuildWeighted(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.Weapon,
                        WeaponRewardItemCreateAddress,
                        WeaponOptionStorageOffset,
                        true,
                        reward,
                        well512Sample,
                        out plan);

                case 6:
                    return TryBuildWeighted(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.Armor,
                        ArmorRewardItemCreateAddress,
                        ArmorOptionStorageOffset,
                        false,
                        reward,
                        well512Sample,
                        out plan);

                case 7:
                    return TryBuildWeighted(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.Shield,
                        ArmorRewardItemCreateAddress,
                        ArmorOptionStorageOffset,
                        false,
                        reward,
                        well512Sample,
                        out plan);

                case 8:
                    return TryBuildWeighted(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.Boot,
                        ArmorRewardItemCreateAddress,
                        ArmorOptionStorageOffset,
                        false,
                        reward,
                        well512Sample,
                        out plan);

                case 10:
                    plan = Build(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.Decoration,
                        DecorationRewardItemCreateAddress,
                        0, 0,
                        false, false, 0, -1,
                        reward.OptionDegree,
                        true, false, false);
                    return true;

                case 11:
                    plan = Build(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.SkillScrollNoOp,
                        SkillScrollRewardItemCreateAddress,
                        0, 0,
                        false, false, 0, -1,
                        reward.OptionDegree,
                        false, true, false);
                    return true;

                case 14:
                    plan = Build(
                        nativeItemClass,
                        KingdomQuestRewardItemAttributeKind.UpSourceByteLot,
                        ByteLotRewardItemCreateAddress,
                        1,
                        reward.Quantity & 0xFFu,
                        false, false, 0, -1,
                        reward.OptionDegree,
                        false, false, false);
                    return true;

                default:
                    return false;
            }
        }

        public static bool TrySelectWeightedGrade(
            ShineRewardSourceRow reward,
            ushort well512Sample,
            out byte grade)
        {
            grade = 0;
            if (reward == null ||
                reward.UnknownShorts == null ||
                reward.UnknownShorts.Count != 9 ||
                well512Sample >= NativeGradeRandomUpperExclusive)
                return false;

            int remaining = well512Sample;
            for (int i = 0; i < GradeWeightCount; i++)
            {
                ushort weight = i == 0
                    ? unchecked((ushort)reward.Upgrade)
                    : unchecked((ushort)reward.UnknownShorts[i - 1]);

                if (remaining < weight)
                {
                    grade = (byte)i;
                    return true;
                }

                remaining -= weight;
            }

            // Native falls through with zero when the ten weights do not cover
            // the 0..999 sample.
            grade = 0;
            return true;
        }

        private static bool TryBuildWeighted(
            byte nativeItemClass,
            KingdomQuestRewardItemAttributeKind kind,
            uint address,
            int optionStorageOffset,
            bool requiresSocketRate,
            ShineRewardSourceRow reward,
            ushort? well512Sample,
            out KingdomQuestRewardItemAttributePlan plan)
        {
            plan = null;
            if (!well512Sample.HasValue)
                return false;

            byte grade;
            if (!TrySelectWeightedGrade(
                    reward, well512Sample.Value, out grade))
                return false;

            plan = Build(
                nativeItemClass,
                kind,
                address,
                1,
                grade,
                true,
                true,
                grade,
                optionStorageOffset,
                reward.OptionDegree,
                false,
                false,
                requiresSocketRate);
            return true;
        }

        private static KingdomQuestRewardItemAttributePlan Build(
            byte nativeItemClass,
            KingdomQuestRewardItemAttributeKind kind,
            uint address,
            int quantityWriteBytes,
            uint quantityWriteValue,
            bool clearsByte0C,
            bool usesWeightedGrade,
            byte weightedGrade,
            int optionCardStorageOffset,
            ushort optionDegree,
            bool clearsDecorationPayload,
            bool rewardOverloadIsNoOp,
            bool requiresWeaponSocketRate)
        {
            return new KingdomQuestRewardItemAttributePlan(
                nativeItemClass,
                kind,
                address,
                quantityWriteBytes,
                quantityWriteValue,
                clearsByte0C,
                usesWeightedGrade,
                weightedGrade,
                optionCardStorageOffset,
                optionDegree,
                clearsDecorationPayload,
                rewardOverloadIsNoOp,
                requiresWeaponSocketRate);
        }
    }
}
