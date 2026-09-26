using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestRewardNativeTransactionStep : byte
    {
        SerializeRequestLockIndex = 1,
        SendGameDbRequest = 2,
        StageMoney = 3,
        StageFame = 4,
        AdvanceInventoryLockIndex = 5,
        GainExperienceImmediately = 6,
        AwaitRewardAck = 7,
    }

    public enum KingdomQuestRewardNativeAckAction : byte
    {
        ApplyAndFree = 1,
        FreeWithoutApply = 2,
    }

    public sealed class KingdomQuestRewardNativeAckPlan
    {
        public KingdomQuestRewardAckKind Kind { get; private set; }
        public ushort LockIndex { get; private set; }
        public KingdomQuestRewardNativeAckAction Action { get; private set; }
        public uint NativeMethodAddress { get; private set; }
        public int NativeVtableOffset { get; private set; }
        public bool SuccessReturnMustBeNonZero { get; private set; }
        public byte ApplyMode { get; private set; }

        internal KingdomQuestRewardNativeAckPlan(
            KingdomQuestRewardAckKind kind,
            ushort lockIndex,
            KingdomQuestRewardNativeAckAction action,
            uint nativeMethodAddress,
            int nativeVtableOffset,
            bool successReturnMustBeNonZero,
            byte applyMode)
        {
            Kind = kind;
            LockIndex = lockIndex;
            Action = action;
            NativeMethodAddress = nativeMethodAddress;
            NativeVtableOffset = nativeVtableOffset;
            SuccessReturnMustBeNonZero = successReturnMustBeNonZero;
            ApplyMode = applyMode;
        }
    }

    /// <summary>
    /// Exact mutation/transaction order recovered from
    /// ShinePlayer::sp_KQReward and the two GameDB KQ reward ACK handlers.
    ///
    /// The player virtual so_ply_GetInventoryLockList (+0x7D4, 0x0055A1D0)
    /// returns the embedded InventoryCellLockList at ShinePlayer +0xFAA8.
    /// Its dynamic vtable is 0x006BB37C.
    ///
    /// sp_KQReward writes the current lock index into NC_KQ_REWARD_REQ,
    /// serializes/sends opcode 0x5815 to GameDB, then stages MONEY and FAME on
    /// that same lock index through icl_StoreMoney / icl_StoreFame, increments
    /// the lock index and immediately calls sp_GainExp. EXP is therefore not
    /// rolled back by a later reward failure ACK.
    ///
    /// Success ACK 0x5816 dispatches InventoryCellLockList::icl_Apply_N_Free
    /// (vtable +0x1C) with (player, lockIndex, 0). That walks matching locked
    /// cells, calls each releaser and removes the cells; a zero return is the
    /// native error/log branch.
    ///
    /// Failure ACK 0x5817 dispatches InventoryCellLockList::icl_Free
    /// (vtable +0x28) with lockIndex. It removes matching locked cells without
    /// invoking their apply releasers. The ACK Error field is not consulted.
    ///
    /// Cen/Fame releasers perform the staged add/sub mutation only on the
    /// success apply path and then invoke their native update hooks.
    /// </summary>
    public static class KingdomQuestRewardNativeTransaction
    {
        public const uint ShinePlayerKqRewardAddress = 0x0052DA70u;
        public const uint GetInventoryLockListAddress = 0x0055A1D0u;
        public const int ShinePlayerInventoryLockOffset = 0xFAA8;
        public const uint InventoryCellLockListVtableAddress = 0x006BB37Cu;

        public const uint ProtocolPacketSetLengthAddress = 0x004C8CE0u;
        public const uint ProtocolPacketSendAddress = 0x004C89C0u;
        public const uint StoreMoneyAddress = 0x00489130u;
        public const uint StoreFameAddress = 0x00489260u;
        public const uint GainExperienceAddress = 0x0042D830u;

        public const uint SuccessAckHandlerAddress = 0x0052E570u;
        public const uint FailureAckHandlerAddress = 0x0052E680u;
        public const int ApplyAndFreeVtableOffset = 0x1C;
        public const int FreeVtableOffset = 0x28;
        public const uint ApplyAndFreeAddress = 0x0048BE90u;
        public const uint FreeAddress = 0x0048B2C0u;

        public const uint CenChangeReleaserAddress = 0x00487D50u;
        public const uint FameChangeReleaserAddress = 0x00487E60u;

        public const byte NativeKqApplyMode = 0;

        private static readonly KingdomQuestRewardNativeTransactionStep[]
            PreAckOrder =
        {
            KingdomQuestRewardNativeTransactionStep.SerializeRequestLockIndex,
            KingdomQuestRewardNativeTransactionStep.SendGameDbRequest,
            KingdomQuestRewardNativeTransactionStep.StageMoney,
            KingdomQuestRewardNativeTransactionStep.StageFame,
            KingdomQuestRewardNativeTransactionStep.AdvanceInventoryLockIndex,
            KingdomQuestRewardNativeTransactionStep.GainExperienceImmediately,
            KingdomQuestRewardNativeTransactionStep.AwaitRewardAck,
        };

        public static IReadOnlyList<KingdomQuestRewardNativeTransactionStep>
            GetPreAckOrder()
        {
            return Array.AsReadOnly(PreAckOrder);
        }

        public static bool IsExperienceAckTransactional
        {
            get { return false; }
        }

        public static bool AreMoneyAndFameAckTransactional
        {
            get { return true; }
        }

        public static bool TryBuildAckPlan(
            KingdomQuestRewardAckTransactionRef transaction,
            out KingdomQuestRewardNativeAckPlan plan)
        {
            plan = null;
            if (transaction == null)
                return false;

            if (transaction.Kind == KingdomQuestRewardAckKind.Success)
            {
                plan = new KingdomQuestRewardNativeAckPlan(
                    transaction.Kind,
                    transaction.LockIndex,
                    KingdomQuestRewardNativeAckAction.ApplyAndFree,
                    ApplyAndFreeAddress,
                    ApplyAndFreeVtableOffset,
                    true,
                    NativeKqApplyMode);
                return true;
            }

            if (transaction.Kind == KingdomQuestRewardAckKind.Failure)
            {
                plan = new KingdomQuestRewardNativeAckPlan(
                    transaction.Kind,
                    transaction.LockIndex,
                    KingdomQuestRewardNativeAckAction.FreeWithoutApply,
                    FreeAddress,
                    FreeVtableOffset,
                    false,
                    0);
                return true;
            }

            return false;
        }
    }
}
