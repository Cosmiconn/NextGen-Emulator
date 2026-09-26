using System;
using System.Globalization;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Exact used-corpus projection of
    /// PineEventScriptNode::SysFuncRand::sfb_Calculate at Zone.exe
    /// 0x004D77D0.
    ///
    /// The supplied nine Pine KQs use this system function exactly once:
    /// @Random(0 99). Native evaluates both operands, extracts their trailing
    /// decimal values, calls the linked MSVC CRT rand() at 0x006594B2, then
    /// computes min + rand() % (max - min + 1) with signed x86 IDIV. The upper
    /// bound is therefore inclusive.
    ///
    /// The caller must supply the shared CRT state. This helper does not seed
    /// or own that process-wide stream.
    /// </summary>
    public static class KingdomQuestPineRandomExpression
    {
        public const int UsedCallCount = 1;
        public const int UsedMinimum = 0;
        public const int UsedMaximum = 99;

        public static KingdomQuestPineExpressionResolution TryCalculateUsed(
            string expression,
            MsvcCrtRand random,
            KingdomQuestPineTokenValue destination)
        {
            if (expression == null ||
                random == null ||
                destination == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            int minimum;
            int maximum;
            if (!TryParseUsedExpression(
                    expression, out minimum, out maximum))
                return KingdomQuestPineExpressionResolution.Unsupported;

            // The exact used source is 0..99. Keeping this helper scoped to
            // that source-proven form avoids inventing behavior for malformed
            // or otherwise unused ranges.
            if (minimum != UsedMinimum ||
                maximum != UsedMaximum)
                return KingdomQuestPineExpressionResolution.Unsupported;

            int width = maximum - minimum + 1;
            int nativeRand = random.Next();
            int result = minimum + (nativeRand % width);

            return destination.TrySetAscii(
                    result.ToString(CultureInfo.InvariantCulture))
                ? KingdomQuestPineExpressionResolution.Success
                : KingdomQuestPineExpressionResolution.Invalid;
        }

        public static bool TryParseUsedExpression(
            string expression,
            out int minimum,
            out int maximum)
        {
            minimum = 0;
            maximum = 0;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string text = expression.Trim();
            const string prefix = "@Random(";
            if (!text.StartsWith(prefix, StringComparison.Ordinal) ||
                !text.EndsWith(")", StringComparison.Ordinal))
                return false;

            string arguments =
                text.Substring(prefix.Length, text.Length - prefix.Length - 1);
            string[] parts = arguments.Split(
                (char[])null,
                StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 &&
                int.TryParse(
                    parts[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out minimum) &&
                int.TryParse(
                    parts[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out maximum);
        }
    }
}
