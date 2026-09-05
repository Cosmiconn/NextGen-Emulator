-- Aus GBHouse.shn generiert (1 Zeilen, 5 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_gbhouse`;
CREATE TABLE `data_gbhouse` (
  `GB_GameMoney` INT UNSIGNED NOT NULL DEFAULT 0,
  `GB_ExchangeTax` INT UNSIGNED NOT NULL DEFAULT 0,
  `GB_ResetTimeHour` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `GB_ResetTimeMin` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `GB_ResetTimeSec` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`GB_GameMoney`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_gbhouse` (`GB_GameMoney`, `GB_ExchangeTax`, `GB_ResetTimeHour`, `GB_ResetTimeMin`, `GB_ResetTimeSec`) VALUES
  (10, 20, 6, 0, 0);
