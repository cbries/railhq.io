/**
 * Steuerung einer Lokomotive im Skript.
 */
declare const hqLocomotive: {
    /**
     * Setzt die Geschwindigkeit der Lok.
     * @param driverName z.B. "ecos", "z21"
     * @param address Lokadresse (z.B. 1010)
     * @param speed Geschwindigkeit von -100 bis +100
     */
    setSpeed(driverName: string, address: number, speed: number): Promise<void>;

    /**
     * Setzt die Fahrtrichtung der Lok.
     * @param driverName z.B. "ecos"
     * @param address Lokadresse
     * @param forward true = vorwärts, false = rückwärts
     */
    setDirection(driverName: string, address: number, forward: boolean): Promise<void>;

    /**
     * Setzt Fahrtrichtung und Geschwindigkeit nacheinander.
     * Erst wird die Richtung gesetzt, dann gewartet, bis sie intern übernommen wurde,
     * und anschließend wird die Geschwindigkeit gesetzt.
     *
     * @param driverName Der Name des verwendeten Treibers (z. B. "z21")
     * @param address Die Lokadresse
     * @param forward Gibt an, ob die Lok vorwärts fahren soll
     * @param speed Die Geschwindigkeit (z. B. 25)
     */
    setDirectionAndSpeed(
        driverName: string,
        address: number,
        forward: boolean,
        speed: number
    ): Promise<void>;

    /**
     * Stoppt die Lok sofort.
     */
    stop(driverName: string, address: number): Promise<void>;

    /**
     * Schaltet eine Funktion der Lok (z. B. Licht, Sound).
     * @param driverName z.B. "ecos"
     * @param address Lokadresse
     * @param functionNumber Funktion (z. B. 0 für Licht, 1 für Sound)
     * @param active true = ein, false = aus
     */
    setFunction(driverName: string, address: number, functionNumber: number, active: boolean): Promise<void>;

    /**
     * Liefert den aktuellen Status der Lok.
     */
    getStatus(driverName: string, address: number): Promise<LocomotiveStatus>;
};