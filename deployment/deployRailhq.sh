#!/bin/bash
set -e  # Stoppt das Skript, wenn ein Fehler auftritt

## aktuelle Umgebung runterfahren
docker-compose down || { echo "Fehler bei docker-compose down"; exit 1; }
## [deaktiviert] ein sauberes Runterfahren entfernt die Einträge
#docker ps -a | grep railhq.io | awk '{print $1}' | xargs docker rm -f || { echo "Fehler beim Entfernen von Containern"; exit 1; }

## Image in das Docker deployn
gunzip -c latest.tar.gz | docker load || { echo "Fehler beim Laden des Docker-Images"; exit 1; }

docker-compose up -d || { echo "Fehler beim Starten mit docker-compose"; exit 1; }
