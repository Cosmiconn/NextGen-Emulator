# Quest block completion gate

## Status

**COMPLETE for the supplied 2304-record NA2016 QuestData runtime.**

This status means every path that is active in the supplied corpus now has a
deterministic emulator behavior, the exact corpus is CI-locked, and no missing
source label/opcode is silently invented. It is **not** a claim that every
internal Zone/GameDB/ItemDB implementation detail is byte-for-byte identical to
the original server.

## Proven native/corpus coverage

The completed block includes:

- one-time QuestData import and SQL-only Zone runtime, with CI rejecting a
  reintroduced runtime dependency on QuestData.shn;
- all 2304 QuestData records and the exact active textual opcode corpus;
- NPC association, effective status, status/type priority, the Type-3 branch,
  Soonable/Doingable eligibility and Type-10 daily prerequisite reset logic;
- Start/Action/Finish routing, unique cross-stage labels, LINK, SAY/0x4401,
  SAY ACK/0x4402, explicit END, 0x440F/0x4410 selection and Scenario flow;
- ACCEPT, CANCEL and stage-independent DONE, including proven QSC errors;
- mob-kill/scenario progress, item operations, SET_ABSTATE, rewards and durable
  SQL quest/objective/completion state;
- selectable reward slots using the native absolute 0..11 QuestData slot;
- active reward types 0, 1, 2 and 4, plus the existing type-8
  forward-compatible path.

The mandatory script audit reports zero unknown textual opcodes.

## Selectable-reward recovery closure

Native evidence remains exact up to this boundary:

1. 0x4411 validates the current QuestID and stores the selected DWORD slot.
2. QSC_DONE validates rewardability and the stored slot.
3. missing/invalid selection sends 0x4412 and leaves DONE pending.
4. the original 0x4411 receiver itself does not call QuestNext or complete.

The separate later native event that wakes an already-pending DONE could not be
attributed from the available original callsites/captures. To make the supplied
runtime complete without reintroducing the disproven "every 0x4411 completes"
behavior, Handler17 uses a deliberately narrow compatibility closure:

- ordinary 0x4411 before DONE is **store-only**;
- only if this emulator has already sent 0x4412 and marked DONE pending does a
  later valid 0x4411 consume that pending selection, perform the same completion
  operation and resume the existing script machine;
- an invalid selection leaves DONE pending and reissues 0x4412.

CI statically guards that the completion call remains behind the pending-DONE
gate. This compatibility behavior is not presented as the unidentified native
wake-up event.

The adjacent 0x4414/0x4415 QUEST_START_REQ/ACK pair is not used as a guessed
wake-up. PDB-derived protocol structures prove its packet shape only.
Likewise, 0x441A/0x441B QUEST_DB_DONE_REQ/ACK are completion/database
infrastructure carrying QuestInfo/QSC/reward state; their presence does not
prove a client reward-selection wake-up.

## Source anomalies closed without fabrication

### Eight missing labels

The authoritative corpus contains exactly eight references with no target label
anywhere in the same quest:

- inventory-full MARK100 branches: quests 85, 108, 229, 416 and 2313;
- RESULT==2 MARK2 branches: quests 60024, 60102 and 60108.

Native CommandRun proves that an unresolved GOTO lookup fails. The corpus itself
provides no valid destination to execute. The emulator therefore treats these
as source-control-flow errors: it logs the exact quest/stage/line and terminates
the local script session without mutating quest state or fabricating a label.
CI locks all eight quest/stage/target/source-command tuples.

The exact original caller-side presentation after CommandRun returns failure is
still a fidelity question, but it is no longer a runtime ambiguity: the
emulator has a deterministic, non-mutating failure boundary.

### Quest 15 comma-bearing SAY source

Exactly five authoritative Quest 15 Start lines use `SAY <DialogID>, ...`.
The exact original preprocessing step before the whitespace tokenizer remains
unidentified. The emulator normalizes a trailing comma **only at the SAY
DialogID token boundary and only for those five complete source lines**. CI
locks the five tuples and statically rejects a reintroduced generic
`TrimEnd(',')` parser. Any other comma-bearing SAY is rejected rather than
silently expanding the grammar.

This closes the emulator-side source-normalization boundary. The unidentified
original preprocessing routine is retained only as an archival native-fidelity
question, not as unfinished Quest runtime behavior.

## Future-compatibility work, not NA2016 blockers

The supplied corpus does not activate:

- Start race/date gates;
- End race/class/scenario/time-limit gates in the supplied snapshot;
- NPC/Mob objective actions 2/3 (the corpus has 961 active action-1 entries);
- textual GET_PLAYER_RACE, GET_PLAYER_CLASS, GET_PLAYER_LEVEL,
  GET_PLAYER_GENDER, DROP_ITEM, SET, ADD, SUB or IS_ABSTATE;
- reward types 3, 5, 6 or 7.

Future QuestData enabling these must still fail conservatively until native
semantics are implemented.

## Remaining fidelity debt

These items do not prevent the supplied Quest block from running completely:

- identification of the original post-0x4412 wake-up event;
- exact caller/UI consequence of native missing-label CommandRun failure;
- exact internal identity of the native preprocessing routine behind the five
  Quest 15 comma lines (emulator behavior itself is source-exact and CI-locked);
- exact ItemDB/GameDB transactional plumbing and rollback packet internals;
- source-level names for a few optimized helpers and internal AbState details.

They remain documented as evidence boundaries and must not be silently promoted
to native-equivalent behavior.

## Completion invariant

The Quest block remains COMPLETE only while all of the following stay green:

1. full 2304-record corpus/provenance audit;
2. exact opcode, IF, DONE, ACCEPT, SAY, LINK and missing-label invariants;
3. selectable-reward pending-DONE recovery guard;
4. QuestData importer normalized-field audit;
5. SQL-only runtime audit;
6. solution build on both GitHub Actions platforms.

Any future corpus change that activates a currently absent command/condition
reopens this gate.
