using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NextGen.Database.DataStore;

namespace NextGen.World.Data
{
    public sealed class KingdomQuestSourceColumnInfo
    {
        public uint Ordinal { get; private set; }
        public string ColumnName { get; private set; }
        public uint TypeByte { get; private set; }
        public uint Length { get; private set; }

        public KingdomQuestSourceColumnInfo(DataRow row)
        {
            Ordinal = GetDataTypes.GetUint(row["Ordinal"]);
            ColumnName = Convert.ToString(row["ColumnName"]);
            TypeByte = GetDataTypes.GetUint(row["TypeByte"]);
            Length = GetDataTypes.GetUint(row["Length"]);
        }
    }

    public sealed class KingdomQuestSourceManifestInfo
    {
        public string SourceName { get; private set; }
        public string Sha256 { get; private set; }
        public uint RecordCount { get; private set; }
        public uint ColumnCount { get; private set; }
        public IReadOnlyList<KingdomQuestSourceColumnInfo> Columns { get; private set; }

        public KingdomQuestSourceManifestInfo(DataRow row,
            IEnumerable<KingdomQuestSourceColumnInfo> columns)
        {
            SourceName = Convert.ToString(row["SourceName"]);
            Sha256 = Convert.ToString(row["Sha256"]);
            RecordCount = GetDataTypes.GetUint(row["RecordCount"]);
            ColumnCount = GetDataTypes.GetUint(row["ColumnCount"]);
            Columns = (columns ?? Enumerable.Empty<KingdomQuestSourceColumnInfo>())
                .OrderBy(v => v.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        public bool IsStructurallyValid()
        {
            if (string.IsNullOrEmpty(SourceName) ||
                string.IsNullOrEmpty(Sha256) ||
                Sha256.Length != 64 ||
                Columns == null ||
                ColumnCount != (uint)Columns.Count)
                return false;

            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i] == null ||
                    Columns[i].Ordinal != (uint)i ||
                    string.IsNullOrEmpty(Columns[i].ColumnName))
                    return false;
            }
            return true;
        }
    }
}
