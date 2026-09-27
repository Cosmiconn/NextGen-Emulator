using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestPineRegenResolvedMob
    {
        public ushort MobId { get; private set; }
        public string InxName { get; private set; }

        public byte MobNum { get; private set; }
        public byte KillNum { get; private set; }
        public int RegStandard { get; private set; }
        public int RegMin { get; private set; }
        public int RegMax { get; private set; }
        public int RegDelta0 { get; private set; }
        public int RegSec0 { get; private set; }
        public int RegDelta1 { get; private set; }
        public int RegSec1 { get; private set; }
        public int RegDelta2 { get; private set; }
        public int RegSec2 { get; private set; }
        public int RegDelta3 { get; private set; }
        public int RegSec3 { get; private set; }
        public int RegDelta4 { get; private set; }

        internal KingdomQuestPineRegenResolvedMob(
            ushort mobId,
            KingdomQuestRegenMobSource source)
        {
            MobId = mobId;
            InxName = source.MobIndex ?? string.Empty;
            MobNum = source.MobNum;
            KillNum = source.KillNum;
            RegStandard = source.RegStandard;
            RegMin = source.RegMin;
            RegMax = source.RegMax;
            RegDelta0 = source.RegDelta0;
            RegSec0 = source.RegSec0;
            RegDelta1 = source.RegDelta1;
            RegSec1 = source.RegSec1;
            RegDelta2 = source.RegDelta2;
            RegSec2 = source.RegSec2;
            RegDelta3 = source.RegDelta3;
            RegSec3 = source.RegSec3;
            RegDelta4 = source.RegDelta4;
        }
    }

    /// <summary>
    /// Runtime-ready identity projection of a source-resolved Pine regengroup.
    ///
    /// This class still performs no spawn, scheduling, kill tracking or
    /// rebreed work. It only replaces each source MobIndex token with a MobID
    /// after cross-checking both original MobInfo and MobInfoServer projections.
    /// All original group geometry and MobRegen timing/count fields are kept
    /// unchanged.
    /// </summary>
    public sealed class KingdomQuestPineRegenRuntimePlan
    {
        public string SourceKey { get; private set; }
        public string GroupIndex { get; private set; }
        public bool IsFamily { get; private set; }
        public int CenterX { get; private set; }
        public int CenterY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RangeDegree { get; private set; }
        public IReadOnlyList<KingdomQuestPineRegenResolvedMob> Mobs
        {
            get;
            private set;
        }

        internal KingdomQuestPineRegenRuntimePlan(
            KingdomQuestPineRegenGroupPlan source,
            IEnumerable<KingdomQuestPineRegenResolvedMob> mobs)
        {
            SourceKey = source.SourceKey;
            GroupIndex = source.GroupIndex;
            IsFamily = source.IsFamily;
            CenterX = source.CenterX;
            CenterY = source.CenterY;
            Width = source.Width;
            Height = source.Height;
            RangeDegree = source.RangeDegree;
            Mobs = mobs.ToList().AsReadOnly();
        }
    }

    public static class KingdomQuestPineRegenRuntimePlanBuilder
    {
        public static bool TryBuild(
            KingdomQuestPineRegenGroupPlan source,
            DataProvider data,
            out KingdomQuestPineRegenRuntimePlan plan)
        {
            plan = null;
            if (source == null ||
                data == null ||
                data.MobsByName == null ||
                data.MobData == null ||
                source.Mobs == null ||
                source.Mobs.Count == 0)
                return false;

            var resolved =
                new List<KingdomQuestPineRegenResolvedMob>(
                    source.Mobs.Count);

            for (int i = 0; i < source.Mobs.Count; i++)
            {
                KingdomQuestRegenMobSource mobSource = source.Mobs[i];
                if (mobSource == null ||
                    string.IsNullOrEmpty(mobSource.MobIndex))
                    return false;

                MobInfo clientInfo;
                MobInfoServer serverInfo;
                if (!data.MobsByName.TryGetValue(
                        mobSource.MobIndex, out clientInfo) ||
                    clientInfo == null ||
                    !data.MobData.TryGetValue(
                        mobSource.MobIndex, out serverInfo) ||
                    serverInfo == null ||
                    serverInfo.ID > ushort.MaxValue ||
                    clientInfo.ID != unchecked((ushort)serverInfo.ID))
                    return false;

                resolved.Add(
                    new KingdomQuestPineRegenResolvedMob(
                        clientInfo.ID,
                        mobSource));
            }

            plan = new KingdomQuestPineRegenRuntimePlan(
                source, resolved);
            return true;
        }
    }
}
