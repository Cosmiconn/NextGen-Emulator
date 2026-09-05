using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Eine einzelne Staerke-Stufe ("Strength") eines AbState, geladen aus
    /// data_subabstate (fiesta_data, real per NA2016-Client-.shn exportiert -
    /// siehe sql/data/data_subabstate.sql). Alle Spalten empirisch verifiziert,
    /// siehe DOCUMENTATION.md Abschnitt 15.
    /// </summary>
    public sealed class SubAbstateInfo
    {
        public ushort ID { get; private set; }
        public string InxName { get; private set; }
        public uint Strength { get; private set; }
        public uint Type { get; private set; }
        public byte SubType { get; private set; }
        // KeepTime steht im .shn in Millisekunden (empirisch: 20000 = 20s bei
        // typischen kurzen Debuffs, konsistent mit einer ms-Interpretation).
        public TimeSpan KeepTime { get; private set; }

        public IReadOnlyList<SubAbStateAction> Actions { get; private set; }

        public static SubAbstateInfo LoadFromDatabase(DataRow row)
        {
            var actions = new List<SubAbStateAction>();
            foreach (var slot in new[] { "A", "B", "C", "D" })
            {
                uint idx = GetDataTypes.GetUint(row["ActionIndex" + slot]);
                uint arg = GetDataTypes.GetUint(row["ActionArg" + slot]);
                if (idx != 0)
                    actions.Add(new SubAbStateAction(idx, arg));
            }

            return new SubAbstateInfo
            {
                ID = GetDataTypes.GetUshort(row["ID"]),
                InxName = (string)row["InxName"],
                Strength = GetDataTypes.GetUint(row["Strength"]),
                Type = GetDataTypes.GetUint(row["Type"]),
                SubType = GetDataTypes.GetByte(row["SubType"]),
                KeepTime = TimeSpan.FromMilliseconds(GetDataTypes.GetUint(row["KeepTime"])),
                Actions = actions.AsReadOnly(),
            };
        }
    }
}
