#ifndef ReleaseDir
#define ReleaseDir "..\release-v2"
#endif

#define MyAppName "ApexTweaker"
#define MyAppVersion "2.0.1"
#define MyAppPublisher "Igor Silva"
#define MyAppExeName "ApexTweaker.exe"
#define MyAppURL "https://github.com/NGK-999/tweaker"

[Setup]
AppId={{B8E87432-68F5-4CC8-A4D7-0C79E58A2A6A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\release-installer
OutputBaseFilename=ApexTweaker-Setup
SetupIconFile=..\assets\app-icon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no
ChangesAssociations=no
DisableProgramGroupPage=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "{#ReleaseDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ApexTweaker"; Filename: "{app}\ApexTweaker.exe"
Name: "{group}\Desinstalar ApexTweaker"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ApexTweaker"; Filename: "{app}\ApexTweaker.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ApexTweaker.exe"; WorkingDir: "{app}"; Description: "Executar ApexTweaker"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    MsgBox(
      'A desinstalacao remove apenas os arquivos do ApexTweaker.' + #13#10 +
      'Tweaks de sistema aplicados anteriormente devem ser revertidos dentro do proprio app antes da remocao.',
      mbInformation,
      MB_OK);
  end;
end;
