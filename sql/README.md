# SQL-Setup — Reihenfolge

## fiesta_account (Login-DB)
```
mysql -u root -p < sql/login/login-base.sql
```
Legt seine Datenbank selbst an (`fiesta_account`). Siehe SETUP.md.

## fiesta_world (World-/Zone-DB, gemeinsam genutzt)
```
mysql -u root -p -e "CREATE DATABASE fiesta_world DEFAULT CHARACTER SET utf8mb4;"
mysql -u root -p fiesta_world < sql/world/schema.sql
```
Aus dem C#-Code abgeleitet (keine echten Server-Daten, nur Struktur).
Enthält u. a. `characters`, `items`, `equips`, `Skillist`, `Guilds`,
`groups`, `friends`. **Ungeprüft — nicht gegen echte Daten getestet**, im
Gegensatz zu den `.shn`-basierten Tabellen unten. Siehe DOCUMENTATION.md,
Abschnitt 10.

## fiesta_data (Zone-Referenzdaten)
Reihenfolge wichtig — erst die Basistabellen, dann die mit echten Daten
befüllten:

```
mysql -u root -p -e "CREATE DATABASE fiesta_data DEFAULT CHARACTER SET utf8mb4;"
mysql -u root -p fiesta_data < sql/data/schema_derived.sql
mysql -u root -p fiesta_data < sql/data/data_iteminfo.sql
mysql -u root -p fiesta_data < sql/data/data_mobinfo.sql
mysql -u root -p fiesta_data < sql/data/mapinfo.sql
mysql -u root -p fiesta_data < sql/data/activeskill.sql
mysql -u root -p fiesta_data < sql/data/minihouse.sql
mysql -u root -p fiesta_data < sql/data/data_abstate.sql
mysql -u root -p fiesta_data < sql/data/data_subabstate.sql
mysql -u root -p fiesta_data < sql/data/data_abstateview.sql
mysql -u root -p fiesta_data < sql/data/data_passiveskill.sql
mysql -u root -p fiesta_data < sql/data/data_charactertitle.sql
mysql -u root -p fiesta_data < sql/data/data_questdialog.sql
mysql -u root -p fiesta_data < sql/data/data_questscript_fragments.sql
mysql -u root -p fiesta_data < sql/data/data_npcdialog.sql
mysql -u root -p fiesta_data < sql/data/data_kqteam.sql
mysql -u root -p fiesta_data < sql/data/data_kqisvote.sql
mysql -u root -p fiesta_data < sql/data/data_kqvotedesc.sql
mysql -u root -p fiesta_data < sql/data/data_kqvotemajorityrate.sql
mysql -u root -p fiesta_data < sql/data/data_kingdomquestdesc.sql
mysql -u root -p fiesta_data < sql/data/data_classname.sql
mysql -u root -p fiesta_data < sql/data/data_guildtournamentrequire.sql
mysql -u root -p fiesta_data < sql/data/data_guildtournamentskill.sql
mysql -u root -p fiesta_data < sql/data/data_guildtournamentskilldesc.sql
mysql -u root -p fiesta_data < sql/data/data_dicedividind.sql
mysql -u root -p fiesta_data < sql/data/data_gbdicedividind.sql
mysql -u root -p fiesta_data < sql/data/data_gbhouse.sql
```

Die letzten acht Dateien enthalten **echte Daten aus dem NA2016-Client**
(14.999 Items, 2.878 Mobs, 138 Maps, 2.791 Skills, 356 Minihäuser, 777
Buff/Debuff-Definitionen, 2.041 Buff-Stärkestufen, 776 Buff/Debuff-
Klassifizierungen mit Klartextbeschreibung) — Herkunft und
Verifikationsmethode: DOCUMENTATION.md, Abschnitte 9, 15 und 19.
`schema_derived.sql` enthält die übrigen Referenztabellen ohne
Client-Entsprechung (Struktur aus dem C#-Code abgeleitet, keine Daten).

## Bekannte Lücken
`data_mobcoordinate` wurde absichtlich nicht angelegt (nur toter,
auskommentierter Code referenziert sie). Mehrere in `Data.Mysql.*`
konfigurierte, aber im Code nicht mehr direkt genutzte Tabellen könnten
zusätzlich existieren — dieses Schema deckt nur ab, was der vorhandene
Code tatsächlich anspricht.
