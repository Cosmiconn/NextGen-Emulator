namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineKqTitleHookKind : byte
    {
        Success = 1,
        Fail = 2,
    }

    /// <summary>
    /// Mutation-free projection of the original ShinePlayer KQ title hooks.
    ///
    /// ShinePlayer::so_ply_KQSuccess / so_ply_KQFail first resolve the native
    /// CharacterTitleZone owner at player offset 0x29638. CT_KQSuccess and
    /// CT_KQFail increment distinct 64-bit counters, set their adjacent dirty
    /// DWORD to 1, and enter the common category evaluator with 21 or 22.
    ///
    /// The current emulator title-tier helper is not substituted here because
    /// it does not represent these native 64-bit counters/dirty fields.
    /// </summary>
    public sealed class KingdomQuestPineKqTitleHookNativePlan
    {
        public const int CharacterTitleZonePlayerOffset = 0x29638;
        public const uint CommonCategoryEvaluateAddress = 0x005CB5F0u;

        public const uint SuccessWrapperAddress = 0x0055AE10u;
        public const uint SuccessHookAddress = 0x005CBFB0u;
        public const int SuccessCounterLowOffset = 0x5E8;
        public const int SuccessCounterHighOffset = 0x5EC;
        public const int SuccessDirtyOffset = 0x5F0;
        public const uint SuccessCategory = 21u;

        public const uint FailWrapperAddress = 0x0055AE30u;
        public const uint FailHookAddress = 0x005CBFD0u;
        public const int FailCounterLowOffset = 0x5F8;
        public const int FailCounterHighOffset = 0x5FC;
        public const int FailDirtyOffset = 0x600;
        public const uint FailCategory = 22u;

        public const ulong NativeCounterIncrement = 1UL;
        public const uint NativeDirtyValue = 1u;

        public KingdomQuestPineKqTitleHookKind Kind { get; private set; }
        public uint WrapperAddress { get; private set; }
        public uint HookAddress { get; private set; }
        public int CounterLowOffset { get; private set; }
        public int CounterHighOffset { get; private set; }
        public int DirtyOffset { get; private set; }
        public uint Category { get; private set; }

        private KingdomQuestPineKqTitleHookNativePlan(
            KingdomQuestPineKqTitleHookKind kind,
            uint wrapperAddress,
            uint hookAddress,
            int counterLowOffset,
            int counterHighOffset,
            int dirtyOffset,
            uint category)
        {
            Kind = kind;
            WrapperAddress = wrapperAddress;
            HookAddress = hookAddress;
            CounterLowOffset = counterLowOffset;
            CounterHighOffset = counterHighOffset;
            DirtyOffset = dirtyOffset;
            Category = category;
        }

        public static KingdomQuestPineKqTitleHookNativePlan Success()
        {
            return new KingdomQuestPineKqTitleHookNativePlan(
                KingdomQuestPineKqTitleHookKind.Success,
                SuccessWrapperAddress,
                SuccessHookAddress,
                SuccessCounterLowOffset,
                SuccessCounterHighOffset,
                SuccessDirtyOffset,
                SuccessCategory);
        }

        public static KingdomQuestPineKqTitleHookNativePlan Fail()
        {
            return new KingdomQuestPineKqTitleHookNativePlan(
                KingdomQuestPineKqTitleHookKind.Fail,
                FailWrapperAddress,
                FailHookAddress,
                FailCounterLowOffset,
                FailCounterHighOffset,
                FailDirtyOffset,
                FailCategory);
        }

        public static bool TryBuild(
            KingdomQuestPineQuestResultPlan result,
            out KingdomQuestPineKqTitleHookNativePlan plan)
        {
            plan = null;
            if (result == null)
                return false;

            if (result.Kind == KingdomQuestPineQuestResultKind.Success)
            {
                if (result.TitleCategoryType != SuccessCategory)
                    return false;
                plan = Success();
                return true;
            }

            if (result.Kind == KingdomQuestPineQuestResultKind.Fail)
            {
                if (result.TitleCategoryType != FailCategory)
                    return false;
                plan = Fail();
                return true;
            }

            return false;
        }
    }
}
