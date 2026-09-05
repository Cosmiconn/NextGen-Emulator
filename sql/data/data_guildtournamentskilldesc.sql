-- Aus GuildTournamentSkillDesc.shn generiert (6 Zeilen, 6 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_guildtournamentskilldesc`;
CREATE TABLE `data_guildtournamentskilldesc` (
  `MAP_TYPE` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Index` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `IconIndex` INT UNSIGNED NOT NULL DEFAULT 0,
  `IconFile` VARCHAR(32) NOT NULL DEFAULT '',
  `Name` VARCHAR(32) NOT NULL DEFAULT '',
  `Description` VARCHAR(64) NOT NULL DEFAULT '',
  PRIMARY KEY (`MAP_TYPE`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_guildtournamentskilldesc` (`MAP_TYPE`, `Index`, `IconIndex`, `IconFile`, `Name`, `Description`) VALUES
  (0, 0, 7, 'ClericSk00', 'Continuous Group Heal', 'Heals 1% of team\'s HP every 1 second for 2 minutes'),
  (0, 1, 17, 'Prdct000', 'Group Speed Increase', 'Increases team\'s travel speed by 20% for 3 minutes'),
  (0, 2, 21, 'ClericSk00', 'Group Defense Decrease', 'Decreases enemy\'s defense by 20% for 2 minutes'),
  (0, 3, 18, 'MageSk00', 'Group Speed Decrease', 'Decreases enemy\'s travel speed by 30% for 3 minutes'),
  (0, 4, 17, 'FighterSk00', 'Group Blackout', 'Blackout the enemies for 3 seconds'),
  (0, 5, 9, 'ClericSk00', 'Group Attack Increase', 'Increase team\'s attack by 30% for 5 minutes');
