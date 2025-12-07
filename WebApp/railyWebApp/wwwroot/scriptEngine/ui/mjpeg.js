// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class hqUiMjpeg extends hqUiTile {
    
    createElement() {
        const posterDataUrl = `data:image/svg+xml;base64,${btoa(`
<svg xmlns="http://www.w3.org/2000/svg" width="320" height="180" viewBox="0 0 320 180" style="background:#222;">
  <rect width="320" height="180" fill="#222"/>
  <g fill="none" stroke="#fff" stroke-width="6" stroke-linejoin="round" stroke-linecap="round" transform="translate(80,40)">
    <rect x="0" y="20" width="160" height="100" rx="15" ry="15"/>
    <polygon points="170,30 200,70 170,110" fill="#fff"/>
  </g>
  <text x="160" y="120" font-family="Arial, sans-serif" font-size="24" fill="#fff" text-anchor="middle" opacity="0.8">
    Kein Stream
  </text>
</svg>
`.trim())}`;

        const el = document.createElement('div');
        el.className = 'data-tile mjpeg-tile';

        const ratio = this.config.ratio || (4 / 3);
        const width = (this.config.width || 10) * 32;
        const heightInTiles = Math.floor((width / ratio) / 32);
        const height = heightInTiles * 32;

        el.style.width = `${width}px`;
        el.style.height = `${height}px`;
        el.style.position = 'absolute';
        el.style.padding = '0';
        el.style.border = '1px solid black';
        el.style.backgroundColor = 'rgba(0,0,0,0.5)';

        el.innerHTML = `
            <div class="mjpeg-title">${this.config.label || 'MJPEG Stream'}</div>
            <img class="mjpeg-image" src="" style="width:100%; height: calc(100% - 24px); object-fit: cover;" />
            <div class="error-message" style="display:none; color: red; position: absolute; top: 18px; left: 0; right: 0; text-align: center; background: rgba(0,0,0,0.8); padding: 10px; font-size: 14px; font-weight: 800;">
                Kein Stream verfügbar oder Mixed Content blockiert.
            </div>
        `;

        if (typeof this.config.x === 'number') el.style.left = `${this.config.x * 32}px`;
        if (typeof this.config.y === 'number') el.style.top = `${this.config.y * 32}px`;

        if (this.config.draggable === true) {
            $(el).draggable();
        }

        const container = document.querySelector('#customerUi');
        container?.appendChild(el);

        const imgEl = el.querySelector('.mjpeg-image');
        const errorEl = el.querySelector('.error-message');

        const poster = this.config.poster || posterDataUrl;
        const url = this.config.url;

        imgEl.src = url;

        imgEl.onerror = () => {
            // Stream konnte nicht geladen werden
            errorEl.style.display = 'block';
            if (poster) {
                imgEl.src = poster;
            } else {
                imgEl.style.display = 'none';
            }
        };

        return el;
    }

    update(value) {
        // kein dynamisches Update nötig
    }

    destroy() {
        super.destroy();
    }
}
