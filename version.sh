#!/bin/bash

# Pfad zur Versionsdatei
VERSION_FILE="version.txt"

# Prüfen, ob die Versionsdatei existiert
if [ ! -f "$VERSION_FILE" ]; then
    echo "1.0" > "$VERSION_FILE"
fi

# Version aus der Datei lesen
VERSION=$(<"$VERSION_FILE")

# Teile der Version extrahieren (Major.Build)
IFS='.' read -r MAJOR BUILD <<< "$VERSION"

# Prüfen auf Parameter --major
if [[ "$1" == "--major" ]]; then
    ((MAJOR++))
    BUILD=0
else
    ((BUILD++))
fi

# Neue Version zusammensetzen
NEW_VERSION="${MAJOR}.${BUILD}"

# Neue Version in die Datei schreiben
echo "$NEW_VERSION" > "$VERSION_FILE"

echo "Neue Version: $NEW_VERSION"
