using System;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// KQ-relevant values projected from the exact original SingleData.shn
    /// snapshot shipped with the supplied NA2016 server data.
    ///
    /// Source SHA-256:
    /// 8a0bf604d4cb843fb998c9d9ea42ef693700a73d3391dfe2590dfdf19fe80a88
    /// Shape: 41 records, default record length 36, two columns:
    /// SingleDataIDX string[32] and SingleDataValue u16.
    /// </summary>
    public static class KingdomQuestSingleDataInfo
    {
        public const string SourceSha256 =
            "8a0bf604d4cb843fb998c9d9ea42ef693700a73d3391dfe2590dfdf19fe80a88";
        public const int RecordCount = 41;
        public const int DefaultRecordLength = 36;
        public const int ColumnCount = 2;

        public const int KqVoteVoteLimitTimeSeconds = 60;
        public const int KqVoteSuggestCoolTimeSeconds = 300;
        public const int KqVoteLoginCoolTimeSeconds = 300;
        public const int KqPlayerListResetListCoolTimeSeconds = 5;
    }
}
