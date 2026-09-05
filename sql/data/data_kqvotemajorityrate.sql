-- Aus KQVoteMajorityRate.shn generiert (2 Zeilen, 1 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_kqvotemajorityrate`;
CREATE TABLE `data_kqvotemajorityrate` (
  `VoteAgreeRate` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`VoteAgreeRate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_kqvotemajorityrate` (`VoteAgreeRate`) VALUES
  (70),
  (50);
