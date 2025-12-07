declare const hqHelper: {
    /**
     * Schlafe eine bestimmte Anzahl Sekunden (default 10s)
     * @param seconds Sekunden zu schlafen (default 10)
     * @param hqAbortSignal Optionales Abbruchsignal
     */
    sleepSec(seconds?: number, hqAbortSignal?: AbortSignal): Promise<void>;

    /**
     * Schlafe eine bestimmte Anzahl Millisekunden (default 100ms)
     * @param ms Millisekunden zu schlafen (default 100)
     * @param hqAbortSignal Optionales Abbruchsignal
     */
    sleepMs(ms?: number, hqAbortSignal?: AbortSignal): Promise<void>;

    /** Schlafe fest 10 Sekunden */
    sleep10Sec(hqAbortSignal?: AbortSignal): Promise<void>;

    /** Schlafe fest 5 Sekunden */
    sleep5Sec(hqAbortSignal?: AbortSignal): Promise<void>;

    /** Schlafe fest 1 Sekunde */
    sleep1Sec(hqAbortSignal?: AbortSignal): Promise<void>;

    /** Schlafe fest 100 Millisekunden */
    sleep100Ms(hqAbortSignal?: AbortSignal): Promise<void>;

    /** Schlafe fest 10 Millisekunden */
    sleep10Ms(hqAbortSignal?: AbortSignal): Promise<void>;

    /**
     * Abbrechbarer Sleep mit AbortController
     * @param ms Zeit in Millisekunden
     * @param hqAbortSignal Optionales Abbruchsignal
     */
    sleepAbortable(ms: number, hqAbortSignal?: AbortSignal): Promise<void>;

    /**
     * Gibt die aktuelle Zeit als Date-Objekt zurück
     * @returns Date
     */
    now(): Date;

    /**
     * Gibt den aktuellen Zeitstempel in Millisekunden zurück
     * @returns number
     */
    nowMs(): number;

    /**
     * Loggt mit ISO-Zeitstempel
     * @param args Beliebige Argumente für console.log
     */
    log(...args: any[]): void;

    /**
     * Erzeugt eine zufällige ganze Zahl zwischen min und max (inklusive)
     * @param min Untere Grenze (inklusive)
     * @param max Obere Grenze (inklusive)
     * @returns number Zufallszahl
     */
    randomInt(min: number, max: number): number;

    /**
     * Wiederholt eine async-Funktion bis sie erfolgreich ist oder retries erreicht
     * @template T
     * @param fn Async-Funktion, die ausgeführt werden soll
     * @param retries Anzahl der Wiederholungen (default 3)
     * @param delayMs Wartezeit zwischen Versuchen in ms (default 1000)
     * @returns Promise<T>
     */
    retry<T>(fn: () => Promise<T>, retries?: number, delayMs?: number): Promise<T>;

    /**
     * Formatiert Millisekunden in hh:mm:ss
     * @param ms Millisekunden
     * @returns string Formatierte Zeit als hh:mm:ss
     */
    formatTime(ms: number): string;

    /**
     * Beschneidet einen Wert auf den Bereich [min, max]
     * @param value Wert, der beschnitten werden soll
     * @param min Untere Grenze
     * @param max Obere Grenze
     * @returns number Beschnittener Wert
     */
    clamp(value: number, min: number, max: number): number;

    /**
     * Führt einen Fetch aus und wertet die Antwort als JSON aus
     * @param url URL zum Abrufen
     * @param options Fetch Optionen
     * @returns Promise<any> JSON-Daten
     */
    fetchJson(url: string, options?: RequestInit): Promise<any>;

    /**
     * Führt den benutzerdefinierten Cleanup-Prozess aus.
     *
     * Diese Methode wird **immer beim Beenden eines Skripts** aufgerufen und dient
     * dazu, Aufräumarbeiten durchzuführen, wie z. B. das Anhalten der Lokomotive
     * und das Setzen von Schaltartikeln in einen definierten Zustand.
     *
     * Typischerweise wird hier sichergestellt, dass alle aktiven Geräte sicher gestoppt
     * werden und keine ungewollten Aktionen mehr laufen.
     *
     * @returns Ein Promise, das auf die Beendigung des Cleanup-Prozesses wartet.
     */
    finalize: () => Promise<void>;
};
