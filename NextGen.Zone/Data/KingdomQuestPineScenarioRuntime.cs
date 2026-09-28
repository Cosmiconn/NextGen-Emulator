using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Native Pine side of Movie::Theater::t_PlayFilm.
    ///
    /// Original Zone.exe:
    /// - PineEventScriptNode::Script::sa_Step at 0x004D8100 resolves the
    ///   literal top-level block "main" and pushes it on the ProcessStack;
    /// - Theater::t_PlayFilm at 0x005081F0 calls ps_Ready(root), then pushes
    ///   the literal VariableStack name "InitFlag" through
    ///   ProcessStack::ps_PushVariable (0x004D6BD0 -> vs_Push 0x004D6A90);
    /// - if that push succeeds, native copies exactly 0x40 DWORDs / 0x100
    ///   bytes from the supplied ScriptInitValue PineScriptToken into the
    ///   new value token before the root script is stepped.
    ///
    /// Therefore ScriptInitValue is initialization data, not an entry-block
    /// selector. This factory reproduces that ordering without inventing any
    /// ScenarioBook command semantics.
    /// </summary>
    public static class KingdomQuestPineScenarioRuntime
    {
        public const string NativeEntryBlockName = "main";
        public const string NativeInitVariableName = "InitFlag";
        public const int NativeInitTokenBytes = 0x100;

        public const uint NativeScriptStepAddress = 0x004D8100u;
        public const uint NativeTheaterPlayFilmAddress = 0x005081F0u;
        public const uint NativeProcessStackReadyAddress = 0x004D6B40u;
        public const uint NativeProcessStackPushVariableAddress = 0x004D6BD0u;
        public const uint NativeVariableStackPushAddress = 0x004D6A90u;

        public static bool TryCreate(
            KingdomQuestPineScriptDocument document,
            KingdomQuestScenarioStartPlan startPlan,
            IKingdomQuestPineRuntimeHost host,
            KingdomQuestPineUsedExpressionContext expressionContext,
            KingdomQuestPineUsedCommandContext commandContext,
            out KingdomQuestPineControlRuntime runtime)
        {
            runtime = null;
            if (document == null ||
                startPlan == null ||
                host == null ||
                !string.Equals(
                    document.ScriptLanguage,
                    startPlan.ScriptLanguage,
                    StringComparison.Ordinal) ||
                startPlan.ScriptInitValue == null)
                return false;

            KingdomQuestPineBlockSource main;
            if (!document.Blocks.TryGetValue(
                    NativeEntryBlockName, out main) ||
                main == null)
                return false;

            if (!KingdomQuestPineControlRuntime.TryCreate(
                    document,
                    host,
                    expressionContext,
                    commandContext,
                    NativeEntryBlockName,
                    out runtime) ||
                runtime == null)
                return false;

            KingdomQuestPineTokenValue initValue;
            if (!runtime.Variables.TryPush(
                    NativeInitVariableName, out initValue) ||
                initValue == null ||
                !initValue.TrySetAscii(startPlan.ScriptInitValue))
            {
                runtime = null;
                return false;
            }

            return true;
        }
    }
}
