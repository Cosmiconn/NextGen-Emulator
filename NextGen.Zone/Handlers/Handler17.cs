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
        private sealed class DialogSession { public uint DialogID; public ushort QuestID; public QuestScriptMachine Machine; public ushort PendingScenarioID; public bool ScenarioPending; }
        private sealed class DialogScriptContext { public QuestScriptInfo Info; public QuestScriptStage Stage; public int InstructionIndex; }
        private static readonly object Sync = new object();
        private static readonly Dictionary<int, DialogSession> Sessions = new Dictionary<int, DialogSession>();
        private static Dictionary<uint, DialogScriptContext> DialogContexts;
        private static Dictionary<uint, QuestScriptInfo> QuestScriptsById;
        private static HashSet<uint> AmbiguousDialogs;
        private const ushort QuestSelectStartSuccess = 0x0B41;
        private const ushort QuestSelectStartFailure = 0x0B47;

        [PacketHandler(CH17Type.QuestSelectStart)]
        public static void QuestSelectStartHandler(ZoneClient client, Packet packet)
        {
            ushort npcId;
            ushort requestedQuestId;
            if (!packet.TryReadUShort(out npcId) || !packet.TryReadUShort(out requestedQuestId))
                return;

            ushort result = QuestSelectStartFailure;
            Game.Npc target = client.Character.CharacterInTarget as Game.Npc;
            if (target != null && target.ID == npcId)
            {
                uint selectedQuestId;
                uint dialogId;
                QuestScriptStage selectedStage;
                if (QuestNpcStartResolver.TryResolveForCharacter(
                        client.Character, target.Point.MobName,
                        out selectedQuestId, out dialogId, out selectedStage) &&
                    selectedQuestId == requestedQuestId)
                {
                    DialogSession current;
                    lock (Sync)
                        Sessions.TryGetValue(client.Character.ID, out current);

                    // Handler8 can already have opened the same first page for
                    // clients following the legacy interaction path. Do not
                    // duplicate that page, but still acknowledge the proven
                    // 0x440F request.
                    if (current == null || current.DialogID != dialogId || current.QuestID != requestedQuestId)
                        SendDialogPage(client, selectedQuestId, dialogId, selectedStage);

                    result = QuestSelectStartSuccess;
                }
            }

            SendQuestSelectStartAck(client, npcId, requestedQuestId, result);
        }

        private static void SendQuestSelectStartAck(ZoneClient client, ushort npcId,
            ushort questId, ushort errorType)
        {
            using (var ack = new Packet(SH17Type.QuestSelectStartAck))
            {
                ack.WriteUShort(npcId);
                ack.WriteUShort(questId);
                ack.WriteUShort(errorType);
                client.SendPacket(ack);
            }
        }

        private static void SendQuestCommandError(Game.ZoneCharacter character,
            uint questId, uint failedCommand, ushort errorType)
        {
            if (character == null || character.Client == null || questId > ushort.MaxValue)
                return;

            // Original CQuestZone::Send_QUEST_ERROR_TO_CLIENT at 0x005BD870
            // forwards a complete QSC_ERROR through Send_NC_QUEST_SCRIPT_CMD_REQ:
            // Cmd=ERROR(0), IsPigeonStartType=0, then Data DWORDs
            // {failed QSC, 0} followed by the WORD error code.
            using (var p = new Packet(SH17Type.NpcDialogMenu))
            {
                p.WriteUShort((ushort)questId);
                p.WriteUInt(0);
                p.WriteByte(0);
                p.WriteUInt(failedCommand);
                p.WriteUInt(0);
                p.WriteUShort(errorType);
                p.Fill(86, 0);
                character.Client.SendPacket(p);
            }
        }

        private static void SendQuestEnd(Game.ZoneCharacter character, uint questId)
        {
            if (character == null || character.Client == null ||
                questId == 0 || questId > ushort.MaxValue)
                return;

            // Native QuestNext command 1 sends the current STRUCT_QSC through
            // Send_NC_QUEST_SCRIPT_CMD_REQ and immediately QuestClose's it.
            // END has no command-specific operands; only Cmd=1 and
            // IsPigeonStartType=0 are semantically used.
            using (var p = new Packet(SH17Type.NpcDialogMenu))
            {
                p.WriteUShort((ushort)questId);
                p.WriteUInt(1);
                p.WriteByte(0);
                p.Fill(96, 0);
                character.Client.SendPacket(p);
            }
        }

        [PacketHandler(CH17Type.RewardSelectItemIndex)] public static void RewardSelectItemIndexHandler(ZoneClient client, Packet packet){ushort questId;uint selectedIndex;if(!packet.TryReadUShort(out questId)||!packet.TryReadUInt(out selectedIndex))return;if(QuestRuntime.Complete(client.Character,questId,selectedIndex,true)){DialogSession session;lock(Sync){Sessions.TryGetValue(client.Character.ID,out session);}if(session!=null)ContinueSession(client,session);else EndDialog(client.Character);}}
        [PacketHandler(CH17Type.ScenarioDoneReq)] public static void ScenarioDoneReqHandler(ZoneClient client,Packet packet){ushort scenarioId;if(!packet.TryReadUShort(out scenarioId))return;DialogSession session;lock(Sync){Sessions.TryGetValue(client.Character.ID,out session);}if(session==null||session.Machine==null||!session.ScenarioPending||session.PendingScenarioID!=scenarioId)return;uint questId=session.Machine.Graph.Info.QuestID;if(!QuestRuntime.RecordScenarioDone(client.Character,questId,scenarioId))return;session.ScenarioPending=false;session.PendingScenarioID=0;using(var ack=new Packet((ushort)0x440C)){ack.WriteUShort(scenarioId);client.SendPacket(ack);}ContinueSession(client,session);}
        [PacketHandler(CH17Type.NpcDialogResponse)]
        public static void NpcDialogResponseHandler(ZoneClient client, Packet packet)
        {
            // Original PROTO_NC_QUEST_SCRIPT_CMD_ACK is exactly:
            //   WORD nQuestID, BYTE nQSC, DWORD nResult.
            ushort questId;
            byte qsc;
            uint result;
            if (!packet.TryReadUShort(out questId) ||
                !packet.TryReadByte(out qsc) ||
                !packet.TryReadUInt(out result))
                return;

            DialogSession session;
            lock (Sync) { Sessions.TryGetValue(client.Character.ID, out session); }
            if (session == null || session.QuestID != questId || qsc != 2)
                return;

            QuestDialogInfo current;
            if (!DataProvider.Instance.QuestDialogsByID.TryGetValue(session.DialogID, out current))
            {
                EndDialog(client.Character);
                return;
            }

            if (current.Text != null && current.Text.Contains("[MENU]"))
            {
                EndDialog(client.Character);
                return;
            }

            if (session.Machine == null)
            {
                EndDialog(client.Character);
                return;
            }

            session.Machine.State.Result = unchecked((int)result);
            for (int guard = 0; guard < 100; guard++)
            {
                QuestScriptStep step = session.Machine.Next();
                if (step.Type == QuestScriptStepType.Say)
                {
                    uint next;
                    if (TryGetSayDialogId(step.Instruction, out next))
                    {
                        SendDialogPage(client, next, session.Machine, step.Instruction);
                        return;
                    }
                }
                else if (step.Type == QuestScriptStepType.Command)
                {
                    if (!ExecuteQuestCommand(client.Character, session.Machine, step.Instruction))
                        return;
                    continue;
                }
                else if (step.Type == QuestScriptStepType.End ||
                         step.Type == QuestScriptStepType.Error)
                {
                    if (step.Type == QuestScriptStepType.Error)
                    {
                        LogScriptError(client.Character, session.Machine, step);
                    }
                    else if (step.Instruction != null &&
                             step.Instruction.OpCode.Equals("END", StringComparison.OrdinalIgnoreCase))
                    {
                        // Explicit textual END is QSC_END (1) and is sent to the
                        // client before QuestClose. Parser EOF has a null
                        // instruction here and closes without a QSC packet.
                        SendQuestEnd(client.Character, session.Machine.Graph.Info.QuestID);
                    }
                    EndDialog(client.Character);
                    return;
                }
                else
                {
                    break;
                }
            }
            EndDialog(client.Character);
        }

        private static bool ExecuteQuestCommand(Game.ZoneCharacter character,QuestScriptMachine machine,QuestScriptInstruction instruction)
        {
            if(instruction==null)return true;
            uint q=machine.Graph.Info.QuestID;
            string[] args=instruction.Arguments.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            if(instruction.OpCode.Equals("ACCEPT",StringComparison.OrdinalIgnoreCase))
            {
                uint acceptedQuest=q;
                if(args.Length>0)
                {
                    ushort explicitQuest;
                    if(!ushort.TryParse(args[0],out explicitQuest))
                    {
                        EndDialog(character);
                        return false;
                    }
                    acceptedQuest=explicitQuest;
                }
                ushort acceptError;
                if(!QuestRuntime.Accept(character,acceptedQuest,out acceptError))
                {
                    // Original command-6 rejection paths use raw errors:
                    // missing QuestData=0x0C02, 40+ doing quests=0x0C0F,
                    // not Doingable=0x0C03. The packet carries the currently
                    // executing script QuestID, not an explicit ACCEPT target.
                    if(acceptError!=0)
                        SendQuestCommandError(character,q,6,acceptError);
                    EndDialog(character);
                    return false;
                }
                return true;
            }
            if(instruction.OpCode.Equals("CANCEL",StringComparison.OrdinalIgnoreCase)&&args.Length==0)
            {
                if(!QuestRuntime.Cancel(character,q))
                {
                    // Native command-7 missing player-quest/QuestData path.
                    SendQuestCommandError(character,q,7,0x0C04);
                    EndDialog(character);
                    return false;
                }
                return true;
            }
            if(instruction.OpCode.Equals("SCENARIO",StringComparison.OrdinalIgnoreCase)&&args.Length>=1){ushort scenarioId;if(!ushort.TryParse(args[0],out scenarioId)||scenarioId==0)return true;DialogSession session;lock(Sync){Sessions.TryGetValue(character.ID,out session);}if(session==null)return false;session.PendingScenarioID=scenarioId;session.ScenarioPending=true;using(var run=new Packet((ushort)0x440E)){run.WriteUShort(scenarioId);character.Client.SendPacket(run);}return false;}
            if(instruction.OpCode.Equals("SET_ABSTATE",StringComparison.OrdinalIgnoreCase)&&args.Length>=3){uint strength,keepTimeMs;if(!uint.TryParse(args[1],out strength)||!uint.TryParse(args[2],out keepTimeMs))return true;QuestRuntime.SetAbstate(character,args[0],strength,keepTimeMs);return true;}
            if(instruction.OpCode.Equals("RESET_ABSTATE",StringComparison.OrdinalIgnoreCase)&&args.Length>=1){QuestRuntime.ResetAbstate(character,args[0]);return true;}
            if(instruction.OpCode.Equals("CREATE_ITEM",StringComparison.OrdinalIgnoreCase)&&args.Length>=2)
            {
                ushort id; uint amount;
                if(ushort.TryParse(args[0],out id)&&uint.TryParse(args[1],out amount)&&
                   !QuestRuntime.CreateItem(character,id,amount))
                {
                    // Native QSC_CREATE_ITEM failure: error 0x0C0B, then QuestClose.
                    SendQuestCommandError(character,q,14,0x0C0B);
                    EndDialog(character);
                    return false;
                }
                return true;
            }
            if(instruction.OpCode.Equals("GET_ITEM_LOT",StringComparison.OrdinalIgnoreCase)&&args.Length>=1){ushort id;machine.State.Result=ushort.TryParse(args[0],out id)?(int)(ushort)QuestRuntime.GetItemLot(character,id):0;return true;}
            if(instruction.OpCode.Equals("GET_PLAYER_EMPTY_INVENTORY",StringComparison.OrdinalIgnoreCase)&&args.Length>=1){machine.State.Variables[args[0]]=(byte)QuestRuntime.GetEmptyInventorySlots(character);return true;}
            if(instruction.OpCode.Equals("DELETE_ITEM",StringComparison.OrdinalIgnoreCase)&&args.Length>=2)
            {
                ushort id;
                if(ushort.TryParse(args[0],out id)&&!QuestRuntime.DeleteItem(character,id,args[1]))
                {
                    // Native QSC_DELETE_ITEM failure: error 0x0C0A, then QuestClose.
                    SendQuestCommandError(character,q,13,0x0C0A);
                    EndDialog(character);
                    return false;
                }
                return true;
            }
            if(instruction.OpCode.Equals("LINK",StringComparison.OrdinalIgnoreCase))
            {
                // ParserNext command 11 shares the ACCEPT-style WORD operand
                // parser. Numeric text is atoi'd and stored through AX, so it
                // truncates to 16 bits. A missing/non-numeric operand falls back
                // to parser+0x83C, which QuestStart/Doing/End initialize with the
                // current QuestID.
                ushort linkedQuestId;
                uint rawLinkedQuestId;
                if(args.Length<1||!uint.TryParse(args[0],out rawLinkedQuestId))
                {
                    if(q==0||q>ushort.MaxValue)
                    {
                        EndDialog(character);
                        return false;
                    }
                    linkedQuestId=(ushort)q;
                }
                else
                {
                    linkedQuestId=unchecked((ushort)rawLinkedQuestId);
                }

                QuestScriptStage linkedStage;
                if(linkedQuestId==0||
                   !QuestNpcStartResolver.TryResolveLinkedStage(character,linkedQuestId,out linkedStage))
                {
                    // Native unmatched LINK status/target reaches QuestClose.
                    EndDialog(character);
                    return false;
                }

                QuestScriptInfo linkedInfo;
                if(!TryGetQuestScriptInfo(linkedQuestId,out linkedInfo))
                {
                    Log.WriteLine(LogLevel.Debug,"Quest LINK target {0} is not present in data_quest_script.",linkedQuestId);
                    EndDialog(character);
                    return false;
                }

                DialogSession session;
                lock(Sync){Sessions.TryGetValue(character.ID,out session);}
                if(session==null)return false;
                session.Machine=new QuestScriptMachine(linkedInfo,linkedStage);
                return true;
            }
            if(instruction.OpCode.Equals("DONE",StringComparison.OrdinalIgnoreCase))
            {
                // Native QuestNext command 10 dispatches DONE through the common
                // command path at 0x005BED38. It resolves the quest record and
                // calls the IsRewardAbleQuest wrapper without testing whether
                // the parser was entered through Start, Doing or End.
                if(QuestRuntime.Complete(character,q))return true;
                try{using(DatabaseClient db=Program.DatabaseManager.GetClient())if(QuestRuntime.NeedsRewardSelection(db,q))return false;}catch{}
                return false;
            }
            return true;
        }
        private static void ContinueSession(ZoneClient client, DialogSession session)
        {
            if (session == null || session.Machine == null)
            {
                EndDialog(client.Character);
                return;
            }

            for (int guard = 0; guard < 100; guard++)
            {
                QuestScriptStep step = session.Machine.Next();
                if (step.Type == QuestScriptStepType.Say)
                {
                    uint next;
                    if (TryGetSayDialogId(step.Instruction, out next))
                    {
                        SendDialogPage(client, next, session.Machine, step.Instruction);
                        return;
                    }
                }
                else if (step.Type == QuestScriptStepType.Command)
                {
                    if (!ExecuteQuestCommand(client.Character, session.Machine, step.Instruction))
                        return;
                    continue;
                }
                else if (step.Type == QuestScriptStepType.End ||
                         step.Type == QuestScriptStepType.Error)
                {
                    if (step.Type == QuestScriptStepType.Error)
                    {
                        LogScriptError(client.Character, session.Machine, step);
                    }
                    else if (step.Instruction != null &&
                             step.Instruction.OpCode.Equals("END", StringComparison.OrdinalIgnoreCase))
                    {
                        // Explicit textual END is QSC_END (1) and is sent to the
                        // client before QuestClose. Parser EOF has a null
                        // instruction here and closes without a QSC packet.
                        SendQuestEnd(client.Character, session.Machine.Graph.Info.QuestID);
                    }
                    EndDialog(client.Character);
                    return;
                }
                else
                {
                    return;
                }
            }
            EndDialog(client.Character);
        }

        public static void SendDialogPage(ZoneClient client, uint dialogId)
        {
            DialogScriptContext context;
            if (!TryFindDialogContext(dialogId, out context))
            {
                Log.WriteLine(LogLevel.Warn,
                    "Quest dialog {0} has no unique script context; refusing guessed 0x4401 payload.",
                    dialogId);
                EndDialog(client.Character);
                return;
            }
            SendDialogPage(client, context.Info.QuestID, dialogId, context.Stage);
        }

        public static void SendDialogPage(ZoneClient client, uint questId,
            uint dialogId, QuestScriptStage stage)
        {
            if (questId == 0 || questId > ushort.MaxValue)
            {
                EndDialog(client.Character);
                return;
            }

            QuestScriptInfo info;
            if (!TryGetQuestScriptInfo(questId, out info))
            {
                EndDialog(client.Character);
                return;
            }

            QuestScriptProgram program = GetProgram(info, stage);
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                QuestScriptInstruction instruction = program.Instructions[i];
                uint candidateDialog;
                if (!instruction.OpCode.Equals("SAY", StringComparison.OrdinalIgnoreCase) ||
                    !TryGetSayDialogId(instruction, out candidateDialog) ||
                    candidateDialog != dialogId)
                    continue;

                QuestScriptMachine machine = new QuestScriptMachine(info, stage);
                machine.State.InstructionIndex = i + 1;
                SendDialogPage(client, dialogId, machine, instruction);
                return;
            }

            Log.WriteLine(LogLevel.Warn,
                "Quest {0} stage {1} has no SAY for dialog {2}; refusing guessed 0x4401 payload.",
                questId, stage, dialogId);
            EndDialog(client.Character);
        }

        private static void SendDialogPage(ZoneClient client, uint dialogId,
            QuestScriptMachine machine, QuestScriptInstruction sayInstruction)
        {
            QuestDialogInfo info;
            if (!DataProvider.Instance.QuestDialogsByID.TryGetValue(dialogId, out info) ||
                machine == null)
            {
                EndDialog(client.Character);
                return;
            }

            uint parsedDialogId;
            uint talkerType;
            ushort npcNo;
            if (!TryGetSayFields(sayInstruction, out parsedDialogId, out talkerType, out npcNo) ||
                parsedDialogId != dialogId)
            {
                Log.WriteLine(LogLevel.Warn,
                    "Quest {0} SAY payload for dialog {1} is not source-faithful.",
                    machine.Graph.Info.QuestID, dialogId);
                EndDialog(client.Character);
                return;
            }

            uint questId = machine.Graph.Info.QuestID;
            if (questId == 0 || questId > ushort.MaxValue)
            {
                EndDialog(client.Character);
                return;
            }

            DialogSession session = new DialogSession
            {
                DialogID = dialogId,
                QuestID = (ushort)questId,
                Machine = machine,
                PendingScenarioID = 0,
                ScenarioPending = false
            };
            lock (Sync) { Sessions[client.Character.ID] = session; }

            // Native CQuestZone::Send_NC_QUEST_SCRIPT_CMD_REQ at 0x005BA960
            // sends opcode 0x4401 followed by WORD QuestID and the complete
            // 101-byte STRUCT_QSC. ParserNext initializes IsPigeonStartType
            // to zero. For QSC_SAY (2), the union is:
            // DWORD nID, DWORD TalkerType, WORD NPCNo, then unused union bytes.
            using (var p = new Packet(SH17Type.NpcDialogMenu))
            {
                p.WriteUShort(session.QuestID);
                p.WriteUInt(2);
                p.WriteByte(0);
                p.WriteUInt(parsedDialogId);
                p.WriteUInt(talkerType);
                p.WriteUShort(npcNo);
                p.Fill(86, 0);
                client.SendPacket(p);
            }
        }

        private static QuestScriptProgram GetProgram(QuestScriptInfo info, QuestScriptStage stage)
        {
            if (stage == QuestScriptStage.Action) return info.Action;
            if (stage == QuestScriptStage.Finish) return info.Finish;
            return info.Start;
        }

        private static bool TryGetSayFields(QuestScriptInstruction instruction,
            out uint dialogId, out uint talkerType, out ushort npcNo)
        {
            dialogId = 0;
            talkerType = 0;
            npcNo = ushort.MaxValue;
            if (instruction == null ||
                !instruction.OpCode.Equals("SAY", StringComparison.OrdinalIgnoreCase))
                return false;

            string[] parts = instruction.Arguments.Split(
                new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 ||
                !uint.TryParse(parts[0].TrimEnd(','), out dialogId))
                return false;

            if (parts[1].Equals("NPC", StringComparison.OrdinalIgnoreCase))
            {
                talkerType = 0;
                if (parts.Length == 2) return true;
                return parts.Length == 3 && ushort.TryParse(parts[2], out npcNo);
            }

            if (parts[1].Equals("ME", StringComparison.OrdinalIgnoreCase))
            {
                talkerType = 1;
                return parts.Length == 2;
            }

            return false;
        }

        private static bool TryFindDialogContext(uint dialogId,out DialogScriptContext context){EnsureContexts();lock(Sync){if(AmbiguousDialogs.Contains(dialogId)){context=null;return false;}return DialogContexts.TryGetValue(dialogId,out context);}}
        private static void EnsureContexts(){lock(Sync){if(DialogContexts!=null)return;DialogContexts=new Dictionary<uint,DialogScriptContext>();QuestScriptsById=new Dictionary<uint,QuestScriptInfo>();AmbiguousDialogs=new HashSet<uint>();try{using(DatabaseClient db=Program.DatabaseManager.GetClient()){DataTable rows=db.ReadDataTable("SELECT * FROM data_quest_script");if(rows==null)return;foreach(DataRow row in rows.Rows){QuestScriptInfo info=QuestScriptInfo.Load(row);QuestScriptsById[info.QuestID]=info;Index(info,QuestScriptStage.Start,info.Start);Index(info,QuestScriptStage.Action,info.Action);Index(info,QuestScriptStage.Finish,info.Finish);}}}catch(Exception ex){Log.WriteLine(LogLevel.Warn,"Quest script index failed: {0}",ex.Message);}}}
        private static bool TryGetQuestScriptInfo(uint questId,out QuestScriptInfo info){EnsureContexts();lock(Sync){info=null;return QuestScriptsById!=null&&QuestScriptsById.TryGetValue(questId,out info);}}
        private static void Index(QuestScriptInfo info,QuestScriptStage stage,QuestScriptProgram program){for(int i=0;i<program.Instructions.Count;i++){uint id;QuestScriptInstruction ins=program.Instructions[i];if(!ins.OpCode.Equals("SAY",StringComparison.OrdinalIgnoreCase)||!TryGetSayDialogId(ins,out id))continue;if(AmbiguousDialogs.Contains(id))continue;DialogScriptContext old;if(DialogContexts.TryGetValue(id,out old)){DialogContexts.Remove(id);AmbiguousDialogs.Add(id);continue;}DialogContexts[id]=new DialogScriptContext{Info=info,Stage=stage,InstructionIndex=i};}}
        private static bool TryGetSayDialogId(QuestScriptInstruction instruction,out uint id){id=0;if(instruction==null)return false;string[] p=instruction.Arguments.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);return p.Length>0&&uint.TryParse(p[0].TrimEnd(','),out id);}
        private static void LogScriptError(Game.ZoneCharacter character,QuestScriptMachine machine,QuestScriptStep step)
        {
            if(machine==null||step==null)return;
            QuestScriptInstruction instruction=step.Instruction;
            Log.WriteLine(LogLevel.Warn,
                "Quest script error: Char={0} Quest={1} Stage={2} Line={3} Source='{4}' Error='{5}'",
                character==null?0:character.ID,
                machine.Graph.Info.QuestID,
                machine.State.Stage,
                instruction==null?0:instruction.LineNumber,
                instruction==null?string.Empty:instruction.Source,
                step.Error??string.Empty);
        }
        private static void EndDialog(Game.ZoneCharacter c){if(c==null)return;lock(Sync)Sessions.Remove(c.ID);}
    }
}
