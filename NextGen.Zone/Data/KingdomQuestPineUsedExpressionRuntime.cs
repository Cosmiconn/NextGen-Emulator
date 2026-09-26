using System;
using NextGen.FiestaLib.Data;

namespace NextGen.Zone.Data
{
    /// <summary>
    /// Explicit dependencies for the source-proven system functions used by the
    /// supplied Pine KQ corpus.
    ///
    /// The context deliberately does not create or infer any dependency:
    /// - CRT state must be supplied by the caller;
    /// - native ShineObject handle resolution stays behind explicit resolvers;
    /// - missing dependencies remain fail-closed for a recognized used
    ///   expression.
    /// </summary>
    public sealed class KingdomQuestPineUsedExpressionContext
    {
        public MsvcCrtRand Random { get; private set; }
        public IKingdomQuestPineNativeObjectCoordinateResolver CoordinateResolver
        {
            get;
            private set;
        }
        public IKingdomQuestPineNativeObjectNameResolver NameResolver
        {
            get;
            private set;
        }

        public KingdomQuestPineUsedExpressionContext(
            MsvcCrtRand random,
            IKingdomQuestPineNativeObjectCoordinateResolver coordinateResolver,
            IKingdomQuestPineNativeObjectNameResolver nameResolver)
        {
            Random = random;
            CoordinateResolver = coordinateResolver;
            NameResolver = nameResolver;
        }
    }

    /// <summary>
    /// Dispatches only the system-function forms proven to occur in the exact
    /// nine-script Pine KQ corpus.
    ///
    /// A recognized expression with a missing explicit native dependency is
    /// Invalid, not Unsupported. That prevents the control runtime from
    /// silently falling through to a generic host implementation for behavior
    /// whose native semantics are already known.
    /// </summary>
    public static class KingdomQuestPineUsedExpressionRuntime
    {
        public static KingdomQuestPineExpressionResolution TryCalculate(
            string expression,
            KingdomQuestPineVariableStack variables,
            KingdomQuestPineUsedExpressionContext context,
            KingdomQuestPineTokenValue destination)
        {
            if (expression == null ||
                variables == null ||
                destination == null)
                return KingdomQuestPineExpressionResolution.Invalid;

            int minimum;
            int maximum;
            if (KingdomQuestPineRandomExpression.TryParseUsedExpression(
                    expression, out minimum, out maximum))
            {
                if (context == null || context.Random == null)
                    return KingdomQuestPineExpressionResolution.Invalid;

                return KingdomQuestPineRandomExpression.TryCalculateUsed(
                    expression, context.Random, destination);
            }

            string leftIdentifier;
            string rightIdentifier;
            if (KingdomQuestPineDistanceExpression.TryParseUsedExpression(
                    expression, out leftIdentifier, out rightIdentifier))
            {
                if (context == null || context.CoordinateResolver == null)
                    return KingdomQuestPineExpressionResolution.Invalid;

                return KingdomQuestPineDistanceExpression.TryCalculateUsed(
                    expression,
                    variables,
                    context.CoordinateResolver,
                    destination);
            }

            string identifier;
            if (KingdomQuestPineCharNameExpression.TryParseUsedExpression(
                    expression, out identifier))
            {
                if (context == null || context.NameResolver == null)
                    return KingdomQuestPineExpressionResolution.Invalid;

                return KingdomQuestPineCharNameExpression.TryCalculateUsed(
                    expression,
                    variables,
                    context.NameResolver,
                    destination);
            }

            return KingdomQuestPineExpressionResolution.Unsupported;
        }
    }
}
