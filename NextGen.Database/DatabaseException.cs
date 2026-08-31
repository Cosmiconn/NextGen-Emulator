using System;

namespace NextGen.Database
{
        [Serializable()]
        public class DatabaseException : Exception
        {
            internal DatabaseException(string sMessage) : base(sMessage) { }
        }
}