/**
 * Steuerung und Abfrage von Digitalzentralen im Skript.
 */
declare const hqControlStation: {
    /**
     * Registrierung eines Change-Listeners für Statusänderungen einer Zentrale.
     * @param driverName z. B. "ecos", "z21"
     * @param callback Callback mit dem neuen Status.
     */
    onChange(driverName: string, callback: (newState: any) => void): void;

    /**
     * Entfernt alle registrierten Event-Listener.
     */
    cleanupAll(): void;

    /**
     * Gibt eine Liste aller verfügbaren Digitalzentralen zurück.
     * @returns Array von Treibernamen, z. B. ["ecos", "z21"]
     */
    getAvailable(): Promise<string[]>;

    /**
     * Ruft Informationen zu einer bestimmten Digitalzentrale ab.
     * @param driverName Name der Zentrale
     */
    getInfo(driverName: string): Promise<CommandStationInfo>;

    /**
     * Aktiviert oder deaktiviert den Fahrstrom.
     * @param driverName Name der Zentrale
     * @param on true = Strom an, false = Strom aus
     */
    setPower(driverName: string, on: boolean): Promise<boolean>;

    /**
     * Fragt den aktuellen Fahrstromstatus ab.
     * @param driverName Name der Zentrale
     * @returns true = an, false = aus, null = unbekannt
     */
    getPowerStatus(driverName: string): Promise<boolean | null>;
};

/**
 * Detailinformationen zu einer Digitalzentrale.
 */
declare interface CommandStationInfo {
    name: string;
    manufacturer: string;
    protocol: string;
    maxCurrent: number;
    supportsProgramming: boolean;
    [key: string]: any;
}