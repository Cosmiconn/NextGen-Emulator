-- Aus KQTeam.shn generiert (8 Zeilen, 8 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_kqteam`;
CREATE TABLE `data_kqteam` (
  `ID` SMALLINT NOT NULL DEFAULT 0,
  `MaxMemberGap` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `IsTeamPVP` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `KQTeamDivideType` SMALLINT NOT NULL DEFAULT 0,
  `RegenXRed` INT UNSIGNED NOT NULL DEFAULT 0,
  `RegenYRed` INT UNSIGNED NOT NULL DEFAULT 0,
  `RegenXBlue` INT UNSIGNED NOT NULL DEFAULT 0,
  `RegenYBlue` INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_kqteam` (`ID`, `MaxMemberGap`, `IsTeamPVP`, `KQTeamDivideType`, `RegenXRed`, `RegenYRed`, `RegenXBlue`, `RegenYBlue`) VALUES
  (26, 1, 0, 1, 764, 3542, 940, 1798),
  (28, 1, 1, 1, 5036, 3176, 1325, 3216),
  (29, 1, 1, 1, 5036, 3176, 1325, 3216),
  (30, 1, 1, 1, 5036, 3176, 1325, 3216),
  (31, 1, 1, 1, 5036, 3176, 1325, 3216),
  (32, 1, 1, 1, 5036, 3176, 1325, 3216),
  (33, 1, 1, 1, 5036, 3176, 1325, 3216),
  (38, 1, 0, 1, 7411, 6150, 6415, 7147);
