using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public interface IKingdomQuestKQHBatCommandSink
    {
        bool TryStep(
            KingdomQuestKQHBatExternalPlan plan,
            KingdomQuestPineVariableStack variables,
            uint? currentKingdomQuestHandle,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Shared Pine dispatcher for KQHBat1..5.
    ///
    /// Exactly 205 source-used sites across 11 families are intercepted before
    /// the generic unrecovered-verb guard. A changed source form or missing
    /// owner is Invalid/fail-closed; no battle/item/chat/reward/link semantics
    /// are inferred here.
    /// </summary>
    public static class KingdomQuestKQHBatCommandRuntime
    {
        public const int SourceUsedScriptCount = 5;
        public const int SourceUsedFamilyCount = 11;
        public const int SourceUsedOccurrenceCount = 205;

        private static readonly HashSet<string> SourceUsedVerbs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "abstateset",
                "battlestart",
                "battlestop",
                "broadcast",
                "chatwin",
                "invidualreward",
                "itemdrop",
                "itemerase",
                "linkto",
                "revival",
                "sendquestresult",
            };

        public static KingdomQuestPineCommandResolution TryStep(
            string scriptLanguage,
            string commandText,
            int canonicalLine,
            uint? currentKingdomQuestHandle,
            KingdomQuestPineVariableStack variables,
            IKingdomQuestKQHBatCommandSink sink,
            ref int nativeState,
            out bool completed)
        {
            completed = false;
            if (!KingdomQuestKQHBatSourceFlow.IsSupportedScript(
                    scriptLanguage))
                return KingdomQuestPineCommandResolution.Unsupported;

            string verb = GetVerb(commandText);
            if (!SourceUsedVerbs.Contains(verb))
                return KingdomQuestPineCommandResolution.Unsupported;

            KingdomQuestKQHBatExternalPlan plan;
            if (variables == null ||
                sink == null ||
                !KingdomQuestKQHBatExternalPlanBuilder.TryBuild(
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
