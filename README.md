# Creator Hub Live

Windows-Steuerungsoberfläche für OBS Studio mit Virtual Camera und paralleler Weiterleitung an Twitch, YouTube und TikTok über FFmpeg.

## Funktionen

- OBS Studio erkennen und starten
- Verbindung mit OBS WebSocket 5.x
- OBS-Stream und Virtual Camera mit einem Button starten/stoppen
- mehrere RTMP-/RTMPS-Ziele gleichzeitig
- getrennte Server-URL und Stream-Key je Plattform
- verschlüsselte Speicherung unter dem aktuellen Windows-Benutzerkonto
- Status- und Fehlerprotokoll
- automatischer Windows-x64-Build über GitHub Actions

## Technischer Aufbau

OBS sendet seinen Hauptstream an einen lokalen RTMP-Relay-Eingang. Creator Hub Live startet je aktivierter Plattform einen FFmpeg-Prozess, der denselben Eingang ohne erneutes Encoding an das Ziel weitergibt.

```text
OBS Studio
  -> lokaler RTMP-Eingang
     -> FFmpeg -> Twitch
     -> FFmpeg -> YouTube
     -> FFmpeg -> TikTok
```

## Voraussetzungen

- Windows 10 oder Windows 11
- OBS Studio 30 oder neuer
- OBS WebSocket aktiviert, Standard-Port 4455
- FFmpeg im Programmordner oder als vollständiger Pfad eingetragen
- lokaler RTMP-Server/Relay-Eingang, zum Beispiel nginx-rtmp oder MediaMTX
- offizieller Stream-Key der jeweiligen Plattform

Creator Hub Live umgeht keine Plattformfreigaben. TikTok funktioniert nur, wenn das Konto einen offiziellen RTMP-Server und Stream-Key erhalten hat.

## Lokal bauen

```cmd
dotnet restore CreatorHubLive.csproj
dotnet publish CreatorHubLive.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Die EXE liegt anschließend unter:

```text
bin\Release\net8.0-windows\win-x64\publish\CreatorHubLive.exe
```

## OBS konfigurieren

1. OBS öffnen.
2. Werkzeuge -> WebSocket-Servereinstellungen.
3. WebSocket aktivieren und Passwort setzen.
4. Unter Einstellungen -> Stream einen benutzerdefinierten RTMP-Dienst verwenden.
5. Als Server den lokalen Relay-Server eintragen, zum Beispiel `rtmp://127.0.0.1:1935/live`.
6. Als Stream-Key `creatorhub` eintragen.
7. In Creator Hub Live als Relay-Eingang `rtmp://127.0.0.1:1935/live/creatorhub` eintragen.

## Sicherheit

Stream-Keys und OBS-Passwort werden mit Windows DPAPI für den aktuellen Benutzer verschlüsselt gespeichert. Sie werden nicht in GitHub oder im Quellcode abgelegt.
