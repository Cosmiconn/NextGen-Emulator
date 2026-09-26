using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestPineStartBinding
    {
        public string ScriptLanguage { get; private set; }
        public string ScriptInitValue { get; private set; }
        public KingdomQuestPineScriptDocument Document { get; private set; }
        public KingdomQuestPineBlockSource EntryBlock { get; private set; }

        internal KingdomQuestPineStartBinding(
            string scriptLanguage,
            string scriptInitValue,
            KingdomQuestPineScriptDocument document,
            KingdomQuestPineBlockSource entryBlock)
        {
            ScriptLanguage = scriptLanguage;
            ScriptInitValue = scriptInitValue;
            Document = document;
            EntryBlock = entryBlock;
        }
    }

    /// <summary>
    /// Exact Pine START source binding for the supplied KQ corpus.
    ///
    /// Native kqe_QuestStart passes the stored ScriptLanguage and
    /// ScriptInitValue tokens to CinemaComplex::cc_PlayFilm. This helper
    /// therefore resolves only that exact pair:
    /// - ScriptLanguage must be one of the source-backed used Pine documents;
    /// - ScriptInitValue must name an exact top-level block in that document.
    ///
    /// No default init value, first-block fallback or Lua substitution exists.
    /// </summary>
    public static class KingdomQuestPineStartRuntime
    {
        public static bool TryResolve(
            KingdomQuestProtocolInfo definition,
            out KingdomQuestPineStartBinding binding)
        {
            binding = null;
            if (definition == null ||
                string.IsNullOrEmpty(definition.ScriptLanguage) ||
                string.IsNullOrEmpty(definition.ScriptInitValue))
                return false;

            KingdomQuestPineScriptDocument document;
            if (!KingdomQuestPineScriptSource.TryGet(
                    definition.ScriptLanguage, out document) ||
                document == null)
                return false;

            KingdomQuestPineBlockSource entryBlock;
            if (!document.Blocks.TryGetValue(
                    definition.ScriptInitValue, out entryBlock) ||
                entryBlock == null)
                return false;

            binding = new KingdomQuestPineStartBinding(
                definition.ScriptLanguage,
                definition.ScriptInitValue,
                document,
                entryBlock);
            return true;
        }

        public static bool TryCreateControlRuntime(
            KingdomQuestProtocolInfo definition,
            IKingdomQuestPineRuntimeHost host,
            KingdomQuestPineUsedExpressionContext expressionContext,
            out KingdomQuestPineControlRuntime runtime)
        {
            runtime = null;

            KingdomQuestPineStartBinding binding;
            if (!TryResolve(definition, out binding))
                return false;

            return KingdomQuestPineControlRuntime.TryCreate(
                binding.Document,
                host,
                expressionContext,
                binding.EntryBlock.Name,
                out runtime);
        }
    }
}
