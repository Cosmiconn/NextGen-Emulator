using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGen.World.Data;
namespace NextGen.World.Events
{
    public class OnCharacterLoginArgs : EventArgs
    {
        public OnCharacterLoginArgs(WorldCharacter pChar,OnCharacterLoginArgs args)
        {
        }
    }
}
