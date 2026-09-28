using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestScenarioStartAction : byte
    {
        DropCurrentFilm = 1,
        CloseAllDoors = 2,
        PlayFilm = 3,
    }

    /// <summary>
    /// Exact mutation-free projection of
    /// KingdomQuest::KQElement::kqe_QuestStart.
    ///
    /// For each populated native KQ map slot, Zone obtains the stored
    /// ScriptLanguage token, drops the current CinemaComplex film, closes all
    /// map doors, then calls cc_PlayFilm with the stored ScriptLanguage and
    /// ScriptInitValue tokens.
    ///
    /// Direct Zone.exe recovery now closes the Pine interpretation without
    /// changing this backend-neutral start envelope: Pine
    /// PineEventScriptNode::Script::sa_Step enters literal block "main", while
    /// Theater::t_PlayFilm pushes VariableStack token "InitFlag" and copies the
    /// complete 0x100-byte ScriptInitValue token into it before script stepping.
    /// Lua keeps ScriptInitValue behind its separate native host boundary.
    /// </summary>
    public sealed class KingdomQuestScenarioStartPlan
    {
        private static readonly KingdomQuestScenarioStartAction[] NativeOrder =
        {
            KingdomQuestScenarioStartAction.DropCurrentFilm,
            KingdomQuestScenarioStartAction.CloseAllDoors,
            KingdomQuestScenarioStartAction.PlayFilm,
        };

        public string ScriptLanguage { get; private set; }
        public string ScriptInitValue { get; private set; }

        private KingdomQuestScenarioStartPlan(
            string scriptLanguage,
            string scriptInitValue)
        {
            ScriptLanguage = scriptLanguage;
            ScriptInitValue = scriptInitValue;
        }

        public IReadOnlyList<KingdomQuestScenarioStartAction> GetNativeOrder()
        {
            return Array.AsReadOnly(NativeOrder);
        }

        public static bool TryBuild(
            KingdomQuestProtocolInfo definition,
            out KingdomQuestScenarioStartPlan plan)
        {
            plan = null;
            if (definition == null ||
                string.IsNullOrEmpty(definition.ScriptLanguage) ||
                definition.ScriptInitValue == null)
                return false;

            if (!KingdomQuestScenarioBookShelfSource.
                    ContainsSourceBackedScenarioBook(
                        definition.ScriptLanguage))
                return false;

            plan = new KingdomQuestScenarioStartPlan(
                definition.ScriptLanguage,
                definition.ScriptInitValue);
            return true;
        }
    }
}
