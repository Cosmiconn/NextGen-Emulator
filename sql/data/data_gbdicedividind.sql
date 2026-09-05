-- Aus GBDiceDividind.shn generiert (1 Zeilen, 16 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_gbdicedividind`;
CREATE TABLE `data_gbdicedividind` (
  `DividendRate` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 0` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 1` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 2` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 3` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 4` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 5` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 6` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 7` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 8` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 9` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 10` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 11` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 12` SMALLINT NOT NULL DEFAULT 0,
  `Undefined 13` SMALLINT NOT NULL DEFAULT 0,
  `AnyTriple` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`DividendRate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_gbdicedividind` (`DividendRate`, `Undefined 0`, `Undefined 1`, `Undefined 2`, `Undefined 3`, `Undefined 4`, `Undefined 5`, `Undefined 6`, `Undefined 7`, `Undefined 8`, `Undefined 9`, `Undefined 10`, `Undefined 11`, `Undefined 12`, `Undefined 13`, `AnyTriple`) VALUES
  (100, 100, 800, 15000, 2400, 5000, 3000, 1800, 1200, 800, 600, 500, 100, 200, 300, 1);
