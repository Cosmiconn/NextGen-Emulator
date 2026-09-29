using System;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineKqRewardTargetDisposition : byte
    {
        Reward = 1,
        NoReward = 2,
    }

    /// <summary>
    /// Mutation-free projection of the KQElement fields consumed by the
    /// native Pine "reward KingdomQuest" path.
    /// </summary>
    public sealed class KingdomQuestPineKqRewardCommandPlan
    {
        public uint Handle { get; private set; }
        public ushort RewardIndex { get; private set; }
        public int DemandMobKill { get; private set; }

        internal KingdomQuestPineKqRewardCommandPlan(
            uint handle,
            ushort rewardIndex,
            ushort demandMobKill)
        {
            Handle = handle;
            RewardIndex = rewardIndex;
            DemandMobKill = demandMobKill;
        }
    }

    /// <summary>
    /// Exact command-side projection of PineEventScriptNode::ShineReward for
    /// the source-used KingdomQuest branch.
    ///
    /// Zone.exe 0x004EF650 obtains the current FieldMap KQ handle and creates
    /// AxialListKQReward(handle). The iterator resolves the KQElement and stores
    /// its DemandMobKill word. For each object in the map, native
    /// KQContributeList::kqcl_GetMobKill uses that KQ Handle plus the object's
    /// virtual so_GetCharRegistNumber. A contribution below DemandMobKill calls
    /// virtual so_SendErrorCode(0x16, 0x23, 0x1104); otherwise it calls virtual
    /// so_ply_KQRewardStruct(KQElement).
    ///
    /// The embedded PROTO_KQ_INFO starts at KQElement +0x04. Its RewardIndex
    /// and DemandMobKill words therefore occur at KQElement +0x97 and +0x99.
    ///
    /// This class does not enumerate emulator map objects, infer native object
    /// identities, or execute the downstream GameDB reward transaction.
    /// </summary>
    public static class KingdomQuestPineKqRewardCommandNative
    {
        public const uint ShineRewardStepAddress = 0x004EF650u;
        public const uint AxialListKqRewardCtorAddress = 0x00428410u;
        public const uint AxialListKqRewardWorkAddress = 0x00428490u;
        public const uint KqContributeGetMobKillAddress = 0x00499D80u;

        public const int KqElementProtocolInfoOffset = 0x04;
        public const int KqElementRewardIndexOffset = 0x97;
        public const int KqElementDemandMobKillOffset = 0x99;

        public const int GetCharRegistNumberVtableOffset = 0x344;
        public const int SendErrorCodeVtableOffset = 0x308;
        public const int KqRewardStructVtableOffset = 0x76C;

        public const byte NativeNoRewardHeader = 0x16;
        public const byte NativeNoRewardType = 0x23;
        public const ushort NativeNoRewardError = 0x1104;
        public const int NativeNoRewardWireSize = 4;

        public static bool TryBuild(
            KingdomQuestZoneRuntimeState state,
            out KingdomQuestPineKqRewardCommandPlan plan)
        {
            plan = null;
            if (state == null ||
                state.Definition == null ||
                state.Definition.Handle != state.Handle)
                return false;

            KingdomQuestProtocolInfo definition = state.Definition;
            plan = new KingdomQuestPineKqRewardCommandPlan(
                state.Handle,
                definition.RewardIndex,
                definition.DemandMobKill);
            return true;
        }

        public static bool TryBuild(
            uint handle,
            out KingdomQuestPineKqRewardCommandPlan plan)
        {
            plan = null;
            KingdomQuestZoneRuntimeState state;
            return KingdomQuestZoneRuntimeRegistry.TryGet(handle, out state) &&
                TryBuild(state, out plan);
        }

        /// <summary>
        /// Mirrors the signed jl comparison in AxialListKQReward::ali_Work.
        /// The native threshold itself is a zero-extended u16.
        /// </summary>
        public static bool TryEvaluateContribution(
            KingdomQuestPineKqRewardCommandPlan plan,
            int mobKillContribution,
            out KingdomQuestPineKqRewardTargetDisposition disposition)
        {
            disposition = 0;
            if (plan == null)
                return false;

            disposition =
                mobKillContribution < plan.DemandMobKill
                    ? KingdomQuestPineKqRewardTargetDisposition.NoReward
                    : KingdomQuestPineKqRewardTargetDisposition.Reward;
            return true;
        }

        public static bool TryCreateNoRewardWire(out byte[] nativeWire)
        {
            nativeWire = null;
            using (var packet = new Packet(SH22Type.KingdomQuestNoReward))
            {
                if (packet.Header != NativeNoRewardHeader ||
                    packet.Type != NativeNoRewardType)
                    return false;

                packet.WriteUShort(NativeNoRewardError);
                byte[] bytes = packet.ToNormalArray();
                if (bytes == null || bytes.Length != NativeNoRewardWireSize)
                    return false;

                nativeWire = bytes;
                return true;
            }
        }
    }
}
