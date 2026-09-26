using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact 20-byte WorldManager KQ_VOTE_INFO logical fields.
    ///
    /// Native storage is one entry per KQ slot:
    /// team byte, starter index, target index, time32 end time,
    /// yes/no/cancel bytes. Padding is intentionally not promoted to state.
    /// </summary>
    public sealed class KingdomQuestVoteState
    {
        public byte TeamType { get; private set; }
        public int StarterIndex { get; private set; }
        public int TargetIndex { get; private set; }
        public int EndTime { get; private set; }
        public byte YesCount { get; private set; }
        public byte NoCount { get; private set; }
        public byte CancelCount { get; private set; }

        public bool IsActive
        {
            get { return StarterIndex != -1 && TargetIndex != -1; }
        }

        internal KingdomQuestVoteState()
        {
            Clear();
        }

        internal KingdomQuestVoteState Clone()
        {
            return new KingdomQuestVoteState
            {
                TeamType = TeamType,
                StarterIndex = StarterIndex,
                TargetIndex = TargetIndex,
                EndTime = EndTime,
                YesCount = YesCount,
                NoCount = NoCount,
                CancelCount = CancelCount,
            };
        }

        internal void Begin(
            byte teamType, int starterIndex, int targetIndex,
            int endTime, byte cancelCount)
        {
            TeamType = teamType;
            StarterIndex = starterIndex;
            TargetIndex = targetIndex;
            EndTime = endTime;
            YesCount = 0;
            NoCount = 0;
            CancelCount = cancelCount;
        }

        internal void Record(int choice)
        {
            unchecked
            {
                if (choice == KingdomQuestNativeConstants.VoteChoiceYes)
                {
                    CancelCount = (byte)(CancelCount - 1);
                    YesCount = (byte)(YesCount + 1);
                }
                else if (choice == KingdomQuestNativeConstants.VoteChoiceNo)
                {
                    CancelCount = (byte)(CancelCount - 1);
                    NoCount = (byte)(NoCount + 1);
                }
                // KVT_CANCEL=0 and out-of-range values leave the initially
                // reserved CancelCount unchanged, matching native code.
            }
        }

        internal void Clear()
        {
            TeamType = KingdomQuestNativeConstants.NeutralTeamType;
            StarterIndex = -1;
            TargetIndex = -1;
            EndTime = 0;
            YesCount = 0;
            NoCount = 0;
            CancelCount = 0;
        }
    }

    public sealed class KingdomQuestVoteStartPlan
    {
        public byte TeamType { get; private set; }
        public int StarterIndex { get; private set; }
        public int TargetIndex { get; private set; }
        public int EndTime { get; private set; }
        public byte VoteType { get; private set; }
        public string StarterName { get; private set; }
        public string TargetName { get; private set; }
        public IReadOnlyList<uint> VoterCharacterNumbers { get; private set; }

        internal KingdomQuestVoteStartPlan(
            byte teamType, int starterIndex, int targetIndex,
            int endTime, byte voteType, string starterName,
            string targetName, IEnumerable<uint> voterCharacterNumbers)
        {
            TeamType = teamType;
            StarterIndex = starterIndex;
            TargetIndex = targetIndex;
            EndTime = endTime;
            VoteType = voteType;
            StarterName = starterName ?? string.Empty;
            TargetName = targetName ?? string.Empty;
            VoterCharacterNumbers =
                (voterCharacterNumbers ?? new uint[0]).ToList().AsReadOnly();
        }
    }

    public sealed class KingdomQuestVoteResolution
    {
        public bool Passed { get; private set; }
        public byte RequiredRate { get; private set; }
        public byte CalculatedYesRate { get; private set; }
        public byte YesCount { get; private set; }
        public byte NoCount { get; private set; }
        public byte CancelCount { get; private set; }
        public uint TargetCharacterNumber { get; private set; }
        public string TargetName { get; private set; }
        public IReadOnlyList<uint> AudienceCharacterNumbers { get; private set; }

        internal KingdomQuestVoteResolution(
            bool passed, byte requiredRate, byte calculatedYesRate,
            byte yesCount, byte noCount, byte cancelCount,
            uint targetCharacterNumber, string targetName,
            IEnumerable<uint> audienceCharacterNumbers)
        {
            Passed = passed;
            RequiredRate = requiredRate;
            CalculatedYesRate = calculatedYesRate;
            YesCount = yesCount;
            NoCount = noCount;
            CancelCount = cancelCount;
            TargetCharacterNumber = targetCharacterNumber;
            TargetName = targetName ?? string.Empty;
            AudienceCharacterNumbers =
                (audienceCharacterNumbers ?? new uint[0]).ToList().AsReadOnly();
        }
    }

    public sealed class KingdomQuestVoteCancelPlan
    {
        public string TargetName { get; private set; }
        public IReadOnlyList<uint> AudienceCharacterNumbers { get; private set; }

        internal KingdomQuestVoteCancelPlan(
            string targetName, IEnumerable<uint> audienceCharacterNumbers)
        {
            TargetName = targetName ?? string.Empty;
            AudienceCharacterNumbers =
                (audienceCharacterNumbers ?? new uint[0]).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Mutation-free-to-gameplay projection of the recovered WorldManager vote
    /// state machine. It mutates only KQ membership/vote bookkeeping; it does
    /// not send packets, transfer/bounce a banned player, or invent the two
    /// external KQVote_* timing configuration values.
    /// </summary>
    public static class KingdomQuestVoteCoordinator
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestVoteState> ByHandle =
            new Dictionary<uint, KingdomQuestVoteState>();

        public static bool TryGetState(
            uint handle, out KingdomQuestVoteState state)
        {
            lock (Sync)
            {
                KingdomQuestVoteState current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    state = new KingdomQuestVoteState();
                    return true;
                }

                state = current.Clone();
                return true;
            }
        }

        /// <summary>
        /// Models Recv_NC_KQ_VOTE_START_REQ after the caller has resolved the
        /// target-session-availability and suggest-cooldown predicates.
        ///
        /// voteEndTime must be supplied from the original KQVote_VoteLimitTime
        /// setting. contentsLength is the request byte at +0x15; native 0x3107
        /// tests that byte, not VoteType at +0x14.
        /// </summary>
        public static bool TryPrepareStart(
            uint handle,
            uint starterCharacterNumber,
            string targetName,
            byte voteType,
            byte contentsLength,
            bool targetSessionAvailable,
            bool suggestCooldownActive,
            int voteEndTime,
            out ushort error,
            out KingdomQuestVoteStartPlan plan)
        {
            error = 0;
            plan = null;

            lock (Sync)
            {
                KingdomQuestProtocolInfo definition;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out definition))
                {
                    error = KingdomQuestNativeConstants.VoteStartInvalidHandle;
                    return true;
                }

                if (definition.Status != KingdomQuestNativeConstants.StatusRunning)
                {
                    error = KingdomQuestNativeConstants.VoteStartWrongStatus;
                    return true;
                }

                KingdomQuestVoteState current;
                if (ByHandle.TryGetValue(handle, out current) &&
                    current.IsActive)
                {
                    error = KingdomQuestNativeConstants.VoteStartAlreadyRunning;
                    return true;
                }

                DataProvider provider = DataProvider.Instance;
                if (provider == null ||
                    provider.KingdomQuestVoteEnabled == null)
                    return false;

                bool voteEnabled;
                if (!provider.KingdomQuestVoteEnabled.TryGetValue(
                        definition.ID, out voteEnabled) ||
                    !voteEnabled)
                {
                    error = KingdomQuestNativeConstants.VoteStartDisabled;
                    return true;
                }

                IReadOnlyList<KingdomQuestMembershipEntry> source;
                if (!KingdomQuestMembershipRegistry.TryGet(handle, out source))
                    return false;

                var members = source.Select(v => v.Clone()).ToList();
                int starterIndex = members.FindIndex(
                    v => v.CharacterNumber == starterCharacterNumber);
                if (starterIndex < 0)
                {
                    error = KingdomQuestNativeConstants.VoteStartInvalidHandle;
                    return true;
                }

                int targetIndex = members.FindIndex(
                    v => string.Equals(
                        v.Name, targetName ?? string.Empty,
                        StringComparison.Ordinal));
                if (targetIndex < 0)
                {
                    error = KingdomQuestNativeConstants.VoteStartTargetRejected;
                    return true;
                }

                KingdomQuestMembershipEntry starter = members[starterIndex];
                KingdomQuestMembershipEntry target = members[targetIndex];
                if (starter.TeamType != target.TeamType ||
                    !targetSessionAvailable ||
                    target.BanRaw == 1)
                {
                    error = KingdomQuestNativeConstants.VoteStartTargetRejected;
                    return true;
                }

                if (string.Equals(
                        target.Name, starter.Name,
                        StringComparison.Ordinal))
                {
                    error = KingdomQuestNativeConstants.VoteStartSelfTarget;
                    return true;
                }

                if (contentsLength == 0)
                {
                    error = KingdomQuestNativeConstants.VoteStartEmptyContents;
                    return true;
                }

                if (suggestCooldownActive)
                {
                    error = KingdomQuestNativeConstants.VoteStartSuggestCooldown;
                    return true;
                }

                var voters = new List<uint>();
                byte cancelCount = 0;
                for (int i = 0; i < members.Count; i++)
                {
                    if (i == starterIndex || i == targetIndex ||
                        members[i].TeamType != starter.TeamType)
                        continue;

                    members[i].InVoteRaw = 1;
                    unchecked { cancelCount++; }
                    voters.Add(members[i].CharacterNumber);
                }

                if (!KingdomQuestSessionCoordinator.TrySetMembership(
                        handle, members))
                    return false;

                var state = new KingdomQuestVoteState();
                state.Begin(
                    starter.TeamType, starterIndex, targetIndex,
                    voteEndTime, cancelCount);
                ByHandle[handle] = state;

                error = KingdomQuestNativeConstants.VoteStartSuccess;
                plan = new KingdomQuestVoteStartPlan(
                    starter.TeamType, starterIndex, targetIndex,
                    voteEndTime, voteType, starter.Name, target.Name, voters);
                return true;
            }
        }

        /// <summary>
        /// Models Recv_NC_KQ_VOTE_VOTING_REQ. The native request carries the
        /// KQ_VOTING_TYPE enum as a 32-bit value. Only YES=1 and NO=2 change
        /// counters; CANCEL=0 and other values simply consume eligibility.
        /// </summary>
        public static bool TryRecordVote(
            uint handle,
            uint characterNumber,
            int choice,
            out ushort error)
        {
            error = 0;
            lock (Sync)
            {
                KingdomQuestProtocolInfo definition;
                IReadOnlyList<KingdomQuestMembershipEntry> source;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out definition) ||
                    !KingdomQuestMembershipRegistry.TryGet(handle, out source))
                {
                    error =
                        KingdomQuestNativeConstants.VoteVotingInvalidJoiner;
                    return true;
                }

                var members = source.Select(v => v.Clone()).ToList();
                int memberIndex = members.FindIndex(
                    v => v.CharacterNumber == characterNumber);
                if (memberIndex < 0)
                {
                    error =
                        KingdomQuestNativeConstants.VoteVotingInvalidJoiner;
                    return true;
                }

                KingdomQuestVoteState state;
                if (!ByHandle.TryGetValue(handle, out state) ||
                    !state.IsActive ||
                    members[memberIndex].TeamType != state.TeamType)
                {
                    error =
                        KingdomQuestNativeConstants.VoteVotingWrongVoteOrTeam;
                    return true;
                }

                if (members[memberIndex].InVoteRaw == 0)
                {
                    error =
                        KingdomQuestNativeConstants.VoteVotingNotEligible;
                    return true;
                }

                members[memberIndex].InVoteRaw = 0;
                state = state.Clone();
                state.Record(choice);

                if (!KingdomQuestSessionCoordinator.TrySetMembership(
                        handle, members))
                    return false;

                ByHandle[handle] = state;
                error = KingdomQuestNativeConstants.VoteVotingSuccess;
                return true;
            }
        }

        /// <summary>
        /// Models the expired-vote branch of CKQServer::VoteProcessing.
        /// Native processing runs only when EndTime &lt; currentTime.
        /// The target VotingCount chooses the majority-rate row, clamped to
        /// the last row; target VotingCount is incremented for every result.
        /// </summary>
        public static bool TryResolveExpired(
            uint handle,
            int currentTime,
            out KingdomQuestVoteResolution resolution)
        {
            resolution = null;
            lock (Sync)
            {
                KingdomQuestVoteState state;
                if (!ByHandle.TryGetValue(handle, out state) ||
                    !state.IsActive ||
                    state.EndTime >= currentTime)
                    return false;

                IReadOnlyList<KingdomQuestMembershipEntry> source;
                if (!KingdomQuestMembershipRegistry.TryGet(handle, out source))
                    return false;

                var members = source.Select(v => v.Clone()).ToList();
                if (state.TargetIndex < 0 ||
                    state.TargetIndex >= members.Count)
                    return false;

                KingdomQuestMembershipEntry target =
                    members[state.TargetIndex];

                DataProvider provider = DataProvider.Instance;
                if (provider == null ||
                    provider.KingdomQuestVoteMajorityRates == null)
                    return false;

                byte requiredRate = 0;
                if (provider.KingdomQuestVoteMajorityRates.Count != 0)
                {
                    int rateIndex = target.VotingCount;
                    if (rateIndex >=
                            provider.KingdomQuestVoteMajorityRates.Count)
                        rateIndex =
                            provider.KingdomQuestVoteMajorityRates.Count - 1;
                    requiredRate =
                        provider.KingdomQuestVoteMajorityRates[rateIndex];
                }

                byte yesRate = 0;
                if (state.YesCount != 0)
                {
                    int decided =
                        (int)state.YesCount + (int)state.NoCount;
                    yesRate = (byte)(
                        ((int)state.YesCount * 100) / decided);
                }

                bool passed = yesRate >= requiredRate;
                unchecked { target.VotingCount++; }

                var audience = new List<uint>();
                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i].TeamType != state.TeamType)
                        continue;
                    members[i].InVoteRaw = 0;
                    audience.Add(members[i].CharacterNumber);
                }

                if (passed)
                    target.BanRaw = 1;

                if (!KingdomQuestSessionCoordinator.TrySetMembership(
                        handle, members))
                    return false;

                resolution = new KingdomQuestVoteResolution(
                    passed,
                    requiredRate,
                    yesRate,
                    state.YesCount,
                    state.NoCount,
                    state.CancelCount,
                    target.CharacterNumber,
                    target.Name,
                    audience);

                ByHandle.Remove(handle);
                return true;
            }
        }

        /// <summary>
        /// Models the vote branch inside CKQServer::PlayerDisjoin.
        /// Only disjoining the active vote target cancels the vote, and a
        /// nonzero target bBan suppresses that cancellation. Native clears
        /// every current joiner's bInVote, sends VOTE_CANCEL_CMD to every
        /// other represented joiner, then clears KQ_VOTE_INFO before the
        /// normal membership removal continues.
        /// </summary>
        public static bool TryCancelForTargetDisjoin(
            uint handle,
            uint characterNumber,
            out KingdomQuestVoteCancelPlan plan)
        {
            plan = null;
            lock (Sync)
            {
                KingdomQuestVoteState state;
                if (!ByHandle.TryGetValue(handle, out state) ||
                    !state.IsActive)
                    return true;

                IReadOnlyList<KingdomQuestMembershipEntry> source;
                if (!KingdomQuestMembershipRegistry.TryGet(handle, out source))
                    return false;

                var members = source.Select(v => v.Clone()).ToList();
                if (state.TargetIndex < 0 ||
                    state.TargetIndex >= members.Count)
                    return false;

                KingdomQuestMembershipEntry target =
                    members[state.TargetIndex];
                if (target.CharacterNumber != characterNumber ||
                    target.BanRaw != 0)
                    return true;

                var audience = new List<uint>();
                for (int i = 0; i < members.Count; i++)
                {
                    members[i].InVoteRaw = 0;
                    if (members[i].CharacterNumber != characterNumber)
                        audience.Add(members[i].CharacterNumber);
                }

                if (!KingdomQuestSessionCoordinator.TrySetMembership(
                        handle, members))
                    return false;

                ByHandle.Remove(handle);
                plan = new KingdomQuestVoteCancelPlan(
                    target.Name, audience);
                return true;
            }
        }

        /// <summary>
        /// VoteProcessing tests bBan as nonzero before attempting
        /// LINK_TO_FORCE_BY_BAN for a represented live session.
        /// </summary>
        public static bool TryGetBannedMembers(
            uint handle,
            out IReadOnlyList<KingdomQuestMembershipEntry> banned)
        {
            banned = null;
            IReadOnlyList<KingdomQuestMembershipEntry> members;
            if (!KingdomQuestMembershipRegistry.TryGet(handle, out members))
                return false;

            banned = members
                .Where(v => v.BanRaw != 0)
                .Select(v => v.Clone())
                .ToList()
                .AsReadOnly();
            return true;
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ByHandle.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                ByHandle.Clear();
        }
    }
}
