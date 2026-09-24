using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Atomically wires an already-defined KQ session into the three World
    /// registries used by list/status/transfer paths.
    ///
    /// It intentionally allocates and schedules nothing. The caller supplies a
    /// complete native 377-byte definition, the exact joiner roster, source-backed
    /// MapID and emulator-internal Map.InstanceID.
    /// </summary>
    public static class KingdomQuestSessionCoordinator
    {
        private static readonly object Sync = new object();

        public static bool TryCreate(KingdomQuestProtocolInfo definition,
            IEnumerable<KingdomQuestJoinCharacterInfo> participants,
            ushort mapId, short mapInstance)
        {
            if (definition == null || participants == null)
                return false;

            List<KingdomQuestJoinCharacterInfo> roster = participants.ToList();
            if (roster.Count > byte.MaxValue ||
                definition.NumOfJoiner != (ushort)roster.Count)
                return false;

            List<string> names = roster.Select(v => v == null ? null : v.Name).ToList();
            if (names.Any(v => v == null))
                return false;

            lock (Sync)
            {
                KingdomQuestProtocolInfo existingProtocolDefinition;
                KingdomQuestClientInfo existingDefinition;
                KingdomQuestInstanceWireState existingState;
                KingdomQuestSessionTarget existingTarget;
                if (KingdomQuestProtocolDefinitionRegistry.TryGet(
                        definition.Handle, out existingProtocolDefinition) ||
                    KingdomQuestDefinitionRegistry.TryGet(definition.Handle, out existingDefinition) ||
                    KingdomQuestInstanceRegistry.TryGet(definition.Handle, out existingState) ||
                    KingdomQuestSessionTargetRegistry.TryGet(definition.Handle, out existingTarget))
                    return false;

                KingdomQuestSessionTarget target;
                if (!KingdomQuestSessionTargetRegistry.TryCreate(
                        definition.Handle, mapId, mapInstance, out target))
                    return false;

                try
                {
                    KingdomQuestProtocolDefinitionRegistry.Upsert(definition);
                    KingdomQuestDefinitionRegistry.Upsert(definition);
                    KingdomQuestInstanceRegistry.Upsert(
                        definition.Handle,
                        definition.Status,
                        definition.ID,
                        definition.MinLevel,
                        definition.MaxLevel);

                    KingdomQuestParticipantRegistry.Set(definition.Handle, roster);

                    if (!KingdomQuestInstanceRegistry.SetJoiners(
                            definition.Handle, names))
                        throw new InvalidOperationException(
                            "KQ instance state disappeared during creation.");

                    return true;
                }
                catch
                {
                    KingdomQuestProtocolDefinitionRegistry.Remove(definition.Handle);
                    KingdomQuestDefinitionRegistry.Remove(definition.Handle);
                    KingdomQuestInstanceRegistry.Remove(definition.Handle);
                    KingdomQuestParticipantRegistry.Remove(definition.Handle);
                    KingdomQuestSessionTargetRegistry.Remove(definition.Handle);
                    throw;
                }
            }
        }

        public static bool TrySetStatus(uint handle, byte status)
        {
            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state))
                    return false;

                byte oldStatus = definition.Status;
                protocolDefinition.Status = status;
                definition.Status = status;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(definition);
                if (KingdomQuestInstanceRegistry.SetStatus(handle, status))
                    return true;

                protocolDefinition.Status = oldStatus;
                definition.Status = oldStatus;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(definition);
                return false;
            }
        }

        public static bool TrySetParticipants(uint handle,
            IEnumerable<KingdomQuestJoinCharacterInfo> participants)
        {
            if (participants == null)
                return false;

            List<KingdomQuestJoinCharacterInfo> roster = participants.ToList();
            if (roster.Count > byte.MaxValue ||
                roster.Any(v => v == null || v.Name == null))
                return false;

            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                IReadOnlyList<KingdomQuestJoinCharacterInfo> oldRoster;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    !KingdomQuestParticipantRegistry.TryGet(handle, out oldRoster))
                    return false;

                ushort oldCount = definition.NumOfJoiner;
                protocolDefinition.NumOfJoiner = (ushort)roster.Count;
                definition.NumOfJoiner = (ushort)roster.Count;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(definition);
                KingdomQuestParticipantRegistry.Set(handle, roster);

                List<string> names = roster.Select(v => v.Name).ToList();
                if (KingdomQuestInstanceRegistry.SetJoiners(handle, names))
                {
                    if (roster.Count == 0)
                    {
                        KingdomQuestMembershipRegistry.Set(
                            handle, new KingdomQuestMembershipEntry[0]);
                        KingdomQuestZoneJoinerRegistry.Set(
                            handle, new KingdomQuestZoneJoinerInfo[0]);
                    }
                    else
                    {
                        // A participant-only mutation cannot authoritatively
                        // manufacture native CharacterNumber identities.
                        KingdomQuestMembershipRegistry.Remove(handle);
                        KingdomQuestZoneJoinerRegistry.Remove(handle);
                    }
                    return true;
                }

                protocolDefinition.NumOfJoiner = oldCount;
                definition.NumOfJoiner = oldCount;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(definition);
                KingdomQuestParticipantRegistry.Set(handle, oldRoster);
                KingdomQuestInstanceRegistry.SetJoiners(
                    handle, oldRoster.Select(v => v.Name));
                return false;
            }
        }

        /// <summary>
        /// Atomically publishes the original KQ_JOINER_BF identity into both
        /// client-visible and World->Zone projections. CharacterNumber and
        /// Name/Class/Level are supplied together; no identity lookup occurs.
        /// </summary>
        public static bool TrySetMembership(uint handle,
            IEnumerable<KingdomQuestMembershipEntry> members)
        {
            if (members == null)
                return false;

            List<KingdomQuestMembershipEntry> roster =
                members.Select(v => v == null ? null : v.Clone()).ToList();
            if (roster.Count > byte.MaxValue ||
                roster.Any(v => v == null || v.Name == null))
                return false;

            List<KingdomQuestJoinCharacterInfo> clientRoster =
                roster.Select(v => v.ToClientInfo()).ToList();
            List<KingdomQuestZoneJoinerInfo> zoneRoster =
                roster.Select(v => v.ToZoneInfo()).ToList();
            List<string> names = roster.Select(v => v.Name).ToList();

            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                IReadOnlyList<KingdomQuestJoinCharacterInfo> oldParticipants;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    !KingdomQuestParticipantRegistry.TryGet(
                        handle, out oldParticipants))
                    return false;

                IReadOnlyList<KingdomQuestMembershipEntry> oldMembership;
                bool hadMembership =
                    KingdomQuestMembershipRegistry.TryGet(
                        handle, out oldMembership);
                IReadOnlyList<KingdomQuestZoneJoinerInfo> oldZoneRoster;
                bool hadZoneRoster =
                    KingdomQuestZoneJoinerRegistry.TryGet(
                        handle, out oldZoneRoster);

                ushort oldCount = definition.NumOfJoiner;
                protocolDefinition.NumOfJoiner = (ushort)roster.Count;
                definition.NumOfJoiner = (ushort)roster.Count;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(definition);
                KingdomQuestParticipantRegistry.Set(handle, clientRoster);
                KingdomQuestMembershipRegistry.Set(handle, roster);
                KingdomQuestZoneJoinerRegistry.Set(handle, zoneRoster);

                if (KingdomQuestInstanceRegistry.SetJoiners(handle, names))
                    return true;

                protocolDefinition.NumOfJoiner = oldCount;
                definition.NumOfJoiner = oldCount;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(definition);
                KingdomQuestParticipantRegistry.Set(handle, oldParticipants);
                KingdomQuestInstanceRegistry.SetJoiners(
                    handle, oldParticipants.Select(v => v.Name));

                if (hadMembership)
                    KingdomQuestMembershipRegistry.Set(handle, oldMembership);
                else
                    KingdomQuestMembershipRegistry.Remove(handle);
                if (hadZoneRoster)
                    KingdomQuestZoneJoinerRegistry.Set(handle, oldZoneRoster);
                else
                    KingdomQuestZoneJoinerRegistry.Remove(handle);
                return false;
            }
        }

        /// <summary>
        /// Reproduces the WorldManager DoSetMakeRoom boundary after a scheduled
        /// entry becomes due: allocate the native KingdomQuestMap slot, resolve
        /// its proven MapBase to a source-backed MapID, allocate an independent
        /// emulator Map.InstanceID, publish Status 1, then let the caller send
        /// NC_KQ_W2Z_MAKE_REQ.
        /// </summary>
        public static bool TryPrepareMake(uint handle,
            out KingdomQuestSessionTarget target)
        {
            target = null;
            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo clientDefinition;
                KingdomQuestInstanceWireState state;
                KingdomQuestSessionTarget existingTarget;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out clientDefinition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    KingdomQuestSessionTargetRegistry.TryGet(
                        handle, out existingTarget))
                    return false;

                if (protocolDefinition.Status !=
                        KingdomQuestNativeConstants.StatusScheduled ||
                    clientDefinition.Status !=
                        KingdomQuestNativeConstants.StatusScheduled ||
                    state.Status != KingdomQuestNativeConstants.StatusScheduled)
                    return false;

                DataProvider provider = DataProvider.Instance;
                if (provider == null ||
                    !provider.HasCompleteKingdomQuestMainSource)
                    return false;

                if (!KingdomQuestMapAllocationRegistry.TryAllocate(
                        protocolDefinition,
                        provider.KingdomQuestSourceDefinitions,
                        provider.KingdomQuestSourceMaps))
                {
                    // Native DoSetMakeRoom writes Status 8 when AllocMapLink
                    // cannot reserve all source slots.
                    TrySetStatus(handle, KingdomQuestNativeConstants.StatusNoMap);
                    return false;
                }

                if (!KingdomQuestSessionTargetRegistry.TryAllocateNative(
                        handle, protocolDefinition, out target))
                {
                    KingdomQuestMapAllocationRegistry.Free(handle);
                    target = null;
                    return false;
                }

                protocolDefinition.Status =
                    KingdomQuestNativeConstants.StatusMakeRequested;
                clientDefinition.Status =
                    KingdomQuestNativeConstants.StatusMakeRequested;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(clientDefinition);

                if (KingdomQuestInstanceRegistry.SetStatus(
                        handle, KingdomQuestNativeConstants.StatusMakeRequested))
                    return true;

                KingdomQuestProtocolDefinitionRegistry.Upsert(
                    ResetPreparedMapLinks(
                        protocolDefinition,
                        KingdomQuestNativeConstants.StatusScheduled));
                clientDefinition.Status =
                    KingdomQuestNativeConstants.StatusScheduled;
                KingdomQuestDefinitionRegistry.Upsert(clientDefinition);
                KingdomQuestSessionTargetRegistry.Remove(handle);
                KingdomQuestMapAllocationRegistry.Free(handle);
                target = null;
                return false;
            }
        }

        /// <summary>
        /// Emulator-transport rollback only. Native gameplay does not observe
        /// this path; it is used when the already-prepared MAKE cannot be
        /// delivered to the Zone connection that owns the proven base map.
        /// </summary>
        public static bool TryRollbackMakePreparation(uint handle)
        {
            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo clientDefinition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out clientDefinition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    state.Status != KingdomQuestNativeConstants.StatusMakeRequested)
                    return false;

                KingdomQuestSessionTargetRegistry.Remove(handle);
                KingdomQuestMapAllocationRegistry.Free(handle);

                protocolDefinition = ResetPreparedMapLinks(
                    protocolDefinition,
                    KingdomQuestNativeConstants.StatusScheduled);
                clientDefinition.Status =
                    KingdomQuestNativeConstants.StatusScheduled;
                KingdomQuestProtocolDefinitionRegistry.Upsert(protocolDefinition);
                KingdomQuestDefinitionRegistry.Upsert(clientDefinition);
                return KingdomQuestInstanceRegistry.SetStatus(
                    handle, KingdomQuestNativeConstants.StatusScheduled);
            }
        }

        private static KingdomQuestProtocolInfo ResetPreparedMapLinks(
            KingdomQuestProtocolInfo definition, byte status)
        {
            definition.Status = status;
            definition.MapLink = new KingdomQuestMapProtocolInfo[4];
            for (int i = 0; i < definition.MapLink.Length; i++)
                definition.MapLink[i] = new KingdomQuestMapProtocolInfo();
            return definition;
        }

        /// <summary>
        /// Applies the proven CKQServer::DoSetStart transition from Status 2
        /// into the native ten-second Status-3 countdown.
        /// </summary>
        public static bool TryEnterStartCountdown(uint handle, int currentTime32)
        {
            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    protocolDefinition.Status !=
                        KingdomQuestNativeConstants.StatusJoining ||
                    definition.Status != KingdomQuestNativeConstants.StatusJoining ||
                    state.Status != KingdomQuestNativeConstants.StatusJoining)
                    return false;

                if (!TrySetStatus(
                        handle,
                        KingdomQuestNativeConstants.StatusStartCountdown))
                    return false;

                KingdomQuestDoneSkipRegistry.Remove(handle);
                KingdomQuestStartCountdownRegistry.Set(
                    handle,
                    unchecked(
                        currentTime32 +
                        KingdomQuestNativeConstants.StartCountdownSeconds));
                return true;
            }
        }

        /// <summary>
        /// Applies the recovered Status-3 expiry boundary from DoSetStart:
        /// Status 4 first, then KQTD_RANDOM division, with the combined
        /// membership/Zone roster updated atomically before W2Z START.
        /// Party leave and transport are intentionally performed by the caller
        /// after a complete session/Zone preflight.
        /// </summary>
        public static bool TryEnterRunning(
            uint handle,
            int currentTime32,
            KingdomQuestNativeRandom random,
            out IReadOnlyList<KingdomQuestMembershipEntry> members)
        {
            members = null;
            if (random == null)
                return false;

            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                int countdownEndsAt;
                IReadOnlyList<KingdomQuestMembershipEntry> oldMembership;

                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    !KingdomQuestStartCountdownRegistry.TryGet(
                        handle, out countdownEndsAt) ||
                    !KingdomQuestMembershipRegistry.TryGet(
                        handle, out oldMembership) ||
                    protocolDefinition.Status !=
                        KingdomQuestNativeConstants.StatusStartCountdown ||
                    definition.Status !=
                        KingdomQuestNativeConstants.StatusStartCountdown ||
                    state.Status !=
                        KingdomQuestNativeConstants.StatusStartCountdown ||
                    currentTime32 < countdownEndsAt)
                    return false;

                // DoSetStart writes Status 4 before KQTeam_DivideRandom.
                if (!TrySetStatus(
                        handle, KingdomQuestNativeConstants.StatusRunning))
                    return false;

                var updated = oldMembership
                    .Select(v => v.Clone())
                    .ToList();

                KingdomQuestTeamInfo team = null;
                DataProvider provider = DataProvider.Instance;
                if (provider != null && provider.KingdomQuestTeams != null)
                    provider.KingdomQuestTeams.TryGetValue(
                        protocolDefinition.ID, out team);

                KingdomQuestRandomTeamDivider.Apply(
                    updated, team, random);

                if (!TrySetMembership(handle, updated))
                {
                    TrySetStatus(
                        handle,
                        KingdomQuestNativeConstants.StatusStartCountdown);
                    return false;
                }

                KingdomQuestStartCountdownRegistry.Remove(handle);
                members = updated.Select(v => v.Clone()).ToList().AsReadOnly();
                return true;
            }
        }

        /// <summary>
        /// Applies CKQServer::SetDone's only registry mutation: find the
        /// existing Handle and write Status 5. The original function has no
        /// prior-status gate. DESTROY, FreeMapLink and FreeJoiner are kept in
        /// the Zone-END handler in their original order.
        /// </summary>
        public static bool TrySetDone(uint handle)
        {
            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state))
                    return false;

                return TrySetStatus(
                    handle, KingdomQuestNativeConstants.StatusDone);
            }
        }

        /// <summary>
        /// Applies the source-proven SetDoneSkip Status-6/reason mutation.
        /// The scheduler runtime preserves the recovered side-effect order:
        /// DESTROY, FreeMapLink, notify, FreeJoiner, JOINING_ALARM_END.
        /// DelOldShceduleList owns later Status-11 removal.
        /// </summary>
        public static bool TrySetDoneSkip(uint handle, byte reason)
        {
            if (reason != KingdomQuestNativeConstants.DoneSkipReasonNotReady &&
                reason != KingdomQuestNativeConstants.DoneSkipReasonTeamGap)
                return false;

            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(
                        handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    protocolDefinition.Status !=
                        KingdomQuestNativeConstants.StatusJoining ||
                    definition.Status != KingdomQuestNativeConstants.StatusJoining ||
                    state.Status != KingdomQuestNativeConstants.StatusJoining)
                    return false;

                if (!TrySetStatus(
                        handle, KingdomQuestNativeConstants.StatusDoneSkip))
                    return false;

                KingdomQuestStartCountdownRegistry.Remove(handle);
                KingdomQuestDoneSkipRegistry.Set(handle, reason);
                return true;
            }
        }

        /// <summary>
        /// Applies the original World-side NC_KQ_Z2W_MAKE_ACK branch.
        /// Non-0x0981 ACKs execute SetNoMapBF => Status 8. A successful ACK
        /// may enter SetJoining only from Status 1/2; SetJoining clears the
        /// join roster and leaves the KQ in Status 2.
        ///
        /// The original invalid-status SetJoining branch also destroys/frees
        /// map state. That destructive edge is intentionally not approximated:
        /// an invalid success transition is rejected here.
        /// </summary>
        public static bool TryApplyMakeAck(uint handle, ushort error)
        {
            lock (Sync)
            {
                KingdomQuestProtocolInfo protocolDefinition;
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestProtocolDefinitionRegistry.TryGet(
                        handle, out protocolDefinition) ||
                    !KingdomQuestDefinitionRegistry.TryGet(handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state))
                    return false;

                if (error != KingdomQuestNativeConstants.MakeAckSuccess)
                    return TrySetStatus(handle, KingdomQuestNativeConstants.StatusNoMap);

                if (state.Status != KingdomQuestNativeConstants.StatusMakeRequested &&
                    state.Status != KingdomQuestNativeConstants.StatusJoining)
                    return false;

                IReadOnlyList<KingdomQuestJoinCharacterInfo> oldRoster;
                if (!KingdomQuestParticipantRegistry.TryGet(handle, out oldRoster))
                    return false;

                if (!TrySetParticipants(
                        handle, new KingdomQuestJoinCharacterInfo[0]))
                    return false;

                if (TrySetStatus(handle, KingdomQuestNativeConstants.StatusJoining))
                    return true;

                TrySetParticipants(handle, oldRoster);
                return false;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
            {
                bool protocolDefinition = KingdomQuestProtocolDefinitionRegistry.Remove(handle);
                bool definition = KingdomQuestDefinitionRegistry.Remove(handle);
                bool state = KingdomQuestInstanceRegistry.Remove(handle);
                bool participants = KingdomQuestParticipantRegistry.Remove(handle);
                bool joinListReply = KingdomQuestJoinListReplyRegistry.Remove(handle);
                bool mapContext = KingdomQuestMapContextRegistry.Remove(handle);
                bool zoneJoiners = KingdomQuestZoneJoinerRegistry.Remove(handle);
                bool membership = KingdomQuestMembershipRegistry.Remove(handle);
                bool target = KingdomQuestSessionTargetRegistry.Remove(handle);
                KingdomQuestStartCountdownRegistry.Remove(handle);
                KingdomQuestDoneSkipRegistry.Remove(handle);
                KingdomQuestMapAllocationRegistry.Free(handle);
                return protocolDefinition || definition || state || participants ||
                    joinListReply || mapContext || zoneJoiners || membership || target;
            }
        }
    }

    /// <summary>
    /// Pure admission/team rules recovered from
    /// CParserClient::fc_NC_KQ_JOIN_REQ, CKQServer::PlayerJoin and
    /// CKQ::IsJoinable. Live handlers consume them only through
    /// KingdomQuestAdmissionCoordinator so membership projections stay atomic.
    /// </summary>
    public static class KingdomQuestAdmissionRules
    {
        public static bool TryGetPreJoinError(
            ushort prisonMinutes, bool alreadyInRequestedKq, out ushort error)
        {
            if (prisonMinutes != 0)
            {
                error = KingdomQuestNativeConstants.JoinPrisonRestricted;
                return true;
            }

            if (alreadyInRequestedKq)
            {
                error = KingdomQuestNativeConstants.JoinAlreadyInRequestedKq;
                return true;
            }

            error = 0;
            return false;
        }

        public static ushort EvaluatePlayerJoin(
            KingdomQuestProtocolInfo definition,
            int currentJoiners,
            byte level,
            byte characterClass,
            byte gender)
        {
            if (definition == null)
                return KingdomQuestNativeConstants.JoinInvalidHandle;
            if (currentJoiners < 0)
                throw new ArgumentOutOfRangeException("currentJoiners");
            if (characterClass > 31)
                throw new ArgumentOutOfRangeException("characterClass");
            if (gender > 1)
                throw new ArgumentOutOfRangeException("gender");

            if (currentJoiners >= KingdomQuestNativeConstants.JoinHardCapacity ||
                currentJoiners >= definition.MaxPlayers)
                return KingdomQuestNativeConstants.JoinCapacityReached;

            if (definition.Status != KingdomQuestNativeConstants.StatusJoining)
                return KingdomQuestNativeConstants.JoinWrongStatus;

            if (level < definition.MinLevel || level > definition.MaxLevel)
                return KingdomQuestNativeConstants.JoinLevelRejected;

            ulong demandClass = unchecked((ulong)definition.DemandClass);
            ulong classBit = 1UL << characterClass;
            if ((demandClass & classBit) == 0)
                return KingdomQuestNativeConstants.JoinClassRejected;

            byte genderBit = (byte)(1 << gender);
            if ((definition.DemandGender & genderBit) == 0)
                return KingdomQuestNativeConstants.JoinGenderRejected;

            return KingdomQuestNativeConstants.JoinSuccess;
        }

        public static byte AssignInitialTeam(
            KingdomQuestTeamInfo team, ref byte team0Count, ref byte team1Count)
        {
            if (team == null ||
                team.TeamDivideType != KingdomQuestNativeConstants.UserSelectTeamDivideType)
                return KingdomQuestNativeConstants.NeutralTeamType;

            if (team1Count <= team0Count)
            {
                team1Count++;
                return 1;
            }

            team0Count++;
            return 0;
        }
    }

}
