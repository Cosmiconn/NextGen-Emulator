using System;
using System.Collections.Generic;
using System.Text;

namespace NextGen.World.Data
{
    /// <summary>
    /// Native KQ map context carried by PROTO_NC_CHAR_KQMAP_CMD.
    /// SHINE_DATETIME is intentionally preserved as its raw packed u32 because
    /// the year epoch/base is not yet source-proven.
    /// </summary>
    public sealed class KingdomQuestMapContext
    {
        public uint Handle { get; private set; }
        public string MapName { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public uint NativeDate { get; private set; }

        internal KingdomQuestMapContext(uint handle, string mapName,
            int x, int y, uint nativeDate)
        {
            Handle = handle;
            MapName = mapName;
            X = x;
            Y = y;
            NativeDate = nativeDate;
        }

        internal KingdomQuestMapContext Clone()
        {
            return new KingdomQuestMapContext(
                Handle, MapName, X, Y, NativeDate);
        }
    }

    /// <summary>
    /// Explicit source/session-owned KQ map coordinates for a live Handle.
    /// MapName is derived from the already source-backed MapID and is never
    /// supplied independently by callers.
    /// </summary>
    public static class KingdomQuestMapContextRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<uint, KingdomQuestMapContext> ByHandle =
            new Dictionary<uint, KingdomQuestMapContext>();

        public static bool TrySet(uint handle, int x, int y, uint nativeDate)
        {
            KingdomQuestSessionTarget target;
            if (!KingdomQuestSessionTargetRegistry.TryGet(handle, out target) ||
                DataProvider.Instance == null ||
                DataProvider.Instance.KingdomQuestMaps == null)
                return false;

            NextGen.FiestaLib.Data.MapInfo map;
            if (!DataProvider.Instance.KingdomQuestMaps.TryGetValue(
                    target.MapID, out map) ||
                string.IsNullOrEmpty(map.ShortName) ||
                Encoding.ASCII.GetByteCount(map.ShortName) > 12)
                return false;

            var context = new KingdomQuestMapContext(
                handle, map.ShortName, x, y, nativeDate);
            lock (Sync)
                ByHandle[handle] = context;
            return true;
        }

        public static bool TryGet(uint handle, out KingdomQuestMapContext context)
        {
            lock (Sync)
            {
                KingdomQuestMapContext current;
                if (!ByHandle.TryGetValue(handle, out current))
                {
                    context = null;
                    return false;
                }

                context = current.Clone();
                return true;
            }
        }

        public static bool Remove(uint handle)
        {
            lock (Sync)
                return ByHandle.Remove(handle);
        }

        public static void Clear()
        {
            lock (Sync)
                ByHandle.Clear();
        }
    }
}
