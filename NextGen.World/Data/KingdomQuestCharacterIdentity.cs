using NextGen.World.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Source-correlated native character registration identity.
    ///
    /// Original evidence:
    /// - World PDB: PROTO_NC_CHAR_CHARDATA_REQ first identity field is
    ///   chrregnum.
    /// - Character.exe fc_NC_CHAR_CHARDATA_REQ logs that same request value
    ///   as nCharNo and uses the character DB nCharNo identity.
    /// - World00_Character p_Char_Create returns nCharNo = @@IDENTITY.
    /// - This emulator already serializes Character.ID in the first
    ///   PROTO_AVATARINFORMATION chrregnum position for CharacterList/Create.
    ///
    /// Therefore Character.ID is not inferred specifically for KQ; it is the
    /// emulator's existing native chrregnum representation.
    /// </summary>
    public static class KingdomQuestCharacterIdentity
    {
        public static bool TryGetCharacterNumber(
            WorldCharacter character, out uint characterNumber)
        {
            characterNumber = 0;
            if (character == null ||
                character.Character == null ||
                character.Character.ID < 0)
                return false;

            characterNumber = unchecked((uint)character.Character.ID);
            return true;
        }
    }
}
