using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// slil_Load 0x0049AA10 and so_SubLayer_CanSee 0x0049AC70. Rows are
    /// the object SENDING its brief, columns the receiving object. This is
    /// independent of the 64-bit registration layer used by map traversal.
    /// </summary>
    public sealed class KingdomQuestPineNativeSubLayer
    {
        public const string OriginalSourceSha256 = "c0b77f1a70cb941fc2402cdbfbf8456fd71c120dfc74f9d90651a613547739da";
        private readonly byte[] sendMyBrief, canAttack;
        public KingdomQuestPineNativeSubLayer(byte[] sendMyBrief36, byte[] canAttack36)
        {
            if (sendMyBrief36 == null || sendMyBrief36.Length != 36 || canAttack36 == null || canAttack36.Length != 36)
                throw new ArgumentException("Two explicit native 6 by 6 byte tables are required.");
            sendMyBrief = (byte[])sendMyBrief36.Clone(); canAttack = (byte[])canAttack36.Clone();
        }
        public bool TrySendMyBrief(byte senderModeFlags, byte receiverModeFlags, out byte result)
            => TryRead(sendMyBrief, senderModeFlags, receiverModeFlags, out result);
        // 0x0049ACB0 tests target abstate 0xC7 BEFORE indexing the matrix.
        public bool TryCanAttack(byte sourceModeFlags, byte targetModeFlags, bool targetHasAbstateC7, out byte result)
        {
            result = 0;
            return targetHasAbstateC7 || TryRead(canAttack, sourceModeFlags, targetModeFlags, out result);
        }
        private static bool TryRead(byte[] table, byte row, byte column, out byte result)
        {
            result = 0;
            if (row >= 6 || column >= 6) return false; // native out-of-table memory is UNRESOLVED
            result = table[row * 6 + column]; return true;
        }

        // Projection of the supplied source layout, not a replacement general
        // OptionReader. Native loader uses the first six ROWS and compares only
        // the two checksum bytes, not their equality to a numeric row index.
        public static bool TryParse(TextReader reader, out KingdomQuestPineNativeSubLayer table)
        {
            table = null;
            if (reader == null) return false;
            var values = new Dictionary<string, List<byte>>(StringComparer.Ordinal);
            string current = null, line; bool schema = false;
            while ((line = reader.ReadLine()) != null)
            {
                int comment = line.IndexOf(';'); if (comment >= 0) line = line.Substring(0, comment);
                var fields = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length == 0) continue;
                if (fields[0] == "#Table")
                {
                    current = fields.Length == 2 ? fields[1] : null; schema = false;
                    if (current != "SendMyBrief" && current != "CanAttack") { current = null; continue; }
                    if (values.ContainsKey(current)) return false;
                    values.Add(current, new List<byte>()); continue;
                }
                if (fields[0] == "#End") { current = null; continue; }
                if (current == null) continue;
                if (fields[0] == "#ColumnType")
                {
                    if (fields.Length != 10 || fields[1] != "BYTE" || fields[2] != "Index") return false;
                    for (int i = 3; i < 10; i++) if (fields[i] != "BYTE") return false;
                    schema = true; continue;
                }
                if (fields[0] != "#Record") continue;
                if (!schema || fields.Length != 10 || !Byte(fields[1], out byte first) ||
                    !Byte(fields[9], out byte last) || first != last) return false;
                // Extra rows are outside the verified six-row source projection.
                if (values[current].Count >= 36) return false;
                for (int i = 3; i < 9; i++)
                { if (!Byte(fields[i], out byte cell)) return false; values[current].Add(cell); }
            }
            if (!values.TryGetValue("SendMyBrief", out var send) || send.Count != 36 ||
                !values.TryGetValue("CanAttack", out var attack) || attack.Count != 36) return false;
            table = new KingdomQuestPineNativeSubLayer(send.ToArray(), attack.ToArray()); return true;
        }
        private static bool Byte(string value, out byte result) =>
            byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
    }
}
