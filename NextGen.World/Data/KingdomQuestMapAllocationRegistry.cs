using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Process-local reproduction of CKQServer::InitMapTable,
    /// GetEmptyMapLink, AllocMapLink and FreeMapLink.
    ///
    /// The original allocation table is one 10-DWORD row for every
    /// KingdomQuestMap source row, initialized to -1. MapLink values in
    /// KINGDOM_QUEST are zero-based row indices into that source table.
    ///
    /// This registry owns only the native KQ map-slot reservation and fills
    /// PROTO_KQ_INFO.MapLink. It deliberately does not choose an emulator
    /// Map.InstanceID or create a KingdomQuestSessionTarget.
    /// </summary>
    public static class KingdomQuestMapAllocationRegistry
    {
        public const int SlotsPerSourceRow = 10;

        private static readonly object Sync = new object();
        private static uint?[,] allocatedBySourceRow;

        public static bool TryAllocate(
            KingdomQuestProtocolInfo definition,
            IReadOnlyList<KingdomQuestSourceDefinition> definitions,
            IReadOnlyList<KingdomQuestMapSourceRow> maps)
        {
            if (definition == null) throw new ArgumentNullException("definition");
            if (definitions == null) throw new ArgumentNullException("definitions");
            if (maps == null) throw new ArgumentNullException("maps");
            if (definition.MapLink == null || definition.MapLink.Length != 4)
                throw new InvalidOperationException(
                    "PROTO_KQ_INFO requires exactly four native MapLink entries.");

            lock (Sync)
            {
                EnsureTable(maps);

                // CKQServer::AllocMapLink scans m_KQData from the beginning
                // and stops on the first KINGDOM_QUEST row whose ID matches
                // PROTO_KQ_INFO.ID. This matters for [A]/[B]/... source rows
                // that intentionally share one KQ ID.
                KingdomQuestSourceDefinition source = null;
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i].ID == definition.ID)
                    {
                        source = definitions[i];
                        break;
                    }
                }

                if (source == null)
                    return false;

                for (int linkIndex = 0; linkIndex < 4; linkIndex++)
                {
                    short sourceMapIndex = source.MapLinkColumns[linkIndex];
                    if (sourceMapIndex == -1)
                        continue;

                    if (sourceMapIndex < 0 || sourceMapIndex >= maps.Count)
                    {
                        FreeLocked(definition.Handle);
                        return false;
                    }

                    KingdomQuestMapSourceRow map = maps[sourceMapIndex];
                    if (map.SourceRow != (uint)sourceMapIndex ||
                        map.MapColumns == null ||
                        map.MapColumns.Count != SlotsPerSourceRow ||
                        map.ClearColumns == null ||
                        map.ClearColumns.Count != SlotsPerSourceRow ||
                        map.NumOfMap > SlotsPerSourceRow)
                    {
                        FreeLocked(definition.Handle);
                        return false;
                    }

                    int slot = GetEmptyMapLinkLocked(sourceMapIndex, map);
                    if (slot < 0)
                    {
                        // Native AllocMapLink calls FreeMapLink(Handle) if any
                        // one of the four requested rows has no free slot.
                        FreeLocked(definition.Handle);
                        return false;
                    }

                    byte clear = unchecked((byte)map.ClearColumns[slot]);
                    if (clear != 0)
                        allocatedBySourceRow[sourceMapIndex, slot] =
                            definition.Handle;

                    definition.MapLink[linkIndex] =
                        new KingdomQuestMapProtocolInfo
                        {
                            MapIndex = (byte)slot,
                            MapBase = map.BaseMap ?? string.Empty,
                            MapName = map.MapColumns[slot] ?? string.Empty,
                            MapClear = clear,
                        };
                }

                return true;
            }
        }

        public static void Free(uint handle)
        {
            lock (Sync)
            {
                if (allocatedBySourceRow == null)
                    return;
                FreeLocked(handle);
            }
        }

        public static bool TryGetOwner(
            int sourceMapIndex, int slot, out uint handle)
        {
            lock (Sync)
            {
                handle = 0;
                if (allocatedBySourceRow == null ||
                    sourceMapIndex < 0 ||
                    sourceMapIndex >= allocatedBySourceRow.GetLength(0) ||
                    slot < 0 ||
                    slot >= SlotsPerSourceRow)
                    return false;

                uint? owner = allocatedBySourceRow[sourceMapIndex, slot];
                if (!owner.HasValue)
                    return false;

                handle = owner.Value;
                return true;
            }
        }

        public static void Clear()
        {
            lock (Sync)
                allocatedBySourceRow = null;
        }

        private static int GetEmptyMapLinkLocked(
            int sourceMapIndex, KingdomQuestMapSourceRow map)
        {
            // Original GetEmptyMapLink walks only NumOfMap slots. A source
            // Clear byte of zero is immediately reusable and bypasses the
            // allocation-table ownership test. Otherwise the slot must be -1.
            for (int slot = 0; slot < map.NumOfMap; slot++)
            {
                if (map.ClearColumns[slot] == 0 ||
                    !allocatedBySourceRow[sourceMapIndex, slot].HasValue)
                    return slot;
            }

            return -1;
        }

        private static void EnsureTable(
            IReadOnlyList<KingdomQuestMapSourceRow> maps)
        {
            if (allocatedBySourceRow != null)
            {
                if (allocatedBySourceRow.GetLength(0) != maps.Count)
                    throw new InvalidOperationException(
                        "KQ map source row count changed after allocation-table initialization.");
                return;
            }

            allocatedBySourceRow =
                new uint?[maps.Count, SlotsPerSourceRow];

            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].SourceRow != (uint)i)
                    throw new InvalidOperationException(
                        "KQ map source rows must retain contiguous zero-based ordinals.");
            }
        }

        private static void FreeLocked(uint handle)
        {
            for (int row = 0; row < allocatedBySourceRow.GetLength(0); row++)
            {
                for (int slot = 0; slot < SlotsPerSourceRow; slot++)
                {
                    if (allocatedBySourceRow[row, slot] == handle)
                        allocatedBySourceRow[row, slot] = null;
                }
            }
        }
    }
}
