// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class ServerHandling {
    constructor(options = {}) {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__reconnectIntervalMsecs = 5000;
        this.__updateServerInformation(options);
        this.__planfield = window.planfield;
        this.__errorHandler = window.errorHandler;
        this.__checkConnectIntervalId = null;
        this.__noReconnectAfterClose = false;
        this.__initEventHandling();
        this.wsReplies = new WebSocketSync();

        this.__latencyHistory = [];
        this.__latencyChart = null;
        this.__initLatencyChart();
    }

    // #region Event Handling

    __initEventHandling() {
        this.__events = new Events();
    }

    on(eventName, callback) {
        this.__events.on(eventName, callback);
    }

    __trigger(eventName, dataObject) {
        this.__events.triggerHandler(eventName, {
            sender: this,
            data: dataObject
        });
    }

    // #endregion

    __getReconnectCountdownMessage(strSeconds) {
        return w2utils.lang("Connection not established to") + " "
            + this.wsUrl
            + ". " + w2utils.lang("Try to reconnect in") + " "
            + strSeconds + " " + w2utils.lang("seconds") + ".";
    }

    __updateServerInformation(options) {
        if (options) {
            if (options.wsAddr) this.wsAddr = options.wsAddr;
            if (options.wsPort) this.wsPort = options.wsPort;
            if (options.wsSubpath) this.wsSubpath = options.wsSubpath;
        }
    }

    __cleanupWebSocket() {
        if (typeof self.__ws !== "undefined" || self.__ws != null) {
            this.__ws.onopen = function () { };
            this.__ws.onmessage = function (e) { };
            this.__ws.onerror = function (e) { }
            this.__ws.onclose = function () { }
            this.__ws.close();
        }
    }

    __connect() {
        const self = this;

        this.__cleanupWebSocket();

        // Use dynamic protocol based on server configuration (ws:// or wss://)
        const wsProtocol = window.__wsProtocol || (window.location.protocol === 'https:' ? 'wss' : 'ws');
        this.wsUrl = wsProtocol + '://' + this.wsAddr + ':' + this.wsPort + this.wsSubpath;
        this.__ws = new WebSocket(this.wsUrl);
        // , ['access_token', `${window.__access_token}`]
        this.__ws.onopen = function () { self.__handleOnOpen(); };
        this.__ws.onmessage = function (e) { self.__handleOnMessage(e); };
        this.__ws.onerror = function (e) { self.__handleOnError(e); }
        this.__ws.onclose = function () { self.__handleOnClose(); }
    }

    __startConnectHandlerInterval() {
        if (this.__checkConnectIntervalId != null) return;
        let secondsToWait = Math.floor(this.__reconnectIntervalMsecs / 1000);
        this.__checkConnectIntervalId = setInterval(() => {
            if (secondsToWait <= 0) {
                console.log("Try to connect...");
                this.__errorHandler.setLevel(ErrorHandlerLevel.Info);
                this.__errorHandler.setText(w2utils.lang("Try to connect..."), true);
                this.__stopConnectHandlerInterval();
                this.__connect();
            } else {
                this.__errorHandler.setLevel(ErrorHandlerLevel.Error);
                this.__errorHandler.setText(this.__getReconnectCountdownMessage(secondsToWait), true);
            }
            secondsToWait--;
        }, 1000);
    }

    __stopConnectHandlerInterval() {
        if (this.__checkConnectIntervalId == null)
            return;
        clearInterval(this.__checkConnectIntervalId);
        this.__checkConnectIntervalId = null;
    }

    __generatePreciseTimestamp() {
        const now = new Date();
        const isoString = now.toISOString(); // Beispiel: "2025-01-12T18:01:27.396Z"
        const microseconds = String(now.getMilliseconds()).padStart(3, '0') +
            Math.floor(Math.random() * 1000).toString().padStart(3, '0');
        return isoString.replace(/\.\d{3}Z$/, `.${microseconds}Z`);
    }

    __handleOnOpen() {
        console.log('Connected to ' + this.wsUrl);

        //
        // send initial authentication
        //
        const accessToken = {
            type: "auth",
            token: window.__access_token
        };
        var jsonData = JSON.stringify(accessToken);
        this.__ws.send(jsonData);

        this.__stopConnectHandlerInterval();
        this.__errorHandler.hide();

        this.__trigger('connected', {});

        //
        // start ping/pong time measuring
        //
        const self = this;
        self.__sendPing();
    }

    __sendPing() {
        sendPing({ c: 'ping', t: Date.now() });
    }

    __getS88Data(jsonData) {
        if (!Array.isArray(jsonData) || jsonData.length !== 5) {
            return null; // TODO fatal, wrong dataset
        }

        const [port, maxPorts, hexState, binaryState, driverName] = jsonData;

        if (typeof port !== "number" || typeof maxPorts !== "number" || typeof hexState !== "string") {
            return null; // TODO fatal, malformed data
        }
        if (!/^(0x)?[0-9a-fA-F]+$/.test(hexState)) {
            return null; // Invalid hex format
        }
        if (binaryState.length !== 16 && binaryState.length !== 8) {
            return null;
        }

        if (/[^01]/.test(binaryState)) {
            return null; // Invalid binary string
        }

        return {
            "port": port,
            "maxPorts": maxPorts,
            "hexState": hexState,
            "binaryState": binaryState,
            "driverName": driverName
        };
    }

    __addLatencyValue(latency) {
        this.__latencyHistory.push(latency);
        if (this.__latencyHistory.length > 100) this.__latencyHistory.shift();
    }

    __initLatencyChart() {
        const self = this;

        $('.serverInfoPing').off('click');
        $('.serverInfoPing').on('click', function (e) {
            const popup = $('#latencyChartPopup');
            popup.css({ top: e.pageY + 10, left: e.pageX + 10 }).show();
            if (self.__latencyChart) self.__latencyChart.destroy();
            const ctx = document.getElementById('latencyChart').getContext('2d');
            self.__latencyChart = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: self.__latencyHistory.map((_, i) => i + 1), // 1..n
                    datasets: [{
                        label: 'Latency (ms)',
                        data: self.__latencyHistory,
                        borderColor: 'rgba(75, 192, 192, 1)',
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        tension: 0.1,
                    }]
                },
                options: {
                    responsive: false,
                    animation: false,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });
            $(popup).css({ top: "25px", left: "25px" });
            $(document).off('click');
            $(document).on('click', function (e) {
                if (!$(e.target).closest('#latencyChartPopup, .serverInfoPing').length) {
                    $('#latencyChartPopup').hide();
                }
            });
            $(popup).draggable();
            //$(popup).resizable({
            //    alsoResize: '#latencyChart',
            //    minWidth: 200,
            //    minHeight: 100
            //});

            popup.show();
        });
    }

    __updateLatencyUi(latency) {
        this.__addLatencyValue(latency);
        $('.serverInfoPing .value').text(`${w2utils.lang('Latency')}: ${latency} ms`);

        // Wenn Chart aktiv ist → live aktualisieren
        if (this.__latencyChart) {
            this.__latencyChart.data.labels = this.__latencyHistory.map((_, i) => i + 1);
            this.__latencyChart.data.datasets[0].data = this.__latencyHistory;
            this.__latencyChart.update();
        }
    }

    __handleOnMessage(e) {
        const self = this;
        const jsonData = JSON.parse(e.data);

        // Is ping?
        if (jsonData.c && jsonData.t && jsonData.tt) {
            const t0 = jsonData.t;
            const t1 = Date.now();
            const latency = t1 - t0;
            self.__updateLatencyUi(latency);
            setTimeout(() => { self.__sendPing(); }, 5 * 1000);
            return;
        }

        // Is S88 feedback?
        const s88Data = self.__getS88Data(jsonData);
        if (s88Data != null) {
            this.__trigger('s88Received', s88Data);
            return;
        }

        if (!jsonData?.command) {
            // TODO fatal, wrong dataset
            return;
        }

        switch (jsonData.command) {
            case "remove":
            {
                if (jsonData.entity) {
                    this.__trigger('entityRemoved', jsonData.entity);
                }
            }
            break;

            case "warning":
                {
                    this.__trigger('warningError', jsonData);
                }
                break;

            case "fatal":
                {
                    this.__trigger('fatalError', jsonData);
                }
                break;

            case "debugMessages":
                this.__trigger('debugMessages', jsonData);
                break;

            case "initialization":
                this.__trigger('initialization', jsonData);
                break;

            case "update":
                this.__trigger('dataReceived', jsonData);
                break;

            case "updateState":
                this.__trigger('dataReceived', jsonData);
                break;

            case "reply":
                this.wsReplies.appendMessage(jsonData);
                break;

            case "automode":
                this.__trigger('automode', jsonData);
                break;

            default:
                console.log(`<__handleOnMessage()> '${e.data}'`);
        }
    }

    applyErrorMessageToOverlay(errorObject) {

        if (typeof errorObject === "string") {
            this.__errorHandler.setText(errorObject);
            return;
        }

        // errorObject.code
        // errorObject.message
        switch (errorObject.code) {
            case 401: // UNAUTHORIZED

                {
                    this.__errorHandler.setText("Deine Sitzung ist abgelaufen.<br>Bitte melde dich erneut an.");
                }
                break;

            case 403: // FORBIDDEN
                {
                    this.__errorHandler.setText(`Der angeforderte Arbeitsbereich ${errorObject.message} ist in dieser Sitzung nicht verfügbar. Bitte überprüfe deinen Browser auf weitere Informationen. Diese Sitzung wird nun beendet.`);
                }
                break;

            default:
                {
                    this.__errorHandler.setText(errorObject.message);

                }
                break;
        }
    }

    __handleOnError(e) {
        const strSeconds = this.__reconnectIntervalMsecs / 1000;
        this.__errorHandler.setLevel(ErrorHandlerLevel.Error);
        this.__errorHandler.setText(this.__getReconnectCountdownMessage(strSeconds));
        this.__cleanupWebSocket();
        this.__startConnectHandlerInterval();
    }

    __handleOnClose() {
        this.__errorHandler.setLevel(ErrorHandlerLevel.Error);
        this.__errorHandler.setText(w2utils.lang("Connection closed."));
        this.__cleanupWebSocket();
        if (!this.__noReconnectAfterClose)
            this.__startConnectHandlerInterval();
    }

    disableReconnectHandler() {
        this.__noReconnectAfterClose = true;
    }

    /**
     * Attempts to establish a connection by setting the error handler level, updating server information, 
     * and initiating the connection.
     * 
     * @param {Object} options - Optional configuration options for the connection.
     */
    establishConnection(options = {}) {
        // Set error handler level and message
        //this.__errorHandler.setLevel(ErrorHandlerLevel.Info);
        //this.__errorHandler.setText("Try to establish connection...");

        // Update server information and initiate connection
        this.__updateServerInformation(options);
        this.__connect();
    }

    /**
     * Checks if the WebSocket connection is open.
     * 
     * @returns {boolean} - Returns true if the WebSocket is open, otherwise false.
     */
    isConnected() {
        if (!this.__ws || this.__ws.readyState !== WebSocket.OPEN) return false;
        return true;
    }

    /**
     * Sends a command over WebSocket if the provided command data is valid.
     * Logs any errors encountered during the send attempt.
     * 
     * @param {Object} cmdData - The command data to send over the WebSocket.
     */
    send(cmdData) {
        if (!cmdData) return; // Returns early if cmdData is undefined or null.

        try {
            this.__ws.send(JSON.stringify(cmdData));
        } catch (error) {
            console.error("Error sending command:", error);
        }
    }

}