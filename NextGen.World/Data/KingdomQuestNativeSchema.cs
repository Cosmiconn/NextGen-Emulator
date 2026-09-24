using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact source-level field names from the original 2016 PROTO_KQ_INFO
    /// layout. This class performs name comparison only; it does not claim
    /// that an SHN column with a different name is equivalent.
    /// </summary>
    public static class KingdomQuestNativeSchema
    {
        private static readonly string[] ProtocolFieldNames =
        {
            "Handle",
            "Status",
            "NumOfJoiner",
            "ID",
            "Title",
            "LimitTime",
            "StartTime",
            "tm_StartTime",
            "StartWaitTime",
            "MinLevel",
            "MaxLevel",
            "MinPlayers",
            "MaxPlayers",
            "PlayerRepeatMode",
            "PlayerRepeatCount",
            "PlayerRevivalMode",
            "PlayerRevivalCount",
            "DemandQuest",
            "DemandItem",
            "DemandClass",
            "DemandGender",
            "NextStartMode",
            "NextStartDelayMin",
            "RepeatMode",
            "RepeatCount",
            "RewardIndex",
            "DemandMobKill",
            "ScheduleTime",
            "tm_ScheduleTime",
            "RunCounter",
            "MapLink",
            "ScriptLanguage",
            "ScriptInitValue",
            "IsTeamPVP",
            "TeamRegenXY",
        };

        public static IReadOnlyList<string> Fields
        {
            get { return Array.AsReadOnly(ProtocolFieldNames); }
        }

        public static KingdomQuestSourceSchemaCoverage Compare(
            KingdomQuestSourceManifestInfo manifest)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");

            var source = new HashSet<string>(
                manifest.Columns.Select(v => v.ColumnName),
                StringComparer.Ordinal);
            var native = new HashSet<string>(ProtocolFieldNames, StringComparer.Ordinal);

            return new KingdomQuestSourceSchemaCoverage(
                source.Intersect(native).OrderBy(v => v, StringComparer.Ordinal),
                native.Except(source).OrderBy(v => v, StringComparer.Ordinal),
                source.Except(native).OrderBy(v => v, StringComparer.Ordinal));
        }
    }

    public sealed class KingdomQuestSourceSchemaCoverage
    {
        public IReadOnlyList<string> ExactNativeNames { get; private set; }
        public IReadOnlyList<string> NativeNamesMissingFromSource { get; private set; }
        public IReadOnlyList<string> SourceNamesWithoutExactNativeMatch { get; private set; }

        internal KingdomQuestSourceSchemaCoverage(
            IEnumerable<string> exact,
            IEnumerable<string> nativeMissing,
            IEnumerable<string> sourceOnly)
        {
            ExactNativeNames = exact.ToList().AsReadOnly();
            NativeNamesMissingFromSource = nativeMissing.ToList().AsReadOnly();
            SourceNamesWithoutExactNativeMatch = sourceOnly.ToList().AsReadOnly();
        }
    }
}
