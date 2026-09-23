# Quest block completion gate

## Purpose

This file is the authoritative closure checklist for the SQL-backed NA2016 quest
block. Older reverse-engineering notes intentionally remain in the repository
and can contain historical `UNRESOLVED` statements that were superseded by
later audits. A historical unresolved symbol name is not automatically a runtime
blocker.

The quest block is complete only when every path that is active in the supplied
2304-record QuestData corpus is either implemented from original evidence or
proven irrelevant/unreachable for that corpus. No missing behavior is filled in
from field names or assumptions.

## Proven complete for the supplied corpus

The current branch has direct binary/PDB/corpus support for:

- one-time QuestData import and SQL-only quest runtime; CI rejects reintroduced
  `QuestData.shn` runtime readers;
- all 2304 QuestData records and the exact active textual opcode corpus;
- NPC quest association, effective status, status/type priorities and the exact
  Type-3 equal-priority branch;
- Soonable/Doingable conditions used by the corpus, including class/gender,
  location, item, predecessor and the Type-10 status-2 daily reset subcase;
- Start/Action/Finish routing, LINK operand parsing/routing, SAY/0x4401,
  SAY-ACK/0x4402, explicit END, 0x440F/0x4410 quest selection and Scenario
  run/done wire flow;
- ACCEPT/CANCEL/DONE state mutations and their proven raw QSC failure paths;
- mob-kill progress, scenario progress, item creation/deletion, AbState command
  inputs, rewards and SQL persistence;
- selectable reward slot semantics: 0x4411 carries the absolute 0..11
  QuestData reward slot and 0x4412 requests a missing selection;
- reward types active in the supplied corpus: EXP, money, item and fame;
- `tQuest`, `tQuestTimes` and normalized objective progress persistence.

The script audit currently reports zero unknown textual opcodes.

## Blocking before the quest block can be marked complete

### 1. Reward-selection wake-up after 0x4412

**Status: UNRESOLVED / functional blocker.**

The original 0x4411 receiver is proven to validate the current QuestID and store
the selected DWORD slot only. It does not itself call QuestNext or complete the
quest. QSC_DONE sends 0x4412 when a required selection is missing and leaves
DONE pending.

The exact native event that re-enters that pending DONE after the later 0x4411
selection has not yet been tied to a callsite/capture. The emulator therefore
must not invent "0x4411 completes the quest" behavior.

This is not hypothetical: the supplied reward corpus contains active selectable
reward rows. Either the re-entry event must be reconstructed from the original
client/server path, or authoritative capture/client evidence must prove that the
0x4412 fallback is unreachable in the supplied normal flow.

The adjacent quest protocol also contains `0x4414/0x4415`
(`QUEST_START_REQ/ACK`). A separate PDB-derived protocol-structure export
corroborates a `u16 QuestID` request and `u16 err` acknowledgement, but no
callsite evidence currently connects that pair to the pending-DONE state.
Therefore `0x4414` is **not** treated as the wake-up merely because it is
numerically adjacent to `0x4411/0x4412`.

### 2. Eight source label misses with reachable trigger shapes

**Status: UNRESOLVED / corpus-active edge paths.**

The verified script corpus contains exactly eight references whose target label
does not exist anywhere in the same quest:

- five inventory-full branches:
  quests 85, 108, 229, 416 and 2313 use
  `IF VAR1 < 1 GOTO MARK100`;
- three dialog-result branches:
  quests 60024, 60102 and 60108 use
  `IF RESULT == 2 GOTO MARK2`.

Native CommandRun proves that a failed GOTO lookup returns failure, but the full
caller consequence has not yet been correlated far enough to claim exact
QuestZone behavior. These labels must not be auto-created or redirected.

CI locks the exact quest, stage, target and source command for all eight.

### 3. Quest 15 comma-bearing SAY preprocessing

**Status: UNRESOLVED native preprocessing / narrow active path.**

Exactly five Quest 15 Start lines spell the first SAY operand as
`<DialogID>,`. The binary tokenizer evidence currently documented is
whitespace-only and the numeric validator requires digit-only tokens, so the
native preprocessing step that makes these authoritative source lines usable is
still unknown.

The emulator currently applies only the five source-observed trailing-comma
normalizations. This makes the supplied content functional but is not promoted
to a general native grammar rule. Full closure requires the original
preprocessing/tokenization path to be identified or equivalent authoritative
client/server evidence.

## Not blockers for the supplied 2304-record corpus

These remain valid future-compatibility/reverse-engineering work, but their
source flags/opcodes are absent in the supplied corpus:

- Start race and date gates: zero active rows.
- End race, class, scenario and time-limit gates: zero active rows in the
  supplied snapshot.
- NPC/Mob action 2 and 3 objective event sources: zero active action-2/action-3
  entries; the corpus has 961 active action-1 entries.
- Textual GET_PLAYER_RACE, GET_PLAYER_CLASS, GET_PLAYER_LEVEL,
  GET_PLAYER_GENDER, DROP_ITEM, SET, ADD, SUB and IS_ABSTATE: zero occurrences.
- Reward types 3, 5, 6 and 7: absent from active supplied reward rows.

Future QuestData that enables any of these must not be silently accepted without
the corresponding native implementation.

## Fidelity debt that does not currently block corpus functionality

These are useful follow-up targets, but the current quest outcomes do not depend
on assigning their missing source-level names or reproducing internal plumbing
byte-for-byte:

- exact source/PDB symbol naming for several optimized helper ranges;
- the source-level semantic name of Quest Type 3;
- exact ItemDB transaction/rollback packet internals, provided the proven
  reward-before-completion and failure ordering remains preserved;
- internal AbState timer/callback member naming where the quest-visible command
  inputs and refresh behavior are already mapped;
- old capture narratives that are superseded by later direct binary/wire audits.

## Exit criteria

Before changing this document to **COMPLETE**:

1. close or prove corpus-irrelevant the 0x4412 -> 0x4411 pending-DONE re-entry;
2. establish the native outcome of the eight missing-label branches without
   fabricating labels;
3. establish the native handling of the five Quest 15 comma-bearing SAY lines;
4. keep all quest audits green on both GitHub Actions platforms;
5. rerun the full 2304-record opcode/label/LINK/SAY audit and SQL-only runtime
   guard at the final branch head.

Until then the quest block remains intentionally **UNRESOLVED** rather than
being declared complete on the strength of broad coverage alone.
