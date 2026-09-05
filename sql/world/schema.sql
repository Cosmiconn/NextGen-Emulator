-- ============================================================================
-- NextGen-Emulator - Schema fuer die World-Datenbank (fiesta_world)
--
-- HERKUNFT: Nicht aus geleakten Server-Dateien uebernommen. Jede Spalte
-- wurde direkt aus den tatsaechlichen SQL-Queries und row["..."]-Zugriffen
-- im vorhandenen (GPL-lizenzierten) NextGen-Emulator-Code abgeleitet -
-- also aus Code, den dieses Projekt bereits besitzt und gerade haertet.
--
-- KEINE Garantie fuer Vollstaendigkeit oder exakte Spaltentypen/-laengen -
-- nicht gegen einen echten Server oder echte Spielstanddaten verifiziert
-- (im Gegensatz zu sql/data/*.sql, das echte Client-.shn-Daten importiert).
-- Bitte vor Produktivnutzung gegenpruefen. Siehe DOCUMENTATION.md.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `fiesta_world` DEFAULT CHARACTER SET utf8mb4;
USE `fiesta_world`;

-- ----------------------------------------------------------------------------
-- characters
-- Quellen: NextGen.Database/DataStore/ReadMethods.cs (ReadCharObjectByIDFromDatabase),
-- NextGen.World/Networking/WorldClient.cs (Charaktererstellung + Laden),
-- NextGen.Database/Storage/{PositionInfo,LookInfo,CharacterStats}.cs,
-- NextGen.World/Data/WorldCharacter.cs, NextGen.World/Data/DatabaseHelper.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `characters`;
CREATE TABLE `characters` (
  `CharID` INT NOT NULL AUTO_INCREMENT,
  `AccountID` INT NOT NULL,
  `Name` VARCHAR(16) NOT NULL,
  `Slot` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Job` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Level` TINYINT UNSIGNED NOT NULL DEFAULT 1,
  `Male` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Hair` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `HairColor` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Face` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Map` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  `XPos` INT NOT NULL DEFAULT 0,
  `YPos` INT NOT NULL DEFAULT 0,
  `Money` BIGINT NOT NULL DEFAULT 0,
  `Exp` BIGINT NOT NULL DEFAULT 0,
  `CurHP` INT NOT NULL DEFAULT 0,
  `CurSP` INT NOT NULL DEFAULT 0,
  `StatPoints` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `UsablePoints` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Str` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `End` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Dex` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Spr` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `StrInt` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `GuildID` INT NULL DEFAULT NULL,
  `AcademyID` INT NULL DEFAULT NULL,
  `GroupID` BIGINT NULL DEFAULT NULL,
  `IsGroupMaster` TINYINT UNSIGNED NULL DEFAULT NULL,
  `MountID` INT NOT NULL DEFAULT 0,
  `MountFood` INT NOT NULL DEFAULT 0,
  -- Ungeklaerter Namenskonflikt: WorldCharacter.cs.UpdateRecviveCoper() schreibt
  -- "ReviveCoper" in eine Tabelle "character" (Singular!), waehrend
  -- WorldClient.cs beim Laden "MasterReciveMoney" aus "characters" (Plural)
  -- liest. Vermutlich ein vorbestehender Bug im Original-Code (falscher
  -- Tabellen-/Spaltenname in UpdateRecviveCoper) - hier wird die beim Laden
  -- tatsaechlich genutzte Spalte angelegt, UpdateRecviveCoper() muesste
  -- separat korrigiert werden. Siehe DOCUMENTATION.md.
  `MasterReciveMoney` BIGINT NOT NULL DEFAULT 0,
  `MasterJoin` DATETIME NOT NULL,
  -- PvP-Kill-Punkte, siehe DOCUMENTATION.md Abschnitt 26. Anders als Fame
  -- (weiterhin nicht persistiert, siehe Abschnitt 25) wird das hier
  -- tatsaechlich geladen/gespeichert (ReadMethods.cs / ZoneCharacter.Save()).
  `KillPoints` INT NOT NULL DEFAULT 0,
  -- Titel-Fortschritt TOTAL_KILL_MOB, siehe DOCUMENTATION.md Abschnitt 41.
  `TotalMobKills` INT UNSIGNED NOT NULL DEFAULT 0,
  `MobKillTitleTier` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `PvPKillTitleTier` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `NpcBuyCount` INT UNSIGNED NOT NULL DEFAULT 0,
  `NpcBuyTitleTier` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `NpcSellCount` INT UNSIGNED NOT NULL DEFAULT 0,
  `NpcSellTitleTier` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `FriendCount` INT UNSIGNED NOT NULL DEFAULT 0,
  `FriendCountTitleTier` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `TotalTitlesEarned` INT UNSIGNED NOT NULL DEFAULT 0,
  `FameCountTitleTier` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  -- Blob-Felder werden als Hex-String gespeichert (ByteArrayToStringForBlobSave),
  -- nicht als Binaerdaten - TEXT reicht, MEDIUMTEXT als Sicherheitsmarge.
  `QuickBar` MEDIUMTEXT NULL,
  `QuickBarState` MEDIUMTEXT NULL,
  `ShortCuts` MEDIUMTEXT NULL,
  `GameSettings` MEDIUMTEXT NULL,
  `ClientSettings` MEDIUMTEXT NULL,
  PRIMARY KEY (`CharID`),
  UNIQUE KEY `uq_characters_name` (`Name`),
  KEY `idx_characters_accountid` (`AccountID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- equips  (ausgeruestete Items, pro Charakter-Slot)
-- Quellen: WorldClient.cs (INSERT), Estrella/DragonFiesta-Konvention:
-- ID via AUTO_INCREMENT / LAST_INSERT_ID (siehe sql/do-not-use/SQL scripts/produrefull.sql,
-- give_equip-Prozedur - nur die ID-Erzeugungslogik uebernommen, keine
-- proprietaere Serverlogik)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `equips`;
CREATE TABLE `equips` (
  `ID` BIGINT NOT NULL AUTO_INCREMENT,
  `owner` INT NOT NULL,
  `slot` TINYINT NOT NULL,
  `EquipID` SMALLINT UNSIGNED NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `idx_equips_owner` (`owner`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- items  (Inventar-Items, pro Charakter-Slot)
-- Quelle: NextGen.Zone/Game/Item.cs (LoadItem), NextGen.Zone/Game/ZoneCharacter.cs
-- (fuelcount-Update fuer Reittier-Futter)
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `items`;
CREATE TABLE `items` (
  `ID` BIGINT NOT NULL AUTO_INCREMENT,
  `Owner` INT NOT NULL,
  `Slot` TINYINT NOT NULL,
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  `Amount` SMALLINT UNSIGNED NOT NULL DEFAULT 1,
  `Equipt` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `fuelcount` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`),
  KEY `idx_items_owner` (`Owner`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- skillist  (erlernte Skills pro Charakter)
-- Quelle: NextGen.Zone/Game/Skill.cs, NextGen.Zone/Game/ZoneCharacter.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Skillist`;
CREATE TABLE `Skillist` (
  `ID` INT NOT NULL,
  `Owner` INT NOT NULL,
  `SkillID` SMALLINT NOT NULL,
  `Upgrades` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `IsPassive` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`, `Owner`, `SkillID`),
  KEY `idx_skillist_owner` (`Owner`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- friends  (Freundeslisten-Eintraege, Pending = Anfrage noch nicht bestaetigt)
-- Quelle: NextGen.World/Data/WorldCharacter.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `friends`;
CREATE TABLE `friends` (
  `CharID` INT NOT NULL,
  `FriendID` INT NOT NULL,
  `Pending` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`CharID`, `FriendID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- groups  (Gruppen/Party, bis zu 5 Mitglieder je Zeile)
-- Quelle: NextGen.World/Data/Group/Group.cs, NextGen.Zone/Game/Group/Group.cs,
-- NextGen.Zone/Managers/GroupManager.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `groups`;
CREATE TABLE `groups` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Member1` INT NULL DEFAULT NULL,
  `Member2` INT NULL DEFAULT NULL,
  `Member3` INT NULL DEFAULT NULL,
  `Member4` INT NULL DEFAULT NULL,
  `Member5` INT NULL DEFAULT NULL,
  `Exists` TINYINT UNSIGNED NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- Guilds / GuildMembers / GuildAcademy / GuildAcademyMembers / GuildStorage
-- Quellen: NextGen.World/Data/Guild/*.cs, NextGen.Zone/Game/Guild/*.cs,
-- Spaltenerzeugung (nicht Serverlogik) an sql/do-not-use/SQL scripts/Guild_Create.sql
-- angelehnt, da genau diese Spalten vom C#-Code per @param befuellt/gelesen werden.
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Guilds`;
CREATE TABLE `Guilds` (
  `ID` INT NOT NULL,
  `GuildName` VARCHAR(16) NOT NULL,
  `pPassword` VARCHAR(12) NULL DEFAULT NULL,
  `AllowGuildWar` TINYINT NOT NULL DEFAULT 0,
  `CreaterID` INT NOT NULL,
  `CreateTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `uq_guilds_name` (`GuildName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `GuildMembers`;
CREATE TABLE `GuildMembers` (
  `GuildID` INT NOT NULL,
  `CharID` INT NOT NULL,
  `Rank` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `Korp` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`GuildID`, `CharID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `GuildAcademy`;
CREATE TABLE `GuildAcademy` (
  `GuildID` INT NOT NULL,
  `Message` VARCHAR(255) NOT NULL DEFAULT '',
  `Points` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`GuildID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Spalten aus GuildAcademyMember_Create-Signatur in produrefull.sql uebernommen
-- (nur die Parameterliste, nicht die Prozedurlogik selbst).
DROP TABLE IF EXISTS `GuildAcademyMembers`;
CREATE TABLE `GuildAcademyMembers` (
  `GuildID` INT NOT NULL,
  `CharacterID` INT NOT NULL,
  `RegisterDate` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Rank` SMALLINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`GuildID`, `CharacterID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `GuildStorage`;
CREATE TABLE `GuildStorage` (
  `GuildID` INT NOT NULL,
  `Slot` TINYINT NOT NULL,
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  `Amount` SMALLINT UNSIGNED NOT NULL DEFAULT 1,
  PRIMARY KEY (`GuildID`, `Slot`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- Masters  (Meister/Schueler-System)
-- Quelle: NextGen.World/Data/MasterSystem/MasterMember.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `Masters`;
CREATE TABLE `Masters` (
  `CharID` INT NOT NULL,
  `MasterID` INT NOT NULL,
  `MemberName` VARCHAR(16) NOT NULL,
  `Level` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `RegisterDate` DATETIME NOT NULL,
  `isMaster` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`CharID`, `MasterID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- BlockUser  (Ignorier-/Blockliste)
-- Quelle: NextGen.World/Handlers/Handler42.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `BlockUser`;
CREATE TABLE `BlockUser` (
  `CharID` INT NOT NULL,
  `BlockCharname` VARCHAR(16) NOT NULL,
  PRIMARY KEY (`CharID`, `BlockCharname`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- PremiumItems / Rewarditems  (Cash-Shop- bzw. Belohnungs-Inventar)
-- Quellen: NextGen.Zone/Game/PremiumItem.cs, NextGen.Zone/Game/RewardItem.cs,
-- NextGen.Zone/Game/Inventory/{PremiumInventory,RewardInventory}.cs
-- ----------------------------------------------------------------------------
DROP TABLE IF EXISTS `PremiumItems`;
CREATE TABLE `PremiumItems` (
  `CharID` INT NOT NULL,
  `ShopID` INT NOT NULL DEFAULT 0,
  `UniqueID` BIGINT NOT NULL,
  `PageID` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`CharID`, `UniqueID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `Rewarditems`;
CREATE TABLE `Rewarditems` (
  `CharID` INT NOT NULL,
  `Slot` TINYINT NOT NULL,
  `ItemID` SMALLINT UNSIGNED NOT NULL,
  `PageID` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`CharID`, `ItemID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
