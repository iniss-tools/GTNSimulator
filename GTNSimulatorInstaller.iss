; Instalator GTNSimulatora. Spolocny obsah instalatorov je v ToolsCore\Installer\AppSetup.iss.
; Pred kompilaciou treba zostavit Release: dotnet build GTNSimulator\GTNSimulator.csproj -c Release

#define MyAppName "GTNSimulator"
#define MyAppExeName "GTNSimulator.exe"
#define MyAppDir SourcePath + "GTNSimulator\bin\Release\net10.0-windows"
#define MyOutputBaseFilename "gtnsimulator-setup"
; Grafikon cita GVDEditor.dll. Spustac GVDEditora a jeho nastavenia behu SDK kopiruje k referencovanemu programu,
; GTNSimulator ich nepotrebuje.
#define MyAppExcludes ",\GVDEditor.exe,\GVDEditor.runtimeconfig.json,\GVDEditor.deps.json"
; framework ASP.NET Core prenasa referencia na GVDEditor - bez neho sa program nespusti
#define NeedAspNetCore

#include "..\ToolsCore\Installer\AppSetup.iss"
