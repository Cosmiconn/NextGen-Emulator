-- Source: EnchantSocketRate; sha256=777b29ac1c42481cc8887f169f1b70bb79467f82506bbf7d670d5a698fc54605; records=5; columns=4
-- Columns: ItemGradeType:11:4  Socket0:2:2  Socket1:2:2  Socket2:2:2
DROP TABLE IF EXISTS `data_enchantsocketrate`;
CREATE TABLE `data_enchantsocketrate` (
  `__SourceRow` INT UNSIGNED NOT NULL,
  `ItemGradeType` INT UNSIGNED NOT NULL,
  `Socket0` SMALLINT UNSIGNED NOT NULL,
  `Socket1` SMALLINT UNSIGNED NOT NULL,
  `Socket2` SMALLINT UNSIGNED NOT NULL,
  PRIMARY KEY (`__SourceRow`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_enchantsocketrate` (`__SourceRow`,`ItemGradeType`,`Socket0`,`Socket1`,`Socket2`) VALUES
  (0,0,0,100,100),
  (1,1,0,100,80),
  (2,2,0,100,40),
  (3,5,0,100,20),
  (4,3,0,100,10);
