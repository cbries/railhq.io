declare const hqApi: {
    /**
     * Konfiguriert das Verhalten des Skripts, z. B. automatischer Start bei bestimmten Events.
     *
     * @param options Konfigurationsoptionen für das Skript.
     */
    configure: (options: {
        /**
         * Gibt an, bei welchen Ereignissen das Skript automatisch gestartet werden soll.
         * Beispiel: ["layoutLoaded", "systemReady", "sensor:on:ecos:1:3"]
         */
        autoStart?: string[];

        /**
         * Optionale Beschreibung des Skripts.
         */
        description?: string;

        /**
         * Optionale Tags zur Klassifizierung oder Filterung von Skripten.
         */
        tags?: string[];
    }) => void;

};
