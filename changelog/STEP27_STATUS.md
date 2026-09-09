# Step 27 status

## Result

Audited the original `CQuest::GetQuestStatusWithNPC` control flow in the newly supplied original `Zone.exe`.

### Proven

- Status priority uses `this + 0x24 + status*4` and lower values win.
- Quest-type priority uses `this + 0x78 + type*4` and lower values win.
- Status priority is evaluated before quest-type priority.
- Equal status priority does **not** immediately fall through to type priority; additional raw candidate-field predicates occur first.
- Raw candidate offsets observed in those predicates: `0x1A`, `0x1B`, `0x11`, `0x38`, `0x20`, `0x12`.

### Not implemented

No runtime quest-selection behavior was changed in Step 27. The meanings of the intermediate raw fields remain UNRESOLVED.

## Evidence

Original binary:

- `Zone.exe`
- `CQuest::GetQuestStatusWithNPC` VA `0x00630f60`
- priority constructor VA `0x006301d0`

See `docs/QUEST_SELECTION_TIEBREAK_AUDIT.md` for the complete raw control-flow audit.
