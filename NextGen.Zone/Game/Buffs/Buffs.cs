/*File for this file Basic Copyright 2012 no0dl */
using System;
using System.Collections.Generic;
using System.Linq;
using NextGen.FiestaLib.Data;
using NextGen.Util;

namespace NextGen.Zone.Game.Buffs
{
	public class Buffs
    {
        private MapObject Character {get;set;}

        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int MinMagic { get; set; }
        public int MaxMagic { get; set; }
        public int WeaponDefense { get; set; }
        public int WeaponDamage { get; set; }
        public int MagicDefense { get; set; }
        public int MagicDamage { get; set; }
        public int Evasion { get; set; }
        public int Str { get; set; }
        public int End { get; set; }
        public int Dex { get; set; }
        public int Int { get; set; }
        public int Spr { get; set; }
        public int MaxHP { get; set; }
        public int MaxSP { get; set; }
        // Ergaenzt beim Verdrahten der evidenzbasierten ActionIndex-Zuordnung
        // (siehe DOCUMENTATION.md Abschnitt 19) - im urspruenglichen
        // Datencontainer nicht vorhanden, aber fuer die am haeufigsten
        // belegten AbState-Effekte (Aim, Critical Rate, Attack Speed,
        // Move Speed) noetig.
        public int Aim { get; set; }
        public int CriticalRate { get; set; }
        public int AttackSpeed { get; set; }
        public int MoveSpeed { get; set; }
        // ActionIndex 107 ("Gives a bonus of X% EXP increase from hunting"),
        // siehe DOCUMENTATION.md Abschnitt 20. Prozentpunkte, addiert auf den
        // EXP-Multiplikator in ZoneCharacter.GiveExp().
        public int ExpBonusPercent { get; set; }
        // Neue Properties ohne bestehenden Verbraucher im Code - Daten stehen
        // bereit, sobald eine passende Spielmechanik (Block-Wurf,
        // Gift-/Krankheits-/Fluch-Anwendungschance) existiert. Siehe
        // DOCUMENTATION.md Abschnitt 22.
        public int BlockRate { get; set; }
        public int PoisonResistance { get; set; }
        public int DiseaseResistance { get; set; }
        public int CurseResistance { get; set; }
        // Ausgehende DoT-Schadensverstaerker (SAA_ADDALLDOTDMG=68/
        // SAA_SUBTRACTALLDOTDMG=75, SAA_ADDPOISONDMG=70/
        // SAA_SUBTRACTPOISONDMG=77, SAA_ADDBLOODINGDMG=69/
        // SAA_SUBTRACTBLOODINGDMG=76 - siehe DOCUMENTATION.md Abschnitt 24).
        // Prozentwerte, wirken nicht auf den Traeger selbst, sondern auf den
        // Schaden, den seine eigenen periodischen Effekte bei ANDEREN
        // anrichten - siehe Buff.Caster/TickPeriodic.
        public int DotDamageBonusPercent { get; set; }
        public int PoisonDamageBonusPercent { get; set; }
        public int BloodingDamageBonusPercent { get; set; }
        // SAA_REFLECTDAMAGE (61), SAA_MISSRATE (60) - siehe MapObject.Damage()
        // und DOCUMENTATION.md Abschnitt 24.
        public int ReflectDamagePercent { get; set; }
        public int MissRatePercent { get; set; }
        // SAA_SHIELDAMOUNT (17) - wird von MapObject.Damage() direkt
        // verbraucht (nicht nur additiv wie die anderen Properties). Setter
        // klemmt auf >=0, damit ein spaeteres Deactivate() (das den
        // urspruenglichen Betrag wieder abzieht) nicht ins Negative laeuft,
        // wenn der Schild zwischenzeitlich schon (teilweise) aufgebraucht
        // wurde.
        private int shieldAmount;
        public int ShieldAmount
        {
            get { return shieldAmount; }
            set { shieldAmount = Math.Max(0, value); }
        }
        // SAA_MINHP (114) - empirisch bestaetigt ("HP will not drop below 1"),
        // siehe DOCUMENTATION.md Abschnitt 40. Konsument: MapObject.Damage().
        public int MinHP { get; set; }
        // SAA_REVIVEHEALRATE (40) - Beleg: SubStaRebirth mit Werten 200-600,
        // interpretiert als Promille (20%-60% MaxHP) statt direktem Prozent,
        // da 200-600% als Prozentwert unplausibel waere (mehr als volle HP).
        // Diese Skalierungs-Annahme ist NICHT verifiziert. Konsument:
        // MapObject.Revive(). Siehe DOCUMENTATION.md Abschnitt 42.
        public int ReviveHealRatePermille { get; set; }
        // Neue Properties fuer die Infrastruktur-Bausteine aus Abschnitt 46:
        public int DropRatePercent { get; set; }              // SAA_DROPRATE (108)
        public int CastingTimeBonusPercent { get; set; }      // SAA_CASTINGTIMEPLUS (29)
        public int IgnoreMagicDamagePercent { get; set; }     // SAA_MRSHIELDRATE (102)
        public int IgnorePhysicalDamagePercent { get; set; }  // SAA_ACSHIELDRATE (103)
        public int SPRegenRatePercent { get; set; }           // SAA_LPAMOUNT (113) - siehe Abschnitt 44.2, vermutlich SP fuer Sentinel/Savior
        // SAA_DEADHPSPRECOVRATE (24) - Beleg: SubStaSacrifice, "Recover
        // party's HP and SP upon death". Interpretiert als Promille (wie
        // ReviveHealRatePermille), da Sacrifice-Werte 500-1100 als direkter
        // Prozentsatz unplausibel waeren. Konsument: ZoneCharacter.Damage().
        // Siehe DOCUMENTATION.md Abschnitt 46.
        public int PartyDeathHealPermille { get; set; }

        private List<Buff> CurrentBuffs { get; set; }

        public Buffs(MapObject pChar)
        {
            Character = pChar;
            CurrentBuffs = new List<Buff>();
        }

        // Fuegt einen Buff/Debuff hinzu (bzw. erneuert ihn, falls derselbe
        // AbState bereits aktiv ist - kein Stacking mehrerer Instanzen
        // desselben AbState, siehe AbStateInfo.Duplicate fuer die im .shn
        // hinterlegte, aber noch nicht ausgewertete Duplicate-Erlaubnis).
        public void AddBuff(AbStateInfo abState, uint strength, MapObject caster = null)
        {
            if (abState == null) return;

            if (!abState.SubAbStates.TryGetValue(strength, out var subState))
            {
                Log.WriteLine(LogLevel.Warn, "AddBuff: AbState '{0}' hat keine Staerke-Stufe {1}.", abState.InxName, strength);
                return;
            }

            lock (CurrentBuffs)
            {
                var existing = CurrentBuffs.FirstOrDefault(b => b.AbState.ID == abState.ID);
                if (existing != null)
                {
                    existing.Deactivate(this);
                    CurrentBuffs.Remove(existing);
                }

                var buff = new Buff(Character, abState, subState, caster);
                CurrentBuffs.Add(buff);
                buff.Activate(this);
            }
        }

        public void RemoveBuff(ushort abStateId)
        {
            lock (CurrentBuffs)
            {
                var existing = CurrentBuffs.FirstOrDefault(b => b.AbState.ID == abStateId);
                if (existing == null) return;
                existing.Deactivate(this);
                CurrentBuffs.Remove(existing);
            }
        }

        // Muss regelmaessig aufgerufen werden (z.B. aus dem Zone-Tick), um
        // abgelaufene Buffs zu entfernen. Aktuell nirgends verdrahtet, da es
        // in NextGen.Zone noch keine zentrale Tick-Schleife fuer
        // Charakter-Updates gibt - siehe DOCUMENTATION.md Abschnitt 15.
        public void Tick(DateTime now)
        {
            List<Buff> expired;
            lock (CurrentBuffs)
            {
                expired = CurrentBuffs.Where(b => b.ExpireTime <= now).ToList();
                foreach (var buff in expired)
                {
                    buff.Deactivate(this);
                    CurrentBuffs.Remove(buff);
                }

                // Periodische Effekte (ActionIndex 26/27/30, siehe
                // DOCUMENTATION.md Abschnitt 21) fuer alle weiterhin aktiven
                // Buffs anwenden - nach dem Entfernen der abgelaufenen, damit
                // ein gerade erst abgelaufener Buff keinen letzten Tick mehr
                // ausloest.
                foreach (var buff in CurrentBuffs)
                {
                    buff.TickPeriodic(now);
                }
            }
        }

        public IEnumerable<Buff> ActiveBuffs
        {
            get { lock (CurrentBuffs) { return CurrentBuffs.ToList(); } }
        }

        // Permanente (nicht ablaufende) Wirkung passiver Skills - anders als
        // AddBuff/RemoveBuff kein Buff-Objekt mit KeepTime, sondern direkte,
        // dauerhafte Aufsummierung, solange der Skill erlernt ist. Gleiches
        // Additions-/Subtraktionsmuster wie Buff.Activate()/Deactivate(),
        // damit Lernen/Verlernen sich sauber gegenseitig aufhebt. Siehe
        // DOCUMENTATION.md Abschnitt 25.
        public void AddPassiveSkill(PassiveSkillInfo skill)
        {
            MaxSP += (int)skill.MaxSP;
            Int += (int)skill.Intel;
            WeaponDamage += (int)skill.WCRateUp;
            MagicDamage += (int)skill.MARateUp;
            CriticalRate += skill.MACriRate;
        }
        public void RemovePassiveSkill(PassiveSkillInfo skill)
        {
            MaxSP -= (int)skill.MaxSP;
            Int -= (int)skill.Intel;
            WeaponDamage -= (int)skill.WCRateUp;
            MagicDamage -= (int)skill.MARateUp;
            CriticalRate -= skill.MACriRate;
        }

        // Siehe Buff.IsStun/IsFear - ueber alle aktiven Buffs berechnet,
        // damit mehrere gleichzeitig aktive Stun-Quellen korrekt behandelt
        // werden (siehe DOCUMENTATION.md Abschnitt 23).
        public bool IsStunned { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsStun); } } }
        public bool IsFeared { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsFear); } } }
        public bool IsSilenced { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsSilence); } } }
        // SAA_HIDEENEMY (65) - gleiches Berechnungs-Muster wie Stun/Fear/
        // Silence (nicht additiv, um Stapel-Bugs zu vermeiden). Siehe
        // DOCUMENTATION.md Abschnitt 46.
        public bool IsInvisible { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsInvisible); } } }
    }
}
