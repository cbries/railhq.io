/**
 * MQTT-Client-Unterstützung im Skript.
 */
declare const hqMqttClient: {
    /**
     * Stellt eine Verbindung zum MQTT-Broker her.
     *
     * @param url Die URL des MQTT-Brokers (z. B. "wss://broker.example.com:8083/mqtt").
     * @returns Ein Promise, das aufgelöst wird, sobald die Verbindung hergestellt ist.
     */
    connect(url: string): Promise<void>;

    /**
     * Trennt die Verbindung zum MQTT-Broker.
     *
     * @returns Ein Promise, das aufgelöst wird, sobald die Verbindung getrennt wurde.
     */
    disconnect(): Promise<void>;

    /**
     * Veröffentlicht eine Nachricht auf einem MQTT-Topic.
     *
     * @param topic Das MQTT-Topic, auf dem die Nachricht gesendet wird.
     * @param payload Der Nachrichtentext (als String).
     */
    publish(topic: string, payload: string): void;

    /**
     * Verbindet sich temporär zum Broker, sendet eine Nachricht und trennt direkt wieder die Verbindung.
     *
     * Ideal für einfache Event-Trigger oder Einmal-Kommandos.
     *
     * @param url Die URL des MQTT-Brokers (z. B. "wss://broker.example.com:8083/mqtt").
     * @param topic Das Ziel-Topic.
     * @param payload Der Nachrichtentext (als String).
     * @returns Ein Promise, das aufgelöst wird, wenn der Ablauf abgeschlossen ist.
     */
    publishOnce(
        url: string,
        topic: string,
        payload: string
    ): Promise<void>;

    /**
     * Abonniert ein MQTT-Topic und registriert einen Callback, der bei Nachrichten ausgelöst wird.
     *
     * Wildcards wie `+` und `#` werden unterstützt.
     *
     * @param topicPattern Das zu abonnierende Topic-Pattern.
     * @param callback Funktion, die bei jeder empfangenen Nachricht aufgerufen wird.
     */
    subscribe(
        topicPattern: string,
        callback: (topic: string, payload: string) => void
    ): void;

    /**
     * Entfernt alle registrierten MQTT-Abonnements und trennt die Verbindung zum Broker.
     */
    cleanupAll(): void;

    /**
     * Prüft, ob ein MQTT-Topic mit einem gegebenen Pattern übereinstimmt.
     *
     * Unterstützt MQTT-Wildcards wie `+` (ein Segment) und `#` (mehrere Segmente).
     *
     * @param pattern Das MQTT-Topic-Pattern.
     * @param topic Das empfangene Topic.
     * @returns `true`, wenn das Topic mit dem Pattern übereinstimmt, andernfalls `false`.
     */
    matchesTopic(pattern: string, topic: string): boolean;
};