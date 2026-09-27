using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Mutation-free projection of the native MobRegenStruct fields that
    /// correspond one-for-one with the original KQ MobRegen source row.
    ///
    /// The supplied source shape and the correlated 2016 Zone layout agree on:
    ///   rms_Number      <- MobNum
    ///   rms_KillNumber  <- KillNum
    ///   standard        <- RegStandard
    ///   minsec          <- RegMin
    ///   maxsec          <- RegMax
    ///   timedist[0..8]  <- RegDelta0,RegSec0,...,RegDelta3,RegSec3,RegDelta4
    ///
    /// This class deliberately does not interpret timedist, choose a delay,
    /// create a mob, allocate a runtime handle, or advance rebreed state.
    /// </summary>
    public sealed class KingdomQuestPineRegenNativeLayout
    {
        public byte Number { get; private set; }
        public byte KillNumber { get; private set; }

        public uint Standard { get; private set; }
        public uint MinSeconds { get; private set; }
        public uint MaxSeconds { get; private set; }

        public int[] TimeDistribution { get; private set; }

        private KingdomQuestPineRegenNativeLayout(
            byte number,
            byte killNumber,
            uint standard,
            uint minSeconds,
            uint maxSeconds,
            int[] timeDistribution)
        {
            Number = number;
            KillNumber = killNumber;
            Standard = standard;
            MinSeconds = minSeconds;
            MaxSeconds = maxSeconds;
            TimeDistribution = (int[])timeDistribution.Clone();
        }

        public static bool TryBuild(
            KingdomQuestPineRegenResolvedMob source,
            out KingdomQuestPineRegenNativeLayout layout)
        {
            layout = null;
            if (source == null ||
                source.RegStandard < 0 ||
                source.RegMin < 0 ||
                source.RegMax < 0)
                return false;

            int[] timeDistribution =
            {
                source.RegDelta0,
                source.RegSec0,
                source.RegDelta1,
                source.RegSec1,
                source.RegDelta2,
                source.RegSec2,
                source.RegDelta3,
                source.RegSec3,
                source.RegDelta4,
            };

            layout = new KingdomQuestPineRegenNativeLayout(
                source.MobNum,
                source.KillNum,
                unchecked((uint)source.RegStandard),
                unchecked((uint)source.RegMin),
                unchecked((uint)source.RegMax),
                timeDistribution);
            return true;
        }
    }
}
