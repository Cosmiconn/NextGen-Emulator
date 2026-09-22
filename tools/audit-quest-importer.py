#!/usr/bin/env python3
"""Regression audit for tools/import-questdata.py normalized condition fields."""

import struct
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "tools" / "import-questdata.py"
FIXED = 0x2A8


def put_u16(buf, off, value):
    struct.pack_into("<H", buf, off, value)


def put_u32(buf, off, value):
    struct.pack_into("<I", buf, off, value)


def put_i32(buf, off, value):
    struct.pack_into("<i", buf, off, value)


def put_i64(buf, off, value):
    struct.pack_into("<q", buf, off, value)


def main():
    fixed = bytearray(FIXED)
    put_u32(fixed, 0x00, 77)       # SourceIndex
    put_u32(fixed, 0x04, 12345)    # QuestID
    fixed[0x11] = 3
    fixed[0x12] = 1

    st = memoryview(fixed)[0x18:0x58]
    st[14] = 1
    put_u16(st, 16, 321)
    put_i32(st, 20, -12345)
    put_i32(st, 24, 67890)
    put_u32(st, 28, 4567)
    st[32] = 1
    put_u16(st, 34, 2345)
    st[42] = 1
    st[43] = 2
    put_i64(st, 48, 111111111)
    put_i64(st, 56, 222222222)

    en = memoryview(fixed)[0x58:0xC0]
    en[1] = 1
    en[2] = 42
    en[0x4A] = 1
    put_u16(en, 0x4C, 654)
    put_i32(en, 0x50, -2222)
    put_i32(en, 0x54, 3333)
    put_u32(en, 0x58, 4444)
    en[0x5C] = 1
    put_u16(en, 0x5E, 55)

    struct.pack_into("<HHH", fixed, 0x294, 0, 0, 0)

    with tempfile.TemporaryDirectory() as td:
        td = Path(td)
        source = td / "QuestData.shn"
        output = td / "QuestData.sql"
        source.write_bytes(b"QST0" + fixed)
        subprocess.run(
            [sys.executable, str(IMPORTER), str(source), "-o", str(output)],
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )
        sql = output.read_text(encoding="utf-8")

    required = [
        "`LocationRaw` VARBINARY(18)",
        "`LocationMap` INT UNSIGNED NOT NULL",
        "`LocationX` INT NOT NULL",
        "`DateStart` BIGINT NOT NULL",
        "`DateEnd` BIGINT NOT NULL",
        ",1,X'010041010000c7cfffff32090100d7110000',321,-12345,67890,4567,1,2345,",
        ",1,2,X'c76b9f06000000008ed73e0d00000000',111111111,222222222);",
        ",1,X'01008e02000052f7ffff050d00005c110000',654,-2222,3333,4444,1,55,",
    ]
    missing = [needle for needle in required if needle not in sql]
    if missing:
        for needle in missing:
            print("missing:", needle)
        return 1

    # Ensure the old, misaligned date slice (padding + DateStart + half DateEnd)
    # is no longer emitted.
    if "X'00000000c76b9f06000000008ed73e0d'" in sql:
        print("legacy misaligned DateRaw slice is still present")
        return 1

    print("quest importer normalized-field audit: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
