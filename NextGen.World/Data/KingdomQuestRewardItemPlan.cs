using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// One selected ITEM reward after source-only Argument classification.
    /// This carries the already-resolved ShineReward row forward without
    /// creating an Item or choosing TreasureChest contents.
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
    /// Mutation-free bridge between KQ reward selection and the still
    /// unresolved TreasureChestMaker item-generation layer.
    ///
    /// Native sp_KQReward routes every ITEM through TreasureChestMaker.
    /// Therefore even an exact ItemInfo name is classification evidence only;
    /// this plan never constructs inventory items or treats opaque arguments
    /// as aliases.
    /// </summary>
    public sealed class KingdomQuestRewardItemPlan
    {
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> Entries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> ExactItemInfoEntries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> OpaqueTreasureChestEntries { get; private set; }
        public IReadOnlyList<KingdomQuestRewardItemPlanEntry> AmbiguousItemInfoEntries { get; private set; }

        private KingdomQuestRewardItemPlan(
            List<KingdomQuestRewardItemPlanEntry> entries,
            List<KingdomQuestRewardItemPlanEntry> exact,
            List<KingdomQuestRewardItemPlanEntry> opaque,
            List<KingdomQuestRewardItemPlanEntry> ambiguous)
        {
            Entries = entries.AsReadOnly();
            ExactItemInfoEntries = exact.AsReadOnly();
            OpaqueTreasureChestEntries = opaque.AsReadOnly();
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

            var entries = new List<KingdomQuestRewardItemPlanEntry>();
            var exact = new List<KingdomQuestRewardItemPlanEntry>();
            var opaque = new List<KingdomQuestRewardItemPlanEntry>();
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
                        catalog, out exactItem);

                var entry = new KingdomQuestRewardItemPlanEntry(
                    selected, kind, exactItem);
                entries.Add(entry);

                switch (kind)
                {
                    case ShineRewardItemArgumentKind.ExactItemInfoName:
                        exact.Add(entry);
                        break;
                    case ShineRewardItemArgumentKind.OpaqueTreasureChestArgument:
                        opaque.Add(entry);
                        break;
                    case ShineRewardItemArgumentKind.AmbiguousItemInfoName:
                        ambiguous.Add(entry);
                        break;
                    default:
                        return false;
                }
            }

            plan = new KingdomQuestRewardItemPlan(
                entries, exact, opaque, ambiguous);
            return true;
        }
    }
}
