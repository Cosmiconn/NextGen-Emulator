using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestKQHBatPkAction : byte
    {
        Stop = 0,
        Start = 1,
    }

    public sealed class KingdomQuestKQHBatBattlePkNativePlan
    {
        public const uint NativeStartStepAddress = 0x004F3CB0u;
        public const uint NativeStopStepAddress = 0x004F4070u;
        public const byte NativeProtocolHeader = 0x06;
        public const byte NativeStartProtocolType = 0x12;
        public const byte NativeStopProtocolType = 0x13;
        public const int NativeSendProtocolVtableOffset = 0x304;
        public const string NativePkToken = "PK";

        public KingdomQuestKQHBatPkAction Action { get; private set; }
        public byte MapPkState { get; private set; }
        public byte ProtocolHeader { get; private set; }
        public byte ProtocolType { get; private set; }

        internal KingdomQuestKQHBatBattlePkNativePlan(
            KingdomQuestKQHBatPkAction action)
        {
            Action = action;
            MapPkState = action == KingdomQuestKQHBatPkAction.Start
                ? (byte)1
                : (byte)0;
            ProtocolHeader = NativeProtocolHeader;
            ProtocolType = action == KingdomQuestKQHBatPkAction.Start
                ? NativeStartProtocolType
                : NativeStopProtocolType;
        }
    }

    public enum KingdomQuestKQHBatTargetResultKind : byte
    {
        Success = 1,
        Fail = 2,
    }

    public sealed class KingdomQuestKQHBatSendQuestResultNativePlan
    {
        private readonly byte[] targetTokenNativeBytes;

        public const uint NativeStepAddress = 0x004F4300u;
        public const uint NativeObjectLookupAddress = 0x0054FD10u;
        public const int NativeSendProtocolVtableOffset = 0x304;
        public const byte NativeHeader = 0x16;
        public const byte NativeSuccessType = 0x12;
        public const byte NativeFailType = 0x13;
        public const string TargetIdentifier = "PlayerHandle";

        public KingdomQuestKQHBatTargetResultKind ResultKind
        {
            get;
            private set;
        }
        public ushort NativeTargetHandle { get; private set; }
        public byte PacketHeader { get; private set; }
        public byte PacketType { get; private set; }

        internal KingdomQuestKQHBatSendQuestResultNativePlan(
            KingdomQuestKQHBatTargetResultKind resultKind,
            KingdomQuestPineTokenValue targetToken,
            ushort nativeTargetHandle)
        {
            ResultKind = resultKind;
            NativeTargetHandle = nativeTargetHandle;
            PacketHeader = NativeHeader;
            PacketType = resultKind ==
                KingdomQuestKQHBatTargetResultKind.Success
                ? NativeSuccessType
                : NativeFailType;
            targetTokenNativeBytes = targetToken.SnapshotNativeBytes();
        }

        public byte[] SnapshotTargetTokenNativeBytes()
        {
            return (byte[])targetTokenNativeBytes.Clone();
        }
    }

    public sealed class KingdomQuestKQHBatIndividualRewardNativePlan
    {
        private readonly byte[] targetTokenNativeBytes;
        private readonly byte[] rewardIndexNativeBytes;

        public const uint NativeStepAddress = 0x004F4470u;
        public const uint NativeObjectLookupAddress = 0x0054FD10u;
        public const int NativeKqRewardIndexVtableOffset = 0x770;
        public const string TargetIdentifier = "PlayerHandle";

        public ushort NativeTargetHandle { get; private set; }
        public string RewardIndex { get; private set; }

        internal KingdomQuestKQHBatIndividualRewardNativePlan(
            KingdomQuestPineTokenValue targetToken,
            ushort nativeTargetHandle,
            KingdomQuestPineTokenValue rewardIndexToken)
        {
            NativeTargetHandle = nativeTargetHandle;
            RewardIndex = rewardIndexToken.Text;
            targetTokenNativeBytes = targetToken.SnapshotNativeBytes();
            rewardIndexNativeBytes = rewardIndexToken.SnapshotNativeBytes();
        }

        public byte[] SnapshotTargetTokenNativeBytes()
        {
            return (byte[])targetTokenNativeBytes.Clone();
        }

        public byte[] SnapshotRewardIndexNativeBytes()
        {
            return (byte[])rewardIndexNativeBytes.Clone();
        }
    }

    public sealed class KingdomQuestKQHBatRevivalAllNativePlan
    {
        public const uint NativeStepAddress = 0x004F7070u;
        public const uint NativeAxialListCtorAddress = 0x0042A060u;
        public const uint NativeAxialListWorkAddress = 0x0042A4B0u;
        public const uint NativeReviveLoopAddress = 0x00429F40u;
        public const uint NativeReviveRequestAddress = 0x00451BD0u;
        public const byte NativePlayerObjectType = 2;
        public const string NativeAllToken = "all";
    }

    public sealed class KingdomQuestKQHBatAbStateSetNativePlan
    {
        private readonly byte[] scopeTokenNativeBytes;

        public const uint NativeStepAddress = 0x004F1010u;
        public const uint NativeObjectLookupAddress = 0x0054FD10u;
        public const uint NativeAxialListCtorAddress = 0x004285C0u;
        public const uint NativeAxialListApplyAddress = 0x004285F0u;
        public const uint NativeAllInMapAddress = 0x0054B9E0u;
        public const ushort SourceAbStateId = 8500;
        public const int NativeAbStateIndex = 306;
        public const int NativeStrength = 1;
        public const int NativeDurationMilliseconds = 3000;
        public const string NativeAbStateName = "StaCommonStun01";
        public const string ScopeIdentifier = "LooterHandle";
        public const string NativeAllTarget = "all";

        public ushort NativeScopeHandle { get; private set; }

        internal KingdomQuestKQHBatAbStateSetNativePlan(
            KingdomQuestPineTokenValue scopeToken,
            ushort nativeScopeHandle)
        {
            NativeScopeHandle = nativeScopeHandle;
            scopeTokenNativeBytes = scopeToken.SnapshotNativeBytes();
        }

        public byte[] SnapshotScopeTokenNativeBytes()
        {
            return (byte[])scopeTokenNativeBytes.Clone();
        }
    }

    public sealed class KingdomQuestKQHBatLinkToAllNativePlan
    {
        public const uint NativeStepAddress = 0x004EFB10u;
        public const uint NativeAxialListCtorAddress = 0x00428510u;
        public const uint NativeAxialListWorkAddress = 0x00428950u;
        public const int NativeLinkVirtualOffset = 0x3FC;
        public const int NativeMapNameBytes = 12;
        public const string NativeAllTarget = "all";

        public string ServerMapName { get; private set; }
        public string ClientMapName { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        internal KingdomQuestKQHBatLinkToAllNativePlan(
            string serverMapName,
            string clientMapName,
            int x,
            int y)
        {
            ServerMapName = serverMapName ?? string.Empty;
            ClientMapName = clientMapName ?? string.Empty;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Builds only the KQHBat command effects whose Zone.exe/PDB behavior is
    /// recovered far enough to cross a typed native-owner boundary.
    ///
    /// Native ShineObject handles remain opaque ushort identities obtained
    /// from PineScriptToken::pst_GetNumber. They are never treated as emulator
    /// MapObjectID values here.
    /// </summary>
    public static class KingdomQuestKQHBatNativePlanBuilder
    {
        public const int NativeClosedOccurrenceCount = 85;
        public const int NativeClosedVerbCount = 9;
        public const int RemainingUnresolvedOccurrenceCount = 120;
        public const int RemainingUnresolvedVerbCount = 2;

        public static bool TryBuildBattlePk(
            KingdomQuestKQHBatExternalPlan source,
            out KingdomQuestKQHBatBattlePkNativePlan plan)
        {
            plan = null;
            if (source == null)
                return false;

            if (source.Kind == KingdomQuestKQHBatExternalKind.BattleStart &&
                string.Equals(
                    source.CommandText,
                    "battlestart PK.",
                    StringComparison.Ordinal))
            {
                plan = new KingdomQuestKQHBatBattlePkNativePlan(
                    KingdomQuestKQHBatPkAction.Start);
                return true;
            }

            if (source.Kind == KingdomQuestKQHBatExternalKind.BattleStop &&
                string.Equals(
                    source.CommandText,
                    "battlestop PK.",
                    StringComparison.Ordinal))
            {
                plan = new KingdomQuestKQHBatBattlePkNativePlan(
                    KingdomQuestKQHBatPkAction.Stop);
                return true;
            }

            return false;
        }

        public static bool TryBuildSendQuestResult(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatSendQuestResultNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind !=
                    KingdomQuestKQHBatExternalKind.SendQuestResult)
                return false;

            KingdomQuestKQHBatTargetResultKind resultKind;
            if (string.Equals(
                    source.CommandText,
                    "sendquestresult Suc PlayerHandle.",
                    StringComparison.Ordinal))
                resultKind = KingdomQuestKQHBatTargetResultKind.Success;
            else if (string.Equals(
                    source.CommandText,
                    "sendquestresult Fail PlayerHandle.",
                    StringComparison.Ordinal))
                resultKind = KingdomQuestKQHBatTargetResultKind.Fail;
            else
                return false;

            KingdomQuestPineTokenValue targetToken;
            ushort nativeTargetHandle;
            if (!TryResolveNativeTarget(
                    variables,
                    KingdomQuestKQHBatSendQuestResultNativePlan.TargetIdentifier,
                    out targetToken,
                    out nativeTargetHandle))
                return false;

            plan = new KingdomQuestKQHBatSendQuestResultNativePlan(
                resultKind,
                targetToken,
                nativeTargetHandle);
            return true;
        }

        public static bool TryBuildIndividualReward(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatIndividualRewardNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind !=
                    KingdomQuestKQHBatExternalKind.IndividualReward)
                return false;

            string rewardExpression;
            if (string.Equals(
                    source.CommandText,
                    "invidualreward PlayerHandle \"HERO_\" % InitFlag % \"_\" % Count.",
                    StringComparison.Ordinal))
            {
                rewardExpression =
                    "\"HERO_\" % InitFlag % \"_\" % Count";
            }
            else if (string.Equals(
                    source.CommandText,
                    "invidualreward PlayerHandle \"HERO_\" % InitFlag % \"_3\".",
                    StringComparison.Ordinal))
            {
                rewardExpression =
                    "\"HERO_\" % InitFlag % \"_3\"";
            }
            else
                return false;

            KingdomQuestPineTokenValue targetToken;
            ushort nativeTargetHandle;
            if (!TryResolveNativeTarget(
                    variables,
                    KingdomQuestKQHBatIndividualRewardNativePlan.TargetIdentifier,
                    out targetToken,
                    out nativeTargetHandle))
                return false;

            var rewardIndexToken = new KingdomQuestPineTokenValue();
            if (KingdomQuestPineBasicExpression.TryCalculate(
                    rewardExpression,
                    variables,
                    rewardIndexToken) !=
                    KingdomQuestPineExpressionResolution.Success ||
                string.IsNullOrEmpty(rewardIndexToken.Text))
                return false;

            plan = new KingdomQuestKQHBatIndividualRewardNativePlan(
                targetToken,
                nativeTargetHandle,
                rewardIndexToken);
            return true;
        }

        public static bool TryBuildRevivalAll(
            KingdomQuestKQHBatExternalPlan source,
            out KingdomQuestKQHBatRevivalAllNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.Revival ||
                !string.Equals(
                    source.CommandText,
                    "revival all.",
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestKQHBatRevivalAllNativePlan();
            return true;
        }

        public static bool TryBuildAbStateSet(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatAbStateSetNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.AbStateSet ||
                !string.Equals(
                    source.CommandText,
                    "abstateset all \"StaCommonStun01\" 1 3000 LooterHandle.",
                    StringComparison.Ordinal))
                return false;

            KingdomQuestPineTokenValue scopeToken;
            ushort nativeScopeHandle;
            if (!TryResolveNativeTarget(
                    variables,
                    KingdomQuestKQHBatAbStateSetNativePlan.ScopeIdentifier,
                    out scopeToken,
                    out nativeScopeHandle))
                return false;

            plan = new KingdomQuestKQHBatAbStateSetNativePlan(
                scopeToken,
                nativeScopeHandle);
            return true;
        }

        public static bool TryBuildLinkToAll(
            KingdomQuestKQHBatExternalPlan source,
            out KingdomQuestKQHBatLinkToAllNativePlan plan)
        {
            plan = null;
            if (source == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.LinkTo ||
                (source.CanonicalLine != 91 && source.CanonicalLine != 148))
                return false;

            string mapName;
            int x;
            int y;
            switch (source.ScriptLanguage)
            {
                case "KQ/KQHBat1":
                case "KQ/KQHBat2":
                    mapName = "Eld";
                    x = 17214;
                    y = 13445;
                    break;

                case "KQ/KQHBat3":
                    mapName = "Urg";
                    x = 6293;
                    y = 5477;
                    break;

                case "KQ/KQHBat4":
                    mapName = "Urg_Alruin";
                    x = 6120;
                    y = 10286;
                    break;

                case "KQ/KQHBat5":
                    mapName = "Adl";
                    x = 11674;
                    y = 9329;
                    break;

                default:
                    return false;
            }

            string expected =
                "linkto all \"" + mapName + "\" \"" + mapName +
                "\" " + x.ToString() + " " + y.ToString() + ".";
            if (!string.Equals(
                    source.CommandText,
                    expected,
                    StringComparison.Ordinal) ||
                System.Text.Encoding.ASCII.GetByteCount(mapName) >=
                    KingdomQuestKQHBatLinkToAllNativePlan.NativeMapNameBytes)
                return false;

            plan = new KingdomQuestKQHBatLinkToAllNativePlan(
                mapName,
                mapName,
                x,
                y);
            return true;
        }

        private static bool TryResolveNativeTarget(
            KingdomQuestPineVariableStack variables,
            string identifier,
            out KingdomQuestPineTokenValue targetToken,
            out ushort nativeTargetHandle)
        {
            targetToken = null;
            nativeTargetHandle = 0;
            if (variables == null ||
                string.IsNullOrEmpty(identifier) ||
                !variables.TryFind(identifier, out targetToken) ||
                targetToken == null)
                return false;

            int prefixLength;
            int nativeNumber =
                KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                    targetToken.Text,
                    out prefixLength);
            nativeTargetHandle = unchecked((ushort)nativeNumber);
            return true;
        }
    }
}
