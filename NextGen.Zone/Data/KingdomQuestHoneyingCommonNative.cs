using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestHoneyingBroadcastNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string ScriptFileKey { get; private set; }
        public string MessageKey { get; private set; }
        public bool MessageTextResolved { get; private set; }
        public string MessageText { get; private set; }

        internal KingdomQuestHoneyingBroadcastNativePlan(
            KingdomQuestHoneyingExternalPlan source,
            string messageKey)
        {
            CanonicalLine = source.CanonicalLine;
            TopLevelBlock = source.TopLevelBlock;
            ScriptFileKey = KingdomQuestHoneyingCommonNative.ScriptFileKey;
            MessageKey = messageKey ?? string.Empty;
            KingdomQuestPineScriptFileSource scriptSource;
            string text;
            bool present;
            if (KingdomQuestPineScriptFile.TryGetSource(ScriptFileKey, out scriptSource) &&
                KingdomQuestHoneyingTextSource.TryResolve(
                    scriptSource, MessageKey, out text, out present))
            {
                MessageTextResolved = true;
                MessageText = text;
            }
        }

        public int NoticeVtableOffset
        {
            get { return KingdomQuestPineBroadcastAllNative.NoticeVtableOffset; }
        }

        public byte NativeNoticeHeader
        {
            get { return KingdomQuestPineBroadcastAllNative.NativeNoticeHeader; }
        }

        public byte NativeNoticeType
        {
            get { return KingdomQuestPineBroadcastAllNative.NativeNoticeType; }
        }
    }

    public sealed class KingdomQuestHoneyingChatWindowNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestPineChatWindowLookupPlan NativeLookup
        {
            get;
            private set;
        }

        internal KingdomQuestHoneyingChatWindowNativePlan(
            KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineChatWindowLookupPlan nativeLookup)
        {
            CanonicalLine = source.CanonicalLine;
            TopLevelBlock = source.TopLevelBlock ?? string.Empty;
            NativeLookup = nativeLookup;
        }
    }

    public sealed class KingdomQuestHoneyingRewardNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestPineKqRewardCommandPlan RewardCommand
        {
            get;
            private set;
        }

        internal KingdomQuestHoneyingRewardNativePlan(
            KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineKqRewardCommandPlan rewardCommand)
        {
            CanonicalLine = source.CanonicalLine;
            TopLevelBlock = source.TopLevelBlock;
            RewardCommand = rewardCommand;
        }
    }

    public sealed class KingdomQuestHoneyingQuestMobKillNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public KingdomQuestPineUsedQuestMobKillNativePlan NativeCommand
        {
            get;
            private set;
        }

        internal KingdomQuestHoneyingQuestMobKillNativePlan(
            KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineUsedQuestMobKillNativePlan nativeCommand)
        {
            CanonicalLine = source.CanonicalLine;
            TopLevelBlock = source.TopLevelBlock;
            NativeCommand = nativeCommand;
        }
    }

    /// <summary>
    /// Reuses already-recovered common Pine KQ command semantics for the
    /// exact Honeying source sites.
    ///
    /// Covered here: 8 broadcast, 2 chatwin, 2 linkto, 1 mobregen,
    /// 1 questmobkill, 1 reward, 5 summonmob, 5 npcshout and 6 door actions, 3 door builds
    /// plus 3 effectobj and 3 vanish = all 40 external Honeying occurrences.
    /// Live services remain explicit command-state owner dependencies.
    ///
    /// No map transfer, spawning, player enumeration, quest mutation, reward
    /// persistence or packet send occurs in this projection.
    /// </summary>
    public static class KingdomQuestHoneyingCommonNative
    {
        public const int NativeClosedOccurrenceCount = 40;
        public const int NativeClosedFamilyCount = 13;
        public const int RemainingOccurrenceCount = 0;
        public const int RemainingFamilyCount = 0;

        public const string ScriptFileKey = "KQHoneying";
        public const string ScriptFilePath = "Script/KQHoneying.txt";
        public const string ScriptFileSha256 =
            "3438e2d0144619b52fd4f66d7ea3f9b6384a0d67c76dbf712641b61d6bfdcc30";

        public const string RuntimeHandleIdentifier = "Boss";
        public const ushort BossMobId = 1129;
        public const string BossMobIndex = "KQ_H_GHoneying";
        public const string BossMobDisplayName = "Giant Honeying";
        public const ushort SummonMobId = 1128;
        public const string SummonMobIndex = "KQ_H_Honeying";
        public const string SummonMobDisplayName = "Shadow Honeying";

        public const ushort ElderineMapId = 9;
        public const string ElderineMapName = "Eld";
        public const int ElderineX = 17214;
        public const int ElderineY = 13445;

        private static readonly Dictionary<int, string> BroadcastKeys =
            new Dictionary<int, string>
            {
                { 183, "KQReturn30" },
                { 185, "KQReturn20" },
                { 187, "KQReturn10" },
                { 189, "KQReturn5" },
                { 197, "KQReturn30" },
                { 199, "KQReturn20" },
                { 201, "KQReturn10" },
                { 203, "KQReturn5" },
            };

        private static readonly Dictionary<int, int> SummonCounts =
            new Dictionary<int, int>
            {
                { 161, 2 },
                { 165, 4 },
                { 169, 8 },
                { 173, 10 },
                { 174, 10 },
            };

        public static bool TryBuildBroadcast(
            KingdomQuestHoneyingExternalPlan source,
            out KingdomQuestHoneyingBroadcastNativePlan plan)
        {
            plan = null;
            string key;
            KingdomQuestPineScriptFileSource scriptSource;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.Broadcast ||
                !BroadcastKeys.TryGetValue(source.CanonicalLine, out key) ||
                !KingdomQuestPineBroadcastAllNative.IsAllTarget("all") ||
                !KingdomQuestPineScriptFile.TryGetSource(
                    ScriptFileKey, out scriptSource) ||
                scriptSource == null ||
                !string.Equals(
                    scriptSource.RelativePath,
                    ScriptFilePath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    scriptSource.Sha256,
                    ScriptFileSha256,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestHoneyingBroadcastNativePlan(source, key);
            return true;
        }

        public static bool TryBuildChatWindow(
            KingdomQuestHoneyingExternalPlan source,
            DataProvider data,
            out KingdomQuestHoneyingChatWindowNativePlan plan)
        {
            plan = null;
            string recordKey;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.ChatWin ||
                !string.Equals(
                    source.TopLevelBlock,
                    "TopFloor",
                    StringComparison.Ordinal))
                return false;

            switch (source.CanonicalLine)
            {
                case 140:
                    recordKey = "Honeying01";
                    break;
                case 142:
                    recordKey = "Honeying02";
                    break;
                default:
                    return false;
            }

            KingdomQuestPineChatWindowLookupPlan nativeLookup;
            if (!KingdomQuestPineChatWindowNative.TryBuildLookup(
                    BossMobIndex,
                    BossMobId,
                    ScriptFileKey,
                    ScriptFilePath,
                    ScriptFileSha256,
                    recordKey,
                    data,
                    out nativeLookup) ||
                nativeLookup == null)
                return false;

            plan = new KingdomQuestHoneyingChatWindowNativePlan(
                source, nativeLookup);
            return true;
        }

        public static bool TryBuildLinkTo(
            KingdomQuestHoneyingExternalPlan source,
            out KingdomQuestPineLinkToOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.LinkTo ||
                !((source.CanonicalLine == 191 &&
                    string.Equals(
                        source.TopLevelBlock,
                        "QuestSuccess",
                        StringComparison.Ordinal)) ||
                   (source.CanonicalLine == 205 &&
                    string.Equals(
                        source.TopLevelBlock,
                        "QuestFail",
                        StringComparison.Ordinal))))
                return false;

            plan = new KingdomQuestPineLinkToOwnerPlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                "all",
                ElderineMapId,
                ElderineMapName,
                ElderineMapName,
                ElderineX,
                ElderineY);
            return true;
        }

        public static bool TryBuildMobRegen(
            KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineTokenValue runtimeHandleToken,
            DataProvider data,
            out KingdomQuestPineMobRegenOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.MobRegen ||
                source.CanonicalLine != 139 ||
                !string.Equals(
                    source.TopLevelBlock,
                    "TopFloor",
                    StringComparison.Ordinal) ||
                runtimeHandleToken == null ||
                runtimeHandleToken.SnapshotNativeBytes().Length !=
                    KingdomQuestPineTokenValue.NativeByteCapacity ||
                !ValidateMob(BossMobIndex, BossMobId, data))
                return false;

            plan = new KingdomQuestPineMobRegenOwnerPlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                RuntimeHandleIdentifier,
                runtimeHandleToken,
                BossMobId,
                BossMobIndex,
                BossMobDisplayName,
                7081,
                5972,
                90,
                1000,
                "Normal");
            return true;
        }

        public static bool TryBuildSummonMob(
            KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineTokenValue runtimeHandleToken,
            DataProvider data,
            out KingdomQuestPineSummonMobOwnerPlan plan)
        {
            plan = null;
            int count;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.SummonMob ||
                !SummonCounts.TryGetValue(source.CanonicalLine, out count) ||
                runtimeHandleToken == null ||
                runtimeHandleToken.SnapshotNativeBytes().Length !=
                    KingdomQuestPineTokenValue.NativeByteCapacity ||
                !ValidateMob(SummonMobIndex, SummonMobId, data))
                return false;

            plan = new KingdomQuestPineSummonMobOwnerPlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                RuntimeHandleIdentifier,
                runtimeHandleToken,
                SummonMobId,
                SummonMobIndex,
                SummonMobDisplayName,
                count);
            return true;
        }

        public static bool TryBuildQuestMobKill(
            KingdomQuestHoneyingExternalPlan source,
            DataProvider data,
            out KingdomQuestHoneyingQuestMobKillNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.QuestMobKill ||
                source.CanonicalLine != 182 ||
                !string.Equals(
                    source.TopLevelBlock,
                    "QuestSuccess",
                    StringComparison.Ordinal))
                return false;

            KingdomQuestPineUsedQuestMobKillNativePlan nativeCommand;
            if (!KingdomQuestPineUsedQuestMobKillNative.TryBuild(
                    2668,
                    "Daliy_Check",
                    1,
                    data,
                    out nativeCommand) ||
                nativeCommand == null)
                return false;

            plan = new KingdomQuestHoneyingQuestMobKillNativePlan(
                source, nativeCommand);
            return true;
        }

        public static bool TryBuildReward(
            KingdomQuestHoneyingExternalPlan source,
            uint currentKingdomQuestHandle,
            out KingdomQuestHoneyingRewardNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.Reward ||
                source.CanonicalLine != 181 ||
                !string.Equals(
                    source.TopLevelBlock,
                    "QuestSuccess",
                    StringComparison.Ordinal))
                return false;

            KingdomQuestPineKqRewardCommandPlan rewardCommand;
            if (!KingdomQuestPineKqRewardCommandNative.TryBuild(
                    currentKingdomQuestHandle,
                    out rewardCommand) ||
                rewardCommand == null ||
                rewardCommand.Handle != currentKingdomQuestHandle)
                return false;

            plan = new KingdomQuestHoneyingRewardNativePlan(
                source, rewardCommand);
            return true;
        }

        private static bool ValidateMob(
            string inxName,
            ushort expectedId,
            DataProvider data)
        {
            if (data == null ||
                data.MobsByName == null ||
                data.MobData == null)
                return false;

            MobInfo clientInfo;
            MobInfoServer serverInfo;
            return data.MobsByName.TryGetValue(inxName, out clientInfo) &&
                clientInfo != null &&
                data.MobData.TryGetValue(inxName, out serverInfo) &&
                serverInfo != null &&
                clientInfo.ID == expectedId &&
                serverInfo.ID == expectedId;
        }
    }
}
