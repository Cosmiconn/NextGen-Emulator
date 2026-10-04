using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Original BlockInfo files for the 23 distinct KingdomQuestMap.shn base
    /// maps. Optional SHAB/SBI absence is source-verified. Each result owns
    /// its mutable bitmap; only immutable source bytes are cached globally.
    /// </summary>
    public static class KingdomQuestMapCollisionSource
    {
        public const string ResourceName = "NextGen.Zone.KingdomQuestCollision.zip";
        public const string BundleSha256 = "6798b510b21d2de11319b3b3808a87ca6d7e826822c2cc489f23784d513a1002";
        private static readonly Lazy<Dictionary<string, byte[]>> Sources =
            new Lazy<Dictionary<string, byte[]>>(Load, true);

        public static bool TryCreate(string mapBase, out KingdomQuestMapCollision collision)
        {
            collision = null;
            byte[] shbd, shab, sbi;
            if (string.IsNullOrEmpty(mapBase) || !Sources.Value.TryGetValue(mapBase + ".shbd", out shbd))
                return false;
            Sources.Value.TryGetValue(mapBase + ".shab", out shab);
            Sources.Value.TryGetValue(mapBase + ".sbi", out sbi);
            return KingdomQuestMapCollision.TryLoad(mapBase, shbd, shab, sbi, out collision);
        }

        private static Dictionary<string, byte[]> Load()
        {
            using (var input = typeof(KingdomQuestMapCollisionSource).Assembly.GetManifestResourceStream(ResourceName))
            using (var copy = new MemoryStream())
            {
                if (input == null) throw new InvalidDataException("KQ collision source resource missing.");
                input.CopyTo(copy);
                byte[] bytes = copy.ToArray();
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), BundleSha256,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("KQ collision source hash mismatch.");
                copy.Position = 0;
                var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                using (var zip = new ZipArchive(copy, ZipArchiveMode.Read))
                    foreach (var entry in zip.Entries)
                        using (var file = entry.Open())
                        using (var contents = new MemoryStream())
                        {
                            file.CopyTo(contents);
                            result.Add(entry.FullName, contents.ToArray());
                        }
                return result;
            }
        }
    }
}
