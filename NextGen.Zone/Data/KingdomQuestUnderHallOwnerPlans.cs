using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// UnderHall-specific validators for the three unresolved native mutation
    /// families. The resulting owner-plan types are script-neutral and shared
    /// with UnderHall2.
    ///
    /// Source-site, MapInfo/MobInfo identity and literal operand shape remain
    /// exact UnderHall constraints. Regen/summon snapshot the complete 0x100
    /// byte Pine runtime-handle token. No map transfer, object creation,
    /// spawning, scheduling or runtime-handle interpretation occurs here.
    /// </summary>
    public static class KingdomQuestUnderHallOwnerPlanBuilder
    {
        public const int LinkToOccurrenceCount = 2;
        public const int MobRegenOccurrenceCount = 1;
        public const int SummonMobOccurrenceCount = 12;

        public const string RuntimeHandleIdentifier = "KQ_BossRobo";
        public const int MobRegenCanonicalLine = 339;
        public const string MobRegenBlock = "Nineteenth";
        public const int MobRegenX = 2300;
        public const int MobRegenY = 2500;
        public const int MobRegenRawNumeric1 = 90;
        public const int MobRegenRawNumeric2 = 1000;
        public const string MobRegenRawText1 = "Normal";

        public static bool TryBuildLinkTo(
            KingdomQuestUnderHallExternalPlan source,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            out KingdomQuestPineLinkToOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                sourceSite == null ||
                source.Kind != KingdomQuestUnderHallExternalPlanKind.LinkTo ||
                sourceSite.Kind != KingdomQuestUnderHallExternalPlanKind.LinkTo ||
                source.Map == null ||
                source.Map.MapId !=
                    KingdomQuestUnderHallSourceCatalog.ElderineMapId ||
                !string.Equals(
                    source.Map.MapName, "Eld", StringComparison.Ordinal) ||
                !string.Equals(
                    source.RawText1, "Eld", StringComparison.Ordinal) ||
                !string.Equals(
                    source.SourceToken, "all", StringComparison.Ordinal) ||
                source.Map.RegenX != 17214 ||
                source.Map.RegenY != 13445 ||
                source.X != 17214 ||
                source.Y != 13445)
                return false;

            bool successSite =
                sourceSite.CanonicalLine == 388 &&
                string.Equals(
                    sourceSite.TopLevelBlock,
                    KingdomQuestUnderHallSourceFlow.SuccessBlock,
                    StringComparison.Ordinal);
            bool failSite =
                sourceSite.CanonicalLine == 402 &&
                string.Equals(
                    sourceSite.TopLevelBlock,
                    KingdomQuestUnderHallSourceFlow.FailureBlock,
                    StringComparison.Ordinal);
            if (!successSite && !failSite)
                return false;

            plan = new KingdomQuestPineLinkToOwnerPlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.SourceToken,
                source.Map.MapId,
                source.Map.MapName,
                source.RawText1,
                source.X,
                source.Y);
            return true;
        }

        public static bool TryBuildMobRegen(
            KingdomQuestUnderHallExternalPlan source,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineTokenValue runtimeHandleToken,
            DataProvider data,
            out KingdomQuestPineMobRegenOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                sourceSite == null ||
                runtimeHandleToken == null ||
                source.Kind != KingdomQuestUnderHallExternalPlanKind.MobRegen ||
                sourceSite.Kind !=
                    KingdomQuestUnderHallExternalPlanKind.MobRegen ||
                sourceSite.CanonicalLine != MobRegenCanonicalLine ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    MobRegenBlock,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.RuntimeHandleIdentifier,
                    RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                source.Mob == null ||
                !string.Equals(
                    source.Mob.InxName,
                    "KQ_BossRobo",
                    StringComparison.Ordinal) ||
                source.Mob.MobId != 1068 ||
                source.X != MobRegenX ||
                source.Y != MobRegenY ||
                source.RawNumeric1 != MobRegenRawNumeric1 ||
                source.RawNumeric2 != MobRegenRawNumeric2 ||
                !string.Equals(
                    source.RawText1,
                    MobRegenRawText1,
                    StringComparison.Ordinal) ||
                !ValidateMobIdentity(source.Mob, data) ||
                runtimeHandleToken.SnapshotNativeBytes().Length !=
                    KingdomQuestPineTokenValue.NativeByteCapacity)
                return false;

            plan = new KingdomQuestPineMobRegenOwnerPlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.RuntimeHandleIdentifier,
                runtimeHandleToken,
                source.Mob.MobId,
                source.Mob.InxName,
                source.Mob.DisplayName,
                source.X,
                source.Y,
                source.RawNumeric1,
                source.RawNumeric2,
                source.RawText1);
            return true;
        }

        public static bool TryBuildSummonMob(
            KingdomQuestUnderHallExternalPlan source,
            KingdomQuestUnderHallExternalSourceSite sourceSite,
            KingdomQuestPineTokenValue runtimeHandleToken,
            DataProvider data,
            out KingdomQuestPineSummonMobOwnerPlan plan)
        {
            plan = null;
            string expectedBlock;
            string expectedMob;
            int expectedCount;
            if (source == null ||
                sourceSite == null ||
                runtimeHandleToken == null ||
                source.Kind != KingdomQuestUnderHallExternalPlanKind.SummonMob ||
                sourceSite.Kind !=
                    KingdomQuestUnderHallExternalPlanKind.SummonMob ||
                !TryExpectedSummon(
                    sourceSite.CanonicalLine,
                    out expectedBlock,
                    out expectedMob,
                    out expectedCount) ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    expectedBlock,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.RuntimeHandleIdentifier,
                    RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                source.Mob == null ||
                !string.Equals(
                    source.Mob.InxName,
                    expectedMob,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.SourceToken,
                    expectedMob,
                    StringComparison.Ordinal) ||
                source.Count != expectedCount ||
                !ValidateMobIdentity(source.Mob, data) ||
                runtimeHandleToken.SnapshotNativeBytes().Length !=
                    KingdomQuestPineTokenValue.NativeByteCapacity)
                return false;

            plan = new KingdomQuestPineSummonMobOwnerPlan(
                sourceSite.CanonicalLine,
                sourceSite.TopLevelBlock,
                source.RuntimeHandleIdentifier,
                runtimeHandleToken,
                source.Mob.MobId,
                source.Mob.InxName,
                source.Mob.DisplayName,
                source.Count);
            return true;
        }

        private static bool ValidateMobIdentity(
            KingdomQuestUnderHallMobSourceRef source,
            DataProvider data)
        {
            if (source == null ||
                data == null ||
                data.MobsByName == null ||
                data.MobData == null)
                return false;

            MobInfo clientInfo;
            MobInfoServer serverInfo;
            return data.MobsByName.TryGetValue(
                    source.InxName, out clientInfo) &&
                clientInfo != null &&
                data.MobData.TryGetValue(
                    source.InxName, out serverInfo) &&
                serverInfo != null &&
                clientInfo.ID == source.MobId &&
                serverInfo.ID == source.MobId;
        }

        private static bool TryExpectedSummon(
            int canonicalLine,
            out string block,
            out string mobIndex,
            out int count)
        {
            block = null;
            mobIndex = null;
            count = 0;

            switch (canonicalLine)
            {
                case 355:
                    block = "Summon1";
                    mobIndex = "KQ_DesertWolf";
                    count = 3;
                    return true;
                case 356:
                    block = "Summon1";
                    mobIndex = "KQ_GiantMushRoom";
                    count = 2;
                    return true;
                case 359:
                    block = "Summon2";
                    mobIndex = "KQ_Skeleton";
                    count = 5;
                    return true;
                case 362:
                    block = "Summon3";
                    mobIndex = "KQ_SkelWarrior";
                    count = 3;
                    return true;
                case 363:
                    block = "Summon3";
                    mobIndex = "KQ_SkelKnight";
                    count = 2;
                    return true;
                case 364:
                    block = "Summon3";
                    mobIndex = "KQ_SkelArcher";
                    count = 4;
                    return true;
                case 367:
                    block = "Summon4";
                    mobIndex = "KQ_Skeleton";
                    count = 5;
                    return true;
                case 368:
                    block = "Summon4";
                    mobIndex = "KQ_WildKebing";
                    count = 5;
                    return true;
                case 369:
                    block = "Summon4";
                    mobIndex = "KQ_Zombie";
                    count = 5;
                    return true;
                case 372:
                    block = "Summon5";
                    mobIndex = "KQ_SkelWarrior";
                    count = 5;
                    return true;
                case 373:
                    block = "Summon5";
                    mobIndex = "KQ_RapidBoar";
                    count = 5;
                    return true;
                case 374:
                    block = "Summon5";
                    mobIndex = "KQ_FireViVi";
                    count = 6;
                    return true;
                default:
                    return false;
            }
        }
    }
}
