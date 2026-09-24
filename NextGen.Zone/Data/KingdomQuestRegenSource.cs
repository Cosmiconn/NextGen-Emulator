using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Original MobRegenGroup source row consumed by Zone's
    /// PineScriptMobRegenerator. This deliberately preserves the source
    /// fields and does not turn them into live map objects.
    /// </summary>
    public sealed class KingdomQuestRegenGroupSource
    {
        public string GroupIndex { get; internal set; }
        public string IsFamily { get; internal set; }
        public int CenterX { get; internal set; }
        public int CenterY { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int RangeDegree { get; internal set; }
    }

    /// <summary>
    /// Original 16-column MobRegen source row. The DWRD columns are kept as
    /// signed Int32 because the source stores negative RegDelta values despite
    /// declaring those columns as DWRD.
    /// </summary>
    public sealed class KingdomQuestRegenMobSource
    {
        public string RegenIndex { get; internal set; }
        public string MobIndex { get; internal set; }
        public byte MobNum { get; internal set; }
        public byte KillNum { get; internal set; }
        public int RegStandard { get; internal set; }
        public int RegMin { get; internal set; }
        public int RegMax { get; internal set; }
        public int RegDelta0 { get; internal set; }
        public int RegSec0 { get; internal set; }
        public int RegDelta1 { get; internal set; }
        public int RegSec1 { get; internal set; }
        public int RegDelta2 { get; internal set; }
        public int RegSec2 { get; internal set; }
        public int RegDelta3 { get; internal set; }
        public int RegSec3 { get; internal set; }
        public int RegDelta4 { get; internal set; }
    }

    public enum KingdomQuestRegenSourceBackend : byte
    {
        KingdomQuest = 1,
        Instant = 2,
    }

    /// <summary>
    /// Parsed source document only. Native Zone first loads source files into
    /// KQRegenTable, then PineScriptMobRegenerator lazily consumes the named
    /// groups when a running ScenarioBook requests them.
    /// </summary>
    public sealed class KingdomQuestRegenSourceDocument
    {
        public string SourceKey { get; private set; }
        public KingdomQuestRegenSourceBackend Backend { get; private set; }
        public IReadOnlyList<KingdomQuestRegenGroupSource> Groups { get; private set; }
        public IReadOnlyList<KingdomQuestRegenMobSource> Mobs { get; private set; }

        internal KingdomQuestRegenSourceDocument(
            string sourceKey,
            KingdomQuestRegenSourceBackend backend,
            List<KingdomQuestRegenGroupSource> groups,
            List<KingdomQuestRegenMobSource> mobs)
        {
            SourceKey = sourceKey;
            Backend = backend;
            Groups = groups.AsReadOnly();
            Mobs = mobs.AsReadOnly();
        }
    }

    /// <summary>
    /// Lossless loader for the modern KQ regen source shape used by every
    /// static MobRegen file referenced by this provenance-locked KQ snapshot.
    ///
    /// Zone.exe KQRegenTable::kqrt_Load stores at most 0x32 elements. Each
    /// native element owns a 12-byte source key plus its loaded OptionReader.
    /// It tries MobRegen/KingdomQuest first and MobRegen/Instant second.
    /// Runtime activation belongs to ScenarioBook/PineScriptMobRegenerator and
    /// is intentionally outside this source loader.
    /// </summary>
    public static class KingdomQuestRegenSourceLoader
    {
        public const int NativeTableCapacity = 50;
        public const int NativeElementNameBytes = 12;

        private const string GroupTable = "MobRegenGroup";
        private const string MobTable = "MobRegen";

        public static bool TryLoad(
            string shineRoot,
            string sourceKey,
            out KingdomQuestRegenSourceDocument document)
        {
            document = null;
            if (string.IsNullOrEmpty(shineRoot) ||
                !IsValidNativeSourceKey(sourceKey))
                return false;

            string kingdomQuestPath = Path.Combine(
                shineRoot, "MobRegen", "KingdomQuest", sourceKey + ".txt");
            if (TryLoadFile(
                    kingdomQuestPath, sourceKey,
                    KingdomQuestRegenSourceBackend.KingdomQuest,
                    out document))
                return true;

            string instantPath = Path.Combine(
                shineRoot, "MobRegen", "Instant", sourceKey + ".txt");
            return TryLoadFile(
                instantPath, sourceKey,
                KingdomQuestRegenSourceBackend.Instant,
                out document);
        }

        public static bool TryParse(
            TextReader reader,
            string sourceKey,
            KingdomQuestRegenSourceBackend backend,
            out KingdomQuestRegenSourceDocument document)
        {
            document = null;
            if (reader == null || !IsValidNativeSourceKey(sourceKey))
                return false;

            var groups = new List<KingdomQuestRegenGroupSource>();
            var mobs = new List<KingdomQuestRegenMobSource>();
            string currentTable = null;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int comment = line.IndexOf(';');
                if (comment >= 0)
                    line = line.Substring(0, comment);

                string[] fields = line.Split(
                    (char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length == 0)
                    continue;

                if (string.Equals(fields[0], "#table", StringComparison.Ordinal))
                {
                    currentTable = fields.Length >= 2 ? fields[1] : null;
                    continue;
                }

                if (!string.Equals(fields[0], "#record", StringComparison.Ordinal))
                    continue;

                if (string.Equals(currentTable, GroupTable, StringComparison.Ordinal))
                {
                    KingdomQuestRegenGroupSource group;
                    if (!TryParseGroup(fields, out group))
                        return false;
                    groups.Add(group);
                    continue;
                }

                if (string.Equals(currentTable, MobTable, StringComparison.Ordinal))
                {
                    KingdomQuestRegenMobSource mob;
                    if (!TryParseMob(fields, out mob))
                        return false;
                    mobs.Add(mob);
                }
            }

            if (groups.Count == 0 || mobs.Count == 0)
                return false;

            document = new KingdomQuestRegenSourceDocument(
                sourceKey, backend, groups, mobs);
            return true;
        }

        private static bool TryLoadFile(
            string path,
            string sourceKey,
            KingdomQuestRegenSourceBackend backend,
            out KingdomQuestRegenSourceDocument document)
        {
            document = null;
            try
            {
                if (!File.Exists(path))
                    return false;

                using (var reader = new StreamReader(path, true))
                    return TryParse(reader, sourceKey, backend, out document);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool IsValidNativeSourceKey(string sourceKey)
        {
            if (string.IsNullOrEmpty(sourceKey) ||
                sourceKey.Length > NativeElementNameBytes)
                return false;

            for (int i = 0; i < sourceKey.Length; i++)
            {
                if (sourceKey[i] > 0x7F)
                    return false;
            }
            return true;
        }

        private static bool TryParseGroup(
            string[] fields, out KingdomQuestRegenGroupSource group)
        {
            group = null;
            int centerX, centerY, width, height, rangeDegree;
            if (fields.Length != 8 ||
                (fields[2] != "Y" && fields[2] != "N") ||
                !TryInt(fields[3], out centerX) ||
                !TryInt(fields[4], out centerY) ||
                !TryInt(fields[5], out width) ||
                !TryInt(fields[6], out height) ||
                !TryInt(fields[7], out rangeDegree))
                return false;

            group = new KingdomQuestRegenGroupSource
            {
                GroupIndex = fields[1],
                IsFamily = fields[2],
                CenterX = centerX,
                CenterY = centerY,
                Width = width,
                Height = height,
                RangeDegree = rangeDegree,
            };
            return true;
        }

        private static bool TryParseMob(
            string[] fields, out KingdomQuestRegenMobSource mob)
        {
            mob = null;
            byte mobNum, killNum;
            int regStandard, regMin, regMax;
            int regDelta0, regSec0, regDelta1, regSec1;
            int regDelta2, regSec2, regDelta3, regSec3, regDelta4;

            if (fields.Length != 17 ||
                !byte.TryParse(
                    fields[3], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out mobNum) ||
                !byte.TryParse(
                    fields[4], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out killNum) ||
                !TryInt(fields[5], out regStandard) ||
                !TryInt(fields[6], out regMin) ||
                !TryInt(fields[7], out regMax) ||
                !TryInt(fields[8], out regDelta0) ||
                !TryInt(fields[9], out regSec0) ||
                !TryInt(fields[10], out regDelta1) ||
                !TryInt(fields[11], out regSec1) ||
                !TryInt(fields[12], out regDelta2) ||
                !TryInt(fields[13], out regSec2) ||
                !TryInt(fields[14], out regDelta3) ||
                !TryInt(fields[15], out regSec3) ||
                !TryInt(fields[16], out regDelta4))
                return false;

            mob = new KingdomQuestRegenMobSource
            {
                RegenIndex = fields[1],
                MobIndex = fields[2],
                MobNum = mobNum,
                KillNum = killNum,
                RegStandard = regStandard,
                RegMin = regMin,
                RegMax = regMax,
                RegDelta0 = regDelta0,
                RegSec0 = regSec0,
                RegDelta1 = regDelta1,
                RegSec1 = regSec1,
                RegDelta2 = regDelta2,
                RegSec2 = regSec2,
                RegDelta3 = regDelta3,
                RegSec3 = regSec3,
                RegDelta4 = regDelta4,
            };
            return true;
        }

        private static bool TryInt(string text, out int value)
        {
            return int.TryParse(
                text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
        }
    }
}
