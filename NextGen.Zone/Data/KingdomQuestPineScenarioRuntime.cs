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

    /// <summary>
    /// One Pine film bound to an already-started live Zone KQ instance.
    ///
    /// The session owns only the Pine ProcessStack/VariableStack runtime and
    /// immutable Zone identity captured at creation. It does not choose the
    /// scheduler cadence or create any gameplay dependency.
    /// </summary>
    public sealed class KingdomQuestZonePineFilmSession
    {
        public uint Handle { get; private set; }
        public ushort MapID { get; private set; }
        public short MapInstance { get; private set; }
        public string ScriptLanguage { get; private set; }
        public KingdomQuestPineControlRuntime Runtime { get; private set; }

        public KingdomQuestPineRuntimeStatus Status
        {
            get { return Runtime.Status; }
        }

        public string Fault
        {
            get { return Runtime.Fault; }
        }

        internal KingdomQuestZonePineFilmSession(
            KingdomQuestZoneRuntimeState state,
            KingdomQuestPineControlRuntime runtime)
        {
            Handle = state.Handle;
            MapID = state.MapID;
            MapInstance = state.MapInstance;
            ScriptLanguage = state.ScenarioStartPlan.ScriptLanguage;
            Runtime = runtime;
        }

        /// <summary>
        /// Executes exactly one native-style Pine top-frame step.
        /// Scheduling frequency remains an external CinemaComplex owner
        /// dependency rather than an invented timer in this session.
        /// </summary>
        public KingdomQuestPineRuntimeStatus Step()
        {
            return Runtime.Step();
        }
    }

    /// <summary>
    /// Bridges the represented W2Z START state to the already-recovered Pine
    /// side of CinemaComplex::cc_PlayFilm.
    ///
    /// Only an instance that is currently Started and carries the exact native
    /// DropFilm -> CloseAllDoors -> PlayFilm start envelope may create a
    /// session. The ScriptLanguage must resolve in the hash-locked nine-script
    /// Pine corpus; Lua books are not treated as Pine fallbacks.
    ///
    /// Host, expression and command dependencies remain caller-owned. A
    /// supplied CurrentKingdomQuestHandle may not disagree with the live Zone
    /// handle. Missing later command dependencies therefore still fault closed
    /// when the Pine runtime reaches them.
    /// </summary>
    public static class KingdomQuestZonePineFilmBridge
    {
        public static bool TryCreateStartedSession(
            uint handle,
            IKingdomQuestPineRuntimeHost host,
            KingdomQuestPineUsedExpressionContext expressionContext,
            KingdomQuestPineUsedCommandContext commandContext,
            out KingdomQuestZonePineFilmSession session)
        {
            session = null;

            KingdomQuestZoneRuntimeState state;
            if (host == null ||
                !KingdomQuestZoneRuntimeRegistry.TryGet(handle, out state) ||
                state == null ||
                state.State != KingdomQuestZoneLifecycleState.Started ||
                state.ScenarioStartPlan == null)
                return false;

            var nativeOrder = state.ScenarioStartPlan.GetNativeOrder();
            if (nativeOrder == null ||
                nativeOrder.Count != 3 ||
                nativeOrder[0] !=
                    KingdomQuestScenarioStartAction.DropCurrentFilm ||
                nativeOrder[1] !=
                    KingdomQuestScenarioStartAction.CloseAllDoors ||
                nativeOrder[2] !=
                    KingdomQuestScenarioStartAction.PlayFilm)
                return false;

            if (commandContext != null &&
                commandContext.CurrentKingdomQuestHandle.HasValue &&
                commandContext.CurrentKingdomQuestHandle.Value != handle)
                return false;

            KingdomQuestPineScriptDocument document;
            if (!KingdomQuestPineScriptSource.TryGet(
                    state.ScenarioStartPlan.ScriptLanguage, out document) ||
                document == null)
                return false;

            KingdomQuestPineControlRuntime runtime;
            if (!KingdomQuestPineScenarioRuntime.TryCreate(
                    document,
                    state.ScenarioStartPlan,
                    host,
                    expressionContext,
                    commandContext,
                    out runtime) ||
                runtime == null)
                return false;

            session = new KingdomQuestZonePineFilmSession(state, runtime);
            return true;
        }
    }
}
