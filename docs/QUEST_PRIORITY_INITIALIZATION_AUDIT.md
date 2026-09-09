# Quest priority initialization audit

## Scope

Step 25 investigated whether the supplied `Zone.pdb` is sufficient to recover the initialized contents of:

- `CQuest::m_QuestStatusPriority`
- `CQuest::m_QuestTypePriority`

The objective was to avoid inventing a priority order from enum declaration order or quest names.

## Proven from CodeView type information

`PLAYER_QUEST_STATUS` has 21 indexed values (`0..20`), with `PQS_MAX_PLAYER_QUEST_STATUS = 21`.

`m_QuestStatusPriority` is represented as an 84-byte member, consistent with 21 32-bit priority entries.

The quest-type enum has 11 usable values (`0..10`) followed by `QT_MAX_QUEST_TYPE = 11`.

`m_QuestTypePriority` is represented as a 44-byte member, consistent with 11 32-bit priority entries.

The PDB also exposes the NPC quest result structure and the methods that consume these members.

## Important limitation

A PDB contains debug/type/symbol metadata. The supplied archive contains PDB files and database backups, but no corresponding original executable/object image from which the initialized memory contents of these two members can be read.

Therefore the following are **UNRESOLVED**:

1. The 21 numeric values in `m_QuestStatusPriority`.
2. The 11 numeric values in `m_QuestTypePriority`.
3. Whether larger or smaller priority numbers win.
4. Whether the two priority dimensions are added, compared lexicographically, or used in separate stages.
5. Tie-breaking between quests with equal status/type priority.

The member sizes must not be mistaken for proof of their contents.

## Capture evidence

The available capture descriptions establish observable quest-list usage, including switching between available and in-progress quest lists and selecting a quest, but they do not provide the raw `0x440F/0x4410` packet bytes needed to assign ACK fields or reconstruct the server's internal priority values. The description of the Tiros sequence also confirms that multiple quests can be presented sequentially at one NPC, but does not by itself establish the ordering algorithm.

## Consequence for implementation

No quest-selection priority constants are added to runtime code in this step.

`QuestNpcStartResolver` must continue to refuse ambiguous NPCs rather than selecting a candidate by SQL order, QuestID order, enum order, or guessed priority.

## Next evidence required

The highest-value missing artifact is either:

- an original executable/object image matching the supplied PDB, or
- a packet capture containing the raw quest-list / select-start exchange, ideally with a character whose quest states can be correlated before and after the interaction.

If neither becomes available, the priority algorithm remains explicitly `UNRESOLVED` and should not be implemented as a guessed approximation.
