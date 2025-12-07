/**
 * Sensorsteuerung im Skript.
 */
declare const hqSensors: {
    /**
     * Wartet, bis ein bestimmter Sensor den erwarteten Zustand erreicht.
     *
     * Diese Methode gibt ein Promise zurück, das aufgelöst wird, sobald der Sensor
     * den angegebenen Zustand ("on" oder "off") erreicht hat.
     *
     * @param sensorDriver Der Name oder die ID des Sensortreibers (z.B. "ecos", "dcc").
     * @param sensorPort Die Portnummer des Sensors.
     * @param sensorPin Die Pin-Nummer des Sensors.
     * @param expectedState Der erwartete Sensorzustand, entweder "on" oder "off".
     * @returns Ein Promise, das auf die Erreichung des erwarteten Sensorzustands wartet.
     */
    waitFor(
        sensorDriver: string,
        sensorPort: number,
        sensorPin: number,
        expectedState: "on" | "off"
    ): Promise<void>;

    /**
     * Registriert einen Callback, der ausgeführt wird, sobald ein bestimmter Sensor
     * den erwarteten Zustand erreicht.
     *
     * Der Callback erhält Sensordetails mit Fahrer-, Port- und Pin-Informationen sowie dem aktuellen Zustand.
     *
     * @param sensorDriver Der Name oder die ID des Sensortreibers (z.B. "ecos", "dcc").
     * @param sensorPort Die Portnummer des Sensors.
     * @param sensorPin Die Pin-Nummer des Sensors.
     * @param expectedState Der erwartete Sensorzustand, entweder "on" oder "off".
     * @param callback Funktion, die ausgeführt wird, wenn der Sensor den erwarteten Zustand erreicht.
     */
    on(
        sensorDriver: string,
        sensorPort: number,
        sensorPin: number,
        expectedState: "on" | "off",
        callback: (detail: {
            driver: string;
            port: number;
            pin: number;
            state: "on" | "off";
        }) => void | Promise<void>
    ): void;
};
