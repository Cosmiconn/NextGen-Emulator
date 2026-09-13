using System;
using System.Collections.Generic;
using System.Globalization;

namespace NextGen.FiestaLib.Data
{
    public enum QuestScriptStage
    {
        Start,
        Action,
        Finish
    }

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
            {
                return new QuestScriptInstruction
                {
                    LineNumber = lineNumber,
                    OpCode = "LABEL",
                    Arguments = string.Empty,
                    Label = source.Substring(1).Trim(),
                    Source = source
                };
            }

            int split = source.IndexOfAny(new[] { ' ', '\t' });
            string op = split < 0 ? source : source.Substring(0, split);
            string args = split < 0 ? string.Empty : source.Substring(split).Trim();
            return new QuestScriptInstruction
            {
                LineNumber = lineNumber,
                OpCode = op.ToUpperInvariant(),
                Arguments = args,
                Label = null,
                Source = source
            };
        }
    }

    public sealed class QuestScriptProgram
    {
        private readonly Dictionary<string, int> _labels;

        public IReadOnlyList<QuestScriptInstruction> Instructions { get; private set; }
        public IReadOnlyDictionary<string, int> Labels { get { return _labels; } }
        public IList<string> ParseWarnings { get; private set; }

        private QuestScriptProgram(List<QuestScriptInstruction> instructions,
            Dictionary<string, int> labels, IList<string> warnings)
        {
            Instructions = instructions.AsReadOnly();
            _labels = labels;
            ParseWarnings = new List<string>(warnings).AsReadOnly();
        }

        public static QuestScriptProgram Parse(string script)
        {
            var instructions = new List<QuestScriptInstruction>();
            var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(script))
                return new QuestScriptProgram(instructions, labels, warnings);

            string[] lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith(";", StringComparison.Ordinal))
                    continue;

                QuestScriptInstruction instruction = QuestScriptInstruction.Parse(i + 1, lines[i]);
                if (instruction == null) continue;

                int index = instructions.Count;
                instructions.Add(instruction);
                if (instruction.OpCode == "LABEL")
                {
                    if (instruction.Label.Length == 0)
                        warnings.Add("Line " + instruction.LineNumber + ": empty label.");
                    else if (labels.ContainsKey(instruction.Label))
                        warnings.Add("Line " + instruction.LineNumber + ": duplicate label '" + instruction.Label + "'.");
                    else
                        labels.Add(instruction.Label, index);
                }
            }

            foreach (QuestScriptInstruction instruction in instructions)
            {
                if (instruction.OpCode == "GOTO")
                {
                    string target = FirstToken(instruction.Arguments);
                    if (target.Length == 0 || !labels.ContainsKey(target))
                        warnings.Add("Line " + instruction.LineNumber + ": GOTO target '" + target + "' is not defined.");
                }
                else if (instruction.OpCode == "IF")
                {
                    string target = ExtractGotoTarget(instruction.Arguments);
                    if (target.Length == 0 || !labels.ContainsKey(target))
                        warnings.Add("Line " + instruction.LineNumber + ": IF GOTO target '" + target + "' is not defined.");
                }
            }

            return new QuestScriptProgram(instructions, labels, warnings);
        }

        internal bool TryGetLabelIndex(string label, out int index)
        {
            return _labels.TryGetValue(label, out index);
        }

        private static string FirstToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? string.Empty : parts[0];
        }

        internal static string ExtractGotoTarget(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string[] parts = text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < parts.Length; i++)
                if (parts[i].Equals("GOTO", StringComparison.OrdinalIgnoreCase))
                    return parts[i + 1];
            return string.Empty;
        }
    }

    public sealed class QuestScriptGraph
    {
        private readonly Dictionary<string, List<Tuple<QuestScriptStage, int>>> _labels =
            new Dictionary<string, List<Tuple<QuestScriptStage, int>>>(StringComparer.OrdinalIgnoreCase);

        public QuestScriptInfo Info { get; private set; }
        public IList<string> ResolutionWarnings { get; private set; }

        private QuestScriptGraph(QuestScriptInfo info)
        {
            Info = info;
            ResolutionWarnings = new List<string>();
            Add(QuestScriptStage.Start, info.Start);
            Add(QuestScriptStage.Action, info.Action);
            Add(QuestScriptStage.Finish, info.Finish);
        }

        public static QuestScriptGraph Create(QuestScriptInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");
            return new QuestScriptGraph(info);
        }

        private void Add(QuestScriptStage stage, QuestScriptProgram program)
        {
            foreach (var pair in program.Labels)
            {
                List<Tuple<QuestScriptStage, int>> list;
                if (!_labels.TryGetValue(pair.Key, out list))
                {
                    list = new List<Tuple<QuestScriptStage, int>>();
                    _labels.Add(pair.Key, list);
                }
                list.Add(Tuple.Create(stage, pair.Value));
            }
        }

        public bool TryResolve(QuestScriptStage currentStage, string label, out QuestScriptStage stage, out int index, out bool ambiguous)
        {
            stage = currentStage;
            index = -1;
            ambiguous = false;

            if (string.IsNullOrWhiteSpace(label)) return false;

            QuestScriptProgram local = GetProgram(currentStage);
            if (local.TryGetLabelIndex(label, out index))
                return true;

            List<Tuple<QuestScriptStage, int>> matches;
            if (!_labels.TryGetValue(label, out matches) || matches.Count == 0)
                return false;

            if (matches.Count != 1)
            {
                ambiguous = true;
                return false;
            }

            stage = matches[0].Item1;
            index = matches[0].Item2;
            return true;
        }

        public QuestScriptProgram GetProgram(QuestScriptStage stage)
        {
            switch (stage)
            {
                case QuestScriptStage.Start: return Info.Start;
                case QuestScriptStage.Action: return Info.Action;
                case QuestScriptStage.Finish: return Info.Finish;
                default: throw new ArgumentOutOfRangeException("stage");
            }
        }
    }

    public sealed class QuestScriptState
    {
        private readonly Dictionary<string, int> _variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public QuestScriptStage Stage { get; internal set; }
        public int InstructionIndex { get; set; }
        public int Result { get; set; }
        public bool Ended { get; internal set; }
        public IDictionary<string, int> Variables { get { return _variables; } }
    }

    public enum QuestScriptStepType
    {
        Continue,
        Say,
        Command,
        End,
        Error
    }

    public sealed class QuestScriptStep
    {
        public QuestScriptStepType Type { get; private set; }
        public QuestScriptInstruction Instruction { get; private set; }
        public string Error { get; private set; }

        private QuestScriptStep() { }

        internal static QuestScriptStep Create(QuestScriptStepType type, QuestScriptInstruction instruction, string error)
        {
            return new QuestScriptStep { Type = type, Instruction = instruction, Error = error };
        }
    }

    public sealed class QuestScriptMachine
    {
        public QuestScriptGraph Graph { get; private set; }
        public QuestScriptState State { get; private set; }

        public QuestScriptMachine(QuestScriptProgram program)
        {
            if (program == null) throw new ArgumentNullException("program");
            var info = new QuestScriptInfo
            {
                QuestID = 0,
                StartScript = string.Empty,
                ActionScript = string.Empty,
                FinishScript = string.Empty,
                Start = QuestScriptProgram.Parse(string.Empty),
                Action = QuestScriptProgram.Parse(string.Empty),
                Finish = QuestScriptProgram.Parse(string.Empty)
            };
            info.Start = program;
            Graph = QuestScriptGraph.Create(info);
            State = new QuestScriptState { Stage = QuestScriptStage.Start, InstructionIndex = 0 };
        }

        public QuestScriptMachine(QuestScriptInfo info, QuestScriptStage stage)
        {
            if (info == null) throw new ArgumentNullException("info");
            Graph = QuestScriptGraph.Create(info);
            State = new QuestScriptState { Stage = stage, InstructionIndex = 0 };
        }

        public QuestScriptStep Next()
        {
            if (State.Ended)
                return QuestScriptStep.Create(QuestScriptStepType.End, null, null);

            int guard = 0;
            while (true)
            {
                QuestScriptProgram program = Graph.GetProgram(State.Stage);
                if (State.InstructionIndex >= program.Instructions.Count)
                {
                    State.Ended = true;
                    return QuestScriptStep.Create(QuestScriptStepType.End, null, null);
                }

                if (++guard > 10000)
                {
                    State.Ended = true;
                    return QuestScriptStep.Create(QuestScriptStepType.Error, null, "Quest script exceeded the control-flow guard.");
                }

                QuestScriptInstruction instruction = program.Instructions[State.InstructionIndex++];
                string op = instruction.OpCode;

                if (op == "LABEL") continue;
                if (op == "END")
                {
                    State.Ended = true;
                    return QuestScriptStep.Create(QuestScriptStepType.End, instruction, null);
                }
                if (op == "GOTO")
                {
                    if (!Jump(FirstToken(instruction.Arguments)))
                        return Error(instruction, "Undefined or ambiguous GOTO label.");
                    continue;
                }
                if (op == "IF")
                {
                    if (EvaluateIf(instruction.Arguments))
                    {
                        string label = QuestScriptProgram.ExtractGotoTarget(instruction.Arguments);
                        if (!Jump(label))
                            return Error(instruction, "Undefined or ambiguous IF/GOTO label.");
                    }
                    continue;
                }
                if (op == "SAY")
                    return QuestScriptStep.Create(QuestScriptStepType.Say, instruction, null);

                return QuestScriptStep.Create(QuestScriptStepType.Command, instruction, null);
            }
        }

        private bool Jump(string label)
        {
            QuestScriptStage stage;
            int index;
            bool ambiguous;
            if (!Graph.TryResolve(State.Stage, label, out stage, out index, out ambiguous))
                return false;
            State.Stage = stage;
            State.InstructionIndex = index;
            return true;
        }

        private QuestScriptStep Error(QuestScriptInstruction instruction, string message)
        {
            State.Ended = true;
            return QuestScriptStep.Create(QuestScriptStepType.Error, instruction, message);
        }

        private bool EvaluateIf(string args)
        {
            string[] parts = args.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || !parts[parts.Length - 2].Equals("GOTO", StringComparison.OrdinalIgnoreCase))
                return false;

            string leftToken = parts[0];
            string op = parts[1];
            int right;
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out right))
                return false;

            int left;
            if (leftToken.Equals("RESULT", StringComparison.OrdinalIgnoreCase))
                left = State.Result;
            else if (!State.Variables.TryGetValue(leftToken, out left))
                left = 0;

            switch (op)
            {
                case "==": return left == right;
                case "!=": return left != right;
                case "<": return left < right;
                case "<=": return left <= right;
                case ">": return left > right;
                case ">=": return left >= right;
                default: return false;
            }
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
        public uint QuestID { get; internal set; }
        public string StartScript { get; internal set; }
        public string ActionScript { get; internal set; }
        public string FinishScript { get; internal set; }
        public QuestScriptProgram Start { get; internal set; }
        public QuestScriptProgram Action { get; internal set; }
        public QuestScriptProgram Finish { get; internal set; }

        public static QuestScriptInfo Load(System.Data.DataRow row)
        {
            string start = row["StartScript"] == DBNull.Value ? string.Empty : (string)row["StartScript"];
            string action = row["ActionScript"] == DBNull.Value ? string.Empty : (string)row["ActionScript"];
            string finish = row["FinishScript"] == DBNull.Value ? string.Empty : (string)row["FinishScript"];
            return new QuestScriptInfo
            {
                QuestID = NextGen.Database.DataStore.GetDataTypes.GetUint(row["QuestID"]),
                StartScript = start,
                ActionScript = action,
                FinishScript = finish,
                Start = QuestScriptProgram.Parse(start),
                Action = QuestScriptProgram.Parse(action),
                Finish = QuestScriptProgram.Parse(finish)
            };
        }
    }
}
