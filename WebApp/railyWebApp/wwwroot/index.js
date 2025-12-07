// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// globals to keep recent data
window.blockSensors = null;
window.routes = null;

// globals to keep control station connectivity info
window.__isEcosAvailable = false;
window.__isZ21Available = false;
window.__isDemoAvailable = false;

// dialog instances
window.blocksDlg = null;
window.routesDlg = null;
window.accessoriesDlg = null;
window.locomotivesDlg = null;
window.locomotiveCtrlDlgs = [];

// staging instance
window.stagingDlg = null;

// planfield controls, states
window.textfieldElementInstances = []; // all text elements in the plan
window.planfield = null;
window.editState = false;
window.toolbox = null;
window.labelShown = false;
window.occLayer = null;

window.__access_token = "##access_token##";
window.__access_email = "##access_email##";
window.__access_uid = "##access_uid##";
window.__access_hasService = "##access_hasService##".toLowerCase() === "true";

window.__urlHq = "{{GENERATE_NET_HOST}}";
window.__urlPortHq = "##GENERATE_NET_PORT##";
window.__wsProtocol = "##GENERATE_WS_PROTOCOL##";
window.__httpProtocol = "##GENERATE_HTTP_PROTOCOL##";
window.__indexUrl = "{{CONFIG_INDEX_URL}}";

window.__planfieldUuid = "";
window.__pageLastUnload = null;
window.__pageReloaded = false;
window.workspaceName = "";

w2utils.settings.phrases = {
    // Start of w2ui internal used phrases.
    "Save": "Speichern",
    "Delete": "Löschen",
    "Cancel": "Abbrechen",
    "Are you sure?": "Bist du sicher?",
    "Loading...": "Lade...",
    "Search": "Suche",
    "All Fields": "Alle Felder",
    "Edit": "Editieren",
    "Reset": "Zurücksetzen",
    "Record ID": "Identifizierer",
    "of": "von",
    "Execute": "Ausführen",
    "Save": "Speichern",
    "Applies all changes to the server.": "Übernimmt die Änderungen",
    "Simulates the route by changing any relevant accessory to let the trains reach their destination.": "Stellt alle nötigen Zubehörteile so, dass Züge ihr Ziel erreichen.",
    "Check": "Prüfen",
    "Add New": "Abschnitt hinzufügen",
    "Add new record": "Neuen Abschnitt hinzufügen",
    "Confirmation": "Bestätigung",
    "Yes": "Ja",
    "No": "Nein",
    "Open Search Fields": "Öffne Suchfilter",
    "Close": "Schließen",
    "Show": "Zeigen",
    "Hide": "Verstecken",

    // Start of railhq.io used phrases.
    "Latency": "Latenz",
    "Protocol": "Protokoll",
    "Try to connect...": "Verbindungsaufbau...",
    "Connection not established to": "Verbindung nicht hergestellt zu",
    "seconds": "Sekunden",
    "Try to reconnect in": "Versuche zu verbinden in",
    "Connection closed.": "Verbindung unterbrochen!"
};

window.findLocomotivesDlg = (function (id) {
    if (!id) return null;
    const len = window.locomotiveCtrlDlgs.length;
    for (let i = 0; i < len; ++i) {
        const instance = window.locomotiveCtrlDlgs[i];
        if (!instance) continue;
        if (instance.__dlgId === id)
            return instance;
    }
    return null;
});

function hideLoadingSpinner() {
    setTimeout(function () {
        document.getElementById("loadingSpinner").style.display = "none";
    }, 100);
}

function removeTextfieldFromGlobalList(uniqueId) {
    if (uniqueId == null)
        return;
    var idxToRemove = -1;
    for (var i = 0; i < window.textfieldElementInstances.length; ++i) {
        var instance = window.textfieldElementInstances[i];
        if (instance.__uniqueId === uniqueId) {
            idxToRemove = i;
            break;
        }
    }
    if (idxToRemove !== -1) {
        window.textfieldElementInstances.splice(idxToRemove, 1);
    }
}

function removeLocomotiveDialogFromGlobalList(dlgId) {
    var idsToRemove = [];
    for (var j = 0; j < window.locomotiveCtrlDlgs.length; ++j) {
        var dlgInstance = window.locomotiveCtrlDlgs[j];
        if (dlgInstance == null) {
            idsToRemove.push(j);
            continue;
        }
        try {
            if (dlgInstance.__dlgId === dlgId)
                idsToRemove.push(j);
        }
        catch (ev) {
            // ignore
        }
    }

    //const reversed = idsToRemove.reverse();
    //for (var j = 0; j < reversed.length; ++j)
    //    window.locomotiveCtrlDlgs.splice(reversed[j], 1);
    for (var j = idsToRemove.length - 1; j >= 0; --j) {
        window.locomotiveCtrlDlgs.splice(idsToRemove[j], 1);
    }
}

function openLocomotiveControlDialog(gridRecordRow) {

    const locCtrl = new LocomotiveControl();
    const res = locCtrl.open(gridRecordRow);
    if (!res) {
        console.log("Already open!");
    } else {
        window.locomotiveCtrlDlgs.push(locCtrl);
        locCtrl.on('dialogClosed', (ev) => removeLocomotiveDialogFromGlobalList(ev.data.instance.id));
        locCtrl.on('changed', (ev) => sendLocomotiveCommand(ev.data));

        refreshEntity({
            objectId: gridRecordRow.oid,
            command: 'refresh',
            argument: 'entity',
            argumentValue: {
                driverName: gridRecordRow.driverName,
                objectId: gridRecordRow.oid
            }
        });
    }
}

$(document).ready(function () {
    initLoadingStateMachine();

    addJqueryExtensions();
    initDebugConsole();
    initLeaveHandler();
    checkForPageReload();

    $('#statusBar div.autoMode').tipso({
        size: 'tiny',
        speed: 100,
        delay: 250,
        useTitle: true,
        background: '#333333',
        titleBackground: '#333333'
    });

    $('#statusBar div.logging').tipso({
        size: 'tiny',
        speed: 100,
        delay: 250,
        useTitle: true,
        background: '#333333',
        titleBackground: '#333333'
    });

    window.toolbox = new Toolbox();

    window.blocksDlg = new Blocks();
    window.blocksDlg.install();
    window.blocksDlg.on("setting", (ev) => changeSetting(ev.data));
    window.blocksDlg.on("highlight",
        (ev) => {
            const blockId = ev.data.blockId;
            const sensorEnter = ev.data.sensorEnter;
            const sensorIn = ev.data.sensorIn;
            const signal = ev.data.signal;
            const vorsignal = ev.data.vorsignal
            window.planfield?.highlightBlock(blockId, sensorEnter, sensorIn, signal, vorsignal);
        });
    window.blocksDlg.on("unhighlight",
        (ev) => {
            window.planfield?.unhighlightAllBlocks();
        });

    // NOTE: access class SensorList like `Blocks`
    window.stagingDlg = new Staging();
    window.stagingDlg.install();
    window.stagingDlg.on("setting", (ev) => changeSetting(ev.data));
    window.stagingDlg.on("automode", (ev) => sendAutoModeCommand(ev.data));

    window.accessoriesDlg = new Accessories();
    window.accessoriesDlg.install();
    window.accessoriesDlg.on("setting", (ev) => changeSetting(ev.data));
    window.accessoriesDlg.on("inventar", (ev) => changeInventar(ev.data));
    window.accessoriesDlg.on("accessoryExecute", (ev) => sendAccessoryCommand(ev.data));

    window.routesDlg = new Routes();
    window.routesDlg.install();
    window.routesDlg.on("checkRoute", (ev) => sendRoutingCommand(ev.data));
    window.routesDlg.on("setting", (ev) => changeSetting(ev.data));

    window.locomotivesDlg = new Locomotives();
    window.locomotivesDlg.install();
    window.locomotivesDlg.on('setting', (ev) => changeSetting(ev.data));
    window.locomotivesDlg.on("inventar", (ev) => changeInventar(ev.data));
    window.locomotivesDlg.on('doubleClickGridRow', (ev) => openLocomotiveControlDialog(ev.data));

    /**
     *
     * Planfield
     *
     */
    window.planfield = new Planfield({ isEditMode: false });
    window.planfield.install();
    window.planfield.on('itemClicked', (ev) => sendTrackplanCommand(ev.data));
    window.planfield.on('setting', (ev) => changeSetting(ev.data));
    window.planfield.on('connectorChanged', (ev) => sendTrackplanCommand(ev.data));
    window.planfield.on('automode', (ev) => sendAutoModeCommand(ev.data));
    window.planfield.on('controlCreated', function (ev) {
        const ctrlInstance = ev.data.instance;

        // inform the Route/S88/Signals dialog for updating its internal lists
        if (typeof window.blocksDlg !== "undefined" && window.blocksDlg != null)
            window.blocksDlg.controlCreated(ctrlInstance);
    });

    /**
     * Occupied Layer
     */
    window.occLayer = new OccLayer();
    window.occLayer.on('automode', (ev) => sendAutoModeCommand(ev.data));
    window.occLayer.on('changed', (ev) => sendLocomotiveCommand(ev.data));
    window.occLayer.on('openLocomotiveControl', (ev) => {
        const driverName = ev.data.driverName;
        const objectId = ev.data.objectId;
        const locRec = window.locomotivesDlg.getLocomotiveRecord(driverName, objectId);
        openLocomotiveControlDialog(locRec);
    });

    /**
     *
     * Server
     *
     */
    // #region Server
    const requestParams = new URLSearchParams(window.location.search);
    const requestWorkspace = requestParams.get('workspace');
    window.workspaceName = requestWorkspace;
    const wsSubpath = '/ws/browser' + ((requestWorkspace && requestWorkspace.length > 0) ? `?workspace=${requestWorkspace}` : '');
    window.serverHandling = new ServerHandling({
        wsAddr: "{{GENERATE_NET_HOST}}",
        wsPort: "##GENERATE_NET_PORT##",
        wsSubpath: wsSubpath
    });
    window.serverHandling.establishConnection();
    window.serverHandling.on('entityRemoved', (ev) => entityRemoved(ev.data));
    window.serverHandling.on('connected', (ev) => serverConnected(ev.data));
    window.serverHandling.on('fatalError', (ev) => fatalErrorReceived(ev.data));
    window.serverHandling.on('warningError', (ev) => warningErrorReceived(ev.data));
    window.serverHandling.on('dataReceived', (ev) => dataReceivedHandle(ev.data));
    window.serverHandling.on('updateState', (ev) => dataReceivedHandle(ev.data));
    window.serverHandling.on('s88Received', (ev) => s88ReceivedHandle(ev.data));
    window.serverHandling.on('debugMessages', (ev) => window.addDebugMessages(ev.data));
    window.serverHandling.on('automode', (ev) => automodeHandle(ev.data));
    window.serverHandling.on('initialization', function (ev) {
        const jsonData = ev.data;
        if (jsonData.themeData) {
            if (!window.themeData) {
                applyLoadingState(STEP_THEMING);
                window.themeData = jsonData.themeData;
                window.themeName = jsonData.themeName;
                window.toolbox.on('close', (ev) => { closeEditMode(); });
            }
        }

        if (jsonData.planfield) {
            applyLoadingState(STEP_GLEISPLAN);
            window.planfield.createPlanfield({
                themeData: window.themeData,
                planfield: jsonData.planfield
            });

            // <div id="customerUi" style="position: absolute; top: 0; left: 0; width: 1px; height: 1px;"></div>
        }

        jsonData.themeData = null;
        jsonData.planfield = null;

        dataReceivedHandle(jsonData);

        hideLoadingSpinner();

        if (window.loadingMachine.isFinished()) {
            window.hqScriptRunner.runAutoload();
        }
    });

    function entityRemoved(entityData) {
        if (entityData) {
            if (entityData.driverName && entityData.address) {
                console.log(`Remove ${entityData.driverName}::${entityData.address} as ${entityData.removeType}`);

                if (entityData.removeType === "Accessory") {
                    window.accessoriesDlg?.removeAccessory(entityData.driverName, entityData.address);
                    return;
                }

                if (entityData.removeType === "Locomotive") {
                    window.locomotivesDlg?.removeLocomotive(entityData.driverName, entityData.address);
                    return;
                }
            }
        }
    }

    function serverConnected(jsonData) {
        handleStateAfterPageReload();
    }

    function fatalErrorReceived(jsonData) {
        if (!jsonData.info) return;
        const info = jsonData.info;
        window.serverHandling.applyErrorMessageToOverlay(info);
        window.serverHandling.disableReconnectHandler();
    }

    function warningErrorReceived(jsonData) {
        if (!jsonData.info) return;
        const info = jsonData.info;
        showWarning(info);
    }

    // Overlay anzeigen
    window.showError = function showError(message = "Fatal error - no additional information available!") {
        document.querySelector("#errorOverlay p").textContent = message;
        document.getElementById("errorOverlay").classList.remove("hidden");
    }

    window.showWarning = function showWarning(message = "Warning - no additional information available!", fadeOutSeconds = 5) {
        if (typeof message === "string")
            $("#warningBox").html(message).stop(true, true).fadeIn(300).delay(fadeOutSeconds * 1000).fadeOut(500);
        else if (typeof message.message === "string")
            $("#warningBox").html(message.message).stop(true, true).fadeIn(300).delay(fadeOutSeconds * 1000).fadeOut(500);
    }

    window.__routeToVisualizeAfterPlanfieldLoad = [];
    window.__destinationToVisualizeAfterPlanfieldLoad = [];

    function automodeHandle(jsonData) {
        if (jsonData.command && jsonData.command === "automode") {
            const argument = jsonData.argument;
            const argumentValue = jsonData.argumentValue;

            switch (argument) {
                case "visualizeRoute":
                    {
                        const afterPlanfieldLoad = argumentValue.afterPlanfieldLoad === true;
                        if (afterPlanfieldLoad === true)
                            __routeToVisualizeAfterPlanfieldLoad.push(argumentValue);
                        else
                            window.planfield.activateRouteVisualization(argumentValue);
                    }
                    break;

                case "unvisualizeRoute":
                    {
                        window.planfield.deactivateRouteVisualization(argumentValue);
                    }
                    break;

                case "resetDestination":
                    {
                        window.occLayer.resetDestination(argumentValue);
                        window.stagingDlg.resetDestination(argumentValue);
                    }
                    break;

                case "setDestination":
                    {
                        const afterPlanfieldLoad = argumentValue.afterPlanfieldLoad === true;
                        if (afterPlanfieldLoad === true) {
                            window.__destinationToVisualizeAfterPlanfieldLoad.push(argumentValue);
                        } else {
                            window.occLayer.setDestination(argumentValue);
                            window.stagingDlg.setDestination(argumentValue);
                        }
                    }
                    break;

                default:
                    console.log(`Unknown automode command: ${argument}`);
                    break;
            }
        }
    }

    function s88ReceivedHandle(s88Data) {
        try {
            applyLoadingState(STEP_RUECKMELDEBAUSTEINE);
            window.planfield?.updateSensors(s88Data);

            showS88Feedback(s88Data);

        } catch (err) {
            console.error(err);
        }
    }

    window.__recentSensorData = null;

    function showS88Feedback(sensorData) {
        if (!sensorData) return;
        window.__recentSensorData = sensorData;

        const isDebugActive = $('.debugConsole input[type="checkbox"][value="debugAccessories"]').is(':checked');
        if(!isDebugActive) return;

        const portId = sensorData.port - 1;
        const hexState = parseInt(sensorData.hexState, 16);
        if (isNaN(hexState)) return;

        for (let index = 0; index < 16; ++index) {
            const mask = 1 << (15 - index);
            const isActive = (hexState & mask) !== 0;  // true, wenn Sensor aktiv

            if (isActive) {
                const calculatedPinS88 = (portId * 16) + index;
                addDebugText(`S88-Addresse: ${calculatedPinS88} (Port: ${portId}, Offset: ${index})`);
            }
        }
    }

    window.__ecosStatusHideTimeout = null;
    window.__z21StatusHideTimeout = null;
    window.__demoStatusHideTimeout = null;

    //
    // update Sidebar
    //
    if (!window.updateSidebarPower) {
        window.updateSidebarPower = function () {
            const maxStations = 3;
            let countAvailable = 0;
            let countOnline = 0;

            const nodeEcos = $('#node_cmdPowerEcos');
            const nodeZ21 = $('#node_cmdPowerZ21');
            const nodeDemo = $('#node_cmdPowerDemo');

            const nodeIconEcos = $('#node_cmdPowerEcos .w2ui-node-image span');
            const nodeIconZ21 = $('#node_cmdPowerZ21 .w2ui-node-image span');
            const nodeIconDemo = $('#node_cmdPowerDemo .w2ui-node-image span');

            // cleanup
            const aktivIcon = 'fa-check-circle';
            const aktiveColor = '#28a745';
            const nichtErreichbarIcon = 'fa-exclamation-circle';
            const nichtErreichbarColor = '#ffc107';
            const deactivatedIcon = 'fa-power-off';
            const deactivatedColor = '#dc3545';

            nodeIconEcos.removeClass(aktivIcon);
            nodeIconEcos.removeClass(nichtErreichbarIcon);
            nodeIconEcos.removeClass(deactivatedIcon);

            nodeIconZ21.removeClass(aktivIcon);
            nodeIconZ21.removeClass(nichtErreichbarIcon);
            nodeIconZ21.removeClass(deactivatedIcon);

            nodeIconDemo.removeClass(aktivIcon);
            nodeIconDemo.removeClass(nichtErreichbarIcon);
            nodeIconDemo.removeClass(deactivatedIcon);

            if (window.__sidebarRailyData.ecosbase) {
                const ecosbase = window.__sidebarRailyData.ecosbase;
                const isOnline = ecosbase.status === "GO";
                const { name, protocolVersion, applicationVersion, hardwareVersion } = ecosbase;
                countAvailable++;
                if (isOnline === true) {
                    countOnline++;
                    nodeIconEcos.addClass(aktivIcon);
                    nodeIconEcos.attr('style', `color: ${aktiveColor} !important`);
                    nodeEcos.attr('title', '');
                } else {
                    nodeIconEcos.addClass(deactivatedIcon);
                    nodeIconEcos.attr('style', `color: ${deactivatedColor} !important`);
                    nodeEcos.attr('title', 'ECoS ausgeschaltet');
                }
            } else {
                // not available
                nodeIconEcos.addClass(nichtErreichbarIcon);
                nodeIconEcos.attr('style', `color: ${nichtErreichbarColor} !important`);
                nodeEcos.attr('title', 'ECoS nicht verfügbar');
            }

            if (window.__sidebarRailyData.z21base) {
                const z21base = window.__sidebarRailyData.z21base;
                const isOnline = z21base.trackOn === true;
                const { name, mainCurrent, trackVoltage, hardwareType, firmwareVersion } = z21base;
                countAvailable++;
                if (isOnline === true) {
                    // online
                    countOnline++;
                    nodeIconZ21.addClass(aktivIcon);
                    nodeIconZ21.attr('style', `color: ${aktiveColor} !important`);
                    nodeZ21.attr('title', '');
                } else {
                    // offline
                    nodeIconZ21.addClass(deactivatedIcon);
                    nodeIconZ21.attr('style', `color: ${deactivatedColor} !important`);
                    nodeZ21.attr('title', 'Z21 ausgeschaltet');
                }
            } else {
                // not available
                nodeIconZ21.addClass(nichtErreichbarIcon);
                nodeIconZ21.attr('style', `color: ${nichtErreichbarColor} !important`);
                nodeZ21.attr('title', 'Z21 nicht verfügbar');
            }

            if (window.__sidebarRailyData.demobase) {
                const demobase = window.__sidebarRailyData.demobase;
                const isOnline = demobase.status === "GO";
                const { name, protocolVersion, applicationVersion, hardwareVersion } = demobase;
                countAvailable++;
                if (isOnline === true) {
                    // online
                    countOnline++;
                    nodeIconDemo.addClass(aktivIcon);
                    nodeIconDemo.attr('style', `color: ${aktiveColor} !important`);
                    nodeDemo.attr('title', '');
                } else {
                    // offline
                    nodeIconDemo.addClass(deactivatedIcon);
                    nodeIconDemo.attr('style', `color: ${deactivatedColor} !important`);
                    nodeDemo.attr('title', 'Demo ausgeschaltet');
                }
            } else {
                // not available
                nodeIconDemo.addClass(nichtErreichbarIcon);
                nodeIconDemo.attr('style', `color: ${nichtErreichbarColor} !important`);
                nodeDemo.attr('title', 'Demo nicht verfügbar');
            }

            const infoPowerConnections = $('#infoPowerConnections');
            infoPowerConnections.html(`(${countOnline}/${maxStations} verbunden)`);
        }
    }

    function dataReceivedHandle(jsonData) {
        if (jsonData.railyData) {
            const railyData = jsonData.railyData;

            //
            // update statusbar for ECoS
            //
            const divEcosInformation = $('#statusBar div.csStatusEcos .stationInfo');
            const ecosinfoVisible = divEcosInformation.is(':visible');
            if (ecosinfoVisible === false)
                divEcosInformation.hide();
            const divEcosPowerStatus = $('#statusBar div.csStatusEcos .powerStatus');
            divEcosPowerStatus.hover(
                function () {
                    clearTimeout(window.__ecosStatusHideTimeout);
                    divEcosInformation.show();
                },
                function () {
                    window.__ecosStatusHideTimeout = setTimeout(function () {
                        divEcosInformation.stop(true, true).fadeOut(200);
                    }, 500);
                }
            );
            if (railyData?.ecosbase) {
                const ecosbase = railyData.ecosbase;
                if (ecosbase) {
                    applyLoadingState(STEP_CONTROLSTATION);

                    window.__sidebarRailyData.ecosbase = ecosbase;
                    window.__isEcosAvailable = true;
                    divEcosPowerStatus.show();

                    const isOnline = ecosbase.status === "GO";
                    divEcosPowerStatus.toggleClass("notAvailaible", false);
                    divEcosPowerStatus.toggleClass("online", isOnline);
                    divEcosPowerStatus.toggleClass("offline", !isOnline);

                    const { name, protocolVersion, applicationVersion, hardwareVersion } = ecosbase;
                    const strInfo =
                        `<b>${name}</b> (SW: ${applicationVersion}, HW: ${hardwareVersion}, ${w2utils.lang('Protocol')
                        }: ${protocolVersion})`;
                    divEcosInformation.html(strInfo);
                } else {
                    divEcosPowerStatus.toggleClass("offline", false);
                    divEcosPowerStatus.toggleClass("online", false);
                    divEcosPowerStatus.toggleClass("notAvailable", true);
                    divEcosInformation.hide();
                }
            }

            //
            // update statusbar for Z21
            //
            const divZ21Information = $('#statusBar div.csStatusZ21 .stationInfo');
            const z21infoVisible = divZ21Information.is(':visible');
            if (z21infoVisible === false)
                divZ21Information.hide();
            const divZ21PowerStatus = $('#statusBar div.csStatusZ21 .powerStatus');
            divZ21PowerStatus.hover(
                function () {
                    // Maus betritt: sofort anzeigen und evtl. geplantes Ausblenden abbrechen
                    clearTimeout(window.__z21StatusHideTimeout);
                    divZ21Information.show();
                },
                function () {
                    // Maus verlässt: mit Verzögerung ausblenden
                    window.__z21StatusHideTimeout = setTimeout(function () {
                        divZ21Information.stop(true, true).fadeOut(200);
                    }, 500); // 500 ms Verzögerung
                }
            );
            if (railyData?.z21base) {
                const z21base = railyData.z21base;
                if (z21base) {
                    applyLoadingState(STEP_CONTROLSTATION);

                    window.__sidebarRailyData.z21base = z21base;
                    window.__isZ21Available = true;
                    divZ21PowerStatus.show();

                    const isOnline = z21base.trackOn === true;
                    divZ21PowerStatus.toggleClass("notAvailaible", false);
                    divZ21PowerStatus.toggleClass("online", isOnline);
                    divZ21PowerStatus.toggleClass("offline", !isOnline);

                    const { name, mainCurrent, trackVoltage, hardwareType, firmwareVersion } = z21base;
                    const strInfo =
                        `<b>${name}</b> (${mainCurrent} mA, ${trackVoltage} mV, HW: ${hardwareType}, FW: ${firmwareVersion})`;

                    divZ21Information.html(strInfo);
                } else {
                    divZ21PowerStatus.toggleClass("offline", false);
                    divZ21PowerStatus.toggleClass("online", false);
                    divZ21PowerStatus.toggleClass("notAvailable", true);
                    divZ21Information.hide();
                }
                if (z21infoVisible === true)
                    divZ21Information.show();
            }

            //
            // update statusbar for Demo
            //
            const divDemoInformation = $('#statusBar div.csStatusDemo .stationInfo');
            const demoinfoVisible = divDemoInformation.is(':visible');
            if (demoinfoVisible === false)
                divDemoInformation.hide();

            const divDemoPowerStatus = $('#statusBar div.csStatusDemo .powerStatus');
            divDemoPowerStatus.hover(
                function () {
                    // Maus betritt: sofort anzeigen und evtl. geplantes Ausblenden abbrechen
                    clearTimeout(window.__demoStatusHideTimeout);
                    divDemoInformation.show();
                },
                function () {
                    // Maus verlässt: mit Verzögerung ausblenden
                    window.__demoStatusHideTimeout = setTimeout(function () {
                        divDemoInformation.stop(true, true).fadeOut(200);
                    }, 500); // 500 ms Verzögerung
                }
            );
            if (railyData?.demobase) {
                const demobase = railyData.demobase;

                if (demobase) {
                    applyLoadingState(STEP_CONTROLSTATION);

                    // in case we receive "demobase" we fake S88 state
                    // because we guess that no hardware for S88 is
                    // connected and configured; this state fake
                    // results in a better UI handling because
                    // no dialog for missing data is shown
                    applyLoadingState(STEP_RUECKMELDEBAUSTEINE);

                    window.__sidebarRailyData.demobase = demobase;
                    window.__isDemoAvailable = true;
                    divDemoPowerStatus.show();

                    const isOnline = demobase.status === "GO";
                    divDemoPowerStatus.toggleClass("online", isOnline);
                    divDemoPowerStatus.toggleClass("offline", !isOnline);

                    const { name, protocolVersion, applicationVersion, hardwareVersion } = demobase;
                    const strInfo =
                        `<b>${name}</b> (SW: ${applicationVersion}, HW: ${hardwareVersion}, ${w2utils.lang('Protocol')
                        }: ${protocolVersion})`;
                    divDemoInformation.html(strInfo);
                } else {
                    //divDemoPowerStatus.hide();
                    divDemoPowerStatus.toggleClass("offline", false);
                    divDemoPowerStatus.toggleClass("online", false);
                    divDemoPowerStatus.toggleClass("notAvailable", true);
                    divDemoInformation.hide();
                }
            }

            // Call after statusbar update!
            updateSidebarPower(railyData);

            //
            // update locomotives
            //
            if (railyData?.locomotives) {
                applyLoadingState(STEP_LOKOMOTIVEN);
                window.__sidebarRailyData.locomotives = railyData.locomotives;
                window.occLayer.loadLocomotives(railyData.locomotives);
                window.locomotivesDlg?.updateLocomotives(railyData.locomotives);
            }

            //
            // update accessories
            //
            if (railyData?.accessories) {
                applyLoadingState(STEP_SCHALTARTIKEL);
                window.accessoriesDlg?.updateAccessories(railyData.accessories);
                window.planfield?.updateAccessories(railyData.accessories);
            }

            window.__initialData.railyData = true;
            checkMinimumSetOfDataAndRevitalizeUi();
        }

        //
        // when entityData is set, only a single entity is updated
        // much better as updating the whole internals
        // higher performance
        //
        if (jsonData.entityData) {

            if (jsonData.entityType === "accessory") {
                const accessoryData = [
                    jsonData.entityData
                ];
                applyLoadingState(STEP_SCHALTARTIKEL);
                window.accessoriesDlg?.updateAccessories(accessoryData);
                window.planfield?.updateAccessories(accessoryData);

            } else if (jsonData.entityType === "locomotive") {

                const locomotiveData = [
                    jsonData.entityData
                ];

                applyLoadingState(STEP_LOKOMOTIVEN);
                window.locomotivesDlg?.updateLocomotives(locomotiveData);

                window.occLayer?.loadLocomotives(locomotiveData);
                window.occLayer?.updateLocomotives(locomotiveData);

            } else {
                console.log("Unknown entity type: " + jsonData.entityType);
                console.log("Command: " + jsonData.command) // in general "update"
                console.log("Data: " + jsonData.entityData);
            }
        }

        if (jsonData.routes != null) {
            applyLoadingState(STEP_ROUTEINFORMATIONEN);
            window.routes = jsonData.routes;
            window.routesDlg?.updateRoutes(window.routes);

            window.__initialData.routesData = true;
            checkMinimumSetOfDataAndRevitalizeUi();
        }

        if (jsonData.systemInfo) {
            window.systemInfo = jsonData.systemInfo;
            window.blocksDlg?.updateSystemInfo(window.systemInfo);
            window.stagingDlg?.updateSystemInfo(window.systemInfo);
            window.routesDlg?.updateSystemInfo(window.systemInfo);

            window.__initialData.systemInfoData = true;
            checkMinimumSetOfDataAndRevitalizeUi();
        }

        if (jsonData.settings) {
            if (jsonData.settings && jsonData.settings.uuid) {
                window.__planfieldUuid = jsonData.settings.uuid;
            }
            applyLoadingState(STEP_ALLGEMEINE_EINSTELLUNGEN);
            window.settingsInfo = jsonData.settings;
            window.__planfieldUuid = settingsInfo.uuid;
            window.occLayer?.updateSettings(jsonData.settings);
            window.blocksDlg?.updateSettings(jsonData.settings);
            window.stagingDlg?.updateSettings(jsonData.settings);
            window.routesDlg?.updateSettings(jsonData.settings);
            window.planfield?.updateSettings(jsonData.settings);
            window.accessoriesDlg?.updateSettings(jsonData.settings);

            if (jsonData.settings.blockSensors) {
                applyLoadingState(STEP_BLOCKINFORMATIONEN);
                window.blockSensors = jsonData.settings.blockSensors;
                window.blocksDlg?.updateEntries(window.blockSensors);

                window.__initialData.blockSensorData = true;
                // NOTE: note needed, is called few lines lower
                //checkMinimumSetOfDataAndRevitalizeUi();
            }

            if (jsonData.settings.debugging) {
                updateInitConsoleCheckboxes(jsonData.settings.debugging);
            }

            window.__initialData.settingsData = true;
            checkMinimumSetOfDataAndRevitalizeUi();
        }

        if (jsonData.workspace) {
            this.workspaceName = jsonData.workspace.name;
        }

        if (jsonData.stateData) {
            window.stateData = jsonData.stateData;
            updateStatusBarAutoModeInfo(jsonData.stateData)
            updateSidebar(jsonData.stateData);
            updateAutomodeRuntime();
        }
    }

    // #endregion

    __loadSideBar();
    __initPlanfieldBackgroundState();
    __initLabelState();
});

window.__initialData = {
    railyData: false,
    routesData: false,
    blockSensorData: false,
    systemInfoData: false,
    settingsData: false
};

window.__restoreDialogsCalled = false;
window.__revializationDone = false;

function checkMinimumSetOfDataAndRevitalizeUi() {
    if (window.__revializationDone === true) return;
    const allTrue = Object.values(window.__initialData).every(Boolean);
    if (allTrue === true) {

        restoreDialogs();

        //
        // highlight selected route after the images of any track are loaded (in case of page reload)
        //
        if (window.__routeToVisualizeAfterPlanfieldLoad && window.__routeToVisualizeAfterPlanfieldLoad.length > 0) {
            for (let i = 0; i < window.__routeToVisualizeAfterPlanfieldLoad.length; ++i) {
                setTimeout(function () {
                    const data = window.__routeToVisualizeAfterPlanfieldLoad[i];
                    window.planfield.activateRouteVisualization(data);
                }, 2500);
            }
        }

        //
        // set target for locomotive in route after all tracks are loaded (in case of page reload)
        //
        if (window.__destinationToVisualizeAfterPlanfieldLoad && window.__destinationToVisualizeAfterPlanfieldLoad.length > 0) {
            for (let i = 0; i < window.__destinationToVisualizeAfterPlanfieldLoad.length; ++i) {
                setTimeout(function () {
                    const data = window.__destinationToVisualizeAfterPlanfieldLoad[i];
                    window.occLayer.setDestination(data);
                    window.stagingDlg.setDestination(data);
                }, 2500);
            }
        }

        window.__revializationDone = true;
    }
}

function restoreDialogs() {
    if (window.__restoreDialogsCalled === true) return;

    restoreBlocksControls();
    restoreRoutesControls();
    restoreLocomotiveControls();
    restoreAccessoriesControls();

    setTimeout(function () {
        restoreLocomotiveControlsIndividual();
        window.occLayer?.updateSettings(window.settingsInfo);
    }, 2500);

    applyLoadingState(STEP_REKONSTRUKTION_SITZUNG);

    window.__restoreDialogsCalled = true;
}

// #region check if page was reloaded

function checkForPageReload() {
    window.__pageLastUnload = localStorage.getItem("pageLastUnload");
    if (window.__pageLastUnload) {
        const timeSinceUnload = Date.now() - window.__pageLastUnload;
        if (timeSinceUnload < 5000) {
            window.__pageReloaded = true;
        } else {
            //console.log("Seite wurde wahrscheinlich geschlossen und neu geöffnet.");
        }
    }
    localStorage.removeItem("pageLastUnload");
}

function handleStateAfterPageReload() {
    sendSystemCommand({
        command: 'initialize',
        "argument": "initialize",
        argumentValue: {
            initViews: true,
            initAccessories: false
        }
    });
}

// #endregion

// #region LeaveHandler

function initLeaveHandler() {
    window.addEventListener("beforeunload", function (event) {

        // Setze Flag für das Reload, so dass wir beim $(document).ready(..) den aktuellen Status abfragen müssen.
        localStorage.setItem("pageLastUnload", Date.now());

        //
        // Wir senden das Abschalten auf zwei Wegen:
        //   (a) mit einem System Command
        //   (b) über einen Post-Request
        //
        // (a)
        sendSystemCommand({
            command: 'emergencyStop',
            "argument": "emergencyStop",
            "argumentValue": "emergencyStop"
        });
        // (b)
        const token = window.__access_token;
        fetch(`${window.__httpProtocol}://${window.__urlHq}:${window.__urlPortHq}/api/workspace/emergencyStop`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ token: token })
        }).then(response => console.log("Ok:", response))
            .catch(error => console.error("Failed:", error));

        // Irgendwas müssen wir machen...
        // Wenn der Benutzer im AutoMode ist, dann kann hier wenigstens noch das Verlassen der Seite abgebrochen werden.
        // Doch leider ist das aktuell nicht wirklich in unserer Hand.
        if (window.stateData && window.stateData.automaticEnabled === true) {
            const message = "Automode ist gestartet, bitte beende diesen bevor du die Seite verlässt?";
            event.preventDefault();
            event.returnValue = message;
            return message;
        }
    });
}

// #endregion

// #region StatusBar

function forceStopAutomode() {
    sendAutoModeCommand({
        command: 'update',
        argument: 'forcestop',
        argumentValue: {}
    });

    const divCtrl = $('.automodeRuntime');
    if (divCtrl && divCtrl.length > 0) {
        divCtrl.hide();
    }

    const divCtrl2 = $('.automodeCancelForceContainer');
    if (divCtrl2 && divCtrl2.length > 0) {
        divCtrl2.hide();
    }
}

function updateAutomodeRuntime() {
    if (stateData.automaticEnabled === true || stateData.runningRoutes > 0) {
        const startedDt = stateData.startedDt;
        const startedDate = new Date(startedDt);
        const now = new Date();
        const deltaMs = now - startedDate;
        const deltaSeconds = Math.floor(Math.abs(deltaMs) / 1000);
        const hours = Math.floor(deltaSeconds / 3600);
        const minutes = Math.floor((deltaSeconds % 3600) / 60);
        const seconds = deltaSeconds % 60;
        const divCtrl = $('.automodeRuntime');
        if (divCtrl && divCtrl.length > 0) {
            if (!divCtrl.is(":visible"))
                divCtrl.show();
            let m3;
            if (hours > 0) {
                m3 = `<i class="fas fa-stopwatch"></i> ${hours} h ${minutes} m´Min ${seconds} Sek`;
            } else {
                m3 = `<i class="fas fa-stopwatch"></i> ${minutes} Min ${seconds} Sek`;
            }
            divCtrl.html(m3);
        }
    } else {
        if (stateData.runningRoutes === 0) {
            const divCtrl = $('.automodeRuntime');
            if (divCtrl && divCtrl.length > 0) {
                divCtrl.html('<i class="fas fa-stopwatch"></i> nicht gestartet');
                divCtrl.hide();
            }
        }
    }
}

let autoModeTimer = null;

function updateStatusBarAutoModeInfo(stateData) {
    if (!stateData) return;
    if (stateData.automaticEnabled === true && autoModeTimer === null) {
        autoModeTimer = setInterval(() => {
            updateAutomodeRuntime();
        }, 1000);
    }

    if (stateData.automaticEnabled === false && autoModeTimer !== null) {
        clearInterval(autoModeTimer);
        autoModeTimer = null;
        updateAutomodeRuntime();
    }
    const container = $('.automodeCancelForceContainer');

    if (stateData.automaticEnabled === false && stateData.runningRoutes > 0) {
        // zeige die allgemeinen Daten
        container.show();
    } else {
        // verstecke die allgemeinen Daten
        container.hide();
    }

    const divCtrl = $('.automodeInfo');
    if (!stateData.automaticEnabled && stateData.runningRoutes === 0) {
        divCtrl.text('');
        return;
    }

    const m0 = stateData.automaticEnabled === true ? 'Automode:ON' : 'Automode:OFF';
    const m1 = stateData.simulationEnabled === true ? 'Simulation:ON' : 'Simulation:OFF';
    const m2 = `Progressing routes:${stateData.runningRoutes}`;

    divCtrl.text(`${m0}, ${m1}, ${m2}`);
}

// #endregion

// #region Background in Planfield

function __initPlanfieldBackgroundState() {
    const planfieldBgShown = $.localStorage.getItem("planfieldBgShown") ?? null;
    if (!planfieldBgShown) return;

    try {
        const { shown } = JSON.parse(planfieldBgShown);
        $('#paintingarea-toggle').prop('checked', shown)
        togglePaintingArea(shown);
    } catch (error) {
        console.error("Error parsing planfieldBgShown:", error);
    }
}

// #endregion

// #region Labeling in Planfield

function __initLabelState() {
    const labelsShown = $.localStorage.getItem("labelShown") ?? null;
    if (!labelsShown) return;

    try {
        const { shown } = JSON.parse(labelsShown);
        window.labelShown = shown;
        $('#labels-toggle').prop('checked', shown)
        toggleAllLabelInformation(shown);
    } catch (error) {
        console.error("Error parsing labelShown:", error);
    }
}

// #endregion

// #region Debugging Output

function scrollDebugToEnd() {
    const debugMessages = document.getElementsByClassName("messageContainer");
    $(debugMessages).scroll();
    $(debugMessages).animate({
        scrollTop: debugMessages[0].scrollHeight
    }, "fast");
}

function initDebugConsole() {
    const self = this;
    const ctrlLoggingBtn = $('#statusBar div.logging');
    const ctrlDebugConsole = $('.debugConsole');
    let __dropdownTimer;

    ctrlDebugConsole.find('.clearDebug').click(function () {
        $('.messageContainer').html("");
    });
    ctrlDebugConsole.find('.scrollTop').click(function () {
        // scroll to top
        const debugMessages = document.getElementsByClassName("messageContainer");
        $(debugMessages).scroll();
        $(debugMessages).animate({
            scrollTop: 0
        }, "slow");
    });
    ctrlDebugConsole.find('.scrollBottom').click(function () {
        // scroll to bottom
        scrollDebugToEnd();
    });
    ctrlDebugConsole.find(".dropdown-btn").click(function (event) {
        event.stopPropagation();
        $(".dropdown-content").toggle();
    });
    ctrlDebugConsole.find(".dropdown-content input").change(function () {
        let selectedOptions = {};
        $(".dropdown-content input").each(function () {
            let key = $(this).val().replace("debug", ""); // "debugAccessories" -> "Accessories"
            key = key.charAt(0).toLowerCase() + key.slice(1); // "Accessories" -> "accessories"
            selectedOptions[key] = $(this).is(":checked"); // true oder false setzen
        });
        //addDebugText("Konfiguration: " + JSON.stringify(selectedOptions, null, 2));

        changeSetting({
            command: 'update',
            argument: 'debugging',
            argumentValue: selectedOptions
        });
    });
    ctrlDebugConsole.find(".dropdown").mouseleave(function () {
        __dropdownTimer = setTimeout(function () {
            $(".dropdown-content").fadeOut();
        }, 2000);
    });
    ctrlDebugConsole.find(".dropdown").mouseenter(function () {
        clearTimeout(__dropdownTimer);
    });

    ctrlDebugConsole.find(".wordwrapToggle").click(function () {
        let isWrapped = $(".messageContainer").toggleClass("wordwrap-disabled").hasClass("wordwrap-disabled");

        // Icon & Tooltip anpassen
        $(this).toggleClass("fa-align-left fa-align-justify")
            .attr("title", isWrapped ? "Wordwrap aus" : "Wordwrap an");
    });

    ctrlLoggingBtn.click(function () {
        if (ctrlDebugConsole.is(':visible')) {
            ctrlDebugConsole.hide();
        } else {
            ctrlDebugConsole.css({
                "bottom": ($('#statusBar').height() + 1) + "px",
                "right": $('#sidebar').width() + "px"
            });

            if (typeof window.__ctrlDebugConsoleInitialized === "undefined" ||
                window.__ctrlDebugConsoleInitialized == null ||
                window.__ctrlDebugConsoleInitialized === false) {
                window.__ctrlDebugConsoleInitialized = true;
                ctrlDebugConsole.resizable({
                    handles: "n, w",
                    minWidth: 300,
                    minHeight: 150,
                    stop: function (event, ui) {
                        realignDebugConsole();
                    }
                });
            }

            ctrlDebugConsole.show();

            scrollDebugToEnd();
        }
    });
}

function realignDebugConsole() {
    const ctrlDebugConsole = $('.debugConsole');
    ctrlDebugConsole.css({
        "inset": "",
        "bottom": ($('#statusBar').height() + 1) + "px",
        "right": $('#sidebar').width() + "px",
        "position": "absolute"
    });
}

function updateInitConsoleCheckboxes(jsonDebuggingData) {
    $.each(jsonDebuggingData, function (key, value) {
        let checkbox = $(".dropdown-content input[value='debug" + key.charAt(0).toUpperCase() + key.slice(1) + "']");
        checkbox.prop("checked", value);
    });
}

// TODO filter message level, i.e. prio [None, Info, Warn, Error, Fatal]
function __formatDebugMessage(msg, prio, datetime) {
    if (!msg) return;
    if (datetime)
        return `<span class="dt">${formatDateFromISO(datetime)}</span><span class="msg">${msg}</span>`;
    return `<span class="dt">${formatDateFromISO(new Date().toISOString())}</span><span class="msg">${msg}</span>`;
}

function addDebugText(txt) {
    const jsonData = {
        messages: [
            txt
        ]
    };

    addDebugMessages(jsonData);
}

function addDebugMessages(jsonData, targetClassName = "messageContainer") {

    const debugConsoles = document.getElementsByClassName(targetClassName);
    const targetCtrl = debugConsoles[0];
    const isBlank = function (str) {
        return (!str || /^\s*$/.test(str));
    }
    for (let i = 0; i < jsonData.messages.length; ++i) {
        const innerHtml = targetCtrl.innerHTML;

        const m = __formatDebugMessage(jsonData.messages[i], jsonData.priority, jsonData.datetime);
        if (m == null) continue;

        if (targetCtrl.innerHTML.length === 0 || isBlank(targetCtrl.innerHTML))
            targetCtrl.innerHTML = m;
        else
            targetCtrl.innerHTML = innerHtml + "<br>" + m;
    }
    targetCtrl.scrollTop = targetCtrl.scrollHeight;
}

// #endregion
