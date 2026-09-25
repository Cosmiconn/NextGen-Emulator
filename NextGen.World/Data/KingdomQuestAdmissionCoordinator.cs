using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;
using NextGen.World.Networking;

namespace NextGen.World.Data
{
    /// <summary>
    /// Atomic World-side membership mutations recovered from
    /// CKQServer::PlayerJoin / PlayerDisjoin.
    ///
    /// Network broadcasts remain in Handler22 so this class never invents
    /// packet audience or ordering.
    /// </summary>
    public static class KingdomQuestAdmissionCoordinator
    {
        private static readonly object Sync = new object();

        public static bool TryGetPreJoinError(
            WorldClient client, uint requestedHandle, out ushort error)
        {
            error = 0;
            if (client == null || client.Character == null ||
                client.Character.Character == null ||
                !client.Character.Character.PrisonMinutes.HasValue)
                return false;

            short prisonMinutes =
                client.Character.Character.PrisonMinutes.Value;
            bool alreadyInRequestedKq =
                client.KingdomQuestHandle.HasValue &&
                client.KingdomQuestHandle.Value == requestedHandle;

            KingdomQuestAdmissionRules.TryGetPreJoinError(
                unchecked((ushort)prisonMinutes),
                alreadyInRequestedKq,
                out error);
            return true;
        }

        /// <summary>
        /// Mirrors the mutation portion of CKQServer::PlayerDisjoin.
        /// The caller must perform the proven Zone broadcast and join-list
        /// broadcast before calling CompleteDisjoin, matching native order.
        /// </summary>
        public static bool TryRemoveCurrentMembership(
            WorldClient client, out uint handle, out uint characterNumber)
        {
            handle = 0;
            characterNumber = 0;
            if (client == null || !client.KingdomQuestHandle.HasValue ||
                !KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                    client.Character, out characterNumber))
                return false;

            lock (Sync)
            {
                handle = client.KingdomQuestHandle.Value;

                IReadOnlyList<KingdomQuestMembershipEntry> current;
                if (!KingdomQuestMembershipRegistry.TryGet(handle, out current))
                    return false;

                var updated = current.Select(v => v.Clone()).ToList();
                uint targetCharacterNumber = characterNumber;
                int index = updated.FindIndex(
                    v => v.CharacterNumber == targetCharacterNumber);
                if (index < 0)
                    return false;

                updated.RemoveAt(index);
                return KingdomQuestSessionCoordinator.TrySetMembership(
                    handle, updated);
            }
        }

        public static void CompleteDisjoin(WorldClient client, uint handle)
        {
            if (client == null)
                return;

            lock (Sync)
            {
                if (client.KingdomQuestHandle.HasValue &&
                    client.KingdomQuestHandle.Value == handle)
                    client.KingdomQuestHandle = null;
            }
        }

        /// <summary>
        /// Mirrors CKQServer::PlayerJoin after the request handler has run the
        /// native prison/same-handle precheck and PlayerDisjoin sequence.
        ///
        /// Returns false only when emulator-owned authoritative state is
        /// incomplete/inconsistent; in that case callers must fail closed
        /// instead of manufacturing a native Error.
        /// </summary>
        public static bool TryAddMembership(
            WorldClient client, uint handle, out ushort error)
        {
            error = 0;
            if (client == null || client.Character == null ||
                client.Character.Character == null)
                return false;

            lock (Sync)
            {
                KingdomQuestProtocolInfo definition;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out definition))
                {
                    error = KingdomQuestNativeConstants.JoinInvalidHandle;
                    return true;
                }

                IReadOnlyList<KingdomQuestMembershipEntry> current;
                if (!KingdomQuestMembershipRegistry.TryGet(handle, out current))
                    return false;

                byte level = client.Character.Character.CharLevel;
                byte characterClass = client.Character.Character.Job;
                byte gender = Convert.ToByte(
                    client.Character.Character.LookInfo.Male);

                error = KingdomQuestAdmissionRules.EvaluatePlayerJoin(
                    definition,
                    current.Count,
                    level,
                    characterClass,
                    gender);
                if (error != KingdomQuestNativeConstants.JoinSuccess)
                    return true;

                uint characterNumber;
                if (!KingdomQuestCharacterIdentity.TryGetCharacterNumber(
                        client.Character, out characterNumber))
                    return false;

                byte team0 = 0;
                byte team1 = 0;
                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i].TeamType == 0)
                        team0++;
                    else if (current[i].TeamType == 1)
                        team1++;
                }

                KingdomQuestTeamInfo team = null;
                DataProvider provider = DataProvider.Instance;
                if (provider != null && provider.KingdomQuestTeams != null)
                    provider.KingdomQuestTeams.TryGetValue(
                        definition.ID, out team);

                byte teamType = KingdomQuestAdmissionRules.AssignInitialTeam(
                    team, ref team0, ref team1);

                var updated = current.Select(v => v.Clone()).ToList();
                updated.Add(new KingdomQuestMembershipEntry
                {
                    CharacterNumber = characterNumber,
                    Level = level,
                    Class = characterClass,
                    Name = client.Character.Character.Name,
                    TeamType = teamType,
                    // PlayerJoin initializes bInVote, bBan and
                    // nVotingCount independently to zero.
                    InVoteRaw = 0,
                    BanRaw = 0,
                    VotingCount = 0,
                });

                if (!KingdomQuestSessionCoordinator.TrySetMembership(
                        handle, updated))
                    return false;

                client.KingdomQuestHandle = handle;
                error = KingdomQuestNativeConstants.JoinSuccess;
                return true;
            }
        }
    }
}
