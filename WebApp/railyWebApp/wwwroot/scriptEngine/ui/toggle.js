// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class hqUiToggle {
    constructor(config) {
        this.id = config.id;
        this.config = config;
        this.element = this.createElement();
    }

    createElement() {
        const el = document.createElement('div');
        el.className = 'data-tile';

        el.innerHTML = `
            <i class="${this.config.icon}"></i>
            <div class="content">
                <div class="label">${this.config.label}</div>
                <label class="switch">
                    <input type="checkbox" ${this.config.value ? 'checked' : ''}>
                    <span class="slider"></span>
                </label>
            </div>
        `;

        const input = el.querySelector('input[type=checkbox]');
        input.addEventListener('mousedown', (e) => e.stopPropagation());
        input.addEventListener('change', () => {
            if (typeof this.config.onToggle === 'function') {
                this.config.onToggle(input.checked);
            }
        });

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

        document.querySelector('#customerUi')?.appendChild(el);
        return el;
    }

    update(value) {
        const input = this.element.querySelector('input[type=checkbox]');
        if (input) input.checked = !!value;
    }

    destroy() {
        this.element?.parentNode?.removeChild(this.element);
    }
}