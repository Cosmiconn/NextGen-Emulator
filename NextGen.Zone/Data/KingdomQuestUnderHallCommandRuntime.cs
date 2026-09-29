using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit side-effect boundary for the six exact UnderHall-special
    /// gameplay command families that remain outside the shared Pine runtime.
    ///
    /// scriptfile is owned by KingdomQuestPineUsedCommandRuntime, while
    /// waitlogin and waitinterrupt are owned by KingdomQuestPineControlRuntime.
    /// Keeping those shared primitives out of this dispatcher prevents a second
    /// UnderHall-only implementation from diverging from the recovered native
    /// behavior.
    /// </summary>
    public interface IKingdomQuestUnderHallCommandSink
    {
        bool TryStep(
            KingdomQuestUnderHallCommandSourcePlan plan,
            KingdomQuestPineVariableStack variables,
            int canonicalLine,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Runtime composition boundary for the six remaining KQ/UnderHall
    /// side-effect command families.
    ///
    /// All source syntax is validated by KingdomQuestUnderHallCommandSource
    /// before a side-effect owner is invoked. A source-used UnderHall external
    /// verb with a non-canonical form is Invalid rather than Unsupported,
    /// preventing a generic host from silently reinterpreting changed script
    /// syntax. Shared Pine primitives are deliberately Unsupported here so the
    /// generic runtime remains their single owner.
    /// </summary>
    public static class KingdomQuestUnderHallCommandRuntime
    {
        public const string ScriptLanguage = "KQ/UnderHall";
        public const int SourceUsedFamilyCount = 6;
        public const int SourceUsedOccurrenceCount = 25;
        public const int SourceDistinctFormCount = 19;

        private static readonly HashSet<string> SourceUsedVerbs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "broadcast",
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
            IKingdomQuestUnderHallCommandSink sink,
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

            KingdomQuestUnderHallCommandSourcePlan plan;
            if (!KingdomQuestUnderHallCommandSource.TryParse(
                    commandText, out plan) ||
                plan == null ||
                variables == null ||
                sink == null)
                return KingdomQuestPineCommandResolution.Invalid;

            if (!sink.TryStep(
                    plan,
                    variables,
                    canonicalLine,
                    currentKingdomQuestHandle,
                    ref nativeState,
                    out completed))
                return KingdomQuestPineCommandResolution.Invalid;

            return KingdomQuestPineCommandResolution.Success;
        }

        public static bool IsSourceUsedVerb(string commandText)
        {
            return SourceUsedVerbs.Contains(GetVerb(commandText));
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
