using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Definition eines Buffs/Debuffs (AbState), geladen aus data_abstate
    /// (fiesta_data, real per NA2016-Client-.shn exportiert - siehe
    /// sql/data/data_abstate.sql). Alle Spalten empirisch verifiziert, siehe
    /// DOCUMENTATION.md Abschnitt 15.
    ///
    /// Ein AbState hat mehrere SubAbState-Eintraege (Staerke-/Level-Stufen,
    /// z.B. "SeverBone Stufe 1/2/3"), verknuepft ueber den Spaltenwert
    /// `SubAbState` (String, verweist auf SubAbstateInfo.InxName). Die
    /// Verknuepfung passiert beim Laden in DataProvider (siehe LoadAbStates),
    /// nicht in dieser Klasse selbst, da sie zwei separate Tabellen
    /// zusammenfuehrt.
    /// </summary>
    public sealed class AbStateInfo
    {
        public ushort ID { get; set; }
        public string InxName { get; set; }

        // KeepTimeRatio/KeepTimePower: Skalierungsfaktoren fuer die Dauer,
        // vermutlich abhaengig vom Skill-/Charakterlevel des Verursachers.
        // Genaue Formel nicht verifiziert - Rohwerte werden trotzdem
        // gespeichert, um spaetere Interpretation nicht durch Datenverlust
        // beim Laden zu verhindern.
        public uint KeepTimeRatio { get; set; }
        public byte KeepTimePower { get; set; }
        public byte StateGrade { get; set; }

        public uint DispelIndex { get; set; }
        public uint SubDispelIndex { get; set; }
        public uint AbStateSaveType { get; set; }
        public byte Duplicate { get; set; }

        // Aus data_abstateview (AbStateView.shn, Spalte IconSort) - "BUFF"
        // oder "DEBUFF" fuer 776 von 777 AbStates, empirisch verifiziert
        // (siehe DOCUMENTATION.md Abschnitt 19). Bestimmt, ob dieser AbState
        // auf den Anwender (Buff) oder das Angriffsziel (Debuff) angewendet
        // wird, UND das Vorzeichen der Stat-Wirkung in BuffActionResolver.
        // Default true (Buff) falls kein Eintrag gefunden wird (sicherer
        // Fallback: eher zu wenig als faelschlich negativ wirken).
        public bool IsBuff { get; set; } = true;
        public bool IsDebuff { get { return !IsBuff; } }

        // Nur fuer Diagnose/Logging - Klartextbeschreibung aus AbStateView.shn.
        public string Description { get; set; }

        // Schluessel zur Verknuepfung mit SubAbstateInfo.InxName - siehe
        // Klassenkommentar. Nach dem Laden ist SubAbStates befuellt,
        // SubAbStateLinkName wird danach nicht mehr gebraucht.
        public string SubAbStateLinkName { get; private set; }

        public IReadOnlyDictionary<uint, SubAbstateInfo> SubAbStates { get; set; }

        public static AbStateInfo LoadFromDatabase(DataRow row)
        {
            AbStateInfo info = new AbStateInfo
            {
                ID = GetDataTypes.GetUshort(row["ID"]),
                InxName = (string)row["InxName"],
                KeepTimeRatio = GetDataTypes.GetUint(row["KeepTimeRatio"]),
                KeepTimePower = GetDataTypes.GetByte(row["KeepTimePower"]),
                StateGrade = GetDataTypes.GetByte(row["StateGrade"]),
                DispelIndex = GetDataTypes.GetUint(row["DispelIndex"]),
                SubDispelIndex = GetDataTypes.GetUint(row["SubDispelIndex"]),
                AbStateSaveType = GetDataTypes.GetUint(row["AbStateSaveType"]),
                Duplicate = GetDataTypes.GetByte(row["Duplicate"]),
                SubAbStateLinkName = (string)row["SubAbState"],
                SubAbStates = new Dictionary<uint, SubAbstateInfo>(),
            };
            return info;
        }
    }
}
