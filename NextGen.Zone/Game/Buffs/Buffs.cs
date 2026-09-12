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
        private MapObject Character { get; set; }
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
        public int Aim { get; set; }
        public int CriticalRate { get; set; }
        public int AttackSpeed { get; set; }
        public int MoveSpeed { get; set; }
        public int ExpBonusPercent { get; set; }
        public int BlockRate { get; set; }
        public int PoisonResistance { get; set; }
        public int DiseaseResistance { get; set; }
        public int CurseResistance { get; set; }
        public int DotDamageBonusPercent { get; set; }
        public int PoisonDamageBonusPercent { get; set; }
        public int BloodingDamageBonusPercent { get; set; }
        public int ReflectDamagePercent { get; set; }
        public int MissRatePercent { get; set; }
        private int shieldAmount;
        public int ShieldAmount { get { return shieldAmount; } set { shieldAmount = Math.Max(0, value); } }
        public int MinHP { get; set; }
        public int ReviveHealRatePermille { get; set; }
        public int DropRatePercent { get; set; }
        public int CastingTimeBonusPercent { get; set; }
        public int IgnoreMagicDamagePercent { get; set; }
        public int IgnorePhysicalDamagePercent { get; set; }
        public int SPRegenRatePercent { get; set; }
        public int PartyDeathHealPermille { get; set; }
        private List<Buff> CurrentBuffs { get; set; }

        public Buffs(MapObject pChar) { Character = pChar; CurrentBuffs = new List<Buff>(); }

        public void AddBuff(AbStateInfo abState, uint strength, MapObject caster = null)
        {
            AddBuff(abState, strength, caster, null);
        }

        public void AddBuff(AbStateInfo abState, uint strength, MapObject caster, uint? durationMs)
        {
            if (abState == null) return;
            if (strength < 1) strength = 1;
            if (strength > 40) strength = 40;
            if (!abState.SubAbStates.TryGetValue(strength, out var subState))
            {
                Log.WriteLine(LogLevel.Warn, "AddBuff: AbState '{0}' hat keine Staerke-Stufe {1}.", abState.InxName, strength);
                return;
            }
            lock (CurrentBuffs)
            {
                var existing = CurrentBuffs.FirstOrDefault(b => b.AbState.ID == abState.ID);
                if (existing != null) { existing.Deactivate(this); CurrentBuffs.Remove(existing); }
                var buff = new Buff(Character, abState, subState, caster, durationMs);
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

        public void Tick(DateTime now)
        {
            List<Buff> expired;
            lock (CurrentBuffs)
            {
                expired = CurrentBuffs.Where(b => b.ExpireTime <= now).ToList();
                foreach (var buff in expired) { buff.Deactivate(this); CurrentBuffs.Remove(buff); }
                foreach (var buff in CurrentBuffs) buff.TickPeriodic(now);
            }
        }

        public IEnumerable<Buff> ActiveBuffs { get { lock (CurrentBuffs) { return CurrentBuffs.ToList(); } } }

        public void AddPassiveSkill(PassiveSkillInfo skill)
        {
            MaxSP += (int)skill.MaxSP; Int += (int)skill.Intel; WeaponDamage += (int)skill.WCRateUp; MagicDamage += (int)skill.MARateUp; CriticalRate += skill.MACriRate;
        }
        public void RemovePassiveSkill(PassiveSkillInfo skill)
        {
            MaxSP -= (int)skill.MaxSP; Int -= (int)skill.Intel; WeaponDamage -= (int)skill.WCRateUp; MagicDamage -= (int)skill.MARateUp; CriticalRate -= skill.MACriRate;
        }
        public bool IsStunned { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsStun); } } }
        public bool IsFeared { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsFear); } } }
        public bool IsSilenced { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsSilence); } } }
        public bool IsInvisible { get { lock (CurrentBuffs) { return CurrentBuffs.Any(b => b.IsInvisible); } } }
    }
}
