//
// Implementierungen der Typen/Callbacks die mit den Typedefinitionen geladen wurden.
//
window.hqControlStation = {
    __registeredControlStationEvents: [],

    /**
     * Registrierung eines Change-Listeners für eine Digitalzentrale.
     * @param {string} driverName - Eindeutige ID der Digitalzentrale (z.B. "demo" oder "z21").
     * @param {(newState: any) => void} callback - Callback mit dem neuen Status.
     */
    onChange: function (driverName, callback) {
        const handler = (e) => {
            if (e.detail.driverName !== driverName) return;
            
            callback(e.detail.data);
        };
        controlStationEvents.addEventListener('controlStationChange', handler);
        this.__registeredControlStationEvents.push({
            event: 'controlStationChange',
            handler
        });
    },

    /**
     * Entfernt alle registrierten ControlStation-Event-Listener.
     */
    cleanupAll: function () {
        for (const { evId, handler } of this.__registeredControlStationEvents) {
            controlStationEvents.removeEventListener(evId, handler);
        }
        this.__registeredControlStationEvents = [];
        console.log("[hqControlStation] Alle Event-Listener entfernt.");
    },

    /**
     * Ruft alle verfügbaren Digitalzentralen ab.
     * GET /api/automation/station/available
     * @returns {Promise<string[]>}
     */
    getAvailable: function () {
        return fetch(`${apiBaseControlStation}/available`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    },

    /**
     * Ruft Detailinfos zu einer bestimmten Digitalzentrale ab.
     * GET /api/automation/station/info/{driverName}
     * @param {string} driverName
     * @returns {Promise<CommandStationInfo>}
     */
    getInfo: function (driverName) {
        return fetch(`${apiBaseControlStation}/info/${encodeURIComponent(driverName)}`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    },

    /**
     * Setzt den Fahrstrom ein-/aus für eine Zentrale.
     * POST /api/automation/station/power/{driverName}/{on}
     * @param {string} driverName
     * @param {boolean} on
     * @returns {Promise<boolean>}
     */
    setPower: function (driverName, on) {
        return fetch(`${apiBaseControlStation}/power/${encodeURIComponent(driverName)}/${on}`, {
            method: 'POST'
        }).then(response => {
            if (!response.ok) throw new Error(`Error ${response.status}`);
            return response.json();
        });
    },

    /**
     * Ruft den aktuellen Power-Status einer Zentrale ab.
     * GET /api/automation/station/power/status/{driverName}
     * @param {string} driverName
     * @returns {Promise<boolean|null>} true = an, false = aus, null = unbekannt
     */
    getPowerStatus: function (driverName) {
        return fetch(`${apiBaseControlStation}/power/status/${encodeURIComponent(driverName)}`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    }
};

window.hqLocomotive = {
    __registeredLocomotiveEvents: [],

    /**
     * Registrierung eines Change-Listeners für Schaltartikel.
     * @param {string} switchId - Eindeutige ID des Schaltartikels (z.B. "weiche-12" oder "signal-3").
     * @param {(newState: any) => void} callback - Callback mit dem neuen Status.
     */
    onChange: function (driverName, address, callback) {
        const handler = (e) => {
            if (e.detail.driverName !== driverName) return;
            if (Number(e.detail.address) !== Number(address)) return;

            callback(e.detail.data);
        };
        locomotiveEvents.addEventListener('locomotiveChange', handler);
        this.__registeredLocomotiveEvents.push({
            event: 'locomotiveChange',
            handler
        });
    },

    /**
     * Entfernt alle registrierten Locomotive-Event-Listener.
     */
    cleanupAll: function () {
        for (const { evId, handler } of this.__registeredLocomotiveEvents) {
            locomotiveEvents.removeEventListener(evId, handler);
        }
        this.__registeredLocomotiveEvents = [];
        console.log("[hqLocomotive] Alle Event-Listener entfernt.");
    },

    getAllLocomotives: function () {
        return fetch(`${apiBaseLocomotive}`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    },

    getStatus: function (driverName, address) {
        return fetch(`${apiBaseLocomotive}/${driverName}/${address}/status`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    },

    setSpeed: function (driverName, address, speed) {
        return fetch(`${apiBaseLocomotive}/${driverName}/${address}/speed`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Speed: speed })
        }).then(response => {
            if (!response.ok) throw new Error(`Error ${response.status}`);
        });
    },

    setDirection: function (driverName, address, forward) {
        return fetch(`${apiBaseLocomotive}/${driverName}/${address}/direction`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Forward: forward })
        }).then(response => {
            if (!response.ok) throw new Error(`Error ${response.status}`);
        });
    },

    setDirectionAndSpeed: async function (driverName, address, forward, speed) {
        // Schritt 1: Richtung setzen
        await this.setDirection(driverName, address, forward);
        // Schritt 2: Warten bis Richtung intern übernommen wurde
        await waitUntilDirectionIs(driverName, address, forward);
        // Schritt 3: Geschwindigkeit setzen
        await this.setSpeed(driverName, address, speed);
    },

    stop: function (driverName, address) {
        return fetch(`${apiBaseLocomotive}/${driverName}/${address}/stop`, {
            method: 'POST'
        }).then(response => {
            if (!response.ok) throw new Error(`Error ${response.status}`);
        });
    },

    toggleFunction: function (driverName, address, functionNumber, active) {
        return fetch(`${apiBaseLocomotive}/${driverName}/${address}/function/${functionNumber}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Active: active })
        }).then(response => {
            if (!response.ok) throw new Error(`Error ${response.status}`);
        });
    }
};

window.hqAccessory = {
    __registeredAccessoryEvents: [],

    /**
     * Registrierung eines Change-Listeners für Schaltartikel.
     * @param {string} switchId - Eindeutige ID des Schaltartikels (z.B. "weiche-12" oder "signal-3").
     * @param {(newState: any) => void} callback - Callback mit dem neuen Status.
     */
    onChange: function (driverName, address, callback) {
        const handler = (e) => {
            if (e.detail.driverName !== driverName) return;
            if (Number(e.detail.address) !== Number(address)) return;

            callback(e.detail.data);
        };
        accessoryEvents.addEventListener('accessoryChange', handler);
        this.__registeredAccessoryEvents.push({
            event: 'accessoryChange',
            handler
        });
    },

    /**
     * Entfernt alle registrierten Accessory-Event-Listener.
     */
    cleanupAll: function () {
        for (const { evId, handler } of this.__registeredAccessoryEvents) {
            accessoryEvents.removeEventListener(evId, handler);
        }
        this.__registeredAccessoryEvents = [];
        console.log("[hqAccessory] Alle Event-Listener entfernt.");
    },

    /**
     * Gibt alle bekannten Schaltartikel zurück.
     */
    getAll: function () {
        return fetch(`${apiBaseAccessory}`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    },

    /**
     * Gibt den Status eines einzelnen Schaltartikels zurück.
     * @param {string} driverName
     * @param {number} address
     */
    getStatus: function (driverName, address) {
        return fetch(`${apiBaseAccessory}/${driverName}/${address}`)
            .then(response => {
                if (!response.ok) throw new Error(`Error ${response.status}`);
                return response.json();
            });
    },

    /**
     * Schaltet den Zustand eines Schaltartikels.
     * @param {string} driverName
     * @param {number} address
     * @param {any} targetState
     */
    switch: function (driverName, address, targetState) {
        return fetch(`${apiBaseAccessory}/${driverName}/${address}/switch`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ targetState: targetState })
        }).then(response => {
            if (!response.ok) throw new Error(`Error ${response.status}`);
        });
    }
};

window.hqSensors = {

    __registeredSensorEvents: [],

    /**
     * Warten auf einen bestimmten Sensorzustand.
     * @param {string} sensorDriver
     * @param {number} sensorPort
     * @param {number} sensorPin
     * @param {"on"|"off"} expectedState
     * @returns {Promise<void>}
     */
    waitFor: function (sensorDriver, sensorPort, sensorPin, expectedState) {
        return new Promise(resolve => {
            const evId = createSensorId(sensorDriver, sensorPort, sensorPin);
            const handler = (e) => {
                if (e.detail.state === expectedState) {
                    feedbackEvents.removeEventListener(evId, handler);
                    resolve();
                }
            };
            feedbackEvents.addEventListener(evId, handler);

            // Registrierung zum späteren Entfernen
            this.__registeredSensorEvents.push({ evId, handler });
        });
    },

    /**
     * Reagiere auf Sensorzustand.
     * @param {string} sensorDriver
     * @param {number} sensorPort
     * @param {number} sensorPin
     * @param {"on"|"off"} expectedState
     * @param {(detail: any) => void} callback
     */
    on: function (sensorDriver, sensorPort, sensorPin, expectedState, callback) {
        const handler = (e) => {
            if (e.detail.state === expectedState) {
                callback(e.detail);
            }
        };
        const evId = createSensorId(sensorDriver, sensorPort, sensorPin);
        feedbackEvents.addEventListener(evId, handler);

        // Registrierung zum späteren Entfernen
        this.__registeredSensorEvents.push({ evId, handler });
    },

    /**
     * Entfernt alle registrierten Sensor-Event-Listener.
     */
    cleanupAll: function () {
        for (const { evId, handler } of this.__registeredSensorEvents) {
            feedbackEvents.removeEventListener(evId, handler);
        }
        this.__registeredSensorEvents = [];
        console.log("[hqSensors] Alle Event-Listener entfernt.");
    }
};

function createHqHelper() {
    return {
        /**
             * Schlafe eine bestimmte Anzahl Sekunden (default 10s)
             * @param {number} [seconds=10] - Sekunden zu schlafen
             * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
             * @returns {Promise<void>}
             */
        sleepSec: function(seconds = 10, hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(seconds * 1000, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, seconds * 1000));
        },

        /**
         * Schlafe eine bestimmte Anzahl Millisekunden (default 100ms)
         * @param {number} [ms=100] - Millisekunden zu schlafen
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         */
        sleepMs: function(ms = 100, hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(ms, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, ms));
        },

        /** Schlafe fest 10 Sekunden
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         */
        sleep10Sec: function(hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(10 * 1000, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, 10 * 1000));
        },

        /** Schlafe fest 5 Sekunden
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         */
        sleep5Sec: function(hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(5 * 1000, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, 5 * 1000));
        },

        /** Schlafe fest 1 Sekunde
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         */
        sleep1Sec: function(hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(1 * 1000, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, 1 * 1000));
        },

        /** Schlafe fest 100 Millisekunden
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         */
        sleep100Ms: function(hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(100, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, 100));
        },

        /** Schlafe fest 10 Millisekunden
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         */
        sleep10Ms: function(hqAbortSignal) {
            if (hqAbortSignal) {
                return this.sleepAbortable(10, hqAbortSignal);
            }
            return new Promise(resolve => setTimeout(resolve, 10));
        },

        /**
         * Abbrechbarer Sleep mit AbortController
         *
         * @param {number} ms - Zeit in Millisekunden
         * @param {AbortSignal} [hqAbortSignal] - Optionales Abbruchsignal
         * @returns {Promise<void>}
         *
         * @example
         * const abortController = new AbortController();
         *
         * hqHelper.sleepAbortable(5000, abortController.signal)
         *     .then(() => hqHelper.log("Fertig geschlafen"))
         *     .catch(err => hqHelper.log("Abgebrochen:", err.message));
         *
         * // Nach 2 Sekunden abbrechen:
         * setTimeout(() => {
         *     abortController.abort();
         * }, 2000);
         */
        sleepAbortable: function(ms, hqAbortSignal) {
            return new Promise((resolve, reject) => {
                if (hqAbortSignal?.aborted) {
                    return reject(new Error("Sleep aborted before start"));
                }

                const timeout = setTimeout(() => {
                        cleanup();
                        resolve();
                    },
                    ms);

                const cleanup = () => {
                    clearTimeout(timeout);
                    if (hqAbortSignal) {
                        hqAbortSignal.removeEventListener("abort", onAbort);
                    }
                };

                const onAbort = () => {
                    cleanup();
                    reject(new Error("Sleep aborted"));
                };

                if (hqAbortSignal) {
                    hqAbortSignal.addEventListener("abort", onAbort, { once: true });
                }
            });
        },
        /**
         * Gibt die aktuelle Zeit als Date-Objekt zurück
         * @returns {Date}
         */
        now: () => new Date(),

        /**
         * Gibt den aktuellen Zeitstempel in Millisekunden zurück
         * @returns {number}
         */
        nowMs: () => Date.now(),

        /**
         * Loggt mit Zeitstempel in ISO-Format
         * @param {...any} args
         */
        log: (...args) => console.log(new Date().toISOString(), ...args),

        /**
         * Erzeugt eine zufällige ganze Zahl zwischen min und max (inklusive)
         * @param {number} min
         * @param {number} max
         * @returns {number}
         */
        randomInt: (min, max) => Math.floor(Math.random() * (max - min + 1)) + min,

        /**
         * Wiederholt eine async-Funktion bis sie erfolgreich ist oder retries erreicht
         * @template T
         * @param {() => Promise<T>} fn - async Funktion, die wiederholt werden soll
         * @param {number} [retries=3] - maximale Anzahl Versuche
         * @param {number} [delayMs=1000] - Wartezeit zwischen Versuchen in ms
         * @returns {Promise<T>}
         */
        retry: async function(fn, retries = 3, delayMs = 1000) {
            for (let i = 0; i < retries; i++) {
                try {
                    return await fn();
                } catch (e) {
                    if (i === retries - 1) throw e;
                    await new Promise(res => setTimeout(res, delayMs));
                }
            }
        },

        /**
         * Formatiert Millisekunden in hh:mm:ss
         * @param {number} ms
         * @returns {string}
         */
        formatTime: ms => {
            const totalSeconds = Math.floor(ms / 1000);
            const hours = Math.floor(totalSeconds / 3600).toString().padStart(2, '0');
            const minutes = Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0');
            const seconds = (totalSeconds % 60).toString().padStart(2, '0');
            return `${hours}:${minutes}:${seconds}`;
        },

        /**
         * Beschneidet value auf Bereich [min, max]
         * @param {number} value
         * @param {number} min
         * @param {number} max
         * @returns {number}
         */
        clamp: (value, min, max) => Math.min(Math.max(value, min), max),

        /**
         * Fetch mit JSON-Auswertung und Fehlerprüfung
         * @param {string} url
         * @param {RequestInit} [options]
         * @returns {Promise<any>}
         */
        fetchJson: async (url, options) => {
            const res = await fetch(url, options);
            if (!res.ok) throw new Error(`HTTP Error ${res.status}`);
            return await res.json();
        },

        finalize: async () => {},
        onError: async (err) => {
            // Standardverhalten: Fehler einfach loggen
            console.error("Fehler im Skript:", err);
        }
    };
}

function createHqMqttClient() {
    const __registeredMqttEvents = [];
    let client = null;

    return {
        /**
         * Verbindet mit dem MQTT-Server.
         * @param {string} url - Die MQTT-Broker-URL (z.B. "wss://broker.example.com:8083/mqtt")
         * @returns {Promise<void>}
         */
        connect: async function (url) {
            if (client) {
                await this.disconnect();
            }

            client = mqtt.connect(url);

            client.on('message', (topic, message) => {
                for (const { topicPattern, callback } of __registeredMqttEvents) {
                    if (this.matchesTopic(topicPattern, topic)) {
                        callback(topic, message.toString());
                    }
                }
            });

            return new Promise((resolve, reject) => {
                client.on('connect', () => {
                    console.log("[hqMqttClient] Verbunden mit MQTT-Broker.");
                    resolve();
                });
                client.on('error', (err) => {
                    console.error("[hqMqttClient] Fehler beim Verbinden:", err);
                    reject(err);
                });
            });
        },

        /**
         * Abonniert ein MQTT-Topic.
         * @param {string} topicPattern - MQTT-Topic oder Pattern mit Wildcards.
         * @param {(topic: string, payload: string) => void} callback
         */
        subscribe: function (topicPattern, callback) {
            if (!client) {
                throw new Error("MQTT-Client nicht verbunden.");
            }

            client.subscribe(topicPattern);
            __registeredMqttEvents.push({ topicPattern, callback });
        },

        /**
         * Veröffentlicht eine Nachricht auf einem MQTT-Topic.
         * @param {string} topiccreateHqApi
         * @param {string} payload
         */
        publish: function (topic, payload) {
            if (!client) {
                throw new Error("MQTT-Client nicht verbunden.");
            }
            client.publish(topic, payload);
        },

        /**
         * Verbindet sich temporär zum Broker, publisht eine Nachricht und trennt sofort wieder die Verbindung.
         *
         * @param url Die MQTT-Broker-URL.
         * @param topic Das MQTT-Topic, auf das veröffentlicht wird.
         * @param payload Der Nachrichtentext.
         * @returns Promise, das aufgelöst wird, sobald der Vorgang abgeschlossen ist.
         */
        publishOnce: async function (url, topic, payload) {
            const client = mqtt.connect(url);

            return new Promise((resolve, reject) => {
                client.on('connect', () => {
                    client.publish(topic, payload, {}, (err) => {
                        if (err) {
                            client.end(true, {}, () => reject(err));
                        } else {
                            setTimeout(() => {
                                client.end(true, {}, () => resolve());
                            }, 250); // 200–500ms reicht oft
                        }
                    });
                });

                client.on('error', (err) => {
                    console.error("[hqMqttClient] Fehler bei publishOnce:", err);
                    client.end(true, {}, () => reject(err));
                });
            });
        },

        /**
         * Trennung vom MQTT-Broker.
         */
        disconnect: function () {
            return new Promise((resolve) => {
                if (client) {
                    client.end(true, {}, () => {
                        console.log("[hqMqttClient] Verbindung getrennt.");
                        client = null;
                        resolve();
                    });
                } else {
                    resolve();
                }
            });
        },

        /**
         * Entfernt alle registrierten MQTT-Event-Listener und trennt die Verbindung.
         */
        cleanupAll: function () {
            __registeredMqttEvents.length = 0;
            if (client) {
                this.disconnect();
            }
            console.log("[hqMqttClient] Alle Event-Listener entfernt und getrennt.");
        },

        /**
         * Prüft, ob ein Topic mit dem Pattern übereinstimmt (z.B. mit Wildcards).
         * @param {string} pattern
         * @param {string} topic
         * @returns {boolean}
         */
        matchesTopic: function (pattern, topic) {
            const regex = new RegExp('^' + pattern.replace(/\+/g, '[^/]+').replace(/#/g, '.*') + '$');
            return regex.test(topic);
        }
    };
}

function createHqApi() {
    let stopFlag = false;
    let config = {};

    const hqHelper = createHqHelper();
    const hqMqttClient = createHqMqttClient();

    const hqUi = createHqUi();
    hqUi.Tile = hqUiTile;

    return {
        hqControlStation: window.hqControlStation,
        hqLocomotive: window.hqLocomotive,
        hqAccessory: window.hqAccessory,
        hqSensors: window.hqSensors,
        hqHelper: hqHelper,
        hqMqttClient: hqMqttClient,
        hqUi: hqUi,

        shouldStop: () => stopFlag,

        stop: function () {
            stopFlag = true;

            this.hqControlStation.cleanupAll();
            this.hqLocomotive.cleanupAll();
            this.hqAccessory.cleanupAll();
            this.hqSensors.cleanupAll();
            this.hqMqttClient.cleanupAll();
        }
    };
}
