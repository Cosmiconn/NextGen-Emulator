using System.Data;
using NextGen.Database.DataStore;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Eine Titel-Kategorie (CharacterTitleData.shn, 128 Zeilen = CHARACTER_TITLE_TYPE-
    /// Enum-Werte 0-127). Jede Kategorie hat bis zu 4 Stufen (Title0-3), mit
    /// einem Schwellenwert (Value0-3, z.B. Anzahl getoeteter Monster) und
    /// einer Fame-Belohnung (Fame0-3) beim Erreichen. Siehe DOCUMENTATION.md
    /// Abschnitt 41.
    ///
    /// STAND: Nur die Datenstruktur + eine einzelne, konkret angebundene
    /// Kategorie (TOTAL_KILL_MOB) sind umgesetzt. Ein vollstaendiges
    /// Titel-System (alle 127 Kategorien) waere ein eigenstaendiges,
    /// deutlich groesseres Vorhaben - die meisten Kategorien (Gildenkriege,
    /// Auktionshaus, Wuerfelspiele, Haustiere, ...) haben in diesem Projekt
    /// noch keine Datenquelle, aus der sich ein Zaehler speisen liesse.
    /// </summary>
    public sealed class TitleTier
    {
        public string Title { get; private set; }
        public uint Value { get; private set; }
        public uint Fame { get; private set; }
        public TitleTier(string title, uint value, uint fame)
        {
            Title = title;
            Value = value;
            Fame = fame;
        }
    }

    public sealed class TitleCategoryInfo
    {
        public uint Type { get; private set; }
        public TitleTier[] Tiers { get; private set; } // 4 Stufen, aufsteigend

        public static TitleCategoryInfo Load(DataRow row)
        {
            var tiers = new[]
            {
                new TitleTier((string)row["Title0"], GetDataTypes.GetUint(row["Value0"]), GetDataTypes.GetUint(row["Fame0"])),
                new TitleTier((string)row["Title1"], GetDataTypes.GetUint(row["Value1"]), GetDataTypes.GetUint(row["Fame1"])),
                new TitleTier((string)row["Title2"], GetDataTypes.GetUint(row["Value2"]), GetDataTypes.GetUint(row["Fame2"])),
                new TitleTier((string)row["Title3"], GetDataTypes.GetUint(row["Value3"]), GetDataTypes.GetUint(row["Fame3"])),
            };
            return new TitleCategoryInfo
            {
                Type = GetDataTypes.GetUint(row["Type"]),
                Tiers = tiers,
            };
        }
    }
}
