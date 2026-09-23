# Quest NPC association — native binary and corpus audit

## Native association rule

Original `Zone.exe` routine at `0x0062FB50` determines whether a quest is
associated with a requested NPC.

It returns a match when either:

1. `Start.bNPC != 0` and `Start.NPCID == requested NPCID`; or
2. one of the five end `NPCMobList` entries has `bNPCMob != 0`, matching
   `NPCMobID`, and `NPCMobAction` is **0 or 3**.

Therefore an NPC quest list is not equivalent to the quest's start-NPC field.
Turn-in/action NPC associations are part of the original candidate set.

## Supplied corpus impact

Direct enumeration of the supplied NA2016 QuestData source finds:

- about 2.29k active end-NPC action-0/3 associations across the corpus;
- **581** end-NPC associations whose NPC differs from the quest's start NPC;
- **575** of those differing associations have a FinishScript with a first SAY;
- the supplied corpus has no active action-3 entries, so its observed end-NPC
  association set is effectively action 0.

This is a material runtime path, not an edge case.

## Runtime implementation

`QuestNpcStartResolver` now builds its NPC candidate mapping from the SQL union
of:

- enabled nonzero `QuestData_ConditionStart.NPCID`; and
- enabled nonzero `QuestData_NPCMob.NPCMobID` rows with action 0 or 3.

The same Candidate object is shared across all NPC associations for a quest, so
effective status and script-stage selection remain quest-centric.

The start-dialog join is a LEFT JOIN because an end-NPC-only candidate may not
need a start dialog for the currently effective state; Action/Finish dialogs are
derived from the corresponding scripts.
