using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestUnderHall2BroadcastNativePlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string ScriptKey { get; private set; }
        public string MessageKey { get; private set; }
        public bool MessageTextResolved { get; private set; }

        internal KingdomQuestUnderHall2BroadcastNativePlan(
            int canonicalLine,
            string topLevelBlock,
            string scriptKey,
            string messageKey)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            ScriptKey = scriptKey ?? string.Empty;
            MessageKey = messageKey ?? string.Empty;
            MessageTextResolved = false;
        }
    }

    /// <summary>
    /// UnderHall2 source binding for the shared native "broadcast all" branch.
    ///
    /// The native audience/virtual-dispatch/opcode semantics are owned by
    /// KingdomQuestPineBroadcastAllNative. This wrapper proves that all twelve
    /// UnderHall2 broadcast sites use target "all", one of the four KQReturn*
    /// keys and the exact original KQUnderHall2 script-file identity.
    ///
    /// The actual KQUnderHall2 string-table values for those keys are not
    /// currently source-projected in the repository. MessageTextResolved is
    /// therefore deliberately false; this class does not borrow UnderHall's
    /// Elderine strings and does not build/send a notice packet.
    /// </summary>
    public static class KingdomQuestUnderHall2BroadcastNative
    {
        public const int SourceUsedOccurrenceCount = 12;
        public const int SourceDistinctKeyCount = 4;

        public const string ScriptFileKey = "KQUnderHall2";
        public const string ScriptFilePath = "Script/KQUnderHall2.txt";
        public const string ScriptFileSha256 =
            "b03c9f342468385992790621a1fa0f78e4e237790f23fd9e0dbfee33ed067b16";

        private static readonly HashSet<int> BroadcastLines =
            new HashSet<int>
            {
                545, 547, 549, 551,
                563, 565, 567, 569,
                577, 579, 581, 583,
            };

        private static readonly HashSet<string> MessageKeys =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "KQReturn5",
                "KQReturn10",
                "KQReturn20",
                "KQReturn30",
            };

        public static bool TryBuild(
            KingdomQuestUnderHall2ExternalPlan source,
            out KingdomQuestUnderHall2BroadcastNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestUnderHall2ExternalKind.Broadcast ||
                !KingdomQuestPineBroadcastAllNative.IsAllTarget(
                    source.TargetToken) ||
                !BroadcastLines.Contains(source.CanonicalLine) ||
                !MessageKeys.Contains(source.SourceToken))
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

            plan = new KingdomQuestUnderHall2BroadcastNativePlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                ScriptFileKey,
                source.SourceToken);
            return true;
        }
    }
}
