using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// One selected ITEM reward after source-backed native classifier lookup.
    /// This carries the already-resolved ShineReward row forward without
    /// choosing a CardDeck candidate or creating an Item.
    /// </summary>
    public sealed class KingdomQuestRewardItemPlanEntry
    {
        public KingdomQuestResolvedRewardEntry SelectedReward { get; private set; }
        public ShineRewardItemArgumentKind ArgumentKind { get; private set; }
        public ItemInfo ExactItemInfo { get; private set; }

        public string Argument
        {
            get { return SelectedReward.Reward.Argument ?? string.Empty; }
        }

        public uint Quantity
        {
            get { return SelectedReward.Reward.Quantity; }
        }

        internal KingdomQuestRewardItemPlanEntry(
            KingdomQuestResolvedRewardEntry selectedReward,
            ShineRewardItemArgumentKind argumentKind,
            ItemInfo exactItemInfo)
        {
            if (selectedReward == null)
                throw new ArgumentNullException("selectedReward");

            SelectedReward = selectedReward;
            ArgumentKind = argumentKind;
            ExactItemInfo = exactItemInfo;
        }
    }

    /// <summary>
    /// Mutation-free projection of the recovered TreasureChestMaker ->
    /// ItemGroupClassifier lookup boundary.
    ///
    /// Native igc_Getitem checks the ItemDataBox first, then its group tree,
    /// then returns 0xFFFF. Group expansion remains separate because the
    /// original CardStack shuffle/class filter has its own RNG/state boundary.
    /// </summary>
    public sealed class KingdomQuestRewardItemPlan
    {
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> Entries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> ExactItemInfoEntries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> ItemGroupEntries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> MissingItemGroupClassifierEntries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> AmbiguousItemInfoEntries { get; private set; }

        private KingdomQuestRewardItemPlan(
            List<KingdomQuestRewardItemPlanEntry> entries,
            List<KingdomQuestRewardItemPlanEntry> exact,
            List<KingdomQuestRewardItemPlanEntry> groups,
            List<KingdomQuestRewardItemPlanEntry> missing,
            List<KingdomQuestRewardItemPlanEntry> ambiguous)
        {
            Entries = entries.AsReadOnly();
            ExactItemInfoEntries = exact.AsReadOnly();
            ItemGroupEntries = groups.AsReadOnly();
            MissingItemGroupClassifierEntries = missing.AsReadOnly();
            AmbiguousItemInfoEntries = ambiguous.AsReadOnly();
        }

        public static bool TryBuild(
            KingdomQuestRewardSelectionPlan selection,
            IEnumerable<ItemInfo> itemInfos,
            out KingdomQuestRewardItemPlan plan)
        {
            plan = null;
            if (selection == null || itemInfos == null)
                return false;

            List<ItemInfo> catalog = itemInfos
                .Where(v => v != null)
                .ToList();
            ISet<string> nativeGroupKeys =
                KingdomQuestRewardItemGroupSource.CreateGroupOnlyKeySet();

            var entries = new List<KingdomQuestRewardItemPlanEntry>();
            var exact = new List<KingdomQuestRewardItemPlanEntry>();
            var groups = new List<KingdomQuestRewardItemPlanEntry>();
            var missing = new List<KingdomQuestRewardItemPlanEntry>();
            var ambiguous = new List<KingdomQuestRewardItemPlanEntry>();

            for (int i = 0; i < selection.Items.Count; i++)
            {
                KingdomQuestResolvedRewardEntry selected =
                    selection.Items[i];
                if (selected == null ||
                    selected.Reward == null ||
                    selected.Reward.RewardType != ShineRewardType.Item)
                    return false;

                ItemInfo exactItem;
                ShineRewardItemArgumentKind kind =
                    selected.Reward.ClassifyItemArgument(
                        catalog, nativeGroupKeys, out exactItem);

                var entry = new KingdomQuestRewardItemPlanEntry(
                    selected, kind, exactItem);
                entries.Add(entry);

                switch (kind)
                {
                    case ShineRewardItemArgumentKind.ExactItemInfoName:
                        exact.Add(entry);
                        break;
                    case ShineRewardItemArgumentKind.ItemGroupClassifierGroup:
                        groups.Add(entry);
                        break;
                    case ShineRewardItemArgumentKind.MissingItemGroupClassifierKey:
                        missing.Add(entry);
                        break;
                    case ShineRewardItemArgumentKind.AmbiguousItemInfoName:
                        ambiguous.Add(entry);
                        break;
                    default:
                        return false;
                }
            }

            plan = new KingdomQuestRewardItemPlan(
                entries, exact, groups, missing, ambiguous);
            return true;
        }
    }
}
