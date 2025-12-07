// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class hqUiSnapshot extends hqUiTile {
    createElement() {
        const el = document.createElement('div');
        el.className = 'data-tile snapshot-tile';

        const ratio = this.config.ratio || (4 / 3);
        const width = (this.config.width || 10) * 32; // Standardbreite: 10 Tiles
        const heightInTiles = Math.ceil((width / ratio) / 32);
        const height = heightInTiles * 32;

        el.style.width = `${width}px`;
        el.style.height = `${height}px`;

        el.innerHTML = `
    <div class="snapshot-title">${this.config.label || 'Snapshot'}</div>
    <img class="snapshot-image" src="" />
`;
        if (typeof this.config.x === 'number' && typeof this.config.y === 'number') {
            el.style.left = `${this.config.x * 32}px`;
            el.style.top = `${this.config.y * 32}px`;
        }
        el.style.position = "absolute";

        if (typeof this.config.draggable === 'boolean') {
            if (this.config.draggable === true) {
                $(el).draggable();
            }
        }

        const container = document.querySelector('#customerUi');
        container?.appendChild(el);
        this.startSnapshot(el.querySelector('.snapshot-image'));
        return el;
    }

    startSnapshot(imgEl) {
        const url = this.config.url;
        let interval = this.config.intervalMsec || 5000;
        if (typeof this.config.intervalSec === "number")
            interval = this.config.intervalSec * 1000;
        const updateImage = () => {
            imgEl.src = `${url}?t=${Date.now()}`;
        };
        updateImage(); // Initiales Bild
        this.intervalId = setInterval(updateImage, interval);
    }

    update(value) {
        // kein dynamischer Wert – Snapsot aktualisiert sich selbst
    }

    destroy() {
        super.destroy();
        clearInterval(this.intervalId);
    }
}