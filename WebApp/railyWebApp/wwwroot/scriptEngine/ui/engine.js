// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

function createHqUi() {
    const components = {};

    return {
        components,

        injectDefaultStyles(options = {}) {
            if (document.getElementById('uiEngineStyles')) return; // nur einmal injizieren

            this.options = options;

            const css = this.options.customCss || `
    .data-tile {
        position: absolute;
        top: 0;
        left: 0;
        width: 128px;
        height: 64px;
        background: #f2f2f2;
        color: #333;
        padding: 6px 8px;
        border-radius: 2px;
        border: 1px solid #eee;
        box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 12px;
        box-sizing: border-box;
        z-index: 99;
        overflow: hidden;
    }

    .data-tile i {
        font-size: 16px;
        color: #7aaee3;
        flex-shrink: 0;
    }

    .data-tile .content {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
        justify-content: center;
        overflow: hidden;
        line-height: 1.1;
    }

    .data-tile .label {
        font-size: 12px;
        color: #666;
        white-space: nowrap;
        text-overflow: ellipsis;
        overflow: hidden;
        line-height: 1;
    }

    .data-tile .value {
        font-size: 14px;
        font-weight: 600;
        color: #222;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        line-height: 1.2;
    }

    .data-tile .unit {
        font-size: 9px;
        color: #999;
        line-height: 1;
    }

    .data-tile button.button-content {
        background: #7aaee3;
        color: #fff;
        border: none;
        padding: 2px 6px;
        border-radius: 3px;
        cursor: pointer;
        font-size: 11px;
        line-height: 1.25;
    }

    .data-tile button.button-content:hover {
        background: #6599cc;
    }

    .data-tile input[type=range] {
        width: 96%;
        max-width: 100px;
        margin-top: 2px;
        accent-color: #7aaee3;
        height: 16px;
    }

    .switch {
        position: relative;
        display: inline-block;
        width: 32px;
        height: 18px;
        margin-top: 2px;
        flex-shrink: 0;
    }

    .switch input {
        opacity: 0;
        width: 0;
        height: 0;
    }

    .switch .slider {
        position: absolute;
        cursor: pointer;
        background-color: #ccc;
        border-radius: 34px;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        transition: .3s;
    }

    .switch .slider:before {
        position: absolute;
        content: "";
        height: 12px;
        width: 12px;
        left: 3px;
        bottom: 3px;
        background-color: white;
        transition: .3s;
        border-radius: 50%;
        box-shadow: 0 0 1px rgba(0,0,0,0.2);
    }

    .switch input:checked + .slider {
        background-color: #7aaee3;
    }

    .switch input:checked + .slider:before {
        transform: translateX(14px);
    }

.clock-tile {
    width: 128px;
    height: 128px;
    padding: 0;
    justify-content: center;
    align-items: center;

    background-color: transparent; /* Kein Hintergrund */
    border: none;                  /* Kein Rahmen */
    box-shadow: none;              /* Kein Schatten */
}

.clock-face {
    width: 100px;
    height: 100px;
    border: 3px solid #343a40;
    border-radius: 50%;
    position: relative;
    background: white;
}

.clock-face .hand {
    position: absolute;
    bottom: 50%;
    left: 50%;
    transform-origin: bottom center;
    background-color: #343a40;
    border-radius: 2px;
    z-index: 10; /* neu: liegt über den Strichen */
}

.clock-face .hour-hand {
    width: 4px;
    height: 30px;
}

.clock-face .minute-hand {
    width: 3px;
    height: 40px;
}

.clock-face .second-hand {
    width: 2px;
    height: 45px;
    background-color: red;
}

.clock-face .second-hand::after {
    content: "";
    position: absolute;
    top: -10px; /* statt bottom */
    left: 50%;
    transform: translateX(-50%);
    width: 10px;
    height: 10px;
    background-color: red;
    border-radius: 50%;
    z-index: 11; /* noch über dem Zeiger */
}

.clock-face .tick {
    position: absolute;
    top: 48px;
    left: 48px;
    transform-origin: center center;
    background-color: #000;
    opacity: 0.6;
    z-index: 1; /* liegt hinter den Zeigern */
}

.clock-face .tick.small {
    width: 2px;
    height: 6px;
}

.clock-face .tick.large {
    width: 3px;
    height: 12px;
    opacity: 0.9;
}

.data-tile.snapshot-tile {
    position: absolute;
    width: auto;
    height: auto;
    overflow: hidden;
    background: none;
    padding: 1px 3px;
}

.snapshot-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
}

/* Overlay-Text */
.snapshot-title {
    position: absolute;
    top: 5px;
    left: 6px;
    padding: 1px 3px;
    font-size: 12px;
    font-weight: 600;
    color: #fff;
    background-color: rgba(0, 0, 0, 0.4); /* leicht transparentes Schwarz */
    border-radius: 2px;
    z-index: 2;
    pointer-events: none; /* verhindert, dass der Titel anklickbar ist */
}



.data-tile.mjpeg-tile {
    border: 1px solid #555;
    box-shadow: 0 0 4px rgba(0, 0, 0, 0.2);
    overflow: hidden;
}

.mjpeg-title {
    position: absolute;
    top: 0;
    left: 0;
    padding: 2px 6px;
    background: rgba(0, 0, 0, 0.5);
    color: #fff;
    font-size: 12px;
    font-weight: bold;
    z-index: 1;
}

.mjpeg-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
}


`;

            const style = document.createElement('style');
            style.id = 'uiEngineStyles';
            style.textContent = css;
            document.head.appendChild(style);
        },

        register(config) {
            const type = config.type || 'tile';
            let comp;

            switch (type) {
                case 'clock':
                    comp = new hqUiClock(config);
                    break;
                case 'snapshot':
                    comp = new hqUiSnapshot(config);
                    break;
                case 'mjpeg':
                    comp = new hqUiMjpeg(config);
                    break;
                case 'button':
                    comp = new hqUiButton(config);
                    break;
                case 'slider':
                    comp = new hqUiSlider(config);
                    break;
                case 'toggle':
                    comp = new hqUiToggle(config);
                    break;
                default:
                    comp = new hqUiTile(config);
            }

            this.components[config.id] = comp;
            return comp;
        },

        update(id, value) {
            const comp = this.components[id];
            if (comp && typeof comp.update === 'function') {
                comp.update(value);
            }
        },

        destroy(id) {
            const comp = this.components[id];
            if (comp) {
                comp.destroy();
                delete this.components[id];
            }
        },

        finalize() {
            for (const id in this.components) {
                this.destroy(id);
            }
            console.log('Alle Komponenten entfernt.');
        }
    };
};
