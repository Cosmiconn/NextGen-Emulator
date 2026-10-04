#!/usr/bin/env python3
"""Reproduce the byte-exact KQ collision bundle from the supplied Server.zip."""
import argparse
import hashlib
import io
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("server_zip", type=Path)
    args = parser.parse_args()
    sql = (ROOT / "sql/data/data_kq_source_20_kingdomquestmap.sql").read_text()
    bases = sorted(set(re.findall(r"^  \(\d+, \d+, '([^']+)'", sql, re.M)))
    assert len(bases) == 23, "Review changed source map corpus before regenerating"
    output = io.BytesIO()
    rows = ["MapBase\tFile\tSize\tSHA256"]
    with zipfile.ZipFile(args.server_zip) as source, zipfile.ZipFile(output, "w") as bundle:
        names = {}
        for name in source.namelist():
            if "/BlockInfo/" in name and not name.startswith("__MACOSX/"):
                key = name.rsplit("/", 1)[-1]
                assert key not in names, "Ambiguous source file: " + key
                names[key] = name
        for base in bases:
            for extension in (".shbd", ".shab", ".sbi"):
                key = base + extension
                if key not in names:
                    assert extension != ".shbd", "Missing required bitmap: " + key
                    rows.append(f"{base}\t{key}\t0\tABSENT")
                    continue
                data = source.read(names[key])
                info = zipfile.ZipInfo(key, (1980, 1, 1, 0, 0, 0))
                info.compress_type = zipfile.ZIP_DEFLATED
                info.external_attr = 0o100644 << 16
                bundle.writestr(info, data, compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)
                rows.append(f"{base}\t{key}\t{len(data)}\t{hashlib.sha256(data).hexdigest()}")
    data = output.getvalue()
    with zipfile.ZipFile(io.BytesIO(data)) as check:
        assert check.testzip() is None
    path = ROOT / "NextGen.Zone/Data/Sources/KingdomQuestCollision.zip"
    path.parent.mkdir(exist_ok=True)
    temporary = path.with_suffix(".tmp")
    temporary.write_bytes(data)
    temporary.replace(path)
    assert path.read_bytes() == data
    (ROOT / "docs/KINGDOM_QUEST_COLLISION_SOURCE.tsv").write_text("\n".join(rows) + "\n")
    print(f"{len(bases)} maps; {len(data)} bytes; bundle SHA256 {hashlib.sha256(data).hexdigest()}")
    print("Verify/update BundleSha256 in KingdomQuestMapCollisionSource.cs after source review.")


if __name__ == "__main__":
    main()
