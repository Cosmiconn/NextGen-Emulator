# Quest DONE stage semantics — binary and corpus audit

## Native dispatch

The original command-name table identifies textual `DONE` as quest command
**10**.

`CQuestZone::QuestNext` at `0x005BDEF0` dispatches parsed commands through
the table at `0x005BF020`. Entry 10 points to `0x005BED38`.

The command-10 branch:

1. reads the quest ID from the parsed command record;
2. resolves the player's quest record through `0x0062F210`;
3. calls `0x006300D0` with that quest ID;
4. `0x006300D0` resolves the player-quest record and calls
   `0x0062FF40`, the proven native reward-eligibility routine;
5. continues into the reward/completion path when eligible.

No branch in the DONE dispatcher tests whether the current parser came from
QuestStart, QuestDoing, or QuestEnd. Stage is therefore not a precondition of
the DONE command itself.

This is distinct from the later completion mutation ordering: reward delivery
must still succeed before the quest is moved to its completed state, and the
parser resumes after completion.

## Corpus impact

The verified 2304-record QuestData corpus contains **2603** textual DONE
commands:

| Script stage | DONE count |
|---|---:|
| Start | 351 |
| Action/Doing | 1 |
| Finish/End | 2251 |

Every one of the 351 Start-stage DONE occurrences has an ACCEPT earlier in the
same Start script. These are real immediate-completion flows rather than
unreachable parser debris.

Quest 2 is a minimal example:

```text
ACCEPT
DONE
END
```

Quest 2230 is the single Action-stage example; its Action and Finish scripts
both contain DONE.

## Runtime consequence

The emulator must not restrict DONE to `QuestScriptStage.Finish`.
`Handler17` now sends DONE from any stage through the same
`QuestRuntime.Complete` path. Existing reward-selection suspension and
post-completion script continuation remain unchanged.

CI records the exact per-stage DONE counts and fails if the supplied corpus
shape changes.

## Evidence boundary

This proves stage independence of command 10 and its active corpus impact. It
does not claim byte-for-byte equivalence for the original ItemDB reward
transaction; that separate boundary remains documented by the reward audits.
