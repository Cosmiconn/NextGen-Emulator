/*File for this file Basic Copyright 2012 no0dl */
using System;
using System.Linq;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Game.Buffs
{
    public sealed class Buff
    {
        private static readonly uint[] PeriodicAmountActionIndices = { 27, 30 };
        private const uint PeriodicIntervalActionIndex = 26;
        private static readonly uint[] StunActionIndices = { 19, 25 };
        private const uint FearActionIndex = 38;
        private const uint SilenceActionIndex = 42;
        private const uint HideEnemyActionIndex = 65;
        public static TimeSpan PeriodicInterval { get; set; } = TimeSpan.FromSeconds(1);
        public MapObject Character { get; private set; }
        public MapObject Caster { get; private set; }
        public AbStateInfo AbState { get; private set; }
        public SubAbstateInfo SubState { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime ExpireTime { get; private set; }
        private DateTime lastPeriodicTick;
        private readonly TimeSpan periodicInterval;

        public Buff(MapObject character, AbStateInfo abState, SubAbstateInfo subState, MapObject caster = null, uint? durationMs = null)
        {
            Character = character;
            Caster = caster;
            AbState = abState;
            SubState = subState;
            StartTime = DateTime.UtcNow;
            ExpireTime = StartTime + (durationMs.HasValue ? TimeSpan.FromMilliseconds(durationMs.Value) : subState.KeepTime);
            lastPeriodicTick = StartTime;
            var intervalAction = subState.Actions.FirstOrDefault(a => a.ActionIndex == PeriodicIntervalActionIndex);
            periodicInterval = intervalAction != null ? TimeSpan.FromMilliseconds(intervalAction.ActionArg) : PeriodicInterval;
        }

        private const uint KnockbackActionIndex = 49;
        private const uint PullActionIndex = 109;

        public void Activate(Buffs owner)
        {
            foreach (var action in SubState.Actions)
            {
                if (StunActionIndices.Contains(action.ActionIndex) || action.ActionIndex == FearActionIndex || action.ActionIndex == SilenceActionIndex || action.ActionIndex == HideEnemyActionIndex) continue;
                if (action.ActionIndex == KnockbackActionIndex) { Character.ForceMove(Caster ?? Character, (int)action.ActionArg, false); continue; }
                if (action.ActionIndex == PullActionIndex) { Character.ForceMove(Caster ?? Character, (int)action.ActionArg, true); continue; }
                if (IsPeriodic(action.ActionIndex)) continue;
                BuffActionResolver.Apply(owner, AbState, action, true);
            }
        }

        public void Deactivate(Buffs owner)
        {
            foreach (var action in SubState.Actions)
            {
                if (StunActionIndices.Contains(action.ActionIndex) || action.ActionIndex == FearActionIndex || action.ActionIndex == SilenceActionIndex || action.ActionIndex == HideEnemyActionIndex) continue;
                if (action.ActionIndex == KnockbackActionIndex || action.ActionIndex == PullActionIndex) continue;
                if (IsPeriodic(action.ActionIndex)) continue;
                BuffActionResolver.Apply(owner, AbState, action, false);
            }
        }

        public bool IsStun { get { return SubState.Actions.Any(a => StunActionIndices.Contains(a.ActionIndex)); } }
        public bool IsSilence { get { return SubState.Actions.Any(a => a.ActionIndex == SilenceActionIndex); } }
        public bool IsFear { get { return SubState.Actions.Any(a => a.ActionIndex == FearActionIndex); } }
        public bool IsInvisible { get { return SubState.Actions.Any(a => a.ActionIndex == HideEnemyActionIndex); } }
        private static bool IsPeriodic(uint actionIndex) { return actionIndex == PeriodicIntervalActionIndex || PeriodicAmountActionIndices.Contains(actionIndex); }

        public void TickPeriodic(DateTime now)
        {
            if (now - lastPeriodicTick < periodicInterval) return;
            lastPeriodicTick = now;
            foreach (var action in SubState.Actions)
            {
                if (!PeriodicAmountActionIndices.Contains(action.ActionIndex)) continue;
                uint amount = action.ActionArg;
                if (AbState.IsDebuff)
                {
                    if (Caster != null) amount = (uint)(amount * (100 + Caster.GetDotDamageBonusPercent(AbState)) / 100);
                    Character.Damage(null, amount);
                }
                else Character.Heal(amount);
            }
        }
    }
}
