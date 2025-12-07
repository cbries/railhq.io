#!/bin/bash
set -e  # Stoppt das Skript, wenn ein Fehler auftritt

docker-compose run restoreResources
docker-compose run restoreSetups
