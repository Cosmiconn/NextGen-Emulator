using System;
using System.Collections.Generic;

namespace NextGen.World.Data
{
    /// <summary>
    /// Native owner for the still-unrecovered normal ItemTotalInformation
    /// construction stage. The returned bytes must be one exact native
    /// 0x6F-byte ITI after normal per-class initialization/registration but
    /// before the KQ reward-specific overlay represented by ItemAttribute.
    /// </summary>
    public interface IKingdomQuestRewardNativeBaseItemSource
    {
        bool TryCreateBaseItem(
            KingdomQuestRewardConstructionEntry entry,
            out byte[] itemTotalInformation);
    }

    /// <summary>
    /// Native owner for a recovered reward-specific effect whose exact byte
    /// range is not encoded in the managed projection. At present this is the
    /// class-10 decoration payload clear. It is kept separate rather than
    /// guessed from the legacy emulator Item layout.
    /// </summary>
    public interface IKingdomQuestRewardNativeResidualAttributeSource
    {
        bool TryApplyResidualAttribute(
            KingdomQuestRewardConstructionEntry entry,
            byte[] itemTotalInformation);
    }

    public sealed class KingdomQuestRewardNativeItemEmissionEntry
    {
        public KingdomQuestRewardConstructionEntry Construction
        {
            get;
            private set;
        }

        public byte[] ItemTotalInformation { get; private set; }

        public bool HasNativeItem
        {
            get { return ItemTotalInformation != null; }
        }

        internal KingdomQuestRewardNativeItemEmissionEntry(
            KingdomQuestRewardConstructionEntry construction,
            byte[] itemTotalInformation)
        {
            Construction = construction;
            ItemTotalInformation = itemTotalInformation == null
                ? null
                : (byte[])itemTotalInformation.Clone();
        }
    }

    /// <summary>
    /// Composes the final KQ reward ITEM construction boundary without
    /// inventing normal ItemTotalInformation bytes.
    ///
    /// Proven successful-item order represented here:
    ///   native base itemcreate/registration
    ///   -> recovered KQ reward-specific byte writes
    ///   -> residual recovered class effect where required
    ///   -> class-5 weapon socket-rate stage where required.
    ///
    /// Base ITI/registration and the residual decoration effect remain
    /// explicit native dependencies. Weapon socket-rate selection is now
    /// source-backed from EnchantSocketRate.shn; the caller supplies the exact
    /// already-generated WELL512 samples in native consumption order. This
    /// coordinator never calls legacy emulator inventory code, never generates
    /// registration numbers, and never owns or seeds RNG.
    /// </summary>
    public sealed class KingdomQuestRewardNativeItemEmissionPlan
    {
        public IReadOnlyList<KingdomQuestRewardNativeItemEmissionEntry> Entries
        {
            get;
            private set;
        }

        public int NativeItemCount { get; private set; }
        public int WeaponSocketSampleCount { get; private set; }

        private KingdomQuestRewardNativeItemEmissionPlan(
            List<KingdomQuestRewardNativeItemEmissionEntry> entries,
            int nativeItemCount,
            int weaponSocketSampleCount)
        {
            Entries = entries.AsReadOnly();
            NativeItemCount = nativeItemCount;
            WeaponSocketSampleCount = weaponSocketSampleCount;
        }

        public static bool TryBuild(
            KingdomQuestRewardConstructionPlan construction,
            IKingdomQuestRewardNativeBaseItemSource baseItemSource,
            IKingdomQuestRewardNativeResidualAttributeSource residualSource,
            IReadOnlyList<KingdomQuestEnchantSocketRateSourceRow> weaponSocketRates,
            IReadOnlyList<ushort> weaponSocketSamples,
            out KingdomQuestRewardNativeItemEmissionPlan plan)
        {
            plan = null;
            if (construction == null || baseItemSource == null ||
                weaponSocketRates == null || weaponSocketSamples == null)
                return false;

            var entries =
                new List<KingdomQuestRewardNativeItemEmissionEntry>(
                    construction.Entries.Count);
            int nativeItemCount = 0;
            int weaponSocketSampleIndex = 0;

            for (int i = 0; i < construction.Entries.Count; i++)
            {
                KingdomQuestRewardConstructionEntry entry =
                    construction.Entries[i];
                if (entry == null)
                    return false;

                if (entry.Kind !=
                        KingdomQuestRewardConstructionEntryKind.ItemAttributeReady)
                {
                    entries.Add(
                        new KingdomQuestRewardNativeItemEmissionEntry(
                            entry, null));
                    continue;
                }

                if (entry.Candidate == null ||
                    entry.Candidate.ItemInfo == null ||
                    entry.ItemAttribute == null ||
                    !entry.RequiresBaseItemCreateRegistration)
                    return false;

                byte[] nativeItem;
                if (!baseItemSource.TryCreateBaseItem(
                        entry, out nativeItem) ||
                    nativeItem == null ||
                    nativeItem.Length !=
                        KingdomQuestRewardTreasureChestNative.ItemTotalInformationBytes)
                    return false;

                // Work on a clone so a rejected later stage cannot mutate the
                // base provider's own buffer by alias.
                byte[] composed = (byte[])nativeItem.Clone();

                if (!ApplyKnownRewardWrites(
                        entry.ItemAttribute, composed))
                    return false;

                if (entry.ItemAttribute.ClearsDecorationPayload)
                {
                    if (residualSource == null ||
                        !residualSource.TryApplyResidualAttribute(
                            entry, composed))
                        return false;
                }

                if (entry.RequiresWeaponSocketRate)
                {
                    if (entry.ItemAttribute.NativeItemClass != 5 ||
                        entry.Candidate.Kind !=
                            KingdomQuestRewardItemCandidateKind.ItemGroupClassifierCandidate ||
                        !KingdomQuestRewardWeaponSocketRateNative.TryApplyWeaponReward(
                            entry.Candidate.ItemInfo,
                            weaponSocketRates,
                            weaponSocketSamples,
                            ref weaponSocketSampleIndex,
                            composed))
                        return false;
                }

                entries.Add(
                    new KingdomQuestRewardNativeItemEmissionEntry(
                        entry, composed));
                nativeItemCount++;
            }

            if (weaponSocketSampleIndex != weaponSocketSamples.Count)
                return false;

            plan = new KingdomQuestRewardNativeItemEmissionPlan(
                entries, nativeItemCount, weaponSocketSampleIndex);
            return true;
        }

        /// <summary>
        /// Applies only byte targets already encoded by the recovered KQ reward
        /// ItemAttribute projection.
        ///
        /// Quantity/grade uses ITI +0x0A and is 0, 1 or 2 bytes. Weighted
        /// weapon/armor/shield/boot also clear ITI +0x0C. No option/decorative
        /// byte ranges are synthesized here.
        /// </summary>
        private static bool ApplyKnownRewardWrites(
            KingdomQuestRewardItemAttributePlan attribute,
            byte[] item)
        {
            if (attribute == null ||
                item == null ||
                item.Length !=
                    KingdomQuestRewardTreasureChestNative.ItemTotalInformationBytes)
                return false;

            int offset =
                KingdomQuestRewardItemAttributeNative.RewardValueOffset;
            switch (attribute.QuantityWriteBytes)
            {
                case 0:
                    break;

                case 1:
                    item[offset] =
                        unchecked((byte)attribute.QuantityWriteValue);
                    break;

                case 2:
                    item[offset] =
                        unchecked((byte)attribute.QuantityWriteValue);
                    item[offset + 1] =
                        unchecked((byte)(attribute.QuantityWriteValue >> 8));
                    break;

                default:
                    return false;
            }

            if (attribute.ClearsByte0C)
                item[
                    KingdomQuestRewardItemAttributeNative.RewardAuxiliaryByteOffset] = 0;

            return true;
        }
    }
}
