using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit side-effect boundary for the eight exact UnderHall-special command
    /// families used by KQ/UnderHall.
    ///
    /// The caller owns native behavior and persistent command-frame state.
    /// This interface deliberately does not define map/mob/reward/login/
    /// interrupt semantics. Returning false is fail-closed.
    /// </summary>
    public interface IKingdomQuestUnderHallCommandSink
    {
        bool TryStep(
            KingdomQuestUnderHallCommandSourcePlan plan,
            KingdomQuestPineVariableStack variables,
            int canonicalLine,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Runtime composition boundary for the exact KQ/UnderHall source forms.
    ///
    /// All source syntax is validated by KingdomQuestUnderHallCommandSource
    /// before a side-effect owner is invoked. A source-used UnderHall verb with
    /// a non-canonical form is Invalid rather than Unsupported, preventing a
    /// generic host from silently reinterpreting changed script syntax.
    ///
    /// The sink receives the Pine command frame's persistent state by ref, so
    /// multi-step native commands such as waitinterrupt/waitlogin can remain
    /// active without the dispatcher inventing their completion predicates.
    /// </summary>
    public static class KingdomQuestUnderHallCommandRuntime
    {
        public const string ScriptLanguage = "KQ/UnderHall";
        public const int SourceUsedFamilyCount = 8;
        public const int SourceUsedOccurrenceCount = 45;
        public const int SourceDistinctFormCount = 21;

        private static readonly HashSet<string> SourceUsedVerbs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "broadcast",
                "linkto",
                "mobregen",
                "questmobkill",
                "reward",
                "summonmob",
                "waitinterrupt",
                "waitlogin",
            };

        public static KingdomQuestPineCommandResolution TryStep(
            string scriptLanguage,
            string commandText,
            int canonicalLine,
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
