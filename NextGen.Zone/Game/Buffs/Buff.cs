/*File for this file Basic Copyright 2012 no0dl */
using System;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Game.Buffs
{
    /// <summary>
    /// Eine aktive Buff/Debuff-Instanz auf einem Charakter: ein konkreter
    /// AbState in einer konkreten Staerke-Stufe (SubAbstateInfo), mit
    /// Start-/Ablaufzeit. Ersetzt die vorherige, komplett auskommentierte
    /// Fassung dieser Datei (referenzierte nicht-existente Typen wie
    /// LivingObject/BuffAction/StatsAction) durch eine Implementierung gegen
    /// die tatsaechlich im Projekt vorhandenen Typen.
    /// </summary>
    public sealed class Buff
    {
        // KORREKTUR (siehe DOCUMENTATION.md Abschnitt 23): ActionIndex 26 ist
        // KEIN periodischer Schadens-/Heilbetrag, sondern das Tick-Intervall
        // selbst, in Millisekunden. Empirisch bestaetigt: 26 bleibt ueber
        // alle Staerke-Stufen einer Faehigkeit hinweg KONSTANT (z.B. immer
        // 1000 bei "PoisonShot"), waehrend 27/30 sauber mit der Staerke
        // skalieren (z.B. PoisonShot: 39->49->58->...->778). Die
        // tatsaechlichen periodischen Betraege stecken in 27 (ueblicherweise
        // Schaden) und 30 (ueblicherweise Heilung) - Richtung wird wie ueberall
        // ueber AbState.IsBuff/IsDebuff bestimmt, nicht durch den Index selbst.
        private static readonly uint[] PeriodicAmountActionIndices = { 27, 30 };
        private const uint PeriodicIntervalActionIndex = 26;

        // ActionIndex 19 = SAA_NOMOVE, 25 = SAA_NOATTACK (autoritative Enum,
        // siehe DOCUMENTATION.md Abschnitt 23 - vorherige Beschriftung als
        // "Stunned" war eine empirische Annaeherung, nicht die exakten
        // Enum-Namen). Beide zusammen ergeben praktisch eine Betaeubung;
        // da es kein Bewegungssystem gibt (NOMOVE waere ohnehin wirkungslos),
        // werden beide vereinfacht auf dieselbe IsStunned-Flag abgebildet.
        // 38 = SAA_FEAR.
        private static readonly uint[] StunActionIndices = { 19, 25 };
        private const uint FearActionIndex = 38;
        // SAA_SILIENCE (Fiesta-Heroes-Doku-Schreibweise, nicht korrigiert) -
        // dritter Crowd-Control-Zustand, siehe DOCUMENTATION.md Abschnitt 38.
        private const uint SilenceActionIndex = 42;
        // SAA_HIDEENEMY (65) - Unsichtbarkeit, siehe DOCUMENTATION.md
        // Abschnitt 46. Vereinfacht als "unsichtbar fuer Monster-Aggro"
        // umgesetzt statt der engeren, belegten Bedeutung ("nur gegenueber
        // gegnerischer Gilde") - Gilden-PvP-Sichtbarkeit ist in diesem
        // Projekt kein bestehendes Konzept.
        private const uint HideEnemyActionIndex = 65;

        public static TimeSpan PeriodicInterval { get; set; } = TimeSpan.FromSeconds(1);

        public MapObject Character { get; private set; }
        // Wer diesen Buff/Debuff verursacht hat (z.B. fuer ausgehende
        // DoT-Schadensverstaerker wie SAA_ADDALLDOTDMG, siehe
        // DOCUMENTATION.md Abschnitt 24) - null, wenn unbekannt (z.B. bei
        // World-seitig per InterServer gesendeten Buffs ohne Caster-Info).
        public MapObject Caster { get; private set; }
        public AbStateInfo AbState { get; private set; }
        public SubAbstateInfo SubState { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime ExpireTime { get; private set; }
        private DateTime lastPeriodicTick;
        // Aus ActionIndex 26 dieses konkreten SubState extrahiert, falls
        // vorhanden - sonst Fallback auf das globale, konfigurierbare
        // PeriodicInterval (Zone.PeriodicBuffTickMs).
        private readonly TimeSpan periodicInterval;

        public Buff(MapObject character, AbStateInfo abState, SubAbstateInfo subState, MapObject caster = null)
        {
            Character = character;
            Caster = caster;
            AbState = abState;
            SubState = subState;
            StartTime = DateTime.UtcNow;
            ExpireTime = StartTime + subState.KeepTime;
            lastPeriodicTick = StartTime;

            var intervalAction = subState.Actions.FirstOrDefault(a => a.ActionIndex == PeriodicIntervalActionIndex);
            periodicInterval = intervalAction != null
                ? TimeSpan.FromMilliseconds(intervalAction.ActionArg)
                : PeriodicInterval;
        }

        // SAA_AWAY (49, Knockback) / SAA_AWAYBACKSPOT (109, Pull) - einmalige
        // Ausloeser, nicht Teil des additiven Resolver-Musters. Siehe
        // DOCUMENTATION.md Abschnitt 47.
        private const uint KnockbackActionIndex = 49;
        private const uint PullActionIndex = 109;

        public void Activate(Buffs owner)
        {
            foreach (var action in SubState.Actions)
            {
                if (StunActionIndices.Contains(action.ActionIndex) || action.ActionIndex == FearActionIndex || action.ActionIndex == SilenceActionIndex || action.ActionIndex == HideEnemyActionIndex)
                    continue; // siehe IsStun/IsFear/IsSilence/IsInvisible - berechnet, nicht gesetzt
                if (action.ActionIndex == KnockbackActionIndex)
                {
                    Character.ForceMove(Caster ?? Character, (int)action.ActionArg, false);
                    continue;
                }
                if (action.ActionIndex == PullActionIndex)
                {
                    Character.ForceMove(Caster ?? Character, (int)action.ActionArg, true);
                    continue;
                }
                if (IsPeriodic(action.ActionIndex))
                    continue; // siehe TickPeriodic(), nicht einmalig anwenden
                BuffActionResolver.Apply(owner, AbState, action, true);
            }
        }

        public void Deactivate(Buffs owner)
        {
            foreach (var action in SubState.Actions)
            {
                if (StunActionIndices.Contains(action.ActionIndex) || action.ActionIndex == FearActionIndex || action.ActionIndex == SilenceActionIndex || action.ActionIndex == HideEnemyActionIndex)
                    continue;
                if (action.ActionIndex == KnockbackActionIndex || action.ActionIndex == PullActionIndex)
                    continue; // einmaliger Ausloeser, nichts rueckgaengig zu machen
                if (IsPeriodic(action.ActionIndex))
                    continue;
                BuffActionResolver.Apply(owner, AbState, action, false);
            }
        }

        // Berechnet statt beim Activate()/Deactivate() gesetzt: verhindert,
        // dass ein ablaufender Stun-Debuff faelschlich IsStunned loescht,
        // waehrend ein ZWEITER, andersartiger Stun-Debuff noch aktiv ist
        // (zwei verschiedene AbStates koennen gleichzeitig aktiv sein, siehe
        // Buffs.AddBuff - nur derselbe AbState wird ersetzt, nicht gestapelt).
        public bool IsStun { get { return SubState.Actions.Any(a => StunActionIndices.Contains(a.ActionIndex)); } }
        public bool IsSilence { get { return SubState.Actions.Any(a => a.ActionIndex == SilenceActionIndex); } }
        public bool IsFear { get { return SubState.Actions.Any(a => a.ActionIndex == FearActionIndex); } }
        public bool IsInvisible { get { return SubState.Actions.Any(a => a.ActionIndex == HideEnemyActionIndex); } }

        private static bool IsPeriodic(uint actionIndex)
        {
            return actionIndex == PeriodicIntervalActionIndex || PeriodicAmountActionIndices.Contains(actionIndex);
        }

        // Von Buffs.Tick() aufgerufen. Wendet periodische HP-Effekte (27/30)
        // an, wenn seit dem letzten Tick genug Zeit vergangen ist (per-Buff-
        // Intervall aus ActionIndex 26, siehe Konstruktor). Debuff = Schaden
        // (ueber Character.Damage(), bully=null - kein Gegenangriff, aber
        // korrekter HP-Sync), Buff = Heilung (ueber Character.Heal()).
        // Schadensbetrag wird um den DoT-Schadensbonus des Verursachers
        // (Caster) skaliert, falls bekannt - siehe DOCUMENTATION.md
        // Abschnitt 24 (SAA_ADDALLDOTDMG/SAA_ADDPOISONDMG etc.).
        public void TickPeriodic(DateTime now)
        {
            if (now - lastPeriodicTick < periodicInterval)
                return;
            lastPeriodicTick = now;

            foreach (var action in SubState.Actions)
            {
                if (!PeriodicAmountActionIndices.Contains(action.ActionIndex))
                    continue;

                uint amount = action.ActionArg;
                if (AbState.IsDebuff)
                {
                    if (Caster != null)
                        amount = (uint)(amount * (100 + Caster.GetDotDamageBonusPercent(AbState)) / 100);
                    Character.Damage(null, amount);
                }
                else
                    Character.Heal(amount);
            }
        }
    }
}
