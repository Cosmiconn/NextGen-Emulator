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

- **2290** enabled nonzero end-NPC action-0/3 associations across the corpus;
- **581** end-NPC associations whose NPC differs from the quest's start NPC;
- **575** of those differing associations have a FinishScript with a first SAY;
- the supplied corpus has no active action-3 entries, so its observed end-NPC
  association set is effectively action 0.

This is a material runtime path, not an edge case.

## NPC-role status filtering at `0x00630570`

The broad association test is followed by an NPC-role-specific status filter.
With the same zeroed extra arguments used by `GetQuestStatusWithNPC`, the
native rules are:

- effective REPEAT (4), ABLE (5), FAILED (7) and READ_ABLE (20) are accepted
  only when the current NPC is the quest's Start NPC;
- ING (6) is accepted at the Start NPC or at an enabled end-NPC entry whose
  action is 3;
- REWARD (8) stays REWARD at the action-0 end NPC;
- REWARD at the Start NPC is accepted but rewritten to NPC-visible ING (6);
- effective 0..3 are rejected from this NPC quest-result path.

This distinction prevents a quest from becoming startable merely because its
turn-in NPC is present in the broad association list.

## Runtime implementation

`QuestNpcStartResolver` builds the SQL candidate mapping from both Start NPC
and end action-0/3 NPC rows and preserves association-role flags per NPC/quest
pair. `TryApplyNpcAssociationStatus` then applies the exact `0x00630570`
role rules above before status priority/tie-break selection.

The start-dialog join is a LEFT JOIN because an end-NPC-only candidate may not
need a start dialog for the currently effective state; Action/Finish dialogs are
derived from the corresponding scripts.
