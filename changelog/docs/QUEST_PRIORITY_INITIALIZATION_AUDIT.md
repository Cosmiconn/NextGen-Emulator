# Quest priority initialization audit — Step 26 evidence

The newly supplied original Zone server executable closes the previous evidence gap.

## Binary identity

`Zone.exe`, `Zone 2.exe`, `Zone 3.exe`, `Zone 4.exe`, and `Zone 5.exe` are byte-identical.

SHA-256:

`db1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5`

The supplied `Zone.pdb` copies are also byte-identical.

SHA-256:

`569a9d3ee6c4478b8e96d176f15ce64a9aab3f42e52d29d17c3c4d5af8f9ec6c`

## Recovered status priority

`m_QuestStatusPriority` is an 84-byte / 21-entry array at object offset `0x24`.

Constructor values:

`[22, 21, 21, 21, 21, 2, 1, 2, 0, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 2]`

## Recovered quest-type priority

`m_QuestTypePriority` is a 44-byte / 11-entry array at object offset `0x78`.

Constructor values:

`[5, 11, 1, 0, 11, 3, 2, 2, 2, 4, 11]`

## Comparison direction

`CQuest::GetQuestStatusWithNPC` indexes the status-priority array by candidate status and rejects a candidate when its value is greater than or equal to the retained value. Lower numeric status priority wins.

After additional equal-status predicates, the same greater-or-equal rejection is applied to quest-type priority. Lower numeric quest-type priority wins at that stage.

This proves separate status-priority and quest-type-priority comparison stages; it does not prove an additive score.

The remaining equal-status predicates are still unresolved and must not be guessed.
