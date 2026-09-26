using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineTimeUnit : byte
    {
        Hour = 1,
        Minute = 2,
        Second = 3,
        Millisecond = 4,
    }

    /// <summary>
    /// Native 10-Hz time conversion used by Pine Pause and Movie::TimeLimit.
    ///
    /// Zone.pdb global token identities:
    ///   index_hour     = 0x1324B440
    ///   index_minute   = 0x1324A338
    ///   index_sec      = 0x1324B640
    ///   index_millisec = 0x1324A740
    ///
    /// ShinePause::sa_Step (0x004EE650) converts hour/min/sec to
    /// 36000/600/10 native ticks. Milliseconds execute signed
    /// (value * 10) / 1000 arithmetic, i.e. one native tick per 100 ms with
    /// integer truncation toward zero.
    /// </summary>
    public static class KingdomQuestPineNativeTime
    {
        public const int NativeTicksPerSecond = 10;
        public const int NativeTicksPerMinute = 600;
        public const int NativeTicksPerHour = 36000;

        public static bool TryParseUnit(
            string value,
            out KingdomQuestPineTimeUnit unit)
        {
            unit = 0;
            if (string.Equals(value, "Hour", StringComparison.OrdinalIgnoreCase))
            {
                unit = KingdomQuestPineTimeUnit.Hour;
                return true;
            }
            if (string.Equals(value, "Min", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Minute", StringComparison.OrdinalIgnoreCase))
            {
                unit = KingdomQuestPineTimeUnit.Minute;
                return true;
            }
            if (string.Equals(value, "Sec", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Second", StringComparison.OrdinalIgnoreCase))
            {
                unit = KingdomQuestPineTimeUnit.Second;
                return true;
            }
            if (string.Equals(value, "Millisec", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Millisecond", StringComparison.OrdinalIgnoreCase))
            {
                unit = KingdomQuestPineTimeUnit.Millisecond;
                return true;
            }
            return false;
        }

        public static uint ConvertToNativeTicks(
            KingdomQuestPineTimeUnit unit,
            int value)
        {
            unchecked
            {
                switch (unit)
                {
                    case KingdomQuestPineTimeUnit.Hour:
                        return (uint)(value * NativeTicksPerHour);

                    case KingdomQuestPineTimeUnit.Minute:
                        return (uint)(value * NativeTicksPerMinute);

                    case KingdomQuestPineTimeUnit.Second:
                        return (uint)(value * NativeTicksPerSecond);

                    case KingdomQuestPineTimeUnit.Millisecond:
                        // Native x86 first wraps the signed IMUL by 10 and
                        // then performs signed division by 1000 using the
                        // 0x10624DD3 magic reciprocal.
                        int scaled = value * NativeTicksPerSecond;
                        return (uint)(scaled / 1000);

                    default:
                        throw new ArgumentOutOfRangeException("unit");
                }
            }
        }

        public static uint AddDuration(uint currentTick, uint durationTicks)
        {
            unchecked
            {
                return currentTick + durationTicks;
            }
        }
    }

    /// <summary>
    /// Mutation-free source-exact Pause statement state.
    ///
    /// On the first ShinePause::sa_Step call native increments the frame state
    /// and stores currentTick + duration in ProcessStack offset +0x1010C.
    /// Later calls return "still running" while deadline >= currentTick and
    /// pop the frame only when the unsigned comparison deadline < currentTick
    /// becomes true.
    /// </summary>
    public sealed class KingdomQuestPinePausePlan
    {
        public KingdomQuestPineTimeUnit Unit { get; private set; }
        public int SourceValue { get; private set; }
        public uint DurationTicks { get; private set; }
        public uint DeadlineTick { get; private set; }

        internal KingdomQuestPinePausePlan(
            KingdomQuestPineTimeUnit unit,
            int sourceValue,
            uint durationTicks,
            uint deadlineTick)
        {
            Unit = unit;
            SourceValue = sourceValue;
            DurationTicks = durationTicks;
            DeadlineTick = deadlineTick;
        }

        public bool IsWaiting(uint currentTick)
        {
            return DeadlineTick >= currentTick;
        }

        public bool IsComplete(uint currentTick)
        {
            return DeadlineTick < currentTick;
        }
    }

    /// <summary>
    /// Mutation-free projection of PineEventScriptNode::ShineTimeLimit::sa_Step
    /// and Movie::TimeLimit::tl_SetTimeLimit.
    ///
    /// tl_SetTimeLimit switches on the first character of the unit token:
    /// H/h -> hours, M/m -> minutes, all other values -> seconds. It stores
    /// currentTick + convertedDuration, sets the previous-left marker to
    /// 99999999 and marks the TimeLimit active, after which sa_Step calls
    /// tl_LeftTick once and pops its Pine frame.
    ///
    /// The supplied KQ Pine corpus uses only Min and Sec for timelimit.
    /// </summary>
    public sealed class KingdomQuestPineTimeLimitPlan
    {
        public const int NativeInitialPreviousLeftValue = 99999999;

        public KingdomQuestPineTimeUnit Unit { get; private set; }
        public int SourceValue { get; private set; }
        public uint DurationTicks { get; private set; }
        public uint DeadlineTick { get; private set; }
        public int InitialPreviousLeftValue { get; private set; }
        public bool NativeActive { get; private set; }

        internal KingdomQuestPineTimeLimitPlan(
            KingdomQuestPineTimeUnit unit,
            int sourceValue,
            uint durationTicks,
            uint deadlineTick)
        {
            Unit = unit;
            SourceValue = sourceValue;
            DurationTicks = durationTicks;
            DeadlineTick = deadlineTick;
            InitialPreviousLeftValue = NativeInitialPreviousLeftValue;
            NativeActive = true;
        }

        public uint GetRemainingWholeSeconds(uint currentTick)
        {
            unchecked
            {
                uint leftTicks = DeadlineTick - currentTick;
                return leftTicks / KingdomQuestPineNativeTime.NativeTicksPerSecond;
            }
        }
    }

    /// <summary>
    /// Exact constant-operand timing projection for the supplied nine Pine KQs.
    ///
    /// Source-corpus facts:
    ///   pause     = 202 calls, all Sec/sec with integer literals
    ///   timelimit = 14 calls, only Min/Sec with integer literals
    ///
    /// Expression-backed timing remains delegated to the future Pine host
    /// rather than being guessed by this source-corpus helper.
    /// </summary>
    public static class KingdomQuestPineTimingPlan
    {
        public const int UsedPauseCommandCount = 202;
        public const int UsedTimeLimitCommandCount = 14;

        public static bool TryBuildPause(
            string commandText,
            uint currentTick,
            out KingdomQuestPinePausePlan plan)
        {
            plan = null;

            string unitText;
            int value;
            if (!TryParseConstantTimeCommand(
                    commandText, "pause", out unitText, out value))
                return false;

            KingdomQuestPineTimeUnit unit;
            if (!KingdomQuestPineNativeTime.TryParseUnit(unitText, out unit))
                return false;

            uint duration =
                KingdomQuestPineNativeTime.ConvertToNativeTicks(unit, value);
            plan = new KingdomQuestPinePausePlan(
                unit,
                value,
                duration,
                KingdomQuestPineNativeTime.AddDuration(
                    currentTick, duration));
            return true;
        }

        public static bool TryBuildTimeLimit(
            string commandText,
            uint currentTick,
            out KingdomQuestPineTimeLimitPlan plan)
        {
            plan = null;

            string unitText;
            int value;
            if (!TryParseConstantTimeCommand(
                    commandText, "timelimit", out unitText, out value))
                return false;

            KingdomQuestPineTimeUnit parsed;
            if (!KingdomQuestPineNativeTime.TryParseUnit(unitText, out parsed))
                return false;

            // The used source corpus contains only Min/Sec. Keep the helper
            // scoped to those source-proven forms even though native
            // tl_SetTimeLimit also recognizes H/h and defaults unknown units
            // to seconds by first-character dispatch.
            if (parsed != KingdomQuestPineTimeUnit.Minute &&
                parsed != KingdomQuestPineTimeUnit.Second)
                return false;

            uint duration =
                KingdomQuestPineNativeTime.ConvertToNativeTicks(parsed, value);
            plan = new KingdomQuestPineTimeLimitPlan(
                parsed,
                value,
                duration,
                KingdomQuestPineNativeTime.AddDuration(
                    currentTick, duration));
            return true;
        }

        private static bool TryParseConstantTimeCommand(
            string commandText,
            string expectedVerb,
            out string unit,
            out int value)
        {
            unit = null;
            value = 0;
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            string text = commandText.Trim();
            if (text.EndsWith(".", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1).TrimEnd();

            string[] parts = text.Split(
                (char[])null,
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 ||
                !string.Equals(
                    parts[0],
                    expectedVerb,
                    StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(parts[2], out value))
                return false;

            unit = parts[1];
            return unit.Length != 0;
        }
    }
}
