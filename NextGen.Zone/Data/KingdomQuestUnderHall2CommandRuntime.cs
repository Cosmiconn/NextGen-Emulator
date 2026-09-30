using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Script-specific command-state boundary for the six external command
    /// families used by KQ/UnderHall2.
    /// </summary>
    public interface IKingdomQuestUnderHall2CommandSink
    {
        bool TryStep(
            string commandText,
            KingdomQuestPineVariableStack variables,
            int canonicalLine,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Pine control-runtime dispatcher for the exact 78 external command
    /// occurrences in KQ/UnderHall2: the 74 shared external sites plus four
    /// source-locked chatwin sites.
    ///
    /// It owns no gameplay mutation. Exact source/data/native plan resolution
    /// belongs to KingdomQuestUnderHall2CommandState; a missing dependency is
    /// Invalid rather than a fall-through to generic host semantics.
    /// </summary>
    public static class KingdomQuestUnderHall2CommandRuntime
    {
        public const string ScriptLanguage = "KQ/UnderHall2";
        public const int SourceUsedFamilyCount = 7;
        public const int SourceUsedOccurrenceCount = 78;

        private static readonly HashSet<string> SourceUsedVerbs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "broadcast",
                "chatwin",
                "linkto",
                "mobregen",
                "questmobkill",
                "reward",
                "summonmob",
            };

        public static KingdomQuestPineCommandResolution TryStep(
            string scriptLanguage,
            string commandText,
            int canonicalLine,
            uint? currentKingdomQuestHandle,
            KingdomQuestPineVariableStack variables,
            IKingdomQuestUnderHall2CommandSink sink,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (!string.Equals(
                    scriptLanguage,
                    ScriptLanguage,
                    StringComparison.Ordinal))
                return KingdomQuestPineCommandResolution.Unsupported;

            string verb = GetVerb(commandText);
            if (!SourceUsedVerbs.Contains(verb))
                return KingdomQuestPineCommandResolution.Unsupported;

            if (variables == null ||
                sink == null ||
                !sink.TryStep(
                    commandText,
                    variables,
                    canonicalLine,
                    currentKingdomQuestHandle,
                    ref nativeState,
                    out completed))
                return KingdomQuestPineCommandResolution.Invalid;

            return KingdomQuestPineCommandResolution.Success;
        }

        private static string GetVerb(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return string.Empty;

            string value = commandText.Trim();
            int end = 0;
            while (end < value.Length &&
                !char.IsWhiteSpace(value[end]))
                end++;

            string verb = value.Substring(0, end);
            if (verb.EndsWith(".", StringComparison.Ordinal))
                verb = verb.Substring(0, verb.Length - 1);

            return verb;
        }
    }
}
