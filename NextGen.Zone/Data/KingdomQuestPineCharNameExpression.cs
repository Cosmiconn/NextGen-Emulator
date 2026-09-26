using System;
using System.Text;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit native ShineObject handle -> so_CharName host boundary.
    /// Native handles are not assumed to equal emulator MapObject IDs.
    /// </summary>
    public interface IKingdomQuestPineNativeObjectNameResolver
    {
        bool TryResolveCharName(
            ushort nativeObjectHandle,
            out string characterName);
    }

    /// <summary>
    /// Exact used-corpus projection of
    /// PineEventScriptNode::SysFuncShineCharName::sfb_Calculate at
    /// Zone.exe 0x004E45A0.
    ///
    /// Native evaluates the one argument, takes pst_GetNumber, truncates the
    /// result to u16, resolves it through ShineObjectManager::som_GetObject at
    /// 0x0054FD10, then invokes virtual ShineObject::so_CharName at vtable
    /// offset +0x56C. Zone.pdb names the return type TName5.
    ///
    /// The returned fixed name buffer is copied as 0x15 bytes (20 payload
    /// bytes plus NUL). If som_GetObject returns null, native returns the
    /// already-cleared destination PineScriptToken rather than failing.
    ///
    /// The supplied nine Pine KQs use @CharName exactly ten times: five
    /// PlayerHandle and five LooterHandle calls.
    /// </summary>
    public static class KingdomQuestPineCharNameExpression
    {
        public const int UsedCallCount = 10;
        public const int UsedPlayerHandleCallCount = 5;
        public const int UsedLooterHandleCallCount = 5;
        public const int NativeNamePayloadBytes = 20;
        public const int NativeNameBufferBytes = 0x15;

        public static KingdomQuestPineExpressionResolution TryCalculateUsed(
            string expression,
            KingdomQuestPineVariableStack variables,
            IKingdomQuestPineNativeObjectNameResolver objectResolver,
            KingdomQuestPineTokenValue destination)
        {
            if (expression == null ||
                variables == null ||
                objectResolver == null ||
                destination == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            string identifier;
            if (!TryParseUsedExpression(expression, out identifier))
                return KingdomQuestPineExpressionResolution.Unsupported;

            KingdomQuestPineTokenValue source;
            if (!variables.TryFind(identifier, out source))
                return KingdomQuestPineExpressionResolution.Invalid;

            int ignoredPrefixLength;
            int nativeNumber =
                KingdomQuestPineBasicExpression.GetNativeNumberSuffix(
                    source.Text, out ignoredPrefixLength);
            ushort nativeHandle = unchecked((ushort)nativeNumber);

            string name;
            if (!objectResolver.TryResolveCharName(
                    nativeHandle, out name))
            {
                // SysFuncShineCharName clears the destination before object
                // lookup and returns that empty token when som_GetObject
                // returns null.
                return destination.TrySetAscii(string.Empty)
                    ? KingdomQuestPineExpressionResolution.Success
                    : KingdomQuestPineExpressionResolution.Invalid;
            }

            if (name == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            byte[] encoded = Encoding.ASCII.GetBytes(name);
            if (encoded.Length > NativeNamePayloadBytes)
                return KingdomQuestPineExpressionResolution.Invalid;

            return destination.TrySetAscii(name)
                ? KingdomQuestPineExpressionResolution.Success
                : KingdomQuestPineExpressionResolution.Invalid;
        }

        public static bool TryParseUsedExpression(
            string expression,
            out string identifier)
        {
            identifier = null;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string text = expression.Trim();
            const string prefix = "@CharName(";
            if (!text.StartsWith(prefix, StringComparison.Ordinal) ||
                !text.EndsWith(")", StringComparison.Ordinal))
                return false;

            string operand =
                text.Substring(prefix.Length, text.Length - prefix.Length - 1)
                    .Trim();
            return KingdomQuestPineBasicExpression.TrySimpleIdentifier(
                operand, out identifier);
        }
    }
}
