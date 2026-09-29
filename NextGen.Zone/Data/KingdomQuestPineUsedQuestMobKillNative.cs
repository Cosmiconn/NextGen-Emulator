using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestPineUsedQuestMobKillNativePlan
    {
        public ushort QuestId { get; private set; }
        public string MobIndex { get; private set; }
        public ushort MobId { get; private set; }
        public int RepeatOperand { get; private set; }

        internal KingdomQuestPineUsedQuestMobKillNativePlan(
            ushort questId,
            string mobIndex,
            ushort mobId,
            int repeatOperand)
        {
            QuestId = questId;
            MobIndex = mobIndex ?? string.Empty;
            MobId = mobId;
            RepeatOperand = repeatOperand;
        }
    }

    /// <summary>
    /// Shared native projection of the source-used
    /// questmobkill 2668 "Daliy_Check" 1 form.
    ///
    /// This exact form is used by both UnderHall and UnderHall2. The native
    /// command scans Player type 2, skips so_IsEmpty, obtains CQuestZone and
    /// selects direct QuestPlayer_ScriptMobKill because QuestID != 0xFFFF.
    ///
    /// This class resolves only the source/data identity and loop bounds. It
    /// performs no player enumeration and no CQuestZone mutation.
    /// </summary>
    public static class KingdomQuestPineUsedQuestMobKillNative
    {
        public const uint ShineQuestMobKillStepAddress = 0x004F8270u;
        public const uint AxialListNearScanObjectTypeCtorAddress = 0x004C1B40u;
        public const uint QuestPlayerScriptMobKillAddress = 0x005C0B40u;
        public const uint QuestPlayerScriptMobKillAllAddress = 0x005C0CF0u;

        public const byte NativePlayerObjectType =
            (byte)KingdomQuestPineNativeObjectType.Player;
        public const byte NativePlayerScanFlag = 0;
        public const int IsEmptyVtableOffset = 0x300;
        public const int GetQuestZoneVtableOffset = 0x808;

        public const ushort AllQuestId = 0xFFFF;
        public const ushort UsedQuestId = 2668;
        public const string UsedMobIndex = "Daliy_Check";
        public const ushort UsedMobId = 50000;
        public const int UsedRepeatOperand = 1;

        public static bool TryBuild(
            int questId,
            string mobIndex,
            int repeatOperand,
            DataProvider data,
            out KingdomQuestPineUsedQuestMobKillNativePlan plan)
        {
            plan = null;
            if (questId != UsedQuestId ||
                repeatOperand != UsedRepeatOperand ||
                !string.Equals(
                    mobIndex, UsedMobIndex, StringComparison.Ordinal) ||
                data == null ||
                data.MobsByName == null ||
                data.MobData == null)
                return false;

            MobInfo clientInfo;
            MobInfoServer serverInfo;
            if (!data.MobsByName.TryGetValue(UsedMobIndex, out clientInfo) ||
                clientInfo == null ||
                !data.MobData.TryGetValue(UsedMobIndex, out serverInfo) ||
                serverInfo == null ||
                clientInfo.ID != UsedMobId ||
                serverInfo.ID != UsedMobId ||
                clientInfo.ID != unchecked((ushort)serverInfo.ID))
                return false;

            plan = new KingdomQuestPineUsedQuestMobKillNativePlan(
                UsedQuestId,
                UsedMobIndex,
                UsedMobId,
                UsedRepeatOperand);
            return true;
        }

        public static bool TryGetEffectiveRepeatCount(
            int repeatOperand,
            int selectedPlayerCount,
            out int effectiveRepeatCount)
        {
            effectiveRepeatCount = 0;
            if (repeatOperand != UsedRepeatOperand ||
                selectedPlayerCount < 0)
                return false;

            effectiveRepeatCount =
                selectedPlayerCount < repeatOperand
                    ? selectedPlayerCount
                    : repeatOperand;
            return true;
        }

        public static bool UsesDirectQuestVariant(ushort questId)
        {
            return questId != AllQuestId && questId == UsedQuestId;
        }
    }
}
