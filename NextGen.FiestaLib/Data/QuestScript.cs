using System;
using System.Collections.Generic;
using System.Globalization;

namespace NextGen.FiestaLib.Data
{
    public enum QuestScriptStage { Start, Action, Finish }
    public sealed class QuestScriptInstruction
    {
        public int LineNumber { get; private set; }
        public string OpCode { get; private set; }
        public string Arguments { get; private set; }
        public string Label { get; private set; }
        public string Source { get; private set; }
        private QuestScriptInstruction() { }
        public static QuestScriptInstruction Parse(int lineNumber, string line)
        {
            string source = (line ?? string.Empty).Trim();
            if (source.Length == 0) return null;
            if (source.StartsWith(":", StringComparison.Ordinal))
                return new QuestScriptInstruction { LineNumber = lineNumber, OpCode = "LABEL", Arguments = string.Empty, Label = source.Substring(1).Trim(), Source = source };
            int split = source.IndexOfAny(new[] { ' ', '\t' });
            string op = split < 0 ? source : source.Substring(0, split);
            string args = split < 0 ? string.Empty : source.Substring(split).Trim();
            return new QuestScriptInstruction { LineNumber = lineNumber, OpCode = op.ToUpperInvariant(), Arguments = args, Label = null, Source = source };
        }
    }
    public sealed class QuestScriptProgram
    {
        private readonly Dictionary<string, int> _labels;
        public IReadOnlyList<QuestScriptInstruction> Instructions { get; private set; }
        public IReadOnlyDictionary<string, int> Labels { get { return _labels; } }
        public IList<string> ParseWarnings { get; private set; }
        private QuestScriptProgram(List<QuestScriptInstruction> instructions, Dictionary<string, int> labels, IList<string> warnings)
        { Instructions = instructions.AsReadOnly(); _labels = labels; ParseWarnings = new List<string>(warnings).AsReadOnly(); }
        public static QuestScriptProgram Parse(string script)
        {
            var instructions = new List<QuestScriptInstruction>();
            var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(script)) return new QuestScriptProgram(instructions, labels, warnings);
            string[] lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith(";", StringComparison.Ordinal)) continue;
                QuestScriptInstruction instruction = QuestScriptInstruction.Parse(i + 1, lines[i]);
                int index = instructions.Count; instructions.Add(instruction);
                if (instruction.OpCode == "LABEL")
                {
                    if (instruction.Label.Length == 0) warnings.Add("Line " + instruction.LineNumber + ": empty label.");
                    else if (labels.ContainsKey(instruction.Label)) warnings.Add("Line " + instruction.LineNumber + ": duplicate label '" + instruction.Label + "'.");
                    else labels.Add(instruction.Label, index);
                }
            }
            foreach (QuestScriptInstruction instruction in instructions)
            {
                if (instruction.OpCode == "GOTO")
                {
                    string target = FirstToken(instruction.Arguments);
                    if (target.Length == 0 || !labels.ContainsKey(target)) warnings.Add("Line " + instruction.LineNumber + ": GOTO target '" + target + "' is not defined.");
                }
                else if (instruction.OpCode == "IF")
                {
                    string target = ExtractGotoTarget(instruction.Arguments);
                    if (target.Length == 0 || !labels.ContainsKey(target)) warnings.Add("Line " + instruction.LineNumber + ": IF GOTO target '" + target + "' is not defined.");
                }
            }
            return new QuestScriptProgram(instructions, labels, warnings);
        }
        internal bool TryGetLabelIndex(string label, out int index) { return _labels.TryGetValue(label, out index); }
        internal static string ExtractGotoTarget(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < parts.Length; i++) if (parts[i].Equals("GOTO", StringComparison.OrdinalIgnoreCase)) return parts[i + 1];
            return string.Empty;
        }
        private static string FirstToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? string.Empty : parts[0];
        }
    }
    public sealed class QuestScriptState
    {
        private readonly Dictionary<string, int> _variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public int InstructionIndex { get; internal set; }
        public int Result { get; set; }
        public bool Ended { get; internal set; }
        public IDictionary<string, int> Variables { get { return _variables; } }
    }
    public enum QuestScriptStepType { Continue, Say, Command, End, Error }
    public sealed class QuestScriptStep
    {
        public QuestScriptStepType Type { get; private set; }
        public QuestScriptInstruction Instruction { get; private set; }
        public string Error { get; private set; }
        private QuestScriptStep() { }
        internal static QuestScriptStep Create(QuestScriptStepType type, QuestScriptInstruction instruction, string error)
        { return new QuestScriptStep { Type = type, Instruction = instruction, Error = error }; }
    }
    public sealed class QuestScriptMachine
    {
        public QuestScriptProgram Program { get; private set; }
        public QuestScriptState State { get; private set; }
        public QuestScriptMachine(QuestScriptProgram program) { if (program == null) throw new ArgumentNullException("program"); Program = program; State = new QuestScriptState(); }
        public QuestScriptStep Next()
        {
            if (State.Ended) return QuestScriptStep.Create(QuestScriptStepType.End, null, null);
            int guard = 0;
            while (State.InstructionIndex < Program.Instructions.Count)
            {
                if (++guard > 10000) { State.Ended = true; return QuestScriptStep.Create(QuestScriptStepType.Error, null, "Quest script exceeded the control-flow guard."); }
                QuestScriptInstruction instruction = Program.Instructions[State.InstructionIndex++];
                string op = instruction.OpCode;
                if (op == "LABEL") continue;
                if (op == "END") { State.Ended = true; return QuestScriptStep.Create(QuestScriptStepType.End, instruction, null); }
                if (op == "GOTO")
                {
                    int target; if (!Program.TryGetLabelIndex(FirstToken(instruction.Arguments), out target)) return Error(instruction, "Undefined GOTO label.");
                    State.InstructionIndex = target; continue;
                }
                if (op == "IF")
                {
                    if (EvaluateIf(instruction.Arguments))
                    {
                        string label = QuestScriptProgram.ExtractGotoTarget(instruction.Arguments); int target;
                        if (!Program.TryGetLabelIndex(label, out target)) return Error(instruction, "Undefined IF/GOTO label.");
                        State.InstructionIndex = target;
                    }
                    continue;
                }
                if (op == "SAY") return QuestScriptStep.Create(QuestScriptStepType.Say, instruction, null);
                return QuestScriptStep.Create(QuestScriptStepType.Command, instruction, null);
            }
            State.Ended = true; return QuestScriptStep.Create(QuestScriptStepType.End, null, null);
        }
        private QuestScriptStep Error(QuestScriptInstruction instruction, string message) { State.Ended = true; return QuestScriptStep.Create(QuestScriptStepType.Error, instruction, message); }
        private bool EvaluateIf(string args)
        {
            string[] parts = args.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || !parts[parts.Length - 2].Equals("GOTO", StringComparison.OrdinalIgnoreCase)) return false;
            int right; if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out right)) return false;
            int left;
            if (parts[0].Equals("RESULT", StringComparison.OrdinalIgnoreCase)) left = State.Result;
            else if (!State.Variables.TryGetValue(parts[0], out left)) left = 0;
            switch (parts[1]) { case "==": return left == right; case "!=": return left != right; case "<": return left < right; case "<=": return left <= right; case ">": return left > right; case ">=": return left >= right; default: return false; }
        }
        private static string FirstToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? string.Empty : parts[0];
        }
    }
    public sealed class QuestScriptInfo
    {
        public uint QuestID { get; private set; }
        public string StartScript { get; private set; }
        public string ActionScript { get; private set; }
        public string FinishScript { get; private set; }
        public QuestScriptProgram Start { get; private set; }
        public QuestScriptProgram Action { get; private set; }
        public QuestScriptProgram Finish { get; private set; }
        public static QuestScriptInfo Load(System.Data.DataRow row)
        {
            string start = row["StartScript"] == DBNull.Value ? string.Empty : (string)row["StartScript"];
            string action = row["ActionScript"] == DBNull.Value ? string.Empty : (string)row["ActionScript"];
            string finish = row["FinishScript"] == DBNull.Value ? string.Empty : (string)row["FinishScript"];
            return new QuestScriptInfo { QuestID = NextGen.Database.DataStore.GetDataTypes.GetUint(row["QuestID"]), StartScript = start, ActionScript = action, FinishScript = finish, Start = QuestScriptProgram.Parse(start), Action = QuestScriptProgram.Parse(action), Finish = QuestScriptProgram.Parse(finish) };
        }
    }
}
