
namespace NextGen.FiestaLib
{
    // Named as SHXType , where X = header ID

    public enum SH2Type : byte
    {
        Ping = 4,
        SetXorKeyPosition = 7,
        Chatblock = 72,
        UpdateClientTime = 73,
        UnkTimePacket = 69,
        Unk1 = 14,
    }
    public enum SH19Type : byte
    {
        SendTradeReqest = 2,
        DeclineRequest = 4,
        SendTradeAccept = 9,
        SendTradeBreak = 12,
        SendAddItemSuccefull = 15,
        SendAddItem = 16,
        SendItemRemove = 19,
        SendChangeMoney = 24,
        SendTradeRdy = 27,
        SendTradeLock = 28,
        SendRemoveItemFromHandel = 20,
        SendTradeAgreeMe = 33,
        SendTradeAgreeTo = 34,
        SendTradeComplett = 36,
    }
    public enum SH3Type : byte
    {
        IncorrectVersion = 2, //please update client
        VersionAllowed = 103,
        FilecheckAllow = 5,
        Error = 9,

        // World list
        WorldlistNew = 10, // Initially sends this
        WorldServerIP = 12,
        WorldistResend = 28,
        

        //Actually used in world
        CharacterList = 20,
        BackToWorldListFromChar = 52,
    }

    public enum SH4Type : byte
    {
        Money = 51,
        UpdateStats = 53,
        ConnectError = 2,
        Unk = 222,
        ServerIP = 3,
        CharacterGuildinfo = 18,
        CharacterInfo = 56,
        CharacterLook = 57,
        CharacterQuestsBusy = 58,
        CharacterQuestsDone = 59,
        CharacterActiveSkillList = 61,
        CharacterPassiveSkillList = 62,
        CharacterItemList = 71,
        CharacterInfoEnd = 72,
        CharacterTitles = 73,
        CharacterTimedItemList = 74,
        // 9-Byte-Body, per echtem Mitschnitt (versuch_5) an ZWEI
        // unabhaengigen Todesfaellen (Uruga-Feld und KQ-Instanz
        // "KDHDragon") BYTE-IDENTISCH beobachtet: [u32 LE][u32 LE][byte].
        // Bisher beobachtet nur die Werte 180/50/0 - da identisch trotz
        // unterschiedlicher Situation (normaler Mob-Tod vs. KQ-Fail),
        // vermutlich feste Server-Konfigurationskonstanten und keine
        // situationsabhaengigen Werte (z.B. Revival-Fenster-Timeout in
        // einer noch unbekannten Zeiteinheit / Revival-Gebuehr in Gold).
        // Siehe DOCUMENTATION.md Abschnitt 54.
        ReviveWindow = 77,
        // 10-Byte-Body: [u16 LE RespawnPointID][u32 LE X][u32 LE Y].
        // URSPRUENGLICH als HP-Werte vermutet - per echtem Mitschnitt
        // widerlegt: die tatsaechliche HP-Wiederherstellung kommt separat
        // ueber SH9Type.HealHP (siehe dort). Die beiden u32-Felder hier
        // aendern sich zwischen zwei Toden in unterschiedlichen Zonen
        // (Uruga vs. Elderine-Umgebung) deutlich, waehrend RespawnPointID
        // ebenfalls je Karte unterschiedlich ist - beides konsistent mit
        // einer Zielposition, nicht mit HP. Siehe DOCUMENTATION.md
        // Abschnitt 54.
        Revive = 79,
        CharacterPoints = 91,
        SetPointOnStat = 95,
        CharacterGuildacademyinfo = 151,
    }

    public enum SH5Type : byte
    {
        CharCreationError = 4,
        CharCreationOK = 6,
        CharDeleteOK = 12,
        SendCharacterChangeNewName = 16,
    }

    public enum SH6Type : byte
    {
        DetailedCharacterInfo = 2,
        Error = 3,
        RemoveDrop = 5,
        ChangeMap = 9,
        ChangeZone = 10,
        TelePorter = 27,

    }

    public enum SH7Type : byte
    {
        ShowUnequip = 4,
        ShowEquip = 5,
        SpawnSinglePlayer = 6,
        SpawnMultiPlayer = 7,
        SpawnSingleObject = 8,
        SpawnMultiObject = 9,
        ShowDrop = 10,
        ShowDrops = 11,
        RemoveObject = 14,
    }
    public enum SH8Type : byte
    {
        ChatNormal = 2,
        WisperFrom = 13,
        WisperTargetNotfound = 14,
        WisperTo = 15,
        // 2-Byte-Body: [byte Kategorie][byte StringLaenge], gefolgt von
        // <StringLaenge> ASCII-Bytes OHNE Nullterminierung - an vier
        // unabhaengigen Beispielen bytegenau bestaetigt (u.a. "From
        // 127.0.0.1" mit Laenge 14, "Admin level is 100" mit Laenge 18,
        // "Move to Elderine in 30 seconds." mit Laenge 31). Wird auch fuer
        // den KQ-Fail-Rueckteleport-Countdown genutzt ("Move to <Stadt> in
        // <n> seconds."). Siehe DOCUMENTATION.md Abschnitt 54.
        GmNotice = 17,
        StopTele = 19, // Stops char but can teleport
        PartyChat = 21,
        Walk = 24,
        Move = 26,
        Teleport = 27,
        Interaction = 28,
        Shout = 31,
        Emote = 33,
        Jump = 37,
        BeginRest = 40,
        BeginDisplayRest = 41,
        EndRest = 43,
        EndDisplayRest = 44,
        Mounting = 63,
        MapMount = 64,
        Unmount = 66,
        MapUnmount = 67,
        UpdateMountFood = 70,
        CastItem = 71,
        BlockWalk = 74,
    }

    public enum SH9Type : byte
    {
        StatUpdate = 2,
        GainExp = 11,
        LevelUP = 12,
        LevelUPAnimation = 13,
        // 6-Byte-Body: [u32 LE Betrag][u16 LE fortlaufender Zaehler].
        // Betrag per echtem Mitschnitt exakt bestaetigt: 459 unmittelbar
        // nach einem Revive - deckt sich bytegenau mit der vom Nutzer
        // notierten Beobachtung "459hp wiederhergestellt". Siehe
        // DOCUMENTATION.md Abschnitt 54.
        HealHP = 14,
        HealSP = 15,
        SkillAck = 53,
        ResetStance = 61,
        AttackAnimation = 71,
        AttackDamage = 72,
        DieAnimation = 74,

        SkillUsePrepareSelf = 78,
        SkillUsePrepareOthers = 79, 

        SkillAnimationPosition = 81,
        SkillAnimationTarget = 82,
        SkillAnimation = 87,
    }

    public enum SH12Type : byte
    { 
        ModifyItemSlot = 1,
        ModifyEquipSlot = 2,
        InventoryFull = 4,
        ObtainedItem = 10,
        MoveIteminContaInComplet = 12,
        FailedEquip = 17,
        FailedUnequip = 19,
        ItemUseEffect = 22,
        ItemUpgrade = 24,
        ItemUsedOk = 26,
        SendPremiumItemList = 33,
        SendRewardList = 45,
    }
    public enum SH14Type : byte
    {
        // According to my informations, 7 is InviteDeclined.
        // NOTE - IT IS.
		// Header 7 somehow changed I guess?
		// seems to be answer or related to CH14::72
		// new data is CHAR[16] NAME | USHORT UNK (C1 04)
		// purpose complete unknown
        InviteDeclined = 7,
        UpdatePartyMemberLoc = 73,
        UpdatePartyMemberStats = 50,
        SetMemberStats = 51,
		// Invite/Accept might be switched up
        PartyInvite = 3,
        PartyAccept = 4,
        PartyDropState = 76,
        PartyList = 9,
		// changed.
        PartyLeave = 11,
        GroupList = 85,
		// might changed as well?
        ChangePartyMaster = 41,
        ChangePartyDrop = 75,
        KickPartyMember = 21,

        BreakUp = 30,

		// COMPLETE UNKOWN
		// DATA: CHAR[16] NAME, thats it.
		UNK_1 = 71,
    }
    public enum SH15Type : byte
    {
        Question = 1,
        HandlerWeapon = 9,
        HanlderSkill = 10,
        HandlerStone = 5,
        HandlerTitel = 11,
        GuildNpcReqest = 12,
    }

    //skills & crap?
    public enum SH18Type : byte
    {
        LearnSkill = 4,
        // Per echtem Paket-Mitschnitt entdeckt (trat exakt bei jedem
        // Levelup auf), siehe DOCUMENTATION.md Abschnitt 35.
        NewSkillsAvailable = 16,
    }

    public enum SH20Type : byte
    {
        ChangeHPStones = 3,
        ChangeSPStones = 4,
        ErrorBuyStone = 5,
        ErrorUseStone = 6,
        StartHPStoneCooldown = 8,
        StartSPStoneCooldown = 10,
    }
    public enum SH21Type : byte
    {
        FriendListDelete = 6,
        FriendInviteResponse = 2,
        FriendInviteRequest = 3,
        FriendExtraInformation = 8,
        FriendOnline = 9,
        FriendOffline = 10,
        FriendInviteReject = 11,
        FriendDeleteSend = 12,
        FriendChangeMap = 13,
    }
    public enum SH25Type : byte
    {
        WorldMessage = 2,
    }

    public enum SH28Type : byte
    {
        LoadQuickBar = 3,
        LoadQuickBarState = 5,
        LoadGameSettings = 11,
        LoadClientSettings = 13,
        LoadShortCuts = 15,
    }

    public enum SH29Type : byte
    {
        SendGuildList = 4,
        CreateGuildResponse = 6,
        GuildInviteError = 10,
        GuildInviteRequest = 11,
        UpdateGuildMessageResponse = 17,
        UpdateGuildMemberRankResponse = 23,
        GuildMemberList = 27,
        LeaveGuildResponse = 29,
        ChangeResponse = 39,
        SendUpdateGuildDetails = 45,
        GuildMemberJoined = 54,
        GuildMemberLeft = 56,
        UpdateGuildMemberRank = 57,
        GuildMemberLoggedIn = 61,
        GuildMemberLoggedOut = 62,
        GuildChat = 116,
        GuildNameResult = 119,
        ClearGuildDetailsMessage = 191,
        UnkMessageChange = 196,

    }
    public enum SH37Type : byte
    {
        SendMasterRequestAccept = 3,
        SendMasterRequestReponse = 2,
        SendMasterRequest = 4,
        SendMasterResponseRemove = 7,
        SendRemoveMember = 11,
        SendMasterList = 20,
        SendRegisterApprentice = 21,
        SendMasterMemberOnline = 22,
        SendMasterMemberOffline = 23,
        SendApprenticeRemoveMaster = 24,
        SendApprenticeLevelUp = 25,
        SendApprenticeReward = 26,
        SendRecivveCopper = 61,
        SendGiveMasterReward = 65,
        MasterReiveCopperDecline = 69,
    
    }
    public enum SH38Type : byte
    {
        SendAcademyList = 12,
        SendAcademyMemberList = 14,
        AcademyResponse = 18,
        AcademyMemberJoined = 19,
        LeaveAcademyResponse = 28,
        AcademyChatBlockResponse = 35,
        SendChangeDetailsResponse = 37,
        SendChangeDetails = 38,
        SendJoinGuildFromAcademy = 46,
        SendAcademyGoldRewardList = 50,
        AcademyMemberLeft = 96,
        AcademyMemberLoggedIn = 97,
        AcademyMemberLoggedOut = 98,
        AcademyMemberLevelUp = 102,

        AcademyChat = 105,
        AcademyChatBlocked = 106,
        GuildItemList = 110,
        RemoveFromGuildStore = 115,
        AddToGuildStore = 117,
    }
    public enum SH42Type : byte
    {
        BlockList = 2,
        AddToBlockList = 6,
        RemoveFromBlockList = 10,
        ClearBlockList = 14,
    }
    public enum SH31Type : byte
    {
        LoadUnkown = 7,
    }

    // Kingdom-Quest-System, siehe DOCUMENTATION.md Abschnitt 53+54. Laeuft
    // ueber den WORLD-Server (nicht Zone!), bestaetigt per echtem
    // Mitschnitt (Namen wie "Mara Pirates' Rage", "Lost Mini Dragon[A]/[B]"
    // exakt gefunden). Typ=29 kann mehrfach pro Oeffnen auftreten
    // (unterschiedliche Unterlisten, z.B. "alle" vs. "meine Liste").
    public enum SH22Type : byte
    {
        // Exact original NC_KQ_* names for the packet family currently used
        // by the emulator. Layouts are guarded by audit-kingdom-quest-wire.py.
        KingdomQuestListAck = 2,
        KingdomQuestStatusAck = 4,
        KingdomQuestJoinAck = 6,
        KingdomQuestJoinCancelAck = 8,
        KingdomQuestScheduleAck = 10,
        KingdomQuestNotify = 11,
        KingdomQuestFailed = 19,
        KingdomQuestListTimeAck = 28,
        KingdomQuestListAddAck = 29,
        KingdomQuestListDeleteAck = 30,
        KingdomQuestListUpdateAck = 31,
        KingdomQuestJoiningAlarm = 36,
        KingdomQuestJoiningAlarmEnd = 37,
        KingdomQuestJoiningAlarmList = 38,
        KingdomQuestVoteStartAck = 40,
        KingdomQuestVoteVotingCmd = 41,
        KingdomQuestVoteVotingAck = 43,
        KingdomQuestVoteResultSuccess = 44,
        KingdomQuestVoteResultFail = 45,
        KingdomQuestVoteCancel = 46,
        KingdomQuestVoteBanMessage = 47,
        KingdomQuestVoteBanMessageLogoff = 48,
        KingdomQuestJoinListAck = 50,
        KingdomQuestLinkToForceByBan = 51,
        KingdomQuestVoteStartCheckAck = 53,
        KingdomQuestScoreInfo = 54,
        KingdomQuestTeamSelectAck = 56,
        KingdomQuestTeamSelectCmd = 57,
        KingdomQuestTeamTypeCmd = 58,
        KingdomQuestPlayerDisjoin = 59,
        KingdomQuestScoreBoardInfo = 60,
        KingdomQuestWinterEvent2014Score = 61,

        // Historical capture-era alias. PDB enum resolves type 58 as
        // NC_KQ_TEAM_TYPE_CMD; keep the alias only for source compatibility.
        Unk58 = KingdomQuestTeamTypeCmd,
    }


    // Per echtem Paket-Mitschnitt entdeckt (2016er Client gegen Original-
    // Server, Quest-Abgabe-Sequenz bei NPC Julia). Struktur inzwischen
    // vollstaendig entschluesselt und gegen data_questdialog.sql
    // kreuzverifiziert, siehe DOCUMENTATION.md Abschnitt 54.1.
    public enum SH17Type : byte
    {
        // Original Zone.exe/PDB: NC_QUEST_SCRIPT_CMD_REQ (0x4401).
        // Normaler QSC_SAY-Request ist exakt 105 Byte inklusive Opcode:
        // u16 QuestID + STRUCT_QSC[101]. STRUCT_QSC beginnt mit u32 Cmd,
        // u8 IsPigeonStartType und einer 96-Byte-Union. Fuer SAY (Cmd=2):
        // u32 DialogID/nID, u32 TalkerType (0=NPC,1=ME), u16 NPCNo
        // (default 0xffff), restliche Union-Bytes 0. Die fruehere
        // Interpretation des ersten WORD als Sequenzzaehler ist damit
        // durch PDB und Send_NC_QUEST_SCRIPT_CMD_REQ widerlegt.
        NpcDialogMenu = 1,
        // Per echtem Paket-Mitschnitt entdeckt, siehe DOCUMENTATION.md
        // Abschnitt 35. QuestProgressUpdate trat exakt bei jedem
        // Mob-Tod waehrend einer aktiven Kill-Quest auf.
        QuestProgressUpdate = 13,
        // Original Zone.exe / PROTO_NC_QUEST_SELECT_START_ACK:
        // Header 17 / Type 16 / Opcode 0x4410, body:
        // NPCID u16 + QuestID u16 + ErrorType u16.
        QuestSelectStartAck = 16,
        // Original Send_NC_QUEST_REWARD_NEED_SELECT_ITEM_CMD:
        // Header 17 / Type 18 / opcode 0x4412, body QuestID u16.
        RewardNeedSelectItem = 18,
        DialogSessionStart = 30,
    }
    // Unmittelbar nach der Quest-Belohnungs-Paketkaskade beobachtet
    // (Typ 38, 4 Byte) - vermutlich Bestaetigung/Quest-Log-Update.
    public enum SH16Type : byte
    {
        Unknown38 = 38,
    }
    // Gluecksspielhaus ("Lucky House") - eigene, zusaetzliche Zone-
    // Verbindung (dynamisch zugewiesener Port, siehe DOCUMENTATION.md
    // Abschnitt 53.2/54.5). Struktur inzwischen teilweise entschluesselt.
    public enum SH47Type : byte
    {
        // Bislang komplett unbekannte Familie, einmalig direkt im
        // Belohnungs-Cluster beobachtet (Typ 5, 10 Byte) - unklar ob
        // ueberhaupt zum Gluecksspielhaus gehoerig oder ein Zufallstreffer
        // im selben Header.
        Unknown5 = 5,
        // Antwort auf CH47Type-Typ23 (Objekt ansprechen). 9-Byte-Body:
        // [byte Status][byte 0x26 konstant][u16 LE ObjektID, Echo der
        // Anfrage][u32 LE Objekttyp - 2=Automat/Spielautomat, 1=
        // Wuerfeltisch, an zwei verschiedenen angesprochenen Objekten
        // bestaetigt][byte Flag].
        InteractResult = 24,
        // Antwort auf CH47Type-Typ200 (Spiel/Tisch betreten). 4-Byte-Body,
        // enthaelt einen sich langsam aendernden Zaehler im selben
        // Wertebereich wie InteractResult - vermutlich eine laufende
        // Session-/Rundennummer fuer den Tisch.
        EnterGameResult = 201,
        // Periodischer Broadcast, exakt alle 10 Sekunden, mit UNVERAENDERTEM
        // Payload zwischen zwei Beobachtungen (kein Live-Jackpot, wie in
        // Abschnitt 53.2 noch vermutet). 19-Byte-Body enthaelt u.a. zwei
        // Werte 100 und 500 - plausibel Mindest-/Hoechsteinsatz des
        // Tisches statt eines Timers.
        TableLimitsBroadcast = 216,
        // 50-Byte-Body, Antwort auf CH47Type-Typ100 (Einsatz/Wurf). Enthaelt
        // einen grossen Wert (~150000), moeglicherweise aktueller
        // Jackpot-Pool oder Kontostand - nicht abschliessend verifiziert.
        GameStateResult = 101,
        // Antwort auf CH47Type-Typ104 (Spiel verlassen), 2-Byte-Body.
        LeaveGameResult = 105,
    }
}
