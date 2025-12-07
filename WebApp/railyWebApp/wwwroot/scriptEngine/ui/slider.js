// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class hqUiSlider {
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
                <input type="range" min="${this.config.min}" max="${this.config.max}" step="${this.config.step || 1}" value="${this.config.value}">
                <div class="value">${this.config.value} ${this.config.unit || ''}</div>
            </div>
        `;

        const rangeInput = el.querySelector('input[type=range]');
        const valueDisplay = el.querySelector('.value');

        rangeInput.addEventListener('input', () => {
            const val = rangeInput.value;
            valueDisplay.textContent = `${val} ${this.config.unit || ''}`;
            if (typeof this.config.onChange === 'function') {
                this.config.onChange(Number(val));
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
        const input = this.element.querySelector('input[type=range]');
        const display = this.element.querySelector('.value');
        if (input && display) {
            input.value = value;
            display.textContent = `${value} ${this.config.unit || ''}`;
        }
    }

    destroy() {
        this.element?.parentNode?.removeChild(this.element);
    }
}