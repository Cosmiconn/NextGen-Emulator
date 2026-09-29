using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Shared fail-closed owner plan for source-resolved Pine linkto commands.
    /// It describes target/map/coordinates only and performs no transfer.
    /// </summary>
    public sealed class KingdomQuestPineLinkToOwnerPlan
    {
        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string TargetToken { get; private set; }
        public ushort MapId { get; private set; }
        public string MapName { get; private set; }
        public string MapAlias { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        internal KingdomQuestPineLinkToOwnerPlan(
            int canonicalLine,
            string topLevelBlock,
            string targetToken,
            ushort mapId,
            string mapName,
            string mapAlias,
            int x,
            int y)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            TargetToken = targetToken ?? string.Empty;
            MapId = mapId;
            MapName = mapName ?? string.Empty;
            MapAlias = mapAlias ?? string.Empty;
            X = x;
            Y = y;
        }
    }

    public sealed class KingdomQuestPineMobRegenOwnerPlan
    {
        private readonly byte[] runtimeHandleNativeBytes;

        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string RuntimeHandleIdentifier { get; private set; }
        public string RuntimeHandleText { get; private set; }
        public ushort MobId { get; private set; }
        public string MobIndex { get; private set; }
        public string MobDisplayName { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int RawNumeric1 { get; private set; }
        public int RawNumeric2 { get; private set; }
        public string RawText1 { get; private set; }

        internal KingdomQuestPineMobRegenOwnerPlan(
            int canonicalLine,
            string topLevelBlock,
            string runtimeHandleIdentifier,
            KingdomQuestPineTokenValue runtimeHandleToken,
            ushort mobId,
            string mobIndex,
            string mobDisplayName,
            int x,
            int y,
            int rawNumeric1,
            int rawNumeric2,
            string rawText1)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            RuntimeHandleIdentifier = runtimeHandleIdentifier ?? string.Empty;
            RuntimeHandleText = runtimeHandleToken == null
                ? string.Empty
                : runtimeHandleToken.Text;
            runtimeHandleNativeBytes = runtimeHandleToken == null
                ? new byte[0]
                : runtimeHandleToken.SnapshotNativeBytes();
            MobId = mobId;
            MobIndex = mobIndex ?? string.Empty;
            MobDisplayName = mobDisplayName ?? string.Empty;
            X = x;
            Y = y;
            RawNumeric1 = rawNumeric1;
            RawNumeric2 = rawNumeric2;
            RawText1 = rawText1 ?? string.Empty;
        }

        public byte[] SnapshotRuntimeHandleNativeBytes()
        {
            return (byte[])runtimeHandleNativeBytes.Clone();
        }
    }

    public sealed class KingdomQuestPineSummonMobOwnerPlan
    {
        private readonly byte[] runtimeHandleNativeBytes;

        public int CanonicalLine { get; private set; }
        public string TopLevelBlock { get; private set; }
        public string RuntimeHandleIdentifier { get; private set; }
        public string RuntimeHandleText { get; private set; }
        public ushort MobId { get; private set; }
        public string MobIndex { get; private set; }
        public string MobDisplayName { get; private set; }
        public int Count { get; private set; }

        internal KingdomQuestPineSummonMobOwnerPlan(
            int canonicalLine,
            string topLevelBlock,
            string runtimeHandleIdentifier,
            KingdomQuestPineTokenValue runtimeHandleToken,
            ushort mobId,
            string mobIndex,
            string mobDisplayName,
            int count)
        {
            CanonicalLine = canonicalLine;
            TopLevelBlock = topLevelBlock ?? string.Empty;
            RuntimeHandleIdentifier = runtimeHandleIdentifier ?? string.Empty;
            RuntimeHandleText = runtimeHandleToken == null
                ? string.Empty
                : runtimeHandleToken.Text;
            runtimeHandleNativeBytes = runtimeHandleToken == null
                ? new byte[0]
                : runtimeHandleToken.SnapshotNativeBytes();
            MobId = mobId;
            MobIndex = mobIndex ?? string.Empty;
            MobDisplayName = mobDisplayName ?? string.Empty;
            Count = count;
        }

        public byte[] SnapshotRuntimeHandleNativeBytes()
        {
            return (byte[])runtimeHandleNativeBytes.Clone();
        }
    }
}
