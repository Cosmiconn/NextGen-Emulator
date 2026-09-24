using System;
using System.Collections.Generic;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact supplied NA2016 main Kingdom Quest source snapshot.
    /// Values come from the original SHN bytes in the project Server archive.
    /// </summary>
    public static class KingdomQuestSourceSnapshot
    {
        public sealed class ExpectedSource
        {
            public string SourceName { get; private set; }
            public string TableName { get; private set; }
            public string Sha256 { get; private set; }
            public uint RecordCount { get; private set; }
            public uint ColumnCount { get; private set; }

            internal ExpectedSource(string sourceName, string tableName,
                string sha256, uint recordCount, uint columnCount)
            {
                SourceName = sourceName;
                TableName = tableName;
                Sha256 = sha256;
                RecordCount = recordCount;
                ColumnCount = columnCount;
            }
        }

        private static readonly Dictionary<string, ExpectedSource> Expected =
            new Dictionary<string, ExpectedSource>(StringComparer.OrdinalIgnoreCase)
            {
                { "KingdomQuest", new ExpectedSource(
                    "KingdomQuest", "data_kingdomquest",
                    "2a4c5c98005bf7253cc1c149a4260662a5c861a1b8ba38bf78d91ffcc260f6c9",
                    57, 35) },
                { "KingdomQuestMap", new ExpectedSource(
                    "KingdomQuestMap", "data_kingdomquestmap",
                    "d69edb81a6e265151eaf1108c48ed0ad3fe703bff7e7d4347d276bd53c2fa5e4",
                    38, 22) },
                { "KingdomQuestRew", new ExpectedSource(
                    "KingdomQuestRew", "data_kingdomquestrew",
                    "a19ad75f5b529a0178d1004182b37ee66703c01646f55f3beab439722e993dd1",
                    64, 33) },
                { "KQItem", new ExpectedSource(
                    "KQItem", "data_kqitem",
                    "2f641d273017bbd41f41f2ffb88df00ac92c1090b51f6438281bc185b1b2814a",
                    2, 4) },
                { "UseClassTypeInfo", new ExpectedSource(
                    "UseClassTypeInfo", "data_useclasstypeinfo",
                    "0ef94a55e26fb992e0497f825984742df681f94f9bf32167e4defebcbead632d",
                    39, 28) },
            };

        public static IReadOnlyDictionary<string, ExpectedSource> Sources
        {
            get { return Expected; }
        }

        public static bool TryGet(string sourceName, out ExpectedSource source)
        {
            return Expected.TryGetValue(sourceName, out source);
        }

        public static bool Matches(KingdomQuestSourceManifestInfo manifest)
        {
            if (manifest == null) return false;

            ExpectedSource expected;
            return Expected.TryGetValue(manifest.SourceName, out expected) &&
                   string.Equals(manifest.Sha256, expected.Sha256,
                       StringComparison.OrdinalIgnoreCase) &&
                   manifest.RecordCount == expected.RecordCount &&
                   manifest.ColumnCount == expected.ColumnCount;
        }
    }
}
