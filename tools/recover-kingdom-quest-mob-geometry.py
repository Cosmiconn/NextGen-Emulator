#!/usr/bin/env python3
"""Recover geometry vectors by executing the ORIGINAL Zone.exe x86 bodies.

Usage: python tools/recover-kingdom-quest-mob-geometry.py Zone.exe output.json
Requires pefile and unicorn. No original executable bytes are redistributed.
Profiler, assertion output, cookie check and CRT thread lookup are shims;
geometry, signed IDIV, table lookup and CRT rand instructions execute unmodified.
Inputs are synthetic explicit runtime states, NOT native initialization captures.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import pefile
from unicorn import Uc, UcError, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_ECX, UC_X86_REG_EIP, UC_X86_REG_ESP, UC_X86_REG_FPCW, UC_X86_REG_MXCSR

EXPECTED_SHA = 'db1cb42912556a4ea5cde5c18f15f2495b81465c70ca9c18ad5bc7e36611aff5'
exe = Path(sys.argv[1]).read_bytes()
if hashlib.sha256(exe).hexdigest() != EXPECTED_SHA:
    raise SystemExit('Wrong original Zone.exe SHA256')
pe = pefile.PE(data=exe)
u = Uc(UC_ARCH_X86, UC_MODE_32)
u.mem_map(0, 0x1000)  # FS:[0] exception chain
u.mem_map(0x400000, (pe.OPTIONAL_HEADER.SizeOfImage + 0xFFF) & ~0xFFF)
for section in pe.sections:
    u.mem_write(0x400000 + section.VirtualAddress, section.get_data())
u.mem_map(0x20000000, 0x30000)
THREAD, GROUP, XY, DIRECTION, STACK, STOP = 0x20000000, 0x20001000, 0x20004000, 0x20004008, 0x20020000, 0x2002F000
RINGS = [(0x14D8A0E8, 0x14D920E8), (0x14E721D0, 0x14E7A1D0), (0x14F722D0, 0x14F7A2D0)]

def write32(address, value):
    u.mem_write(address, struct.pack('<I', value & 0xFFFFFFFF))

def read32(address):
    return struct.unpack('<I', u.mem_read(address, 4))[0]

def signed(value):
    return value if value < 0x80000000 else value - 0x100000000

assertions = 0
returned = False

def hook(uc, address, size, data):
    global assertions, returned
    if address == STOP:
        returned = True
        uc.emu_stop()
        return
    cleanup = {0x4682D0: 4, 0x468360: 4, 0x6566BF: 0, 0x6637A8: 0, 0x63C6C0: 8}
    if address not in cleanup:
        return
    sp = uc.reg_read(UC_X86_REG_ESP)
    if address == 0x6637A8:
        uc.reg_write(UC_X86_REG_EAX, THREAD)
    elif address == 0x63C6C0:
        assertions += 1
    uc.reg_write(UC_X86_REG_EIP, read32(sp))
    uc.reg_write(UC_X86_REG_ESP, sp + 4 + cleanup[address])

u.hook_add(UC_HOOK_CODE, hook)
table = [i * 17 - 1500 for i in range(360)]
u.mem_write(0x13283770, struct.pack('<360i', *table))
cases = []

def case(name, **overrides):
    c = dict(name=name, centerX=1000, centerY=-2000, regionHeight=1, halfWidth=17, halfHeight=31,
             rotationFirst=1024, rotationSecond=0, radius=128, direction=-99, crtSeed=1,
             selector=[0, 0, 0, 0], radial=[5, 17, 31, 43], angular=[3, 33, 65, 127])
    c.update(overrides)
    cases.append(c)

case('rectangle-base')
case('rectangle-rotation', rotationFirst=721, rotationSecond=723, crtSeed=0xDEADBEEF)
case('rectangle-negative-rotation', rotationFirst=-1023, rotationSecond=-1024, crtSeed=0xFFFFFFFF)
case('rectangle-zero-width', halfWidth=0)
case('rectangle-negative-width', halfWidth=-1)
case('rectangle-zero-height-fault', halfHeight=0)
case('rectangle-height-double-wrap-fault', halfHeight=-2147483648)
case('rectangle-width-double-sign', halfWidth=1073741824, halfHeight=-9)
case('rectangle-width-double-minus-two', halfWidth=2147483647, halfHeight=2147483647)
case('rectangle-product-wrap', centerX=2147483647, centerY=-2147483648, halfWidth=16777217,
     halfHeight=-16777219, rotationFirst=2147483647, rotationSecond=-2147483648, crtSeed=0xAABBCCDD)
for branch in range(3):
    case('circle-branch-' + str(branch), regionHeight=0, selector=[0, branch, 0, 0])
case('circle-negative-radius', regionHeight=0, radius=-129)
case('circle-radius-product-wrap', regionHeight=0, radius=2147483647, angular=[0, 65535, 65535, 0])
case('circle-zero-radius-consumes-random', regionHeight=0, radius=0, selector=[0, 2, 0, 0])
case('circle-center-wrap', regionHeight=0, radius=1048576, centerX=2147483647, centerY=-2147483648)
case('circle-angle-wrap', regionHeight=0, selector=[0, 1, 0, 0], angular=[0, 65535, 0, 0])

for c in cases:
    for key, offset in [('centerX', 0x1980), ('centerY', 0x1984), ('regionHeight', 0x198C),
                        ('halfWidth', 0x1994), ('halfHeight', 0x1998), ('rotationFirst', 0x199C),
                        ('rotationSecond', 0x19A0), ('radius', 0x19A4), ('direction', 0x19AC)]:
        write32(GROUP + offset, c[key])
    write32(THREAD + 0x14, c['crtSeed'])
    for (table_address, index_address), key in zip(RINGS, ['selector', 'radial', 'angular']):
        u.mem_write(table_address, struct.pack('<4H', *c[key]))
        write32(index_address, 0)
        write32(index_address + 4, 3)
    write32(XY, 0x12345678)
    write32(XY + 4, 0x23456789)
    write32(DIRECTION, 0)
    write32(0x7549A8, 0)
    for i, value in enumerate([STOP, XY, DIRECTION]):
        write32(STACK + i * 4, value)
    u.reg_write(UC_X86_REG_ECX, GROUP)
    u.reg_write(UC_X86_REG_ESP, STACK)
    assertions = 0
    returned = False
    fault = None
    try:
        u.emu_start(0x4B1D60, STOP + 1, count=10000)
    except UcError as error:
        fault = str(error)
    if not returned and fault is None:
        raise RuntimeError('Original routine did not return: ' + c['name'])
    c['expected'] = dict(resolved=returned, x=signed(read32(XY)), y=signed(read32(XY + 4)),
                         direction=signed(read32(DIRECTION)), assertions=assertions,
                         nativeResult=(u.reg_read(UC_X86_REG_EAX) & 255) if returned else 0,
                         crtState=read32(THREAD + 0x14),
                         indices=[read32(address) for _, address in RINGS], fault=fault)
# Recover the actual angle-table constructor too. No trigonometric or
# conversion calls are shimmed. Both CRT feature flags are exercised separately.
startup = None
profiles = []
for math_sse in (0, 1):
    for conversion_sse in (0, 1):
        for control_word in (0x027F, 0x037F):
            write32(0x1550CC20, math_sse)
            write32(0x1550CD40, conversion_sse)
            write32(STACK, STOP)
            u.reg_write(UC_X86_REG_ESP, STACK)
            u.reg_write(UC_X86_REG_ECX, 0x13283770)
            u.reg_write(UC_X86_REG_FPCW, control_word)
            u.reg_write(UC_X86_REG_MXCSR, 0x1F80)
            returned = False
            u.emu_start(0x579070, STOP + 1, count=1000000)
            if not returned:
                raise RuntimeError('Native angle constructor did not return')
            data = bytes(u.mem_read(0x13283770, 1440))
            if startup is not None and data != startup:
                raise RuntimeError('Native angle table differs between verified FP paths')
            startup = data
            profiles.append(dict(mathSse=math_sse, conversionSse=conversion_sse,
                                 x87ControlWord=control_word, mxcsr=0x1F80))
if hashlib.sha256(startup).hexdigest() != '50aca2c9f617c45ebfdde052be10ff95095edb93fb79ad519aed77c2e7b28bad':
    raise RuntimeError('Native startup table differs from the recorded original execution')

output = dict(sourceSha256=EXPECTED_SHA, nativeEntry='0x004B1D60',
              note='Geometry vectors use synthetic runtime states; original startup table separately recovered in eight FP paths.',
              originalStartupTable=list(struct.unpack('<360i', startup)), startupProfiles=profiles,
              startupSha256=hashlib.sha256(startup).hexdigest(),
              angleTable=table, cases=cases)
Path(sys.argv[2]).write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
print(f'Wrote {len(cases)} original-x86 geometry cases and {len(profiles)} startup FP profiles')
