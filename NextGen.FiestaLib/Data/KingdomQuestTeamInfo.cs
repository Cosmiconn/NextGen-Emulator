using System.Data;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Source-faithful KQTeam.shn row. Field names and widths mirror the
    /// exported NA2016 data; no gameplay meaning is assigned beyond the
    /// source names.
    /// </summary>
    public sealed class KingdomQuestTeamInfo
    {
        public ushort ID { get; private set; }
        public byte MaxMemberGap { get; private set; }
        public bool IsTeamPvp { get; private set; }
        public ushort TeamDivideType { get; private set; }
        public uint RegenXRed { get; private set; }
        public uint RegenYRed { get; private set; }
        public uint RegenXBlue { get; private set; }
        public uint RegenYBlue { get; private set; }

        public static KingdomQuestTeamInfo Load(DataRow row)
        {
            return new KingdomQuestTeamInfo
            {
                ID = GetDataTypes.GetUshort(row["ID"]),
                MaxMemberGap = GetDataTypes.GetByte(row["MaxMemberGap"]),
                IsTeamPvp = GetDataTypes.GetByte(row["IsTeamPVP"]) != 0,
                TeamDivideType = GetDataTypes.GetUshort(row["KQTeamDivideType"]),
                RegenXRed = GetDataTypes.GetUint(row["RegenXRed"]),
                RegenYRed = GetDataTypes.GetUint(row["RegenYRed"]),
                RegenXBlue = GetDataTypes.GetUint(row["RegenXBlue"]),
                RegenYBlue = GetDataTypes.GetUint(row["RegenYBlue"]),
            };
        }
    }

    public sealed class KingdomQuestVoteReasonInfo
    {
        public byte ID { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }

        public static KingdomQuestVoteReasonInfo Load(DataRow row)
        {
            return new KingdomQuestVoteReasonInfo
            {
                ID = GetDataTypes.GetByte(row["ID"]),
                Title = (string)row["KQVoteTitle"],
                Description = (string)row["KQVoteDescription"],
            };
        }
    }
}
