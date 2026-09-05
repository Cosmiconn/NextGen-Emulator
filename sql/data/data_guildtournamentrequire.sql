-- Aus GuildTournamentRequire.shn generiert (1 Zeilen, 3 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_guildtournamentrequire`;
CREATE TABLE `data_guildtournamentrequire` (
  `MinLv` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `MinMem` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `JoinMoney` INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`MinLv`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_guildtournamentrequire` (`MinLv`, `MinMem`, `JoinMoney`) VALUES
  (70, 10, 1000000);
