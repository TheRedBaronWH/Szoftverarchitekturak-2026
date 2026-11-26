# Necker's Paradox

Az oktatópályán a karakter P1-ről P4-re jut. A Unity-jelenet és a konzolos változat ugyanazt a pályadefiníciót és játékszabályokat használja.

## Indítás Unityben

1. Klónozd a projektet, majd a projekt gyökerében futtasd a `git lfs pull` parancsot a modellek és textúrák letöltéséhez.
2. A Unity Hub **Projects → Add → Add project from disk** menüpontjában válaszd az `Assets`, `Packages` és `ProjectSettings` mappákat tartalmazó könyvtárat. Az editor verziója `6000.6.9f1`.
3. Nyisd meg az `Assets/Scenes/GameManagement.unity` jelenetet. Kattints a **Play**, majd az **Új játék** gombra.

Kattintással vagy az **1–4** billentyűkkel választhatsz platformot. A **Space** az A/B módot váltja, az **R** újrakezdi a pályát, az **Esc** visszalép a menübe.

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
quit
```

## Automatikus ellenőrzés

```sh
dotnet run --project Tests/CoreRules
dotnet run --project Tests/TutorialRoute
```

A `CoreRules` a szabályokat, a mentést és a visszatöltést vizsgálja. A `TutorialRoute` az oktatópályát járja be, beleértve a mozgás közbeni tiltásokat és az újrakezdés után beérkező régi mozgásjelzést.
