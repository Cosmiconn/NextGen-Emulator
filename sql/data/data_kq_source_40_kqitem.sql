-- Source KQItem.shn; sha256=2f641d273017bbd41f41f2ffb88df00ac92c1090b51f6438281bc185b1b2814a; records=2; columns=4
DROP TABLE IF EXISTS `data_kqitem`;
CREATE TABLE `data_kqitem` (
  `ItemIndex` VARCHAR(32) NOT NULL,
  `MoveSpdRate` SMALLINT UNSIGNED NOT NULL,
  `AbsoluteAttack` SMALLINT UNSIGNED NOT NULL,
  `PickupLimit` SMALLINT UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_kqitem` (`ItemIndex`, `MoveSpdRate`, `AbsoluteAttack`, `PickupLimit`) VALUES
  ('KQ_InvincibleHammer', 350, 30000, 1),
  ('KQ_Ice01', 0, 0, 1);
