using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public interface IKingdomQuestHoneyingCommandSink
    {
        bool TryStep(
            KingdomQuestHoneyingExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed);
    }

    public static class KingdomQuestHoneyingCommandRuntime
    {
        public const string ScriptLanguage = "KQ/Honeying";
        public const int SourceUsedFamilyCount = 13;
        public const int SourceUsedOccurrenceCount = 40;

        private static readonly HashSet<string> SourceUsedVerbs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "broadcast",
                "chatwin",
                "doorbuild",
                "doorclose",
                "dooropen",
                "effectobj",
                "linkto",
                "mobregen",
                "npcshout",
                "questmobkill",
                "reward",
                "summonmob",
                "vanish",
            };

        public static KingdomQuestPineCommandResolution TryStep(
            string scriptLanguage,
            string commandText,
            int canonicalLine,
            uint? currentKingdomQuestHandle,
            KingdomQuestPineVariableStack variables,
            IKingdomQuestHoneyingCommandSink sink,
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

            KingdomQuestHoneyingExternalPlan plan;
            if (variables == null ||
                sink == null ||
                !KingdomQuestHoneyingExternalPlanBuilder.TryBuild(
                    scriptLanguage,
                    canonicalLine,
                    commandText,
                    out plan) ||
                plan == null ||
                !sink.TryStep(
                    plan,
                    variables,
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
