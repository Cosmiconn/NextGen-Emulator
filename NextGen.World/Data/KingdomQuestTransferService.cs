using NextGen.World.InterServer;

namespace NextGen.World.Data
{
    /// <summary>
    /// Bridges an already-proven KQ session target into the existing Zone
    /// ChangeMap path. This service does not choose KQ maps, coordinates or
    /// internal instance numbers.
    /// </summary>
    public static class KingdomQuestTransferService
    {
        public static bool TryRequest(WorldCharacter character, uint instanceId, int x, int y)
        {
            if (character == null || character.Character == null)
                return false;

            KingdomQuestSessionTarget target;
            if (!KingdomQuestSessionTargetRegistry.TryGet(instanceId, out target))
                return false;

            ZoneConnection currentZone =
                Program.GetZoneByMap(character.Character.PositionInfo.Map);
            if (currentZone == null)
                return false;

            currentZone.SendKingdomQuestTransferRequest(
                character.Character.Name,
                target.MapID,
                target.MapInstance,
                x,
                y);
            return true;
        }
    }
}
