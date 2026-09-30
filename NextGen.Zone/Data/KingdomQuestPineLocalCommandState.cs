namespace NextGen.Zone.Data
{
    /// <summary>
    /// External side-effect boundary for the three already-parsed one-step
    /// Pine commands whose observable work belongs outside local Movie/
    /// ScriptInterruptManager state.
    ///
    /// No implementation is supplied here: map breeding, COMPLETE/FAIL
    /// player effects and Z2W END remain owned by their separately recovered
    /// native paths.
    /// </summary>
    public interface IKingdomQuestPineExternalCommandSink
    {
        bool TryRunRegenGroup(KingdomQuestPineMobHatcheryBreedPlan plan);

        bool TryApplyQuestResult(KingdomQuestPineQuestResultPlan plan);

        bool TryEndKingdomQuest(KingdomQuestPineEndPlan plan);
    }

    /// <summary>
    /// Concrete local state owner for the source-proven Pine one-step commands
    /// whose native effects are confined to Movie::TimeLimit and
    /// ScriptInterruptManager bookkeeping.
    ///
    /// Native boundaries already recovered elsewhere:
    /// - TimeLimit::tl_SetTimeLimit replaces the active deadline state;
    /// - ScriptInterruptManager owns exactly 20 interrupt entries;
    /// - sim_InterruptErase removes every matching 16-byte interrupt name;
    /// - ShineInterruptClear applies the list eraser to all active entries.
    ///
    /// The remaining regengroup / questresult / endofkq side effects are
    /// delegated verbatim to an optional external sink. Missing external
    /// ownership therefore stays fail-closed rather than being simulated here.
    /// </summary>
    public sealed class KingdomQuestPineLocalCommandState :
        IKingdomQuestPineUsedCommandSink
    {
        private readonly KingdomQuestPineInterruptRegistryState interrupts;
        private readonly IKingdomQuestPineExternalCommandSink externalSink;
        private KingdomQuestPineTimeLimitPlan timeLimit;
        private KingdomQuestPineScriptFileSource currentScriptFile;

        public KingdomQuestPineInterruptRegistryState Interrupts
        {
            get { return interrupts; }
        }

        public KingdomQuestPineTimeLimitPlan TimeLimit
        {
            get { return timeLimit; }
        }

        public KingdomQuestPineScriptFileSource CurrentScriptFile
        {
            get { return currentScriptFile; }
        }

        public KingdomQuestPineLocalCommandState(
            IKingdomQuestPineExternalCommandSink externalSink = null)
        {
            interrupts = new KingdomQuestPineInterruptRegistryState();
            this.externalSink = externalSink;
        }

        public KingdomQuestUnderHallCommandState CreateUnderHallCommandState(
            IKingdomQuestUnderHallExternalCommandSink externalSink = null)
        {
            return new KingdomQuestUnderHallCommandState(externalSink);
        }

        public KingdomQuestUnderHall2CommandState CreateUnderHall2CommandState(
            IKingdomQuestUnderHall2ExternalCommandSink externalSink = null)
        {
            return new KingdomQuestUnderHall2CommandState(externalSink);
        }

        public bool TrySetTimeLimit(KingdomQuestPineTimeLimitPlan plan)
        {
            if (plan == null)
                return false;

            // Native tl_SetTimeLimit writes the current movie's one TimeLimit
            // object; a later command therefore replaces the prior state.
            timeLimit = plan;
            return true;
        }

        public bool TryRegisterInterrupt(
            KingdomQuestPineInterruptSetPlan plan)
        {
            return interrupts.TryRegister(plan);
        }

        public bool TryEraseInterrupt(byte[] nativeName16)
        {
            if (nativeName16 == null ||
                nativeName16.Length !=
                    KingdomQuestPineInterruptPlan.NativeEraseNameBytes)
                return false;

            // sim_InterruptErase is an erase pass, not a lookup-with-error
            // operation. Zero matches still means the command itself ran.
            interrupts.Erase(nativeName16);
            return true;
        }

        public bool TryClearInterrupts()
        {
            interrupts.Clear();
            return true;
        }

        public bool TrySetScriptFile(
            KingdomQuestPineScriptFilePlan plan)
        {
            if (plan == null)
                return false;

            if (plan.ClearsCurrentScript)
            {
                if (plan.Source != null)
                    return false;
                currentScriptFile = null;
                return true;
            }

            if (plan.Source == null)
                return false;

            currentScriptFile = plan.Source;
            return true;
        }

        public bool TryRunRegenGroup(KingdomQuestPineRegenGroupPlan plan)
        {
            KingdomQuestPineRegenRuntimePlan runtimePlan;
            KingdomQuestPineMobHatcheryBreedPlan breedPlan;
            if (externalSink == null ||
                !KingdomQuestPineRegenRuntimePlanBuilder.TryBuild(
                    plan,
                    DataProvider.Instance,
                    out runtimePlan) ||
                runtimePlan == null ||
                !KingdomQuestPineMobHatcheryBreedPlanBuilder.TryBuild(
                    runtimePlan,
                    out breedPlan) ||
                breedPlan == null)
                return false;

            return externalSink.TryRunRegenGroup(breedPlan);
        }

        public bool TryApplyQuestResult(KingdomQuestPineQuestResultPlan plan)
        {
            return externalSink != null &&
                externalSink.TryApplyQuestResult(plan);
        }

        public bool TryEndKingdomQuest(KingdomQuestPineEndPlan plan)
        {
            return externalSink != null &&
                externalSink.TryEndKingdomQuest(plan);
        }
    }
}
