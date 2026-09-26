using System;
using System.Collections.Generic;

namespace NextGen.Zone.Data
{
    public enum KingdomQuestPineRuntimeStatus : byte
    {
        Running = 1,
        Completed = 2,
        Faulted = 3,
    }

    /// <summary>
    /// Host boundary for Pine behavior that is not structural control flow.
    ///
    /// The control runtime owns only Zone.exe-proven Block/IF/INFINITE/CALL/
    /// BREAK stack semantics. Expressions and gameplay commands stay outside
    /// until their corresponding native Pine node behavior is recovered.
    /// Returning false is fail-closed and faults the runtime.
    /// </summary>
    public interface IKingdomQuestPineRuntimeHost
    {
        bool TryEvaluateCondition(string expression, out int value);

        bool TryResolveIdentifier(
            string identifierExpression,
            out string identifier);

        bool TryCalculateExpression(
            string expression,
            KingdomQuestPineTokenValue destination);

        bool TryStepCommand(
            string commandText,
            int canonicalLine,
            ref int nativeState,
            out bool completed);
    }

    /// <summary>
    /// Structural Pine execution stack recovered directly from the supplied
    /// Zone.exe/PDB.
    ///
    /// Native anchors:
    ///   PineScriptStack::ProcessStack::ps_Push      0x004D6BE0
    ///   PineScriptStack::ProcessStack::ps_Pop       0x004D6C20
    ///   PineScriptStack::ProcessStack::ps_ExitBlock 0x004D8C70
    ///   PineEventScriptNode::Block::sa_Step         0x004D81B0
    ///   PineEventScriptNode::StateInfinite::sa_Step 0x004D82C0
    ///   PineEventScriptNode::StateIf::sa_Step       0x004D8400
    ///   PineEventScriptNode::StateBreak::sa_Step    0x004DA120
    ///   PineEventScriptNode::StateCall::sa_Step     0x004DA180
    ///
    /// The native ProcessStack starts with frame index 0 and refuses a push
    /// when the current frame index is already 0x1F, so at most 32 frames
    /// (0..31) can be represented. A Block increments its statement index
    /// before pushing that statement. IF evaluates once, pushes exactly one
    /// branch and removes itself after that branch returns. INFINITE repeatedly
    /// pushes its body. CALL resolves a named top-level block, pushes it once,
    /// then removes itself when the callee returns. BREAK walks outward until
    /// a named Block accepts the supplied name and sets that Block's statement
    /// index to native 0x270F (9999), causing normal Block completion.
    ///
    /// This class deliberately executes no Fiesta gameplay command.
    /// </summary>
    public sealed class KingdomQuestPineControlRuntime
    {
        private const int NativeMaxFrameIndex = 0x1F;
        private const int NativeBreakExitIndex = 0x270F;

        private enum FrameKind : byte
        {
            Sequence = 1,
            Node = 2,
        }

        private sealed class Frame
        {
            public FrameKind Kind;
            public string Name;
            public IReadOnlyList<KingdomQuestPineNodeSource> Statements;
            public KingdomQuestPineNodeSource Node;
            public int State;

            public static Frame Sequence(
                string name,
                IReadOnlyList<KingdomQuestPineNodeSource> statements)
            {
                return new Frame
                {
                    Kind = FrameKind.Sequence,
                    Name = name,
                    Statements = statements ??
                        new KingdomQuestPineNodeSource[0],
                    State = 0,
                };
            }

            public static Frame NodeFrame(KingdomQuestPineNodeSource node)
            {
                return new Frame
                {
                    Kind = FrameKind.Node,
                    Node = node,
                    State = 0,
                };
            }
        }

        private readonly KingdomQuestPineScriptDocument document;
        private readonly IKingdomQuestPineRuntimeHost host;
        private readonly List<Frame> stack;
        private readonly KingdomQuestPineVariableStack variables;
        private string fault;

        public KingdomQuestPineRuntimeStatus Status { get; private set; }

        public string Fault
        {
            get { return fault ?? string.Empty; }
        }

        public int FrameCount
        {
            get { return stack.Count; }
        }

        public KingdomQuestPineVariableStack Variables
        {
            get { return variables; }
        }

        private KingdomQuestPineControlRuntime(
            KingdomQuestPineScriptDocument document,
            IKingdomQuestPineRuntimeHost host)
        {
            this.document = document;
            this.host = host;
            stack = new List<Frame>(NativeMaxFrameIndex + 1);
            variables = new KingdomQuestPineVariableStack();
            Status = KingdomQuestPineRuntimeStatus.Running;
        }

        public static bool TryCreate(
            KingdomQuestPineScriptDocument document,
            IKingdomQuestPineRuntimeHost host,
            string entryBlock,
            out KingdomQuestPineControlRuntime runtime)
        {
            runtime = null;
            if (document == null ||
                host == null ||
                string.IsNullOrEmpty(entryBlock))
                return false;

            KingdomQuestPineBlockSource block;
            if (!document.Blocks.TryGetValue(entryBlock, out block) ||
                block == null)
                return false;

            runtime = new KingdomQuestPineControlRuntime(document, host);
            runtime.stack.Add(Frame.Sequence(block.Name, block.Statements));
            return true;
        }

        /// <summary>
        /// Performs one native-style top-frame step. It does not spin through
        /// multiple statements in one call. Callers therefore retain control of
        /// scheduling/yield cadence exactly at the statement boundary.
        /// </summary>
        public KingdomQuestPineRuntimeStatus Step()
        {
            if (Status != KingdomQuestPineRuntimeStatus.Running)
                return Status;

            if (stack.Count == 0)
            {
                Status = KingdomQuestPineRuntimeStatus.Completed;
                return Status;
            }

            Frame frame = stack[stack.Count - 1];
            if (frame.Kind == FrameKind.Sequence)
                StepSequence(frame);
            else
                StepNode(frame);

            if (Status == KingdomQuestPineRuntimeStatus.Running &&
                stack.Count == 0)
                Status = KingdomQuestPineRuntimeStatus.Completed;

            return Status;
        }

        private void StepSequence(Frame frame)
        {
            if (frame.Statements == null)
            {
                Fail("Pine sequence has no statement source.");
                return;
            }

            if (frame.State >= frame.Statements.Count ||
                frame.State >= NativeBreakExitIndex)
            {
                Pop();
                return;
            }

            KingdomQuestPineNodeSource node =
                frame.Statements[frame.State];
            frame.State++;

            if (node == null)
            {
                Fail("Pine sequence contains a null statement.");
                return;
            }

            // A named nested OPEN block is itself the pushed Block frame in
            // native Pine; do not add an emulator-only wrapper frame.
            if (node.Kind == KingdomQuestPineNodeKind.Scope)
            {
                PushSequence(node.Text, node.Children);
                return;
            }

            Push(Frame.NodeFrame(node));
        }

        private void StepNode(Frame frame)
        {
            KingdomQuestPineNodeSource node = frame.Node;
            if (node == null)
            {
                Fail("Pine node frame has no node.");
                return;
            }

            switch (node.Kind)
            {
                case KingdomQuestPineNodeKind.If:
                    StepIf(frame, node);
                    break;

                case KingdomQuestPineNodeKind.Infinite:
                    StepInfinite(node);
                    break;

                case KingdomQuestPineNodeKind.Command:
                    StepCommand(frame, node);
                    break;

                case KingdomQuestPineNodeKind.VariableDeclaration:
                    StepVariableDeclaration(node);
                    break;

                case KingdomQuestPineNodeKind.Assignment:
                    StepAssignment(node);
                    break;

                case KingdomQuestPineNodeKind.Scope:
                    Fail("Named Pine scope reached node-frame path.");
                    break;

                default:
                    Fail("Unknown Pine node kind.");
                    break;
            }
        }

        private void StepIf(
            Frame frame,
            KingdomQuestPineNodeSource node)
        {
            if (frame.State != 0)
            {
                // Native StateIf removes its own frame when the chosen child
                // Block returns.
                Pop();
                return;
            }

            int value;
            if (!host.TryEvaluateCondition(node.Text, out value))
            {
                Fail(
                    "Unresolved Pine IF expression at canonical line " +
                    node.CanonicalLine + ".");
                return;
            }

            frame.State = 1;
            IReadOnlyList<KingdomQuestPineNodeSource> chosen =
                value != 0 ? node.Children : node.ElseChildren;

            if (chosen == null || chosen.Count == 0)
            {
                Pop();
                return;
            }

            PushSequence(null, chosen);
        }

        private void StepInfinite(KingdomQuestPineNodeSource node)
        {
            // Native StateInfinite never advances a state member. Whenever its
            // child Block returns, the same frame pushes that body again.
            PushSequence(null, node.Children);
        }

        private void StepVariableDeclaration(
            KingdomQuestPineNodeSource node)
        {
            // StateVarDeclear::sa_Step (0x004DA040) loops every declaration
            // in this one statement. For each entry it resolves the identifier,
            // pushes it to VariableStack, then calculates the initializer
            // directly into the returned value-token storage.
            if (node.Declarations == null ||
                node.Declarations.Count == 0)
            {
                Fail(
                    "Pine variable declaration has no source entries at " +
                    "canonical line " + node.CanonicalLine + ".");
                return;
            }

            for (int i = 0; i < node.Declarations.Count; i++)
            {
                KingdomQuestPineVariableDeclarationSource declaration =
                    node.Declarations[i];
                if (declaration == null ||
                    string.IsNullOrEmpty(declaration.Name))
                {
                    Fail(
                        "Invalid Pine variable declaration at canonical line " +
                        node.CanonicalLine + ".");
                    return;
                }

                KingdomQuestPineTokenValue destination;
                if (!variables.TryPush(
                        declaration.Name, out destination))
                {
                    Fail(
                        "Native Pine VariableStack push failed for " +
                        declaration.Name + " at canonical line " +
                        declaration.CanonicalLine + ".");
                    return;
                }

                if (!TryCalculateExpression(
                        declaration.InitializerExpression,
                        destination))
                {
                    // Native has already pushed this entry before invoking the
                    // expression calculate virtual. Preserve that order rather
                    // than rolling the stack back on failure.
                    Fail(
                        "Unresolved Pine variable initializer at canonical " +
                        "line " + declaration.CanonicalLine + ": " +
                        declaration.InitializerExpression);
                    return;
                }
            }

            Pop();
        }

        private void StepAssignment(
            KingdomQuestPineNodeSource node)
        {
            // StateAssignment::sa_Step (0x004DB110) resolves the LHS Identify
            // token, finds the newest matching VariableStack entry, calculates
            // the RHS directly into that existing value token, then pops.
            string identifier;
            if (!KingdomQuestPineBasicExpression.TrySimpleIdentifier(
                    node.AssignmentTarget, out identifier) &&
                (!host.TryResolveIdentifier(
                    node.AssignmentTarget,
                    out identifier) ||
                 string.IsNullOrEmpty(identifier)))
            {
                Fail(
                    "Unresolved Pine assignment target at canonical line " +
                    node.CanonicalLine + ": " + node.AssignmentTarget);
                return;
            }

            KingdomQuestPineTokenValue destination;
            if (!variables.TryFind(identifier, out destination))
            {
                Fail(
                    "Pine assignment variable not found at canonical line " +
                    node.CanonicalLine + ": " + identifier);
                return;
            }

            if (!TryCalculateExpression(
                    node.AssignmentExpression,
                    destination))
            {
                Fail(
                    "Unresolved Pine assignment expression at canonical line " +
                    node.CanonicalLine + ": " +
                    node.AssignmentExpression);
                return;
            }

            Pop();
        }

        private bool TryCalculateExpression(
            string expression,
            KingdomQuestPineTokenValue destination)
        {
            KingdomQuestPineExpressionResolution resolution =
                KingdomQuestPineBasicExpression.TryCalculate(
                    expression, variables, destination);
            if (resolution ==
                KingdomQuestPineExpressionResolution.Success)
                return true;
            if (resolution ==
                KingdomQuestPineExpressionResolution.Invalid)
                return false;

            return host.TryCalculateExpression(
                expression, destination);
        }

        private void StepCommand(
            Frame frame,
            KingdomQuestPineNodeSource node)
        {
            string target;
            if (TryQuotedControl(node.Text, "call", out target))
            {
                StepCall(frame, node, target);
                return;
            }

            if (TryQuotedControl(node.Text, "break", out target))
            {
                StepBreak(node, target);
                return;
            }

            bool completed;
            int state = frame.State;
            if (!host.TryStepCommand(
                    node.Text,
                    node.CanonicalLine,
                    ref state,
                    out completed))
            {
                Fail(
                    "Unresolved Pine command at canonical line " +
                    node.CanonicalLine + ": " + node.Text);
                return;
            }

            frame.State = state;
            if (completed)
                Pop();
        }

        private void StepCall(
            Frame frame,
            KingdomQuestPineNodeSource node,
            string target)
        {
            if (frame.State != 0)
            {
                // Native StateCall removes its own frame after the named block
                // returns.
                Pop();
                return;
            }

            KingdomQuestPineBlockSource block;
            if (!document.Blocks.TryGetValue(target, out block) ||
                block == null)
            {
                Fail(
                    "Pine CALL target not found at canonical line " +
                    node.CanonicalLine + ": " + target);
                return;
            }

            frame.State = 1;
            PushSequence(block.Name, block.Statements);
        }

        private void StepBreak(
            KingdomQuestPineNodeSource node,
            string target)
        {
            // Native ps_ExitBlock starts at the current BREAK statement and
            // walks outward until a Block's sa_BlockNameCheck accepts target.
            // The BREAK statement and any intervening frames disappear.
            int match = -1;
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                Frame candidate = stack[i];
                if (candidate.Kind != FrameKind.Sequence)
                    continue;

                if (target == null ||
                    string.Equals(
                        candidate.Name,
                        target,
                        StringComparison.Ordinal))
                {
                    match = i;
                    break;
                }
            }

            if (match < 0)
            {
                Fail(
                    "Pine BREAK target not found at canonical line " +
                    node.CanonicalLine + ": " + (target ?? "<nearest>"));
                return;
            }

            if (match + 1 < stack.Count)
                stack.RemoveRange(match + 1, stack.Count - match - 1);

            // ps_ExitBlock writes exactly 0x270F to the accepted Block frame's
            // statement index. The next Block step performs normal completion.
            stack[match].State = NativeBreakExitIndex;
        }

        private void PushSequence(
            string name,
            IReadOnlyList<KingdomQuestPineNodeSource> statements)
        {
            Push(Frame.Sequence(name, statements));
        }

        private void Push(Frame frame)
        {
            if (frame == null)
            {
                Fail("Pine attempted to push a null frame.");
                return;
            }

            // With an initial frame at index 0, native ps_Push rejects a push
            // once the current index is already 0x1F.
            if (stack.Count - 1 >= NativeMaxFrameIndex)
            {
                Fail("Native Pine ProcessStack frame capacity exceeded.");
                return;
            }

            stack.Add(frame);
        }

        private void Pop()
        {
            if (stack.Count != 0)
                stack.RemoveAt(stack.Count - 1);
        }

        private void Fail(string message)
        {
            fault = message ?? "Unknown Pine runtime fault.";
            Status = KingdomQuestPineRuntimeStatus.Faulted;
        }

        private static bool TryQuotedControl(
            string text,
            string keyword,
            out string target)
        {
            target = null;
            if (text == null || keyword == null)
                return false;

            string prefix = keyword + " \"";
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            int end = text.IndexOf('"', prefix.Length);
            if (end < prefix.Length)
                return false;

            string tail = text.Substring(end + 1).Trim();
            if (tail.Length != 0 && tail != ".")
                return false;

            target = text.Substring(
                prefix.Length, end - prefix.Length);
            return target.Length != 0;
        }
    }
}
