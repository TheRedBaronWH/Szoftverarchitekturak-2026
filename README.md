# Necker's Paradox

Az oktatópályán a karakter P1-ről P4-re jut. A Crossroads pályán nyolc platform, egy zsákutca és négy A/B módváltás vezet P8-hoz. A Skybridge tíz platformja két útvonalat kínál P10 felé. A Unity-jelenet és a konzolos változat ugyanazokat a pályadefiníciókat és játékszabályokat használja.

## Indítás Unityben

1. Klónozd a projektet, majd a projekt gyökerében futtasd a `git lfs pull` parancsot a modellek és textúrák letöltéséhez.
2. A Unity Hub **Projects → Add → Add project from disk** menüpontjában válaszd az `Assets`, `Packages` és `ProjectSettings` mappákat tartalmazó könyvtárat. Az editor verziója `6000.6.9f1`.
3. Nyisd meg az `Assets/Scenes/GameManagement.unity` jelenetet. Kattints a **Play**, majd az **Új játék** gombra.

Kattintással vagy az **1–9** billentyűkkel választhatsz platformot, a **0** a P10-et jelenti. A **Space** az A/B módot váltja, az **R** újrakezdi a pályát, az **Esc** megnyitja a szünetmenüt. Győzelem után az **N** indítja a következő pályát. A **H** egyetlen következő lépést vagy módváltást javasol.

Minden pályának saját elrendezése és színvilága van. A fehér platform az aktuális hely, az arany a cél, a kiemelt szín az elérhető szomszédot jelzi. A világos vonalak járható kapcsolatokat, a vékony sötét vonalak az aktuális módban lezárt kapcsolatokat mutatják.

## Mentés és folytatás

Az **Esc** megállítja a karakter mozgását és a pálya animációit. A szünetmenü **Folytatás** gombja vagy az újabb **Esc** ugyanonnan folytatja a mozgást. Szünet alatt a játékbillentyűk és a platformokra kattintás nem változtatják az állapotot. A menü gombjai egérrel és billentyűzettel is kezelhetők.

A szünetmenü **Főmenü** gombja az utolsó teljesen elért platformot és az A/B módot a memóriában tartja meg, és automatikusan menti. A főmenü **Folytatás** gombja ezt a futó játékot részesíti előnyben a fájllal szemben. Az **Új játék** mindig az oktatópálya P1 platformjáról, A módban indul, és frissíti az automatikus mentést. A korábbi kézi mentést csak a **Mentés** vagy **F5** írja felül. Ha a főmenübe egy teljesített pályáról lépsz ki, a folytatás a következő pálya elejére visz. Az utolsó pálya után a befejezett állapot marad meg.

| Művelet | Eredmény |
| --- | --- |
| Szünetmenü: **Mentés** / játék közben **F5** | A kiválasztott állapot kézi mentése. A szünet és a folyamatban lévő mozgás megmarad. |
| Szünetmenü: **Betöltés** / játék közben **F9** | Visszatérés a kézi mentéshez. A régi mozgás megszakad; a szünetmenüből indított betöltés után a játék szünetben marad. |
| Új játék, célba érő lépés, elfogadott A/B-váltás, pálya újrakezdése vagy váltása, kilépés a főmenübe, alkalmazás bezárása | Automatikus mentés az utolsó elért platformról. |
| Főmenü: **Folytatás** alkalmazás-újraindítás után | Először az automatikus mentést ellenőrzi. Ha az hiányzik vagy nem tölthető be, a kézi mentést próbálja. |

A két mentés külön fájlban van: az automatikus előrehaladás nem írja felül a kézzel kiválasztott visszatérési pontot. Sikeres kézi betöltés után az automatikus mentés is a betöltött állapothoz igazodik. A szünetmenü **Betöltés** gombja csak érvényes kézi mentés esetén aktív; a mellette látható szöveg megmagyarázza, ha nincs használható kézi mentés. A mentés eredménye a szünetmenüben és a játék állapotsávjában is megjelenik.

A **Folytatás** gomb csak futó játék vagy ellenőrzött, betölthető mentés esetén aktív. Ha egy használhatatlan automatikus mentés helyett a kézi mentés töltődött be, a játék ezt jelzi. Egy sikertelen betöltés megőrzi a futó játékot, a szünetet és a függőben lévő mozgást. A sikertelen automatikus írás hibaüzenetet ad, de nem vonja vissza az elfogadott játékbeli műveletet.

A kézi fájl az `Application.persistentDataPath` könyvtárban `progress.xml`, az automatikus `autosave.xml` néven található. Az útvonalakat a `GameplayController.SavePath` és `GameplayController.AutosavePath` tulajdonságai adják vissza. A korábbi `progress.xml` fájlok továbbra is betölthetők. A meglévő `SaveStore` ellenőrzi az adatokat és ideiglenes fájlon keresztül, atomikus cserével ír. Mozgás közbeni mentés az utolsó elért platformot rögzíti, nem a karakter két platform közötti látványpozícióját.

## Pályák szerkesztése

A szabályok az `Assets/Scripts/GameManagement/TutorialCampaign.cs` fájlban vannak. A `SampleScene` jelenet `Tutorial Board` objektumán a `Platforms` lista kapcsolja a platformazonosítókat a csapat kockáihoz. A `Layouts` lista pályánként tárolja a pozíciókat, a kameraméretet és a színeket. A pozíciók sorrendje a pályadefiníció platformjainak sorrendje, P1-től indulva.

Új játékhoz a `GameManagement` jelenetet indítsd. A jelenetkezelő tölti be a menüt és a játékos jelenetét.

## Konzolos próba

A projekt gyökerében, .NET 8 SDK-val futtasd a következő parancsot.

```sh
dotnet run --project Tools/TutorialConsole
```

Írd be egymás után az alábbi parancsokat. A `go` elindítja a mozgást, az `arrive` a Unity-animáció végének felel meg.

```text
go P2
arrive
go P3
arrive
switch
go P4
arrive
next
go P2
arrive
switch
go P5
arrive
switch
go P6
arrive
switch
go P7
arrive
switch
go P8
arrive
next
go P2
arrive
go P5
arrive
switch
go P8
arrive
switch
go P6
arrive
go P7
arrive
switch
go P9
arrive
switch
go P10
arrive
quit
```

## Automatikus ellenőrzés

```sh
dotnet run --project Tests/CoreRules
dotnet run --project Tests/TutorialRoute
```

A `CoreRules` a szabályokat, a mentést és a visszatöltést vizsgálja. A `TutorialRoute` mindhárom pályát, a Skybridge két útvonalát és a tippeket ellenőrzi. A pályaváltás, a régi mentések betöltése és a hibás mentés miatti állapotmegőrzés is része a vizsgálatnak. A `FirstStageTests.cs` tíz szünet- és folytatási forgatókönyvet futtat. A `SecondStageTests.cs` tizenkét további forgatókönyvben az automatikus mentést, a két mentési hely elkülönítését, a szünetben történő betöltést és a fájlhibák kezelését ellenőrzi. Mindkettő valódi ideiglenes mentési fájlokat használ.

## A szünet és folytatás megvalósítása

| Fájl | Feladat |
| --- | --- |
| `Assets/Scripts/OLE.Core/Runtime/GameSession.cs` | A szünet állapota, a műveletek tiltása és a mozgás befejezésének ellenőrzése. |
| `Assets/Scripts/OLE.Core/Runtime/GameProgress.cs` | A futó játék megtartása, az új játék, a két mentési hely és a folytatás sorrendje. Az elfogadott műveletek után automatikusan ment; Unity-függőség nélkül tesztelhető. |
| `Assets/Scripts/GameManagement/GameplayController.cs` | A C#-állapot összekapcsolása a jelenetekkel, az animációval és az idő megállításával. |
| `Assets/Scripts/GameManagement/PauseMenuUI.cs` | A szünetmenü négy gombjának felépítése, bekötése és a mentési visszajelzés megjelenítése. |
| `Assets/Scripts/GameManagement/MainMenuActions.cs` | A főmenü Új játék és Folytatás gombjainak kezelése. |
| `Assets/Scripts/GameManagement/TutorialControls.cs` | Esc és a többi billentyű kezelése, a játékbevitel tiltása szünetben. |

A `PauseMenuUI` futás közben jön létre a pálya jelenetében. Nem kell kézzel gombokat vagy eseményeket hozzáadni a jelenethez. A `GameManagement` jelenet a jelenetváltások között is betöltve marad, ezért a benne lévő `GameplayController` meg tudja tartani a `GameProgress` példányát.

Az automatikus mentéshez a Unity-vezérlő a `GameProgress.CompleteMove`, `TryToggleMode`, `TryNextLevel` és `RestartLevel` műveleteit használja. A `GameSession` továbbra is a játékszabályokat kezeli, a `SaveStore` pedig a fájlműveleteket. A `GameProgress` harmadik, opcionális paramétere nélkül az automatikus mentés kikapcsolva marad; a Unity-vezérlő itt adja át a második mentési helyet.

## Kézi ellenőrzés Unityben és Windows-buildben

Az automatikus .NET-próbák a C#-állapotot és a mentési fájlokat vizsgálják. A Unity felületét és animációját külön ellenőrizd Play módban, majd Windows-buildben is:

1. **Új játék → P2 → Space → Esc.** A szünetmenü jelenjen meg. A számgombok és a Space ne mozgassák a karaktert és ne váltsanak módot. A **Folytatás** tartsa meg P2-t és a B módot.
2. Mozgás közben nyomj **Esc**-et. A karakter és az animáció álljon meg; folytatás után ugyanaz a mozgás fejeződjön be. A gomb megnyomása ne indítson új platformlépést.
3. **Esc → Főmenü → Folytatás**, előzetes F5 nélkül. Ugyanaz a pálya, az utolsó elért platform és az A/B mód térjen vissza. Ha mozgás közben léptél ki, a kiinduló platform legyen az aktuális hely.
4. Érj el egy platformot, nyomj **F5**-öt, majd zárd be az alkalmazást. Újraindítás után a főmenü **Folytatás** gombja a fájlból állítsa helyre a mentett helyet és módot.
5. A főmenü **Új játék** gombja korábbi haladás mellett is a tutorial P1 platformjáról, A módban induljon.
6. Teljesítsd az oktatópályát, majd válaszd az **Esc → Főmenü → Folytatás** útvonalat. A Crossroads eleje jelenjen meg. Közvetlenül a pályán továbbra is az **N** visz a következő pályára.

A második szakasz ellenőrzése:

1. Indíts új játékot, lépj P2-re, válts B módra, majd zárd be az alkalmazást **F5 nélkül**. Újraindítás után a **Folytatás** P2-t és B módot állítson helyre az automatikus fájlból.
2. P2-n válaszd az **Esc → Mentés** gombot. A mentési üzenet jelenjen meg, a játék maradjon szünetben. Folytasd, lépj P3-ra, majd **Esc → Betöltés**: a karakter térjen vissza P2-re, és a szünetmenü maradjon nyitva.
3. A kézi betöltés után zárd be és indítsd újra az alkalmazást. A főmenü **Folytatás** gombja is a betöltött P2-t állítsa helyre.
4. Ha még nincs kézi mentés, a **Betöltés** gomb legyen inaktív akkor is, ha az automatikus folytatás működik. Ezt külön, üres mentési könyvtárral ellenőrizd, a saját mentések biztonsági másolata mellett.
5. Mozgás közben lépj szünetbe, majd ments. A karakter ne mozduljon el és ne fejezze be a lépést; folytatás után ugyanaz a mozgás fejeződjön be.
