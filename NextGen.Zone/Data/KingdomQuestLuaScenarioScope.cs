using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public sealed class KingdomQuestLuaScenarioSource
    {
        public string ScriptLanguage { get; private set; }
        public string RelativePath { get; private set; }
        public string Sha256 { get; private set; }

        internal KingdomQuestLuaScenarioSource(
            string scriptLanguage,
            string relativePath,
            string sha256)
        {
            ScriptLanguage = scriptLanguage;
            RelativePath = relativePath;
            Sha256 = sha256;
        }
    }

    /// <summary>
    /// Exact Lua ScenarioBook scope referenced by the supplied KingdomQuest
    /// definitions.
    ///
    /// ScenarioBookShelf contains more Lua books, but only these 18 keys are
    /// referenced by the current KingdomQuest.shn corpus. The paths and SHA-256
    /// values come from the checked source-provenance snapshot. This class
    /// intentionally defines no Lua API semantics and executes no script.
    /// </summary>
    public static class KingdomQuestLuaScenarioScope
    {
        public const int UsedLuaScriptLanguageCount = 18;

        private static readonly Dictionary<string, KingdomQuestLuaScenarioSource>
            Used =
            new Dictionary<string, KingdomQuestLuaScenarioSource>(
                StringComparer.Ordinal)
            {
                { "KQ/AntiHenis/AntiHenis",
                    Source("KQ/AntiHenis/AntiHenis",
                        "LuaScript/KQ/AntiHenis/AntiHenis.lua",
                        "4daac87c87db43ff9b497898882ea6b3e39c3874816a1528b763561366c3e96e") },
                { "KQ/EmperorSlime/EmperorSlime",
                    Source("KQ/EmperorSlime/EmperorSlime",
                        "LuaScript/KQ/EmperorSlime/EmperorSlime.lua",
                        "a681745ced8a528e1b0b7bbafe3501edaced726195e4f721087ef0833cb92975") },
                { "KQ/GoldHill/GoldHill",
                    Source("KQ/GoldHill/GoldHill",
                        "LuaScript/KQ/GoldHill/GoldHill.lua",
                        "c9b8a01fe34c369f5ecf2bc31b87390b6d701031f1a7baf52d38411690b00d8e") },
                { "KQ/HMiniDragon/HMiniDragon",
                    Source("KQ/HMiniDragon/HMiniDragon",
                        "LuaScript/KQ/HMiniDragon/HMiniDragon.lua",
                        "fc739c0ae4de6bbcdfc9b3ee3c920b5d2648edc3de4304c04bc7577231e1c478") },
                { "KQ/KDArena/KDArena1",
                    Source("KQ/KDArena/KDArena1",
                        "LuaScript/KQ/KDArena/KDArena1.lua",
                        "e9b5f1874e345d31c5ff189fc9cf38b24ab9fad6e6f299956c85a86ca27efc19") },
                { "KQ/KDArena/KDArena2",
                    Source("KQ/KDArena/KDArena2",
                        "LuaScript/KQ/KDArena/KDArena2.lua",
                        "c12b165b062919a08e11eec95797d2400f6cda58c1293e8e021a31fd9a30b564") },
                { "KQ/KDArena/KDArena3",
                    Source("KQ/KDArena/KDArena3",
                        "LuaScript/KQ/KDArena/KDArena3.lua",
                        "d4531585d982b0e9bd7729ff7ce0a187ac19f89a775e8594e9a09a90354faaa9") },
                { "KQ/KDArena/KDArena4",
                    Source("KQ/KDArena/KDArena4",
                        "LuaScript/KQ/KDArena/KDArena4.lua",
                        "517903e74f6add4a6c36a862abc68fa912d817f7f57804c66d7f60ec459bbb59") },
                { "KQ/KDArena/KDArena5",
                    Source("KQ/KDArena/KDArena5",
                        "LuaScript/KQ/KDArena/KDArena5.lua",
                        "116260a4fb727a2852ac2edfcd0802214fbc37adad46e883027d2960ffa6945c") },
                { "KQ/KDArena/KDArena6",
                    Source("KQ/KDArena/KDArena6",
                        "LuaScript/KQ/KDArena/KDArena6.lua",
                        "4f9950a390eb2abd850398c1b99189d5b4739dd84283a8cc34ba094b1fbaedf1") },
                { "KQ/KDFargels/KDFargels",
                    Source("KQ/KDFargels/KDFargels",
                        "LuaScript/KQ/KDFargels/KDFargels.lua",
                        "79cd7549d49d4d7ed8eb61df4e45ae115c0ababffbaae166bfaf0f05d25f09ab") },
                { "KQ/KDMine/KDMine",
                    Source("KQ/KDMine/KDMine",
                        "LuaScript/KQ/KDMine/KDMine.lua",
                        "fc02a774f3d7ab7455b9809fe0ca96e380760cab851e2febc4794aef122b56b0") },
                { "KQ/KDSpring/KDSpring",
                    Source("KQ/KDSpring/KDSpring",
                        "LuaScript/KQ/KDSpring/KDSpring.lua",
                        "8e3b691944022e04ec05f31d4ffca52b999b8b30f61ce5e4faa1798205849f24") },
                { "KQ/KingSlime/KingSlime",
                    Source("KQ/KingSlime/KingSlime",
                        "LuaScript/KQ/KingSlime/KingSlime.lua",
                        "a31a590c78d68a33f8b47d66da194af23d3dfcb1c8d2d72821e510413eab39c5") },
                { "KQ/Kingkong/Kingkong",
                    Source("KQ/Kingkong/Kingkong",
                        "LuaScript/KQ/Kingkong/Kingkong.lua",
                        "a6026eac4bc615ffdcb5af3c97b8614d811e3d439e76b13537d24443361ceea1") },
                { "KQ/LegendOfBijou/LegendOfBijou",
                    Source("KQ/LegendOfBijou/LegendOfBijou",
                        "LuaScript/KQ/LegendOfBijou/LegendOfBijou.lua",
                        "0c68dc9ef4752f2a9df182b651c7f4bd36500a44732e75a76c9fefd4473da0a7") },
                { "KQ/MaraPirate/MaraPirate",
                    Source("KQ/MaraPirate/MaraPirate",
                        "LuaScript/KQ/MaraPirate/MaraPirate.lua",
                        "a2a7c7092547037a9341d2e8a14ae34f1cbc154589c462c50169af311bb3a5e0") },
                { "KQ/MiniDragon/MiniDragon",
                    Source("KQ/MiniDragon/MiniDragon",
                        "LuaScript/KQ/MiniDragon/MiniDragon.lua",
                        "b6c9e8f32d9b3adf26bc28ed506d2cebef7e7159e3578ebd75e153ecb1ba8554") },
            };

        public static bool TryGet(
            string scriptLanguage,
            out KingdomQuestLuaScenarioSource source)
        {
            source = null;
            return !string.IsNullOrEmpty(scriptLanguage) &&
                Used.TryGetValue(scriptLanguage, out source);
        }

        public static IReadOnlyDictionary<string, KingdomQuestLuaScenarioSource>
            Snapshot()
        {
            return new Dictionary<string, KingdomQuestLuaScenarioSource>(
                Used, StringComparer.Ordinal);
        }

        private static KingdomQuestLuaScenarioSource Source(
            string key,
            string path,
            string sha256)
        {
            return new KingdomQuestLuaScenarioSource(key, path, sha256);
        }
    }

    /// <summary>
    /// Explicit execution boundary for a source-verified KQ Lua ScenarioBook.
    /// Implementations must provide the original Lua API registration/runtime;
    /// this interface does not substitute the community scripting API or infer
    /// a script entry function from ScriptInitValue.
    /// </summary>
    public interface IKingdomQuestLuaScenarioHost
    {
        bool TryStart(
            KingdomQuestLuaScenarioSource source,
            string scriptInitValue);
    }
}
