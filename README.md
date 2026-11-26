# Necker's Paradox

Az oktatópályán a karakter P1-ről P4-re jut. Ezután a Crossroads pályán nyolc platform, egy zsákutca és négy A/B módváltás vezet P8-hoz. A Unity-jelenet és a konzolos változat ugyanazokat a pályadefiníciókat és játékszabályokat használja.

## Indítás Unityben

1. Klónozd a projektet, majd a projekt gyökerében futtasd a `git lfs pull` parancsot a modellek és textúrák letöltéséhez.
2. A Unity Hub **Projects → Add → Add project from disk** menüpontjában válaszd az `Assets`, `Packages` és `ProjectSettings` mappákat tartalmazó könyvtárat. Az editor verziója `6000.6.9f1`.
3. Nyisd meg az `Assets/Scenes/GameManagement.unity` jelenetet. Kattints a **Play**, majd az **Új játék** gombra.

Kattintással vagy az **1–8** billentyűkkel választhatsz platformot. A **Space** az A/B módot váltja, az **R** újrakezdi a pályát, az **Esc** visszalép a menübe. Győzelem után az **N** indítja a következő pályát. A platformok felirata és a cél a Game nézetben is látható.

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
quit
```

## Automatikus ellenőrzés

```sh
dotnet run --project Tests/CoreRules
dotnet run --project Tests/TutorialRoute
```

A `CoreRules` a szabályokat, a mentést és a visszatöltést vizsgálja. A `TutorialRoute` az oktatópályát, a pályaváltást és a Crossroads zsákutcáját és célját járja be. Ellenőrzi a mozgás közbeni tiltásokat és az újrakezdés után beérkező régi mozgásjelzést is.
