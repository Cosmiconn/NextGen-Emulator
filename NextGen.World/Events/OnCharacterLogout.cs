using System;
using NextGen.World.Data;


namespace NextGen.World.Events
{
    public class OnCharacterLogoutArgs : EventArgs
    {
        public WorldCharacter PCharacter { get; set; }

        public OnCharacterLogoutArgs(WorldCharacter pChar)
        {
            this.PCharacter = pChar;
        }
    }
}
