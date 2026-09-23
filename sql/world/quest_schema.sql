-- Quest runtime persistence.
-- The original SQL Server backup supplied with the project proves tQuest and
-- tQuestTimes as the character quest persistence contract. Progress is kept in
-- a separate normalized table because the emulator needs per-objective counters.
CREATE TABLE IF NOT EXISTS `tQuest` (
  `nCharNo` INT NOT NULL,
  `nQuestNo` INT UNSIGNED NOT NULL,
  `nStatus` TINYINT UNSIGNED NOT NULL,
  `sData` VARBINARY(100) NULL,
  PRIMARY KEY (`nCharNo`,`nQuestNo`),
  KEY `idx_tquest_char_status` (`nCharNo`,`nStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `tQuestTimes` (
  `nCharNo` INT NOT NULL,
  `nQuestNo` INT UNSIGNED NOT NULL,
  `nTimes` INT UNSIGNED NOT NULL DEFAULT 0,
  `dLastComplete` DATETIME NULL,
  PRIMARY KEY (`nCharNo`,`nQuestNo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `character_quest_progress` (
  `CharID` INT NOT NULL,
  `QuestID` INT UNSIGNED NOT NULL,
  `Slot` TINYINT UNSIGNED NOT NULL,
  `Progress` INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`CharID`,`QuestID`,`Slot`),
  KEY `idx_cqp_quest` (`QuestID`,`Slot`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
