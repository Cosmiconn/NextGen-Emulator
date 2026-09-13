-- Exact QuestData.shn -> MobInfo -> QuestDialog start mapping.
-- StartingNpc is the MobInfo.ID decoded from QuestData.shn.
-- MobInfo.InxName is the same NPC type name exposed by ShineNpc.MobName.
-- No quest eligibility is inferred here; one NPC type may legitimately
-- expose multiple distinct start dialogs and requires character quest state.

CREATE OR REPLACE VIEW `data_quest_npc_start` AS
SELECT
    m.`InxName` AS `MobName`,
    q.`StartingNpc`,
    q.`QuestID`,
    s.`DialogID`
FROM `data_quest` q
INNER JOIN `data_quest_start_dialog` s ON s.`QuestID` = q.`QuestID`
INNER JOIN `data_mobinfo` m ON m.`ID` = q.`StartingNpc`
WHERE q.`StartingNpc` <> 0;
