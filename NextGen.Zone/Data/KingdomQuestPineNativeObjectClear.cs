using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Exact ShineObject::so_ObjectType values recovered from the original
    /// Zone.exe vtables. These are native type numbers, not emulator MapObject
    /// categories or IDs.
    /// </summary>
    public enum KingdomQuestPineNativeObjectType : byte
    {
        AxialFlag = 0,
        DropItem = 1,
        Player = 2,
        MiniHouse = 3,
        Npc = 4,
        Mob = 5,
        MagicField = 6,
        Door = 7,
        Bandit = 8,
        EffectObject = 9,
        Servant = 10,
        Mover = 11,
        Pet = 12,
    }

    /// <summary>
    /// Native FieldMap::fm_ClearObject mask semantics.
    ///
    /// AxialListObjectClear::ali_Work calls so_ObjectType through vtable
    /// +0x4D0, computes (1 &lt;&lt; objectType), tests that bit against the
    /// supplied mask, and invokes so_RetrateFromMap through vtable +0x3F4 only
    /// for selected types. Native mobile/door implementations set retreat
    /// state; this class deliberately does not translate that into an emulator
    /// collection delete.
    /// </summary>
    public static class KingdomQuestPineNativeObjectClear
    {
        public const uint FieldMapClearObjectAddress = 0x00495A10u;
        public const uint AxialListObjectClearWorkAddress = 0x00495940u;
        public const int ObjectTypeVtableOffset = 0x4D0;
        public const int RetrateFromMapVtableOffset = 0x3F4;

        public const uint EndOfKqMask = 0x000000B0u;
        public const uint QuestResultMask = 0x000001B0u;

        private static readonly KingdomQuestPineNativeObjectType[]
            EndOfKqTypes =
        {
            KingdomQuestPineNativeObjectType.Npc,
            KingdomQuestPineNativeObjectType.Mob,
            KingdomQuestPineNativeObjectType.Door,
        };

        private static readonly KingdomQuestPineNativeObjectType[]
            QuestResultTypes =
        {
            KingdomQuestPineNativeObjectType.Npc,
            KingdomQuestPineNativeObjectType.Mob,
            KingdomQuestPineNativeObjectType.Door,
            KingdomQuestPineNativeObjectType.Bandit,
        };

        public static bool Includes(
            uint mask,
            KingdomQuestPineNativeObjectType objectType)
        {
            byte value = (byte)objectType;
            if (value >= 32)
                return false;

            return (mask & (1u << value)) != 0;
        }

        public static IReadOnlyList<KingdomQuestPineNativeObjectType>
            GetEndOfKqTypes()
        {
            return Array.AsReadOnly(EndOfKqTypes);
        }

        public static IReadOnlyList<KingdomQuestPineNativeObjectType>
            GetQuestResultTypes()
        {
            return Array.AsReadOnly(QuestResultTypes);
        }

        public static bool ValidateNativeMasks()
        {
            return BuildMask(EndOfKqTypes) == EndOfKqMask &&
                BuildMask(QuestResultTypes) == QuestResultMask;
        }

        private static uint BuildMask(
            IReadOnlyList<KingdomQuestPineNativeObjectType> types)
        {
            uint result = 0;
            for (int i = 0; i < types.Count; i++)
                result |= 1u << (byte)types[i];
            return result;
        }
    }
}
