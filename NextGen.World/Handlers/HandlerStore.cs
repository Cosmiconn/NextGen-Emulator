using System;
using System.Collections.Generic;
using System.Reflection;
using NextGen.FiestaLib.Networking;
using NextGen.Util;

namespace NextGen.World.Handlers
{
    [ServerModule(Util.InitializationStage.Metadata)]
    public class HandlerStore
    {
        // Pro (Header, Type) koennen mehrere Handler mit unterschiedlichen,
        // sich nicht ueberschneidenden Versionsbereichen registriert sein -
        // z.B. ein generischer Fallback (MinVersion=0, MaxVersion=max) plus
        // ein spezifischerer Handler fuer eine einzelne Client-Version.
        private struct VersionedHandler
        {
            public ushort MinVersion;
            public ushort MaxVersion;
            public MethodInfo Method;
        }

        private static Dictionary<byte, Dictionary<byte, List<VersionedHandler>>> handlers;

        [InitializerMethod]
        public static bool Load()
        {
            handlers = new Dictionary<byte, Dictionary<byte, List<VersionedHandler>>>();
            foreach (var info in Reflector.FindMethodsByAttribute<PacketHandlerAttribute>())
            {
                PacketHandlerAttribute attribute = info.First;
                MethodInfo method = info.Second;
                if (!handlers.ContainsKey(attribute.Header))
                    handlers.Add(attribute.Header, new Dictionary<byte, List<VersionedHandler>>());
                if (!handlers[attribute.Header].ContainsKey(attribute.Type))
                    handlers[attribute.Header].Add(attribute.Type, new List<VersionedHandler>());

                var list = handlers[attribute.Header][attribute.Type];
                bool overlaps = list.Exists(h => attribute.MinVersion <= h.MaxVersion && attribute.MaxVersion >= h.MinVersion);
                if (overlaps)
                {
                    Log.WriteLine(LogLevel.Warn, "Duplicate/ueberlappender Handler gefunden: {0}:{1} (Version {2}-{3})", attribute.Header, attribute.Type, attribute.MinVersion, attribute.MaxVersion);
                }
                list.Add(new VersionedHandler { MinVersion = attribute.MinVersion, MaxVersion = attribute.MaxVersion, Method = method });
            }

            int count = 0;
            foreach (var dict in handlers.Values)
                foreach (var list in dict.Values)
                    count += list.Count;
            Log.WriteLine(LogLevel.Info, "{0} Handlers loaded.", count);
            return true;
        }

        // clientVersion=0 (nicht gemeldet/nicht relevant) matcht immer den
        // Default-Bereich (0..ushort.MaxValue) - bestehendes Verhalten fuer
        // alle unversionierten Handler bleibt dadurch unveraendert.
        public static MethodInfo GetHandler(byte header, byte type, ushort clientVersion = 0)
        {
            Dictionary<byte, List<VersionedHandler>> dict;
            List<VersionedHandler> list;
            if (!handlers.TryGetValue(header, out dict) || !dict.TryGetValue(type, out list))
                return null;

            // Spezifischste (schmalste) passende Version bevorzugen, damit ein
            // versionsspezifischer Handler einen generischen Fallback
            // ueberstimmt, statt von der Registrierungsreihenfolge abzuhaengen.
            VersionedHandler? best = null;
            uint bestRange = uint.MaxValue;
            foreach (var h in list)
            {
                if (clientVersion < h.MinVersion || clientVersion > h.MaxVersion) continue;
                uint range = (uint)h.MaxVersion - h.MinVersion;
                if (range < bestRange)
                {
                    best = h;
                    bestRange = range;
                }
            }
            return best?.Method;
        }

        public static Action GetCallback(MethodInfo method, params object[] parameters)
        {
            return () => method.Invoke(null, parameters);
        }
    }
}
