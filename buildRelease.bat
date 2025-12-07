@echo off
setlocal enabledelayedexpansion

:: Optional das Signieren aktivieren oder deaktivieren
:: $ buildRelease.bat true
:: Mit `true` baut und signiert es alles, Assemblies und Setup.
set SIGNING_ENABLED=false

:: Prüfen, ob ein Parameter übergeben wurde
if not "%~1"=="" set SIGNING_ENABLED=%~1

echo Signing Enabled: %SIGNING_ENABLED%

:: Konfiguration
set BUILD_CONFIGURATION=Release
set BUILD_DIR=%CD%\%BUILD_CONFIGURATION%

:: GlobalSign	http://timestamp.globalsign.com/tsa/r6advanced1
:: DigiCert	http://timestamp.digicert.com
:: Sectigo (Comodo)	http://timestamp.sectigo.com
set TIMESTAMP_URL=http://timestamp.globalsign.com/tsa/r6advanced1

set SETUP_SCRIPT=buildReleaseSetup.iss
set INNOSETUP_PATH="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

:: Build-Verzeichnisse erstellen
if exist %BUILD_DIR% rmdir /s /q %BUILD_DIR%
mkdir %BUILD_DIR%

:: Lösche alte Setup-Dateien
if exist setup\railhqGatewaySetup.exe del setup\railhqGatewaySetup.exe

:: Baue die Projekte
dotnet build Gateway\railyGateway\railyGateway.csproj --configuration %BUILD_CONFIGURATION%
dotnet build Gateway\railySystray\railySystray.csproj --configuration %BUILD_CONFIGURATION%

:: Publiziere
dotnet publish Gateway\railyGateway\railyGateway.csproj --configuration %BUILD_CONFIGURATION% -o %BUILD_DIR%\railyGateway -r win-x64 --self-contained
dotnet publish Gateway\railySystray\railySystray.csproj --configuration %BUILD_CONFIGURATION% -o %BUILD_DIR%\railySystray -r win-x64 --self-contained

:: Kopiere Systray zu Gateway
xcopy /E /Y %BUILD_DIR%\railySystray\* %BUILD_DIR%\railyGateway\
xcopy /E /Y %BUILD_DIR%\railyGateway\* %BUILD_DIR%\railhqGateway\

:: PDB-Dateien auslagern
mkdir "%BUILD_DIR%\pdbs"
for /r "%BUILD_DIR%" %%f in (*.pdb) do move "%%f" "%BUILD_DIR%\pdbs\"

if "%SIGNING_ENABLED%"=="true" (
    :: Signiere nur nicht-signierte Dateien
    for %%f in (%BUILD_DIR%\railhqGateway\*.exe %BUILD_DIR%\railhqGateway\*.dll) do (
        signtool verify /pa "%%f" >nul 2>&1
        if errorlevel 1 (
            echo Signing %%f
            signtool sign /n "riesolution" /tr %TIMESTAMP_URL% /td SHA256 /fd SHA256 "%%f"
        ) else (
            echo Already signed: %%f
        )
    )
)

:: ZIP-Erstellung
powershell Compress-Archive -Path "%BUILD_DIR%\railhqGateway\*" -DestinationPath "%BUILD_DIR%\railhqGateway.zip"

:: Version in relevanten Dateien aktualisieren
call version.bat

:: Setup erstellen
if exist %INNOSETUP_PATH% (
    %INNOSETUP_PATH% %SETUP_SCRIPT%
) else (
    echo Inno Setup nicht gefunden, bitte installieren!
    exit /b 1
)

:: Lese die Versionsnummer aus der version.txt und entferne führende/abschließende Leerzeichen und Newlines
for /f "delims=" %%a in (version.txt) do set VERSION=%%a
set VERSION=%VERSION: =%

:: Erstelle den Ziel-Dateinamen mit der Versionsnummer
set TARGET_FILE=resourcesSetups\railhqGatewaySetup-%VERSION%.exe

:: Führe den Copy-Befehl aus und ersetze {VERSIONSNUMMER} mit der Versionsnummer
cp "Setup\railhqGatewaySetup.exe" "%TARGET_FILE%"

if "%SIGNING_ENABLED%"=="true" (
    :: Setup signieren (nur falls nicht bereits signiert)
    if exist "%TARGET_FILE%" (
        signtool verify /pa "%TARGET_FILE%" >nul 2>&1
        if errorlevel 1 (
            echo Signing Setup...
            signtool sign /n "riesolution" /tr %TIMESTAMP_URL% /td SHA256 /fd SHA256 "%TARGET_FILE%"
        ) else (
            echo Setup ist bereits signiert.
        )
    ) else (
        echo Setup wurde nicht erstellt!
        exit /b 1
    )
)

:: zeige Setup Information
"Tools\FileInfoHelper\bin\Release\net8.0\FileInfoHelper.exe" "%TARGET_FILE%"

echo Build abgeschlossen!
exit /b 0
