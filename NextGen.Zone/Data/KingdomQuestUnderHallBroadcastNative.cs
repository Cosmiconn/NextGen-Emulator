using System;
using System.Collections.Generic;
using NextGen.FiestaLib;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHallBroadcastNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string ScriptKey { get; private set; }
        public string Message { get; private set; }

        internal KingdomQuestUnderHallBroadcastNativePlan(
            int canonicalLine,
            string topLevelBlock,
            string scriptKey,
            string message)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            ScriptKey = scriptKey ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// Mutation-free native/source projection of the eight KQ/UnderHall
    /// "broadcast all" sites.
    ///
    /// The native all-target branch of ShineBroadcast constructs AxialListWall
    /// with the resolved script message and traverses the current map through
    /// ShineObject::so_AllInMap. AxialListWall::ali_Work dispatches vtable
    /// +0x784; ShinePlayer implements that slot as so_ply_Notice.
    ///
    /// The source messages below come from the original Script/KQUnderHall.txt
    /// whose exact SHA-256 is already carried by KingdomQuestPineScriptFile.
    ///
    /// The player notice opcode family is Header 8 / type 17, but the native
    /// notice body includes a category byte whose value on this KQ path is not
    /// independently recovered. NoticeCategoryByteResolved therefore remains
    /// false and this class deliberately does not build/send a packet.
    /// </summary>
    public static class KingdomQuestUnderHallBroadcastNative
    {
        public const int SourceUsedOccurrenceCount = 8;
        public const int SourceDistinctKeyCount = 4;
        public const string NativeAllTarget = "all";
        public const int NoticeVtableOffset = 0x784;
        public const byte NativeNoticeHeader = 0x08;
        public const byte NativeNoticeType = (byte)SH8Type.GmNotice;
        public const bool NoticeCategoryByteResolved = false;

        public const string ScriptFileKey = "KQUnderHall";
        public const string ScriptFilePath = "Script/KQUnderHall.txt";
        public const string ScriptFileSha256 =
            "9b6dff7ca269bf43437fcb2aa0e34eb46610d35a75c6de6b1478a56c3cfed4c0";

        private static readonly HashSet<int> BroadcastLines =
            new HashSet<int> { 380, 382, 384, 386, 394, 396, 398, 400 };

        private static readonly Dictionary<string, string> MessageByScriptKey =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "KQReturn30", "Move to Elderine in 30 seconds." },
                { "KQReturn20", "Move to Elderine in 20 seconds." },
                { "KQReturn10", "Move to Elderine in 10 seconds." },
                { "KQReturn5", "Move to Elderine in 5 seconds." },
            };

        public static bool TryBuild(
            KingdomQuestUnderHallExternalPlan source,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            out KingdomQuestUnderHallBroadcastNativePlan plan)
        {
            plan = null;
            if (source == null ||
                sourceSite == null ||
                source.Kind != KingdomQuestUnderHallExternalPlanKind.Broadcast ||
                sourceSite.Kind != KingdomQuestUnderHallExternalPlanKind.Broadcast ||
                !BroadcastLines.Contains(sourceSite.CanonicalLine))
                return false;

            KingdomQuestPineScriptFileSource scriptSource;
            if (!KingdomQuestPineScriptFile.TryGetSource(
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

            string message;
            if (!MessageByScriptKey.TryGetValue(source.SourceToken, out message))
                return false;

            plan = new KingdomQuestUnderHallBroadcastNativePlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.SourceToken,
                message);
            return true;
        }

        public static IReadOnlyDictionary<string, string> SnapshotMessages()
        {
            return new Dictionary<string, string>(
                MessageByScriptKey,
                StringComparer.Ordinal);
        }
    }
}
