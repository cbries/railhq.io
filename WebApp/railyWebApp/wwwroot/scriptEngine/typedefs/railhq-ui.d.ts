declare namespace hqUi {

    /**
     * @typedef {Object} Konfiguration für eine UI-Komponente (Tile).
     * @property {string} id - Eindeutige ID der Komponente
     * @property {'tile'|'clock'|'snapshot'|'mjpeg'|'button'|'slider'|'toggle'} [type] - Typ der Komponente
     * @property {number} [x] - X-Position im Grid (in Einheiten à 32px)
     * @property {number} [y] - Y-Position im Grid (in Einheiten à 32px)
     * @property {boolean} [draggable] - Erlaubt das Verschieben mit der Maus
     * @property {string} [icon] - Optionales Icon (z. B. FontAwesome-Klasse)
     * @property {string} [label] - Titel oder Beschriftung
     * @property {string} [unit] - Maßeinheit (z. B. %, °C)
     * @property {string} [value] - Anfangswert oder Text
     * 
     * @property {string} [url] - Bildquelle (nur für Webcam)
     * @property {number} [ratio] - Seitenverhältnis (z. B. 16/9)
     * @property {number} [width] - Breite in Gleisplan-Kachelanzahl
     * @property {number} [height] - Höhe in Gleisplan-Kachelanzahl
     * @property {number} [intervalMsec] - Bildaktualisierung in Millisekunden
     * @property {number} [intervalSec] - Alternative Angabe in Sekunden
     */
    interface TileConfig {
        id: string;
        type: string;

        x?: number;
        y?: number;
        draggable?: boolean;

        icon?: string;
        label?: string;
        unit?: string;
        value?: string;
        
        url?: string; // snapshot | mjpeg
        ratio?: number; // snapshot | mjpeg
        width?: number; // snapshot | mjpeg
        height?: number; // snapshot | mjpeg
        intervalMsec?: number; // snapshot
        intervalSec?: number; // snapshot

        poster?: string; // mjpeg
    }

    /**
     * Basisklasse für UI-Komponenten (Tiles).
     */
    class Tile {
        id: string;
        config: TileConfig;
        element: HTMLElement;

        constructor(config: TileConfig);

        createElement(): HTMLElement;

        update(value: string): void;

        destroy(): void;
    }

    /**
     * UI-Engine für das Erstellen, Verwalten und Zerstören von UI-Komponenten.
     */
    const components: Record<string, Tile>;
    function injectDefaultStyles(options?: { customCss?: string }): void;
    function register(config: TileConfig): Tile;
    function update(id: string, value: string): void;
    function destroy(id: string): void;
    function finalize(): void;
}