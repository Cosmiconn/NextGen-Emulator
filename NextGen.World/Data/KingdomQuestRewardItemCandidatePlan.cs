using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    public enum KingdomQuestRewardItemCandidateKind : byte
    {
        ExactItemInfo = 1,
        ItemGroupClassifierCandidate = 2,
        NativeClassifierKeyMiss = 3,
        NativeNoCompatibleGroupCandidate = 4,
        NativeTreasureChestCapacityRejected = 5,
    }

    public sealed class KingdomQuestRewardItemCandidatePlanEntry
    {
        public KingdomQuestRewardItemPlanEntry SourceEntry { get; private set; }
        public KingdomQuestRewardItemCandidateKind Kind { get; private set; }
        public ItemInfo ItemInfo { get; private set; }

        internal KingdomQuestRewardItemCandidatePlanEntry(
            KingdomQuestRewardItemPlanEntry sourceEntry,
            KingdomQuestRewardItemCandidateKind kind,
            ItemInfo itemInfo)
        {
            SourceEntry = sourceEntry;
            Kind = kind;
            ItemInfo = itemInfo;
        }
    }

    /// <summary>
    /// Mutation-free continuation of KingdomQuestRewardItemPlan through the
    /// recovered ItemGroupClassifier/CardStack candidate-choice boundary.
    ///
    /// The caller owns both explicit native-thread CRT states:
    /// classifierState was built from the authoritative igc_Load-start state;
    /// random is the authoritative reward-time state for the native thread
    /// executing this path. No seed/thread assignment is guessed and no
    /// framework RNG is used.
    ///
    /// Native TreasureChestMaker checks count < 8 before igc_Getitem. Because
    /// slot zero is already the chest, only seven successfully resolved reward
    /// items may reach construction. Later calls are represented as native
    /// capacity rejects without classifier/CardDeck RNG consumption.
    ///
    /// This does not create ItemTotalInformation, apply upgrades/options,
    /// mutate inventory, persist, or send GameDB packets.
    /// </summary>
    public sealed class KingdomQuestRewardItemCandidatePlan
    {
        public IReadOnlyList<KingdomQuestRewardItemCandidatePlanEntry> Entries
        {
            get;
            private set;
        }

        public bool HasNativeMiss
        {
            get
            {
                return Entries.Any(v =>
                    v.Kind ==
                        KingdomQuestRewardItemCandidateKind.NativeClassifierKeyMiss ||
                    v.Kind ==
                        KingdomQuestRewardItemCandidateKind.NativeNoCompatibleGroupCandidate);
            }
        }

        public bool HasTreasureChestCapacityRejection
        {
            get
            {
                return Entries.Any(v =>
                    v.Kind ==
                        KingdomQuestRewardItemCandidateKind.NativeTreasureChestCapacityRejected);
            }
        }

        private KingdomQuestRewardItemCandidatePlan(
            List<KingdomQuestRewardItemCandidatePlanEntry> entries)
        {
            Entries = entries.AsReadOnly();
        }

        public static bool TryBuildFromNativeClass(
            KingdomQuestRewardItemPlan itemPlan,
            IEnumerable<ItemInfo> itemInfos,
            KingdomQuestNativeItemGroupClassifierState classifierState,
            byte nativePlayerClass,
            IDictionary<uint, long> useClassMasks,
            KingdomQuestMsvcCrtRand random,
            out KingdomQuestRewardItemCandidatePlan plan)
        {
            return TryBuild(
                itemPlan,
                itemInfos,
                classifierState,
                KingdomQuestRewardClassGroup.FromNativeClass(
                    nativePlayerClass),
                useClassMasks,
                random,
                out plan);
        }

        public static bool TryBuild(
            KingdomQuestRewardItemPlan itemPlan,
            IEnumerable<ItemInfo> itemInfos,
            KingdomQuestNativeItemGroupClassifierState classifierState,
            uint classGroup,
            IDictionary<uint, long> useClassMasks,
            KingdomQuestMsvcCrtRand random,
            out KingdomQuestRewardItemCandidatePlan plan)
        {
            plan = null;
            if (itemPlan == null ||
                itemInfos == null ||
                classifierState == null ||
                useClassMasks == null ||
                random == null)
                return false;

            var itemById = new Dictionary<ushort, ItemInfo>();
            foreach (ItemInfo item in itemInfos)
            {
                if (item == null)
                    continue;
                if (itemById.ContainsKey(item.ItemID))
                    return false;
                itemById[item.ItemID] = item;
            }

            var entries =
                new List<KingdomQuestRewardItemCandidatePlanEntry>(
                    itemPlan.Entries.Count);
            int successfulContentCount = 0;

            for (int i = 0; i < itemPlan.Entries.Count; i++)
            {
                KingdomQuestRewardItemPlanEntry source = itemPlan.Entries[i];
                if (source == null)
                    return false;

                // TreasureChestMaker::tcm_ItemMake(int, ShineReward*, u32)
                // checks its current item count before igc_Getitem. Keep this
                // before the switch so capacity rejection consumes no
                // classifier/CardDeck RNG.
                if (successfulContentCount >=
                        KingdomQuestRewardTreasureChestNative.RewardContentCapacity)
                {
                    entries.Add(
                        new KingdomQuestRewardItemCandidatePlanEntry(
                            source,
                            KingdomQuestRewardItemCandidateKind.NativeTreasureChestCapacityRejected,
                            null));
                    continue;
                }

                switch (source.ArgumentKind)
                {
                    case ShineRewardItemArgumentKind.ExactItemInfoName:
                        if (source.ExactItemInfo == null)
                            return false;
                        entries.Add(
                            new KingdomQuestRewardItemCandidatePlanEntry(
                                source,
                                KingdomQuestRewardItemCandidateKind.ExactItemInfo,
                                source.ExactItemInfo));
                        successfulContentCount++;
                        break;

                    case ShineRewardItemArgumentKind.ItemGroupClassifierGroup:
                    {
                        ushort selectedItemId;
                        KingdomQuestItemGroupLookupKind result =
                            classifierState.Select(
                                source.Argument,
                                classGroup,
                                useClassMasks,
                                random,
                                out selectedItemId);

                        if (result ==
                                KingdomQuestItemGroupLookupKind.SourceIncomplete)
                            return false;

                        if (result ==
                                KingdomQuestItemGroupLookupKind.NativeNoCompatibleCandidate)
                        {
                            entries.Add(
                                new KingdomQuestRewardItemCandidatePlanEntry(
                                    source,
                                    KingdomQuestRewardItemCandidateKind.NativeNoCompatibleGroupCandidate,
                                    null));
                            break;
                        }

                        ItemInfo selectedItem;
                        if (!itemById.TryGetValue(
                                selectedItemId, out selectedItem))
                            return false;

                        entries.Add(
                            new KingdomQuestRewardItemCandidatePlanEntry(
                                source,
                                KingdomQuestRewardItemCandidateKind.ItemGroupClassifierCandidate,
                                selectedItem));
                        successfulContentCount++;
                        break;
                    }

                    case ShineRewardItemArgumentKind.MissingItemGroupClassifierKey:
                        entries.Add(
                            new KingdomQuestRewardItemCandidatePlanEntry(
                                source,
                                KingdomQuestRewardItemCandidateKind.NativeClassifierKeyMiss,
                                null));
                        break;

                    case ShineRewardItemArgumentKind.AmbiguousItemInfoName:
                    default:
                        return false;
                }
            }

            plan = new KingdomQuestRewardItemCandidatePlan(entries);
            return true;
        }
    }
}
