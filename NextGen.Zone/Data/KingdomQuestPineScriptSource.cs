using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineNodeKind : byte
    {
        Command = 1,
        If = 2,
        Infinite = 3,
        Scope = 4,
    }

    public sealed class KingdomQuestPineNodeSource
    {
        public KingdomQuestPineNodeKind Kind { get; private set; }
        public int CanonicalLine { get; private set; }
        public string Text { get; private set; }
        public IReadOnlyList<KingdomQuestPineNodeSource> Children { get; private set; }
        public IReadOnlyList<KingdomQuestPineNodeSource> ElseChildren { get; private set; }

        internal KingdomQuestPineNodeSource(
            KingdomQuestPineNodeKind kind,
            int canonicalLine,
            string text,
            List<KingdomQuestPineNodeSource> children,
            List<KingdomQuestPineNodeSource> elseChildren)
        {
            Kind = kind;
            CanonicalLine = canonicalLine;
            Text = text ?? string.Empty;
            Children = (children ?? new List<KingdomQuestPineNodeSource>()).AsReadOnly();
            ElseChildren = (elseChildren ?? new List<KingdomQuestPineNodeSource>()).AsReadOnly();
        }
    }

    public sealed class KingdomQuestPineBlockSource
    {
        public string Name { get; private set; }
        public IReadOnlyList<KingdomQuestPineNodeSource> Statements { get; private set; }

        internal KingdomQuestPineBlockSource(
            string name,
            List<KingdomQuestPineNodeSource> statements)
        {
            Name = name ?? string.Empty;
            Statements = statements.AsReadOnly();
        }
    }

    public sealed class KingdomQuestPineScriptDocument
    {
        public string ScriptLanguage { get; private set; }
        public string CanonicalSha256 { get; private set; }
        public IReadOnlyDictionary<string, KingdomQuestPineBlockSource> Blocks { get; private set; }

        internal KingdomQuestPineScriptDocument(
            string scriptLanguage,
            string canonicalSha256,
            Dictionary<string, KingdomQuestPineBlockSource> blocks)
        {
            ScriptLanguage = scriptLanguage;
            CanonicalSha256 = canonicalSha256;
            Blocks = new Dictionary<string, KingdomQuestPineBlockSource>(
                blocks, StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Exact executable-source projection for the nine Pine ScenarioBooks used
    /// by the supplied KingdomQuest.shn corpus.
    ///
    /// The bundled form is generated from the original Server.zip .ps bytes by
    /// removing only comments/blank lines and normalizing executable lines to
    /// ASCII LF text. Command text, expression text and ordering are preserved.
    /// It is a parser/source boundary only: no Pine command is assigned gameplay
    /// meaning here.
    /// </summary>
    public static class KingdomQuestPineScriptSource
    {
        public const int UsedPineScriptCount = 9;
        public const string CanonicalBundleSha256 =
            "390c0e948eb035aee62078327cb63d81ddef54aa9e28c38d642d02c86a9f9e57";

        private sealed class PineMeta
        {
            public int Lines;
            public int Blocks;
            public int Commands;
            public int Ifs;
            public int Infinites;
            public int Scopes;
            public string Sha256;

            public PineMeta(
                int lines, int blocks, int commands, int ifs,
                int infinites, int scopes, string sha256)
            {
                Lines = lines;
                Blocks = blocks;
                Commands = commands;
                Ifs = ifs;
                Infinites = infinites;
                Scopes = scopes;
                Sha256 = sha256;
            }
        }

        private static readonly Dictionary<string, PineMeta> Expected =
            new Dictionary<string, PineMeta>(StringComparer.Ordinal)
        {
            { "KQ/GordonMaster", new PineMeta(355, 16, 277, 8, 5, 0, "44707dadd6281c5bd98ab43dc3f73d8cb1e4005e9650d0c35c2227222eb85d19") },
            { "KQ/Honeying", new PineMeta(208, 15, 163, 1, 4, 0, "b73986320a92f88f02c4efe07391a13bd6f4c6fba08273ee73ad1ed1a94fbbae") },
            { "KQ/KQHBat1", new PineMeta(151, 10, 112, 3, 2, 1, "28e47ac8886c882119e5440903b3b717e50debc50d8d188583b3f86f1ebdfacb") },
            { "KQ/KQHBat2", new PineMeta(151, 10, 112, 3, 2, 1, "8682d925484e2fb4673689c6e1503b11b33a77edbf87e5bcd3180ce106fd6225") },
            { "KQ/KQHBat3", new PineMeta(151, 10, 112, 3, 2, 1, "c9ec42eb1772691bf1e9191daab3fea0556c9c5e887b7348b854cea11da6d12b") },
            { "KQ/KQHBat4", new PineMeta(151, 10, 112, 3, 2, 1, "f8a8b43e783c7878e87e6a47cef1d0b748047d65d2f06a0a105a7be3ddd9b235") },
            { "KQ/KQHBat5", new PineMeta(151, 10, 112, 3, 2, 1, "988c99a4a47665137a08bbcaf2a72280829af8016413e26d005f1e234ef85ebc") },
            { "KQ/UnderHall", new PineMeta(405, 45, 255, 1, 19, 0, "887c0372185d90d46e598ff2697d344a4c04640229a84d3169702b2b713d77e7") },
            { "KQ/UnderHall2", new PineMeta(588, 57, 405, 1, 22, 0, "818c228f2f05d7c1fbba2a14aaa128df0c607eaa44b30ecfa0a2d53189674c7a") },
        };

        private const string CompressedCanonicalSource =
            "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";

        private static readonly Lazy<
            IReadOnlyDictionary<string, KingdomQuestPineScriptDocument>>
            Documents = new Lazy<
                IReadOnlyDictionary<string, KingdomQuestPineScriptDocument>>(
                    LoadDocuments, true);

        public static bool TryGet(
            string scriptLanguage,
            out KingdomQuestPineScriptDocument document)
        {
            document = null;
            return !string.IsNullOrEmpty(scriptLanguage) &&
                Documents.Value.TryGetValue(scriptLanguage, out document);
        }

        public static IReadOnlyDictionary<string, KingdomQuestPineScriptDocument>
            Snapshot()
        {
            return Documents.Value;
        }

        private static IReadOnlyDictionary<string, KingdomQuestPineScriptDocument>
            LoadDocuments()
        {
            string bundle = DecodeBundle();
            if (!HashEquals(bundle, CanonicalBundleSha256))
                throw new InvalidDataException(
                    "KQ Pine canonical bundle hash mismatch.");

            var sources = SplitBundle(bundle);
            if (sources.Count != UsedPineScriptCount ||
                !new HashSet<string>(sources.Keys, StringComparer.Ordinal)
                    .SetEquals(Expected.Keys))
                throw new InvalidDataException(
                    "KQ Pine canonical script key set changed.");

            var documents =
                new Dictionary<string, KingdomQuestPineScriptDocument>(
                    StringComparer.Ordinal);

            foreach (KeyValuePair<string, PineMeta> pair in Expected)
            {
                string source;
                if (!sources.TryGetValue(pair.Key, out source) ||
                    !HashEquals(source, pair.Value.Sha256))
                    throw new InvalidDataException(
                        "KQ Pine canonical source hash mismatch: " + pair.Key);

                KingdomQuestPineScriptDocument document =
                    Parse(pair.Key, source);

                int commands = 0;
                int ifs = 0;
                int infinites = 0;
                int scopes = 0;
                foreach (KingdomQuestPineBlockSource block
                    in document.Blocks.Values)
                    CountNodes(
                        block.Statements,
                        ref commands, ref ifs, ref infinites, ref scopes);

                int lines = source.Split(
                    new[] { '\n' },
                    StringSplitOptions.RemoveEmptyEntries).Length;

                if (lines != pair.Value.Lines ||
                    document.Blocks.Count != pair.Value.Blocks ||
                    commands != pair.Value.Commands ||
                    ifs != pair.Value.Ifs ||
                    infinites != pair.Value.Infinites ||
                    scopes != pair.Value.Scopes)
                    throw new InvalidDataException(
                        "KQ Pine parsed source shape changed: " + pair.Key);

                documents.Add(pair.Key, document);
            }

            return documents;
        }

        private static void CountNodes(
            IReadOnlyList<KingdomQuestPineNodeSource> nodes,
            ref int commands,
            ref int ifs,
            ref int infinites,
            ref int scopes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                KingdomQuestPineNodeSource node = nodes[i];
                switch (node.Kind)
                {
                    case KingdomQuestPineNodeKind.Command:
                        commands++;
                        break;
                    case KingdomQuestPineNodeKind.If:
                        ifs++;
                        break;
                    case KingdomQuestPineNodeKind.Infinite:
                        infinites++;
                        break;
                    case KingdomQuestPineNodeKind.Scope:
                        scopes++;
                        break;
                }

                CountNodes(
                    node.Children,
                    ref commands, ref ifs, ref infinites, ref scopes);
                CountNodes(
                    node.ElseChildren,
                    ref commands, ref ifs, ref infinites, ref scopes);
            }
        }

        private static KingdomQuestPineScriptDocument Parse(
            string key, string source)
        {
            string[] lines = source.Split(
                new[] { '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            int index = 0;
            var blocks =
                new Dictionary<string, KingdomQuestPineBlockSource>(
                    StringComparer.Ordinal);

            while (index < lines.Length)
            {
                string name;
                if (!TryNamedOpen(lines[index].Trim(), out name))
                    throw ParseError(key, index,
                        "expected top-level named open");

                index++;
                List<KingdomQuestPineNodeSource> statements =
                    ParseSequence(key, lines, ref index);

                if (blocks.ContainsKey(name))
                    throw ParseError(key, index,
                        "duplicate block " + name);

                blocks.Add(
                    name,
                    new KingdomQuestPineBlockSource(name, statements));
            }

            return new KingdomQuestPineScriptDocument(
                key,
                Expected[key].Sha256,
                blocks);
        }

        private static List<KingdomQuestPineNodeSource> ParseSequence(
            string key, string[] lines, ref int index)
        {
            var result = new List<KingdomQuestPineNodeSource>();
            while (index < lines.Length)
            {
                int lineNumber = index + 1;
                string line = lines[index].Trim();

                if (EqualsToken(line, "close"))
                {
                    index++;
                    return result;
                }

                if (line.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
                {
                    string expression = line.Substring(3).Trim();
                    bool inlineThen =
                        expression.EndsWith(
                            " then", StringComparison.OrdinalIgnoreCase);
                    if (inlineThen)
                        expression =
                            expression.Substring(
                                0, expression.Length - 5).TrimEnd();

                    index++;
                    string expectedOpen =
                        inlineThen ? "open" : "then open";
                    RequireToken(
                        key, lines, ref index, expectedOpen, lineNumber);

                    List<KingdomQuestPineNodeSource> children =
                        ParseSequence(key, lines, ref index);
                    var elseChildren =
                        new List<KingdomQuestPineNodeSource>();

                    if (index < lines.Length &&
                        EqualsToken(lines[index].Trim(), "else open"))
                    {
                        index++;
                        elseChildren =
                            ParseSequence(key, lines, ref index);
                    }
                    else if (index < lines.Length &&
                        EqualsToken(lines[index].Trim(), "else"))
                    {
                        index++;
                        RequireToken(
                            key, lines, ref index, "open", lineNumber);
                        elseChildren =
                            ParseSequence(key, lines, ref index);
                    }

                    result.Add(new KingdomQuestPineNodeSource(
                        KingdomQuestPineNodeKind.If,
                        lineNumber,
                        expression,
                        children,
                        elseChildren));
                    continue;
                }

                if (EqualsToken(line, "infinite"))
                {
                    index++;
                    RequireToken(
                        key, lines, ref index, "open", lineNumber);
                    List<KingdomQuestPineNodeSource> children =
                        ParseSequence(key, lines, ref index);
                    result.Add(new KingdomQuestPineNodeSource(
                        KingdomQuestPineNodeKind.Infinite,
                        lineNumber,
                        string.Empty,
                        children,
                        null));
                    continue;
                }

                string scopeName;
                if (TryNamedOpen(line, out scopeName))
                {
                    index++;
                    List<KingdomQuestPineNodeSource> children =
                        ParseSequence(key, lines, ref index);
                    result.Add(new KingdomQuestPineNodeSource(
                        KingdomQuestPineNodeKind.Scope,
                        lineNumber,
                        scopeName,
                        children,
                        null));
                    continue;
                }

                if (EqualsToken(line, "open") ||
                    EqualsToken(line, "then open") ||
                    EqualsToken(line, "else") ||
                    EqualsToken(line, "else open"))
                    throw ParseError(
                        key, index, "orphan structural token " + line);

                result.Add(new KingdomQuestPineNodeSource(
                    KingdomQuestPineNodeKind.Command,
                    lineNumber,
                    line,
                    null,
                    null));
                index++;
            }

            throw ParseError(
                key, lines.Length, "missing close before EOF");
        }

        private static void RequireToken(
            string key,
            string[] lines,
            ref int index,
            string token,
            int ownerLine)
        {
            if (index >= lines.Length ||
                !EqualsToken(lines[index].Trim(), token))
                throw new InvalidDataException(
                    "KQ Pine parse error " + key +
                    " after canonical line " + ownerLine +
                    ": expected " + token + ".");

            index++;
        }

        private static bool TryNamedOpen(
            string line, out string name)
        {
            name = null;
            if (!line.StartsWith(
                    "open [", StringComparison.OrdinalIgnoreCase) ||
                !line.EndsWith("]", StringComparison.Ordinal))
                return false;

            int start = line.IndexOf('[') + 1;
            int length = line.Length - start - 1;
            if (length <= 0)
                return false;

            name = line.Substring(start, length);
            return name.Length != 0;
        }

        private static bool EqualsToken(
            string value, string token)
        {
            return string.Equals(
                value, token, StringComparison.OrdinalIgnoreCase);
        }

        private static InvalidDataException ParseError(
            string key, int zeroBasedLine, string message)
        {
            return new InvalidDataException(
                "KQ Pine parse error " + key +
                " canonical line " + (zeroBasedLine + 1) +
                ": " + message + ".");
        }

        private static Dictionary<string, string> SplitBundle(
            string bundle)
        {
            var result =
                new Dictionary<string, string>(StringComparer.Ordinal);
            int marker = 0;
            while (marker < bundle.Length)
            {
                if (string.Compare(
                        bundle, marker, "@@ ", 0, 3,
                        StringComparison.Ordinal) != 0)
                    throw new InvalidDataException(
                        "Malformed KQ Pine canonical bundle.");

                int keyEnd = bundle.IndexOf('\n', marker);
                if (keyEnd < 0)
                    throw new InvalidDataException(
                        "Malformed KQ Pine bundle key.");

                string key = bundle.Substring(
                    marker + 3, keyEnd - marker - 3);
                int next = bundle.IndexOf(
                    "\n@@ ", keyEnd, StringComparison.Ordinal);
                int sourceEnd =
                    next < 0 ? bundle.Length : next + 1;
                string source = bundle.Substring(
                    keyEnd + 1, sourceEnd - keyEnd - 1);

                if (source.Length == 0 ||
                    result.ContainsKey(key))
                    throw new InvalidDataException(
                        "Malformed KQ Pine bundle source: " + key);

                result.Add(key, source);
                marker = next < 0 ? bundle.Length : next + 1;
            }
            return result;
        }

        private static string DecodeBundle()
        {
            byte[] compressed =
                Convert.FromBase64String(CompressedCanonicalSource);
            using (var input = new MemoryStream(compressed))
            using (var gzip =
                new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return Encoding.ASCII.GetString(output.ToArray());
            }
        }

        private static bool HashEquals(
            string value, string expected)
        {
            byte[] hash =
                SHA256.HashData(Encoding.ASCII.GetBytes(value));
            return string.Equals(
                Convert.ToHexString(hash).ToLowerInvariant(),
                expected,
                StringComparison.Ordinal);
        }
    }
}
