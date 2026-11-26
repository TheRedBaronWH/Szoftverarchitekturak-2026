# Necker's Paradox

Öt pályából álló, C# és Unity alapú térbeli logikai játék. A kamera játék közben rögzített; az A/B nézőpontváltás a csapat animált Necker-kockáit és a járható lépcsőkapcsolatokat változtatja. A kapcsolatok szabályait a TutorialCampaign definiálja.

## Indítás

1. A klónozott projekt gyökerében futtasd a `git lfs pull` parancsot. A modellek és a hangfájlok Git LFS-ben vannak.
2. A Unity Hubban nyisd meg az Assets, Packages és ProjectSettings mappákat tartalmazó projektet. Editor: **6000.6.9f1**.
3. Nyisd meg az **Assets/Scenes/GameManagement.unity** jelenetet, majd **Play → Új játék**.

Windows-buildnél a Build Profiles jelenetlistája ebben a sorrendben tartalmazza az alábbiakat:

| Index | Jelenet |
| --- | --- |
| 0 | GameManagement |
| 1 | Player |
| 2 | MainMenu |
| 3 | SampleScene |

A GameManagement marad betöltve; a többi jelenet betöltését és kiürítését a GameManager végzi.

## Irányítás

| Művelet | Kezelés |
| --- | --- |
| Mozgás | A platform billentyűjele; kattintás a platformra, a kis feliratára vagy a nyitott lépcsőre. A nyilak választható kiegészítésként működnek. |
| A/B nézőpont | **Nézőpont** gomb vagy Space |
| Kézi mentés | **Mentés** gomb vagy F5 |
| Kézi betöltés | **Kézi betöltés** gomb vagy F9 |
| Szünet | **Szünet** gomb vagy Esc |
| Kilépés az alkalmazásból | **Kilépés** a főmenüben vagy a szünetmenüben |
| Pálya újrakezdése | R |
| Tipp | H; a célplatform kiemelést kap |
| Diagnosztika | F3 |

A platformok alatt **mindig látszik a hozzájuk tartozó billentyűjel**: 1–9, 0, −, =, majd Q, E, T, Y, U, I, O, P. A látható jel az aktuális billentyűzetkiosztást követi, így magyar vagy orosz kiosztásnál is a megfelelő karakter látszik. Az első tíz platformhoz a numerikus billentyűzet 1–9 és 0 gombjai is használhatók. A P1–P20 technikai azonosítók és a kapcsolatok szöveges listája csak F3 után jelennek meg. Mozgás és A/B-animáció alatt újabb nézőpontváltás nem indul.

## Mentés és betöltés

| Fájl | Ki írja? | Ki olvassa? |
| --- | --- | --- |
| progress.xml | **Mentés / F5** | **Betöltés / F9**, illetve a főmenü Folytatás tartalékként |
| autosave.xml | Elfogadott lépés befejezése, A/B-váltás, újrakezdés, pályaváltás, főmenü, alkalmazás bezárása | Főmenü: **Folytatás**, ha nincs futó játék |

A két fájl az Application.persistentDataPath könyvtárban található. **Betöltés mindig a kézi progress.xml fájlt tölti be**, akkor is, ha az automatikus mentés már másik pályán, másik platformon vagy másik módban van. A Betöltés gomb használható kézi fájl hiányában inaktív, magyarázó üzenettel.

A főmenü Folytatás először a memóriában megőrzött játékot használja. Alkalmazás-újraindítás után az automatikus fájlt próbálja, majd annak hiánya vagy hibája esetén a kézit. A sikertelen betöltés megőrzi a futó játékot és a függőben lévő mozgást. A fájlírás ideiglenes fájlon és atomikus cserén keresztül történik.

A mentés a legutolsó teljesen elért platformot és az A/B módot rögzíti. Sikeres kézi betöltés után az automatikus mentés a betöltött állapothoz igazodik; a kézi fájl ettől nem változik. A szünetmenüből indított betöltés szünetben marad, kivéve egy már teljesített pálya betöltését: ott a befejező ablak és annak visszaszámlálása jelenik meg.

## Pályák és automatikus továbblépés

| Pálya | Platformok | Tartalom |
| --- | --- | --- |
| Első lépések / tutorial | 4 | Alapmozgás és első A/B-váltás |
| Elágazások / crossroads | 8 | Mellékág és visszatérés a fő útvonalra |
| Égi hidak / skybridge | 10 | Két megoldható útvonal |
| Paradox lépcsők / paradox-stairs | 12 | Három mellékág, hosszabb lépcsős útvonal és hét kötelező A/B-váltás |
| Szőtt labirintus / woven-labyrinth | 20 | Négy magassági szint, 22 kapcsolat, három független kör, zsákutca és négy kötelező kapcsolókitérő |

Az első négy pálya céljára érkezéskor automatikus mentés készül; az ötödik végén mindkét mentési hely törlődik. A befejező ablak **2,5 másodperc** után automatikusan elindítja a következő pályát, amelynek kezdőállapota szintén mentésre kerül. A **Következő pálya** gombbal korábban is tovább lehet lépni. Az újrakezdés és a főmenü megszakítja az aktuális visszaszámlálást.

A tizedik platform fölötti két példányt a TutorialBoard a csapat eredeti prefabjának futásidejű másolatából készíti; nem kell új objektumokat kézzel bekötni a jelenetben. A meglévő három pálya azonosítói és mentési formátuma megmaradtak. Egy korábbi, már teljesített Skybridge-mentés folytatása az új negyedik pályára vezet.

## Megjelenítés

A PlatformSurface az animált kocka pillanatnyi meshéből számít támaszpontot és felületi orientációt. A figura, a céljel és a hidak ezeket követik A és B állapotban is. A figura feje **a csapat eredeti gömbmeshét és emisszív anyagát használja**, az eredeti Particle System textúrája, színei és animációja megmarad. A prefab fájlját nem módosítjuk: futás közben kerül a fejhez törzs, kar és láb. A figura normál mélységtesztet használ, hogy ne látszódjon át a kockákon. A HoverOutline komponens megmarad; ezen a táblán mélységhelyes színvisszajelzést kap, a többi jelenetben az eredeti működése marad. A lábak lépés közben mozognak, a talpnál kontaktárnyék van.

A StairBridge a nyitott kapcsolatokhoz fehér lépcsőfelületet és sötét éleket rajzol. A WalkableFace a teljes járható lap körvonalára illeszti a lépcső mindkét szélét, így nem állandó távolságot vágunk le a kocka középpontjától. A WalkwayPath ugyanazokat a csatlakozópontokat és lépcsőmagasságokat adja a rajzoláshoz és a figura mozgásához. A zárt hagyományos kapcsolatok halvány körvonalként jelennek meg. A vizuális átmenet alatt új mozgás nem indul.

A **Assets/Resources/OLE/Presentation** beállításban módosítható a paletta, az anyagok, a finálé zenéje, hangereje és a készítők neve.

## Finálé

Az ötödik pálya céljára érkezéskor a játék törli a kézi és automatikus mentési helyet, rövid gratulációt jelenít meg, majd automatikusan elindítja a zárójelenetet: lassú kameramozgás, csillagos háttér, zene és alkotói névsor.

A **Kihagyás** vagy Esc a névsor végére ugrik és leállítja a zenét. A végén **Új játék** és **Főmenü** választható. Mindkettő leállítja a finálét és helyreállítja a kamerát. Befejezett kampány után az új játék az első pályáról indul; a régi kézi mentés már nem marad meg.

A beállított **Assets/Audio/OLE_Finale_User.ogg** a felhasználó által adott Star Wars.mp3 első **58 másodperce**, három másodperces lekeveréssel. Nem ismétlődik; a FinaleDirector más klip esetén is legfeljebb 60 másodpercig játszik. Az OLE_Finale_Original.wav megmaradt tartaléknak. A Presentation / Finale Music mezőben cserélhető a klip. A pályák végén rövid konfetti, a fináléban nagyobb színes konfettieső jelenik meg.

Készítők: Kopácsi Artem, Kohár Zsombor, Bencze János István, Béres Mátyás.

## A kód részei

| Rész | Fájlok és felelősség |
| --- | --- |
| Tiszta C# szabályok | GameSession, LevelDefinition, RouteHints |
| Játék és mentések | GameProgress, SaveStore, SaveMac |
| Időzítés és lépcsőprofil | CompletionTimer, StairProfile; Unity-függőség nélkül tesztelhetők |
| Unity-kapcsolat | GameplayController: jelenetkötés, bevitel, mozgás, szünet, befejezés |
| Pálya megjelenítése | TutorialBoard, PlatformSurface, StairBridge, SceneMaterials |
| Figura és eredeti effekt | AvatarVisual; a Player prefab gömbje, anyaga és Particle Systemje |
| Felület | MenuUI, GameHUD, PauseMenuUI, LevelCompleteUI, MainMenuActions |
| Finálé | FinaleDirector, CelebrationEffect, PresentationSettings és a hozzárendelt OGG |

## Automatikus tesztek

A különálló konzolos ellenőrzéshez .NET 8 SDK szükséges.

```sh
dotnet run --project Tests/CoreRules
dotnet run --project Tests/TutorialRoute
dotnet run --project Tests/PresentationGeometry
```

A CoreRules 40 szabály- és fájlkezelési tesztet, valamint egy független, 300 parancsos referenciavisszajátszást tartalmaz. A TutorialRoute ellenőrzi mind az öt pályát, a korábbi mentések kompatibilitását, a szünetet, a két mentési helyet, a visszaszámlálást és a lépcsőprofilokat.

A RemainingIntegrationTests egy korábbi tutorial/P2/A kézi mentést állít vissza egy későbbi Crossroads/B automatikus állapot helyett, majd az új negyedik pálya közepén mentett pontot is visszatölti a frissebb automatikus győzelem helyett.

A .NET-tesztek a C#-logikát vizsgálják. A Unity-felület, a mesh-megjelenítés, a hang és a Windows-build kézi ellenőrzést igényel.

## Kézi ellenőrzés

1. Indítsd a GameManagement jelenetet. Az állapotsáv, a négy kezelőgomb és a platformok billentyűjelei F3 nélkül is látszódjanak; a P1-felirat és a kapcsolati lista csak F3 után jelenjen meg.
2. Mozogj kattintással, a nyilakkal és nyitott lépcsőre kattintva. A talp kövesse a lépcsőfokokat, ne a két platform közti egyenes magasságát.
3. Válts A/B-t álló helyzetben. A kocka, a figura támaszpontja és a lépcsővégek együtt változzanak; az átmenet alatt ne induljon új lépés.
4. Tutorial P2/A állapotban válaszd a Mentés gombot. Haladj tovább P3-ra és B módba. Betöltés után P2/A térjen vissza.
5. Ismételd meg a kézi mentést, majd fejezd be a tutorialt és haladj a második pályán. Betöltés továbbra is az első pálya mentett pontjára vigyen.
6. Mozgás közben nyiss szünetet. A figura, a lábak és a kockák álljanak meg. Folytatás ugyanazt a mozgást fejezze be.
7. Tutorial befejezésekor a mentési állapot jelenjen meg, majd 2,5 másodpercen belül induljon a Crossroads. Ismételt következő-pálya művelet ne ugorhasson át befejezetlen pályát.
8. Zárd be és indítsd újra az alkalmazást F5 nélkül. A Folytatás az automatikusan mentett pályát és állapotot állítsa helyre.
9. A negyedik pályán járd be a három mellékágat is; legyen lehetséges visszatérni. Tippel és saját megoldással is lehessen P12-re érni.
10. Az ötödik pályán az A/B csak a türkiz kapcsolókon működjön. A kereszteződő hidak ne hozzanak létre új gráfkapcsolatot; a zsákutcából lehessen visszafordulni.
11. A végén induljon a zárójelenet zenével és a négy névvel. Kihagyás állítsa le a zenét; Új játék után a kamera, a bevitel és az első pálya működjön. Főmenü után a zene ne maradjon szólva.

A lépcsős pálya ellenőrző útvonala a konzolban: P1 → P2 (A) → P3 (B) → P6 (A) → P5 (B) → P8 (A) → P11 (B) → P10 (A) → P12 (B). A kapcsolatok kétirányúak; az A/B-zás mindkét irányban ugyanazt a szabályt használja.

## Konzolos futtatás

```sh
dotnet run --project Tools/TutorialConsole
```

A go P2 parancs elindít egy lépést, az arrive a Unity-animáció végét jelenti. A switch az A/B-váltás, a hint a következő lépés, a next a teljesített pálya utáni továbblépés. A konzolban nincs grafikus visszaszámlálás vagy zárójelenet.

## Kísérleti perspektívakapcsolatok és az ötödik pálya

A Presentation / **Perspective Seams** kapcsolóval a kísérleti megjelenítés ki- és bekapcsolható. A csapat Necker_Cube FBX-e, animációja és meglévő jelenetkötései megmaradnak. Egyes hidak két félből állnak: a találkozó élek 1,25 egységre vannak egymástól a kamera mélységi irányában, de nyitott állapotban az ortografikus vetületük azonos. A figura ugyanitt vált mélységet; a képernyőn nem ugrik el. A másik módban valódi, látható rés nyílik a két fél között, a kocka eredeti animációjával együtt. Ez rögzített nézethez készített prototípus, nem tetszőleges kameraforgatásra megírt Echochrome-motor.

Az ötödik pályán a **türkiz A/B-jelű kapcsolók**: P4, P9, P14 és P18. A többi négy pálya továbbra is bárhol váltható. Egy geometriai keresztezés önmagában nem kapcsolat: a különböző magasságú hidak egymás fölött haladnak el. A minimumút 25 művelet; mind a négy kapcsolót fel kell keresni és vissza kell térni a megfelelő csomóponthoz. A H-tipp ezt a korlátozást is figyelembe veszi.

A LabyrinthTests független kereséssel ellenőrzi a 34 elérhető platform/mód állapotot, a zsákutcákból kijutást, a kapcsolók szükségességét, a mentést és a tippeket. A PresentationGeometry a csatlakozófelület teljes szélességét, a talpút végpontjait és a perspektívarés vetületi folytonosságát ellenőrzi egy különálló, csak tesztekhez használt matematikai segédkóddal. Ez nem helyettesíti a Unity Play Mode, a renderelés és a Windows-build próbáját.

## Átmenetek és kijelölés javítása

Az elsődleges kezelés változatlanul a platformokhoz tartozó billentyű. A kis feliratok a kockák alatt jelennek meg, körülbelül 12–14 képpontos betűkkel; nem takarják a figurát vagy a lépcső bejáratát. A feliratra kattintás ugyanazt a mozgáskérést indítja, mint a billentyű. A nyilak a képernyő iránya szerint választanak nyitott szomszédot; közel azonos iránynál a közelebbi cél nyer. A zárt kapcsolatokon sem billentyűvel, sem kattintással, sem nyíllal nem lehet átmenni.

A SampleScene 12 eredeti kockát tartalmaz, de a TutorialBoard 10-et regisztrál. A két regisztrálatlan bemutatókocka a jelenetfájlban megmarad; játék közben inaktív, így nem takarja el a célt, és nem fogja el a kattintást. A lépcsők pontos MeshColliderje a megrajzolt meshből épül fel, a perspektivikus félhidaknál is. A zárt vagy animáció alatt álló lépcső nem kattintható; az átmenet végén a nyitott lépcső újra kiválasztható.

### Öt pálya ellenőrzése

A `dotnet run --project Tests/TutorialRoute` a TraversalMatrixTests segítségével mind az öt pálya összes elérhető platform/mód állapotából ellenőrzi az engedélyezett és tiltott lépéseket, a visszautat és a célplatform lezárását. A PresentationGeometry a nyilak irányválasztását is ellenőrzi.

A natív Unity-ellenőrzéshez indítsd a **GameManagement → Play → Új játék** folyamatot, várd meg, hogy a figura álljon, majd válaszd a **Tools → OLE → Check all level boards (Play Mode)** menüpontot. Ez a tényleges Unity Animatorral és fizikával végigellenőrzi az öt tábla mindkét A/B állapotát: az aktív kockák számát, a feliratok célját, a lépcsők MeshCollider-raycastját és a mozgási végpontokat mindkét irányban. Az eredmény a Console ablakban `OLE board check PASS`, hiba esetén konkrét pálya és kapcsolat. A futó játékállapot és a mentési fájlok nem módosulnak; az eredeti tábla a vizsgálat után visszaáll.

Kézi regresszió: tutorial P3-on válts B-re, majd külön próbáld a 4-es billentyűt, a 4-es kis feliratot és a lépcsőre kattintást. R után ismételd meg. A módban a P3–P4 kapcsolat maradjon zárt.

## Befejezett kampány és kilépés

**A teljes öt pálya befejezése törli a progress.xml és autosave.xml fájlokat.** A gratuláció és a stáblista az élő memóriabeli végállapottal tovább működik. A finálé indulása, a Főmenü és a Kilépés/OnApplicationQuit nem írhatja vissza a befejezett mentést. A főmenübe visszalépés eldobja a befejezett memóriabeli folytatást is; a Folytatás inaktív marad, és az Új játék a tutorial P1/A állapotából indul. Régebbi verzióval írt, már teljesített ötödik pályás mentés is törlődik a folytatás ellenőrzésekor.

A törlés csak a két mentési helyet és az adott aláírt helyhez tartozó .key fájlt érinti, más beállításokat nem. Törlési hiba esetén a felület hibát jelez; későbbi kilépési művelet újra megpróbálja a törlést.

A kampány befejezése előtti Kilépés továbbra is automatikusan ment. Szünet vagy megkezdett lépés közbeni kilépés a legutolsó elért platformot tárolja. A kézi mentési helyet az automatikus mentés továbbra sem írja felül.

A **Kilépés** gomb a főmenüben és a szünetmenüben is elérhető. Windows-buildben bezárja az alkalmazást; Unity Editorban a Play Mode-ot állítja le.

A CompletionCleanupTests ellenőrzi az azonnali törlést a végső érkezéskor, a köztes pályák mentéseinek megőrzését, az F5/F9 és ismételt kilépési callbackek védelmét, a régi befejezett mentések kezelését, az új kampány kezdőállapotát és a törlési hiba utáni újrapróbálást.
