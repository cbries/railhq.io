#!/bin/bash
#
# Lokales Build-Skript für alle Gateway-Varianten
# Entspricht dem GitHub Actions Workflow build-gateway.yml
#
# Voraussetzungen:
#   - .NET 8.0 SDK installiert (dotnet --version)
#   - Ausreichend Speicherplatz (~500MB pro Variante)
#
# Verwendung:
#   ./build-gateway-local.sh              # Alle Varianten bauen
#   ./build-gateway-local.sh linux-x64    # Nur eine Variante bauen
#   ./build-gateway-local.sh --list       # Verfügbare Varianten auflisten
#   ./build-gateway-local.sh --clean      # Alte Builds löschen
#

# NICHT set -e verwenden - wir wollen alle Varianten bauen auch wenn eine fehlschlägt

# Konfiguration
DOTNET_VERSION="8.0"
BUILD_CONFIGURATION="Release"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/publish-local"
VERSION_FILE="$SCRIPT_DIR/version.txt"

# Farben für Ausgabe
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Alle unterstützten Runtimes (wie in build-gateway.yml)
# Liste der Runtimes in fester Reihenfolge
ALL_RUNTIMES=(
    "win-x64"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "linux-arm"
    "osx-x64"
    "osx-arm64"
)

# Archiv-Typ pro Runtime
declare -A ARCHIVE_TYPE
ARCHIVE_TYPE=(
    ["win-x64"]="zip"
    ["win-arm64"]="zip"
    ["linux-x64"]="tar.gz"
    ["linux-arm64"]="tar.gz"
    ["linux-arm"]="tar.gz"
    ["osx-x64"]="tar.gz"
    ["osx-arm64"]="tar.gz"
)

# Hilfsfunktionen
print_header() {
    echo ""
    echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

# Version ermitteln
get_version() {
    if [ -f "$VERSION_FILE" ]; then
        cat "$VERSION_FILE" | tr -d '[:space:]'
    else
        echo "0.0.0"
    fi
}

# .NET SDK prüfen
check_dotnet() {
    print_header "Prüfe Voraussetzungen"
    
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK nicht gefunden!"
        echo ""
        echo "Installation:"
        echo "  Ubuntu/Debian: sudo apt-get install dotnet-sdk-8.0"
        echo "  Fedora:        sudo dnf install dotnet-sdk-8.0"
        echo "  macOS:         brew install dotnet-sdk"
        echo "  Windows:       winget install Microsoft.DotNet.SDK.8"
        echo ""
        echo "Oder besuche: https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version)
    print_success ".NET SDK gefunden: $dotnet_version"
    
    # Prüfe ob Version >= 8.0
    if [[ ! "$dotnet_version" =~ ^8\. ]]; then
        print_warning "Empfohlen: .NET 8.0 SDK (aktuell: $dotnet_version)"
    fi
}

# Verfügbare Varianten auflisten
list_variants() {
    print_header "Verfügbare Build-Varianten"
    
    echo "Runtime          | Archiv-Typ | Beschreibung"
    echo "-----------------|------------|---------------------------"
    echo "win-x64          | zip        | Windows x64"
    echo "win-arm64        | zip        | Windows ARM64"
    echo "linux-x64        | tar.gz     | Linux x64"
    echo "linux-arm64      | tar.gz     | Linux ARM64 (Raspberry Pi 4)"
    echo "linux-arm        | tar.gz     | Linux ARM (Raspberry Pi 3)"
    echo "osx-x64          | tar.gz     | macOS Intel"
    echo "osx-arm64        | tar.gz     | macOS Apple Silicon"
    echo ""
    echo "Verwendung:"
    echo "  $0                    # Alle Varianten bauen"
    echo "  $0 linux-x64          # Nur Linux x64 bauen"
    echo "  $0 linux-x64 osx-arm64 # Mehrere Varianten bauen"
}

# Alte Builds löschen
clean_builds() {
    print_header "Lösche alte Builds"
    
    if [ -d "$OUTPUT_DIR" ]; then
        rm -rf "$OUTPUT_DIR"
        print_success "Verzeichnis gelöscht: $OUTPUT_DIR"
    else
        print_info "Kein Build-Verzeichnis vorhanden"
    fi
}

# Eine Variante bauen
build_variant() {
    local runtime=$1
    local archive_ext=${ARCHIVE_TYPE[$runtime]}
    local version=$(get_version)
    local artifact_name="railhqGateway-${runtime//-/-}"
    
    # Runtime zu artifact_name konvertieren (z.B. win-x64 -> windows-x64)
    case "$runtime" in
        win-*)
            artifact_name="railhqGateway-windows-${runtime#win-}"
            ;;
        osx-*)
            artifact_name="railhqGateway-macos-${runtime#osx-}"
            ;;
        *)
            artifact_name="railhqGateway-$runtime"
            ;;
    esac
    
    local publish_path="$OUTPUT_DIR/$artifact_name"
    local archive_name="$artifact_name-$version.$archive_ext"
    
    echo ""
    print_info "Baue: $runtime -> $artifact_name"
    echo ""
    
    # Restore
    echo "  [1/4] Restore dependencies..."
    if ! dotnet restore "$SCRIPT_DIR/Gateway/railyGateway/railyGateway.csproj" --verbosity quiet; then
        print_error "Restore fehlgeschlagen für $runtime"
        return 1
    fi
    
    # Build/Publish
    echo "  [2/4] Publish für $runtime..."
    if ! dotnet publish "$SCRIPT_DIR/Gateway/railyGateway/railyGateway.csproj" \
        --configuration "$BUILD_CONFIGURATION" \
        --runtime "$runtime" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -o "$publish_path" \
        --verbosity quiet; then
        print_error "Publish fehlgeschlagen für $runtime"
        return 1
    fi
    
    # Konfigurationsdateien kopieren
    echo "  [3/4] Kopiere Konfigurationsdateien..."
    cp "$SCRIPT_DIR/Gateway/railyGateway/railhqGateway.json" "$publish_path/" 2>/dev/null || true
    cp "$SCRIPT_DIR/Gateway/railyGateway/log4net.config" "$publish_path/" 2>/dev/null || true
    
    # Archiv erstellen
    echo "  [4/4] Erstelle Archiv..."
    pushd "$OUTPUT_DIR" > /dev/null
    
    if [ "$archive_ext" == "zip" ]; then
        # Zip für Windows
        if command -v zip &> /dev/null; then
            zip -rq "$archive_name" "$artifact_name" || true
        else
            print_warning "zip nicht installiert - überspringe Archiv-Erstellung"
        fi
    else
        # tar.gz für Linux/macOS
        tar -czf "$archive_name" "$artifact_name" || true
    fi
    
    popd > /dev/null
    
    print_success "Fertig: $archive_name"
    return 0
}

# Hauptprogramm
main() {
    local start_time=$(date +%s)
    
    # Argumente verarbeiten
    case "${1:-}" in
        --list|-l)
            list_variants
            exit 0
            ;;
        --clean|-c)
            clean_builds
            exit 0
            ;;
        --help|-h)
            echo "Lokales Build-Skript für railhq.io Gateway"
            echo ""
            echo "Verwendung: $0 [OPTIONEN] [RUNTIME...]"
            echo ""
            echo "Optionen:"
            echo "  --list, -l     Verfügbare Varianten auflisten"
            echo "  --clean, -c    Alte Builds löschen"
            echo "  --help, -h     Diese Hilfe anzeigen"
            echo ""
            echo "Beispiele:"
            echo "  $0                       # Alle Varianten bauen"
            echo "  $0 linux-x64             # Nur Linux x64"
            echo "  $0 linux-x64 linux-arm64 # Linux x64 und ARM64"
            exit 0
            ;;
    esac
    
    print_header "railhq.io Gateway - Lokaler Build"
    
    local version=$(get_version)
    echo "Version:         $version"
    echo "Konfiguration:   $BUILD_CONFIGURATION"
    echo "Ausgabe:         $OUTPUT_DIR"
    echo ""
    
    # Voraussetzungen prüfen
    check_dotnet
    
    # Ausgabeverzeichnis erstellen
    mkdir -p "$OUTPUT_DIR"
    
    # Zu bauende Varianten bestimmen
    local variants_to_build=()
    
    if [ $# -eq 0 ]; then
        # Alle Varianten aus der Liste
        variants_to_build=("${ALL_RUNTIMES[@]}")
    else
        # Nur angegebene Varianten
        for arg in "$@"; do
            if [ -n "${ARCHIVE_TYPE[$arg]:-}" ]; then
                variants_to_build+=("$arg")
            else
                print_error "Unbekannte Runtime: $arg"
                echo "Verfügbare Runtimes: ${ALL_RUNTIMES[*]}"
                exit 1
            fi
        done
    fi
    
    print_header "Baue ${#variants_to_build[@]} Variante(n)"
    
    local built_count=0
    local failed_count=0
    
    for runtime in "${variants_to_build[@]}"; do
        if build_variant "$runtime"; then
            ((built_count++))
        else
            ((failed_count++))
            print_error "Build fehlgeschlagen: $runtime"
        fi
    done
    
    # Zusammenfassung
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    print_header "Build abgeschlossen"
    
    echo "Ergebnis:"
    echo "  Erfolgreich: $built_count"
    echo "  Fehlgeschlagen: $failed_count"
    echo "  Dauer: ${duration}s"
    echo ""
    
    if [ -d "$OUTPUT_DIR" ]; then
        echo "Erstellte Dateien:"
        ls -lh "$OUTPUT_DIR"/*.{zip,tar.gz} 2>/dev/null || true
        echo ""
        echo "Verzeichnisse:"
        ls -d "$OUTPUT_DIR"/railhq* 2>/dev/null || true
    fi
    
    if [ $failed_count -gt 0 ]; then
        exit 1
    fi
}

# Skript ausführen
main "$@"
