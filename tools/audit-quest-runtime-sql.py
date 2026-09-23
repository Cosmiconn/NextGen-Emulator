#!/usr/bin/env python3
"""Regression guard: QuestData.shn is import-only; quest runtime reads SQL."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

RUNTIME_FILES = {
    "NextGen.Zone/Data/DataProvider.cs": (
        "data_questdialog",
    ),
    "NextGen.Zone/Data/QuestRuntime.cs": (
        "QuestData",
        "QuestData_ConditionEnd",
        "QuestData_NPCMob",
        "QuestData_Item",
        "QuestData_Reward",
    ),
    "NextGen.Zone/Handlers/QuestNpcStartResolver.cs": (
        "QuestData_ConditionStart",
        "FROM QuestData q",
    ),
    "NextGen.Zone/Handlers/Handler17.cs": (
        "data_quest_script",
    ),
}

FORBIDDEN_RUNTIME_READERS = (
    "SHNFile",
    "SHNReader",
    "ShineReader",
)

ALLOWED_QUESTDATA_SHN = {
    "NextGen.Zone/Handlers/QuestNpcStartResolver.cs":
        "re-import QuestData.shn to get normalized location/daily columns",
}


def main():
    failures = []

    for rel, required in RUNTIME_FILES.items():
        path = ROOT / rel
        if not path.is_file():
            failures.append(f"missing runtime source: {rel}")
            continue

        text = path.read_text(encoding="utf-8")
        for token in required:
            if token not in text:
                failures.append(f"{rel}: missing SQL runtime marker {token!r}")

        for token in FORBIDDEN_RUNTIME_READERS:
            if token in text:
                failures.append(f"{rel}: forbidden SHN reader token {token!r}")

    zone_root = ROOT / "NextGen.Zone"
    if not zone_root.is_dir():
        failures.append("missing NextGen.Zone source tree")
    else:
        for path in zone_root.rglob("*.cs"):
            rel = path.relative_to(ROOT).as_posix()
            for lineno, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
                if "QuestData.shn" not in line:
                    continue
                allowed = ALLOWED_QUESTDATA_SHN.get(rel)
                if not allowed or allowed not in line:
                    failures.append(
                        f"{rel}:{lineno}: QuestData.shn runtime reference is not import-only"
                    )

    if failures:
        for failure in failures:
            print("FAIL:", failure)
        return 1

    print("quest SQL-only runtime audit: OK")
    print("QuestData.shn remains a one-time importer source, not a Zone runtime dependency.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
