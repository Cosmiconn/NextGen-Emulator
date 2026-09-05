using System;
using NextGen.World.Data;


namespace NextGen.World.Events
{
    public class OnCharacterLevelUpArgs : EventArgs
    {
        public delegate void DelegatetType(WorldCharacter pChar);
        public WorldCharacter PCharacter { get; set; }

        public OnCharacterLevelUpArgs(WorldCharacter pChar = null)
        {
            this.PCharacter = pChar;
        }
        public OnCharacterLevelUpArgs()
        {
        }
    }
}
