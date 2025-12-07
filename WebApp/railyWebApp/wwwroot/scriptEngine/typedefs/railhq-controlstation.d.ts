/**
 * Steuerung und Statusabfrage von Digitalzentralen im Skript.
 */
declare const hqControlStation: {
    /**
     * Registrierung eines Change-Listeners für eine bestimmte Zentrale.
     * @param driverName Der Name des verwendeten Treibers (z. B. "ecos")
     * @param callback Callback-Funktion, die bei Statusänderungen aufgerufen wird.
     */
    onChange(driverName: string, callback: (newState: any) => void): void;

    /**
     * Entfernt alle registrierten Event-Listener.
     */
    cleanupAll(): void;

    /**
     * Ruft alle verfügbaren Digitalzentralen (Treibermodule) ab.
     * @returns Liste von Treibernamen (z. B. ["ecos", "z21"])
     */
    getAvailable(): Promise<string[]>;

    /**
     * Ruft Detailinformationen zu einer bestimmten Digitalzentrale ab.
     * @param driverName Der Name des verwendeten Treibers (z. B. "ecos")
     * @returns Informationen zur Zentrale wie Name, Version usw.
     */
    getInfo(driverName: string): Promise<{
        name: string;
        version: string;
        manufacturer?: string;
        [key: string]: any;
    }>;

    /**
     * Setzt den Fahrstrom einer bestimmten Digitalzentrale ein oder aus.
     * @param driverName Der Name des verwendeten Treibers
     * @param on true = ein, false = aus
     * @returns true bei Erfolg, sonst Fehler
     */
    setPower(driverName: string, on: boolean): Promise<boolean>;

    /**
     * Ruft den aktuellen Fahrstromstatus einer bestimmten Zentrale ab.
     * @param driverName Der Name des verwendeten Treibers
     * @returns true = an, false = aus, null = unbekannt
     */
    getPowerStatus(driverName: string): Promise<boolean | null>;
};
