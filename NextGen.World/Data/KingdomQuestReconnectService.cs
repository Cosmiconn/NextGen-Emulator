using System;
using System.Collections.Generic;
using System.Text;
using NextGen.World.Networking;

namespace NextGen.World.Data
{
    /// <summary>
    /// Source-backed reconnect bridge for the original
    /// IsExisted -> JoinerInfoUpdateByLogin path.
    ///
    /// The original GetJoinerIndex compares the complete 20-byte Name5 field
    /// as five DWORDs. A reconnect therefore reuses an already-retained
    /// joiner; it never creates membership from the persisted Handle alone.
    /// </summary>
    public static class KingdomQuestReconnectService
    {
        public static bool TryRestore(
            WorldClient client,
            WorldCharacter character,
            DateTime now,
            out KingdomQuestSessionTarget target,
            out int x,
            out int y)
        {
            target = null;
            x = 0;
            y = 0;

            if (client == null ||
                character == null ||
                character.Character == null)
                return false;

            var source = character.Character;
            if (!source.KingdomQuestHandle.HasValue ||
                string.IsNullOrEmpty(source.KingdomQuestMapName) ||
                !source.KingdomQuestX.HasValue ||
                !source.KingdomQuestY.HasValue ||
                !source.KingdomQuestDate.HasValue)
                return false;

            uint handle = unchecked((uint)source.KingdomQuestHandle.Value);
            if (!KingdomQuestReconnectRules.TryIsExistingFromDatabase(
                    handle,
                    source.KingdomQuestMapName,
                    source.KingdomQuestDate.Value,
                    now))
                return false;

            IReadOnlyList<KingdomQuestMembershipEntry> members;
            if (!KingdomQuestMembershipRegistry.TryGet(handle, out members))
                return false;

            bool existingJoiner = false;
            for (int i = 0; i < members.Count; i++)
            {
                KingdomQuestMembershipEntry member = members[i];
                if (member != null &&
                    Name5Equals(member.Name, source.Name))
                {
                    existingJoiner = true;
                    break;
                }
            }

            if (!existingJoiner ||
                !KingdomQuestSessionTargetRegistry.TryGet(handle, out target))
                return false;

            // JoinerInfoUpdateByLogin writes the already-existing KQ Handle
            // into the new World session. The native joiner row itself is not
            // inserted/replaced here.
            client.KingdomQuestHandle = handle;
            x = source.KingdomQuestX.Value;
            y = source.KingdomQuestY.Value;
            return true;
        }

        private static bool Name5Equals(string left, string right)
        {
            if (left == null || right == null)
                return false;

            byte[] leftBytes = Encoding.ASCII.GetBytes(left);
            byte[] rightBytes = Encoding.ASCII.GetBytes(right);
            if (leftBytes.Length > 20 || rightBytes.Length > 20)
                return false;

            for (int i = 0; i < 20; i++)
            {
                byte a = i < leftBytes.Length ? leftBytes[i] : (byte)0;
                byte b = i < rightBytes.Length ? rightBytes[i] : (byte)0;
                if (a != b)
                    return false;
            }

            return true;
        }
    }
}
