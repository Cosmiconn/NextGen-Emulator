using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Util;
using NextGen.Zone.Data;
using NextGen.Zone.Handlers;

namespace NextGen.Zone.Game
{
    public abstract class MapObject
    {
        #region .ctor
        public MapObject()
        {
            IsAttackable = true;
            SelectedBy = new List<ZoneCharacter>();
            Buffs = new Buffs.Buffs(this);
        }
        ~MapObject()
        {
            SelectedBy.Clear();
        }
        #endregion
        #region Buffs
        // Gemeinsame Buff/Debuff-Verwaltung fuer ZoneCharacter UND Mob (vorher
        // in beiden Klassen dupliziert, siehe DOCUMENTATION.md Abschnitt 17/19).
        // protected statt private: ZoneCharacter nutzt Buffs direkt in
        // mehreren Get*()-Stat-Aggregationsmethoden (GetExtraStr,
        // GetWeaponDamage(buffed) etc. - bereits im Original-Code vorgesehen,
        // siehe DOCUMENTATION.md Abschnitt 19).
        protected Buffs.Buffs Buffs { get; set; }
        public void AddBuff(AbStateInfo abState, uint strength, MapObject caster = null)
        {
            Buffs.AddBuff(abState, strength, caster);
        }
        public void RemoveBuff(ushort abStateId)
        {
            Buffs.RemoveBuff(abStateId);
        }
        public void TickBuffs(DateTime now)
        {
            Buffs.Tick(now);
        }
        // Oeffentliche Zugriffsmethoden fuer Klassen ausserhalb der
        // MapObject-Hierarchie (z.B. AttackSequence.cs), die Buffs (protected)
        // nicht direkt lesen koennen. Siehe DOCUMENTATION.md Abschnitt 20.
        public int GetCriticalRateBuff()
        {
            return Buffs.CriticalRate;
        }
        // SAA_DROPRATE (108), siehe DOCUMENTATION.md Abschnitt 46.
        public int GetDropRateBonusPercent()
        {
            return Buffs.DropRatePercent;
        }
        // SAA_CASTINGTIMEPLUS (29), siehe DOCUMENTATION.md Abschnitt 46.
        public int GetCastingTimeBonusPercent()
        {
            return Buffs.CastingTimeBonusPercent;
        }
        // SAA_MRSHIELDRATE (102) / SAA_ACSHIELDRATE (103), siehe
        // DOCUMENTATION.md Abschnitt 46.
        public int GetIgnoreMagicDamagePercent()
        {
            return Buffs.IgnoreMagicDamagePercent;
        }
        public int GetIgnorePhysicalDamagePercent()
        {
            return Buffs.IgnorePhysicalDamagePercent;
        }
        public int GetAttackSpeedBuff()
        {
            return Buffs.AttackSpeed;
        }
        public int GetMoveSpeedBuff()
        {
            return Buffs.MoveSpeed;
        }
        // Fuer Buff.TickPeriodic() - Schadensbonus auf ausgehende periodische
        // Effekte (SAA_ADDALLDOTDMG etc.), unterschieden nach Debuff-Art ueber
        // DispelIndex/SubDispelIndex (DispelAttr.DA_POISON=4,
        // SubDispelAttr.SDA_BLOODING=4 - siehe DOCUMENTATION.md Abschnitt 24).
        public int GetDotDamageBonusPercent(AbStateInfo debuff)
        {
            int bonus = Buffs.DotDamageBonusPercent;
            if (debuff.DispelIndex == 4) // DA_POISON
                bonus += Buffs.PoisonDamageBonusPercent;
            if (debuff.SubDispelIndex == 4) // SDA_BLOODING
                bonus += Buffs.BloodingDamageBonusPercent;
            return bonus;
        }
        // Wrapper fuer Buffs.AddPassiveSkill/RemovePassiveSkill (permanente
        // Wirkung, siehe DOCUMENTATION.md Abschnitt 25).
        public void AddPassiveSkill(FiestaLib.Data.PassiveSkillInfo skill)
        {
            Buffs.AddPassiveSkill(skill);
        }
        public void RemovePassiveSkill(FiestaLib.Data.PassiveSkillInfo skill)
        {
            Buffs.RemovePassiveSkill(skill);
        }
        // Wendet die bis zu 4 AbState-Slots eines Skills an. Pro Slot wird das
        // Ziel anhand von AbStateInfo.IsBuff/IsDebuff bestimmt (aus
        // AbStateView.shn, siehe DOCUMENTATION.md Abschnitt 19): Buffs gehen
        // an buffRecipient, Debuffs an debuffRecipient. Zwei getrennte
        // Parameter statt eines einzelnen Ziels, weil "wer den Buff bekommt"
        // und "wer den Debuff bekommt" je nach Skill-Art unterschiedlich sind
        // - z.B. bei einem Heilzauber auf einen Verbuendeten sollte ein
        // begleitender Buff an den Verbuendeten (das Heilziel) gehen, nicht an
        // den Anwender; bei einem Angriffszauber dagegen an den Anwender
        // selbst, waehrend ein Debuff an den angegriffenen Gegner geht.
        public void ApplySkillAbStates(ActiveSkillInfo skillInfo, MapObject buffRecipient, MapObject debuffRecipient)
        {
            foreach (var slot in skillInfo.AbStateSlots)
            {
                if (slot.SuccessRate < 100 && Program.Randomizer.Next(0, 100) >= slot.SuccessRate)
                    continue;

                if (!DataProvider.Instance.AbStatesByName.TryGetValue(slot.AbStateName, out var abState))
                {
                    Log.WriteLine(LogLevel.Warn, "Skill referenziert unbekannten AbState '{0}'.", slot.AbStateName);
                    continue;
                }
                MapObject recipient = abState.IsBuff ? buffRecipient : debuffRecipient;
                recipient.AddBuff(abState, slot.Strength, this);
            }
        }
        #endregion
        #region Properties
        public bool IsAdded { get; set; }
        public bool IsAttackable { get; set; }
        // Crowd-Control-Zustaende aus AbState-Effekten (ActionIndex 19/25 =
        // "Stunned", 38 = "Fear") - siehe DOCUMENTATION.md Abschnitt 23.
        // Bewusst einfache Flags statt eines komplexen CC-Systems: deckt die
        // mit Abstand haeufigste Kategorie (223 Vorkommen in den echten
        // Daten) minimal-invasiv ab, ohne ein Bewegungs-/Physik-System
        // vorauszusetzen, das es in diesem Projekt noch nicht gibt. Ueber
        // Buffs berechnet (nicht direkt gesetzt), damit mehrere gleichzeitig
        // aktive Stun-Quellen korrekt behandelt werden.
        public bool IsStunned { get { return Buffs.IsStunned; } }
        public bool IsFeared { get { return Buffs.IsFeared; } }
        // Silence blockiert nur Skills, nicht den normalen Nahkampf - anders
        // als Stun/Fear daher bewusst NICHT in CanAct, sondern separat in den
        // Skill-Handlern geprueft (Handler9.cs). Siehe DOCUMENTATION.md
        // Abschnitt 38.
        public bool IsSilenced { get { return Buffs.IsSilenced; } }
        // SAA_HIDEENEMY (65), siehe DOCUMENTATION.md Abschnitt 46.
        public bool IsInvisible { get { return Buffs.IsInvisible; } }
        public bool CanAct { get { return !IsStunned && !IsFeared; } }
        public bool IsDead { get { return HP == 0; } }


        public Map Map { get; set; }
        public Sector MapSector { get; set; }
        public Vector2 Position { get; set; }
        // SAA_AWAY (49, Knockback) / SAA_AWAYBACKSPOT (109, Pull), siehe
        // DOCUMENTATION.md Abschnitt 46/47. Bewusst einfach gehalten: keine
        // Kollisions- oder Kartengrenzen-Pruefung (dokumentierte
        // Vereinfachung) - berechnet eine neue Position entlang der Achse
        // zum/vom Ursprung und broadcastet sie ueber die bestehende
        // Bewegungs-Infrastruktur (Handler8.MoveObject).
        public void ForceMove(MapObject source, int distance, bool towardSource)
        {
            if (source == null || distance == 0) return;
            var oldPos = Position;
            double dx = Position.X - source.Position.X;
            double dy = Position.Y - source.Position.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) { dx = 1; dy = 0; len = 1; } // Angreifer exakt auf Ziel-Position: beliebige Richtung
            double dirX = towardSource ? -dx / len : dx / len;
            double dirY = towardSource ? -dy / len : dy / len;
            Position = new Vector2((int)(Position.X + dirX * distance), (int)(Position.Y + dirY * distance));
            using (var packet = Handler8.MoveObject(this, oldPos.X, oldPos.Y, false))
            {
                MapSector.Broadcast(packet);
            }
        }
        public byte Rotation { get; set; }
        public ushort MapObjectID { get; set; }

        public virtual uint HP { get; set; }
        public virtual uint MaxHP { get; set; }
        public virtual uint SP { get; set; }
        public virtual uint MaxSP { get; set; }

        public List<ZoneCharacter> SelectedBy { get; private set; }
        public ushort UpdateCounter { get { return ++statUpdateCounter; } }

        // HP/SP update counter thingy
        private ushort statUpdateCounter = 0;
        public static readonly TimeSpan HpSpUpdateInterval = TimeSpan.FromSeconds(3);
        protected DateTime lastHpSpUpdate = DateTime.Now; 
        #endregion
        #region Methods
        public virtual void Attack(MapObject victim)
        {
            if (victim != null && !victim.IsAttackable) return;
        }
        public virtual void AttackSkill(ushort skillid, MapObject victim)
        {
            if (victim != null && !victim.IsAttackable) return;
        }
        public virtual void AttackSkillAoE(ushort skillid, uint x, uint y)
        {
        }
        public virtual void Revive(bool totally = false)
        {
            if (totally)
            {
                HP = MaxHP;
                SP = MaxSP;
            }
            else if (Buffs.ReviveHealRatePermille > 0)
            {
                // SAA_REVIVEHEALRATE (40), siehe Buffs.cs und
                // DOCUMENTATION.md Abschnitt 42.
                HP = (uint)(MaxHP * (uint)Math.Min(1000, Buffs.ReviveHealRatePermille) / 1000);
            }
            else
            {
                // Note - Why not take e.g. 10% of your MaxHp?
                // HP = MaxHP * 0.1;
                HP = 50;
            }
        }
        // isReflected: true nur beim rekursiven Aufruf aus der eigenen
        // Reflect-Logik unten - verhindert endlose Reflexions-Schleifen,
        // wenn beide Beteiligten SAA_REFLECTDAMAGE aktiv haben. Siehe
        // DOCUMENTATION.md Abschnitt 24.
        public virtual void Damage(MapObject bully, uint amount, bool isSP = false, bool isReflected = false)
        {
            if (!isReflected && !isSP)
            {
                // SAA_MISSRATE (60) - komplette Negierung des Treffers, siehe
                // DOCUMENTATION.md Abschnitt 24. Nur fuer HP-Schaden (SP-Verbrauch
                // durch Skills soll davon nicht betroffen sein).
                if (Buffs.MissRatePercent > 0 && Program.Randomizer.Next(0, 100) < Buffs.MissRatePercent)
                    return;

                // SAA_SHIELDAMOUNT (17) - absorbiert Schaden aus einem
                // Schadensschild-Pool, bevor er die HP erreicht. Vereinfachung:
                // der Pool wird direkt verbraucht, nicht beim Buff-Ablauf
                // zurueckgesetzt (er ist ohnehin dazu da, aufgebraucht zu werden).
                if (Buffs.ShieldAmount > 0)
                {
                    uint absorbed = (uint)Math.Min(Buffs.ShieldAmount, amount);
                    Buffs.ShieldAmount -= (int)absorbed;
                    amount -= absorbed;
                    if (amount == 0) return;
                }
            }

            if (isSP)
            {
                if (SP < amount) SP = 0;
                else SP -= amount;
            }
            else
            {
                if (HP < amount) HP = 0;
                else HP -= amount;

                // SAA_MINHP (114) - HP faellt durch Schaden nicht unter diesen
                // Wert (Unsterblichkeits-Schwelle). Siehe DOCUMENTATION.md
                // Abschnitt 40. Nur fuer HP-Schaden, nicht SP.
                if (Buffs.MinHP > 0 && HP < (uint)Buffs.MinHP)
                    HP = (uint)Buffs.MinHP;
            }

            // SAA_REFLECTDAMAGE (61) - spiegelt einen Prozentsatz des
            // erlittenen Schadens an den Angreifer zurueck. isReflected=true
            // beim Rueckschlag selbst, damit das nicht in eine Endlosschleife
            // laeuft, falls beide Seiten reflektieren.
            if (!isReflected && !isSP && bully != null && Buffs.ReflectDamagePercent > 0)
            {
                uint reflected = (uint)(amount * Buffs.ReflectDamagePercent / 100);
                if (reflected > 0)
                    bully.Damage(this, reflected, false, true);
            }

            if (bully == null)
            {
                if (this is ZoneCharacter)
                {
                    ZoneCharacter character = this as ZoneCharacter;
                    if (isSP)
                        Handler9.SendUpdateSP(character);
                    else
                        Handler9.SendUpdateHP(character);
                }
            }
            else
            {
                if (this is Mob && ((Mob)this).AttackingSequence == null)
                {
                    this.Attack(bully);
                }
                else if (this is ZoneCharacter && !((ZoneCharacter)this).IsAttacking)
                {
                    this.Attack(bully);
                }
            }
        }
        // Gegenstueck zu Damage() fuer periodische Heilung (ActionIndex
        // 26/27/30, siehe DOCUMENTATION.md Abschnitt 21) - dieselbe
        // HP/SP-Synchronisations-Logik wie beim ZoneCharacter-spezifischen
        // HealHP()/HealSP(), aber auf MapObject-Ebene, damit auch Mob
        // periodisch geheilt werden kann (z.B. ein sich selbst heilender
        // Boss).
        public virtual void Heal(uint amount, bool isSP = false)
        {
            if (isSP)
            {
                if (SP + amount > MaxSP) SP = MaxSP;
                else SP += amount;
            }
            else
            {
                if (HP + amount > MaxHP) HP = MaxHP;
                else HP += amount;
            }

            if (this is ZoneCharacter character)
            {
                if (isSP)
                    Handler9.SendUpdateSP(character);
                else
                    Handler9.SendUpdateHP(character);
            }
        }

        public abstract void Update(DateTime date);
        public abstract Packet Spawn();
        #endregion
        #region Event-Stuff
        // Event trigger
        protected virtual void OnHpSpChanged()
        {
            if (HpSpChanged != null)
            {
                HpSpChanged(this, new EventArgs());
            }
        }

        // Event-Variables
        public event EventHandler<EventArgs> HpSpChanged;
        #endregion
    }
}
