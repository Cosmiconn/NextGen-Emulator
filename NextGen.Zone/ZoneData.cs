using System.Collections.Generic;

using NextGen.FiestaLib.Data;

namespace NextGen.Zone
{
    public sealed class ZoneData
    {
        public byte ID { get; set; }
        public List<MapInfo> MapsToLoad { get; set; }
        public ushort Port { get; set; } //we let this assign by worldserver
        public string IP { get; set; }
    }
}
