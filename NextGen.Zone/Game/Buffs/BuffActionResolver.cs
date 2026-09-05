using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Game.Buffs
{
    /// <summary>
    /// Uebersetzt einen rohen SubAbStateAction (ActionIndex+ActionArg aus
    /// SubAbState.shn) in eine tatsaechliche Wirkung auf die Stat-Summen einer
    /// Buffs-Instanz.
    ///
    /// STAND: Urspruenglich rein empirisch aus Descript-Text-Korrelation
    /// abgeleitet (siehe DOCUMENTATION.md Abschnitt 19/20/22). Seit dieser
    /// Aenderung gegen eine von Fiesta Heroes bereitgestellte, autoritative
    /// Enum-Tabelle (SubAbstateAction, 121 Werte) abgeglichen und korrigiert -
    /// siehe DOCUMENTATION.md Abschnitt 23. Zwei konkrete Fehler dabei
    /// gefunden und behoben: ActionIndex 35/36 zeigten faelschlich auf
    /// End/Spr (tatsaechlich MaxHP/MaxSP), und die "kombinierten" Eintraege
    /// 2/12/13 (Str+Crit, Int+Crit, MagicDmg+MagicDef) waren falsch geraten -
    /// es sind laut Enum einfache Einzelwerte (STRPLUS/INTPLUS/MAPLUS), meine
    /// fruehere Mehrfach-Slot-Analyse hatte zwei separate Slots derselben
    /// AbState faelschlich als einen kombinierten Effekt interpretiert.
    ///
    /// WICHTIGE EINSCHRAENKUNG, WEITERHIN OFFEN: Die Enum unterscheidet
    /// zwischen "PLUS" (flacher Wert) und "RATE" (Prozentsatz) fuer viele
    /// Stats (z.B. STRPLUS=2 vs STRRATE=1). Diese Klasse wendet BEIDE
    /// Varianten aktuell gleich an (flache Addition), weil eine echte
    /// Prozent-Berechnung voraussetzen wuerde, den Basiswert VOR Anwendung
    /// des Buffs zu kennen - das ist in der aktuellen Buffs-Architektur
    /// (reine Additions-Summen, kein Zugriff auf BaseStats an dieser Stelle)
    /// nicht vorgesehen. Bei RATE-Eintraegen ist der Ingame-Effekt damit
    /// vermutlich falsch skaliert (zu schwach bei hohem Basiswert, zu stark
    /// bei niedrigem). Als naechster Ausbauschritt markiert.
    ///
    /// VORZEICHEN: Bleibt ueber AbStateInfo.IsBuff/IsDebuff bestimmt (aus
    /// AbStateView.shn IconSort), nicht durch den Index selbst - fuer
    /// "MINUS"/"DOWNRATE"-benannte Eintraege (die laut Enum-Namen ohnehin nur
    /// in Debuff-Kontexten sinnvoll sind) redundant aber konsistent.
    /// </summary>
    public static class BuffActionResolver
    {
        public delegate void ActionApplier(Buffs buffs, int signedArg);

        private static readonly Dictionary<uint, ActionApplier> Resolvers = new Dictionary<uint, ActionApplier>
        {
            // Primaerattribute (SAA_xPLUS = flach, SAA_xRATE = Prozent -
            // siehe Klassenkommentar zur RATE-Einschraenkung)
            [1]  = (b, v) => b.Str += v,   // SAA_STRRATE
            [2]  = (b, v) => b.Str += v,   // SAA_STRPLUS
            [7]  = (b, v) => b.Dex += v,   // SAA_DEXPLUS
            [12] = (b, v) => b.Int += v,   // SAA_INTPLUS
            [14] = (b, v) => b.Spr += v,   // SAA_MENTALPLUS (Mental = Spirit)
            [37] = (b, v) => b.Int += v,   // SAA_INTRATE
            [81] = (b, v) => b.Dex += v,   // SAA_DEXMINUS
            [89] = (b, v) => b.Str += v,   // SAA_STRMINUS
            [99] = (b, v) => b.Spr += v,   // SAA_MENDOWNRATE

            // HP/SP
            [22] = (b, v) => b.MaxHP += v,  // SAA_MAXHPRATE
            [23] = (b, v) => b.MaxSP += v,  // SAA_MAXSPRATE
            [35] = (b, v) => b.MaxHP += v,  // SAA_MAXHPPLUS (Korrektur: vorher faelschlich End)
            [36] = (b, v) => b.MaxSP += v,  // SAA_MAXSPPLUS (Korrektur: vorher faelschlich Spr)

            // Schaden
            [3]  = (b, v) => b.WeaponDamage += v,  // SAA_WCPLUS
            [4]  = (b, v) => b.WeaponDamage += v,  // SAA_WCRATE
            [13] = (b, v) => b.MagicDamage += v,   // SAA_MAPLUS
            [46] = (b, v) => b.MagicDamage += v,   // SAA_MARATE
            [94] = (b, v) => b.WeaponDamage += v,  // SAA_WCMINUS
            [95] = (b, v) => b.WeaponDamage += v,  // SAA_WCDOWNRATE

            // Verteidigung
            [5]  = (b, v) => b.WeaponDefense += v,  // SAA_ACPLUS
            [6]  = (b, v) => b.WeaponDefense += v,  // SAA_ACRATE
            [15] = (b, v) => b.MagicDefense += v,   // SAA_MRPLUS
            [16] = (b, v) => b.MagicDefense += v,   // SAA_MRRATE
            [73] = (b, v) => b.WeaponDefense += v,  // SAA_ACMINUS
            [74] = (b, v) => b.WeaponDefense += v,  // SAA_ACDOWNRATE
            [86] = (b, v) => b.MagicDefense += v,   // SAA_MRMINUS
            [87] = (b, v) => b.MagicDefense += v,   // SAA_MRDOWNRATE

            // Trefferwerte
            [10] = (b, v) => b.Aim += v,      // SAA_THPLUS
            [11] = (b, v) => b.Aim += v,      // SAA_THRATE
            [92] = (b, v) => b.Aim += v,      // SAA_THMINUS
            [93] = (b, v) => b.Aim += v,      // SAA_THDOWNRATE
            [34] = (b, v) => b.CriticalRate += v,  // SAA_CRITICALRATE
            [80] = (b, v) => b.CriticalRate += v,  // SAA_CRITICALDOWNRATE

            // Geschwindigkeit
            [20] = (b, v) => b.MoveSpeed += v,    // SAA_SPEEDRATE
            [88] = (b, v) => b.MoveSpeed += v,    // SAA_SPEEDDOWNRATE
            [21] = (b, v) => b.AttackSpeed += v,  // SAA_ATTACKSPEEDRATE
            [78] = (b, v) => b.AttackSpeed += v,  // SAA_ATKSPEEDDOWNRATE (Korrektur: vorher faelschlich als "Erhoehung" beschriftet)

            // Sonstiges mit vorhandenem Konsumenten
            [18] = (b, v) => b.BlockRate += v,          // SAA_SHIELDACRATE
            [71] = (b, v) => b.Evasion += v,            // SAA_EVASIONAMOUNT - vorher uebersehen, obwohl Buffs.Evasion laengst existiert
            // TB-Familie (8/9/90/91): per Nutzer-Hinweis geprueft, empirisch
            // bestaetigt durch saubere Einzel-Slot-Belege ("Increased/
            // Decreased Evasion") - offenbar eine zweite, eigenstaendige
            // Ausweichen-Aktionsfamilie neben SAA_EVASIONAMOUNT (71). Siehe
            // DOCUMENTATION.md Abschnitt 39.
            [8]  = (b, v) => b.Evasion += v,            // SAA_TBPLUS
            [90] = (b, v) => b.Evasion += v,            // SAA_TBMINUS
            [9]  = (b, v) => b.Evasion += v,            // SAA_TBRATE (Analogieschluss zu 8, keine eigene saubere Einzel-Slot-Evidenz)
            [91] = (b, v) => b.Evasion += v,            // SAA_TBDOWNRATE (Analogieschluss zu 90, keine eigene saubere Einzel-Slot-Evidenz)
            [31] = (b, v) => b.PoisonResistance += v,   // SAA_POISONRESISTRATE
            [32] = (b, v) => b.DiseaseResistance += v,  // SAA_DISEASERESISTRATE
            [33] = (b, v) => b.CurseResistance += v,    // SAA_CURSERESISTRATE
            [107] = (b, v) => b.ExpBonusPercent += v,   // SAA_EXPRATE (Konsument: ZoneCharacter.GiveExp())

            // Ausgehende DoT-Schadensverstaerker (Konsument: Buff.TickPeriodic
            // via MapObject.GetDotDamageBonusPercent())
            [68] = (b, v) => b.DotDamageBonusPercent += v,        // SAA_ADDALLDOTDMG
            [75] = (b, v) => b.DotDamageBonusPercent += v,        // SAA_SUBTRACTALLDOTDMG
            [70] = (b, v) => b.PoisonDamageBonusPercent += v,     // SAA_ADDPOISONDMG
            [77] = (b, v) => b.PoisonDamageBonusPercent += v,     // SAA_SUBTRACTPOISONDMG
            [69] = (b, v) => b.BloodingDamageBonusPercent += v,   // SAA_ADDBLOODINGDMG
            [76] = (b, v) => b.BloodingDamageBonusPercent += v,   // SAA_SUBTRACTBLOODINGDMG

            // Unverwundbarkeit/Reflexion (Konsument: MapObject.Damage())
            [17] = (b, v) => b.ShieldAmount += v,          // SAA_SHIELDAMOUNT
            [61] = (b, v) => b.ReflectDamagePercent += v,  // SAA_REFLECTDAMAGE
            [60] = (b, v) => b.MissRatePercent += v,       // SAA_MISSRATE
            // SAA_GTIRESISTRATE (56) - Einzelbeleg "Grants immunity from all
            // damaging effects", Arg=1000. Funktional identisch zu 100%
            // MissRate (nie getroffen = immun). Skalierung (/10, 1000->100%)
            // auf Basis nur EINES Datenpunkts angenommen, nicht mehrfach
            // verifiziert. Siehe DOCUMENTATION.md Abschnitt 43.
            [56] = (b, v) => b.MissRatePercent += v / 10,
            // Infrastruktur-Bausteine, siehe DOCUMENTATION.md Abschnitt 46.
            [108] = (b, v) => b.DropRatePercent += v,             // SAA_DROPRATE
            [29] = (b, v) => b.CastingTimeBonusPercent += v,      // SAA_CASTINGTIMEPLUS
            [102] = (b, v) => b.IgnoreMagicDamagePercent += v,    // SAA_MRSHIELDRATE
            [103] = (b, v) => b.IgnorePhysicalDamagePercent += v, // SAA_ACSHIELDRATE
            [113] = (b, v) => b.SPRegenRatePercent += v,          // SAA_LPAMOUNT
            [24] = (b, v) => b.PartyDeathHealPermille += v,       // SAA_DEADHPSPRECOVRATE
            // Per Nutzer-Hinweis empirisch bestaetigt (Einzel-Slot-Beleg
            // "HP will not drop below 1"), siehe DOCUMENTATION.md
            // Abschnitt 40.
            [114] = (b, v) => b.MinHP += v,                // SAA_MINHP
            // Per zweiter Nutzer-CSV empirisch verifiziert (SubStaRebirth,
            // Werte 200-600), siehe DOCUMENTATION.md Abschnitt 42.
            [40] = (b, v) => b.ReviveHealRatePermille += v, // SAA_REVIVEHEALRATE

            // Alle Primaerattribute gleichzeitig
            [39]  = (b, v) => { b.Str += v; b.End += v; b.Dex += v; b.Int += v; b.Spr += v; },  // SAA_ALLSTATEPLUS
            [119] = (b, v) => { b.Str += v; b.End += v; b.Dex += v; b.Int += v; b.Spr += v; },  // SAA_ALLSTATPLUS
        };

        public static void Apply(Buffs buffs, AbStateInfo abState, SubAbStateAction action, bool apply)
        {
            if (Resolvers.TryGetValue(action.ActionIndex, out var resolver))
            {
                int magnitude = (int)action.ActionArg * (abState.IsBuff ? 1 : -1);
                int signedArg = apply ? magnitude : -magnitude;
                resolver(buffs, signedArg);
            }
            else
            {
                Util.Log.WriteLine(Util.LogLevel.Debug, "BuffActionResolver: ActionIndex {0} (Arg {1}, AbState '{2}') nicht aufgeloest, keine Wirkung angewendet.", action.ActionIndex, action.ActionArg, abState.InxName);
            }
        }
    }
}
