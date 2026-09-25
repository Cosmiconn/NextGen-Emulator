using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Native reward ACK branch after the Zone GameDB session has already
    /// resolved NETPACKETZONEHEADER.ClientHandle back to a player.
    /// </summary>
    public enum KingdomQuestRewardAckKind : byte
    {
        Success = 0,
        Failure = 1,
    }

    /// <summary>
    /// Mutation-free transaction identity forwarded by the recovered native
    /// ACK handlers after both ClientHandle and CharacterNumber validation.
    ///
    /// The native item-store virtual method names/commit semantics remain
    /// unresolved, so this object carries only the proven identity and
    /// LockIndex boundary.
    /// </summary>
    public sealed class KingdomQuestRewardAckTransactionRef
    {
        public KingdomQuestRewardAckKind Kind { get; private set; }
        public ushort ClientHandle { get; private set; }
        public uint CharacterNumber { get; private set; }
        public ushort LockIndex { get; private set; }

        internal KingdomQuestRewardAckTransactionRef(
            KingdomQuestRewardAckKind kind,
            ushort clientHandle,
            uint characterNumber,
            ushort lockIndex)
        {
            Kind = kind;
            ClientHandle = clientHandle;
            CharacterNumber = characterNumber;
            LockIndex = lockIndex;
        }
    }

    /// <summary>
    /// Models only the common post-resolution identity gate recovered from
    /// GameDBSession::gds_NC_KQ_REWARDSUC_ACK and
    /// GameDBSession::gds_NC_KQ_REWARDFAIL_ACK.
    ///
    /// The caller must already have resolved the native ClientHandle to a
    /// player and supplies that player's exact CharacterNumber. This class
    /// does not resolve ClientHandle, does not access inventory/database state,
    /// and does not invoke the native item-store virtual methods.
    /// </summary>
    public static class KingdomQuestRewardAckIdentity
    {
        public static bool TryValidateResolvedSuccess(
            KingdomQuestRewardSuccessAckInfo ack,
            ushort resolvedClientHandle,
            uint resolvedCharacterNumber,
            out KingdomQuestRewardAckTransactionRef transaction)
        {
            transaction = null;
            if (ack == null ||
                ack.ClientHandle != resolvedClientHandle ||
                ack.CharacterNumber != resolvedCharacterNumber)
                return false;

            transaction = new KingdomQuestRewardAckTransactionRef(
                KingdomQuestRewardAckKind.Success,
                ack.ClientHandle,
                ack.CharacterNumber,
                ack.LockIndex);
            return true;
        }

        public static bool TryValidateResolvedFailure(
            KingdomQuestRewardFailAckInfo ack,
            ushort resolvedClientHandle,
            uint resolvedCharacterNumber,
            out KingdomQuestRewardAckTransactionRef transaction)
        {
            transaction = null;
            if (ack == null ||
                ack.ClientHandle != resolvedClientHandle ||
                ack.CharacterNumber != resolvedCharacterNumber)
                return false;

            // Native failure ACK handling does not read the trailing Error
            // field before forwarding LockIndex to its distinct item-store
            // transaction path. Do not promote that wire field into gameplay.
            transaction = new KingdomQuestRewardAckTransactionRef(
                KingdomQuestRewardAckKind.Failure,
                ack.ClientHandle,
                ack.CharacterNumber,
                ack.LockIndex);
            return true;
        }
    }
}
