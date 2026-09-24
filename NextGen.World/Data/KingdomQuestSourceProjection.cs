using System;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Applies only statically proven source fields from KINGDOM_QUEST to an
    /// already-owned native PROTO_KQ_INFO instance.
    ///
    /// Deliberately excluded here: Handle/Status/joiners, ST_* time expansion,
    /// DemandClass/UseClass conversion, MapLink allocation, schedule/run state
    /// and team/PvP data.
    /// </summary>
    public static class KingdomQuestSourceProjection
    {
        public static void ApplyProvenStaticFields(
            KingdomQuestSourceDefinition source,
            KingdomQuestProtocolInfo target)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("target");
            if (source.ID < 0) throw new InvalidOperationException("Negative KQ source ID.");

            target.ID = (ushort)source.ID;
            target.Title = source.Title ?? string.Empty;
            target.LimitTime = source.LimitTime;
            target.StartWaitTime = source.StartWaitTime;
            target.MinLevel = source.MinLevel;
            target.MaxLevel = source.MaxLevel;
            target.MinPlayers = source.MinPlayers;
            target.MaxPlayers = source.MaxPlayers;
            target.PlayerRepeatMode = source.PlayerRepeatMode;
            target.PlayerRepeatCount = source.PlayerRepeatCount;
            target.PlayerRevivalMode = source.PlayerRevivalMode;
            target.PlayerRevivalCount = source.PlayerRevivalCount;
            target.DemandQuest = source.DemandQuest;
            target.DemandItem = source.DemandItem;

            // WorldManager.exe 0x45580F..0x45582F (CKQServer::AddNewScheduleList)
            // reads both signed SHN bytes as raw bytes and packs the protocol
            // field as (Undefined3 * 2) + DemandGender, with byte overflow.
            target.DemandGender = unchecked((byte)(
                unchecked((byte)source.Undefined3) * 2 +
                unchecked((byte)source.DemandGender)));

            target.NextStartMode = source.NextStartMode;

            // Original 2016 PDB table-header member:
            // KINGDOM_QUEST::NextStartDeleyMin (source spelling)
            // Native protocol member at the corresponding scheduler field:
            // PROTO_KQ_INFO::NextStartDelayMin.
            target.NextStartDelayMin = source.NextStartDeleyMin;

            target.RepeatMode = source.RepeatMode;
            target.RepeatCount = source.RepeatCount;
            // The original AddNewScheduleList reads only WORD [KINGDOM_QUEST+0x62].
            target.RewardIndex = unchecked((ushort)source.RewardIndex);
            target.DemandMobKill = source.DemandMobKill;
            target.ScriptLanguage = source.ScriptLanguage ?? string.Empty;

            // Original source member InitValue corresponds to the native
            // PROTO_KQ_INFO ScriptInitValue slot adjacent to ScriptLanguage.
            target.ScriptInitValue = source.InitValue ?? string.Empty;
        }
    }
}
