-- Aus NpcDialogData.shn generiert (223 Zeilen, 3 Spalten).
-- Spaltennamen/-typen 1:1 aus der echten Client-.shn-Datei uebernommen (ggf. umbenannt, siehe unten),
-- damit sie exakt zu den row["..."]-Zugriffen im vorhandenen C#-Code passen.
DROP TABLE IF EXISTS `data_npcdialog`;
CREATE TABLE `data_npcdialog` (
  `MobIDX` VARCHAR(32) NOT NULL DEFAULT '',
  `FaceCutFile` VARCHAR(32) NOT NULL DEFAULT '',
  `Dialog` VARCHAR(1) NOT NULL DEFAULT '',
  PRIMARY KEY (`MobIDX`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `data_npcdialog` (`MobIDX`, `FaceCutFile`, `Dialog`) VALUES
  ('EldItemMctNina', 'EldItemMctNina', 'Lalala!Lalala!Lalala! I cannot stop singing--that\'s how happy I am! [NAME], what item do you need?
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Upgrade Mover][opendlg moverupgrade]'),
  ('RouSmithJames', 'RouSmithJames', 'I am James, the best Blacksmith in Roumen village. Here are the weapons and armor that I put my utmost effort into crafting. Choose whichever you want. I guarantee the quality of my weapons!
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Refine Item][opendlg itemupgrade]'),
  ('RouSoulMctJulia', 'RouSoulMctJulia', 'Hi I am Julia the Healer. I sell Health (HP) Stones and Spirit (SP) Stones. Well, [NAME], what kind of Stone do you need?
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('RouGaianMaria', 'RouGaianMaria', 'I will guide you to the other side of the sea road safely.
[BUTTON_NPC]=[server_ack 1]'),
  ('RouSkillRubi', 'RouSkillRubi', 'Do you wish to learn a new skill? If not, do not get in my way. I\'m busy training!
[BUTTON_NPC]=[Purchase][server_ack 1] '),
  ('EldSoulMctAvon', 'EldSoulMctAvon', 'Hi, [NAME], I am Avon the Healer. I sell Health (HP) Stones and Spirit (SP) Stones. What kind of Stone do you need?
[BUTTON_NPC]=[Purchase][server_ack 1] 
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('EldSmithKarls', 'EldSmithKarls', 'Hi, are you [NAME]? I\'ve heard of you. Elderine is the trade center of Isya. Weapons sold here are better than the ones you have seen before. So if you need a weapon, you should choose carefully. 
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Refine Item][opendlg itemupgrade] '),
  ('RouWeaponTitleMctZach', 'RouWeaponTitleMctZach', 'If you want to have a powerful weapon of your own, register a title for your weapon. Keep in mind that weapon titles do not have an impact on monsters that are much weaker than you.
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Register License][opendlg weapontitle]
[BUTTON_NPC]=[Enchant][opendlg enchant]'),
  ('EldStoreKyle', 'EldStoreKyle', 'Hallo~ It is really stressful to shoulder a heavy pack during the journey.
I will take care of your baggages till you call for them. You can just get me something to drink on your way back.
[BUTTON_NPC]=[Use Storage][server_ack 1]
[BUTTON_NPC]=[Receive Reward][HolyReward_Req]
[END]
[NAME], you don\'t have an apprentice yet. You can receive your reward only when you have an apprentice.
What is the \'Monarch System\'?
It is a system where a Master takes on an Apprentice. The Master can lead the Apprentice through the wonderful world of Isya.
While the Apprentice enjoys their adventures, items will be given as a gift when reaching certain levels (check your reward inventory).
A Master is rewarded here, by me, whenever an Apprentice purchases items.
If you want to be a Master, invite a novice user to become your Apprentice.
If you want to become an Apprentice, ask some great user to be your Master.
[BUTTON_NPC]=[Master Reward?][linkto 89 2]
[BUTTON_NPC]=[Apprentice Reward?][linkto 89 3]
[END]
[NAME], if you become a master, you will get a fixed amount of money whenever your apprentice purchases an item.
The more you help your apprentice, the more you will be rewarded.
[END]
[NAME], if you become an apprentice, you will receive rewards when you reach specified levels.
There are various rewards that will be distributed to your \'Reward Inventory\'.
[END]
'),
  ('EldWeaponTitleMctBran', 'EldWeaponTitleMctBran', 'Do you see this sword in my hand? This sword is so powerful that ordinary people cannot touch it. There\'s nothing you can\'t cut with this. 
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Register License][opendlg weapontitle]
[BUTTON_NPC]=[Enchant][opendlg enchant]'),
  ('RouTownChiefRoumenus', 'RouTownChiefRoumenus', 'Welcome, [NAME], I was looking for you. Roumen village has been a target of our greedy neighbors because of its great scenery and active trading. [NAME], I think that you would be a great asset to our village. Will you help us?
[BUTTON_NPC]=[Kingdom Quest][opendlg kingdomquestwin]'),
  ('RouItemMctPey', 'RouItemMctPey', 'Are you an adventurer? Ever since I was young, it was my dream to be one. Right now I am selling items in Roumen village. Someday, I want to be a great adventurer, who will travel across the Isya Continent. 
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('EldWarSkillMarty', 'EldWarSkillMarty', 'The most important aspect of being a great warrior is not one\'s physical strength, or having splendid weapons; instead, it is the courage to stand against injustice. 
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('EldPalSkillKeest', 'EldPalSkillKeest', 'We are priests who use our weapons to protect the beloved. [NAME], please keep in mind that before God, everyone is equal. You, I, and all of the creatures in this world. 
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('EldScoSkillDeikid', 'EldScoSkillDeikid', 'There is nothing more beautiful than the sound of flying arrows. At the crucial moment when the last arrow is released from the bowstring and hits the enemy, you feel blessed that you are an archer. 
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('EldWizSkillWishis', 'EldWizSkillWishis', 'The power of magic can be either the light of the world, or the cursed flame that drives people into suffering and confusion. If you wish to be a wise and great magician, make sure that you use your powers for the world--not just yourself. 
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('EldGuardCaptainShutian', 'EldGuardCaptainShutian', 'I am the Guard Captain, tasked with protecting the peace and safety of Elderine. I can see that you are a novice, but are nevertheless full of spirit and chivalry. I will entrust with you Elderine\'s Kingdomquest. 
[BUTTON_NPC]=[Kingdom Quest][opendlg kingdomquestwin]'),
  ('EldGuardNus', 'EldGuardNus', 'Sir! There is no problem here! What? You are not the Captain. You surprised me!'),
  ('EldArcGuard01', 'EldArcGuard01', 'I am a proud Guard of Elderine. There will be no evil in Elderine as long as we are here. '),
  ('EldSpeGuard01', 'EldSpeGuard01', 'I am a proud Guard of Elderine. As long as we serve, we will never allow any evil in Elderine. '),
  ('EldKidWorze', 'EldKidWorze', 'I am going to be the best magician in Isya. Then, I will eat lots of delicious food and people will respect me. '),
  ('RouStoreRaina', 'RouStoreRaina', 'You can place your belongings and valuables in my custody. No matter what happens, I shall take care of them. 
[BUTTON_NPC]=[Use Storage][server_ack 1]'),
  ('RouRookieGuideRaemi', 'RouRookieGuideRaemi', 'Hello I am a guide for beginners. If you have any questions, press the F10 button. [NAME]\'s questions will be answered. '),
  ('EldItemMctKenton', 'EldItemMctKenton', 'Oh... you don\'t know anything about fashion... terrible
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('EldGaianBjurin', 'EldGaianBjurin', 'Last night I had the most amazing dream, I saw thousands of sparkling lights. [NAME], do you ever have dreams of that sort?'),
  ('EldCastleLordElderiss', 'EldCastleLordElderiss', '...'),
  ('EldMrsMisen', 'EldMrsMisen', 'Why? Do I look scary? You shouldn\'t judge a book by its cover. '),
  ('EldGuildPredrick', 'EldGuildPredrick', 'Stand straight! Are you here to make a guild? Three things are required to make a guild. First, a sense of camaraderie - There is no \'I\' in \'team.\' Second, you must possess a chivalrous spirit, standing always against injustice. Third, you must be righteous; free from guilt or sin. These are the three conditions that you must meet before you can make a guild. 
[BUTTON_NPC]=[Guild Management][opendlg guildmenu]'),
  ('RouDiggerPalmers', 'RouDiggerPalmers', 'Why does it make me so happy to look at the sparkle of a jewel?'),
  ('EldDiggerRoyquin', 'EldDiggerRoyquin', 'If I had been born just 2 minutes earlier, 
I could have been elder brother of Palmers. I feel so shameful!!'),
  ('RouGrandfatherRobin', 'RouGrandfatherRobin', 'Oh, my back. Do you know what this is on my head? Shh..this is a legendary Phoenix feather which is very rare. One with an evil heart sees it as just a normal feather. What does it look like to you?'),
  ('GrandMasterShone', 'GrandMasterShone', 'I assume you already know that you can take the promotion test from me when you reach level 20. 
Keep in mind it\'s not an easy test.
Let me tell you once more to visit me when you become level 20.
Ah! I forgot to introduce myself.
I\'m grand master Sean, secluding myself in this forest studying various skills and magic.
And I\'m also in charge of the promotion test in level 60. Don\'t forget to see me then.'),
  ('UruSmithHans', 'UruSmithHans', 'The more you hit, the stronger the metal becomes. Just like myself! You are stronger after strenuous effort. Now people call me Hans the iron man. Hahaha!
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Refine Item][opendlg itemupgrade]'),
  ('UruItemMctVellon', 'UruItemMctVellon', 'You look worried! I will play music that will make you happy. Forget about the unpleasant things that worry you!
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('UruTownChiefAdrien', 'UruTownChiefAdrien', '[NAME], we were meant to meet each other. It was foretold. Can you believe it? After a while, when you have had many adventures, you will understand what I mean. 
[BUTTON_NPC]=[Kingdom Quest][opendlg kingdomquestwin]'),
  ('UruSoulPooring', 'UruSoulPooring', 'Don\'t look down on me just because I am small. 
Most monsters will be defeated with my punch!
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('UruGuildLump', 'UruGuildLump', 'Hahaha! *hiccup*! Lets drink. Today is the happiest day of my life, let\'s get drunk! *hiccup* Do I look drunk already? Haha, you don\'t know me. I am Lump, and I can break a guild into tiny pieces! *hiccup*
[BUTTON_NPC]=[Guild Management][opendlg guildmenu]'),
  ('UruFurnitureForestTeem', 'UruFurnitureForestTeem', 'Do I look weird to you? 
You probably are not familiar with my tribe. I am from the tree tribe. 
Please don\'t be scared of me. 
Our tree tribe is a friend of humans and elfs..
[BUTTON_NPC]=[Buy][server_ack 1]'),
  ('UruStoreCurly', 'UruStoreCurly', 'Nice to meet you! This is a very rugged mountain village where monster invasions are frequent. However, if you live in Uruga for a while, you will realize this is a cordial and warm place. You may not see it that way now, but if you spend a few days here and become used to the scenery and atmosphere, it will not be easy to leave. 
[BUTTON_NPC]=[Use Storage][server_ack 1]'),
  ('UruSkillChyburn', 'UruSkillChyburn', 'In a moment of crisis, is a weapon the only thing that can save your life? Absolutely not. It is skill that will protect you. Hahaha! I talk too much. Hahaha!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('PostCos01', 'PostCos01', 'This is a way to Rookie Hunting field.'),
  ('PostCos02', 'PostCos02', 'This is a way to Rookie Hunting field.'),
  ('PostCos03', 'PostCos03', 'Rookies should hunt at Sand Beach or the Forest of Tides.
[No Rookies] 
Level 1~13 restricted.'),
  ('PostRoumenus', 'PostRoumenus', 'The arrows will lead you to the hunting fields for rookies. 
- Town chief Roumenus'),
  ('PostRemi', 'PostRemi', 'Hi, this is a note from Remi, the Beginner Guide. 
- Press "M" to bring up a map of the area. 
- Press "F10" is for the help file. 
- There are two ways to use HP and SP recovery stones: 
   (1)QuickSlot keys (Keyboard buttons "1"=HP, "2"=SP) 
   (2)Shortcut keys (Keyboard buttons "E"=HP, "Q"=SP) 
- Press "Home" to toggle "Resting" mode on and off.
- Chatting codes: 
   (1) Whisper ( /w ID "content") 
   (2) Outloud ( /s "content")  
   (3) Party chat ( /p "content") 
   (4) Guild chat ( /g "content")'),
  ('EldArcGuard03', 'EldArcGuard03', 'Hello [NAME], I am a guard, and have been sent to provide you with equipment. I have many different kinds of weapons, armor, mining equipment and movers. Please check if there is anything that you would like. 
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('TempSoul', 'TempSoul', 'Hello [NAME], I am a guard, and have been sent to sell Health Power (HP) Stones and Spirit Power (SP) Stones. I hope that I can be of help to you. Tell me which Power Stones you need!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('TempSkill', 'TempSkill', 'Hello [NAME], I am a guard, and have been sent to sell you Skill Scrolls. I will try my best to give you a hand even though it\'s for a short while. Tell me which kind of Skill Scroll you need!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('FbattleSoul', 'FbattleSoul', 'You should be alert at all times here in Free Battle Zone.
You don\'t know who might be targetting on you rigtht at this moment.
Please check if you have plenty of HP and SP Soul Stone for your own safety.
If you need Soul Stones, you can purchase them from me any time.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('GodEnide', 'GodEnide', 'Heneath Adventure Clan, huh?
I see you guys are getting bigger and bigger these days.
Maybe it\'s a certain thing since the Breath of \'Her\' and the will of \'Her\' are all spreaded around the continent of Isya.'),
  ('Vietree', 'Vietree', '(Lalala~lala~lalala~) 
Are you tired of looking like everyone else?. 
Want others to notice that new style, [NAME].
If you are bored of your look, then you need a make-over! 
Enter the Beauty Shop and get a new refreshing look!
[BUTTON_NPC]=[Enter Beauty Shop][logout beautyshop]'),
  ('HednisFigGuard01', 'HednisFigGuard01', 'Marlone Clan is getting stronger as time goes by, but don\'t worry. 
Hennis Alliance is strong enough.'),
  ('HednisArcGuard01', 'HednisArcGuard01', 'Marlone Clan is getting stronger as time goes by, but don\'t worry. 
Hennis Alliance is strong enough.'),
  ('HednisClericMan', 'HednisClericMan', 'Marlone Clan and monsters are threatening Elderin.
We judged that we cannot defend against them within the town anymore. Hennis Alliance Mission was dispatched to Sandy Wind Hill.
We will fight against them to the end. '),
  ('HednisMageWoman', 'HednisMageWoman', 'Marlone Clan and monsters are threatening Elderin.
We judged that we cannot defend against them within the town anymore. Hennis Alliance Mission was dispatched to Sandy Wind Hill.
We will fight against them to the end. '),
  ('HednisSkillGrunt', 'HednisSkillGrunt', 'Attention!
Do you want to learn a new skill? We have to learn strong skills in order to kill those worthless creatures outside of the camp.
Check if there are skills that you don\'t learn yet carefully.
[BUTTON_NPC]=[Purchase][server_ack 1] '),
  ('HednisSmithRohan', 'HednisSmithRohan', 'Welcome, [NAME].
(cough, cough)
I\'ve heard about you, people say you are a really good person.
As you know, we have severe sandy wind here all the time. Thanks to the horrible weather, I have trouble in my throat.
(cough cough)
[BUTTON_NPC]=[Buy][server_ack 1]
[BUTTON_NPC]=[Refine Item][opendlg itemupgrade]'),
  ('HednisSoulKeroll', 'HednisSoulKeroll', 'Hello, [NAME].
Be careful not to be too conceited. You have to have a sharp eye on you. 
Please put priority on the safety of you and your fellows rather than victory in the battle. 
Teba bless you.
[BUTTON_NPC]=[Purchase Stone][server_ack 1]'),
  ('HednisStoreDrein', 'HednisStoreDrein', 'What? What are you doing here?
You want to leave your baggage here?
Hurry up! To leave or not?
[BUTTON_NPC]=[Use storage][server_ack 1]'),
  ('VegetarianGoblin', 'VegetarianGoblin', 'I\'m always hungry
Do you have any leftover mushrooms or fruits?
You see, I don\'t eat meats.  I think it\'s barbaric.'),
  ('SpyGoblin', 'SpyGoblin', 'Quiet!
If we get caught, all we\'ve been doing becomes useless.
I don\'t have any business with you so go away!'),
  ('PatrolGuardianPolmon', 'PatrolGuardianPolmon', 'I am so bored...
That\'s why I hate going on a round.
I wish my shift would end soon.
Wha!
Nothing unsual here sir!'),
  ('DustAdventurer01', 'DustAdventurer01', 'Have you seen my friends?
I was hurt, and my friend went for help, but
Hasn\'t came back yet It\'s been hours.'),
  ('DustAdventurer02', 'DustAdventurer02', 'Ugh... I can\'t get up... I\'m waiting for my friend.'),
  ('PillarofLight', 'PillarofLight', '..............................
..........................................'),
  ('MctMasterMaxUtter', 'MctMasterMaxUtter', 'I am not some fool who didn\'t know that Burning Rock isn\'t safe.
But...
There are adventurers out there that need our help.  Profit comes in second.'),
  ('ItemMctJelluin', 'ItemMctJelluin', 'As you know, Burning Rock is a recent discovery.  Also not many people know that this used to be a booming mining town back in the days.  
There are many hidden secrets that lie beneath the surface.  

[BUTTON_NPC]=[Sell item][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('StoneMctTomson', 'StoneMctTomson', 'This just doesn\'t feel right.  Send me home!!!
There\'s nothing but monsters and hot lava around here.  Why am I selling stuff here?  What was I thinking?  Hey go back to town safely while you can, okay?

[BUTTON_NPC]=[Purchase stone][server_ack 1]'),
  ('DustAdventurer03', 'DustAdventurer03', 'Argh!!! (Ripping hair out.)
Where, and what is this?  Argh...  Look, please help, okay?'),
  ('PriestMediang', 'PriestMediang', 'Isn\'t this wonderful?
I haven\'t seen such an old temple.  I\'m sure you, [NAME], haven\'t seen such a sight either.
Who do you think this temple was for?  I am so curious.  Don\'t you want to find out?'),
  ('DustAdventurer04', 'DustAdventurer04', 'Wha!!!
Oink, oink... Oink... 
You are just a human... I thought you were a pig.'),
  ('SecretaryCleo', 'SecretaryCleo', 'This place isn\'t suitable for a merchant to be.  It\'s wet and plagued with monsters.  Is there anything you want to say to me?'),
  ('EventPost01', '-', 'Merry Christmas!  Have a wonderful Christmas in Fiesta!'),
  ('WeddingDreian', 'WeddingDreian', 'My name is Dreian Uriel. I oversee all the wedding activities.
You may purchase an Engagement Ring from me for a proposal and a Wedding Application from the Fiesta Store for a wedding. You can also apply for divorce from me.
To apply for a marriage, both you and your partner must have a Wedding Application, but only one of you needs to apply for the actual marriage.
Don\'t forget that ceremony is not free. It will cost you (200 Silvers), and the divorce proceedings will cost you even more.
When you and your partner both agree to get divorced, it costs 250 Silvers. But if you divorce your partner forcefully, it costs the applicant 750 Silvers.
Never take marriage lightly and always take extra caution when making a marital decision.
[BUTTON_NPC]=[Join Wedding][getin weddinghall]
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('UrgSwaItemMct', 'UrgSwaItemMct', 'Have you met King Helga of Hellgate?  
I heard there was a great king of Hellgate.  Why do think the king turned into a monster?  
[BUTTON_NPC]=[Sell item][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('AlruinChiefKiera', 'AlruinChiefKiera', 'Endless amounts of spirits screaming...  I can hear them shouting.
Currently the looks of Alberstol must have been caused by the Spirits themselves.
...I only hope it\'s not too late.
[BUTTON_NPC]=[Join Kingdom Quest][opendlg kingdomquestwin]'),
  ('AlruinSmithMacurdos', 'AlruinSmithMacurdos', 'Hahaha! Is the weapon you\'re holding now ok? 
I mean isn\'t it time you changed it? Keep in mind that what protects your life is the very thing
you\'re holding in your hands. 
[BUTTON_NPC]=[Sell or buy weapons][server_ack 1]
[BUTTON_NPC]=[Refine Items][opendlg itemupgrade]'),
  ('AlruinStoreRel', 'AlruinStoreRel', 'This, is an item a handsome young man asked me to hold for him, and this is an item a wide-eyed angry looking elf sister asked me to hold... No no, was it the guy with hair all over his body? This is...This... Is... Eeeeeeek!!! I don\'t know! I quit! Why is this so complicated? 
[BUTTON_NPC]=[Use Storage][server_ack 1]'),
  ('AlruinSoulRunadilla', 'AlruinSoulRunadilla', 'Excuse me, adventurer!
Are you leaving on a journey right now? Umm, do you have enough recovery stones?
I think it\'s best for you to prepare as much as you can since this place is full of unknown dangers.

[BUTTON_NPC]=[Buy stones][server_ack 1]
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('AlruinItemMctGeric', 'AlruinItemMctGeric', 'We will no longer live missing the old days of Alberstol.
We will need stronger weapons and sound armors to confront the dangerous beings outside. 
[BUTTON_NPC]=[Buy or sell items][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('AlruinSkillPaela', 'AlruinSkillPaela', 'What\'s that!?
I heard you!
Why you little!! Get over here right now!! 
[BUTTON_NPC]=[Buy Skill Scrolls][server_ack 1]'),
  ('AlruinTeiler', 'AlruinTeiler', '... ... '),
  ('AlruinRick', 'AlruinRick', 'Well, I will be the richest man in Isya one day. I will make a fortune running my business by trading exotic items. That\'s why I cam here to Alberstol.'),
  ('EldFurnitureForestTall', 'EldFurnitureForestTall', 'Many people try to buy furniture these days, my brothers are too busy.
I am not sure we can even finish today\'s order...
Oh, Don\'t worry.
We\'ve never and ever break the promise with our costomer.
[BUTTON_NPC]=[Buy][server_ack 1]'),
  ('RouFurnitureForestTom', 'RouFurnitureForestTom', 'Greeting.[NAME]
I am \'Tom\' who is youngest of Forest family.
You will see my brothers in other towns.
But please don\'t be confused. 
I am \'Tom\'.
[BUTTON_NPC]=[Buy][server_ack 1]'),
  ('Alruin_EarthStone', 'Alruin_EarthStone', '...'),
  ('Alruin_FireStone', 'Alruin_FireStone', '...'),
  ('Alruin_TreeStone', 'Alruin_TreeStone', '...'),
  ('Alruin_WaterStone', 'Alruin_WaterStone', '...'),
  ('Alruin_WindStone', 'Alruin_WindStone', '...'),
  ('Gate_Town', 'Gate_Town', 'I service teleporting you between towns.Please choose the town you wish to go to.
Oh, I don\'t need money. This is free of charge.  (You can start using from Level 10)'),
  ('SecretaryClio', 'SecretaryClio', 'Hello, I\'m Clio from Maxouter Merchant Group.  My name is familiar to you?  That\'s probably because I\'m related to Cleo.'),
  ('GuildItemMct', 'GuildItemMct', 'Do you have the Guild Item Exchange Token? If so, go ahead and pick the items you want. Keep in mind that I don\'t take Silver or Gold here!
[BUTTON_NPC]=[Purchase Item][server_ack 1]'),
  ('EldStoreFranz', 'EldStoreFranz', 'Hello, my name is Franz. I am the new Storage Keeper for this area.
It must have been a burden to carry all that load.
I will keep your valuables here for you.
[BUTTON_NPC]=[Use Storage][server_ack 1]'),
  ('Q_Kassandra', 'Q_Kassandra', 'hey you! Do you want to enroll the hunter alliance? Did you? 
Then, you don\'t have anything to do it. You can come over next time.'),
  ('Q_Keroll', 'Q_Keroll', 'Hi~ adventurer. 
Do you have something to talk with me? 
You don\'t have? That\'s releave. '),
  ('Q_Lino', 'Q_Lino', 'What are you looking at?'),
  ('Q_Polan', 'Q_Polan', 'I should find it. I am not sure how much it will take though.
They have the giant gold wing  '),
  ('Raphael', 'Raphael', 'Adventurer.... I need your help.
Please ask Town Chief Roumenus for help.
Without the key, No one can enter this place.
He may have the spare key.
....In a hurry to let him know.'),
  ('Q_Hugues', 'Q_Hugues', 'Many things would happen from now. 
But don\'t worry.  There will be many people who think and act like you. '),
  ('Q_Como', 'Q_Como', 'I can\'t even sleep well here.  Too many things are bothering me!
What should I do if it hurts my skin?'),
  ('Q_Tiara', 'Q_Tiara', 'I like a strong man like Mr. Hugues~
You\'d better get stronger to make friends with me~'),
  ('Joker', 'Joker', 'Hi, My name is Mario, he is Reo. We are on same body. Listen! Trickster can represent with 2words, Speed and Fancy
[BUTTON_NPC]=[Store][server_ack 1]'),
  ('Edge', 'Edge', 'Uh...I am Edge. Doll.Reo, Mario, Doll'),
  ('Ring', 'Ring', 'I, Ling'),
  ('BeraChiefValiere', 'BeraChiefValiere', 'Welcome to elf\'s city "Bera". 
I am cheif "Valiere". Would you like to talk with Bera\'s people?
[BUTTON_NPC]=[Kingdom Quest][opendlg kingdomquestwin]'),
  ('BeraMargentia', 'BeraMargentia', ' There is full of secent of Hyacinth today.'),
  ('BeraEtty', 'BeraEtty', 'My name is Etty, Margentia\'s pupil. Do you have any favor for me?'),
  ('BeraDuskin', 'BeraDuskin', 'Hu..Hu.. Great to see you, Are you an advanture from the east land?'),
  ('BeraAmelie', 'BeraAmelie', 'Hey, Do you have any idea that what you step on it? 
Step back. You are standing on the little flower!! Anyway, what\'s going on?'),
  ('BeraItemMilly', 'BeraItemMilly', 'Hey dude. Why don\'t you drink this one? 
You can run very fast, even your face may become orange color. Run faster is more important than face, Isn\' it?
[BUTTON_NPC]=[Item Trading][server_ack 1]'),
  ('BeraItemEdmong', 'BeraItemEdmong', 'Fine cuisine is like \'Art\'! Fresh ingrediants and colorful fruits How fantastic!! Please have them I cooked.
[BUTTON_NPC]=[Item Trading][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('BeraSmithMcDilan', 'BeraSmithMcDilan', 'I am different not like anyone. You can see!  
You are an advanturer. What I make armor and weapon is the best!! Look around. You might like that. 
[BUTTON_NPC]=[Weapon Trading][server_ack 1]
[BUTTON_NPC]=[Item Refine][opendlg itemupgrade]'),
  ('BeraSkillHal', 'BeraSkillHal', 'Nice to see you. I will master the skill. Please feel free to stay the town. 
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('BeraStoreShane', 'BeraStoreShane', 'Look at you, How cute you are!! 
You must be new in here. Is there any sweet and fruit taste chocolate or scent flavor candy outside? 
If you can, please bring me some ot them to try. I can\'t wait!!! 
[BUTTON_NPC]=[Using Inventory][server_ack 1]'),
  ('BeraSoulOlivia', 'BeraSoulOlivia', 'Hi, advanturer. I am a Bera\'s Healer \'Olivia\'.
Have you met chief and skill Master?  They are so nice and gentle. I hpoe you take a break as much as you can. 
[BUTTON_NPC]=[Purchase stone][server_ack 1]
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('BeraVillager', 'BeraVillager', 'You are an adventurer. Can I tell you one secret you might like?'),
  ('BeraGuardArcher', 'BeraGuardArcher', 'Hello,  I am incharge of guard Archer in this beautiful elf\'s city. Let me show how I am good at archery.
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Register License][opendlg weapontitle]'),
  ('SecretaryClio', 'SecretaryClio', 'Hello, I am a Cleo in Maxuter. Are you accustomed to my name? Of course you can. As I am a Clair\'s brother Cleo.'),
  ('GuildItemMct', 'GuildItemMct', 'Only user pay Guild token, Npc sells the items.(test)
[BUTTON_NPC]=[Purchase item][server_ack 1]'),
  ('EldSmithKarls2', '-', 'How are you doiong? I am incharge of loaming, called "Karls".
You are [NAME]? I heard about you a lot.
Everybody says Elderin is the best merchant town in Isya land.
These weapons are totally different compared to anything you have ever seen. Whatever you need, just pick one. 
[BUTTON_NPC]=[Weapon trading][server_ack 1]
[BUTTON_NPC]=[Item Refine][opendlg itemupgrade]'),
  ('GB_CoinMachine', 'GB_CoinMachine', 'This is the coin exchange machine.'),
  ('GB_Touter', 'GB_Touter', 'Would you like to enter Fortune Game House?
[BUTTON_NPC]=[Enter][server_ack 1]'),
  ('GB_MasterRoan', 'GB_MasterRoan', 'The heavy gaming begins on the 3rd game!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('GB_Waitress', 'GB_Waitress', 'Hey sweetie, want to participate in a deal? Hehe'),
  ('GB_Dealer', 'GB_Dealer', 'To participate in a deal, you need to bet your coins.'),
  ('GB_Store', 'GB_Store', 'I will securely take care of your valuable items.
[BUTTON_NPC]=[Stash][server_ack 1]'),
  ('RouDiggerDolTurn', 'RouDiggerDolTurn', 'Why does it make me so happy to look at the sparkle of a jewel?'),
  ('GB_CoinStore', 'GB_CoinStore', '���¾�~�����Ͽ콺 ���λ���!! �پ��� �������� �Ȱ� �־�.
��!! �̺�! �� ���θ� �޴´ٱ�!!
���ӸӴ�, ĳ��, ������ �ִ� ������ ������ �̷��͵��� ���� �����̾�.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('LC_Machine', 'LC_Machine', '���� �������� �����Է�!!
"�����Ե� ���� �������� �������� ���ھ�!" 
�̷� �е��� ���� �� 2�ֿ� 1�� 3�� ���ȸ� 
��Ű ĸ���� �پ��� ������ �� �ϳ��� ���վȿ�!!
�ھ�! ���θ�����!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('LC_MachineRed', 'LC_MachineRed', '"Try your luck and win the top prize! Buy a Blue Lucky Capsule and get something rare. You\'re always guaranteed a prize! Don\'t hesitate! Try now!"
 [BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('LC_MachineBlue', 'LC_MachineBlue', '"Try your luck and win the top prize! Buy a Blue Lucky Capsule and get something rare. You\'re always guaranteed a prize! Don\'t hesitate! Try now!" 
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('AdlSoulAngela', 'AdlSoulAngela', 'My potion contains my caring love which will awaken your hidden strength with just one drop. Don\'t hesitate; just see for yourself..
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('AdlSkillEdwina', 'AdlSkillEdwina', 'Hey, can you get me some magic books, such as "The Powers of the Land", "The Book of the Heavens", and "The Forbidden Book of Magic" when you go out hunting? I can\'t get these in the village.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('AdlSmithAlexia', 'AdlSmithAlexia', 'Pound!! The sound of banging heated steel really motivates me!! OK! What should I make today?
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Refine Item][opendlg itemupgrade]'),
  ('AdlStoreKaruha', 'AdlStoreKaruha', 'Your inventory should be getting rather heavy by now. Your body will get sore if you carry too much at once.
 Don\'t worry, you can keep items here with me. 
[BUTTON_NPC]=[Use Storage][server_ack 1]'),
  ('QM_Bunis', 'QM_Bunis', 'You are a stranger just like me!  Good to meet you!  I am Bunis!!
You look pretty tense.  I think this village makes you nervous. 
It\'s OK.  Relax.
[BUTTON_NPC]=[Guild][opendlg guildmenu]'),
  ('AdlSpeGuiltian', 'AdlSpeGuiltian', 'I am Guiltian, the acting chief of this village.
Nice to meet you. 
[BUTTON_NPC]=[Kingdom Quest][opendlg kingdomquestwin]'),
  ('AdlGuardNell', 'AdlGuardNell', 'If you have no business with me, please leave me alone.'),
  ('AdlMarlene', 'AdlMarlene', '...Oh... I\'m a little busy now. Let\'s talk later.'),
  ('AdlLoussier', 'AdlLoussier', 'Hmm... Um... Please don\'t talk to me. I don\'t... like to... talk to strangers.
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Item Combine][opendlg itemmix]'),
  ('AdlAertsina', 'AdlAertsina', 'Have you met our town chief, Guiltian? He\'s so dedicated to his post; sometimes to the point where he forgets to eat. It worries me when he does that, though... 
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Register License][opendlg weapontitle]'),
  ('Q_Rourke', 'Q_Rourke', 'I am the leader of the Hunter\'s Union. I may not be the best leader, but I have my members who support and trust me.'),
  ('Q_Joanna', 'Q_Joanna', 'The vast land and plains open up my heart. '),
  ('Q_Huey', 'Q_Huey', 'We are not the only Hunter\'s Union. Other regions have them as well.'),
  ('Q_Dalian', 'Q_Dalian', 'The only person to acquire the knowledge of wise man.. '),
  ('�Ƶ����� ���� �̵� NPC', '', ''),
  ('AdlLantesUp', 'AdlLantesUp', 'With Claude, I get to fly with the winged ones in the sky. 
Are you jealous? Wouldn\'t you like to fly too?
[BUTTON_NPC]=[Disembark]'),
  ('Claude', 'Claude', '...(Argggg).'),
  ('AdlLantesDown', 'AdlLantesDown', '...This... place... is... worth... staying in. 
[BUTTON_NPC]=[Disembark]'),
  ('AdlF_Loussier', 'AdlLoussier', 'Please hurry. 
We must stop Eglack before it invades the village.  

[BUTTON_NPC]=[Escort][NpcAct 1]
[BUTTON_NPC]=[Standby][NpcAct 2]
[BUTTON_NPC]=[Activate Stone][NpcAct 3]'),
  ('Q_W_Chapman', 'Q_W_Chapman', 'Guarding all by myself is so lonely. Any pretty girl guard won\'t come around~ '),
  ('Q_W_Jacks', 'Q_W_Jacks', 'Hello wanderer Jax. I am not planning to stay here longer either~'),
  ('MineDigger', 'MineDigger', 'Each landmine has different purpose. 
A landmine is an explosive and cannot leave the mine. 
Please purchase the number of landmines you need. 
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('Daliy_Merchant', 'Daliy_Merchant', 'I am Ms.Lee came from far east. I will exchange your fame credits with various items.

[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('E_SantaClaus', 'E_SantaClaus', 'Hello, My name is Young Santa, Happy Christmas~ EveryOne~'),
  ('E_DadNPC', 'E_DadNPC', 'Where is my beloved Queen Slime?'),
  ('E_MomNPC', 'E_MomNPC', 'I can\'t take this anymore!! It was just a little picnic with other slime.
Only thing he has money! He never gives me any present nor listen when I ask him to go for picnic. I\'m not sure if he loves me or not...
I cannot live with such a slime!!!'),
  ('E_DanielNPC', 'E_DanielNPC', 'Because of your help, our family is in peace again. Thank you. 
Please, let me bless you with this buff for each day. '),
  ('Egg_Digger', 'Egg_Digger', 'There is an item that can make my grandchildren happy.
Please protect it for their sake. You can use the landmines I brought from the mine to defeat them.
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('Tiros', 'Tiros', 'The essence of the Crusader is like a ray of light that brightens the darkness in Isya.
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('KQSpring_Rman', 'KQSpring_Rman', 'Do you want to win this battlefield?
Then it\'d be best to buy this item now. Haha!
Oh, you do know that you\'re meant to hit the target, right?
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('KQSpring_Bman', 'KQSpring_Bman', 'Do you want to win this battlefield?
Then it\'d be best to buy this item now. Haha!
Oh, you do know that you\'re meant to hit the target, right?
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('Xiaoming', 'Xiaoming', 'Which puzzle would you like to select?
I will collect one silver from you after your choice!
[BUTTON_NPC]=[Slime Puzzle][NpcAct 1]
[BUTTON_NPC]=[Honeying Puzzle][NpcAct 2]
[BUTTON_NPC]=[Select all puzzles][NpcAct 3]'),
  ('Oluming', 'Oluming', 'Which puzzle would you like to select?
I will collect one silver from you after your choice!
[BUTTON_NPC]=[Slime Puzzle][NpcAct 1]
[BUTTON_NPC]=[Honeying Puzzle][NpcAct 2]
[BUTTON_NPC]=[Select all puzzles][NpcAct 3]'),
  ('Toryming', 'Toryming', 'Which puzzle would you like to select?
I will collect one silver from you after your choice!
[BUTTON_NPC]=[Slime Puzzle][NpcAct 1]
[BUTTON_NPC]=[Honeying Puzzle][NpcAct 2]
[BUTTON_NPC]=[Select all puzzles][NpcAct 3]'),
  ('DigGrifin', 'DigGrifin', 'We have to excavate......the expensive treasures!'),
  ('DigWebster', 'DigWebster', 'I\'m sorry but I\'m little busy here so could you not talk to me please?'),
  ('DigGregory', 'DigGregory', 'How can I survive in here....sigh....'),
  ('DigChavez', 'DigChavez', 'We are excavation team to search for treasures in Dark Land!
Please cheer for us!'),
  ('DigKupers', 'DigKupers', 'Ouch..there are only monsters..'),
  ('DigRoss', 'DigRoss', 'I don\'t know how long I can stand to this...'),
  ('RouT_Smith', 'RouT_Smith', 'Are you just going to stand there staring?
If you\'re not going to buy anything, go away so you don\'t scare my customers.
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('RouT_Soul', 'RouT_Soul', 'The goods I\'m selling will help you on your journey.
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('RouT_Skill', 'RouT_Skill', 'Do you want to become stronger? Then simply learn some skills from me.
[BUTTON_NPC]=[Shop][server_ack 1]'),
  ('E_HwinQuest', 'E_HwinQuest', 'Happy Halloween! Have some of my candy!
Are you here for a present? Shall we see what you\'ve brought me?
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('IM_Arena01', 'RouGaianMaria', 'Did you train hard?
With enough Arena coins, you can acquire equipment for level 70, 80, or 90.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('IM_Arena02', 'RouGaianMaria', 'Did you train hard?
With enough Arena coins, you can acquire equipment for level 100, 110, or 115.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('IM_Arena_TE', 'EldArcGuard01', 'If you want to extend duration for Arena item, you can purchase from me.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('E_XXiaoming', 'E_XXiaoming', 'Merry Christmas!
If you bring me commemorative coins, I\'ll exchange them for some items I\'ve prepared.
But before you do that, could you help me with something?
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('E_Ski_CongressNPC', 'E_XXiaoming', 'Thank you for visiting the 2014 Ski Tournament. 
If you participate in the tournament and bring me commemorative coins , I can offer you great items in exchange.
[BUTTON_NPC]=[Start Race][NpcAct 1]
[BUTTON_NPC]=[Top Ranks][NpcAct 2]'),
  ('E_Ski_MerchantNPC', 'E_XXiaoming', 'Would you like to register for the 2014 Ski Tournament?
Try to finish the season with an outstanding record!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('E_Ski_QuestNPC', 'E_XXiaoming', 'I\'m so glad you made it through safely.
Use the gate next to me to return to Elderine.
[BUTTON_NPC]=[Top Ranks][NpcAct 1]
[BUTTON_NPC]=[Retry][NpcAct 2]'),
  ('E_Ski_RentMachine', '-', 'We rent snowboards that can be used in Ski Tournament.
These snowboards will disappear when you come back to starting line through Snowming Jr. at the end of the track or if you leave this area.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('Egg2014_HoshemingNPC', 'Xiaoming_Ghost', 'Traveler! Ah.. Don\'t freak out.. I\'m not a bad soul.
Please help me return to help the townspeople by collecting the huge eggs. The more Golden Eggs you create from Huge ones, the faster the ressurection proccess will be. I will repay you for sure for your troubles.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('', '', ''),
  ('SoulMctChloe', 'SoulMctChloe', ' Hi. My name is healer Chloe and I sell HP stone and SP stone. So, [NAME], what do you need?
[BUTTON_NPC]=[Purchase][server_ack 1]
[BUTTON_NPC]=[Dismantle][opendlg make_karis]'),
  ('Pie', 'Pie', 'If I stay here, monsters won\'t find me, right?'),
  ('DigGrifin', 'DigGrifin', 'We have to excavate......the expensive treasures!'),
  ('DigWebster', 'DigWebster', 'I\'m sorry but I\'m little busy here so could you not talk to me please?'),
  ('DigGregory', 'DigGregory', 'How can I survive in here....sigh....'),
  ('DigChavez', 'DigChavez', 'We are excavation team to search for treasures in Dark Land!
Please cheer for us!'),
  ('DigKupers', 'DigKupers', 'Ouch..there are only monsters..'),
  ('DigRoss', 'DigRoss', 'I don\'t know how long I can stand to this...'),
  ('KDSoccer_MctNPC', 'Xiaoming_Soccer', 'Was that game of soccer fun? Hehe, if you have soccer tournament coins you can exchange them with the rewards I have prepared.
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('Swimming', 'Swimming', 'Did you have fun with the Water Balloon battle? Hehe!
If you have Summer Coins, you can exchange that with what I prepared! You get coins from the Water Balloon Battle Kingdom Quest, hehe��!
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('SwimmingR', 'Swimming', 'Hehe, good luck! You can buy Water Balloons and Water Cannons from me!~
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('SwimmingB', 'Swimming', 'Hehe, good luck! You can buy Water Balloons and Water Cannons from me!~
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('ClassChangeMaster01', 'EldCastleLordElderiss', 'Um? Ah�� Did you come here to change your class?
Did you bring a respecialization Scroll? Let\'s start once you get ready.
[BUTTON_NPC]=[ClassChange][NpcAct 1]'),
  ('ClassChangeMaster02', 'EldCastleLordElderiss', 'Um? Ah�� Did you come here to change your Class specialization?
Did you bring a respecialization scroll? Let\'s start once you get ready.
[BUTTON_NPC]=[ClassChange][NpcAct 1]');
INSERT INTO `data_npcdialog` (`MobIDX`, `FaceCutFile`, `Dialog`) VALUES
  ('Nagro', 'Nagro', 'Isn\'t that beautiful? Hard to believe that it\'s cursed.
It could have become a paradise for hunters.'),
  ('Hilda', 'Hilda', 'I can\'t believe that a beautiful woman like me is in a desolate place like this running from monsters.
*Achoo!* If it wasn\'t for that mean girl Helen, I\'d return to Bera.'),
  ('Akisha', 'Akisha', 'Dancing was my whole life.
If it hadn\'t been for Shuray��'),
  ('SirenStatue', 'SirenStatue', 'Dangerous... Siren. Beware...
City of... Bannel... Shuray...
Akisha...
(You can only make out some words)'),
  ('Cuero', 'Cuero', 'Argh... Daughter.
*sniff, sniff, sniff*'),
  ('Gerta', 'Gerta', 'Wow! You\'re tall.
Let\'s be friends!'),
  ('Chaoming', 'Chaoming', 'You defeated Anais. Hehe.. 
 You can exchange your Ocean Crystal for other good items.
 That shiny jewel! Hehe..
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('Basilone', 'Basilone', 'No matter what someone says, here is the safest place around Kahal Canyon. 
Watch Garuda carefully if you want to survive.'),
  ('Nicole', '-', 'I really hate cold places. But What i hate more, is someone telling silly jokes. '),
  ('Q_Jey', '-', 'I have to go as soon as possible to save my colleague, I left alone at the camp'),
  ('BeraGuildLucas', 'UruGuildLump', 'Hahaha! *hiccup*! Lets drink. Today is the happiest day of my life, let\'s get drunk! *hiccup* Do I look drunk already? Haha, you don\'t know me. I am Lucas, and I can break a guild into tiny pieces! *hiccup*
[BUTTON_NPC]=[Guild Management][opendlg guildmenu]'),
  ('ClassChangeMaster03', 'EldCastleLordElderiss', 'Um? Ah�� Did you come here to change your Class specialization?
Did you bring a respecialization scroll? Let\'s start once you get ready.
[BUTTON_NPC]=[ClassChange][NpcAct 1]'),
  ('AdlFH_Loussier', 'AdlLoussier', 'Hurry up.
Before this would going down to darkness of madness..

[BUTTON_NPC]=[Escort][NpcAct 1]
[BUTTON_NPC]=[Standby][NpcAct 2]
[BUTTON_NPC]=[Activate Stone][NpcAct 3]'),
  ('KDSoccer_MctNPC_14', 'Xiaoming_Soccer', 'Did you enjoy the Winter Cup Soccer tournament?
If you collect enough memorial coins from playing,
you can exchange them for some "cool" prizes
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('SD_Futureming', 'SD_Futureming', 'Do you have some of ominous coins? That looks like something I might be intersted in researching..
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('Meily', 'Vietree', '(Lalala~lala~lalala~) 
Are you tired of looking like everyone else?. 
Want others to notice that new style, [NAME].
If you are bored of your look, then you need a make-over! 
Enter the Beauty Shop and get a new refreshing look!
[BUTTON_NPC]=[Enter Beauty Shop][logout beautyshop]'),
  ('Bellen', 'Vietree', '(Lalala~lala~lalala~) 
Are you tired of looking like everyone else?. 
Want others to notice that new style, [NAME].
If you are bored of your look, then you need a make-over! 
Enter the Beauty Shop and get a new refreshing look!
[BUTTON_NPC]=[Enter Beauty Shop][logout beautyshop]'),
  ('Hermosia', 'Vietree', '(Lalala~lala~lalala~) 
Are you tired of looking like everyone else?. 
Want others to notice that new style, [NAME].
If you are bored of your look, then you need a make-over! 
Enter the Beauty Shop and get a new refreshing look!
[BUTTON_NPC]=[Enter Beauty Shop][logout beautyshop]'),
  ('Ayollar', 'Vietree', '(Lalala~lala~lalala~) 
Are you tired of looking like everyone else?. 
Want others to notice that new style, [NAME].
If you are bored of your look, then you need a make-over! 
Enter the Beauty Shop and get a new refreshing look!
[BUTTON_NPC]=[Enter Beauty Shop][logout beautyshop]'),
  ('Salyon', 'Vietree', '(Lalala~lala~lalala~) 
Are you tired of looking like everyone else?. 
Want others to notice that new style, [NAME].
If you are bored of your look, then you need a make-over! 
Enter the Beauty Shop and get a new refreshing look!
[BUTTON_NPC]=[Enter Beauty Shop][logout beautyshop]'),
  ('Xiaoming_7th', 'Xiaoming_7th', 'Was the 8th anniversary cup cake war fun? Hehe if you have a 7th anniv. Token, you can exchange it for these things that I have prepared for you.
You know, the token you have received after enjoying the cake war! Hehe..
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('XiaomingR_7th', 'Xiaoming_7th', 'Hehe I hope the Red team wins. You can purchase cupcakes and soda cannons from me~
[BUTTON_NPC]=[Purchase][server_ack 1]'),
  ('XiaomingB_7th', 'Xiaoming_7th', 'Hehe I hope the Blue team  wins. You can purchase cupcakes and soda cannons from me~
[BUTTON_NPC]=[Purchase][server_ack 1]');
