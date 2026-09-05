using System.Data;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Ein einzelner Quest-Dialogtext (QuestDialog.shn, 25222 Zeilen).
    /// Wird per DialogID aus QuestData.shn-Skripten referenziert (Befehl
    /// "SAY &lt;DialogID&gt; NPC"). Enthaelt Quest-Titel, -Beschreibungen und
    /// NPC-Gespraechstext inklusive Markup ([NAME], [LINE], [SHOW_REWARD],
    /// [BUTTON]=[Label][ID], [MENU], {color,...}). Markup-Interpretation
    /// noch nicht umgesetzt - siehe DOCUMENTATION.md Abschnitt 48.
    /// </summary>
    public sealed class QuestDialogInfo
    {
        public uint DialogID { get; private set; }
        public string Text { get; private set; }

        public static QuestDialogInfo Load(DataRow row)
        {
            return new QuestDialogInfo
            {
                DialogID = GetDataTypes.GetUint(row["DialogID"]),
                Text = (string)row["Text"],
            };
        }
    }
}
