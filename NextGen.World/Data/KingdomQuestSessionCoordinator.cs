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
            IEnumerable<string> joinerNames, ushort mapId, short mapInstance)
        {
            if (definition == null || joinerNames == null)
                return false;

            List<string> names = joinerNames.ToList();
            if (names.Count > ushort.MaxValue ||
                definition.NumOfJoiner != (ushort)names.Count)
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
                    KingdomQuestSessionTargetRegistry.Remove(definition.Handle);
                    throw;
                }
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
            {
                bool definition = KingdomQuestDefinitionRegistry.Remove(handle);
                bool state = KingdomQuestInstanceRegistry.Remove(handle);
                bool target = KingdomQuestSessionTargetRegistry.Remove(handle);
                return definition || state || target;
            }
        }
    }
}
