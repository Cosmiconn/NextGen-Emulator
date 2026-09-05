-- Aus GuildTournamentSkill.shn generiert (6 Zeilen, 6 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_guildtournamentskill`;
CREATE TABLE `data_guildtournamentskill` (
  `MAP_TYPE` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Index` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `DeathPoint` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `StaName` VARCHAR(32) NOT NULL DEFAULT '',
  `TargetType` INT UNSIGNED NOT NULL DEFAULT 0,
  `DlyTime` INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`Index`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_guildtournamentskill` (`MAP_TYPE`, `Index`, `DeathPoint`, `StaName`, `TargetType`, `DlyTime`) VALUES
  (0, 0, 20, 'StaGldRestore', 10, 3000),
  (0, 1, 15, 'StaGldMoveSpeedUp', 10, 3000),
  (0, 2, 80, 'StaGldACMinus', 9, 3000),
  (0, 3, 50, 'StaGldSlow', 9, 3000),
  (0, 4, 60, 'StaGldStun', 9, 3000),
  (0, 5, 0, 'StaGldAtkUp', 10, 10000);
