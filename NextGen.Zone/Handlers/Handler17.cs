using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Util;
using NextGen.Zone.Networking;

namespace NextGen.Zone.Handlers
{
    // Header 17 quest-dialog continuation. The NPC -> first-dialog mapping is
    // deliberately not guessed here; it remains a separate data-mapping task.
    // Once a dialog page is sent, button results are fed into the quest script
    // machine and the next SAY is selected from the SQL-backed script corpus.
    public static class Handler17
    {
        private sealed class DialogSession
        {
            public uint DialogID;
            public ushort Seq;
            public QuestScriptMachine Machine;
        }

        private sealed class DialogScriptContext
        {
            public QuestScriptInfo Info;
            public QuestScriptStage Stage;
            public int InstructionIndex;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<int, DialogSession> Sessions = new Dictionary<int, DialogSession>();
        private static Dictionary<uint, QuestScriptInfo> QuestScripts;
        private static Dictionary<uint, DialogScriptContext> DialogContexts;
        private static HashSet<uint> AmbiguousDialogs;

        [PacketHandler(CH17Type.NpcDialogResponse)]
        public static void NpcDialogResponseHandler(ZoneClient client, Packet packet)
        {
            ushort seq;
            byte marker, button;
            if (!packet.TryReadUShort(out seq) || !packet.TryReadByte(out marker) || !packet.TryReadByte(out button))
            {
                Log.WriteLine(LogLevel.Warn, "{0}: ungueltige NpcDialogResponse.", client.Character?.Name);
                return;
            }

            DialogSession session;
            lock (Sync)
            {
                if (!Sessions.TryGetValue(client.Character.ID, out session) || session.Seq != seq)
                    session = null;
            }

            if (session == null)
            {
                Log.WriteLine(LogLevel.Debug, "{0}: NpcDialogResponse ohne (oder mit veralteter) laufende Dialog-Sitzung ignoriert.", client.Character?.Name);
                return;
            }

            string text = ReadDialogText(session.DialogID);
            if (text == null)
            {
                EndDialog(client.Character.ID);
                return;
            }

            if (text.Contains("[MENU]"))
            {
                EndDialog(client.Character.ID);
                return;
            }

            if (session.Machine != null)
            {
                session.Machine.State.Result = button;
                QuestScriptStep step = session.Machine.Next();
                if (step.Type == QuestScriptStepType.Say)
                {
                    uint nextDialogID;
                    if (TryGetSayDialogId(step.Instruction, out nextDialogID))
                    {
                        SendDialogPage(client, nextDialogID, session.Machine);
                        return;
                    }
                }

                if (step.Type == QuestScriptStepType.End)
                {
                    EndDialog(client.Character.ID);
                    return;
                }

                if (step.Type == QuestScriptStepType.Error)
                {
                    Log.WriteLine(LogLevel.Warn, "{0}: QuestScript {1} stopped: {2}",
                        client.Character.Name, session.Machine.Graph.Info.QuestID, step.Error);
                    EndDialog(client.Character.ID);
                    return;
                }
            }

            // Non-quest/ambiguous legacy fallback. No quest semantics are
            // invented when the dialog cannot be mapped uniquely.
            SendDialogPage(client, session.DialogID + 1, null);
        }

        /// <summary>
        /// Sends a verified dialog page and, if its SAY is uniquely attributable
        /// to one quest script stage, attaches a quest-script continuation state.
        /// </summary>
        public static void SendDialogPage(ZoneClient client, uint dialogId)
        {
            DialogScriptContext context;
            QuestScriptMachine machine = null;
            if (TryFindDialogContext(dialogId, out context))
                machine = CreateMachineAtContext(context);
            SendDialogPage(client, dialogId, machine);
        }

        private static void SendDialogPage(ZoneClient client, uint dialogId, QuestScriptMachine machine)
        {
            if (ReadDialogText(dialogId) == null)
            {
                EndDialog(client.Character.ID);
                return;
            }

            DialogSession session;
            lock (Sync)
            {
                DialogSession previous;
                Sessions.TryGetValue(client.Character.ID, out previous);
                ushort nextSeq = previous == null ? (ushort)1 : (ushort)(previous.Seq + 1);
                session = new DialogSession { DialogID = dialogId, Seq = nextSeq, Machine = machine };
                Sessions[client.Character.ID] = session;
            }

            using (var p = new Packet(SH17Type.NpcDialogMenu))
            {
                p.WriteUShort(session.Seq);
                p.WriteUInt(2);
                p.WriteByte(0);
                p.WriteUShort((ushort)dialogId);
                p.Fill(94, 0);
                client.SendPacket(p);
            }
        }

        private static QuestScriptMachine CreateMachineAtContext(DialogScriptContext context)
        {
            var machine = new QuestScriptMachine(context.Info, context.Stage);
            machine.State.InstructionIndex = context.InstructionIndex + 1;
            return machine;
        }

        private static bool TryFindDialogContext(uint dialogId, out DialogScriptContext context)
        {
            EnsureQuestScriptsLoaded();
            lock (Sync)
            {
                if (AmbiguousDialogs.Contains(dialogId))
                {
                    context = null;
                    return false;
                }
                return DialogContexts.TryGetValue(dialogId, out context);
            }
        }

        private static void EnsureQuestScriptsLoaded()
        {
            lock (Sync)
            {
                if (QuestScripts != null) return;
                QuestScripts = new Dictionary<uint, QuestScriptInfo>();
                DialogContexts = new Dictionary<uint, DialogScriptContext>();
                AmbiguousDialogs = new HashSet<uint>();

                try
                {
                    using (DatabaseClient db = Program.DatabaseManager.GetClient())
                    {
                        DataTable data = db.ReadDataTable("SELECT * FROM data_quest_script");
                        if (data == null) return;
                        foreach (DataRow row in data.Rows)
                        {
                            QuestScriptInfo info = QuestScriptInfo.Load(row);
                            if (!QuestScripts.ContainsKey(info.QuestID))
                                QuestScripts.Add(info.QuestID, info);

                            IndexProgram(info, QuestScriptStage.Start, info.Start);
                            IndexProgram(info, QuestScriptStage.Action, info.Action);
                            IndexProgram(info, QuestScriptStage.Finish, info.Finish);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogLevel.Warn, "QuestScript SQL load unavailable for Header 17: {0}", ex.Message);
                }
            }
        }

        private static void IndexProgram(QuestScriptInfo info, QuestScriptStage stage, QuestScriptProgram program)
        {
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                QuestScriptInstruction instruction = program.Instructions[i];
                uint dialogId;
                if (!instruction.OpCode.Equals("SAY", StringComparison.OrdinalIgnoreCase) ||
                    !TryGetSayDialogId(instruction, out dialogId))
                    continue;

                if (AmbiguousDialogs.Contains(dialogId)) continue;
                DialogScriptContext existing;
                if (DialogContexts.TryGetValue(dialogId, out existing))
                {
                    DialogContexts.Remove(dialogId);
                    AmbiguousDialogs.Add(dialogId);
                    continue;
                }

                DialogContexts.Add(dialogId, new DialogScriptContext
                {
                    Info = info,
                    Stage = stage,
                    InstructionIndex = i
                });
            }
        }

        private static string ReadDialogText(uint dialogId)
        {
            try
            {
                using (DatabaseClient db = Program.DatabaseManager.GetClient())
                {
                    DataTable data = db.ReadDataTable("SELECT Text FROM data_questdialog WHERE DialogID=" + dialogId);
                    if (data != null && data.Rows.Count > 0)
                        return (string)data.Rows[0]["Text"];
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Warn, "QuestDialog SQL read failed for {0}: {1}", dialogId, ex.Message);
            }
            return null;
        }

        private static bool TryGetSayDialogId(QuestScriptInstruction instruction, out uint dialogId)
        {
            dialogId = 0;
            if (instruction == null || !instruction.OpCode.Equals("SAY", StringComparison.OrdinalIgnoreCase)) return false;
            string[] parts = instruction.Arguments.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 1 && uint.TryParse(parts[0], out dialogId);
        }

        private static void EndDialog(int characterId)
        {
            lock (Sync)
            {
                Sessions.Remove(characterId);
            }
        }
    }
}
