#!/bin/bash

set -e

APP_NAME="railhqgateway"
VERSION=$(cat ../../version.txt | tr -d '[:space:]')
OUTPUT_DIR="dist"
PUBLISH_DIR="publish"
SERVICE_FILE="railhqgateway.service"

DESCRIPTION="RailHQ Gateway Service - Version $VERSION"

RUNTIMES=("linux-x64" "linux-arm64" "linux-arm")

# Stelle sicher, dass fpm installiert ist
if ! command -v fpm &> /dev/null; then
    echo "❌ fpm ist nicht installiert. Installiere es mit: gem install --no-document fpm"
    exit 1
fi

# systemd-Service-Datei generieren
generate_service_file() {
  cat <<EOF > $SERVICE_FILE
[Unit]
Description=$DESCRIPTION
After=network.target

[Service]
ExecStart=$DOTNET_ROOT/dotnet /opt/$APP_NAME/$APP_NAME.dll
WorkingDirectory=/opt/$APP_NAME
Restart=on-failure
Environment=DOTNET_ROOT=$DOTNET_ROOT

[Install]
WantedBy=multi-user.target
EOF
}

mkdir -p "$OUTPUT_DIR"

for RUNTIME in "${RUNTIMES[@]}"; do
  ARCH=${RUNTIME##*-}

  echo "🔧 Baue für $RUNTIME..."

  rm -rf "$PUBLISH_DIR"
  dotnet publish -c Release -r "$RUNTIME" --self-contained true -o "$PUBLISH_DIR"

  generate_service_file

  #PACKAGE_NAME="${APP_NAME}_${VERSION}_${ARCH}.deb"

  # Nach dpkg-Architektur benennen
  case "$ARCH" in
    x64) FPM_ARCH="amd64" ;;
    x86) FPM_ARCH="i386" ;;
    arm) FPM_ARCH="armhf" ;;
    *) FPM_ARCH="$ARCH" ;;
  esac

  PACKAGE_NAME="${APP_NAME}_${VERSION}_${FPM_ARCH}.deb"

  fpm -s dir -t deb \
    -n "$APP_NAME" \
    -v "$VERSION" \
    -a "$FPM_ARCH" \
    --description "$DESCRIPTION" \
    --url "$URL" \
    "$PUBLISH_DIR/=/opt/$APP_NAME/" \
    "$SERVICE_FILE=/lib/systemd/system/$APP_NAME.service"

  mv "$PACKAGE_NAME" "$OUTPUT_DIR/" || mv *.deb "$OUTPUT_DIR/"
done

echo "✅ Alle Pakete wurden erfolgreich erstellt unter: $OUTPUT_DIR/"

