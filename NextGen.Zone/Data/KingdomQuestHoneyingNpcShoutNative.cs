using System;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// ShineNPCShout::sa_Step (0x004EECD0): resolve the source object,
    /// evaluate the record key, require the current script, call ss_String,
    /// then dispatch object vtable +0x534 with (0xFFFF, "", text, strlen, 0).
    /// Native object resolution and sending remain the owner's responsibility.
    /// </summary>
    public sealed class KingdomQuestHoneyingNpcShoutNativePlan
    {
        public const uint NativeStepAddress = 0x004EECD0;
        public const uint NativeObjectResolutionAddress = 0x004EBBB0;
        public const uint NativeScriptStringLookupAddress = 0x0048CE60;
        public const int NativeShoutVtableOffset = 0x534;
        public const ushort NativeReceiverHandle = 0xFFFF;
        public const string NativeSenderName = "";
        public const int NativeTrailingArgument = 0;

        private readonly byte[] sourceObjectToken;
        public int CanonicalLine { get; private set; }
        public string RecordKey { get; private set; }
        public string MessageText { get; private set; }
        public bool RecordPresent { get; private set; }

        private KingdomQuestHoneyingNpcShoutNativePlan(
            int canonicalLine, byte[] token, string recordKey,
            string messageText, bool recordPresent)
        {
            CanonicalLine = canonicalLine;
            sourceObjectToken = (byte[])token.Clone();
            RecordKey = recordKey;
            MessageText = messageText;
            RecordPresent = recordPresent;
        }

        public byte[] SnapshotSourceObjectToken()
        {
            return (byte[])sourceObjectToken.Clone();
        }

        public static bool TryBuild(
            KingdomQuestHoneyingExternalPlan source,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestHoneyingNpcShoutNativePlan plan)
        {
            plan = null;
            if (source == null || variables == null ||
                source.Kind != KingdomQuestHoneyingExternalKind.NpcShout)
                return false;

            string key;
            string block;
            switch (source.CanonicalLine)
            {
                case 160: block = "Summon1"; key = "Summon01"; break;
                case 164: block = "Summon2"; key = "Summon01"; break;
                case 168: block = "Summon3"; key = "Summon01"; break;
                case 172: block = "Summon4"; key = "Summon01"; break;
                case 177: block = "Dead"; key = "KQ_H_GHoneyingDead"; break;
                default: return false;
            }

            KingdomQuestPineTokenValue token;
            KingdomQuestPineScriptFileSource script;
            string text;
            bool present;
            if (!string.Equals(source.TopLevelBlock, block, StringComparison.Ordinal) ||
                !string.Equals(source.CommandText,
                    "npcshout Boss \"" + key + "\".", StringComparison.Ordinal) ||
                !variables.TryFind("Boss", out token) || token == null ||
                !KingdomQuestPineScriptFile.TryGetSource(
                    KingdomQuestHoneyingCommonNative.ScriptFileKey, out script) ||
                !KingdomQuestHoneyingTextSource.TryResolve(
                    script, key, out text, out present))
                return false;

            plan = new KingdomQuestHoneyingNpcShoutNativePlan(
                source.CanonicalLine, token.SnapshotNativeBytes(), key, text, present);
            return true;
        }
    }
}
