using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Source-faithful projection of the two original
    /// KQRewardDataBox::operator[] overloads.
    ///
    /// The ushort overload linearly compares the requested value against the
    /// DWORD KINGDOM_QUEST_REW.ID field. The char* overload linearly compares
    /// the 32-byte IndexString and succeeds only when a matching NUL is reached
    /// before byte 32. Neither overload treats a request as a source-row index.
    /// </summary>
    public static class KingdomQuestRewardSourceResolver
    {
        public const int NativeIndexStringBytes = 32;

        public static bool TryFindById(
            IReadOnlyList<KingdomQuestRewardSourceRow> rows,
            ushort rewardIndex,
            out KingdomQuestRewardSourceRow reward)
        {
            reward = null;
            if (rows == null)
                return false;

            uint nativeId = rewardIndex;
            for (int i = 0; i < rows.Count; i++)
            {
                KingdomQuestRewardSourceRow current = rows[i];
                if (current != null && current.ID == nativeId)
                {
                    reward = current;
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindByIndexString(
            IReadOnlyList<KingdomQuestRewardSourceRow> rows,
            string indexString,
            out KingdomQuestRewardSourceRow reward)
        {
            reward = null;
            if (rows == null || indexString == null ||
                !IsNativeIndexString(indexString))
                return false;

            for (int i = 0; i < rows.Count; i++)
            {
                KingdomQuestRewardSourceRow current = rows[i];
                if (current != null &&
                    IsNativeIndexString(current.IndexString) &&
                    string.Equals(
                        current.IndexString, indexString,
                        StringComparison.Ordinal))
                {
                    reward = current;
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindShineRewardByHandle(
            IReadOnlyList<ShineRewardSourceRow> rows,
            ushort rewardHandle,
            out ShineRewardSourceRow reward)
        {
            reward = null;
            if (rows == null)
                return false;

            // RewardData::rd_FindHandle resolves the native RewardHandle, not
            // the physical SHN row. Preserve first-match linear semantics so
            // duplicate handle 0 rows are not silently normalized away.
            for (int i = 0; i < rows.Count; i++)
            {
                ShineRewardSourceRow current = rows[i];
                if (current != null && current.RewardHandle == rewardHandle)
                {
                    reward = current;
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindDefault(
            IReadOnlyList<KingdomQuestRewardSourceRow> rows,
            KingdomQuestProtocolInfo definition,
            out KingdomQuestRewardSourceRow reward)
        {
            reward = null;
            return definition != null &&
                TryFindById(rows, definition.RewardIndex, out reward);
        }

        private static bool IsNativeIndexString(string value)
        {
            if (value == null || value.Length >= NativeIndexStringBytes)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                // Original data is narrow-char SHN/Pine/Lua text. Reject
                // characters that cannot be represented by the byte compare
                // instead of introducing a Unicode comparison policy.
                if (value[i] == '\0' || value[i] > 0x7f)
                    return false;
            }

            return true;
        }
    }
}
