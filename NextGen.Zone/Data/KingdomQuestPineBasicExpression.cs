using System;
using System.Globalization;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineExpressionResolution : byte
    {
        Unsupported = 0,
        Success = 1,
        Invalid = 2,
    }

    /// <summary>
    /// Source-proven host-free subset of the native Pine expression runtime.
    ///
    /// Native anchors:
    ///   String::sa_Load                         0x004DB330
    ///     removes surrounding quotation marks through
    ///     PineScriptToken::pst_RemoveQuatator 0x004D6310.
    ///   Number::sa_Calculate / String::sa_Calculate 0x004D6710
    ///     then copy the full 0x100-byte stored token into the destination.
    ///   Identify::sa_Calculate                    0x004D6650
    ///     copies the resolved VariableStack value token.
    ///   PineScriptToken::pst_GetNumber           0x004D6360
    ///     reads the trailing decimal digit suffix.
    ///   PineScriptToken::operator+               0x004D7390
    ///   PineScriptToken::operator-               0x004D74B0
    ///     preserve the left token prefix before its numeric suffix and append
    ///     the signed decimal result using native "%d" formatting.
    ///   PineScriptToken::operator%= / pst_Merge   0x004D63E0
    ///     appends the right token text at the left token's terminating NUL.
    ///     The supplied KQ Pine source uses that operator to compose strings
    ///     such as HERO_<stage>_<rank>.
    ///
    /// Quoted source strings are therefore represented at runtime without
    /// their surrounding quotation marks, exactly as native String::sa_Load
    /// prepares them before calculation.
    ///
    /// The exact used @RemoveFirst(variable-name, delimiter) system-function
    /// form is composed before these generic primitives because its native
    /// behavior mutates both the destination and the referenced source
    /// VariableStack token.
    ///
    /// Other system functions, dynamic '#(...)' identifiers,
    /// multiply/divide and comparison operators remain outside this helper.
    /// Native '%' token merge is handled here.
    /// </summary>
    public static class KingdomQuestPineBasicExpression
    {
        public static KingdomQuestPineExpressionResolution TryCalculate(
            string expression,
            KingdomQuestPineVariableStack variables,
            KingdomQuestPineTokenValue destination)
        {
            if (expression == null ||
                variables == null ||
                destination == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            string source = expression.Trim();
            if (source.Length == 0)
                return KingdomQuestPineExpressionResolution.Invalid;

            KingdomQuestPineExpressionResolution removeFirst =
                KingdomQuestPineRemoveFirst.TryCalculateUsed(
                    source, variables, destination);
            if (removeFirst !=
                KingdomQuestPineExpressionResolution.Unsupported)
                return removeFirst;

            if (IsQuotedLiteral(source))
                return destination.TrySetAscii(
                        source.Substring(1, source.Length - 2))
                    ? KingdomQuestPineExpressionResolution.Success
                    : KingdomQuestPineExpressionResolution.Invalid;

            if (IsUnsignedDecimal(source))
                return destination.TrySetAscii(source)
                    ? KingdomQuestPineExpressionResolution.Success
                    : KingdomQuestPineExpressionResolution.Invalid;

            string identifier;
            if (TrySimpleIdentifier(source, out identifier))
            {
                KingdomQuestPineTokenValue value;
                if (!variables.TryFind(identifier, out value))
                    return KingdomQuestPineExpressionResolution.Invalid;

                return destination.TryCopyFrom(value)
                    ? KingdomQuestPineExpressionResolution.Success
                    : KingdomQuestPineExpressionResolution.Invalid;
            }

            bool percentRecognized;
            string percentMerged;
            if (TryPercentMerge(
                    source,
                    variables,
                    out percentRecognized,
                    out percentMerged))
            {
                return destination.TrySetAscii(percentMerged)
                    ? KingdomQuestPineExpressionResolution.Success
                    : KingdomQuestPineExpressionResolution.Invalid;
            }
            if (percentRecognized)
                return KingdomQuestPineExpressionResolution.Invalid;

            string leftExpression;
            string rightExpression;
            char operation;
            if (!TryBinaryAddSubtract(
                    source,
                    out leftExpression,
                    out operation,
                    out rightExpression))
                return KingdomQuestPineExpressionResolution.Unsupported;

            KingdomQuestPineTokenValue left;
            KingdomQuestPineTokenValue right;
            if (!TryResolveSimpleOperand(
                    leftExpression, variables, out left) ||
                !TryResolveSimpleOperand(
                    rightExpression, variables, out right))
                return KingdomQuestPineExpressionResolution.Unsupported;

            string merged = MergeNativeNumberSuffix(
                left.Text, right.Text, operation == '-');
            return destination.TrySetAscii(merged)
                ? KingdomQuestPineExpressionResolution.Success
                : KingdomQuestPineExpressionResolution.Invalid;
        }

        public static bool TrySimpleIdentifier(
            string expression,
            out string identifier)
        {
            identifier = null;
            if (string.IsNullOrEmpty(expression))
                return false;

            string value = expression.Trim();
            if (value.Length == 0 ||
                !IsIdentifierStart(value[0]))
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!IsIdentifierPart(value[i]))
                    return false;
            }

            identifier = value;
            return true;
        }

        internal static string MergeNativeNumberSuffix(
            string left,
            string right,
            bool subtract)
        {
            if (left == null)
                left = string.Empty;
            if (right == null)
                right = string.Empty;

            int leftPrefixLength;
            int rightPrefixLength;
            int leftNumber = GetNativeNumberSuffix(
                left, out leftPrefixLength);
            int rightNumber = GetNativeNumberSuffix(
                right, out rightPrefixLength);

            int result = subtract
                ? unchecked(leftNumber - rightNumber)
                : unchecked(leftNumber + rightNumber);

            return left.Substring(0, leftPrefixLength) +
                result.ToString(CultureInfo.InvariantCulture);
        }

        internal static int GetNativeNumberSuffix(
            string value,
            out int prefixLength)
        {
            if (value == null)
                value = string.Empty;

            int result = 0;
            int multiplier = 1;
            int index = value.Length - 1;
            while (index >= 0)
            {
                char ch = value[index];
                if (ch < '0' || ch > '9')
                    break;

                unchecked
                {
                    result += (ch - '0') * multiplier;
                    multiplier *= 10;
                }
                index--;
            }

            prefixLength = index + 1;
            return result;
        }

        private static bool TryPercentMerge(
            string expression,
            KingdomQuestPineVariableStack variables,
            out bool recognized,
            out string merged)
        {
            recognized = false;
            merged = null;
            if (expression == null || variables == null)
                return false;

            var parts = new System.Collections.Generic.List<string>();
            bool quoted = false;
            int depth = 0;
            int partStart = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];
                if (ch == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (quoted)
                    continue;

                if (ch == '(')
                {
                    depth++;
                    continue;
                }
                if (ch == ')')
                {
                    if (depth > 0)
                        depth--;
                    continue;
                }
                if (ch != '%' || depth != 0)
                    continue;

                recognized = true;
                string part = expression.Substring(
                    partStart, i - partStart).Trim();
                if (part.Length == 0)
                    return false;

                parts.Add(part);
                partStart = i + 1;
            }

            if (!recognized)
                return false;

            string finalPart = expression.Substring(partStart).Trim();
            if (finalPart.Length == 0)
                return false;
            parts.Add(finalPart);

            string result = string.Empty;
            for (int i = 0; i < parts.Count; i++)
            {
                KingdomQuestPineTokenValue operand;
                if (!TryResolveSimpleOperand(
                        parts[i], variables, out operand) ||
                    operand == null)
                    return false;

                result += operand.Text;
                // PineScriptToken has a fixed 0x100-byte buffer. Fail closed
                // instead of reproducing native unchecked writes if a source
                // expression would overflow that storage.
                if (System.Text.Encoding.ASCII.GetByteCount(result) >=
                    KingdomQuestPineTokenValue.NativeByteCapacity)
                    return false;
            }

            merged = result;
            return true;
        }

        private static bool TryResolveSimpleOperand(
            string expression,
            KingdomQuestPineVariableStack variables,
            out KingdomQuestPineTokenValue value)
        {
            value = null;
            string source = expression == null
                ? string.Empty
                : expression.Trim();

            if (IsQuotedLiteral(source))
                return KingdomQuestPineTokenValue.TryCreate(
                    source.Substring(1, source.Length - 2),
                    out value);

            if (IsUnsignedDecimal(source))
                return KingdomQuestPineTokenValue.TryCreate(
                    source, out value);

            string identifier;
            return TrySimpleIdentifier(source, out identifier) &&
                variables.TryFind(identifier, out value);
        }

        private static bool TryBinaryAddSubtract(
            string expression,
            out string left,
            out char operation,
            out string right)
        {
            left = null;
            right = null;
            operation = '\0';

            bool quoted = false;
            int depth = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                char ch = expression[i];
                if (ch == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (quoted)
                    continue;

                if (ch == '(')
                {
                    depth++;
                    continue;
                }
                if (ch == ')')
                {
                    if (depth > 0)
                        depth--;
                    continue;
                }

                if (depth != 0 || (ch != '+' && ch != '-'))
                    continue;

                string candidateLeft =
                    expression.Substring(0, i).Trim();
                string candidateRight =
                    expression.Substring(i + 1).Trim();
                if (candidateLeft.Length == 0 ||
                    candidateRight.Length == 0)
                    return false;

                left = candidateLeft;
                operation = ch;
                right = candidateRight;
                return true;
            }

            return false;
        }

        private static bool IsQuotedLiteral(string value)
        {
            return value.Length >= 2 &&
                value[0] == '"' &&
                value[value.Length - 1] == '"';
        }

        private static bool IsUnsignedDecimal(string value)
        {
            if (value.Length == 0)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9')
                    return false;
            }
            return true;
        }

        private static bool IsIdentifierStart(char ch)
        {
            return (ch >= 'A' && ch <= 'Z') ||
                (ch >= 'a' && ch <= 'z') ||
                ch == '_';
        }

        private static bool IsIdentifierPart(char ch)
        {
            return IsIdentifierStart(ch) ||
                (ch >= '0' && ch <= '9');
        }
    }
}
