using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Complete mutation-free KQ reward preparation boundary.
    ///
    /// This composes the independently recovered source stages:
    /// KingdomQuestRew dice -> ShineReward handle lookup -> ITEM argument
    /// classification -> KQBoxItemIDX identity -> scalar Quantity sums.
    ///
    /// It deliberately stops before TreasureChestMaker generation, character
    /// mutation, database persistence and reward ACK handling.
    /// </summary>
    public sealed class KingdomQuestRewardPreparationPlan
    {
        public KingdomQuestRewardSelectionPlan Selection { get; private set; }
        public KingdomQuestRewardItemPlan ItemPlan { get; private set; }
        public KingdomQuestRewardBoxPlan BoxPlan { get; private set; }
        public KingdomQuestRewardScalarPlan ScalarPlan { get; private set; }

        public bool RequiresTreasureChestRuntime
        {
            get { return Selection.Items.Count != 0; }
        }

        public bool HasUnresolvedLaterRewardTypes
        {
            get { return Selection.LaterTypes.Count != 0; }
        }

        public bool HasAmbiguousItemInfoSource
        {
            get { return ItemPlan.AmbiguousItemInfoEntries.Count != 0; }
        }

        public bool HasNativeItemClassifierMisses
        {
            get
            {
                return ItemPlan.MissingItemGroupClassifierEntries.Count != 0;
            }
        }

        public bool HasUnresolvedBoxSource
        {
            get
            {
                return BoxPlan.ResolutionKind ==
                        KingdomQuestRewardBoxResolutionKind.MissingItemInfoName ||
                    BoxPlan.ResolutionKind ==
                        KingdomQuestRewardBoxResolutionKind.AmbiguousItemInfoName;
            }
        }

        /// <summary>
        /// True only when every source-only classification represented by this
        /// plan is authoritative. A proven native ItemGroupClassifier miss does
        /// not make this false: 0xFFFF is the recovered lookup result, not an
        /// unresolved source fallback.
        /// </summary>
        public bool IsSourceProjectionComplete
        {
            get
            {
                return !HasUnresolvedLaterRewardTypes &&
                    !HasAmbiguousItemInfoSource &&
                    !HasUnresolvedBoxSource;
            }
        }

        private KingdomQuestRewardPreparationPlan(
            KingdomQuestRewardSelectionPlan selection,
            KingdomQuestRewardItemPlan itemPlan,
            KingdomQuestRewardBoxPlan boxPlan,
            KingdomQuestRewardScalarPlan scalarPlan)
        {
            Selection = selection;
            ItemPlan = itemPlan;
            BoxPlan = boxPlan;
            ScalarPlan = scalarPlan;
        }

        public static bool TryBuild(
            KingdomQuestRewardSourceRow rewardSource,
            IReadOnlyList<ShineRewardSourceRow> shineRewards,
            IEnumerable<ItemInfo> itemInfos,
            IReadOnlyList<ushort> randomSamples,
            out KingdomQuestRewardPreparationPlan plan)
        {
            plan = null;
            if (rewardSource == null ||
                shineRewards == null ||
                itemInfos == null ||
                randomSamples == null)
                return false;

            KingdomQuestRewardSelectionPlan selection;
            if (!KingdomQuestRewardSelectionPlan.TryBuild(
                    rewardSource, shineRewards, randomSamples,
                    out selection))
                return false;

            KingdomQuestRewardItemPlan itemPlan;
            if (!KingdomQuestRewardItemPlan.TryBuild(
                    selection, itemInfos, out itemPlan))
                return false;

            KingdomQuestRewardBoxPlan boxPlan;
            if (!KingdomQuestRewardBoxPlan.TryBuild(
                    rewardSource, itemInfos, out boxPlan))
                return false;

            KingdomQuestRewardScalarPlan scalarPlan;
            if (!KingdomQuestRewardScalarPlan.TryBuild(
                    selection, out scalarPlan))
                return false;

            plan = new KingdomQuestRewardPreparationPlan(
                selection, itemPlan, boxPlan, scalarPlan);
            return true;
        }
    }
}
