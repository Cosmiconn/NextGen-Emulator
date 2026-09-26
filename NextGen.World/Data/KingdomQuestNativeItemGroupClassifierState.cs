using System;
using System.Collections.Generic;
using NextGen.FiestaLib.Data;

namespace NextGen.World.Data
{
    /// <summary>
    /// Explicit state for the MSVC CRT rand()/srand() algorithm linked into the
    /// supplied Zone.exe. No seed is invented here; callers supply native state.
    /// </summary>
    public sealed class KingdomQuestMsvcCrtRand
    {
        private readonly MsvcCrtRand shared;

        public uint State
        {
            get { return shared.State; }
        }

        public KingdomQuestMsvcCrtRand(uint state)
        {
            shared = new MsvcCrtRand(state);
        }

        public ushort Next()
        {
            return shared.Next();
        }

        internal void ConsumeShuffle()
        {
            shared.Consume(2);
        }
    }

    public enum KingdomQuestItemGroupLookupKind : byte
    {
        Selected = 1,
        NativeNoCompatibleCandidate = 2,
        SourceIncomplete = 3,
    }

    /// <summary>
    /// State model for only KQ-relevant decks inside native
    /// ItemGroupClassifier.
    ///
    /// Native igc_Load performs 5,758 valid igc_Store calls. Each inserts at
    /// CardStack top and immediately cs_Suffle(1), consuming exactly two CRT
    /// rand() values. Candidate rows carry the global store ordinal so RNG use
    /// by non-KQ groups is preserved without fabricating their card contents.
    ///
    /// This is deliberately not a live reward RNG owner: native CRT rand is
    /// shared with unrelated Zone code, so authoritative load-start and
    /// reward-time states remain external inputs.
    /// </summary>
    public sealed class KingdomQuestNativeItemGroupClassifierState
    {
        private sealed class Deck
        {
            private readonly List<ushort> cards = new List<ushort>();

            public int Count
            {
                get { return cards.Count; }
            }

            public void InsertTop(ushort itemId)
            {
                cards.Insert(0, itemId);
            }

            public void ShuffleOnce(KingdomQuestMsvcCrtRand random)
            {
                if (random == null)
                    throw new ArgumentNullException("random");
                if (cards.Count == 0)
                    throw new InvalidOperationException(
                        "Native CardStack shuffle requires at least one card.");

                int first = random.Next() % cards.Count;
                int second = random.Next() % cards.Count;
                ushort value = cards[first];
                cards[first] = cards[second];
                cards[second] = value;
            }

            public ushort RotateTopToBottom()
            {
                if (cards.Count == 0)
                    throw new InvalidOperationException("CardStack is empty.");

                ushort value = cards[0];
                cards.RemoveAt(0);
                cards.Add(value);
                return value;
            }
        }

        private readonly Dictionary<string, Deck> decks;
        private readonly Dictionary<ushort, uint> useClassByItemId;

        public uint RandomStateAfterNativeLoad { get; private set; }

        private KingdomQuestNativeItemGroupClassifierState(
            Dictionary<string, Deck> decks,
            Dictionary<ushort, uint> useClassByItemId,
            uint randomStateAfterNativeLoad)
        {
            this.decks = decks;
            this.useClassByItemId = useClassByItemId;
            RandomStateAfterNativeLoad = randomStateAfterNativeLoad;
        }

        public static bool TryCreate(
            uint randomStateAtLoadStart,
            out KingdomQuestNativeItemGroupClassifierState state)
        {
            state = null;
            IReadOnlyList<KingdomQuestRewardItemGroupCandidateSourceRow> rows =
                KingdomQuestRewardItemGroupCandidateSource.Snapshot();

            if (rows == null ||
                rows.Count !=
                    KingdomQuestRewardItemGroupCandidateSource.KqCandidateRows)
                return false;

            var random = new KingdomQuestMsvcCrtRand(randomStateAtLoadStart);
            var decks = new Dictionary<string, Deck>(StringComparer.Ordinal);
            var useClassByItemId = new Dictionary<ushort, uint>();
            int nativeStoreOrdinal = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                KingdomQuestRewardItemGroupCandidateSourceRow row = rows[i];
                if (row == null ||
                    row.NativeShuffleOrdinal < nativeStoreOrdinal ||
                    row.NativeShuffleOrdinal >=
                        KingdomQuestRewardItemGroupCandidateSource.NativeValidStoreCalls ||
                    string.IsNullOrEmpty(row.GroupName))
                    return false;

                while (nativeStoreOrdinal < row.NativeShuffleOrdinal)
                {
                    random.ConsumeShuffle();
                    nativeStoreOrdinal++;
                }

                uint priorUseClass;
                if (useClassByItemId.TryGetValue(row.ItemId, out priorUseClass))
                {
                    if (priorUseClass != row.UseClass)
                        return false;
                }
                else
                {
                    useClassByItemId[row.ItemId] = row.UseClass;
                }

                Deck deck;
                if (!decks.TryGetValue(row.GroupName, out deck))
                {
                    deck = new Deck();
                    decks[row.GroupName] = deck;
                }

                // ItemGroupClassifier::igc_Store:
                // cs_InsertTop(itemId) -> cs_Suffle(1).
                deck.InsertTop(row.ItemId);
                deck.ShuffleOnce(random);
                nativeStoreOrdinal++;
            }

            while (nativeStoreOrdinal <
                    KingdomQuestRewardItemGroupCandidateSource.NativeValidStoreCalls)
            {
                random.ConsumeShuffle();
                nativeStoreOrdinal++;
            }

            state = new KingdomQuestNativeItemGroupClassifierState(
                decks, useClassByItemId, random.State);
            return true;
        }

        /// <summary>
        /// Reproduces the group branch of ItemGroupClassifier::igc_Getitem.
        /// Direct ItemDataBox lookup has already won before this branch.
        ///
        /// Native group lookup calls cs_Suffle(1), then examines at most the
        /// original card count. Each top card is moved to the bottom before
        /// ccdb_UseClassTypeToBit(ItemInfo.UseClass) is intersected with the
        /// caller class-group mask. If no candidate matches, native returns
        /// 0xFFFF.
        /// </summary>
        public KingdomQuestItemGroupLookupKind Select(
            string groupName,
            uint classGroup,
            IDictionary<uint, long> useClassMasks,
            KingdomQuestMsvcCrtRand random,
            out ushort itemId)
        {
            itemId = ushort.MaxValue;
            if (groupName == null ||
                useClassMasks == null ||
                random == null)
                return KingdomQuestItemGroupLookupKind.SourceIncomplete;

            Deck deck;
            if (!decks.TryGetValue(groupName, out deck) || deck.Count == 0)
                return KingdomQuestItemGroupLookupKind.SourceIncomplete;

            deck.ShuffleOnce(random);
            int cardCount = deck.Count;
            for (int i = 0; i < cardCount; i++)
            {
                ushort candidate = deck.RotateTopToBottom();

                uint useClass;
                long mask;
                if (!useClassByItemId.TryGetValue(candidate, out useClass) ||
                    !useClassMasks.TryGetValue(useClass, out mask))
                    return KingdomQuestItemGroupLookupKind.SourceIncomplete;

                if ((unchecked((ulong)mask) & (ulong)classGroup) != 0)
                {
                    itemId = candidate;
                    return KingdomQuestItemGroupLookupKind.Selected;
                }
            }

            return KingdomQuestItemGroupLookupKind.NativeNoCompatibleCandidate;
        }
    }
}
