using System.Data;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Ziel-Typ fuer Gilden-Turnier-Skills (GuildTournamentSkill.shn) - per
    /// Fiesta-Heroes-Community-Dokumentation (GuildTournament.md) bestaetigt,
    /// nicht geraten. Siehe DOCUMENTATION.md Abschnitt 52.
    /// </summary>
    public enum GuildTournamentTargetType : uint
    {
        Enemy = 0,
        Me = 1,
        Party = 2,
        Friend = 3,
        Spot = 4,
        All = 5,
        Group = 6,
        EnemyUser = 7,
        Every = 8,
        EnemyGuild = 9,
        MyGuild = 10,
        MyNpc = 11,
        MyRaid = 12,
        Box = 13,
        ThisAction = 14,
        AttackMe = 15,
        DamageByMe = 16,
        ThisSkill = 17,
        None = 18,
    }

    /// <summary>
    /// Ein Gilden-Turnier-Skill (GuildTournamentSkill.shn, 6 Zeilen in den
    /// echten Client-Daten). Wirkt ueber das bestehende AbState/SubAbState-
    /// Buff-System (StaName referenziert einen normalen AbState-Namen, z.B.
    /// "StaGldRestore"). Ausgeloest wenn eine bestimmte Anzahl Gildenmitglieder
    /// im Turnier gestorben ist (DeathPoint). Nur die Skill-Definitionen
    /// selbst liegen als echte Client-Daten vor - die eigentliche Turnier-
    /// Ablauflogik (Zeitplan, Punktevergabe, Belohnungen) braucht weitere
    /// Tabellen, die im eigenen Fileset nicht vorhanden sind (siehe
    /// DOCUMENTATION.md Abschnitt 52 fuer die vollstaendige, per
    /// Community-Doku bekannte, aber unbelegte Struktur).
    /// </summary>
    public sealed class GuildTournamentSkillInfo
    {
        public ushort MapType { get; private set; }
        public ushort Index { get; private set; }
        public ushort DeathPoint { get; private set; }
        public string StaName { get; private set; }
        public GuildTournamentTargetType TargetType { get; private set; }
        public uint ReuseDelayMs { get; private set; }

        public static GuildTournamentSkillInfo Load(DataRow row)
        {
            return new GuildTournamentSkillInfo
            {
                MapType = GetDataTypes.GetUshort(row["MAP_TYPE"]),
                Index = GetDataTypes.GetUshort(row["Index"]),
                DeathPoint = GetDataTypes.GetUshort(row["DeathPoint"]),
                StaName = (string)row["StaName"],
                TargetType = (GuildTournamentTargetType)GetDataTypes.GetUint(row["TargetType"]),
                ReuseDelayMs = GetDataTypes.GetUint(row["DlyTime"]),
            };
        }
    }
}
