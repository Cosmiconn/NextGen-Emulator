# Step 26 status — Quest priority recovery from original Zone binary

## Result

The original Zone server executable is now available and was inspected together with the matching Zone PDB.

All five supplied Zone executable variants are byte-identical. SHA-256:

`db1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5`

All supplied Zone PDB copies are byte-identical. SHA-256:

`569a9d3ee6c4478b8e96d176f15ce64a9aab3f42e52d29d17c3c4d5af8f9ec6c`

## Recovered constants

`m_QuestStatusPriority` (21 entries, object offset `0x24`):

`[22, 21, 21, 21, 21, 2, 1, 2, 0, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 2]`

`m_QuestTypePriority` (11 entries, object offset `0x78`):

`[5, 11, 1, 0, 11, 3, 2, 2, 2, 4, 11]`

These values are direct constructor writes in `CQuest::CQuest` at Zone.exe VA `0x006301d0`.

## Selection direction

`CQuest::GetQuestStatusWithNPC` indexes `m_QuestStatusPriority` by status and rejects a candidate whose value is greater than or equal to the retained value. Lower numeric status priority therefore wins.

After additional equal-status predicates, the function indexes `m_QuestTypePriority` by quest type and applies the same greater-or-equal rejection. Lower numeric quest-type priority therefore wins at that stage.

The implementation is not an additive score. Status priority and quest-type priority are separate comparison stages.

## Still unresolved

The additional equal-status candidate predicates in `GetQuestStatusWithNPC` have not yet been mapped to emulator-side quest/player fields. They must not be replaced with guessed semantics.

No Handler8 or NPC resolver behavior is changed solely from the recovered constants.
