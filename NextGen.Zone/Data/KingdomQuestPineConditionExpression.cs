using System;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineConditionOperator : byte
    {
        NumericEqual = 1,
        NumericNotEqual = 2,
        NumericLess = 3,
        NumericGreater = 4,
        NumericLessOrEqual = 5,
        NumericGreaterOrEqual = 6,
        TokenEqual = 7,
        TokenNotEqual = 8,
    }

    public sealed class KingdomQuestPineConditionSource
    {
        public string LeftExpression { get; private set; }
        public KingdomQuestPineConditionOperator Operator { get; private set; }
        public string RightExpression { get; private set; }

        internal KingdomQuestPineConditionSource(
            string leftExpression,
            KingdomQuestPineConditionOperator conditionOperator,
            string rightExpression)
        {
            LeftExpression = leftExpression;
            Operator = conditionOperator;
            RightExpression = rightExpression;
        }
    }

    /// <summary>
    /// Exact comparison-operator boundary recovered from Zone.exe.
    ///
    /// CompareOperator::sa_Load                    0x004DB1B0
    /// CompareOperator::co_Equal                  0x004DAA50
    /// CompareOperator::co_Excremation            0x004DABB0
    /// CompareOperator::co_NotEqual               0x004DAC50
    /// Condition::sa_Calculate                    0x004D87A0
    ///
    /// Native modes 1..6 compare PineScriptToken::pst_GetNumber values:
    ///   == != < > <= >=
    /// Modes 7/8 compare the complete NUL-terminated token text:
    ///   === =!=
    ///
    /// The unusual three-character operators are source syntax, not aliases
    /// invented by the emulator. co_Equal consumes the second and third
    /// single-character tokens and assigns native mode 7 for === and mode 8
    /// for =!=.
    ///
    /// Supplied nine-Pine corpus: 26 IFs total:
    /// 11 ===, 5 <, 4 ==, 3 >, 3 =!=.
    /// </summary>
    public static class KingdomQuestPineConditionExpression
    {
        public const int UsedConditionCount = 26;
        public const int UsedTokenEqualCount = 11;
        public const int UsedNumericLessCount = 5;
        public const int UsedNumericEqualCount = 4;
        public const int UsedNumericGreaterCount = 3;
        public const int UsedTokenNotEqualCount = 3;

        private static readonly string[] OperatorTokens =
        {
            "=!=",
            "===",
            "==",
            "!=",
            "<=",
            ">=",
            "<",
            ">",
        };

        public static bool TryParse(
            string expression,
            out KingdomQuestPineConditionSource condition)
        {
            condition = null;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string source = expression.Trim();
            bool quoted = false;
            int depth = 0;
            int matchOffset = -1;
            string matchToken = null;

            for (int i = 0; i < source.Length; i++)
            {
                char ch = source[i];
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
                    if (depth == 0)
                        return false;
                    depth--;
                    continue;
                }
                if (depth != 0)
                    continue;

                for (int j = 0; j < OperatorTokens.Length; j++)
                {
                    string token = OperatorTokens[j];
                    if (i + token.Length > source.Length ||
                        string.CompareOrdinal(
                            source, i, token, 0, token.Length) != 0)
                        continue;

                    if (matchOffset >= 0)
                        return false;

                    matchOffset = i;
                    matchToken = token;
                    i += token.Length - 1;
                    break;
                }
            }

            if (quoted || depth != 0 ||
                matchOffset <= 0 ||
                string.IsNullOrEmpty(matchToken))
                return false;

            string left =
                source.Substring(0, matchOffset).Trim();
            string right =
                source.Substring(
                    matchOffset + matchToken.Length).Trim();
            if (left.Length == 0 || right.Length == 0)
                return false;

            KingdomQuestPineConditionOperator conditionOperator;
            if (!TryMapOperator(
                    matchToken, out conditionOperator))
                return false;

            condition = new KingdomQuestPineConditionSource(
                left, conditionOperator, right);
            return true;
        }

        public static bool TryEvaluate(
            KingdomQuestPineConditionOperator conditionOperator,
            KingdomQuestPineTokenValue left,
            KingdomQuestPineTokenValue right,
            out int result)
        {
            result = 0;
            if (left == null || right == null)
                return false;

            int ignoredPrefix;
            int leftNumber;
            int rightNumber;

            switch (conditionOperator)
            {
                case KingdomQuestPineConditionOperator.NumericEqual:
                    leftNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            left.Text, out ignoredPrefix);
                    rightNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            right.Text, out ignoredPrefix);
                    result = leftNumber == rightNumber ? 1 : 0;
                    return true;

                case KingdomQuestPineConditionOperator.NumericNotEqual:
                    leftNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            left.Text, out ignoredPrefix);
                    rightNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            right.Text, out ignoredPrefix);
                    result = leftNumber != rightNumber ? 1 : 0;
                    return true;

                case KingdomQuestPineConditionOperator.NumericLess:
                    leftNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            left.Text, out ignoredPrefix);
                    rightNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            right.Text, out ignoredPrefix);
                    result = leftNumber < rightNumber ? 1 : 0;
                    return true;

                case KingdomQuestPineConditionOperator.NumericGreater:
                    leftNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            left.Text, out ignoredPrefix);
                    rightNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            right.Text, out ignoredPrefix);
                    result = leftNumber > rightNumber ? 1 : 0;
                    return true;

                case KingdomQuestPineConditionOperator.NumericLessOrEqual:
                    leftNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            left.Text, out ignoredPrefix);
                    rightNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            right.Text, out ignoredPrefix);
                    result = leftNumber <= rightNumber ? 1 : 0;
                    return true;

                case KingdomQuestPineConditionOperator.NumericGreaterOrEqual:
                    leftNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            left.Text, out ignoredPrefix);
                    rightNumber =
                        KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                            right.Text, out ignoredPrefix);
                    result = leftNumber >= rightNumber ? 1 : 0;
                    return true;

                case KingdomQuestPineConditionOperator.TokenEqual:
                    result = string.Equals(
                        left.Text,
                        right.Text,
                        StringComparison.Ordinal)
                        ? 1
                        : 0;
                    return true;

                case KingdomQuestPineConditionOperator.TokenNotEqual:
                    result = !string.Equals(
                        left.Text,
                        right.Text,
                        StringComparison.Ordinal)
                        ? 1
                        : 0;
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryMapOperator(
            string token,
            out KingdomQuestPineConditionOperator conditionOperator)
        {
            conditionOperator = 0;
            switch (token)
            {
                case "==":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.NumericEqual;
                    return true;
                case "!=":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.NumericNotEqual;
                    return true;
                case "<":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.NumericLess;
                    return true;
                case ">":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.NumericGreater;
                    return true;
                case "<=":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.NumericLessOrEqual;
                    return true;
                case ">=":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.NumericGreaterOrEqual;
                    return true;
                case "===":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.TokenEqual;
                    return true;
                case "=!=":
                    conditionOperator =
                        KingdomQuestPineConditionOperator.TokenNotEqual;
                    return true;
                default:
                    return false;
            }
        }
    }
}
