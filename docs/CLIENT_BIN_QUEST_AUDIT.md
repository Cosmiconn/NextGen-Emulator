# Client-Bin Quest Audit — Step 33

## Input

Uploaded `Client-Bin-DLLs.zip` was inspected as an additional clean-room reference.

Relevant executable:

- `Fiesta.bin` — PE32 i386, image base `0x00400000`, timestamp `2016-06-02 08:20:09`.
- SHA256: `01196b20abe4fb542ee8c4685f57224ef29051b2bf24196eed80a9b2a95ae4fb`

Supporting DLLs were present but no direct quest-status conclusion was extracted from them in this step.

## Quest-related client evidence

The client binary contains explicit quest UI/script/resource identifiers, including:

- `cGetQuestStatus`
- `NpcQuestWin`
- `DlgQuestListWin`
- `QuestList`
- `NPCQuest`
- `NPCQuestING`
- `NPCQuestEND`
- `NPCQuestLowLevel`
- `quest_ack`
- `QuestCursor.cur`
- `KingdomQuestDesc.shn`
- `KQuestSucc.nif`
- `KQuestFail.nif`

The client also contains the resource/script identifier `cGetQuestStatus`, with nearby command-style identifiers such as `cLoadEffect`, `cLoadKFM`, etc. This establishes that the client has an explicit quest-status-facing command/resource layer.

## Important negative result

The strings `NPCQuestING`, `NPCQuestEND`, and `NPCQuestLowLevel` occur in a client-side NPC/resource-name table. Their presence alone does **not** prove the server `PLAYER_QUEST_STATUS` numeric mapping. They are therefore not mapped to `PQS_ING`, `PQS_DONE`, `PQS_LOWABLE`, etc. in this audit.

Likewise, occurrences of byte sequences corresponding to `0x440F` / `0x4410` are present in the client executable, but the current pass did not establish a unique packet-builder call site with a proven field layout. No packet semantics are inferred from raw byte occurrences.

## Relevance to the current blocker

The client binary confirms that quest status/list handling exists on the client side and provides additional search anchors (`cGetQuestStatus`, `quest_ack`, quest-window resources). It does **not** yet resolve the server PDB/machine-code contradiction around `GetNewQuestStatus`.

The next high-value investigation is to follow the client-side `cGetQuestStatus` registration/use and the `quest_ack` path, then correlate any resulting packet fields with the already captured Header17 quest traffic. This will only be promoted to protocol semantics when the field mapping is directly demonstrated.

## Runtime

No runtime changes.
No SQL changes.
No client binary modifications.
