# ILTISSimulator – pokyny pre agentov

Simulátor ILTIS – posiela INISSu správy GTN o pohybe vlakov podľa grafikonu (protokol v `iniss-tools-docs/docs/iniss/protokoly/iltis.mdx`). Repozitár je súčasťou pracovného priečinka `INISSTools` (nie je git repozitár, každý podpriečinok má vlastné repo); vedľa musia byť naklonované `ExControls`, `ToolsCore` a `GVDEditor` (projekt ho odkazuje). Komunikácia, texty UI aj komentáre v kóde (bez diakritiky) sú slovenské.

Pravidlá desktopových programov (WinForms, texty v `Resources`, ručné `*.Designer.cs`, príkazy `CommandSet`, téma, dialógy, ikony SVG) a logovania (Serilog) sú v `../ToolsCore/AGENTS.md` – platia aj tu.

## Štruktúra a zásady

- Projekty: `ILTISSimulator` (`FMain` rozdelený na partial podľa oblastí – `Run`, `Gvd`, `Scenario`, `GtnLog`, `Manual`, `Log`; `Net/GtnClient` – spojenie s INISSom, `Simulation/SimulationEngine` – plán a tik simulácie, `Monitor/GtnLogTail` – odpovede INISSu z `GTN.LOG`), `ILTISSimulatorTests`, `ILTISSimulator.DocScreenshots`.
- INISS2: otvorený priečinok stanice (`Data\<grafikon>\<stanica>`) spozná `Data/Iniss2GvdReader` a naplní rovnaký `GvdData`; každé zastavenie varianty je `Train` s dňami platnej varianty a platnosťou posunutou na deň v stanici. Rozdiely ide cez `GvdData.Iniss2` (`IsIniss2`, `TrackOn` – koľaj podľa dňa, `ArrivalNumber`/`DepartureNumber`) a `TrainRun.Iniss2`: `GtnMessageBuilder` pridá pravidelný čas (vjazdová cesta podľa príchodu), `RunEvents` vynechá dôvod meškania, 0805 a 0806 nie sú v ponukách. Port a denný log berie zo servera. Správanie servera je v `iniss-tools-docs/docs/iniss2/protokoly/gtn.mdx`.
- DPI PerMonitorV2 (na rozdiel od ostatných programov) – rozmery z návrhu sa neškálujú samy, treba `Control.Scale` podľa `DeviceDpi`.
- Záznam v hlavnom okne (`AppendLog`) je len pre obsluhu; do logu programu sa zapisujú pripojenia a chyby s výnimkou (`SimulationEngine` má udalosť `Log` – tam `Serilog.Log`). Každá správa protokolu nanajvýš Debug.

## Príkazy

```bash
dotnet build ILTISSimulator.slnx -nologo -v q
dotnet test ILTISSimulator.slnx
"/c/Program Files/Inno Setup 7/ISCC.exe" ILTISSimulatorInstaller.iss   # instalator do Output/; najprv Release build
```

Vydanie: značka `vX.Y` spustí `.github/workflows/release.yml` (spoločný postup v `ToolsCore/Installer/README.md`).

## Spoločné pravidlá

- `Directory.Build.props`: `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `AnalysisLevel latest-recommended` – build bez jediného varovania; zámerne vypnuté pravidlá len tam (`NoWarn` s dôvodom), varovanie opraviť v kóde, výnimočné potlačenie priamo na nájdenom člene (`[SuppressMessage(..., Justification = ...)]`).
- Kultúra: UI `CultureInfo.CurrentCulture`, formáty, protokol a logy `InvariantCulture`.
- GUI nespúšťať cez Bash – overuje ho používateľ ručne; bez klikania harness (reflexia + `DrawToBitmap`).
- Testy MSTest, metódy `Oblast_CoOveruje`; do repa len testy s vlastnými dátami (korpusové overenie nad `D:\INISSroot` len na stroji používateľa).
- Skripty: generátory ukladať ako súbor (bash heredoc mení `\\` a `\n`); v `.ps1` žiadna diakritika.

## Commity (Conventional Commits)

Commitovať len na požiadanie. Správy podľa [Conventional Commits](https://www.conventionalcommits.org/): `<typ>(<oblasť>): <popis>`.

- Typ: `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `build` (projekty, balíky, inštalátor), `ci` (workflow), `style`, `chore`, `revert`.
- Oblasť nepovinná, malými písmenami (napr. `gtn`, `simulation`, `scenario`, `track-diagram`).
- Popis anglicky v rozkazovacom spôsobe, malým písmenom, bez bodky, najviac 72 znakov; názvy tried v backtickoch. Telo po prázdnom riadku vysvetľuje prečo; nekompatibilná zmena (formát scenára, config.xml, príkazový riadok) má `!` a pätičku `BREAKING CHANGE: …`.
- Príklad: ``fix(gtn): reconnect `GtnClient` after INISS restarts``.
