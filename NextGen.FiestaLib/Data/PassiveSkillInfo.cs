using System.Data;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Passiver Skill (PassiveSkill.shn, 48 Spalten). Anders als ActiveSkill
    /// wirkt ein passiver Skill nicht ueber AbState-Slots, sondern direkt
    /// ueber benannte Stat-Spalten (WeaponMastery-Raten pro Waffentyp,
    /// Intel, MaxSP, WCRateUp/MARateUp, ...) - siehe DOCUMENTATION.md
    /// Abschnitt 25. Nur die Spalten mit einigermassen klarer, eindeutiger
    /// Semantik werden geladen und angewendet; die vielen waffentyp-
    /// spezifischen Mastery-Felder (MstRtSword1, MstPlAxe2, ...) sind hier
    /// bewusst NICHT abgebildet - dafuer muesste zusaetzlich bekannt sein,
    /// welche Waffe der Charakter aktuell traegt, und die genaue Bedeutung
    /// von "MstRt" (Rate?) vs. "MstPl" (Plus?) ist nicht zweifelsfrei
    /// belegt. Rohdaten werden trotzdem vollstaendig in die Datenbank
    /// exportiert (sql/data/data_passiveskill.sql), falls spaeter genauer
    /// ausgewertet werden soll.
    /// </summary>
    public sealed class PassiveSkillInfo
    {
        public ushort ID { get; private set; }
        public string InxName { get; private set; }
        public string Name { get; private set; }

        // Direkt zuordenbare, permanente Stat-Boni.
        public uint MaxSP { get; private set; }
        public uint Intel { get; private set; }
        // "Up"-Suffix deutet laut Namenskonvention (analog zu den
        // ActionIndex-RATE-Werten, siehe Abschnitt 23) auf einen
        // Prozentsatz hin - wird wie dort als flacher Wert angenaehert
        // (dieselbe RATE-vs-PLUS-Einschraenkung gilt auch hier).
        public uint WCRateUp { get; private set; }
        public uint MARateUp { get; private set; }
        public ushort MACriRate { get; private set; }

        public static PassiveSkillInfo Load(DataRow row)
        {
            return new PassiveSkillInfo
            {
                ID = GetDataTypes.GetUshort(row["ID"]),
                InxName = (string)row["InxName"],
                Name = (string)row["Name"],
                MaxSP = GetDataTypes.GetUint(row["MaxSP"]),
                Intel = GetDataTypes.GetUint(row["Intel"]),
                WCRateUp = GetDataTypes.GetUint(row["WCRateUp"]),
                MARateUp = GetDataTypes.GetUint(row["MARateUp"]),
                MACriRate = GetDataTypes.GetUshort(row["MACriRate"]),
            };
        }
    }
}
