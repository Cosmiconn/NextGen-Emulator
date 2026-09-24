using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Stores the raw ushort Error returned by native NC_KQ_Z2W_MAKE_ACK.
    /// WorldManager.exe proves 0x0981 as the success value; all other values
    /// are still retained unchanged for diagnostics/evidence.
    /// </summary>
    public static class KingdomQuestMakeAckRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, ushort> ErrorByHandle =
            new Dictionary<uint, ushort>();

        public static void Set(uint handle, ushort error)
        {
            lock (Sync)
                ErrorByHandle[handle] = error;
        }

        public static bool TryGet(uint handle, out ushort error)
        {
            lock (Sync)
                return ErrorByHandle.TryGetValue(handle, out error);
        }

        public static bool IsSuccess(ushort error)
        {
            return error == KingdomQuestNativeConstants.MakeAckSuccess;
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ErrorByHandle.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                ErrorByHandle.Clear();
        }
    }
}
