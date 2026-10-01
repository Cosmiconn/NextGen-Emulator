using System;
using System.Collections.Generic;
using System.Text;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestKQHBatScriptTextSource
    {
        public string ScriptLanguage { get; private set; }
        public string ScriptFileKey { get; private set; }
        public string Sha256 { get; private set; }
        public string ReturnDestination { get; private set; }

        internal KingdomQuestKQHBatScriptTextSource(
            string scriptLanguage,
            string scriptFileKey,
            string sha256,
            string returnDestination)
        {
            ScriptLanguage = scriptLanguage ?? string.Empty;
            ScriptFileKey = scriptFileKey ?? string.Empty;
            Sha256 = sha256 ?? string.Empty;
            ReturnDestination = returnDestination ?? string.Empty;
        }
    }

    /// <summary>
    /// Exact source text projection of Script/KQHBat1..5.txt.
    /// Chat records are common across all five files; only KQReturn
    /// destinations differ by stage.
    /// </summary>
    public static class KingdomQuestKQHBatTextSourceCatalog
    {
        private static readonly Dictionary<string, KingdomQuestKQHBatScriptTextSource>
            Sources =
                new Dictionary<string, KingdomQuestKQHBatScriptTextSource>(
                    StringComparer.Ordinal)
                {
                    { "KQ/KQHBat1", Source(
                        "KQ/KQHBat1", "KQHBat1",
                        "ed41dc52b4b4042ae430f74e559ab547995e31667ff8b498825a3842a61ec156",
                        "Elderine") },
                    { "KQ/KQHBat2", Source(
                        "KQ/KQHBat2", "KQHBat2",
                        "ed41dc52b4b4042ae430f74e559ab547995e31667ff8b498825a3842a61ec156",
                        "Elderine") },
                    { "KQ/KQHBat3", Source(
                        "KQ/KQHBat3", "KQHBat3",
                        "8f1073942debae21c847609c042aa4337717e2112f38c298b061e67313bc1943",
                        "Uruga") },
                    { "KQ/KQHBat4", Source(
                        "KQ/KQHBat4", "KQHBat4",
                        "ab821667e63fe85a9abd4b6ded493ad0eecbf3710183e3c9d36159854789c963",
                        "Alberstol Ruins") },
                    { "KQ/KQHBat5", Source(
                        "KQ/KQHBat5", "KQHBat5",
                        "0e2cddcaad8028693807a62c04e977e32a6e1ba5a24846c2c54b8713bdc61e36",
                        "Adealia") },
                };

        private static readonly Dictionary<string, string> CommonRecords =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Intro0", "To those who wishes to test their might, I welcome you." },
                { "Intro1", "In such turbulent times as this, we are in dire need of heroes like you." },
                { "Intro2", "Let go of your inhibitions, and unleash all the strength and knowledge you have accumulated so far." },
                { "Intro3", "Please remember that in the Mystic Weapon Chest, you'll find mighty weapon that you may have not seen yet." },
                { "Intro4", "Best of the best will be rewarded accordingly." },
                { "Intro5", "Of course, the opposite will be true to those call themselves a warrior that falls below 5000 points or less." },
                { "Intro6", "Well, are you ready?  What are we waiting for, let's get ready to rumble!" },
                { "DualStart", "Let the battle begin!" },
                { "DualStop", "Stop!  Battle is over!" },
                { "DualResult", "1st place %s, 2nd place %s, and 3rd place goes to %s.  These three are truly the mightiest warriors!" },
                { "DualResult1", "Those who received less than 5000 points do not deserves to be called a warriors!" },
                { "DualResult2", "If you received less than 5000 points, it isn't end of the world, just practice harder and live the code of warriors. " },
                { "DualResult3", "I hope I see your more fierce self, next time we meet." },
                { "DualResult4", "Dismissed!" },
            };

        public static bool TryGetSource(
            string scriptLanguage,
            out KingdomQuestKQHBatScriptTextSource source)
        {
            source = null;
            if (scriptLanguage == null ||
                !Sources.TryGetValue(scriptLanguage, out source) ||
                source == null)
                return false;

            KingdomQuestPineScriptFileSource pineSource;
            return KingdomQuestPineScriptFile.TryGetSource(
                    source.ScriptFileKey, out pineSource) &&
                pineSource != null &&
                string.Equals(
                    pineSource.RelativePath,
                    "Script/" + source.ScriptFileKey + ".txt",
                    StringComparison.Ordinal) &&
                string.Equals(
                    pineSource.Sha256,
                    source.Sha256,
                    StringComparison.Ordinal);
        }

        public static bool TryGetRecord(
            string scriptLanguage,
            string key,
            out string text)
        {
            text = null;
            KingdomQuestKQHBatScriptTextSource source;
            if (!TryGetSource(scriptLanguage, out source) ||
                string.IsNullOrEmpty(key))
                return false;

            if (CommonRecords.TryGetValue(key, out text))
                return true;

            int seconds;
            switch (key)
            {
                case "KQReturn30":
                    seconds = 30;
                    break;
                case "KQReturn20":
                    seconds = 20;
                    break;
                case "KQReturn10":
                    seconds = 10;
                    break;
                case "KQReturn5":
                    seconds = 5;
                    break;
                default:
                    return false;
            }

            text =
                "Returning to " + source.ReturnDestination +
                " in " + seconds.ToString() + " seconds";
            return true;
        }

        private static KingdomQuestKQHBatScriptTextSource Source(
            string scriptLanguage,
            string scriptFileKey,
            string sha256,
            string returnDestination)
        {
            return new KingdomQuestKQHBatScriptTextSource(
                scriptLanguage,
                scriptFileKey,
                sha256,
                returnDestination);
        }
    }

    public sealed class KingdomQuestKQHBatBroadcastAllNativePlan
    {
        private readonly byte[] charNameSourceTokenNativeBytes;
        private readonly string fixedMessage;
        private readonly string dynamicSuffix;

        public const uint NativeStepAddress = 0x004EF830u;
        public const uint NativeAxialListWallCtorAddress = 0x004281A0u;
        public const uint NativeAxialListWallWorkAddress = 0x004281C0u;
        public const uint NativeAllInMapAddress = 0x0054B9E0u;

        public int NoticeVtableOffset
        {
            get { return KingdomQuestPineBroadcastAllNative.NoticeVtableOffset; }
        }
        public int CanonicalLine { get; private set; }
        public string ScriptLanguage { get; private set; }
        public string ExpectedScriptFileKey { get; private set; }
        public string MessageKey { get; private set; }
        public bool RequiresNativeCharName { get; private set; }
        public ushort NativeCharNameHandle { get; private set; }

        internal KingdomQuestKQHBatBroadcastAllNativePlan(
            KingdomQuestKQHBatExternalPlan source,
            string expectedScriptFileKey,
            string messageKey,
            string fixedMessage)
        {
            CanonicalLine = source.CanonicalLine;
            ScriptLanguage = source.ScriptLanguage;
            ExpectedScriptFileKey = expectedScriptFileKey ?? string.Empty;
            MessageKey = messageKey ?? string.Empty;
            this.fixedMessage = fixedMessage ?? string.Empty;
            RequiresNativeCharName = false;
        }

        internal KingdomQuestKQHBatBroadcastAllNativePlan(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineTokenValue charNameSourceToken,
            ushort nativeCharNameHandle,
            string dynamicSuffix)
        {
            CanonicalLine = source.CanonicalLine;
            ScriptLanguage = source.ScriptLanguage;
            ExpectedScriptFileKey = string.Empty;
            MessageKey = string.Empty;
            RequiresNativeCharName = true;
            NativeCharNameHandle = nativeCharNameHandle;
            this.dynamicSuffix = dynamicSuffix ?? string.Empty;
            charNameSourceTokenNativeBytes =
                charNameSourceToken.SnapshotNativeBytes();
        }

        public byte[] SnapshotCharNameSourceTokenNativeBytes()
        {
            return charNameSourceTokenNativeBytes == null
                ? null
                : (byte[])charNameSourceTokenNativeBytes.Clone();
        }

        public bool TryResolveMessage(
            IKingdomQuestPineNativeObjectNameResolver resolver,
            out string message)
        {
            message = null;
            if (!RequiresNativeCharName)
            {
                message = fixedMessage;
                return true;
            }

            if (resolver == null)
                return false;

            string characterName;
            if (!resolver.TryResolveCharName(
                    NativeCharNameHandle, out characterName))
            {
                // SysFuncShineCharName returns its already-cleared token when
                // native som_GetObject misses.
                characterName = string.Empty;
            }

            if (characterName == null)
                return false;

            message = characterName + dynamicSuffix;
            return true;
        }
    }

    public sealed class KingdomQuestKQHBatChatWindowNativePlan
    {
        public const uint NativeStepAddress = 0x004F50F0u;
        public const uint NativeNpcLookupAddress = 0x0063CC30u;
        public const uint NativeScriptStringLookupAddress = 0x0048CE60u;
        public const ushort NativePacketOpcode = 0x6C0C;
        public const int NativeArgumentSlotCount = 7;
        public const int NativePacketTextCapacity = 0x100;
        public const int NativePacketNpcIdOffset = 2;
        public const int NativePacketTextLengthOffset = 4;
        public const int NativePacketTextOffset = 5;
        public const uint NativePacketBroadcastCtorAddress = 0x00428A40u;
        public const uint NativePacketBroadcastWorkAddress = 0x00428B20u;
        public const uint NativePacketBroadcastDtorAddress = 0x00428D00u;
        public const uint NativeAllInMapAddress = 0x0054B9E0u;

        public ushort NpcId { get; private set; }
        public string NpcIndex { get; private set; }
        public string ScriptFileKey { get; private set; }
        public string RecordKey { get; private set; }
        public string FormatText { get; private set; }
        public string FormattedText { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; }

        internal KingdomQuestKQHBatChatWindowNativePlan(
            ushort npcId,
            string npcIndex,
            string scriptFileKey,
            string recordKey,
            string formatText,
            string formattedText,
            IList<string> arguments)
        {
            NpcId = npcId;
            NpcIndex = npcIndex ?? string.Empty;
            ScriptFileKey = scriptFileKey ?? string.Empty;
            RecordKey = recordKey ?? string.Empty;
            FormatText = formatText ?? string.Empty;
            FormattedText = formattedText ?? string.Empty;
            Arguments = new List<string>(arguments).AsReadOnly();
        }

        public bool TryCreateNativeWire(out byte[] wire)
        {
            wire = null;
            byte[] textBytes = Encoding.ASCII.GetBytes(FormattedText);
            if (textBytes.Length >= NativePacketTextCapacity)
                return false;

            wire = new byte[NativePacketTextOffset + textBytes.Length];
            wire[0] = unchecked((byte)(NativePacketOpcode & 0xFF));
            wire[1] = unchecked((byte)(NativePacketOpcode >> 8));
            wire[NativePacketNpcIdOffset] = unchecked((byte)(NpcId & 0xFF));
            wire[NativePacketNpcIdOffset + 1] =
                unchecked((byte)(NpcId >> 8));
            wire[NativePacketTextLengthOffset] =
                unchecked((byte)textBytes.Length);
            Buffer.BlockCopy(
                textBytes,
                0,
                wire,
                NativePacketTextOffset,
                textBytes.Length);
            return true;
        }
    }

    public static class KingdomQuestKQHBatTextNativePlanBuilder
    {
        public const int BroadcastOccurrenceCount = 50;
        public const int ChatWindowOccurrenceCount = 70;
        public const ushort RoumenusMobId = 92;
        public const ushort EldGuardMobId = 103;
        public const string LooterHandleIdentifier = "LooterHandle";
        public const string DynamicHammerSuffix =
            " has obtained Invincible Hammer.";

        public static bool TryBuildBroadcastAll(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatBroadcastAllNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.Broadcast)
                return false;

            if (source.CanonicalLine == 101 &&
                string.Equals(
                    source.CommandText,
                    "broadcast all @CharName(LooterHandle) % \" has obtained Invincible Hammer.\".",
                    StringComparison.Ordinal))
            {
                KingdomQuestPineTokenValue token;
                if (!variables.TryFind(LooterHandleIdentifier, out token) ||
                    token == null)
                    return false;

                int ignoredPrefixLength;
                int nativeNumber =
                    KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                        token.Text,
                        out ignoredPrefixLength);
                plan = new KingdomQuestKQHBatBroadcastAllNativePlan(
                    source,
                    token,
                    unchecked((ushort)nativeNumber),
                    DynamicHammerSuffix);
                return true;
            }

            if (source.CanonicalLine == 102 &&
                string.Equals(
                    source.CommandText,
                    "broadcast all \"Everyone else will be immobilized for 3 seconds.\".",
                    StringComparison.Ordinal))
            {
                plan = new KingdomQuestKQHBatBroadcastAllNativePlan(
                    source,
                    string.Empty,
                    string.Empty,
                    "Everyone else will be immobilized for 3 seconds.");
                return true;
            }

            string key = GetReturnKey(source.CanonicalLine);
            if (key == null)
                return false;

            string message;
            KingdomQuestKQHBatScriptTextSource textSource;
            if (!KingdomQuestKQHBatTextSourceCatalog.TryGetSource(
                    source.ScriptLanguage, out textSource) ||
                textSource == null ||
                !KingdomQuestKQHBatTextSourceCatalog.TryGetRecord(
                    source.ScriptLanguage, key, out message) ||
                string.IsNullOrEmpty(message) ||
                !string.Equals(
                    source.CommandText,
                    "broadcast all \"" + key + "\".",
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestKQHBatBroadcastAllNativePlan(
                source,
                textSource.ScriptFileKey,
                key,
                message);
            return true;
        }

        public static bool TryBuildChatWindow(
            KingdomQuestKQHBatExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestKQHBatChatWindowNativePlan plan)
        {
            plan = null;
            if (source == null ||
                variables == null ||
                source.Kind != KingdomQuestKQHBatExternalKind.ChatWin)
                return false;

            string npcIndex;
            ushort npcId;
            string recordKey;
            string[] argumentIdentifiers;
            if (!TryGetChatSite(
                    source.CanonicalLine,
                    out npcIndex,
                    out npcId,
                    out recordKey,
                    out argumentIdentifiers))
                return false;

            KingdomQuestKQHBatScriptTextSource textSource;
            string formatText;
            if (!KingdomQuestKQHBatTextSourceCatalog.TryGetSource(
                    source.ScriptLanguage, out textSource) ||
                textSource == null ||
                !KingdomQuestKQHBatTextSourceCatalog.TryGetRecord(
                    source.ScriptLanguage, recordKey, out formatText) ||
                formatText == null)
                return false;

            var arguments = new List<string>(argumentIdentifiers.Length);
            for (int i = 0; i < argumentIdentifiers.Length; i++)
            {
                KingdomQuestPineTokenValue token;
                if (!variables.TryFind(argumentIdentifiers[i], out token) ||
                    token == null)
                    return false;
                arguments.Add(token.Text ?? string.Empty);
            }

            string formattedText;
            if (!TryFormatPercentS(
                    formatText, arguments, out formattedText) ||
                Encoding.ASCII.GetByteCount(formattedText) >=
                    KingdomQuestKQHBatChatWindowNativePlan
                        .NativePacketTextCapacity)
                return false;

            plan = new KingdomQuestKQHBatChatWindowNativePlan(
                npcId,
                npcIndex,
                textSource.ScriptFileKey,
                recordKey,
                formatText,
                formattedText,
                arguments);
            return true;
        }

        private static string GetReturnKey(int canonicalLine)
        {
            switch (canonicalLine)
            {
                case 83:
                case 140:
                    return "KQReturn30";
                case 85:
                case 142:
                    return "KQReturn20";
                case 87:
                case 144:
                    return "KQReturn10";
                case 89:
                case 146:
                    return "KQReturn5";
                default:
                    return null;
            }
        }

        private static bool TryGetChatSite(
            int canonicalLine,
            out string npcIndex,
            out ushort npcId,
            out string recordKey,
            out string[] argumentIdentifiers)
        {
            npcIndex = null;
            npcId = 0;
            recordKey = null;
            argumentIdentifiers = new string[0];

            switch (canonicalLine)
            {
                case 22:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "Intro0";
                    break;
                case 24:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "Intro1";
                    break;
                case 26:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "Intro2";
                    break;
                case 28:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "Intro3";
                    break;
                case 30:
                    npcIndex = "EldSpeGuard01";
                    npcId = EldGuardMobId;
                    recordKey = "Intro4";
                    break;
                case 32:
                    npcIndex = "EldSpeGuard01";
                    npcId = EldGuardMobId;
                    recordKey = "Intro5";
                    break;
                case 34:
                    npcIndex = "EldSpeGuard01";
                    npcId = EldGuardMobId;
                    recordKey = "Intro6";
                    break;
                case 36:
                    npcIndex = "EldSpeGuard01";
                    npcId = EldGuardMobId;
                    recordKey = "DualStart";
                    break;
                case 51:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "DualStop";
                    break;
                case 78:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "DualResult";
                    argumentIdentifiers =
                        new[] { "Winner0", "Winner1", "Winner2" };
                    break;
                case 79:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "DualResult1";
                    break;
                case 80:
                    npcIndex = "EldSpeGuard01";
                    npcId = EldGuardMobId;
                    recordKey = "DualResult2";
                    break;
                case 81:
                    npcIndex = "EldSpeGuard01";
                    npcId = EldGuardMobId;
                    recordKey = "DualResult3";
                    break;
                case 82:
                    npcIndex = "RouTownChiefRoumenus";
                    npcId = RoumenusMobId;
                    recordKey = "DualResult4";
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static bool TryFormatPercentS(
            string format,
            IList<string> arguments,
            out string formatted)
        {
            formatted = null;
            if (format == null || arguments == null ||
                arguments.Count > KingdomQuestKQHBatChatWindowNativePlan
                    .NativeArgumentSlotCount)
                return false;

            var builder = new StringBuilder();
            int argumentIndex = 0;
            for (int offset = 0; offset < format.Length;)
            {
                int marker = format.IndexOf("%s", offset, StringComparison.Ordinal);
                int percent = format.IndexOf('%', offset);
                if (percent >= 0 && (marker < 0 || percent < marker))
                    return false;

                if (marker < 0)
                {
                    builder.Append(format, offset, format.Length - offset);
                    offset = format.Length;
                    break;
                }

                if (argumentIndex >= arguments.Count)
                    return false;

                builder.Append(format, offset, marker - offset);
                builder.Append(arguments[argumentIndex++] ?? string.Empty);
                offset = marker + 2;
            }

            if (argumentIndex != arguments.Count)
                return false;

            formatted = builder.ToString();
            return true;
        }
    }
}
