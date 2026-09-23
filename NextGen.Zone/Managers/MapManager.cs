using System.Collections.Generic;
using NextGen.FiestaLib.Data;
using NextGen.Util;
using NextGen.Zone.Data;
using NextGen.Zone.Game;

namespace NextGen.Zone
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class MapManager
    {
        public static MapManager Instance { get; private set; }
        public Dictionary<MapInfo, List<Map>> Maps { get; private set; }

        public MapManager()
        {
            Maps = new Dictionary<MapInfo, List<Map>>();
        }

        public Map GetMap(MapInfo info, short instance = (short)0)
        {
            if (info == null) return null;
            if (instance < 0) instance = 0;

            if (!Maps.ContainsKey(info))
                Maps.Add(info, new List<Map>());

            BlockInfo block;
            DataProvider.Instance.Blocks.TryGetValue(info.ID, out block);
            List<Map> maps = Maps[info];

            while (maps.Count <= instance)
                maps.Add(new Map(info, block, (short)maps.Count));

            return maps[instance];
        }

        [InitializerMethod]
        public static bool Load()
        {
            Instance = new MapManager();
            return true;
        }
    }
}
