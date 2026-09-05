-- Aus KQIsVote.shn generiert (30 Zeilen, 2 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_kqisvote`;
CREATE TABLE `data_kqisvote` (
  `ID` SMALLINT NOT NULL DEFAULT 0,
  `IsVote` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_kqisvote` (`ID`, `IsVote`) VALUES
  (0, 1),
  (1, 1),
  (2, 1),
  (3, 1),
  (5, 1),
  (16, 1),
  (13, 1),
  (14, 1),
  (15, 1),
  (4, 1),
  (17, 0),
  (18, 0),
  (19, 0),
  (20, 0),
  (21, 1),
  (22, 1),
  (23, 0),
  (24, 1),
  (26, 0),
  (28, 0),
  (29, 0),
  (30, 0),
  (31, 0),
  (32, 0),
  (33, 0),
  (27, 1),
  (35, 0),
  (37, 1),
  (25, 0),
  (38, 0);
