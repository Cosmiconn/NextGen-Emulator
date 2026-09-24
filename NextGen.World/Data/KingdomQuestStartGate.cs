using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    public enum KingdomQuestStartDecisionKind : byte
    {
        Wait = 0,
        StartCountdown = 1,
        DoneSkip = 2,
    }

    public sealed class KingdomQuestStartDecision
    {
        public KingdomQuestStartDecisionKind Kind { get; private set; }
        public byte DoneSkipReason { get; private set; }

        private KingdomQuestStartDecision(
            KingdomQuestStartDecisionKind kind, byte doneSkipReason)
        {
            Kind = kind;
            DoneSkipReason = doneSkipReason;
        }

        public static KingdomQuestStartDecision Wait()
        {
            return new KingdomQuestStartDecision(
                KingdomQuestStartDecisionKind.Wait, 0);
        }

        public static KingdomQuestStartDecision Countdown()
        {
            return new KingdomQuestStartDecision(
                KingdomQuestStartDecisionKind.StartCountdown, 0);
        }

        public static KingdomQuestStartDecision Skip(byte reason)
        {
            return new KingdomQuestStartDecision(
                KingdomQuestStartDecisionKind.DoneSkip, reason);
        }
    }

    /// <summary>
    /// Pure CKQServer::DoSetStart/KQTeam_CanKQStart decision logic recovered
    /// from the original NA2016 WorldManager executable.
    ///
    /// Status-3 expiry is modeled separately below from the recovered
    /// DoSetStart/KQTeam_DivideRandom sequence.
    /// </summary>
    public static class KingdomQuestStartGate
    {
        public static KingdomQuestStartDecision Evaluate(
            KingdomQuestProtocolInfo definition,
            int currentTime32,
            IReadOnlyList<KingdomQuestMembershipEntry> participants,
            KingdomQuestTeamInfo team)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");
            if (participants == null)
                throw new ArgumentNullException("participants");
            if (definition.Status != KingdomQuestNativeConstants.StatusJoining)
                return KingdomQuestStartDecision.Wait();
            if (definition.NumOfJoiner != participants.Count)
                throw new InvalidOperationException(
                    "KQ participant count diverged from PROTO_KQ_INFO.NumOfJoiner.");

            // Original DoSetStart takes the immediate countdown path when the
            // current joiner count reaches MaxPlayers.
            if (definition.NumOfJoiner == definition.MaxPlayers)
                return KingdomQuestStartDecision.Countdown();

            long deadline =
                (long)definition.StartTime +
                ((long)definition.StartWaitTime * 60L);
            if ((long)currentTime32 < deadline)
                return KingdomQuestStartDecision.Wait();

            if (definition.NumOfJoiner < definition.MinPlayers)
                return KingdomQuestStartDecision.Skip(
                    KingdomQuestNativeConstants.DoneSkipReasonNotReady);

            // KQTeam_CanKQStart only applies the two-team gates when the
            // source KQTeamDivideType is exactly 2. All other types bypass.
            if (team == null ||
                team.TeamDivideType !=
                    KingdomQuestNativeConstants.UserSelectTeamDivideType)
                return KingdomQuestStartDecision.Countdown();

            int team0 = 0;
            int team1 = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] == null)
                    throw new InvalidOperationException(
                        "KQ participant entry is null.");

                if (participants[i].TeamType == 0)
                    team0++;
                else if (participants[i].TeamType == 1)
                    team1++;
            }

            if (team0 == 0 || team1 == 0)
                return KingdomQuestStartDecision.Skip(
                    KingdomQuestNativeConstants.DoneSkipReasonNotReady);

            if (Math.Abs(team0 - team1) > team.MaxMemberGap)
                return KingdomQuestStartDecision.Skip(
                    KingdomQuestNativeConstants.DoneSkipReasonTeamGap);

            return KingdomQuestStartDecision.Countdown();
        }
    }


    /// <summary>
    /// Exact CKQServer::KQTeam_DivideRandom assignment for KQTD_RANDOM (1).
    /// The supplied random source reproduces RandomBox::rb_1000.
    /// </summary>
    public static class KingdomQuestRandomTeamDivider
    {
        public static void Apply(
            IList<KingdomQuestMembershipEntry> members,
            KingdomQuestTeamInfo team,
            KingdomQuestNativeRandom random)
        {
            if (members == null) throw new ArgumentNullException("members");
            if (random == null) throw new ArgumentNullException("random");
            if (team == null ||
                team.TeamDivideType != KingdomQuestNativeConstants.RandomTeamDivideType)
                return;

            int half = members.Count / 2;
            int team0 = 0;
            int team1 = 0;

            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null)
                    throw new InvalidOperationException("KQ membership entry is null.");

                ushort sample = random.Next1000();
                byte selected;
                if (sample < 500)
                {
                    selected = 1;
                    if (team1 >= half && team0 < half)
                        selected = 0;
                }
                else
                {
                    selected = 0;
                    if (team0 >= half && team1 < half)
                        selected = 1;
                }

                members[i].TeamType = selected;
                if (selected == 0)
                    team0++;
                else
                    team1++;
            }
        }
    }

    /// <summary>
    /// World-side record of the executable-proven Status-3 ten-second
    /// countdown deadline. The executable-proven expiry target is Status 4,
    /// followed by random division (type 1), party/raid leave and W2Z START.
    /// </summary>
    public static class KingdomQuestStartCountdownRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, int> EndsAt =
            new Dictionary<uint, int>();

        public static void Set(uint handle, int endsAtTime32)
        {
            lock (Sync)
                EndsAt[handle] = endsAtTime32;
        }

        public static bool TryGet(uint handle, out int endsAtTime32)
        {
            lock (Sync)
                return EndsAt.TryGetValue(handle, out endsAtTime32);
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return EndsAt.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                EndsAt.Clear();
        }
    }

    /// <summary>
    /// Retains the raw SetDoneSkip reason observed at the recovered start gate.
    /// No later cleanup/repeat semantics are inferred from the reason.
    /// </summary>
    public static class KingdomQuestDoneSkipRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, byte> ReasonByHandle =
            new Dictionary<uint, byte>();

        public static void Set(uint handle, byte reason)
        {
            lock (Sync)
                ReasonByHandle[handle] = reason;
        }

        public static bool TryGet(uint handle, out byte reason)
        {
            lock (Sync)
                return ReasonByHandle.TryGetValue(handle, out reason);
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ReasonByHandle.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                ReasonByHandle.Clear();
        }
    }
}
