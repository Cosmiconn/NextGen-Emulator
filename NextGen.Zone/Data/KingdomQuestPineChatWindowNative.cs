using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Script-text lookup plan for the native Pine chatwin command.
    ///
    /// Zone.exe recovery already fixes ShineChatWindow::sa_Step, NPC lookup,
    /// the seven argument slots, 0x100 text capacity, opcode 0x6C0C and
    /// AxialListPacketBroadcast/so_AllInMap delivery. Some KQ script text is
    /// intentionally not embedded in the emulator yet; this plan preserves the
    /// exact script-file key/hash plus record key until a source-backed text
    /// resolver provides it.
    /// </summary>
    public sealed class KingdomQuestPineChatWindowLookupPlan
    {
        public ushort NpcId { get; private set; }
        public string NpcIndex { get; private set; }
        public string ScriptFileKey { get; private set; }
        public string ScriptFilePath { get; private set; }
        public string ScriptFileSha256 { get; private set; }
        public string RecordKey { get; private set; }
        public bool MessageTextResolved { get; private set; }

        internal KingdomQuestPineChatWindowLookupPlan(
            ushort npcId,
            string npcIndex,
            KingdomQuestPineScriptFileSource scriptSource,
            string recordKey)
        {
            NpcId = npcId;
            NpcIndex = npcIndex ?? string.Empty;
            ScriptFileKey = scriptSource == null
                ? string.Empty
                : scriptSource.Key;
            ScriptFilePath = scriptSource == null
                ? string.Empty
                : scriptSource.RelativePath;
            ScriptFileSha256 = scriptSource == null
                ? string.Empty
                : scriptSource.Sha256;
            RecordKey = recordKey ?? string.Empty;
            MessageTextResolved = false;
        }
    }

    /// <summary>
    /// Common native metadata/source-identity boundary for Pine chatwin.
    /// It deliberately does not synthesize text when the original ShineScript
    /// record body has not been projected into this repository.
    /// </summary>
    public static class KingdomQuestPineChatWindowNative
    {
        public const uint NativeStepAddress =
            KingdomQuestKQHBatChatWindowNativePlan.NativeStepAddress;
        public const uint NativeNpcLookupAddress =
            KingdomQuestKQHBatChatWindowNativePlan.NativeNpcLookupAddress;
        public const uint NativeScriptStringLookupAddress =
            KingdomQuestKQHBatChatWindowNativePlan.NativeScriptStringLookupAddress;
        public const ushort NativePacketOpcode =
            KingdomQuestKQHBatChatWindowNativePlan.NativePacketOpcode;
        public const int NativeArgumentSlotCount =
            KingdomQuestKQHBatChatWindowNativePlan.NativeArgumentSlotCount;
        public const int NativePacketTextCapacity =
            KingdomQuestKQHBatChatWindowNativePlan.NativePacketTextCapacity;
        public const uint NativePacketBroadcastCtorAddress =
            KingdomQuestKQHBatChatWindowNativePlan.NativePacketBroadcastCtorAddress;
        public const uint NativePacketBroadcastWorkAddress =
            KingdomQuestKQHBatChatWindowNativePlan.NativePacketBroadcastWorkAddress;
        public const uint NativeAllInMapAddress =
            KingdomQuestKQHBatChatWindowNativePlan.NativeAllInMapAddress;

        public static bool TryBuildLookup(
            string npcIndex,
            ushort expectedNpcId,
            string scriptFileKey,
            string expectedScriptPath,
            string expectedScriptSha256,
            string recordKey,
            DataProvider data,
            out KingdomQuestPineChatWindowLookupPlan plan)
        {
            plan = null;
            if (string.IsNullOrEmpty(npcIndex) ||
                string.IsNullOrEmpty(scriptFileKey) ||
                string.IsNullOrEmpty(expectedScriptPath) ||
                string.IsNullOrEmpty(expectedScriptSha256) ||
                string.IsNullOrEmpty(recordKey) ||
                data == null ||
                data.MobsByName == null ||
                data.MobData == null)
                return false;

            MobInfo clientInfo;
            MobInfoServer serverInfo;
            KingdomQuestPineScriptFileSource scriptSource;
            if (!data.MobsByName.TryGetValue(npcIndex, out clientInfo) ||
                clientInfo == null ||
                !data.MobData.TryGetValue(npcIndex, out serverInfo) ||
                serverInfo == null ||
                clientInfo.ID != expectedNpcId ||
                serverInfo.ID != expectedNpcId ||
                !KingdomQuestPineScriptFile.TryGetSource(
                    scriptFileKey, out scriptSource) ||
                scriptSource == null ||
                !string.Equals(
                    scriptSource.RelativePath,
                    expectedScriptPath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    scriptSource.Sha256,
                    expectedScriptSha256,
                    StringComparison.Ordinal))
                return false;

            plan = new KingdomQuestPineChatWindowLookupPlan(
                expectedNpcId,
                npcIndex,
                scriptSource,
                recordKey);
            return true;
        }
    }
}
