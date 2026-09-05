using System.Collections.Generic;
using System;
namespace NextGen.Database.Storage
{
   public class Character
    {

       public int ID { get; set; }
       public int AccountID { get; set; }
       public string Name { get; set; }
       public byte Slot { get; set; }
       public byte CharLevel { get; set; }
       public byte Job { get; set; }
       public int HP { get; set; }
       public int SP { get; set; }
       public int instanzeID { get; set; }
       public short HPStones { get; set; }
       public short SPStones { get; set; }
       public long Exp { get; set; }
       public int Fame { get; set; }
       // Kill Points aus PvP-Kills - anders als Fame (nie aus der DB
       // geladen/gespeichert) wird das hier wirklich persistiert, siehe
       // ReadMethods.cs und DatabaseHelper.cs. DOCUMENTATION.md Abschnitt 26.
       public int KillPoints { get; set; }
       // Titel-Fortschritt fuer TOTAL_KILL_MOB (CharacterTitleData.shn Typ
       // 11) - einzige aktuell angebundene Titel-Kategorie, siehe
       // DOCUMENTATION.md Abschnitt 41. MobKillTitleTier: 0-4, wie viele der
       // 4 Stufen bereits mit Fame belohnt wurden (verhindert Doppel-Fame).
       public uint TotalMobKills { get; set; }
       public byte MobKillTitleTier { get; set; }
       // Weitere Titel-Kategorien (siehe DOCUMENTATION.md Abschnitt 42):
       // KILL_GUILD(12), BUY_NPC_COUNT(24), SELL_NPC_COUNT(23).
       public byte PvPKillTitleTier { get; set; }
       public uint NpcBuyCount { get; set; }
       public byte NpcBuyTitleTier { get; set; }
       public uint NpcSellCount { get; set; }
       public byte NpcSellTitleTier { get; set; }
       // FRIEND_COUNT (34) - wird im World-Server gezaehlt (AddFriend lebt
       // dort), die eigentliche Titel-/Fame-Vergabe erfolgt aber erst beim
       // naechsten Zone-Login (dort existiert TitleCategoriesByType).
       // FAME_COUNT (44) - selbstreferenziell: Anzahl bisher vergebener
       // Titelstufen (ueber alle Kategorien) gibt selbst wieder Fame.
       // Siehe DOCUMENTATION.md Abschnitt 50.
       public uint FriendCount { get; set; }
       public byte FriendCountTitleTier { get; set; }
       public uint TotalTitlesEarned { get; set; }
       public byte FameCountTitleTier { get; set; }
       public long Money { get; set; }
       public LookInfo LookInfo = new LookInfo();
       public byte StatPoints { get; set; }
       public byte UsablePoints { get; set; }
       public byte[] QuickBar { get; set; }
       public byte[] Shortcuts { get; set; }
       public byte[] QuickBarState { get; set; }
       public PositionInfo PositionInfo = new PositionInfo();
       public byte[] GameSettings { get; set; }
       public byte[] ClientSettings { get; set; }
       public int MountID { get; set; }
       public int MountFood { get; set; }
       public CharacterStats CharacterStats = new CharacterStats();
       public int GuildID { get; set; }
       public int AcademyID { get; set; }
       public DateTime MasterJoin { get; set; }
       public List<DatabaseSkill> SkillList = new List<DatabaseSkill>();
       public long GroupId { get; set; }
       public bool IsGroupMaster { get; set; }
       public long ReviveCoper { get; set; }
    }
}
