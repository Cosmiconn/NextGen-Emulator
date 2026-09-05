using System.Data;
using System.Collections.Generic;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    public sealed class ActiveSkillInfo
    {
        public ushort ID { get; private set; }
        public string Name { get; private set; }
        public byte Step { get; private set; }
        public string Required { get; private set; }
        public ushort SP { get; private set; }
        public ushort HP { get; private set; }
        public ushort Range { get; private set; }
        public uint CoolTime { get; private set; }
        public uint CastTime { get; private set; }
        public ushort SkillAniTime { get; set; }
        public uint MinDamage { get; private set; }
        public uint MaxDamage { get; private set; }
        public bool IsMagic { get; private set; }
        public byte DemandType { get; private set; }
        public byte MaxTargets { get; private set; }

        // Bis zu 4 AbState-Slots (StaNameA-D/StaStrengthA-D/StaSucRateA-D aus
        // ActiveSkill.shn, 96 Spalten insgesamt) - Skills koennen beim
        // Einsatz Buffs/Debuffs anwenden, zusaetzlich zu oder statt Schaden/
        // Heilung. SuccessRate vermutlich Promille oder Prozent (nicht
        // verifiziert, siehe DOCUMENTATION.md) - beim Anwenden defensiv als
        // "von 100" behandelt.
        public IReadOnlyList<SkillAbStateSlot> AbStateSlots { get; private set; }

        public static ActiveSkillInfo Load(DataRow row)
        {
            ActiveSkillInfo inf = new ActiveSkillInfo
            {
                           
                ID = GetDataTypes.GetUshort(row["ID"]),
                Name = (string)row["InxName"],
                Step = GetDataTypes.GetByte(row["Step"]),
                Required = (string)row["DemandSk"],
                SP = GetDataTypes.GetUshort(row["SP"]),
                HP = GetDataTypes.GetUshort(row["HP"]),
                Range = GetDataTypes.GetUshort(row["Range"]),
                CoolTime = GetDataTypes.GetUint(row["DlyTime"]),
                CastTime = GetDataTypes.GetUint(row["CastTime"]),
                DemandType = GetDataTypes.GetByte(row["DemandType"]),
                MaxTargets = GetDataTypes.GetByte(row["TargetNumber"]),
            };

            uint maxdamage =  GetDataTypes.GetUint(row["MaxWC"]);
            if (maxdamage == 0)
            {
                inf.IsMagic = true;
                inf.MinDamage =  GetDataTypes.GetUshort(row["MinMA"]);
                inf.MaxDamage =  GetDataTypes.GetUshort(row["MaxMA"]);
            }
            else
            {
                inf.MaxDamage = maxdamage;
                inf.MinDamage =  GetDataTypes.GetUint(row["MinWC"]);
            }

            var slots = new List<SkillAbStateSlot>();
            foreach (var letter in new[] { "A", "B", "C", "D" })
            {
                string name = (string)row["StaName" + letter];
                if (string.IsNullOrEmpty(name) || name == "-")
                    continue;
                slots.Add(new SkillAbStateSlot(
                    name,
                    GetDataTypes.GetUint(row["StaStrength" + letter]),
                    GetDataTypes.GetUint(row["StaSucRate" + letter])));
            }
            inf.AbStateSlots = slots.AsReadOnly();

            return inf;
        }
    }

    /// <summary>
    /// Ein AbState-Slot eines Skills (StaNameX/StaStrengthX/StaSucRateX).
    /// AbStateName verweist auf AbStateInfo.InxName, analog zur Verknuepfung
    /// AbState -> SubAbState (siehe DOCUMENTATION.md Abschnitt 15).
    /// </summary>
    public sealed class SkillAbStateSlot
    {
        public string AbStateName { get; private set; }
        public uint Strength { get; private set; }
        public uint SuccessRate { get; private set; }

        public SkillAbStateSlot(string abStateName, uint strength, uint successRate)
        {
            AbStateName = abStateName;
            Strength = strength;
            SuccessRate = successRate;
        }
    }
}
