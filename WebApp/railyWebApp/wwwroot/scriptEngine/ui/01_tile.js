// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// UI-Komponenten-Basisklasse: Tile
class hqUiTile {
    constructor(config) {
        this.id = config.id;
        this.config = config;
        this.element = this.createElement();
        this.update(config.value || '--');
    }

    createElement() {
        const el = document.createElement('div');
        el.className = 'data-tile';
        el.innerHTML = `
            <i class="${this.config.icon}"></i>
            <div class="content">
                <div class="label">${this.config.label}</div>
                <div class="value">--</div>
                <div class="unit">${this.config.unit}</div>
            </div>
        `;

        // 💡 Position anhand x/y-Koordinaten setzen (je 32px pro Einheit)
        if (typeof this.config.x === 'number' && typeof this.config.y === 'number') {
            el.style.left = `${this.config.x * 32}px`;
            el.style.top = `${this.config.y * 32}px`;
        }

        if (typeof this.config.draggable === 'boolean') {
            if (this.config.draggable === true) {
                $(el).draggable();
            }
        }
        
        const container = document.querySelector('#customerUi');
        container?.appendChild(el);
        return el;
    }

    update(value) {
        this.element.querySelector('.value').textContent = value;
    }

    destroy() {
        if (this.element?.parentNode) {
            this.element.parentNode.removeChild(this.element);
        }
    }
}