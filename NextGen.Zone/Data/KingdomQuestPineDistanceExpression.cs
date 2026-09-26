using System;
using System.Globalization;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Host boundary matching ShineObjectManager::som_GetObject for the two
    /// source-used @DistanceBetween calls. A native Pine numeric handle is not
    /// assumed to be an emulator MapObjectID.
    /// </summary>
    public interface IKingdomQuestPineNativeObjectCoordinateResolver
    {
        bool TryResolveObject(
            int nativeObjectHandle,
            out int x,
            out int y);
    }

    /// <summary>
    /// Exact integer result of DirectDistanceTable::ddt_Distance
    /// (Zone.exe 0x004012D0).
    ///
    /// Native first keeps both deltas unchanged while each is inside
    /// [-1024, 1024]. If either is outside that range, BOTH are divided by two
    /// with signed truncation toward zero and a scale factor is doubled. The
    /// precomputed 2049x2049 table is then indexed by the reduced pair and its
    /// integer distance is multiplied by that scale.
    ///
    /// DirectDistanceTable::ddt_Initialize (0x0045FC80) builds each table
    /// distance as floor(sqrt(dx*dx + dy*dy)); the positive x87 result is
    /// converted under truncate rounding. Computing that same integer directly
    /// is equivalent and avoids carrying the 2049x2049 native cache.
    /// </summary>
    public static class KingdomQuestPineNativeDistance
    {
        public const int NativeCoordinateLimit = 0x400;

        public static int Calculate(int deltaX, int deltaY)
        {
            int x = deltaX;
            int y = deltaY;
            int scale = 1;

            unchecked
            {
                while (x < -NativeCoordinateLimit ||
                       x > NativeCoordinateLimit ||
                       y < -NativeCoordinateLimit ||
                       y > NativeCoordinateLimit)
                {
                    // Native: CDQ; SUB EAX,EDX; SAR EAX,1.
                    x = HalfTowardZero(x);
                    y = HalfTowardZero(y);
                    scale += scale;
                }

                int squared = x * x + y * y;
                int baseDistance = (int)Math.Sqrt((double)squared);
                return baseDistance * scale;
            }
        }

        private static int HalfTowardZero(int value)
        {
            return value < 0
                ? (value + 1) >> 1
                : value >> 1;
        }
    }

    /// <summary>
    /// Exact used-corpus projection of
    /// PineEventScriptNode::SysFuncShineDistance::sfb_Calculate
    /// (Zone.exe 0x004E5F40).
    ///
    /// The nine supplied Pine KQs use @DistanceBetween exactly twice, both in
    /// GordonMaster. Native evaluates the two argument expressions, takes
    /// PineScriptToken::pst_GetNumber from each result, resolves both numbers
    /// through ShineObjectManager::som_GetObject (0x0054FD10), subtracts their
    /// X/Y coordinates, calls DirectDistanceTable::ddt_Distance and stores the
    /// resulting integer token.
    ///
    /// Used source operands are simple identifiers, so this helper resolves
    /// those values from the native-modeled VariableStack. Object lookup itself
    /// remains an explicit host dependency.
    /// </summary>
    public static class KingdomQuestPineDistanceExpression
    {
        public const int UsedCallCount = 2;

        public static KingdomQuestPineExpressionResolution TryCalculateUsed(
            string expression,
            KingdomQuestPineVariableStack variables,
            IKingdomQuestPineNativeObjectCoordinateResolver objectResolver,
            KingdomQuestPineTokenValue destination)
        {
            if (expression == null ||
                variables == null ||
                objectResolver == null ||
                destination == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            string leftIdentifier;
            string rightIdentifier;
            if (!TryParseUsedExpression(
                    expression,
                    out leftIdentifier,
                    out rightIdentifier))
                return KingdomQuestPineExpressionResolution.Unsupported;

            KingdomQuestPineTokenValue leftValue;
            KingdomQuestPineTokenValue rightValue;
            if (!variables.TryFind(leftIdentifier, out leftValue) ||
                !variables.TryFind(rightIdentifier, out rightValue))
                return KingdomQuestPineExpressionResolution.Invalid;

            int ignoredPrefixLength;
            int leftHandle =
                KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                    leftValue.Text, out ignoredPrefixLength);
            int rightHandle =
                KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                    rightValue.Text, out ignoredPrefixLength);

            int leftX;
            int leftY;
            int rightX;
            int rightY;
            if (!objectResolver.TryResolveObject(
                    leftHandle, out leftX, out leftY) ||
                !objectResolver.TryResolveObject(
                    rightHandle, out rightX, out rightY))
                return KingdomQuestPineExpressionResolution.Invalid;

            int distance = KingdomQuestPineNativeDistance.Calculate(
                unchecked(leftX - rightX),
                unchecked(leftY - rightY));

            return destination.TrySetAscii(
                    distance.ToString(CultureInfo.InvariantCulture))
                ? KingdomQuestPineExpressionResolution.Success
                : KingdomQuestPineExpressionResolution.Invalid;
        }

        public static bool TryParseUsedExpression(
            string expression,
            out string leftIdentifier,
            out string rightIdentifier)
        {
            leftIdentifier = null;
            rightIdentifier = null;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string text = expression.Trim();
            const string prefix = "@DistanceBetween(";
            if (!text.StartsWith(prefix, StringComparison.Ordinal) ||
                !text.EndsWith(")", StringComparison.Ordinal))
                return false;

            string arguments =
                text.Substring(prefix.Length, text.Length - prefix.Length - 1);
            string[] parts = arguments.Split(
                (char[])null,
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !KingdomQuestPineBasicExpression.TrySimpleIdentifier(
                    parts[0], out leftIdentifier) ||
                !KingdomQuestPineBasicExpression.TrySimpleIdentifier(
                    parts[1], out rightIdentifier))
                return false;

            return true;
        }
    }
}
