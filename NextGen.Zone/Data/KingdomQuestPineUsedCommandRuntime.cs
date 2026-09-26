using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineCommandResolution : byte
    {
        Unsupported = 0,
        Success = 1,
        Invalid = 2,
    }

    /// <summary>
    /// Explicit native tick dependency used by one-step Pine commands that
    /// create time-based native plans. The dispatcher never reads wall-clock
    /// time itself.
    /// </summary>
    public interface IKingdomQuestPineNativeTickSource
    {
        bool TryGetCurrentTick(out uint currentTick);
    }

    /// <summary>
    /// Source-document dependency for regengroup. Implementations decide only
    /// how the already-proven KQRegenTable source document is obtained.
    /// </summary>
    public interface IKingdomQuestPineRegenDocumentResolver
    {
        bool TryResolve(
            string sourceKey,
            out KingdomQuestRegenSourceDocument document);
    }

    /// <summary>
    /// Mutation boundary for already-recovered one-step Pine commands.
    ///
    /// The dispatcher below parses/builds only source-proven plans. Actual map,
    /// packet, title, item or scenario mutation remains behind this sink until
    /// each native side-effect owner is wired to the emulator.
    /// </summary>
    public interface IKingdomQuestPineUsedCommandSink
    {
        bool TrySetTimeLimit(KingdomQuestPineTimeLimitPlan plan);

        bool TryRegisterInterrupt(KingdomQuestPineInterruptSetPlan plan);

        bool TryEraseInterrupt(byte[] nativeName16);

        bool TryClearInterrupts();

        bool TryRunRegenGroup(KingdomQuestPineRegenGroupPlan plan);

        bool TryApplyQuestResult(KingdomQuestPineQuestResultPlan plan);

        bool TryEndKingdomQuest(KingdomQuestPineEndPlan plan);
    }

    public sealed class KingdomQuestPineUsedCommandContext
    {
        public IKingdomQuestPineNativeTickSource TickSource { get; private set; }
        public IKingdomQuestPineRegenDocumentResolver RegenResolver
        {
            get;
            private set;
        }
        public IKingdomQuestPineUsedCommandSink Sink { get; private set; }
        public uint? CurrentKingdomQuestHandle { get; private set; }

        public KingdomQuestPineUsedCommandContext(
            IKingdomQuestPineNativeTickSource tickSource,
            IKingdomQuestPineRegenDocumentResolver regenResolver,
            IKingdomQuestPineUsedCommandSink sink,
            uint? currentKingdomQuestHandle)
        {
            TickSource = tickSource;
            RegenResolver = regenResolver;
            Sink = sink;
            CurrentKingdomQuestHandle = currentKingdomQuestHandle;
        }
    }

    /// <summary>
    /// Composes the already-recovered single-step command families that occur
    /// in the exact nine-script Pine KQ corpus.
    ///
    /// Covered source occurrences:
    ///   regengroup      243
    ///   interruptset   225
    ///   interruptclear  65
    ///   endofkq          19
    ///   interrupterase   14
    ///   timelimit        14
    ///   questresult       9
    ///   --------------------
    ///   total           589
    ///
    /// pause (202) and waitinterrupt (55) are deliberately excluded because
    /// they remain active across multiple frame steps and require persistent
    /// per-frame wait state. Every recognized family below is fail-closed when
    /// its explicit native dependency is absent.
    /// </summary>
    public static class KingdomQuestPineUsedCommandRuntime
    {
        public const int UsedOneStepCommandCount = 589;

        public static KingdomQuestPineCommandResolution TryStep(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            string verb = GetVerb(commandText);
            if (verb.Length == 0)
                return KingdomQuestPineCommandResolution.Invalid;

            switch (verb)
            {
                case "timelimit":
                    return StepTimeLimit(commandText, context, out completed);

                case "interruptset":
                    return StepInterruptSet(commandText, context, out completed);

                case "interrupterase":
                    return StepInterruptErase(commandText, context, out completed);

                case "interruptclear":
                    return StepInterruptClear(commandText, context, out completed);

                case "regengroup":
                    return StepRegenGroup(commandText, context, out completed);

                case "questresult":
                    return StepQuestResult(commandText, context, out completed);

                case "endofkq":
                    return StepEndOfKq(commandText, context, out completed);

                default:
                    return KingdomQuestPineCommandResolution.Unsupported;
            }
        }

        private static KingdomQuestPineCommandResolution StepTimeLimit(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            uint currentTick;
            if (context == null ||
                context.TickSource == null ||
                context.Sink == null ||
                !context.TickSource.TryGetCurrentTick(out currentTick))
                return KingdomQuestPineCommandResolution.Invalid;

            KingdomQuestPineTimeLimitPlan plan;
            if (!KingdomQuestPineTimingPlan.TryBuildTimeLimit(
                    commandText, currentTick, out plan) ||
                !context.Sink.TrySetTimeLimit(plan))
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static KingdomQuestPineCommandResolution StepInterruptSet(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            uint currentTick;
            if (context == null ||
                context.TickSource == null ||
                context.Sink == null ||
                !context.TickSource.TryGetCurrentTick(out currentTick))
                return KingdomQuestPineCommandResolution.Invalid;

            KingdomQuestPineInterruptSetPlan plan;
            if (!KingdomQuestPineInterruptPlan.TryParseUsedSet(
                    commandText, currentTick, out plan) ||
                !context.Sink.TryRegisterInterrupt(plan))
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static KingdomQuestPineCommandResolution StepInterruptErase(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            byte[] nativeName16;
            if (context == null ||
                context.Sink == null ||
                !KingdomQuestPineInterruptPlan.TryParseErase(
                    commandText, out nativeName16) ||
                !context.Sink.TryEraseInterrupt(nativeName16))
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static KingdomQuestPineCommandResolution StepInterruptClear(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            if (context == null ||
                context.Sink == null ||
                !KingdomQuestPineInterruptPlan.IsInterruptClear(commandText) ||
                !context.Sink.TryClearInterrupts())
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static KingdomQuestPineCommandResolution StepRegenGroup(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            string sourceKey;
            string groupIndex;
            if (context == null ||
                context.RegenResolver == null ||
                context.Sink == null ||
                !KingdomQuestPineRegenGroupResolver.TryParseUsedCommand(
                    commandText, out sourceKey, out groupIndex))
                return KingdomQuestPineCommandResolution.Invalid;

            KingdomQuestRegenSourceDocument document;
            KingdomQuestPineRegenGroupPlan plan;
            if (!context.RegenResolver.TryResolve(
                    sourceKey, out document) ||
                !KingdomQuestPineRegenGroupResolver.TryBuild(
                    document, groupIndex, out plan) ||
                !context.Sink.TryRunRegenGroup(plan))
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static KingdomQuestPineCommandResolution StepQuestResult(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            KingdomQuestPineQuestResultPlan plan;
            if (context == null ||
                context.Sink == null ||
                !KingdomQuestPineKqTerminalPlan.TryParseQuestResult(
                    commandText, out plan) ||
                !context.Sink.TryApplyQuestResult(plan))
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static KingdomQuestPineCommandResolution StepEndOfKq(
            string commandText,
            KingdomQuestPineUsedCommandContext context,
            out bool completed)
        {
            completed = false;
            if (context == null ||
                context.Sink == null ||
                !context.CurrentKingdomQuestHandle.HasValue)
                return KingdomQuestPineCommandResolution.Invalid;

            KingdomQuestPineEndPlan plan;
            if (!KingdomQuestPineKqTerminalPlan.TryParseEndOfKq(
                    commandText,
                    context.CurrentKingdomQuestHandle.Value,
                    out plan) ||
                !context.Sink.TryEndKingdomQuest(plan))
                return KingdomQuestPineCommandResolution.Invalid;

            completed = true;
            return KingdomQuestPineCommandResolution.Success;
        }

        private static string GetVerb(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return string.Empty;

            string value = commandText.Trim();
            int whitespace = -1;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    whitespace = i;
                    break;
                }
            }

            string verb = whitespace < 0
                ? value
                : value.Substring(0, whitespace);
            if (verb.EndsWith(".", StringComparison.Ordinal))
                verb = verb.Substring(0, verb.Length - 1);

            return verb.ToLowerInvariant();
        }
    }
}
