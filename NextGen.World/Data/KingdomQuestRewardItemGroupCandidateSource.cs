using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace NextGen.World.Data
{
    public sealed class KingdomQuestRewardItemGroupCandidateSourceRow
    {
        public int NativeShuffleOrdinal { get; private set; }
        public int SourceRow { get; private set; }
        public byte Column { get; private set; }
        public string GroupName { get; private set; }
        public ushort ItemId { get; private set; }
        public uint UseClass { get; private set; }

        internal KingdomQuestRewardItemGroupCandidateSourceRow(
            int nativeShuffleOrdinal, int sourceRow, byte column,
            string groupName, ushort itemId, uint useClass)
        {
            NativeShuffleOrdinal = nativeShuffleOrdinal;
            SourceRow = sourceRow;
            Column = column;
            GroupName = groupName;
            ItemId = itemId;
            UseClass = useClass;
        }
    }

    /// <summary>
    /// Exact KQ-relevant native ItemGroupClassifier insert source joined from
    /// original ItemInfoServer.shn and ItemInfo.shn.
    ///
    /// Zone.exe igc_Load walks source rows and calls igc_Store for DropGroupA
    /// then DropGroupB. NativeShuffleOrdinal is global across every valid
    /// igc_Store, so unrelated groups still retain their exact RNG consumption.
    /// </summary>
    public static class KingdomQuestRewardItemGroupCandidateSource
    {
        public const string ItemInfoServerSha256 =
            "d8cf2b411783822908e6ecbfc833b4ae104d2f3ece1b3cc2250aafa5aa714c10";
        public const string ItemInfoSha256 =
            "7ef63c5463ac8a5d51c3cb5ca8c80ff9311bb8ecac05fd04e5107f2fce02d494";
        public const string CanonicalRowsSha256 =
            "2944aecbb00d305929075f54b9571fca78252b64c2e0316d3d4e1fa7264fee35";

        public const int NativeValidStoreCalls = 5758;
        public const int NativeDistinctGroups = 789;
        public const int KqCandidateRows = 791;
        public const int KqCandidateUniqueItemIds = 790;

        private const string CompressedCanonicalRows = "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";

        private static readonly IReadOnlyList<
            KingdomQuestRewardItemGroupCandidateSourceRow> Rows = Load();

        public static IReadOnlyList<
            KingdomQuestRewardItemGroupCandidateSourceRow> Snapshot()
        {
            return Rows;
        }

        private static IReadOnlyList<
            KingdomQuestRewardItemGroupCandidateSourceRow> Load()
        {
            byte[] compressed = Convert.FromBase64String(
                CompressedCanonicalRows);
            byte[] canonical;
            using (var input = new MemoryStream(compressed))
            using (var gzip = new GZipStream(
                input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                canonical = output.ToArray();
            }

            string hash = Convert.ToHexString(
                SHA256.HashData(canonical)).ToLowerInvariant();
            if (!string.Equals(
                    hash, CanonicalRowsSha256, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "KQ ItemGroup candidate source hash mismatch.");

            string text = Encoding.UTF8.GetString(canonical);
            string[] lines = text.Split(
                new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != KqCandidateRows + 1 ||
                lines[0] !=
                    "native_shuffle_ordinal\tsource_row\tcolumn\tgroup\titem_id\tuse_class")
                throw new InvalidOperationException(
                    "KQ ItemGroup candidate source shape changed.");

            var rows =
                new List<KingdomQuestRewardItemGroupCandidateSourceRow>(
                    KqCandidateRows);
            var uniqueIds = new HashSet<ushort>();
            int previousOrdinal = -1;

            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split('\t');
                if (fields.Length != 6)
                    throw new InvalidOperationException(
                        "Malformed KQ ItemGroup candidate row.");

                int ordinal = int.Parse(
                    fields[0], CultureInfo.InvariantCulture);
                int sourceRow = int.Parse(
                    fields[1], CultureInfo.InvariantCulture);
                byte column = byte.Parse(
                    fields[2], CultureInfo.InvariantCulture);
                ushort itemId = ushort.Parse(
                    fields[4], CultureInfo.InvariantCulture);
                uint useClass = uint.Parse(
                    fields[5], CultureInfo.InvariantCulture);

                if (ordinal <= previousOrdinal ||
                    ordinal < 0 ||
                    ordinal >= NativeValidStoreCalls ||
                    sourceRow < 0 ||
                    column > 1 ||
                    string.IsNullOrEmpty(fields[3]))
                    throw new InvalidOperationException(
                        "Invalid KQ ItemGroup candidate row.");

                previousOrdinal = ordinal;
                uniqueIds.Add(itemId);
                rows.Add(
                    new KingdomQuestRewardItemGroupCandidateSourceRow(
                        ordinal, sourceRow, column, fields[3],
                        itemId, useClass));
            }

            if (rows.Count != KqCandidateRows ||
                uniqueIds.Count != KqCandidateUniqueItemIds)
                throw new InvalidOperationException(
                    "KQ ItemGroup candidate corpus changed.");

            return rows.AsReadOnly();
        }
    }
}
