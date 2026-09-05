-- Aus ClassName.shn generiert (28 Zeilen, 4 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_classname`;
CREATE TABLE `data_classname` (
  `ClassID` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `acPrefix` VARCHAR(4) NOT NULL DEFAULT '',
  `acEngName` VARCHAR(16) NOT NULL DEFAULT '',
  `acLocalName` VARCHAR(32) NOT NULL DEFAULT '',
  PRIMARY KEY (`ClassID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_classname` (`ClassID`, `acPrefix`, `acEngName`, `acLocalName`) VALUES
  (0, '-', '-', '-'),
  (1, 'Fig', 'Fighter', 'Fighter'),
  (2, 'Cfi', 'CleverFighter', 'CleverFighter'),
  (3, 'War', 'Warrior', 'Warrior'),
  (4, 'Gla', 'Gladiator', 'Gladiator'),
  (5, 'Kni', 'Knight', 'Knight'),
  (6, 'Cle', 'Cleric', 'Cleric'),
  (7, 'Hcl', 'HighCleric', 'HighCleric'),
  (8, 'Pal', 'Paladin', 'Paladin'),
  (9, 'Hol', 'HolyKnight', 'HolyKnight'),
  (10, 'Gua', 'Guardian', 'Guardian'),
  (11, 'Arc', 'Archer', 'Archer'),
  (12, 'Har', 'HawkArcher', 'HawkArcher'),
  (13, 'Sco', 'Scout', 'Scout'),
  (14, 'Sha', 'SharpShooter', 'SharpShooter'),
  (15, 'Ran', 'Ranger', 'Ranger'),
  (16, 'Mag', 'Mage', 'Mage'),
  (17, 'Wma', 'WizMage', 'WizMage'),
  (18, 'Enc', 'Enchanter', 'Enchanter'),
  (19, 'War', 'Warlock', 'Warlock'),
  (20, 'Wiz', 'Wizard', 'Wizard'),
  (21, 'Jok', 'Joker', 'Trickster'),
  (22, 'Chs', 'Chaser', 'Gambit'),
  (23, 'Cru', 'Cruel', ' Renegade '),
  (24, 'Cls', 'Closer', 'Spectre'),
  (25, 'Ass', 'Assassin', 'Reaper'),
  (26, 'Sen', 'Sentinel', 'Crusader'),
  (27, 'Sav', 'Savior', 'Templar');
