using System;
using System.Globalization;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestUnderHallExternalPlanKind : byte
    {
        Broadcast = 1,
        LinkTo = 2,
        MobRegen = 3,
        QuestMobKill = 4,
        Reward = 5,
        ScriptFile = 6,
        SummonMob = 7,
        WaitLogin = 8,
    }

    /// <summary>
    /// Mutation-free, source-resolved projection of the eight UnderHall
    /// command families whose native side effects remain external.
    ///
    /// Fields are deliberately named only where the original source/catalog
    /// proves their identity. Unknown numeric Pine operand meaning stays in
    /// RawNumeric* rather than being promoted to guessed gameplay semantics.
    /// </summary>
    public sealed class KingdomQuestUnderHallExternalPlan
    {
        public KingdomQuestUnderHallExternalPlanKind Kind { get; private set; }
        public string SourceToken { get; private set; }

        public KingdomQuestUnderHallMapSourceRef Map { get; private set; }
        public KingdomQuestUnderHallMobSourceRef AnchorMob { get; private set; }
        public KingdomQuestUnderHallMobSourceRef Mob { get; private set; }

        public int Count { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public int RawNumeric1 { get; private set; }
        public int RawNumeric2 { get; private set; }
        public string RawText1 { get; private set; }

        internal KingdomQuestUnderHallExternalPlan(
            KingdomQuestUnderHallExternalPlanKind kind,
            string sourceToken,
            KingdomQuestUnderHallMapSourceRef map,
            KingdomQuestUnderHallMobSourceRef anchorMob,
            KingdomQuestUnderHallMobSourceRef mob,
            int count,
            int x,
            int y,
            int rawNumeric1,
            int rawNumeric2,
            string rawText1)
        {
            Kind = kind;
            SourceToken = sourceToken ?? string.Empty;
            Map = map;
            AnchorMob = anchorMob;
            Mob = mob;
            Count = count;
            X = x;
            Y = y;
            RawNumeric1 = rawNumeric1;
            RawNumeric2 = rawNumeric2;
            RawText1 = rawText1 ?? string.Empty;
        }
    }

    /// <summary>
    /// Resolves only the exact KQ/UnderHall source forms already accepted by
    /// KingdomQuestUnderHallCommandSource into checked-in MapInfo/MobInfo
    /// identities and literal operands.
    ///
    /// It performs no broadcast, transfer, spawn, quest, reward, script or
    /// login mutation. That remains behind the native side-effect boundary.
    /// </summary>
    public static class KingdomQuestUnderHallExternalPlanBuilder
    {
        public const int SourceUsedOccurrenceCount = 27;
        public const int SourceDistinctFormCount = 21;

        public static bool TryBuild(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source == null || source.Arguments == null)
                return false;

            switch (source.Kind)
            {
                case KingdomQuestUnderHallCommandKind.Broadcast:
                    return TryBroadcast(source, out plan);

                case KingdomQuestUnderHallCommandKind.LinkTo:
                    return TryLinkTo(source, out plan);

                case KingdomQuestUnderHallCommandKind.MobRegen:
                    return TryMobRegen(source, out plan);

                case KingdomQuestUnderHallCommandKind.QuestMobKill:
                    return TryQuestMobKill(source, out plan);

                case KingdomQuestUnderHallCommandKind.Reward:
                    return TryReward(source, out plan);

                case KingdomQuestUnderHallCommandKind.ScriptFile:
                    return TryScriptFile(source, out plan);

                case KingdomQuestUnderHallCommandKind.SummonMob:
                    return TrySummonMob(source, out plan);

                case KingdomQuestUnderHallCommandKind.WaitLogin:
                    return TryWaitLogin(source, out plan);

                case KingdomQuestUnderHallCommandKind.WaitInterrupt:
                default:
                    return false;
            }
        }

        private static bool TryBroadcast(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 2 ||
                !string.Equals(
                    source.Arguments[0], "all",
                    StringComparison.Ordinal))
                return false;

            plan = Build(
                KingdomQuestUnderHallExternalPlanKind.Broadcast,
                source.Arguments[1]);
            return true;
        }

        private static bool TryLinkTo(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 5 ||
                !string.Equals(source.Arguments[0], "all", StringComparison.Ordinal) ||
                !string.Equals(source.Arguments[1], "Eld", StringComparison.Ordinal) ||
                !string.Equals(source.Arguments[2], "Eld", StringComparison.Ordinal))
                return false;

            int x;
            int y;
            if (!TryInt(source.Arguments[3], out x) ||
                !TryInt(source.Arguments[4], out y))
                return false;

            KingdomQuestUnderHallMapSourceRef map =
                KingdomQuestUnderHallSourceCatalog.GetElderine();
            if (map == null ||
                map.MapId != KingdomQuestUnderHallSourceCatalog.ElderineMapId ||
                !string.Equals(map.MapName, "Eld", StringComparison.Ordinal) ||
                map.RegenX != x ||
                map.RegenY != y)
                return false;

            plan = new KingdomQuestUnderHallExternalPlan(
                KingdomQuestUnderHallExternalPlanKind.LinkTo,
                "all",
                map,
                null,
                null,
                0,
                x,
                y,
                0,
                0,
                source.Arguments[2]);
            return true;
        }

        private static bool TryMobRegen(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 7)
                return false;

            KingdomQuestUnderHallMobSourceRef mob;
            if (!KingdomQuestUnderHallSourceCatalog.TryGetMob(
                    source.Arguments[0], out mob) ||
                mob == null ||
                !string.Equals(
                    source.Arguments[1], mob.InxName,
                    StringComparison.Ordinal))
                return false;

            int x;
            int y;
            int raw1;
            int raw2;
            if (!TryInt(source.Arguments[2], out x) ||
                !TryInt(source.Arguments[3], out y) ||
                !TryInt(source.Arguments[4], out raw1) ||
                !TryInt(source.Arguments[5], out raw2))
                return false;

            plan = new KingdomQuestUnderHallExternalPlan(
                KingdomQuestUnderHallExternalPlanKind.MobRegen,
                source.Arguments[1],
                null,
                mob,
                mob,
                0,
                x,
                y,
                raw1,
                raw2,
                source.Arguments[6]);
            return true;
        }

        private static bool TryQuestMobKill(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 3)
                return false;

            int raw;
            int count;
            if (!TryInt(source.Arguments[0], out raw) ||
                !TryInt(source.Arguments[2], out count))
                return false;

            plan = new KingdomQuestUnderHallExternalPlan(
                KingdomQuestUnderHallExternalPlanKind.QuestMobKill,
                source.Arguments[1],
                null,
                null,
                null,
                count,
                0,
                0,
                raw,
                0,
                string.Empty);
            return true;
        }

        private static bool TryReward(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 1)
                return false;

            plan = Build(
                KingdomQuestUnderHallExternalPlanKind.Reward,
                source.Arguments[0]);
            return true;
        }

        private static bool TryScriptFile(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 1)
                return false;

            plan = Build(
                KingdomQuestUnderHallExternalPlanKind.ScriptFile,
                source.Arguments[0]);
            return true;
        }

        private static bool TrySummonMob(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 3)
                return false;

            KingdomQuestUnderHallMobSourceRef anchor;
            KingdomQuestUnderHallMobSourceRef mob;
            int count;
            if (!KingdomQuestUnderHallSourceCatalog.TryGetMob(
                    source.Arguments[0], out anchor) ||
                !KingdomQuestUnderHallSourceCatalog.TryGetMob(
                    source.Arguments[1], out mob) ||
                anchor == null ||
                mob == null ||
                !TryInt(source.Arguments[2], out count))
                return false;

            plan = new KingdomQuestUnderHallExternalPlan(
                KingdomQuestUnderHallExternalPlanKind.SummonMob,
                source.Arguments[1],
                null,
                anchor,
                mob,
                count,
                0,
                0,
                0,
                0,
                string.Empty);
            return true;
        }

        private static bool TryWaitLogin(
            KingdomQuestUnderHallCommandSourcePlan source,
            out KingdomQuestUnderHallExternalPlan plan)
        {
            plan = null;
            if (source.Arguments.Count != 1)
                return false;

            plan = Build(
                KingdomQuestUnderHallExternalPlanKind.WaitLogin,
                source.Arguments[0]);
            return true;
        }

        private static KingdomQuestUnderHallExternalPlan Build(
            KingdomQuestUnderHallExternalPlanKind kind,
            string sourceToken)
        {
            return new KingdomQuestUnderHallExternalPlan(
                kind,
                sourceToken,
                null,
                null,
                null,
                0,
                0,
                0,
                0,
                0,
                string.Empty);
        }

        private static bool TryInt(string value, out int result)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}
