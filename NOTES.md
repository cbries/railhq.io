# Technische Notizen

Diese Datei enthält hilfreiche technische Notizen für Entwickler und fortgeschrittene Benutzer.

## Inhaltsverzeichnis

- [Default Credentials](#default-credentials)
- [GitHub Actions - Gateway Release](#github-actions---gateway-release)
- [Build Linux-Binary](#build-linux-binary)
- [Build Linux-Packages](#build-linux-packages)
- [SSL/TLS Zertifikate](#ssltls-zertifikate)
- [Docker Tipps](#docker-tipps)
- [Redis](#redis)
- [z21 Netzwerk-Routing](#z21-netzwerk-routing)
- [WebSocket Verbindung testen](#websocket-verbindung-testen)
- [Release-Checkliste](#release-checkliste)

---

## Default Credentials

```
Username: free@railhq.io
Password: railhq.io
```

---

## GitHub Actions - Gateway Release

Das Gateway wird automatisch für verschiedene Betriebssysteme gebaut, wenn ein Git-Tag gepusht wird.

### Unterstützte Plattformen

| Betriebssystem | Architektur | Datei |
|----------------|-------------|-------|
| Windows | x64 | `railhqGateway-windows-x64-*.zip` |
| Windows | ARM64 | `railhqGateway-windows-arm64-*.zip` |
| Linux | x64 | `railhqGateway-linux-x64-*.tar.gz` |
| Linux | ARM64 (RPi 4) | `railhqGateway-linux-arm64-*.tar.gz` |
| Linux | ARM (RPi 3) | `railhqGateway-linux-arm-*.tar.gz` |
| macOS | Intel | `railhqGateway-macos-x64-*.tar.gz` |
| macOS | Apple Silicon | `railhqGateway-macos-arm64-*.tar.gz` |

### Release erstellen

```bash
# Version in version.txt aktualisieren
echo "1.62" > version.txt

# Änderungen committen
git add .
git commit -m "Release v1.62"

# Tag erstellen und pushen
git tag v1.62
git push origin main --tags
```

Der GitHub Actions Workflow baut dann automatisch alle Pakete und erstellt ein GitHub Release mit allen Downloads.

### Manueller Build

Der Workflow kann auch manuell über die GitHub Actions UI gestartet werden:

1. Gehe zu **Actions** → **Build Gateway Packages**
2. Klicke auf **Run workflow**
3. Optional: Versionsnummer eingeben
4. Klicke auf **Run workflow**

Die fertigen Artefakte stehen dann als Download bereit.

---

## Build Linux-Binary

### Voraussetzungen (Fedora)

```bash
sudo dnf install ruby ruby-devel gcc make rpm-build -y
sudo gem install --no-document fpm
```

### Voraussetzungen (Debian/Ubuntu)

```bash
sudo apt update
sudo apt install ruby ruby-dev build-essential
sudo gem install --no-document fpm
```

### Build

Im Projektordner `Gateway/railyGateway`:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
```

### Gateway-Konfiguration

Die Konfigurationsdatei `railhqGateway.json` muss im Home-Verzeichnis (`~`) bereitgestellt werden. Bei einem lokalen Build kann ein symbolischer Link gesetzt werden:

```bash
ln -s /pfad/zum/projekt/Gateway/railyGateway/bin/Release/net8.0/linux-x64/railhqGateway.json ~/railhqGateway.json
```

---

## Build Linux-Packages

### DEB-Package erstellen

```bash
fpm -s dir -t deb \
  -n railhqGateway \
  -v $(cat version.txt) \
  --prefix /opt/railhqGateway \
  ./publish/=/opt/railhqGateway/
```

### RPM-Package erstellen

```bash
fpm -s dir -t rpm \
  -n railhqGateway \
  -v $(cat version.txt) \
  --prefix /opt/railhqGateway \
  ./publish/=/opt/railhqGateway/
```

### Mit Build-Skript (Raspberry Pi)

```bash
cd Gateway/railyGateway
./build-and-package.sh
```

Die Pakete landen in `./dist` und können installiert werden:

```bash
sudo dpkg -i dist/railhqgateway_*.deb
```

---

## SSL/TLS Zertifikate

### Entwickler-Zertifikat erstellen

> **Hinweis:** Selbst-signierte Zertifikate werden von Browsern als unsicher eingestuft und eignen sich nur für lokale Tests.

```bash
# Zertifikat erstellen
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout localhost.key \
  -out localhost.crt

# Falls .pfx benötigt wird (für ASP.NET)
openssl pkcs12 -export -out localhost.pfx \
  -inkey localhost.key \
  -in localhost.crt

# PEM-Datei erstellen (für Node.js)
cat localhost.crt localhost.key > localhost.pem
```

### PFX aus PEM erstellen

```bash
openssl pkcs12 -export \
  -out certificate.pfx \
  -inkey privkey.pem \
  -in fullchain.pem
```

### Key und CRT aus PFX extrahieren

```bash
# Key extrahieren
openssl pkcs12 -in certificate.pfx -nocerts -out certificate.key

# Passwort vom Key entfernen
openssl ec -in certificate.key -out certificate-nopass.key

# CRT extrahieren
openssl pkcs12 -in certificate.pfx -clcerts -nokeys -out certificate.crt
```

### Zertifikat-Gültigkeit prüfen

```bash
openssl x509 -in cert.pem -noout -dates
```

---

## Docker Tipps

### Image mit Shell starten (Debugging)

```bash
docker run --rm -it --entrypoint bash railhq.io
```

### Host-IP im Container

```bash
# Im Container ausführen
ping host.docker.internal
```

### Host-Redirect für lokale Entwicklung

**Windows:**
```bash
docker run --add-host=railhq.io:host.docker.internal ...
```

**Linux:**
```bash
docker run --add-host=railhq.io:$(ip route | awk '/default/ {print $3}') ...
```

### WSL2 Memory-Limit setzen

Datei `~/.wslconfig` oder `C:\Users\<username>\.wslconfig`:

```ini
[wsl2]
memory=2GB
processors=2

[experimental]
autoMemoryReclaim=gradual
```

### Hosts-Datei für lokale Tests

**Windows:** `C:\Windows\System32\drivers\etc\hosts`
**Linux/Mac:** `/etc/hosts`

```
127.0.0.1 railhq.io
127.0.0.1 www.railhq.io
127.0.0.1 admin.railhq.io
```

---

## Redis

### Lokaler Start mit Docker

```bash
docker run --name redis-server -d -p 6379:6379 redis
```

### Redis CLI verbinden

```bash
docker exec -it redis-server redis-cli
```

### Installation auf Linux

```bash
sudo apt update
sudo apt install redis-server
sudo systemctl enable redis-server
sudo systemctl start redis-server
```

---

## z21 Netzwerk-Routing

Falls die z21-Zentrale in einem anderen Netzwerk-Segment liegt:

### Route anzeigen (Windows)

```cmd
route print
```

### Route hinzufügen (Windows)

```cmd
route add 192.168.0.111 MASK 255.255.255.0 192.168.0.1 IF 22 -p
```

---

## WebSocket Verbindung testen

### Mit wscat

```bash
npm install -g wscat
wscat -c ws://localhost:5001/ws/browser
```

### Mit Node.js

```javascript
const WebSocket = require('ws');
const fs = require('fs');

const ws = new WebSocket('wss://localhost:7226/ws', {
    // Bei selbst-signiertem Zertifikat:
    ca: fs.readFileSync('certificate/localhost.pem')
});

ws.on('open', () => console.log('Connected!'));
ws.on('error', (err) => console.error('Error:', err));
ws.on('message', (data) => console.log('Message:', data.toString()));
```

---

## Release-Checkliste

### System & Technik

- [ ] Volumes säubern
- [ ] Testaccounts löschen
- [ ] Fehlermeldungen benutzerfreundlich?
- [ ] Backup einrichten (tägliches Backup)
- [ ] Monitoring aktivieren (CPU, RAM, Uptime)
- [ ] SSL/TLS überprüfen – läuft alles über HTTPS?

### Release-Vorbereitung

- [ ] Versionsnummer setzen
- [ ] Changelog erstellen
- [ ] Releasenachricht verfassen
- [ ] Setup bauen und bereitstellen

### Dokumentation

- [ ] Dokumentation mit Startinfos bereitstellen
- [ ] FAQ-Bereich befüllen
- [ ] Optional: Einstiegsvideo oder Screenshots

### Benutzer & Onboarding

- [ ] E-Mail-System testen (Willkommensmail, Passwort vergessen)
- [ ] Erster Login: Funktionstest für neuen Nutzer
- [ ] Demo-Daten oder leerer Start definieren

### Kommunikation

- [ ] Release-Post veröffentlichen
- [ ] Support-Kanal nennen (E-Mail, Discord, GitHub Issues)
- [ ] Feedback-System anbieten

### Optional

- [ ] Feature-Flags konfiguriert?
- [ ] Entwicklung von Produktion getrennt?
- [ ] Logs durchsuchbar? Fehlerreporting integriert?

---

## Dokumentation bauen

Die Dokumentation basiert auf Docusaurus und befindet sich im `WebDoc/`-Verzeichnis:

```bash
cd WebDoc
npm install
npm run build-doc
```

Die generierte Dokumentation landet in `WebApp/railyWebIndex/wwwroot/documentation/`.
