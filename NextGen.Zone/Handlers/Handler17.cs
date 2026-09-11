using System;
using System.Collections.Generic;
using System.Data;
using NextGen.Database;
using NextGen.FiestaLib;
using NextGen.FiestaLib.Data;
using NextGen.FiestaLib.Networking;
using NextGen.Util;
using NextGen.Zone.Data;
using NextGen.Zone.Networking;

namespace NextGen.Zone.Handlers
{
    public static class Handler17
    {
        private sealed class DialogSession { public uint DialogID; public ushort Seq; public QuestScriptMachine Machine; public ushort PendingScenarioID; public bool ScenarioPending; }
        private sealed class DialogScriptContext { public QuestScriptInfo Info; public QuestScriptStage Stage; public int InstructionIndex; }
        private static readonly object Sync = new object();
        private static readonly Dictionary<int, DialogSession> Sessions = new Dictionary<int, DialogSession>();
        private static Dictionary<uint, DialogScriptContext> DialogContexts;
        private static HashSet<uint> AmbiguousDialogs;

        [PacketHandler(CH17Type.RewardSelectItemIndex)]
        public static void RewardSelectItemIndexHandler(ZoneClient client, Packet packet)
        {
            ushort questId; uint selectedIndex;
            if (!packet.TryReadUShort(out questId) || !packet.TryReadUInt(out selectedIndex)) return;
            if (QuestRuntime.Complete(client.Character, questId, selectedIndex, true))
            {
                DialogSession session;
                lock (Sync) { Sessions.TryGetValue(client.Character.ID, out session); }
                if (session != null) ContinueSession(client, session); else EndDialog(client.Character);
            }
        }

        [PacketHandler(CH17Type.ScenarioDoneReq)]
        public static void ScenarioDoneReqHandler(ZoneClient client, Packet packet)
        {
            ushort scenarioId;
            if (!packet.TryReadUShort(out scenarioId)) return;
            DialogSession session;
            lock (Sync) { Sessions.TryGetValue(client.Character.ID, out session); }
            if (session == null || !session.ScenarioPending || session.PendingScenarioID != scenarioId) return;
            session.ScenarioPending = false;
            session.PendingScenarioID = 0;
            // Original CQuestZone::Send_NC_QUEST_CLIENT_SCENARIO_DONE_ACK:
            // Header 17 / Type 12 / opcode 0x440C, payload ScenarioID u16.
            using (var ack = new Packet((ushort)0x440C))
            {
                ack.WriteUShort(scenarioId);
                client.SendPacket(ack);
            }
            if (session.Machine != null) ContinueSession(client, session);
        }

        [PacketHandler(CH17Type.NpcDialogResponse)]
        public static void NpcDialogResponseHandler(ZoneClient client, Packet packet)
        {
            ushort seq; byte marker, button;
            if (!packet.TryReadUShort(out seq) || !packet.TryReadByte(out marker) || !packet.TryReadByte(out button)) return;
            DialogSession session;
            lock (Sync) { Sessions.TryGetValue(client.Character.ID, out session); }
            if (session == null || session.Seq != seq) return;
            QuestDialogInfo current;
            if (!DataProvider.Instance.QuestDialogsByID.TryGetValue(session.DialogID, out current)) { EndDialog(client.Character); return; }
            if (current.Text != null && current.Text.Contains("[MENU]")) { EndDialog(client.Character); return; }
            if (session.Machine != null)
            {
                session.Machine.State.Result = button;
                for (int guard=0; guard<100; guard++)
                {
                    QuestScriptStep step=session.Machine.Next();
                    if(step.Type==QuestScriptStepType.Say){uint next;if(TryGetSayDialogId(step.Instruction,out next)){SendDialogPage(client,next,session.Machine);return;}}
                    else if(step.Type==QuestScriptStepType.Command){if(!ExecuteQuestCommand(client.Character,session.Machine,step.Instruction))return;continue;}
                    else if(step.Type==QuestScriptStepType.End||step.Type==QuestScriptStepType.Error){EndDialog(client.Character);return;}
                    else break;
                }
                EndDialog(client.Character); return;
            }
            SendDialogPage(client, session.DialogID + 1, null);
        }

        private static bool ExecuteQuestCommand(Game.ZoneCharacter character, QuestScriptMachine machine, QuestScriptInstruction instruction)
        {
            if (instruction == null) return true;
            uint q = machine.Graph.Info.QuestID;
            string[] args = instruction.Arguments.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (instruction.OpCode.Equals("ACCEPT", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length == 0) { QuestRuntime.Accept(character, q); return true; }
                uint acceptedQuest;
                if (uint.TryParse(args[0], out acceptedQuest)) QuestRuntime.Accept(character, acceptedQuest);
                return true;
            }
            if (instruction.OpCode.Equals("SCENARIO", StringComparison.OrdinalIgnoreCase) && args.Length >= 1)
            {
                ushort scenarioId;
                if (!ushort.TryParse(args[0], out scenarioId) || scenarioId == 0) return true;
                DialogSession session;
                lock (Sync) { Sessions.TryGetValue(character.ID, out session); }
                if (session == null) return false;
                session.PendingScenarioID = scenarioId;
                session.ScenarioPending = true;
                // Proven original CQuestZone::Send_NC_QUEST_SCENARIO_RUN_CMD:
                // Header 17 / Type 14 / opcode 0x440E, payload ScenarioID u16.
                using (var run = new Packet((ushort)0x440E))
                {
                    run.WriteUShort(scenarioId);
                    character.Client.SendPacket(run);
                }
                return false;
            }
            if (instruction.OpCode.Equals("CREATE_ITEM", StringComparison.OrdinalIgnoreCase) && args.Length >= 2) { ushort id, amount; if (ushort.TryParse(args[0], out id) && ushort.TryParse(args[1], out amount)) QuestRuntime.CreateItem(character, id, amount); return true; }
            if (instruction.OpCode.Equals("GET_ITEM_LOT", StringComparison.OrdinalIgnoreCase) && args.Length >= 1) { ushort id; machine.State.Result = ushort.TryParse(args[0], out id) ? (int)Math.Min(int.MaxValue, QuestRuntime.GetItemLot(character, id)) : 0; return true; }
            if (instruction.OpCode.Equals("GET_PLAYER_EMPTY_INVENTORY", StringComparison.OrdinalIgnoreCase) && args.Length >= 1) { machine.State.Variables[args[0]] = QuestRuntime.GetEmptyInventorySlots(character); return true; }
            if (instruction.OpCode.Equals("DELETE_ITEM", StringComparison.OrdinalIgnoreCase) && args.Length >= 2) { ushort id; if (ushort.TryParse(args[0], out id)) QuestRuntime.DeleteItem(character, id, args[1]); return true; }
            if (instruction.OpCode.Equals("DONE", StringComparison.OrdinalIgnoreCase) && machine.State.Stage == QuestScriptStage.Finish)
            {
                if (QuestRuntime.Complete(character, q)) return true;
                try { using (DatabaseClient db = Program.DatabaseManager.GetClient()) if (QuestRuntime.NeedsRewardSelection(db, q)) return false; } catch { }
                return false;
            }
            return true;
        }

        private static void ContinueSession(ZoneClient client, DialogSession session)
        {
            for (int guard=0; guard<100; guard++)
            {
                QuestScriptStep step=session.Machine.Next();
                if(step.Type==QuestScriptStepType.Say){uint next;if(TryGetSayDialogId(step.Instruction,out next)){SendDialogPage(client,next,session.Machine);return;}}
                else if(step.Type==QuestScriptStepType.Command){if(!ExecuteQuestCommand(client.Character,session.Machine,step.Instruction))return;continue;}
                else if(step.Type==QuestScriptStepType.End||step.Type==QuestScriptStepType.Error){EndDialog(client.Character);return;}
                else return;
            }
            EndDialog(client.Character);
        }

        public static void SendDialogPage(ZoneClient client, uint dialogId)
        {
            DialogScriptContext context; QuestScriptMachine machine=null;
            if(TryFindDialogContext(dialogId,out context)){machine=new QuestScriptMachine(context.Info,context.Stage);machine.State.InstructionIndex=context.InstructionIndex+1;}
            SendDialogPage(client,dialogId,machine);
        }

        private static void SendDialogPage(ZoneClient client, uint dialogId, QuestScriptMachine machine)
        {
            QuestDialogInfo info;
            if(!DataProvider.Instance.QuestDialogsByID.TryGetValue(dialogId,out info)){EndDialog(client.Character);return;}
            DialogSession session;
            lock(Sync){DialogSession previous;Sessions.TryGetValue(client.Character.ID,out previous);ushort seq=previous==null?(ushort)1:(ushort)(previous.Seq+1);session=new DialogSession{DialogID=dialogId,Seq=seq,Machine=machine,PendingScenarioID=0,ScenarioPending=false};Sessions[client.Character.ID]=session;}
            using(var p=new Packet(SH17Type.NpcDialogMenu)){p.WriteUShort(session.Seq);p.WriteUInt(2);p.WriteByte(0);p.WriteUShort((ushort)dialogId);p.Fill(94,0);client.SendPacket(p);}
        }

        private static bool TryFindDialogContext(uint dialogId,out DialogScriptContext context){EnsureContexts();lock(Sync){if(AmbiguousDialogs.Contains(dialogId)){context=null;return false;}return DialogContexts.TryGetValue(dialogId,out context);}}
        private static void EnsureContexts(){lock(Sync){if(DialogContexts!=null)return;DialogContexts=new Dictionary<uint,DialogScriptContext>();AmbiguousDialogs=new HashSet<uint>();try{using(DatabaseClient db=Program.DatabaseManager.GetClient()){DataTable rows=db.ReadDataTable("SELECT * FROM data_quest_script");if(rows==null)return;foreach(DataRow row in rows.Rows){QuestScriptInfo info=QuestScriptInfo.Load(row);Index(info,QuestScriptStage.Start,info.Start);Index(info,QuestScriptStage.Action,info.Action);Index(info,QuestScriptStage.Finish,info.Finish);}}}catch(Exception ex){Log.WriteLine(LogLevel.Warn,"Quest script index failed: {0}",ex.Message);}}}
        private static void Index(QuestScriptInfo info,QuestScriptStage stage,QuestScriptProgram program){for(int i=0;i<program.Instructions.Count;i++){uint id;QuestScriptInstruction ins=program.Instructions[i];if(!ins.OpCode.Equals("SAY",StringComparison.OrdinalIgnoreCase)||!TryGetSayDialogId(ins,out id))continue;if(AmbiguousDialogs.Contains(id))continue;DialogScriptContext old;if(DialogContexts.TryGetValue(id,out old)){DialogContexts.Remove(id);AmbiguousDialogs.Add(id);continue;}DialogContexts[id]=new DialogScriptContext{Info=info,Stage=stage,InstructionIndex=i};}}
        private static bool TryGetSayDialogId(QuestScriptInstruction instruction,out uint id){id=0;if(instruction==null)return false;string[] p=instruction.Arguments.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);return p.Length>0&&uint.TryParse(p[0],out id);}
        private static void EndDialog(Game.ZoneCharacter c){if(c==null)return;lock(Sync)Sessions.Remove(c.ID);}
    }
}
