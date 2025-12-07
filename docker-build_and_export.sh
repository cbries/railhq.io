#!/bin/sh

./version.sh

VERSION=$(tr -d ' ' < version.txt)

cd WebDoc
yarn install
npm run build-docs
cd ..

sudo docker compose build railhq

# Prüfe, ob der Parameter "--save" übergeben wurde
if [ "$1" = "--save" ]; then
    #
    # Backup vom railhq.io Systemabbild
    #
    sudo docker save railhq.io:latest | gzip > "deployment/railhq.io-$VERSION.tar.gz"
    ls -alh "deployment/railhq.io-$VERSION.tar.gz"
    cp -v -f "deployment/railhq.io-$VERSION.tar.gz" "deployment/latest.tar.gz"
    echo "Docker-Image wurde als railhq.io-$VERSION.tar.gz gespeichert!"

    #
    # Backup vom /app/resources Ordner
    #
    #docker compose run --rm backup   
    #(cd deployment && cp -v -f "$(ls -t railhq.io-app*.tar.gz | head -n 1)" latestResources.tar.gz)

else
    echo "Docker-Image wurde gebaut, aber nicht gespeichert. Fuehre das Skript mit '--save' aus, um es zu exportieren."
fi

# 
# Wieder entpacken mit:
# $ gunzip -c railhq.io.tar.gz | docker load
#
