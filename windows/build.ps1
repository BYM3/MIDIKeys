# Baut MIDIKeys für Windows und installiert es nach %LOCALAPPDATA%\Programs\MIDIKeys.
#
#   .\build.ps1              # bauen + installieren + Startmenü-Verknüpfung
#   .\build.ps1 -Run         # danach gleich starten
#   .\build.ps1 -SelfContained   # ohne vorinstallierte .NET-Laufzeit lauffähig (größer)
#   .\build.ps1 -Zip         # zusätzlich dist\MIDIKeys-windows.zip für Releases
param(
    [switch]$Run,
    [switch]$SelfContained,
    [switch]$Zip,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\MIDIKeys')
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$publishDir = Join-Path $PSScriptRoot 'dist\MIDIKeys'

Write-Host '==> Tests'
dotnet test tests\MIDIKeys.Core.Tests --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Tests fehlgeschlagen' }

Write-Host '==> Baue Release'
$sc = if ($SelfContained) { 'true' } else { 'false' }
dotnet publish src\MIDIKeys -c Release -r win-x64 --self-contained $sc -p:PublishSingleFile=true -o $publishDir --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen' }

# Eine laufende Instanz hält die .exe gesperrt.
Get-Process MIDIKeys -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "==> Beende laufende Instanz (PID $($_.Id))"
    $_.CloseMainWindow() | Out-Null
    if (-not $_.WaitForExit(2000)) { Stop-Process -Id $_.Id -Force; $_.WaitForExit(5000) | Out-Null }
}

Write-Host "==> Installiere nach $InstallDir"
New-Item -ItemType Directory -Force $InstallDir | Out-Null
# Windows gibt die Sperre auf die .exe manchmal erst kurz nach Prozessende frei.
for ($i = 1; ; $i++) {
    try { Copy-Item (Join-Path $publishDir '*') $InstallDir -Recurse -Force -ErrorAction Stop; break }
    catch { if ($i -ge 10) { throw }; Start-Sleep -Milliseconds 500 }
}
$exe = Join-Path $InstallDir 'MIDIKeys.exe'

$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\MIDIKeys.lnk'
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($startMenu)
$lnk.TargetPath = $exe
$lnk.WorkingDirectory = $InstallDir
$lnk.Description = 'Tastatur als MIDI-Keyboard'
$lnk.Save()
Write-Host "==> Startmenü-Verknüpfung: $startMenu"

if ($Zip) {
    $zipPath = Join-Path $PSScriptRoot 'dist\MIDIKeys-windows.zip'
    Remove-Item $zipPath -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath
    Write-Host "==> Zip: $zipPath"
}

Write-Host ''
Write-Host "Fertig: $exe"
if ($Run) { Start-Process $exe } else { Write-Host "Starten mit:  & '$exe'" }
