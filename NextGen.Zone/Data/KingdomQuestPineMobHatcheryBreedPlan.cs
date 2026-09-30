using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineRegenNativeAction : byte
    {
        ResolveMapNameServer = 1,
        FindRegenerator = 2,
        ScriptBreed = 3,
    }

    public sealed class KingdomQuestPineMobHatcheryBreedMobPlan
    {
        public ushort MobId { get; private set; }
        public string InxName { get; private set; }
        public KingdomQuestPineRegenNativeLayout NativeLayout
        {
            get;
            private set;
        }

        internal KingdomQuestPineMobHatcheryBreedMobPlan(
            KingdomQuestPineRegenResolvedMob source,
            KingdomQuestPineRegenNativeLayout nativeLayout)
        {
            MobId = source.MobId;
            InxName = source.InxName ?? string.Empty;
            NativeLayout = nativeLayout;
        }
    }

    /// <summary>
    /// Final mutation-free plan before the recovered Pine regengroup path
    /// crosses into the native MobHatchery owner.
    ///
    /// Zone.exe fixes the call order at ShineRegenGroup::sa_Step
    /// (0x004EE0F0): Theater::t_MapNameServer, then
    /// PineScriptMobRegenerator::psmr_find(sourceKey, groupIndex), then
    /// MobHatchery::mh_ScriptBreed. The 243 commands in the supplied KQ Pine
    /// corpus use no optional geometry override operands, so this plan carries
    /// the source geometry unchanged and materializes every MobRegen row into
    /// its already-correlated native layout.
    ///
    /// This plan deliberately contains no spawn coordinates chosen by RNG, no
    /// runtime object handle, no rebreed deadline and no emulator Mobspawn row.
    /// Those remain owned by the unrecovered MobHatchery live implementation.
    /// </summary>
    public sealed class KingdomQuestPineMobHatcheryBreedPlan
    {
        public const uint ShineRegenGroupStepAddress = 0x004EE0F0u;
        public const int UsedCommandCount = 243;
        public const int UsedOptionalGeometryOverrideCount = 0;

        private static readonly KingdomQuestPineRegenNativeAction[]
            NativeOrder =
            {
                KingdomQuestPineRegenNativeAction.ResolveMapNameServer,
                KingdomQuestPineRegenNativeAction.FindRegenerator,
                KingdomQuestPineRegenNativeAction.ScriptBreed,
            };

        public string SourceKey { get; private set; }
        public string GroupIndex { get; private set; }
        public bool IsFamily { get; private set; }
        public int CenterX { get; private set; }
        public int CenterY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RangeDegree { get; private set; }
        public IReadOnlyList<KingdomQuestPineMobHatcheryBreedMobPlan> Mobs
        {
            get;
            private set;
        }

        internal KingdomQuestPineMobHatcheryBreedPlan(
            KingdomQuestPineRegenRuntimePlan source,
            IList<KingdomQuestPineMobHatcheryBreedMobPlan> mobs)
        {
            SourceKey = source.SourceKey;
            GroupIndex = source.GroupIndex;
            IsFamily = source.IsFamily;
            CenterX = source.CenterX;
            CenterY = source.CenterY;
            Width = source.Width;
            Height = source.Height;
            RangeDegree = source.RangeDegree;
            Mobs = new List<KingdomQuestPineMobHatcheryBreedMobPlan>(
                mobs).AsReadOnly();
        }

        public KingdomQuestPineRegenNativeAction[] GetNativeOrder()
        {
            return (KingdomQuestPineRegenNativeAction[])NativeOrder.Clone();
        }
    }

    public static class KingdomQuestPineMobHatcheryBreedPlanBuilder
    {
        public static bool TryBuild(
            KingdomQuestPineRegenRuntimePlan source,
            out KingdomQuestPineMobHatcheryBreedPlan plan)
        {
            plan = null;
            if (source == null ||
                source.Mobs == null ||
                source.Mobs.Count == 0 ||
                string.IsNullOrEmpty(source.SourceKey) ||
                string.IsNullOrEmpty(source.GroupIndex))
                return false;

            var mobs =
                new List<KingdomQuestPineMobHatcheryBreedMobPlan>(
                    source.Mobs.Count);
            for (int i = 0; i < source.Mobs.Count; i++)
            {
                KingdomQuestPineRegenResolvedMob mob = source.Mobs[i];
                KingdomQuestPineRegenNativeLayout nativeLayout;
                if (mob == null ||
                    !KingdomQuestPineRegenNativeLayout.TryBuild(
                        mob, out nativeLayout) ||
                    nativeLayout == null)
                    return false;

                mobs.Add(
                    new KingdomQuestPineMobHatcheryBreedMobPlan(
                        mob, nativeLayout));
            }

            plan = new KingdomQuestPineMobHatcheryBreedPlan(source, mobs);
            return true;
        }
    }
}
