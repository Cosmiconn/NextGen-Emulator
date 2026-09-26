using System;
using System.Collections.Generic;

namespace NextGen.World.Data
{
    public enum KingdomQuestTreasureChestConstructionStage : byte
    {
        ClassifierLookup = 1,
        ItemDataValidation = 2,
        RegistrationNumber = 3,
        ItemAttributeCreate = 4,
        RandomOptionLookup = 5,
        ItemOptionStorageLookup = 6,
        RandomOptionFill = 7,
        EmbedChildRegistration = 8,
        IncrementItemCount = 9,
    }

    /// <summary>
    /// Mutation-free projection of the native TreasureChestMaker layout and
    /// KQ reward-item construction order recovered from the supplied Zone.exe
    /// and Zone.pdb.
    ///
    /// Native anchors:
    ///   TreasureChestMaker::TreasureChestMaker              0x00595A40
    ///   TreasureChestMaker::tcm_ItemMake(ITI*)              0x00595BA0
    ///   TreasureChestMaker::tcm_ItemMake(int,Reward*,u32)    0x00595D00
    ///   ItemGroupClassifier::igc_Getitem                    0x004903E0
    ///   ItemDataBox::operator[](u16)                        0x00419020
    ///   ItemAttributeClassContainer::operator[](u16)        0x0063E2A0
    ///   ItemTotalInformation::iti_mkregnum                  0x00640710
    ///   RandomOptionTable::rot_FillOption                   0x00493590
    ///
    /// The internal ItemTotalInformation stride is exactly 0x6F = 111 bytes.
    /// Slot zero is the chest itself. The constructor initializes count to one,
    /// creates the chest registration number, stores the chest item ID and
    /// writes (byte)(chestFlag << 4) at chest ITI offset +0x0A.
    ///
    /// The KQ reward overload rejects immediately while count >= 8. Therefore
    /// only seven successfully built reward contents can follow the chest, and
    /// a capacity-rejected call never reaches igc_Getitem or consumes CardDeck
    /// RNG. This ordering matters for the thread-local CRT stream.
    ///
    /// For an accepted call the native order is:
    /// classifier -> ItemDataBox validation -> iti_mkregnum ->
    /// ItemAttributeClass::iac_itemcreate -> optional RandomOption datum /
    /// iac_GetItemOptionStruct / rot_FillOption -> embed the child registration
    /// number in the chest ITI -> increment count.
    ///
    /// This type models only the proven layout/order. It does not synthesize
    /// registration numbers, execute per-item-class iac_itemcreate, fill random
    /// options, mutate inventory, persist data or send GameDB packets.
    /// </summary>
    public static class KingdomQuestRewardTreasureChestNative
    {
        public const int ItemTotalInformationBytes = 0x6F;
        public const int InternalItemSlotCount = 9;
        public const int ItemCountOffset = 0x3E8;
        public const int ItemIdOffset = 0x08;
        public const int ChestFlagOffset = 0x0A;
        public const byte RequiredChestItemClass = 0x0F;
        public const int InitialItemCount = 1;
        public const int RewardMakeCountLimitExclusive = 8;
        public const int RewardContentCapacity = 7;
        public const ushort NativeNoItem = 0xFFFF;

        public const uint ConstructorAddress = 0x00595A40u;
        public const uint RawItemMakeAddress = 0x00595BA0u;
        public const uint RewardItemMakeAddress = 0x00595D00u;
        public const uint ItemGroupClassifierLookupAddress = 0x004903E0u;
        public const uint ItemDataLookupAddress = 0x00419020u;
        public const uint ItemAttributeClassLookupAddress = 0x0063E2A0u;
        public const uint MakeRegistrationNumberAddress = 0x00640710u;
        public const uint RandomOptionFillAddress = 0x00493590u;

        private static readonly KingdomQuestTreasureChestConstructionStage[]
            RewardStages =
        {
            KingdomQuestTreasureChestConstructionStage.ClassifierLookup,
            KingdomQuestTreasureChestConstructionStage.ItemDataValidation,
            KingdomQuestTreasureChestConstructionStage.RegistrationNumber,
            KingdomQuestTreasureChestConstructionStage.ItemAttributeCreate,
            KingdomQuestTreasureChestConstructionStage.RandomOptionLookup,
            KingdomQuestTreasureChestConstructionStage.ItemOptionStorageLookup,
            KingdomQuestTreasureChestConstructionStage.RandomOptionFill,
            KingdomQuestTreasureChestConstructionStage.EmbedChildRegistration,
            KingdomQuestTreasureChestConstructionStage.IncrementItemCount,
        };

        public static bool CanMakeRewardItem(int currentItemCount)
        {
            return currentItemCount >= InitialItemCount &&
                currentItemCount < RewardMakeCountLimitExclusive;
        }

        /// <summary>
        /// The raw ITI-copy overload uses the distinct native guard count <= 8.
        /// This is intentionally not reused for the KQ Reward* overload.
        /// </summary>
        public static bool CanCopyRawItem(int currentItemCount)
        {
            return currentItemCount >= 0 &&
                currentItemCount <= RewardMakeCountLimitExclusive;
        }

        public static int GetInternalItemOffset(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= InternalItemSlotCount)
                throw new ArgumentOutOfRangeException("itemIndex");

            return checked(itemIndex * ItemTotalInformationBytes);
        }

        /// <summary>
        /// Child registration numbers are embedded inside slot-zero chest ITI.
        /// Native writes the two DWORD halves at count*8+3 and count*8+7.
        /// </summary>
        public static int GetChestChildRegistrationOffset(int itemCount)
        {
            if (itemCount < InitialItemCount ||
                itemCount >= InternalItemSlotCount)
                throw new ArgumentOutOfRangeException("itemCount");

            return checked(itemCount * 8 + 3);
        }

        public static byte BuildChestFlagByte(int chestFlag)
        {
            unchecked
            {
                return (byte)(chestFlag << 4);
            }
        }

        public static IReadOnlyList<KingdomQuestTreasureChestConstructionStage>
            GetRewardConstructionStages()
        {
            return Array.AsReadOnly(RewardStages);
        }
    }
}
