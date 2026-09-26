using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Source-resolved Pine "regengroup" request.
    ///
    /// Direct Zone.exe recovery:
    /// ShineRegenGroup::sa_Step (0x004EE0F0) resolves its first two tokens,
    /// calls Theater::t_MapNameServer, then
    /// PineScriptMobRegenerator::psmr_find(sourceKey, groupIndex), and finally
    /// MobHatchery::mh_ScriptBreed with the resolved regenerator.
    ///
    /// The supplied nine Pine KQ scripts contain 243 regengroup commands,
    /// covering 225 unique source/group pairs. Every used pair resolves in the
    /// original static KQ regen corpus and, for this exact snapshot, each pair
    /// owns exactly one MobRegen row.
    ///
    /// This class deliberately stops before MobHatchery spawning/timing.
    /// </summary>
    public sealed class KingdomQuestPineRegenGroupPlan
    {
        public string SourceKey { get; private set; }
        public string GroupIndex { get; private set; }
        public bool IsFamily { get; private set; }
        public int CenterX { get; private set; }
        public int CenterY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RangeDegree { get; private set; }
        public IReadOnlyList<KingdomQuestRegenMobSource> Mobs { get; private set; }

        internal KingdomQuestPineRegenGroupPlan(
            KingdomQuestRegenSourceDocument document,
            KingdomQuestRegenGroupSource group,
            IEnumerable<KingdomQuestRegenMobSource> mobs)
        {
            SourceKey = document.SourceKey;
            GroupIndex = group.GroupIndex;
            IsFamily = string.Equals(
                group.IsFamily, "Y", StringComparison.Ordinal);
            CenterX = group.CenterX;
            CenterY = group.CenterY;
            Width = group.Width;
            Height = group.Height;
            RangeDegree = group.RangeDegree;

            Mobs = mobs.Select(CloneMob).ToList().AsReadOnly();
        }

        private static KingdomQuestRegenMobSource CloneMob(
            KingdomQuestRegenMobSource source)
        {
            return new KingdomQuestRegenMobSource
            {
                RegenIndex = source.RegenIndex,
                MobIndex = source.MobIndex,
                MobNum = source.MobNum,
                KillNum = source.KillNum,
                RegStandard = source.RegStandard,
                RegMin = source.RegMin,
                RegMax = source.RegMax,
                RegDelta0 = source.RegDelta0,
                RegSec0 = source.RegSec0,
                RegDelta1 = source.RegDelta1,
                RegSec1 = source.RegSec1,
                RegDelta2 = source.RegDelta2,
                RegSec2 = source.RegSec2,
                RegDelta3 = source.RegDelta3,
                RegSec3 = source.RegSec3,
                RegDelta4 = source.RegDelta4,
            };
        }
    }

    public static class KingdomQuestPineRegenGroupResolver
    {
        public const int UsedPineCommandCount = 243;
        public const int UsedUniquePairCount = 225;
        public const int UsedSourceKeyCount = 5;

        /// <summary>
        /// Parses the exact two-string form used by every supplied Pine KQ:
        /// regengroup "SourceKey" "GroupIndex".
        ///
        /// Native ShineRegenGroup also has two optional expression operands
        /// which temporarily override the regenerator's first two DWORD
        /// geometry fields before mh_ScriptBreed and are then restored.
        /// None of the 243 supplied Pine calls uses those optional operands,
        /// so this source-corpus resolver does not invent their expression
        /// evaluation.
        /// </summary>
        public static bool TryParseUsedCommand(
            string commandText,
            out string sourceKey,
            out string groupIndex)
        {
            sourceKey = null;
            groupIndex = null;

            string text = NormalizeStatement(commandText);
            const string verb = "regengroup";
            if (!text.StartsWith(
                    verb, StringComparison.OrdinalIgnoreCase))
                return false;

            int offset = verb.Length;
            if (offset >= text.Length ||
                !char.IsWhiteSpace(text[offset]))
                return false;

            if (!TryReadQuoted(text, ref offset, out sourceKey) ||
                !TryReadQuoted(text, ref offset, out groupIndex))
                return false;

            SkipWhiteSpace(text, ref offset);
            return offset == text.Length &&
                sourceKey.Length != 0 &&
                groupIndex.Length != 0;
        }

        public static bool TryBuild(
            KingdomQuestRegenSourceDocument document,
            string groupIndex,
            out KingdomQuestPineRegenGroupPlan plan)
        {
            plan = null;
            if (document == null || string.IsNullOrEmpty(groupIndex))
                return false;

            KingdomQuestRegenGroupSource group = null;
            for (int i = 0; i < document.Groups.Count; i++)
            {
                KingdomQuestRegenGroupSource candidate =
                    document.Groups[i];
                if (!string.Equals(
                        candidate.GroupIndex,
                        groupIndex,
                        StringComparison.Ordinal))
                    continue;

                if (group != null)
                    return false;
                group = candidate;
            }

            if (group == null)
                return false;

            List<KingdomQuestRegenMobSource> mobs = document.Mobs
                .Where(v => string.Equals(
                    v.RegenIndex,
                    groupIndex,
                    StringComparison.Ordinal))
                .ToList();

            if (mobs.Count == 0)
                return false;

            plan = new KingdomQuestPineRegenGroupPlan(
                document, group, mobs);
            return true;
        }

        private static bool TryReadQuoted(
            string text,
            ref int offset,
            out string value)
        {
            value = null;
            SkipWhiteSpace(text, ref offset);
            if (offset >= text.Length || text[offset] != '"')
                return false;

            int start = ++offset;
            while (offset < text.Length && text[offset] != '"')
                offset++;

            if (offset >= text.Length)
                return false;

            value = text.Substring(start, offset - start);
            offset++;
            return true;
        }

        private static void SkipWhiteSpace(
            string text, ref int offset)
        {
            while (offset < text.Length &&
                char.IsWhiteSpace(text[offset]))
                offset++;
        }

        private static string NormalizeStatement(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return string.Empty;

            string value = commandText.Trim();
            if (value.EndsWith(".", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 1).TrimEnd();
            return value;
        }
    }
}
