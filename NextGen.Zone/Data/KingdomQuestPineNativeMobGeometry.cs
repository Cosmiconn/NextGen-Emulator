using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Runtime table at 0x13283770: two 180-DWORD halves. Lookup routines
    /// 0x5790E0/0x579110 use signed remainder, not degree normalization.
    /// Require an explicit native table; the x87/CRT constructor (0x579070)
    /// is NOT replaced by Math.Sin/Math.Cos or a generated approximate table.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobAngleTable
    {
        private readonly int[] values;
        public KingdomQuestPineNativeMobAngleTable(int[] nativeValues)
        {
            if (nativeValues == null || nativeValues.Length != 360) throw new ArgumentException("360 native DWORDs required.");
            values = (int[])nativeValues.Clone();
        }
        public bool TryFirst(int angle, out int value) => TryRead(angle % 180, out value);
        public bool TrySecond(int angle, out int value) => TryRead(180 + angle % 180, out value);
        private bool TryRead(int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= values.Length) return false;
            value = values[index]; return true;
        }
    }

    /// <summary>
    /// Concrete mbg_RegenLoc (0x4B1D60) and circle picker (0x4B0E80).
    /// These are runtime fields AFTER group setup, not a replacement for
    /// mbg_Set's x87 rotation initialization or the outer hatchery.
    /// All random streams/table data belong to their original Zone/thread.
    /// </summary>
    public sealed class KingdomQuestPineNativeMobGeometry : IKingdomQuestPineNativeMobRegenLocation
    {
        public int CenterX { get; set; }       // group +1980
        public int CenterY { get; set; }       // +1984
        public uint RegionHeight { get; set; } // +198C: zero selects circle
        public int HalfWidth { get; set; }     // +1994
        public int HalfHeight { get; set; }    // +1998
        public int RotationFirst { get; set; } // +199C: explicit native fixed-point value
        public int RotationSecond { get; set; }// +19A0
        public int Radius { get; set; }        // +19A4
        public int Direction { get; set; }     // +19AC, not initialized by mbg_Set

        private readonly MsvcCrtRand rectangleRandom;
        private readonly KingdomQuestPineNativeMobRandomWords selector, radial, angular;
        private readonly KingdomQuestPineNativeMobAngleTable table;
        private readonly Action invalidWidth;

        public KingdomQuestPineNativeMobGeometry(int centerX, int centerY, uint regionHeight,
            int halfWidth, int halfHeight, int rotationFirst, int rotationSecond, int radius, int direction,
            MsvcCrtRand rectangleRandom, KingdomQuestPineNativeMobRandomWords selector,
            KingdomQuestPineNativeMobRandomWords radial, KingdomQuestPineNativeMobRandomWords angular,
            KingdomQuestPineNativeMobAngleTable table, Action invalidWidth)
        {
            // Native rings occupy distinct addresses. Sharing one managed ring
            // between these three fields would invent an alias in native state.
            if ((selector != null && (ReferenceEquals(selector, radial) || ReferenceEquals(selector, angular))) ||
                (radial != null && ReferenceEquals(radial, angular))) throw new ArgumentException("Distinct native ring owners required.");
            CenterX = centerX; CenterY = centerY; RegionHeight = regionHeight;
            HalfWidth = halfWidth; HalfHeight = halfHeight; RotationFirst = rotationFirst;
            RotationSecond = rotationSecond; Radius = radius; Direction = direction;
            this.rectangleRandom = rectangleRandom; this.selector = selector; this.radial = radial;
            this.angular = angular; this.table = table; this.invalidWidth = invalidWidth;
        }
        // True is resolved; nativeResult is always 1 on a completed native call.
        // An unavailable dependency/divide fault retains earlier RNG consumption.
        public bool TryRegenLocation(out int x, out int y, out int direction, out byte nativeResult)
        {
            x = y = 0; direction = Direction; nativeResult = 0;
            if (RegionHeight == 0)
            {
                // Original constructs a by-value local circle before picking.
                int centerX = CenterX, centerY = CenterY, radius = Radius;
                if (!TryCircle(radius, out int dx, out int dy)) return false;
                x = unchecked(centerX + dx); y = unchecked(centerY + dy);
            }
            else
            {
                int dx = 0, dy = 0;
                if (HalfWidth <= 0)
                {
                    if (invalidWidth == null) return false;
                    invalidWidth(); // native assertion, then use zero offsets and return success
                }
                else
                {
                    if (rectangleRandom == null) return false;
                    int width = HalfWidth;
                    int sampleX = rectangleRandom.Next();
                    int divisorX = unchecked(width * 2);
                    if (divisorX == 0) return false; // original IDIV fault, not rejection/rollback
                    dx = unchecked(sampleX % divisorX - width);
                    int height = HalfHeight;
                    int sampleY = rectangleRandom.Next();
                    int divisorY = unchecked(height * 2);
                    if (divisorY == 0) return false;
                    dy = unchecked(sampleY % divisorY - height);
                }
                // Both products and the sum/difference wrap BEFORE division.
                // Signed truncation toward zero matches CDQ/bias/SAR in the EXE.
                int rotatedX = unchecked(RotationFirst * dx + RotationSecond * dy) / 1024;
                int rotatedY = unchecked(RotationFirst * dy - RotationSecond * dx) / 1024;
                x = unchecked(CenterX + rotatedX); y = unchecked(CenterY + rotatedY);
            }
            nativeResult = 1; return true;
        }
        private bool TryCircle(int radius, out int x, out int y)
        {
            x = y = 0;
            // Selector 14D920E8 / data 14D8A0E8; radial 14E7A1D0 /
            // data 14E721D0; angular 14F7A2D0 / data 14F722D0.
            if (selector == null || !selector.TryNext(out ushort branch)) return false;
            if (angular == null) return false; // native reads its index/mask before the switch
            int scale;
            if (branch == 0)
            {
                if (!angular.TryNext(out ushort word)) return false;
                scale = word;
            }
            else if (branch == 1 || branch == 2)
            {
                if (radial == null || !radial.TryNext(out ushort word)) return false;
                scale = word + (branch == 1 ? 64 : 96);
            }
            else
            {
                // Native falls through using the output POINTER as scale.
                // No portable address exists here; do not invent another branch.
                return false;
            }
            int distance = unchecked(radius * scale) / 128;
            if (!angular.TryNext(out ushort angleWord)) return false;
            int angle = angleWord * 180 / 64;
            if (table == null || !table.TryFirst(angle, out int first)) return false;
            int dx = unchecked(first * distance) / 1024;
            if (!table.TrySecond(angle, out int second)) return false;
            x = dx; y = unchecked(second * distance) / 1024;
            return true;
        }
    }
}
