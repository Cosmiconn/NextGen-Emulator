using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    public enum KingdomQuestRewardBoxResolutionKind : byte
    {
        Empty = 0,
        ExactItemInfoName = 1,
        MissingItemInfoName = 2,
        AmbiguousItemInfoName = 3,
    }

    /// <summary>
    /// Source-only projection of KINGDOM_QUEST_REW.KQBoxItemIDX.
    ///
    /// The original reward row carries a separate box-item name in addition
    /// to its fifteen ShineReward handles. This model resolves only that name
    /// against ItemInfo using ordinal equality. It never opens the box,
    /// chooses TreasureChest contents, grants an item or persists anything.
    /// </summary>
    public sealed class KingdomQuestRewardBoxPlan
    {
        public string BoxItemIndex { get; private set; }
        public KingdomQuestRewardBoxResolutionKind ResolutionKind { get; private set; }
        public ItemInfo ExactItemInfo { get; private set; }

        private KingdomQuestRewardBoxPlan(
            string boxItemIndex,
            KingdomQuestRewardBoxResolutionKind resolutionKind,
            ItemInfo exactItemInfo)
        {
            BoxItemIndex = boxItemIndex ?? string.Empty;
            ResolutionKind = resolutionKind;
            ExactItemInfo = exactItemInfo;
        }

        public static bool TryBuild(
            KingdomQuestRewardSourceRow rewardSource,
            IEnumerable<ItemInfo> itemInfos,
            out KingdomQuestRewardBoxPlan plan)
        {
            plan = null;
            if (rewardSource == null)
                return false;

            string boxItemIndex = rewardSource.KQBoxItemIDX ?? string.Empty;
            if (boxItemIndex.Length == 0)
            {
                plan = new KingdomQuestRewardBoxPlan(
                    string.Empty,
                    KingdomQuestRewardBoxResolutionKind.Empty,
                    null);
                return true;
            }

            if (itemInfos == null)
                return false;

            ItemInfo match = null;
            foreach (ItemInfo candidate in itemInfos)
            {
                if (candidate == null ||
                    !string.Equals(
                        candidate.InxName, boxItemIndex,
                        StringComparison.Ordinal))
                    continue;

                if (match != null)
                {
                    plan = new KingdomQuestRewardBoxPlan(
                        boxItemIndex,
                        KingdomQuestRewardBoxResolutionKind.AmbiguousItemInfoName,
                        null);
                    return true;
                }

                match = candidate;
            }

            if (match == null)
            {
                plan = new KingdomQuestRewardBoxPlan(
                    boxItemIndex,
                    KingdomQuestRewardBoxResolutionKind.MissingItemInfoName,
                    null);
                return true;
            }

            plan = new KingdomQuestRewardBoxPlan(
                boxItemIndex,
                KingdomQuestRewardBoxResolutionKind.ExactItemInfoName,
                match);
            return true;
        }
    }
}
