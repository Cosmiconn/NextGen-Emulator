# Quest NPC selection: equal-priority tie-break audit

This changelog copy records the Step 27 audit of the original `CQuest::GetQuestStatusWithNPC` control flow in `Zone.exe`.

- Function VA: `0x00630f60`
- Status priority table: `this + 0x24`, 21 x 32-bit values
- Quest-type priority table: `this + 0x78`, 11 x 32-bit values
- Lower numeric priority wins at both priority comparison stages.
- Equal status priority is followed by additional raw candidate-field predicates before quest-type priority.
- Raw offsets observed in those predicates: `0x1A`, `0x1B`, `0x11`, `0x38`, `0x20`, `0x12`.

The meanings of those offsets remain **UNRESOLVED** until supported by CodeView field definitions or additional binary/database/capture evidence. No runtime behavior is changed from this audit alone.
