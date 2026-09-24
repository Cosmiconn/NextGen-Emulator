using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Exact supplied NA2016 MsgWorldManager.shn dependency used by
    /// CKQServer::SetDoneSkip via WorldManagerServer::GetMsg.
    ///
    /// Source SHA-256:
    /// 36b573c6f604a693cf0d0c7533fc90615233ae4a8be62f3ced9bd4119f25884e
    /// records=3, columns=1 (Desc, SHN type 26).
    ///
    /// GetMsg returns the executable's empty fallback string for an index
    /// outside the source row count; SetDoneSkip reason 3 requests indices
    /// 3 and 4, so the supplied source intentionally produces two empty
    /// NC_KQ_NOTIFY_CMD messages rather than guessed localization text.
    /// </summary>
    public static class KingdomQuestDoneSkipMessages
    {
        public const string SourceSha256 =
            "36b573c6f604a693cf0d0c7533fc90615233ae4a8be62f3ced9bd4119f25884e";

        private static readonly string[] SourceRows =
        {
            "Recruitment for Kingdom Quest - '%s' has begun.",
            "Kingdom Quest - %s will begin in  %d seconds.",
            "Kingdom Quest - %s has been canceled due to lack of participants(%d/%d).",
        };

        public static IReadOnlyList<string> Create(
            byte reason, KingdomQuestProtocolInfo definition)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");

            if (reason == KingdomQuestNativeConstants.DoneSkipReasonNotReady)
            {
                return new[]
                {
                    FormatPrintf(
                        GetSourceMessage(2),
                        definition.Title ?? string.Empty,
                        definition.NumOfJoiner,
                        definition.MinPlayers),
                };
            }

            if (reason == KingdomQuestNativeConstants.DoneSkipReasonTeamGap)
            {
                return new[]
                {
                    GetSourceMessage(3),
                    GetSourceMessage(4),
                };
            }

            return new string[0];
        }

        internal static string GetSourceMessage(int index)
        {
            return index >= 0 && index < SourceRows.Length
                ? SourceRows[index]
                : string.Empty;
        }

        private static string FormatPrintf(string format, params object[] values)
        {
            var result = new StringBuilder(format == null ? string.Empty : format);
            int valueIndex = 0;
            int search = 0;

            while (valueIndex < values.Length)
            {
                int marker = -1;
                char kind = '\0';
                for (int i = search; i + 1 < result.Length; i++)
                {
                    if (result[i] == '%' &&
                        (result[i + 1] == 's' || result[i + 1] == 'd'))
                    {
                        marker = i;
                        kind = result[i + 1];
                        break;
                    }
                }

                if (marker < 0)
                    throw new InvalidOperationException(
                        "MsgWorldManager source format no longer matches SetDoneSkip arguments.");

                string replacement;
                if (kind == 'd')
                    replacement = Convert.ToString(
                        values[valueIndex], CultureInfo.InvariantCulture);
                else
                    replacement = values[valueIndex] == null
                        ? string.Empty
                        : Convert.ToString(
                            values[valueIndex], CultureInfo.InvariantCulture);

                result.Remove(marker, 2);
                result.Insert(marker, replacement);
                search = marker + replacement.Length;
                valueIndex++;
            }

            return result.ToString();
        }
    }
}
