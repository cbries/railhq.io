[Setup]
AppId=railhq.io_-_Gateway
AppName=railhq.io - Gateway
AppVersion=1.28
VersionInfoVersion=1.28
DefaultDirName={commonappdata}\railhqGateway
DefaultGroupName=railhqGateway
UninstallDisplayIcon={app}\railhq_icon2.ico
SetupIconFile=Setup\railhq_icon2.ico
WizardSmallImageFile=Setup\railhq_icon58.bmp
WizardImageFile=Setup\WizardImageFile.bmp
OutputDir=setup
OutputBaseFilename="railhqGatewaySetup"
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

; Metainformationen
AppCopyright=© 2025 riesolution (Dr. Christian Benjamin Ries)
AppPublisher=riesolution (Dr. Christian Benjamin Ries)
AppPublisherURL=https://www.riesolution.de
AppSupportURL=https://www.railhq.io/support
AppContact=mail@riesolution.de
LicenseFile=setup\license.txt

[Files]
; Füge alle entpackten Dateien hinzu
Source: "Release\railhqGateway\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "Setup\railhq_icon2.ico"; DestDir: "{app}"

[VersionInfo]
CustomVersion=1.28
FileVersion=1.28

[Icons]
Name: "{commondesktop}\railhqGateway"; \
    Filename: "{app}\railhqSystray.exe"; \
    IconFilename: "{app}\railhqSystray.exe"; \
    IconIndex: 0; \
    WorkingDir: "{app}"; \
    Comment: "Starte dein Modelleisenbahnerlebnis mit railhq.io!"

[Registry]
; Setzt das Icon für die Deinstallationseinträge
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\railhq.io_-_Gateway"; \
  ValueType: string; ValueName: "DisplayIcon"; \
  ValueData: "{app}\railhq_icon2.ico"; Flags: uninsdeletekey
    
;[Run] 
;Filename: "cmd.exe"; \
;  Parameters: "/c start ""{app}\railhqSystray.exe"" && ""{app}\railhqGateway.exe"""; \
;  Description: "Start railhq.io Gateway?"; \
;  Flags: postinstall runascurrentuser    
;Filename: "cmd.exe"; \
;  Description: "Konfigurationswebseite öffnen?"; \
;  Flags: postinstall runascurrentuser; \
;  Parameters: "/c timeout /t 2 && start http://127.0.0.1:8090";      
 
[Run]  
Filename: "{app}\railhqSystray.exe"; Parameters: ""; Description: "Start railhq.io Systray?"; Flags: postinstall runascurrentuser nowait
Filename: "{app}\railhqGateway.exe"; Parameters: ""; Description: "Start railhq.io Gateway?"; Flags: postinstall runascurrentuser nowait
Filename: "http://127.0.0.1:8090"; Description: "Konfigurationswebseite öffnen?"; Flags: postinstall runascurrentuser shellexec  
    
[Code]
var
  SelectPage: TInputQueryWizardPage;
  SkipLoginCheckbox: TNewCheckBox;
  HostNameEdit, UserNameEdit, PasswordEdit: TEdit;
  HostNameLabel, UserNameLabel, PasswordLabel: TLabel;
  HostName, UserName, Password: string;
  StartGatewayCheckbox: TNewCheckBox;
  BackupPath: string;

procedure BackupConfig();
begin
  BackupPath := ExpandConstant('{tmp}\config_backup.json');
  if FileExists(ExpandConstant('{commonappdata}\railhqGateway\railhqGateway.json')) then
  begin
    FileCopy(ExpandConstant('{commonappdata}\railhqGateway\railhqGateway.json'), BackupPath, False);
  end;
end;
  
procedure RestoreConfig();
var
  Restored: Boolean;
begin
  Restored := False;
  if FileExists(BackupPath) then
  begin
    Restored := FileCopy(BackupPath, ExpandConstant('{commonappdata}\railhqGateway\railhqGateway.json'), False);
  end;

  if not Restored then
  begin
    MsgBox('Hinweis: Die vorherige Konfiguration konnte nicht wiederhergestellt werden. ' +
           'Bitte prüfen Sie die Einstellungen nach der Installation.', mbInformation, MB_OK);
  end;
end;
  
procedure KillProcess(const EXEName: string);
var
  ResultCode: Integer;
begin
  Exec('taskkill', '/F /IM "' + EXEName + '" /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function InitializeSetup(): Boolean;
var
  UninstallExe: string;
  ResultCode: Integer;
begin
  BackupConfig();
  KillProcess('railhqGateway.exe');
  KillProcess('railhqSystray.exe');

  UninstallExe := ExpandConstant('{commonappdata}\railhqGateway\unins000.exe');
  if FileExists(UninstallExe) then
  begin
    MsgBox('Eine bestehende Installation von railhq.io Gateway wird jetzt entfernt.', mbInformation, MB_OK);
    Exec(UninstallExe, '/VERYSILENT /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
  Result := True;
end;

procedure ToggleLoginFields(Sender: TObject);
begin
  // Falls die Checkbox aktiviert ist, deaktiviere die Eingabefelder
  HostNameEdit.Enabled := not SkipLoginCheckbox.Checked;
  UserNameEdit.Enabled := not SkipLoginCheckbox.Checked;
  PasswordEdit.Enabled := not SkipLoginCheckbox.Checked;
end;

procedure InitializeWizard;
begin
  // Erstelle die Seite mit wpSelectTasks für Host, Benutzername und Passwort
  SelectPage := CreateInputQueryPage(wpSelectTasks, 
    'Verbindungs- und Anmeldeinformationen', 
    'Bitte geben Sie die Verbindungsdetails und Ihre Anmeldedaten ein, oder überspringen Sie diesen Schritt.', 
    'Diese Daten werden zur Überprüfung an den Server gesendet.');

  // Label und Eingabefeld für Hostname
  HostNameLabel := TLabel.Create(WizardForm);
  HostNameLabel.Parent := SelectPage.Surface;
  HostNameLabel.Left := 0;
  HostNameLabel.Top := 0;
  HostNameLabel.Caption := 'Hostname (z.B. https://localhost:5001):';

  HostNameEdit := TEdit.Create(WizardForm);
  HostNameEdit.Parent := SelectPage.Surface;
  HostNameEdit.Left := 0;
  HostNameEdit.Top := HostNameLabel.Top + HostNameLabel.Height + 5; // Etwas Abstand nach dem Label
  HostNameEdit.Width := 300;
  HostNameEdit.Text := 'https://localhost:5001';  // Standardwert

  // Label und Eingabefeld für Benutzername
  UserNameLabel := TLabel.Create(WizardForm);
  UserNameLabel.Parent := SelectPage.Surface;
  UserNameLabel.Left := 0;
  UserNameLabel.Top := HostNameEdit.Top + HostNameEdit.Height + 10; // Abstand zum vorherigen Feld
  UserNameLabel.Caption := 'Benutzername:';

  UserNameEdit := TEdit.Create(WizardForm);
  UserNameEdit.Parent := SelectPage.Surface;
  UserNameEdit.Left := 0;
  UserNameEdit.Top := UserNameLabel.Top + UserNameLabel.Height + 5;
  UserNameEdit.Width := 300;

  // Label und Eingabefeld für Passwort
  PasswordLabel := TLabel.Create(WizardForm);
  PasswordLabel.Parent := SelectPage.Surface;
  PasswordLabel.Left := 0;
  PasswordLabel.Top := UserNameEdit.Top + UserNameEdit.Height + 10; // Abstand zum vorherigen Feld
  PasswordLabel.Caption := 'Passwort:';

  PasswordEdit := TEdit.Create(WizardForm);
  PasswordEdit.Parent := SelectPage.Surface;
  PasswordEdit.Left := 0;
  PasswordEdit.Top := PasswordLabel.Top + PasswordLabel.Height + 5;
  PasswordEdit.Width := 300;
  PasswordEdit.PasswordChar := '*';

  // Checkbox zum Überspringen der Anmeldung
  SkipLoginCheckbox := TNewCheckBox.Create(WizardForm);
  SkipLoginCheckbox.Parent := SelectPage.Surface;
  SkipLoginCheckbox.Top := PasswordEdit.Top + PasswordEdit.Height + 10;
  SkipLoginCheckbox.Left := 0;
  SkipLoginCheckbox.Width := SelectPage.SurfaceWidth;
  SkipLoginCheckbox.Caption := 'Anmeldung überspringen';

  // Event für das Ändern der Checkbox hinzufügen
  SkipLoginCheckbox.OnClick := @ToggleLoginFields;  
end;

function ValidateLogin(User, Pass, Host: string): Boolean;
var
  HTTP: Variant;
  Response: string;
begin
  Result := False;
  
  try
    HTTP := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    HTTP.Open('POST', Host + '/api/Auth', False);
    HTTP.SetRequestHeader('Content-Type', 'application/json');
    HTTP.Send(Format('{"username":"%s","password":"%s"}', [User, Pass]));
    
    Response := HTTP.ResponseText;

    if HTTP.Status = 200 then
    begin
      MsgBox('Login erfolgreich!', mbInformation, MB_OK);
      Result := True;
    end
    else
    begin
      MsgBox('Fehlgeschlagene Anmeldung: ' + Response, mbError, MB_OK);
    end;
  except
    MsgBox('Fehler bei der Authentifizierung!', mbError, MB_OK);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = SelectPage.ID then
  begin
    // Wenn die Checkbox gesetzt ist, überspringe die Anmeldung
    if SkipLoginCheckbox.Checked then
    begin
      //MsgBox('Anmeldung wird übersprungen.', mbInformation, MB_OK);
      Exit;
    end;

    // Werte aus den Eingabefeldern holen
    HostName := HostNameEdit.Text;
    UserName := UserNameEdit.Text;
    Password := PasswordEdit.Text;

    // Wenn Hostname, Benutzername oder Passwort leer sind, abfangen
    if (HostName = '') or (UserName = '') or (Password = '') then
    begin
      MsgBox('Bitte alle Felder ausfüllen, um fortzufahren.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    // Versuche, den Benutzer zu validieren
    if not ValidateLogin(UserName, Password, HostName) then
    begin
      Result := False; // Stoppt das Weitergehen zur Installation
    end;
  end;
end;
    
procedure SaveSettingsToFile;
var
  SetupFile, UserNameValue, PasswordValue, HostNameValue: string;
  SetupText: string;
begin
  SetupFile := ExpandConstant('{app}\railhqGateway.setup');

  // Falls der Benutzer die Anmeldung überspringt, nichts speichern
  if SkipLoginCheckbox.Checked then Exit;

  // Die Benutzereingaben extrahieren
  UserNameValue := Trim(UserNameEdit.Text);
  PasswordValue := Trim(PasswordEdit.Text);
  HostNameValue := Trim(HostNameEdit.Text);

  // Den Inhalt der Setup-Datei erstellen
  SetupText := 'username:' + UserNameValue + #13#10 +
               'password:' + PasswordValue + #13#10 +
               'host:' + HostNameValue + #13#10;

  // Die Datei speichern
  SaveStringToFile(SetupFile, SetupText, False);
end;
    
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    RestoreConfig();
    SaveSettingsToFile();
  end;
end;    
    

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
