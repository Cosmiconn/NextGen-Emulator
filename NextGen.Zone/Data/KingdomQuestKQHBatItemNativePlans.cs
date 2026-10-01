using System;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestKQHBatItemEraseAllNativePlan
    {
        private readonly byte[] itemTokenNativeBytes;

        public const uint NativeStepAddress = 0x004F0570u;
        public const uint NativeAxialListCtorAddress = 0x004285A0u;
        public const uint NativeAxialListWorkAddress = 0x00429750u;
        public const uint NativeAllInMapAddress = 0x0054B9E0u;
        public const int NativeEraseItemVtableOffset = 0x8B8;
        public const int NativeEraseAllAmount = -1;
        public const string NativeAllTarget = "all";

        public ushort ItemId { get; private set; }
        public string InxName { get; private set; }
        public bool UsesVariableItemToken { get; private set; }

        internal KingdomQuestKQHBatItemEraseAllNativePlan(
            ushort itemId,
            string inxName,
            KingdomQuestPineTokenValue itemToken)
        {
            ItemId = itemId;
            InxName = inxName ?? string.Empty;
            UsesVariableItemToken = itemToken != null;
            itemTokenNativeBytes = itemToken == null
                ? null
                : itemToken.SnapshotNativeBytes();
        }

        public byte[] SnapshotItemTokenNativeBytes()
        {
            return itemTokenNativeBytes == null
                ? null
                : (byte[])itemTokenNativeBytes.Clone();
        }
    }

    public sealed class KingdomQuestKQHBatItemDropNativePlan
    {
        private readonly byte[] sourceObjectTokenNativeBytes;
        private readonly byte[] itemTokenNativeBytes;

        public const uint NativeStepAddress = 0x004EFF00u;
        public const uint NativeObjectResolverAddress = 0x004EBBB0u;
        public const int NativeWhoKilledMeVtableOffset = 0x80C;
        public const uint NativeObjectLookupAddress = 0x0054FD10u;
        public const int NativeCharRegistNumberVtableOffset = 0x344;
        public const int NativeRateRandomMaximum = 1000000;
        public const uint NativeItemClearAddress = 0x00470CF0u;
        public const uint NativeMakeRegistrationAddress = 0x00640710u;
        public const uint NativeItemAttributeContainerLookupAddress =
            0x0063E2A0u;
        public const int NativeDropItemMakeVtableOffset = 0x2C;
        public const int NativeDropAttributeRandomMaximum = 1000;
        public const uint NativeRandomAddress = 0x0063CB10u;
        public const uint NativeRandomOptionLookupAddress = 0x00493FF0u;
        public const uint NativeRandomOptionFillAddress = 0x00493590u;
        public const uint NativeMultiTypeHandleCtorAddress = 0x004C1460u;
        public const uint NativeSetHandleObjectAddress = 0x004C14A0u;
        public const uint NativeIsDroppingAddress = 0x004B0A20u;
        public const int NativeSourceDropContextVtableOffset = 0x708;
        public const int NativeDropDatabaseValue = 1;
        public const ushort NativeMissingKillerHandle = 0xFFFF;
        public const uint NativeMissingKillerCharacterNumber = 0xFFFFFFFFu;

        public ushort ItemId { get; private set; }
        public string InxName { get; private set; }
        public int DropRate { get; private set; }

        internal KingdomQuestKQHBatItemDropNativePlan(
            ushort itemId,
            string inxName,
            int dropRate,
            KingdomQuestPineTokenValue sourceObjectToken,
            KingdomQuestPineTokenValue itemToken)
        {
            ItemId = itemId;
            InxName = inxName ?? string.Empty;
            DropRate = dropRate;
            sourceObjectTokenNativeBytes =
                sourceObjectToken.SnapshotNativeBytes();
            itemTokenNativeBytes = itemToken.SnapshotNativeBytes();
        }

        public byte[] SnapshotSourceObjectTokenNativeBytes()
        {
            return (byte[])sourceObjectTokenNativeBytes.Clone();
        }

        public byte[] SnapshotItemTokenNativeBytes()
        {
            return (byte[])itemTokenNativeBytes.Clone();
        }
    }

    /// <summary>
    /// Native/source projection for the Warrior's Code itemerase/itemdrop
    /// sites.
    ///
    /// ItemInfo.shn source identity:
    /// SHA-256
    /// 7ef63c5463ac8a5d51c3cb5ca8c80ff9311bb8ecac05fd04e5107f2fce02d494
    /// KQ_InvincibleHammer = ItemID 57000, KQ_Ice01 = ItemID 57001.
    ///
    /// Runtime ShineObject identities remain opaque. In particular InterruptArg
    /// and the killer handle recovered through WhoIsKillMe are never mapped to
    /// MapObjectID here. This builder performs no inventory/item/drop mutation.
    /// </summary>
    public static class KingdomQuestKQHBatItemNativePlanBuilder
    {
        public const string SourceItemInfoSha256 =
            "7ef63c5463ac8a5d51c3cb5ca8c80ff9311bb8ecac05fd04e5107f2fce02d494";
        public const ushort InvincibleHammerItemId = 57000;
        public const string InvincibleHammerIndex = "KQ_InvincibleHammer";
        public const ushort IceItemId = 57001;
        public const string IceItemIndex = "KQ_Ice01";
        public const string MaulIdentifier = "Maul";
        public const string InterruptArgIdentifier = "InterruptArg";
        public const int ItemEraseOccurrenceCount = 20;
        public const int ItemDropOccurrenceCount = 5;
        public const int SourceDropRate = 1000000;

        public static bool TryBuildItemEraseAll(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatItemEraseAllNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.ItemErase)
                return false;

            if (source.CanonicalLine == 50 &&
                string.Equals(
                    source.CommandText,
                    "itemerase all \"KQ_Ice01\".",
                    StringComparison.Ordinal))
            {
                plan = new KingdomQuestKQHBatItemEraseAllNativePlan(
                    IceItemId,
                    IceItemIndex,
                    null);
                return true;
            }

            bool maulSite =
                source.CanonicalLine == 49 ||
                source.CanonicalLine == 110 ||
                source.CanonicalLine == 120;
            if (!maulSite ||
                !string.Equals(
                    source.CommandText,
                    "itemerase all Maul.",
                    StringComparison.Ordinal))
                return false;

            KingdomQuestPineTokenValue itemToken;
            if (!variables.TryFind(MaulIdentifier, out itemToken) ||
                itemToken == null ||
                !string.Equals(
                    itemToken.Text,
                    InvincibleHammerIndex,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestKQHBatItemEraseAllNativePlan(
                InvincibleHammerItemId,
                InvincibleHammerIndex,
                itemToken);
            return true;
        }

        public static bool TryBuildItemDrop(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatItemDropNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.ItemDrop ||
                source.CanonicalLine != 121 ||
                !string.Equals(
                    source.CommandText,
                    "itemdrop InterruptArg Maul 1000000.",
                    StringComparison.Ordinal))
                return false;

            KingdomQuestPineTokenValue sourceObjectToken;
            KingdomQuestPineTokenValue itemToken;
            if (!variables.TryFind(
                    InterruptArgIdentifier, out sourceObjectToken) ||
                sourceObjectToken == null ||
                !variables.TryFind(MaulIdentifier, out itemToken) ||
                itemToken == null ||
                !string.Equals(
                    itemToken.Text,
                    InvincibleHammerIndex,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestKQHBatItemDropNativePlan(
                InvincibleHammerItemId,
                InvincibleHammerIndex,
                SourceDropRate,
                sourceObjectToken,
                itemToken);
            return true;
        }
    }
}
