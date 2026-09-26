using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    public enum KingdomQuestRewardConstructionEntryKind : byte
    {
        ItemAttributeReady = 1,
        NativeClassifierMiss = 2,
        NativeNoCompatibleCandidate = 3,
        NativeTreasureChestCapacityRejected = 4,
    }

    public sealed class KingdomQuestRewardConstructionEntry
    {
        public KingdomQuestRewardItemCandidatePlanEntry Candidate { get; private set; }
        public KingdomQuestRewardConstructionEntryKind Kind { get; private set; }
        public KingdomQuestRewardItemAttributePlan ItemAttribute { get; private set; }
        public bool RequiresBaseItemCreateRegistration { get; private set; }
        public bool RequiresWeaponSocketRate { get; private set; }

        internal KingdomQuestRewardConstructionEntry(
            KingdomQuestRewardItemCandidatePlanEntry candidate,
            KingdomQuestRewardConstructionEntryKind kind,
            KingdomQuestRewardItemAttributePlan itemAttribute,
            bool requiresBaseItemCreateRegistration,
            bool requiresWeaponSocketRate)
        {
            Candidate = candidate;
            Kind = kind;
            ItemAttribute = itemAttribute;
            RequiresBaseItemCreateRegistration =
                requiresBaseItemCreateRegistration;
            RequiresWeaponSocketRate = requiresWeaponSocketRate;
        }
    }

    /// <summary>
    /// Mutation-free composition of the already recovered KQ ITEM stages:
    ///
    /// selected ShineReward ITEM -> exact/CardDeck candidate ->
    /// native ItemInfo class -> reward-specific ItemAttributeClass writes.
    ///
    /// Native ItemInfo.class is loaded directly into ItemInfo.Class by the
    /// source loader, so no emulator-only class translation is introduced.
    ///
    /// Classes 5..8 consume one explicit well512_GetRandom(1000) sample in
    /// candidate order for the reward-specific grade write. Native misses and
    /// TreasureChest capacity rejection consume no sample here. This helper
    /// never owns or advances WELL512 itself.
    ///
    /// The remaining boundary is deliberately explicit on each successfully
    /// selected item: normal per-class itemcreate/registration is still
    /// required for every item; class 5 additionally requires the recovered
    /// weapon-socket-rate stage. The supplied reward OptionCard path is not
    /// promoted to a blocker here because source correlation already proves
    /// OptionDegree=0 for all 247 used ITEM rewards and no degree-zero option
    /// cards exist in the supplied ItemOptions source.
    /// </summary>
    public sealed class KingdomQuestRewardConstructionPlan
    {
        public IReadOnlyList<KingdomQuestRewardConstructionEntry> Entries
        {
            get;
            private set;
        }

        public int WeightedSampleCount { get; private set; }

        public bool RequiresBaseItemCreateRegistration
        {
            get
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (Entries[i].RequiresBaseItemCreateRegistration)
                        return true;
                }
                return false;
            }
        }

        public bool RequiresWeaponSocketRate
        {
            get
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (Entries[i].RequiresWeaponSocketRate)
                        return true;
                }
                return false;
            }
        }

        private KingdomQuestRewardConstructionPlan(
            List<KingdomQuestRewardConstructionEntry> entries,
            int weightedSampleCount)
        {
            Entries = entries.AsReadOnly();
            WeightedSampleCount = weightedSampleCount;
        }

        public static bool TryBuild(
            KingdomQuestRewardItemCandidatePlan candidates,
            IReadOnlyList<ushort> weightedGradeSamples,
            out KingdomQuestRewardConstructionPlan plan)
        {
            plan = null;
            if (candidates == null || weightedGradeSamples == null)
                return false;

            var entries =
                new List<KingdomQuestRewardConstructionEntry>(
                    candidates.Entries.Count);
            int sampleIndex = 0;

            for (int i = 0; i < candidates.Entries.Count; i++)
            {
                KingdomQuestRewardItemCandidatePlanEntry candidate =
                    candidates.Entries[i];
                if (candidate == null || candidate.SourceEntry == null)
                    return false;

                switch (candidate.Kind)
                {
                    case KingdomQuestRewardItemCandidateKind.NativeClassifierKeyMiss:
                        entries.Add(new KingdomQuestRewardConstructionEntry(
                            candidate,
                            KingdomQuestRewardConstructionEntryKind.NativeClassifierMiss,
                            null,
                            false,
                            false));
                        continue;

                    case KingdomQuestRewardItemCandidateKind.NativeNoCompatibleGroupCandidate:
                        entries.Add(new KingdomQuestRewardConstructionEntry(
                            candidate,
                            KingdomQuestRewardConstructionEntryKind.NativeNoCompatibleCandidate,
                            null,
                            false,
                            false));
                        continue;

                    case KingdomQuestRewardItemCandidateKind.NativeTreasureChestCapacityRejected:
                        entries.Add(new KingdomQuestRewardConstructionEntry(
                            candidate,
                            KingdomQuestRewardConstructionEntryKind.NativeTreasureChestCapacityRejected,
                            null,
                            false,
                            false));
                        continue;

                    case KingdomQuestRewardItemCandidateKind.ExactItemInfo:
                    case KingdomQuestRewardItemCandidateKind.ItemGroupClassifierCandidate:
                        break;

                    default:
                        return false;
                }

                if (candidate.ItemInfo == null ||
                    candidate.SourceEntry.SelectedReward == null ||
                    candidate.SourceEntry.SelectedReward.Reward == null)
                    return false;

                byte nativeItemClass =
                    unchecked((byte)candidate.ItemInfo.Class);
                bool requiresWeightedSample =
                    nativeItemClass >= 5 && nativeItemClass <= 8;

                ushort? sample = null;
                if (requiresWeightedSample)
                {
                    if (sampleIndex >= weightedGradeSamples.Count)
                        return false;
                    sample = weightedGradeSamples[sampleIndex++];
                }

                KingdomQuestRewardItemAttributePlan attribute;
                if (!KingdomQuestRewardItemAttributeNative.TryBuild(
                        nativeItemClass,
                        candidate.SourceEntry.SelectedReward.Reward,
                        sample,
                        out attribute))
                    return false;

                entries.Add(new KingdomQuestRewardConstructionEntry(
                    candidate,
                    KingdomQuestRewardConstructionEntryKind.ItemAttributeReady,
                    attribute,
                    true,
                    attribute.RequiresWeaponSocketRate));
            }

            if (sampleIndex != weightedGradeSamples.Count)
                return false;

            plan = new KingdomQuestRewardConstructionPlan(
                entries, sampleIndex);
            return true;
        }
    }
}
