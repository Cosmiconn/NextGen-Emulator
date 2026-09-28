using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestPineScriptFileSource
    {
        public string Key { get; private set; }
        public string RelativePath { get; private set; }
        public string Sha256 { get; private set; }

        internal KingdomQuestPineScriptFileSource(
            string key,
            string relativePath,
            string sha256)
        {
            Key = key;
            RelativePath = relativePath;
            Sha256 = sha256;
        }
    }

    public sealed class KingdomQuestPineScriptFilePlan
    {
        public bool ClearsCurrentScript { get; private set; }
        public KingdomQuestPineScriptFileSource Source { get; private set; }

        internal KingdomQuestPineScriptFilePlan(
            bool clearsCurrentScript,
            KingdomQuestPineScriptFileSource source)
        {
            ClearsCurrentScript = clearsCurrentScript;
            Source = source;
        }
    }

    /// <summary>
    /// Exact used-corpus projection of
    /// PineEventScriptNode::ShineScriptFile::sa_Step (Zone.exe 0x004EBDE0).
    ///
    /// Native evaluates one token. Empty string stores null in ProcessStack
    /// current-script field +0x10130. A non-empty token is passed to
    /// KQScriptManager::operator[] (0x0064CE70); its ShineScript* result is
    /// stored in that same field. The original manager has a separate 64-entry
    /// capacity and is unrelated to ScenarioBookShelf MAKE capacity.
    ///
    /// The supplied nine Pine KQs contain 19 scriptfile commands: five clears
    /// and fourteen selections over these nine exact DialogFile keys.
    /// </summary>
    public static class KingdomQuestPineScriptFile
    {
        public const int UsedCommandCount = 19;
        public const int UsedClearCount = 5;
        public const int UsedSourceCount = 9;
        public const int NativeKqScriptManagerCapacity = 64;
        public const uint NativeStepAddress = 0x004EBDE0u;
        public const uint NativeManagerLookupAddress = 0x0064CE70u;
        public const uint NativeProcessStackScriptOffset = 0x10130u;

        private static readonly Dictionary<string, KingdomQuestPineScriptFileSource>
            Sources =
            new Dictionary<string, KingdomQuestPineScriptFileSource>(
                StringComparer.Ordinal)
            {
                { "KQGordonMaster", Source(
                    "KQGordonMaster",
                    "Script/KQGordonMaster.txt",
                    "c6d0f02b822a2613932e943f93ec7b90453436c54b82b8a48be58bd759a72946") },
                { "KQHoneying", Source(
                    "KQHoneying",
                    "Script/KQHoneying.txt",
                    "3438e2d0144619b52fd4f66d7ea3f9b6384a0d67c76dbf712641b61d6bfdcc30") },
                { "KQHBat1", Source(
                    "KQHBat1",
                    "Script/KQHBat1.txt",
                    "ed41dc52b4b4042ae430f74e559ab547995e31667ff8b498825a3842a61ec156") },
                { "KQHBat2", Source(
                    "KQHBat2",
                    "Script/KQHBat2.txt",
                    "ed41dc52b4b4042ae430f74e559ab547995e31667ff8b498825a3842a61ec156") },
                { "KQHBat3", Source(
                    "KQHBat3",
                    "Script/KQHBat3.txt",
                    "8f1073942debae21c847609c042aa4337717e2112f38c298b061e67313bc1943") },
                { "KQHBat4", Source(
                    "KQHBat4",
                    "Script/KQHBat4.txt",
                    "ab821667e63fe85a9abd4b6ded493ad0eecbf3710183e3c9d36159854789c963") },
                { "KQHBat5", Source(
                    "KQHBat5",
                    "Script/KQHBat5.txt",
                    "0e2cddcaad8028693807a62c04e977e32a6e1ba5a24846c2c54b8713bdc61e36") },
                { "KQUnderHall", Source(
                    "KQUnderHall",
                    "Script/KQUnderHall.txt",
                    "9b6dff7ca269bf43437fcb2aa0e34eb46610d35a75c6de6b1478a56c3cfed4c0") },
                { "KQUnderHall2", Source(
                    "KQUnderHall2",
                    "Script/KQUnderHall2.txt",
                    "b03c9f342468385992790621a1fa0f78e4e237790f23fd9e0dbfee33ed067b16") },
            };

        public static bool TryParseUsed(
            string commandText,
            out KingdomQuestPineScriptFilePlan plan)
        {
            plan = null;
            string key;
            if (!TryQuotedArgument(commandText, out key))
                return false;

            if (key.Length == 0)
            {
                plan = new KingdomQuestPineScriptFilePlan(true, null);
                return true;
            }

            KingdomQuestPineScriptFileSource source;
            if (!Sources.TryGetValue(key, out source) || source == null)
                return false;

            plan = new KingdomQuestPineScriptFilePlan(false, source);
            return true;
        }

        public static bool TryGetSource(
            string key,
            out KingdomQuestPineScriptFileSource source)
        {
            source = null;
            return key != null && Sources.TryGetValue(key, out source);
        }

        public static IReadOnlyDictionary<string, KingdomQuestPineScriptFileSource>
            Snapshot()
        {
            return new Dictionary<string, KingdomQuestPineScriptFileSource>(
                Sources, StringComparer.Ordinal);
        }

        private static bool TryQuotedArgument(
            string commandText,
            out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            string text = commandText.Trim();
            if (text.EndsWith(".", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1).TrimEnd();

            const string prefix = "scriptfile ";
            if (!text.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            string operand = text.Substring(prefix.Length).Trim();
            if (operand.Length < 2 ||
                operand[0] != '"' ||
                operand[operand.Length - 1] != '"')
                return false;

            string inner = operand.Substring(1, operand.Length - 2);
            if (inner.IndexOf('"') >= 0)
                return false;

            value = inner;
            return true;
        }

        private static KingdomQuestPineScriptFileSource Source(
            string key,
            string path,
            string sha256)
        {
            return new KingdomQuestPineScriptFileSource(
                key, path, sha256);
        }
    }
}
