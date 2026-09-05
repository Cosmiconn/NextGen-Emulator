-- Aus KQVoteDesc.shn generiert (4 Zeilen, 3 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_kqvotedesc`;
CREATE TABLE `data_kqvotedesc` (
  `ID` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `KQVoteTitle` VARCHAR(64) NOT NULL DEFAULT '',
  `KQVoteDescription` VARCHAR(256) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_kqvotedesc` (`ID`, `KQVoteTitle`, `KQVoteDescription`) VALUES
  (1, 'Impolite behavior', 'I request voting for: impolite behavior.'),
  (2, 'Improper gameplay', 'I request voting for: improper gameplay.'),
  (3, 'Abusive language', 'I request voting for: abusive language/insults.'),
  (4, 'Abusing game system', 'I request voting for: abuse of the game system.');
