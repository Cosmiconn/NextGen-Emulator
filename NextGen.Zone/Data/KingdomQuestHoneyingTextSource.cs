using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Complete Script table from the hash-locked original KQHoneying.txt.
    /// A missing record is a proved empty-string result of ss_String (0x0048CE60).
    /// An unknown script identity remains unresolved, not an empty record.
    /// </summary>
    public static class KingdomQuestHoneyingTextSource
    {
        private static readonly Dictionary<string, string> Records =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "KQReturn60", "Giant Honeying is defeated." },
                { "KQReturn50", "Move to Elderine in 50 seconds." },
                { "KQReturn40", "Move to Elderine in 40 seconds." },
                { "KQReturn30", "Move to Elderine in 30 seconds." },
                { "KQReturn20", "Move to Elderine in 20 seconds." },
                { "KQReturn10", "Move to Elderine in 10 seconds." },
                { "KQReturn5", "Move to Elderine in 5 seconds." },
                { "KQFReturn30", "Move to Elderine in 30 seconds." },
                { "KQFReturn20", "Move to Elderine in 20 seconds." },
                { "KQFReturn10", "Move to Elderine in 10 seconds." },
                { "KQFReturn5", "Move to Elderine in 5 seconds." },
                { "Honeying01", "Where do you think you are!" },
                { "Honeying02", "You want to get stung!!?" },
                { "Summon01", "Friends! Attack those worthless people!!" },
            };

        public static bool TryResolve(
            KingdomQuestPineScriptFileSource source,
            string recordKey,
            out string text,
            out bool recordPresent)
        {
            text = null;
            recordPresent = false;
            if (source == null || recordKey == null ||
                source.Key != KingdomQuestHoneyingCommonNative.ScriptFileKey ||
                source.RelativePath != KingdomQuestHoneyingCommonNative.ScriptFilePath ||
                source.Sha256 != KingdomQuestHoneyingCommonNative.ScriptFileSha256)
                return false;

            recordPresent = Records.TryGetValue(recordKey, out text);
            if (!recordPresent)
                text = string.Empty;
            return true;
        }
    }
}
