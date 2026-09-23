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
    /// complete native client definition, the exact joiner names, source-backed
    /// MapID and emulator-internal Map.InstanceID.
    /// </summary>
    public static class KingdomQuestSessionCoordinator
    {
        private static readonly object Sync = new object();

        public static bool TryCreate(KingdomQuestClientInfo definition,
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
                KingdomQuestClientInfo existingDefinition;
                KingdomQuestInstanceWireState existingState;
                KingdomQuestSessionTarget existingTarget;
                if (KingdomQuestDefinitionRegistry.TryGet(definition.Handle, out existingDefinition) ||
                    KingdomQuestInstanceRegistry.TryGet(definition.Handle, out existingState) ||
                    KingdomQuestSessionTargetRegistry.TryGet(definition.Handle, out existingTarget))
                    return false;

                KingdomQuestSessionTarget target;
                if (!KingdomQuestSessionTargetRegistry.TryCreate(
                        definition.Handle, mapId, mapInstance, out target))
                    return false;

                try
                {
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
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                if (!KingdomQuestDefinitionRegistry.TryGet(handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state))
                    return false;

                byte oldStatus = definition.Status;
                definition.Status = status;
                KingdomQuestDefinitionRegistry.Upsert(definition);
                if (KingdomQuestInstanceRegistry.SetStatus(handle, status))
                    return true;

                definition.Status = oldStatus;
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
                KingdomQuestClientInfo definition;
                KingdomQuestInstanceWireState state;
                IReadOnlyList<KingdomQuestJoinCharacterInfo> oldRoster;
                if (!KingdomQuestDefinitionRegistry.TryGet(handle, out definition) ||
                    !KingdomQuestInstanceRegistry.TryGet(handle, out state) ||
                    !KingdomQuestParticipantRegistry.TryGet(handle, out oldRoster))
                    return false;

                ushort oldCount = definition.NumOfJoiner;
                definition.NumOfJoiner = (ushort)roster.Count;
                KingdomQuestDefinitionRegistry.Upsert(definition);
                KingdomQuestParticipantRegistry.Set(handle, roster);

                List<string> names = roster.Select(v => v.Name).ToList();
                if (KingdomQuestInstanceRegistry.SetJoiners(handle, names))
                    return true;

                definition.NumOfJoiner = oldCount;
                KingdomQuestDefinitionRegistry.Upsert(definition);
                KingdomQuestParticipantRegistry.Set(handle, oldRoster);
                KingdomQuestInstanceRegistry.SetJoiners(
                    handle, oldRoster.Select(v => v.Name));
                return false;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
            {
                bool definition = KingdomQuestDefinitionRegistry.Remove(handle);
                bool state = KingdomQuestInstanceRegistry.Remove(handle);
                bool participants = KingdomQuestParticipantRegistry.Remove(handle);
                bool target = KingdomQuestSessionTargetRegistry.Remove(handle);
                return definition || state || participants || target;
            }
        }
    }
}
