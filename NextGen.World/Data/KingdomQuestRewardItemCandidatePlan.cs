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
    /// The caller owns both native CRT states: classifierState was built from
    /// the authoritative igc_Load-start state; random is the authoritative
    /// reward-time CRT state. No seed is guessed and no framework RNG is used.
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

            for (int i = 0; i < itemPlan.Entries.Count; i++)
            {
                KingdomQuestRewardItemPlanEntry source = itemPlan.Entries[i];
                if (source == null)
                    return false;

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
