@echo off
setlocal enabledelayedexpansion

:: Pfad zur Versionsdatei
set VERSION_FILE=version.txt

:: Prüfen, ob die Versionsdatei existiert
if not exist "%VERSION_FILE%" (
    echo 1.0 > "%VERSION_FILE%"
)

:: Version aus der Datei lesen
set /p VERSION=<"%VERSION_FILE%"

:: Teile der Version extrahieren (Major.Build)
for /f "tokens=1,2 delims=." %%a in ("!VERSION!") do (
    set MAJOR=%%a
    set BUILD=%%b
)

:: Build-Nummer inkrementieren
set /a BUILD+=1

:: Neue Version zusammensetzen
set NEW_VERSION=%MAJOR%.%BUILD%

:: Neue Version in die Datei schreiben
echo !NEW_VERSION! > "%VERSION_FILE%"

echo Neue Version: %NEW_VERSION%
