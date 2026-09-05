using System;

namespace NextGen.FiestaLib.Data
{
    /// <summary>
    /// Eine einzelne Wirkung eines SubAbState (Staerke-Stufe eines Buffs/Debuffs).
    /// Jeder SubAbState hat bis zu 4 Slots (A-D) aus SubAbState.shn
    /// (ActionIndexA/ActionArgA .. ActionIndexD/ActionArgD). Empirisch verifiziert
    /// gegen die echte NA2016-Client-Datei (siehe DOCUMENTATION.md, Abschnitt 15):
    /// ActionIndex identifiziert die Art der Wirkung, ActionArg deren Staerke.
    ///
    /// WICHTIG: Was ein konkreter ActionIndex-Wert bedeutet, ist grossteils NICHT
    /// verifiziert (111 verschiedene Werte in den Referenzdaten, siehe
    /// BuffActionResolver.cs). Diese Klasse speichert die Rohdaten verlustfrei -
    /// die Interpretation passiert erst in BuffActionResolver, getrennt haltbar
    /// von der reinen Datenrepraesentation.
    /// </summary>
    public sealed class SubAbStateAction
    {
        public uint ActionIndex { get; private set; }
        public uint ActionArg { get; private set; }

        public SubAbStateAction(uint actionIndex, uint actionArg)
        {
            ActionIndex = actionIndex;
            ActionArg = actionArg;
        }
    }
}
