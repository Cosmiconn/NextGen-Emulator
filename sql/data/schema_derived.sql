-- ============================================================================
-- NextGen-Emulator - Ergaenzendes Schema fuer fiesta_data
--
-- Diese Tabellen haben KEINE Entsprechung unter den 130 realen .shn-Dateien
-- aus dem NA2016-Client (siehe DOCUMENTATION.md, Abschnitt 9) - sie wurden
-- vermutlich von der Zepheus/DragonFiesta/Estrella-Community von Hand
-- angelegt, um Serverlogik abzubilden, die im Original ggf. anders (Stored
-- Procedures, Server-eigene .shn-Varianten) geloest war.
--
-- HERKUNFT: Ausschliesslich aus row["..."]-Zugriffen im vorhandenen
-- (GPL-lizenzierten) NextGen-Emulator-C#-Code abgeleitet. Keine Nutzung der
-- geleakten NA2016-Server-Dateien.
--
-- Fuer data_iteminfo, data_mobinfo, mapinfo, activeskill, minihouse siehe
-- die einzelnen Dateien in diesem Ordner (data_iteminfo.sql etc.) - die
-- enthalten bereits CREATE TABLE + echte Daten aus dem Client.
-- ============================================================================

USE `fiesta_data`;

-- ----------------------------------------------------------------------------
-- BaseStats  (Attribut-Tabelle pro Klasse+Level, sehr breite Tabelle)
-- Quelle: NextGen.FiestaLib/Data/FiestaBaseStat.cs, NextGen.Zone/Data/DataProvider.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `BaseStats`;
CREATE TABLE `BaseStats` (
  `Class` VARCHAR(32) NOT NULL,
  `Level` INT NOT NULL,
  `Strength` INT NOT NULL DEFAULT 0,
  `Constitution` INT NOT NULL DEFAULT 0,
  `Intelligence` INT NOT NULL DEFAULT 0,
  `Dexterity` INT NOT NULL DEFAULT 0,
  `MentalPower` INT NOT NULL DEFAULT 0,
  `SoulHP` INT NOT NULL DEFAULT 0,
  `MAXSoulHP` INT NOT NULL DEFAULT 0,
  `PriceHPStone` INT NOT NULL DEFAULT 0,
  `SoulSP` INT NOT NULL DEFAULT 0,
  `MAXSoulSP` INT NOT NULL DEFAULT 0,
  `PriceSPStone` INT NOT NULL DEFAULT 0,
  `AtkPerAP` INT NOT NULL DEFAULT 0,
  `DmgPerAP` INT NOT NULL DEFAULT 0,
  `MaxPwrStone` INT NOT NULL DEFAULT 0,
  `NumPwrStone` INT NOT NULL DEFAULT 0,
  `PricePwrStone` INT NOT NULL DEFAULT 0,
  `PwrStoneWC` INT NOT NULL DEFAULT 0,
  `PwrStoneMA` INT NOT NULL DEFAULT 0,
  `MaxGrdStone` INT NOT NULL DEFAULT 0,
  `NumGrdStone` INT NOT NULL DEFAULT 0,
  `PriceGrdStone` INT NOT NULL DEFAULT 0,
  `GrdStoneAC` INT NOT NULL DEFAULT 0,
  `GrdStoneMR` INT NOT NULL DEFAULT 0,
  `PainRes` INT NOT NULL DEFAULT 0,
  `RestraintRes` INT NOT NULL DEFAULT 0,
  `CurseRes` INT NOT NULL DEFAULT 0,
  `ShockRes` INT NOT NULL DEFAULT 0,
  `MaxHP` INT UNSIGNED NOT NULL DEFAULT 0,
  `MaxSP` INT UNSIGNED NOT NULL DEFAULT 0,
  `CharTitlePt` INT NOT NULL DEFAULT 0,
  `SkillPwrPt` INT NOT NULL DEFAULT 0,
  `HPStoneEffectID` INT NOT NULL DEFAULT 0,
  `SPStoneEffectID` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`Class`, `Level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- ItemStats  (Attribut-Boni pro Item, per InxName verknuepft)
-- Quelle: NextGen.FiestaLib/Data/ItemStats.cs, NextGen.Zone/Data/DataProvider.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `ItemStats`;
CREATE TABLE `ItemStats` (
  `itemIndex` VARCHAR(32) NOT NULL,
  `Str` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `con` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Dex` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Int` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Spr` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`itemIndex`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- blockinfo  (Block&Walk-Kollisionsgrid pro Map - vgl. TheSeed-Map-Editor-Projekt)
-- Quelle: NextGen.FiestaLib/Data/BlockInfo.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `blockinfo`;
CREATE TABLE `blockinfo` (
  `MapID` SMALLINT UNSIGNED NOT NULL,
  `Width` INT NOT NULL,
  `Height` INT NOT NULL,
  `Byte` MEDIUMBLOB NULL,
  PRIMARY KEY (`MapID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- Vendors  (Verkaufsliste pro Handels-NPC)
-- Quelle: NextGen.Zone/Data/DataProvider.cs (LoadVendors)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Vendors`;
CREATE TABLE `Vendors` (
  `NPCID` SMALLINT NOT NULL,
  `InvSlot` TINYINT UNSIGNED NOT NULL,
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  PRIMARY KEY (`NPCID`, `InvSlot`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- ShineNpc  (NPC-Platzierung pro Map, inkl. Vendor-Flag)
-- Quelle: NextGen.FiestaLib/Data/ShineNPC.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `ShineNpc`;
CREATE TABLE `ShineNpc` (
  `MobID` SMALLINT NOT NULL,
  `MobName` VARCHAR(32) NOT NULL,
  `Map` VARCHAR(16) NOT NULL,
  `RegenX` INT NOT NULL,
  `RegenY` INT NOT NULL,
  `Direct` SMALLINT NOT NULL DEFAULT 0,
  `NPCMenu` TINYINT NOT NULL DEFAULT 0,
  `Role` VARCHAR(64) NULL,
  `RoleArg0` VARCHAR(64) NULL,
  `Flags` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`MobID`, `Map`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- LinkTable  (Map-Uebergaenge/Teleporter)
-- Quelle: NextGen.FiestaLib/Data/LinkTable.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `LinkTable`;
CREATE TABLE `LinkTable` (
  `argument` VARCHAR(32) NOT NULL,
  `MapServer` VARCHAR(16) NOT NULL,
  `MapClient` VARCHAR(16) NOT NULL,
  `Coord_X` INT NOT NULL,
  `Coord_Y` INT NOT NULL,
  `Direct` SMALLINT NOT NULL DEFAULT 0,
  `LevelFrom` SMALLINT NOT NULL DEFAULT 0,
  `LevelTo` SMALLINT NOT NULL DEFAULT 0,
  `Party` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`argument`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- dropgroupinfo / itemdroptable  (Drop-System)
-- Quelle: NextGen.FiestaLib/Data/DropGroupInfo.cs, NextGen.Zone/Data/DataProvider.cs
-- (LoadDrops)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `dropgroupinfo`;
CREATE TABLE `dropgroupinfo` (
  `GroupID` VARCHAR(32) NOT NULL,
  `MinCount` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `MaxCount` TINYINT UNSIGNED NOT NULL DEFAULT 1,
  PRIMARY KEY (`GroupID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `itemdroptable`;
CREATE TABLE `itemdroptable` (
  `MobId` VARCHAR(32) NOT NULL,
  `MinLevel` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `MaxLevel` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `GroupID` VARCHAR(32) NOT NULL,
  `Rate` FLOAT NOT NULL DEFAULT 0,
  PRIMARY KEY (`MobId`, `GroupID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- expTable  (Erfahrungspunkte-Bedarf pro Level)
-- Quelle: NextGen.Zone/Data/DataProvider.cs (LoadExpTable)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `expTable`;
CREATE TABLE `expTable` (
  `Level` TINYINT UNSIGNED NOT NULL,
  `Exp` BIGINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`Level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- recall  (Teleport-Item-Zielkoordinaten)
-- Quelle: NextGen.FiestaLib/Data/RecallCoordinate.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `recall`;
CREATE TABLE `recall` (
  `ItemIndex` VARCHAR(32) NOT NULL,
  `MapName` VARCHAR(16) NOT NULL,
  `LinkX` SMALLINT NOT NULL,
  `LinkY` SMALLINT NOT NULL,
  PRIMARY KEY (`ItemIndex`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- Mounts  (Reittier-Konfiguration pro Item)
-- Quelle: NextGen.FiestaLib/Data/Mount.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Mounts`;
CREATE TABLE `Mounts` (
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  `Level` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Tickspeed` INT NOT NULL DEFAULT 0,
  `Handle` SMALLINT UNSIGNED NOT NULL,
  `Food` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Speed` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `CastTime` INT NOT NULL DEFAULT 0,
  `Cooldown` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `permanent` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- MasterRewardStates  (Meister-Belohnungssystem, Attributboni pro Item)
-- Quelle: NextGen.FiestaLib/Data/MasterRewardState.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `MasterRewardStates`;
CREATE TABLE `MasterRewardStates` (
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  `Str` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `End` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Dex` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Int` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Spr` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- data_MobInfoServer  (Server-seitige, erweiterte Mob-Kampfwerte -
-- Gegenstueck zu data_mobinfo/MobInfo.shn, aber ohne Client-.shn-Entsprechung)
-- Quelle: NextGen.FiestaLib/Data/MobInfoServer.cs (vollstaendig, 45 Spalten)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `data_MobInfoServer`;
CREATE TABLE `data_MobInfoServer` (
  `ID` INT UNSIGNED NOT NULL,
  `InxName` VARCHAR(32) NOT NULL,
  `Visible` TINYINT UNSIGNED NOT NULL DEFAULT 1,
  `AC` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `TB` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `MR` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `MB` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `EnemyDetectType` INT UNSIGNED NOT NULL DEFAULT 0,
  `MobKillInx` INT UNSIGNED NOT NULL DEFAULT 0,
  `MonEXP` INT UNSIGNED NOT NULL DEFAULT 0,
  `EXPRange` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `DetectCha` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `ResetInterval` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `CutInterval` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `CutNonAT` INT UNSIGNED NOT NULL DEFAULT 0,
  `FollowCha` INT UNSIGNED NOT NULL DEFAULT 0,
  `PceHPRcvDly` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `PceHPRcv` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `AtkHPRcvDly` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `AtkHPRcv` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Str` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Dex` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Con` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Int` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Men` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `MobRaceType` INT UNSIGNED NOT NULL DEFAULT 0,
  `Rank` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `FamilyArea` INT UNSIGNED NOT NULL DEFAULT 0,
  `FamilyRescArea` INT UNSIGNED NOT NULL DEFAULT 0,
  `FamilyRescCount` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `BloodingResi` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `StunResi` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `MoveSpeedResi` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `FearResi` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `ResIndex` VARCHAR(32) NULL,
  `KQKillPoint` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Return2Regen` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `IsRoaming` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `RoamingNumber` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `RoamingDistance` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `MaxSP` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `BroadAtDead` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `TurnSpeed` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `WalkChase` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `AllCanLoot` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `DmgByHealMin` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `DmgByHealMax` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- data_itemuseeffect  (Nutzeffekte von Verbrauchsgegenstaenden, bis zu 3 pro Item)
-- Quelle: NextGen.FiestaLib/Data/ItemUseEffectInfo.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `data_itemuseeffect`;
CREATE TABLE `data_itemuseeffect` (
  `ItemIndex` VARCHAR(32) NOT NULL,
  `UseEffectA` INT UNSIGNED NOT NULL DEFAULT 0,
  `UseValueA` INT UNSIGNED NOT NULL DEFAULT 0,
  `UseEffectB` INT UNSIGNED NOT NULL DEFAULT 0,
  `UseValueB` INT UNSIGNED NOT NULL DEFAULT 0,
  `UseEffectC` INT UNSIGNED NOT NULL DEFAULT 0,
  `UseValueC` INT UNSIGNED NOT NULL DEFAULT 0,
  `UseAbStateName` VARCHAR(32) NULL,
  PRIMARY KEY (`ItemIndex`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- data_iteminfoserver  (verknuepft Items mit bis zu 3 Drop-Gruppen)
-- Quelle: NextGen.Zone/Data/DataProvider.cs (dropGroupNames-Array + Konsument)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `data_iteminfoserver`;
CREATE TABLE `data_iteminfoserver` (
  `ID` SMALLINT UNSIGNED NOT NULL,
  `DropGroupA` VARCHAR(32) NOT NULL DEFAULT '',
  `DropGroupB` VARCHAR(32) NOT NULL DEFAULT '',
  `RandomOptionDropGroup` VARCHAR(32) NOT NULL DEFAULT '',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- Mobspawn  (Spawnpunkte, siehe NextGen.Zone/Game/Map.cs / CommandHandler.cs)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Mobspawn`;
CREATE TABLE `Mobspawn` (
  `ID` BIGINT NOT NULL AUTO_INCREMENT,
  `MobID` SMALLINT UNSIGNED NOT NULL,
  `MapID` SMALLINT UNSIGNED NOT NULL,
  `PosX` INT NOT NULL DEFAULT 0,
  `PosY` INT NOT NULL DEFAULT 0,
  `InstanceID` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`),
  KEY `idx_mobspawn_map` (`MapID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- data_mobcoordinate  -  UNGEKLAERT, nicht angelegt.
-- Die einzige Fundstelle im Code (NextGen.World/Data/DataProvider.cs, Zeile 71)
-- ist auskommentiert ("//  Program.DatabaseManager...UPDATE data_mobcoordinate
-- SET mapname=..."). Kein lebender Code liest oder schreibt diese Tabelle -
-- vermutlich Ueberbleibsel eines nie fertiggestellten Features. Absichtlich
-- nicht angelegt, um kein Rateschema zu erzeugen. Es gibt eine echte
-- MobCoordinate.shn im Client (siehe DOCUMENTATION.md Abschnitt 9-Kandidaten),
-- die aber vom aktuellen Code nirgends konsumiert wird.
-- ----------------------------------------------------------------------------

-- ----------------------------------------------------------------------------
-- badnames  (Namensfilter bei Charaktererstellung)
-- Quelle: NextGen.World/Data/DataProvider.cs (LoadBadNames) - Kommentar im
-- Code nennt explizit "Columns: BadName Type", genutzt wird nur BadName.
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `badnames`;
CREATE TABLE `badnames` (
  `BadName` VARCHAR(32) NOT NULL,
  `Type` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`BadName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- MasterRewards  (Belohnungs-Item-Zuordnung pro Job+Level fuer das
-- Meister/Schueler-System - andere Tabelle als MasterRewardStates!)
-- Quelle: NextGen.FiestaLib/Data/MasterRewardItem.cs (erbt von
-- MasterRewardState, siehe data_ItemStats-artige Spalten oben, plus Job/Level)
-- ACHTUNG: Wird per "USE fiesta_data; ... USE fiesta_world" umgeschaltet -
-- diese Tabelle liegt also tatsaechlich in fiesta_data trotz World-naher
-- Bedeutung (NextGen.World/Data/DataProvider.cs, LoadMasterRewards).
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `MasterRewards`;
CREATE TABLE `MasterRewards` (
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  `Job` TINYINT UNSIGNED NOT NULL,
  `Level` TINYINT UNSIGNED NOT NULL,
  `Count` TINYINT UNSIGNED NOT NULL DEFAULT 1,
  `Str` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `End` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Dex` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Int` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `Spr` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`Job`, `Level`, `ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- Buffs  -  NIEDRIGE KONFIDENZ, vermutlich toter Code.
-- Quelle: NextGen.World/Game/MapObjectBuffCollection.cs. Diese Klasse:
--   1) liegt im Namespace "Fiesta.Zone.Game.Buffs" statt "NextGen.World...."
--      (Ueberbleibsel aus einer noch aelteren Codebasis als Estrella selbst),
--   2) nutzt System.Data.SqlClient (SqlConnection/SqlCommand/SqlParameter) -
--      also MICROSOFT SQL SERVER, nicht MySQL/MariaDB wie der Rest des
--      Projekts,
--   3) wird nachweislich von KEINER anderen Stelle im Code aufgerufen
--      (grep-verifiziert) - toter, nie aufgerufener Code.
-- Selbst wenn sie aufgerufen wuerde, waere sie inkompatibel (SqlConnection
-- funktioniert nicht gegen MySQL). Tabelle hier nur der Vollstaendigkeit
-- halber angelegt (Spalten aus GetInt16/GetInt32/GetInt64-Aufrufen an den
-- Ordinalpositionen 0-3 grob rekonstruiert), NICHT verifiziert, vermutlich
-- fuer den produktiven Betrieb irrelevant.
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Buffs`;
CREATE TABLE `Buffs` (
  `ID` BIGINT NOT NULL AUTO_INCREMENT,
  `CharacterID` INT NOT NULL,
  `AbStateID` SMALLINT UNSIGNED NOT NULL,
  `Value` INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`),
  KEY `idx_buffs_character` (`CharacterID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
