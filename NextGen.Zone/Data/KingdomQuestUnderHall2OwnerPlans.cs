using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Final mutation-free owner boundary for the three UnderHall2 families
    /// whose actual native mutation is still unresolved: linkto, mobregen and
    /// summonmob.
    ///
    /// The incoming UnderHall2 plan has already passed exact canonical
    /// line/block/text validation. This builder adds live DataProvider MobInfo
    /// identity checks and snapshots the complete 0x100-byte Pine runtime token
    /// for regen/summon. It does not transfer a player, breed a mob, schedule
    /// regen or interpret the runtime handle.
    /// </summary>
    public static class KingdomQuestUnderHall2OwnerPlanBuilder
    {
        public const int LinkToOccurrenceCount = 3;
        public const int MobRegenOccurrenceCount = 1;
        public const int SummonMobOccurrenceCount = 54;

        public static bool TryBuildLinkTo(
            KingdomQuestUnderHall2ExternalPlan source,
            out KingdomQuestPineLinkToOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestUnderHall2ExternalKind.LinkTo ||
                source.Map == null ||
                !string.Equals(
                    source.TargetToken,
                    "all",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.RawText1,
                    source.Map.MapName,
                    StringComparison.Ordinal) ||
                !IsLinkSite(source.CanonicalLine, source.TopLevelBlock))
                return false;

            plan = new KingdomQuestPineLinkToOwnerPlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                source.TargetToken,
                source.Map.MapId,
                source.Map.MapName,
                source.RawText1,
                source.X,
                source.Y);
            return true;
        }

        public static bool TryBuildMobRegen(
            KingdomQuestUnderHall2ExternalPlan source,
            KingdomQuestPineTokenValue runtimeHandleToken,
            DataProvider data,
            out KingdomQuestPineMobRegenOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                runtimeHandleToken == null ||
                source.Kind != KingdomQuestUnderHall2ExternalKind.MobRegen ||
                source.CanonicalLine !=
                    KingdomQuestUnderHall2ExternalPlanBuilder
                        .MobRegenCanonicalLine ||
                !string.Equals(
                    source.TopLevelBlock,
                    KingdomQuestUnderHall2ExternalPlanBuilder.MobRegenBlock,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    source.RuntimeHandleIdentifier,
                    KingdomQuestUnderHall2ExternalPlanBuilder
                        .RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                source.Mob == null ||
                !ValidateMobIdentity(source.Mob, data) ||
                runtimeHandleToken.SnapshotNativeBytes().Length !=
                    KingdomQuestPineTokenValue.NativeByteCapacity)
                return false;

            plan = new KingdomQuestPineMobRegenOwnerPlan(
                source.CanonicalLine,
                source.TopLevelBlock,
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
            KingdomQuestUnderHall2ExternalPlan source,
            KingdomQuestPineTokenValue runtimeHandleToken,
            DataProvider data,
            out KingdomQuestPineSummonMobOwnerPlan plan)
        {
            plan = null;
            if (source == null ||
                runtimeHandleToken == null ||
                source.Kind != KingdomQuestUnderHall2ExternalKind.SummonMob ||
                !string.Equals(
                    source.RuntimeHandleIdentifier,
                    KingdomQuestUnderHall2ExternalPlanBuilder
                        .RuntimeHandleIdentifier,
                    StringComparison.Ordinal) ||
                source.Mob == null ||
                source.Count <= 0 ||
                !ValidateMobIdentity(source.Mob, data) ||
                runtimeHandleToken.SnapshotNativeBytes().Length !=
                    KingdomQuestPineTokenValue.NativeByteCapacity)
                return false;

            KingdomQuestUnderHall2ExternalSourceSite sourceSite;
            if (!KingdomQuestUnderHall2SourceFlow.TryResolve(
                    source.CanonicalLine,
                    KingdomQuestUnderHall2ExternalKind.SummonMob,
                    out sourceSite) ||
                sourceSite == null ||
                !string.Equals(
                    sourceSite.TopLevelBlock,
                    source.TopLevelBlock,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestPineSummonMobOwnerPlan(
                source.CanonicalLine,
                source.TopLevelBlock,
                source.RuntimeHandleIdentifier,
                runtimeHandleToken,
                source.Mob.MobId,
                source.Mob.InxName,
                source.Mob.DisplayName,
                source.Count);
            return true;
        }

        private static bool ValidateMobIdentity(
            KingdomQuestUnderHall2MobSourceRef source,
            DataProvider data)
        {
            if (source == null ||
                data == null ||
                data.MobsByName == null ||
                data.MobData == null)
                return false;

            MobInfo clientInfo;
            MobInfoServer serverInfo;
            return data.MobsByName.TryGetValue(source.InxName, out clientInfo) &&
                clientInfo != null &&
                data.MobData.TryGetValue(source.InxName, out serverInfo) &&
                serverInfo != null &&
                clientInfo.ID == source.MobId &&
                serverInfo.ID == source.MobId;
        }

        private static bool IsLinkSite(int line, string block)
        {
            return
                (line == 553 &&
                    string.Equals(
                        block, "QuestSuc", StringComparison.Ordinal)) ||
                (line == 571 &&
                    string.Equals(
                        block, "QuestSuc2", StringComparison.Ordinal)) ||
                (line == 585 &&
                    string.Equals(
                        block, "QuestFail", StringComparison.Ordinal));
        }
    }
}
