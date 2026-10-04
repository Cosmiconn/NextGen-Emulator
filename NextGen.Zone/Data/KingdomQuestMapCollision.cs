using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Instance-owned native collision bitmap. Source: mbi_Load 0x0049E3B0,
    /// bitmap lookup 0x0049DF70, mdbe_Load 0x0049DC20 and mdbe_DoorAction
    /// 0x0049E080. Bit 1 blocks; closing ORs a mask, opening copies saved rows.
    /// </summary>
    public sealed class KingdomQuestMapCollision
    {
        private sealed class Door
        {
            public byte[] Name;
            public int Left, Top, Right, Bottom, Size, Offset;
        }
        private readonly object sync = new object();
        private readonly byte[] bits;
        private readonly byte[] doorBits;
        private readonly List<Door> doors;
        public string MapBase { get; }
        public int RowBytes { get; }
        public int Rows { get; }
        public int DoorCount { get { return doors.Count; } }

        private KingdomQuestMapCollision(string mapBase, int rowBytes, int rows,
            byte[] bits, byte[] doorBits, List<Door> doors)
        {
            MapBase = mapBase;
            RowBytes = rowBytes;
            Rows = rows;
            this.bits = bits;
            this.doorBits = doorBits;
            this.doors = doors;
        }

        public static bool TryLoad(string mapBase, byte[] shbd, byte[] shab,
            byte[] sbi, out KingdomQuestMapCollision collision)
        {
            collision = null;
            int width, height;
            byte[] bitmap;
            if (string.IsNullOrEmpty(mapBase) ||
                !TryBitmap(shbd, out width, out height, out bitmap)) return false;
            if (shab != null)
            {
                int aw, ah;
                byte[] overlay;
                if (!TryBitmap(shab, out aw, out ah, out overlay) || aw != width || ah != height)
                    return false;
                for (int i = 0; i < bitmap.Length; i++) bitmap[i] |= overlay[i];
            }

            var doors = new List<Door>();
            byte[] doorBits = Array.Empty<byte>();
            if (sbi != null)
            {
                if (sbi.Length < 4) return false;
                uint count = U32(sbi, 0);
                // mda_Load asserts count < 0x20 before reading 0x38-byte records.
                if (count >= 32 || 4L + count * 56L > sbi.Length) return false;
                int start = 4 + (int)count * 56;
                int dataLength = sbi.Length - start;
                long total = 0;
                for (int i = 0; i < count; i++)
                {
                    int p = 4 + i * 56;
                    var door = new Door { Name = sbi.Skip(p).Take(32).ToArray(),
                        Left = I32(sbi, p + 32), Top = I32(sbi, p + 36),
                        Right = I32(sbi, p + 40), Bottom = I32(sbi, p + 44),
                        Size = I32(sbi, p + 48), Offset = I32(sbi, p + 52) };
                    long columns = (long)door.Right - door.Left + 1;
                    long rows = (long)door.Bottom - door.Top + 1;
                    // Native assumes valid source bounds. Reject malformed data
                    // before any mutation instead of risking an out-of-bounds write.
                    if (door.Left < 0 || door.Top < 0 || columns <= 0 || rows <= 0 ||
                        door.Right >= (long)width * 8 || door.Bottom >= height ||
                        columns % 8 != 0 || columns / 8 * rows != door.Size ||
                        door.Size <= 0 || door.Offset < 0 ||
                        (long)door.Offset + 2L * door.Size > dataLength) return false;
                    total += 2L * door.Size;
                    doors.Add(door);
                }
                if (total != dataLength) return false;
                doorBits = sbi.Skip(start).ToArray();
            }
            collision = new KingdomQuestMapCollision(mapBase, width, height, bitmap, doorBits, doors);
            return true;
        }

        public bool CanWalk(int x, int y)
        {
            // Native unsigned multiply wraps BEFORE division by 50.
            uint column = unchecked((uint)x * 8u) / 50u;
            uint row = unchecked((uint)y * 8u) / 50u;
            if (column >= (uint)RowBytes * 8u || row >= (uint)Rows) return false;
            lock (sync)
                return (bits[(int)row * RowBytes + (int)(column >> 3)] &
                    (1 << (int)(column & 7))) == 0;
        }

        public void CloseAllDoors()
        {
            lock (sync)
                foreach (Door door in doors) Apply(door, false);
        }

        public bool TryDoorAction(byte[] nativeName32, bool open)
        {
            if (nativeName32 == null || nativeName32.Length != 32) return false;
            lock (sync)
                foreach (Door door in doors)
                    // Native compares eight DWORDs, applying only the first match.
                    if (door.Name.SequenceEqual(nativeName32))
                    {
                        Apply(door, open);
                        return true;
                    }
            return false;
        }

        private void Apply(Door door, bool open)
        {
            int width = (door.Right - door.Left + 1) / 8;
            int source = door.Offset + (open ? door.Size : 0);
            for (int row = door.Top; row <= door.Bottom; row++, source += width)
            {
                int target = row * RowBytes + (door.Left >> 3);
                if (open) Buffer.BlockCopy(doorBits, source, bits, target, width);
                else for (int i = 0; i < width; i++) bits[target + i] |= doorBits[source + i];
            }
        }

        public byte[] SnapshotBitmap()
        {
            lock (sync) return (byte[])bits.Clone();
        }

        private static bool TryBitmap(byte[] bytes, out int width, out int height, out byte[] bitmap)
        {
            width = height = 0;
            bitmap = null;
            if (bytes == null || bytes.Length < 8) return false;
            width = I32(bytes, 0);
            height = I32(bytes, 4);
            if (width <= 0 || width > int.MaxValue / 8 || height <= 0 ||
                (long)width * height != bytes.Length - 8L) return false;
            bitmap = bytes.Skip(8).ToArray();
            return true;
        }
        private static uint U32(byte[] b, int p) =>
            (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24);
        private static int I32(byte[] b, int p) => unchecked((int)U32(b, p));
    }

    public sealed class KingdomQuestPineMapStartOwner : IKingdomQuestPineMapStartOwner
    {
        public static KingdomQuestPineMapStartOwner Instance { get; } = new KingdomQuestPineMapStartOwner();
        public bool TryCloseAllDoors(Game.Map map)
        {
            if (map == null || map.MapInfo == null || map.KingdomQuestCollision == null ||
                map.KingdomQuestCollision.MapBase != map.MapInfo.ShortName) return false;
            map.KingdomQuestCollision.CloseAllDoors();
            return true;
        }
    }
}
