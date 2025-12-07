// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


window.__sidebarRailyData = {};

if (!window.__stateDataRecent) {
    window.__stateDataRecent = {};
}

function updateSidebar(stateData) {
    if (stateData) {
        const sidebar = w2ui['sidebar'];

        //
        // update cmdAutomatic
        //
        const cmdAutomatic = sidebar.get('cmdAutomatic');
        const newAutoText = stateData.automaticEnabled
            ? 'Automatikbetrieb <i class="fas fa-stop-circle" style="color: red;"></i>'
            : 'Automatikbetrieb <i class="fas fa-play-circle" style="color: green;"></i>';

        if (cmdAutomatic.text !== newAutoText) {  // Nur aktualisieren, wenn sich der Text ändert
            cmdAutomatic.text = newAutoText;
            sidebar.refresh(cmdAutomatic.id);
        }

        // Statusbar-Animation nur ändern, wenn notwendig
        const autoModeDiv = $('#statusBar div.autoMode');
        if (stateData.automaticEnabled && !autoModeDiv.hasClass("fa-spin")) {
            autoModeDiv.addClass("fa-spin");
        } else if (!stateData.automaticEnabled && autoModeDiv.hasClass("fa-spin")) {
            autoModeDiv.removeClass("fa-spin");
        }

        // Countdown-Stop nur ausführen, wenn sich der Zustand geändert hat
        if (stateData.automaticEnabled !== window.__stateDataRecent.automaticEnabled) {
            if (!stateData.automaticEnabled) {
                window.occLayer.__stopAllCountdowns();
            }
        }

        //
        // update cmdSimulation
        //
        const cmdSimulation = sidebar.get('cmdSimulation');
        const newSimText = stateData.simulationEnabled ? "Simulation ✅" : "Simulation";

        if (cmdSimulation.text !== newSimText) {  // Nur aktualisieren, wenn sich der Text ändert
            cmdSimulation.text = newSimText;
            sidebar.refresh(cmdSimulation.id);
        }

        // Menüpunkt nur öffnen, wenn sich der Simulationsstatus ändert
        if (stateData.simulationEnabled && !window.__stateDataRecent.simulationEnabled) {
            sidebar.expand('levelSimulation');
        }

        // Speichere den aktuellen Zustand für den nächsten Vergleich
        window.__stateDataRecent = { ...stateData };
    }
}

function __loadSideBar() {
    const sidebarElement = $('#sidebar');
    const sidebarConfig = {
        name: 'sidebar',
        flatButton: true,
        nodes: [
            {
                id: 'level-1',
                text: 'Kontrolle',
                img: 'icon-folder',
                expanded: true,
                group: true,
                groupShowHide: false,
                nodes: [
                    { id: 'cmdOpenLocomotives', text: 'Lokomotiven', icon: 'fa fa-train' },
                    { id: 'cmdAccessories', text: 'Schaltartikel', icon: 'fas fa-magic' },
                    { id: 'cmdAutomatic', text: 'Automatikbetrieb', icon: 'fas fa-robot fa-red' }
                ]
            },
            {
                id: 'levelSystem',
                text: 'System',
                img: 'icon-folder',
                expanded: true,
                group: true,
                groupShowHide: false,
                nodes: [
                    {
                        id: 'cmdPower',
                        text: 'Stationen <span id="infoPowerConnections"></span>',
                        icon: 'fas fa-network-wired',
                        expanded: true,
                        nodes: [
                            {
                                id: 'cmdPowerEcos',
                                text: 'ECoS',
                                tooltip: 'Hier kannst du die Konfiguration ändern',
                                icon: 'fas fa-exclamation-circle'
                            },
                            {
                                id: 'cmdPowerZ21',
                                text: 'Z21',
                                icon: 'fas fa-exclamation-circle'
                            },
                            {
                                id: 'cmdPowerDemo',
                                text: 'Demo',
                                icon: 'fas fa-exclamation-circle'
                            }
                        ]
                    },
                    { id: 'cmdStop', text: 'Alle Züge stoppen', icon: 'fas fa-stop-circle' },
                    { id: 'cmdStopScripts', text: 'Alle Skripte stoppen', icon: 'fas fa-file-alt' },
                    { id: 'cmdShutdown', text: 'Herunterfahren', icon: 'fas fa-times-circle' }
                ]
            },
            {
                id: 'level-2',
                text: 'Administration',
                img: 'icon-folder',
                expanded: false,
                group: true,
                groupShowHide: true,
                nodes: [
                    { id: 'cmdEditPlan', text: 'Layout', icon: 'fa fa-edit' },
                    { id: 'cmdOpenBlocks', text: 'Blöcke', icon: 'fas fa-traffic-light' },
                    { id: 'cmdOpenRoutes', text: 'Routen', icon: 'fa fa-route' },
                    { id: 'cmdAnalyzeRoutes', text: 'Routenanalyse...', icon: 'fas fa-diagnoses' },
                    { id: 'cmdInitialize', text: 'Initialisierung...', icon: 'fas fa-rocket' },
                ]
            },
            {
                id: 'levelSimulation',
                text: 'Simulation',
                img: 'icon-folder',
                expanded: false,
                group: true,
                groupShowHide: true,
                nodes: [
                    { id: 'cmdSimulation', text: 'Simulate', icon: 'fas fa-cogs' }
                ]
            },
            {
                id: 'level-3',
                text: 'Hilfe',
                img: 'icon-folder',
                expanded: false,
                group: true,
                groupShowHide: true,
                nodes: [
                    { id: 'cmdHelp', text: 'Hilfe', icon: 'fas fa-question-circle fa-external-link-alt' },
                    { id: 'cmdAbout', text: 'Über uns', icon: 'fas fa-address-card fa-external-link-alt' }
                ]
            },
            {
                id: 'levelHQ',
                text: 'HQ',
                img: 'icon-folder',
                expanded: true,
                group: true,
                groupShowHide: false,
                nodes: [
                    { id: 'cmdGoToIndex', text: 'Übersicht', icon: 'fas fa-home' },
                    { id: 'cmdGoToWorkspaces', text: 'Arbeitsbereiche', icon: 'fas fa-code-branch' },
                    { id: 'cmdGoToProfil', text: 'Profil', icon: 'fas fa-user' },
                ]
            }
        ],
        onFlat: function (event) {
            sidebarElement.css('width', event.goFlat ? '35px' : '200px');
            realignDebugConsole();
        },
        onClick: function (event) {

            if (event.target === 'cmdGoToIndex') {
                window.location.href = window.__indexUrl + '/dashboard';
            } else if (event.target === 'cmdGoToWorkspaces') {
                window.location.href = window.__indexUrl + '/workspaces/overview';
            } else if (event.target === 'cmdGoToProfil') {
                window.location.href = window.__indexUrl + '/profile/edit';
            } else {
                handleSidebarClick(event.target, event.node);
            }
        },
        onExpand: function (event) {
            if (event.target === 'cmdPower') {
                event.onComplete = function () {
                    setTimeout(() => {
                        window.updateSidebarPower();
                    },
                        250);
                }
            }
        }
    };

    sidebarElement.w2sidebar(sidebarConfig);

    function handleSidebarClick(target, node) {
        const sidebar = w2ui['sidebar'];

        if (node && node.nodes && node.nodes.length > 0) {
            sidebar.toggle(target)
            setTimeout(() => { sidebar.unselect(target); }, 250);
            return;
        }

        setTimeout(() => { sidebar.unselect(target); }, 250);

        switch (target) {
            case "cmdOpenLocomotives":
                if (locomotivesDlg.isShown())
                    locomotivesDlg.close();
                else
                    locomotivesDlg.show();
                break;
            case "cmdAccessories":
                if (accessoriesDlg.isShown())
                    accessoriesDlg.close();
                else
                    accessoriesDlg.show();
                break;
            case "cmdAutomatic":
                {
                    if (!window.stateData) window.stateData = {};
                    if (!window.stateData.automaticEnabled)
                        window.stateData.automaticEnabled = false;

                    let enabledCommandStations = 0;
                    if (window.__sidebarRailyData?.ecosbase?.status && window.__sidebarRailyData?.ecosbase?.status === "STOP") {
                        ++enabledCommandStations;
                    }
                    if (window.__sidebarRailyData?.z21base?.status && window.__sidebarRailyData?.z21base?.trackOn == true) {
                        ++enabledCommandStations;
                    }

                    // Wenn wir die Simulation angeschaltet haben, 
                    // dann muss nicht geprüft werden ob eine CommandStation aktiv ist.
                    if (!stateData.simulationEnabled || stateData.simulationEnabled === false) {
                        if (window.__sidebarRailyData?.ecosbase && window.stateData.automaticEnabled === false) {
                            if (enabledCommandStations === 0) {
                                w2alert(
                                    'Keine Zentrale ist im GO-Modus.<br>Automatik wird nicht aktiviert.<br><br>Für Tests aktiviere einfach den Simulationsmodus.',
                                    'Zentrale nicht aktiv');
                                return;
                            }
                        }
                    }

                    // Weiteres zur vorherigen Prüfung, es ist eher so,
                    // dass wir die CommandStation eher stoppen/abschalten
                    // sollten, so dass keine Fahrzeug im Simulationsmodus
                    // fahren und es zu Defekten kommen kann.
                    if (stateData.simulationEnabled === true) {
                        if (enabledCommandStations > 0) {
                            w2alert(
                                'Mindestens eine Zentrale (ECoS oder Z21) ist aktiv.<br>Bitte deaktiviere alle Zentralen für den Simulationsmodus, um Schäden an deiner Anlage zu vermeiden.',
                                'Zentralen bitte abschalten');
                            return;
                        }
                    }

                    // Wir prüfen ob überaupt Routen vorhanden sind.
                    // Wenn nicht, wozu dann in den Automatikmodus wechseln?
                    if (window.routesDlg) {
                        const noOfRoutes = window.routesDlg.noOfRoutes();
                        if (noOfRoutes === 0) {
                            w2alert('Bitte lege zunächst mindestens eine Route an, bevor du den Automatikmodus aktivierst.', 'Keine Routen gefunden');
                            return;
                        }
                    }

                    const promptMessage = window.stateData.automaticEnabled === true
                        ? 'Du schaltest den Automatikbetrieb ab. Bitte beachte, dass die aktuell fahrenden Züge noch ihre Endposition erreichen müssen. Warte daher nach dem Abschalten, bis alle Züge ihr Ziel erreicht haben, bevor du die Webseite verlässt.'
                        : 'Du bist dabei, den Automatikbetrieb zu aktivieren.<br><br><b>Bitte bestätigen!</b>';

                    w2confirm(promptMessage, 'Automaikbetrieb')
                        .yes(function () {
                            if (window.stateData.automaticEnabled === true) { // switch off AutoMode
                                sendAutoModeCommand({
                                    argument: 'stop',
                                    argumentValue: ''
                                })

                            } else if (window.stateData.automaticEnabled === false) { // switch on AutoMode
                                sendAutoModeCommand({
                                    argument: 'start',
                                    argumentValue: ''
                                })
                            }
                        })
                        .no(function () {
                            // ignore
                        });
                }
                break;

            // #region Simulation

            case "cmdSimulation":
                {
                    if (!window.stateData ||
                        !window.stateData.simulationEnabled) {

                        w2alert(
                            '\nDer Simulationsmodus wird aktiviert und bleibt während des Automatikbetriebs aktiv. Damit die Änderung wirksam wird, muss der Automatikbetrieb ebenfalls eingeschaltet sein. Sobald der Automatikbetrieb beendet wird, wird der Simulationsmodus automatisch deaktiviert. Seine Einstellung wird nicht gespeichert.',
                            'Simulationsmodus').ok(function () {

                                if (!window.stateData)
                                    window.stateData = {};
                                window.stateData.simulationEnabled = true;
                                sendAutoModeCommand({
                                    argument: 'simulationMode',
                                    argumentValue: window.stateData.simulationEnabled
                                })

                            });

                    } else {

                        if (!window.stateData)
                            window.stateData = {};
                        window.stateData.simulationEnabled = false;
                        sendAutoModeCommand({
                            argument: 'simulationMode',
                            argumentValue: window.stateData.simulationEnabled
                        })
                    }

                }
                break;

            // #endregion

            // #region Administration

            case "cmdOpenBlocks":
                if (window.blocksDlg.isShown())
                    window.blocksDlg.close();
                else
                    window.blocksDlg.show();
                break;
            case "cmdOpenRoutes":
                if (routesDlg.isShown())
                    routesDlg.close();
                else
                    routesDlg.show()
                break;
            case "cmdEditPlan":
                if (!window.toolbox.isShown())
                    startEditMode();
                else
                    closeEditMode();
                break;
            case "cmdAnalyzeRoutes":
                handleAnalyzeRoutes(node);
                break;

            // #endregion

            // #region System

            case "cmdInitialize":
                handleInitialize();
                break;

            //case "cmdPower":
            //    sendSystemCommand({
            //        command: 'update',
            //        argument: 'power',
            //        argumentValue: 'toggle'
            //    });
            //    break;

            // #region Power

            // ECoS
            case "cmdPowerEcos":
                {
                    sendSystemCommand({
                        command: 'update',
                        argument: 'power',
                        argumentValue: {
                            driverName: 'ecos',
                            action: 'toggle'
                        }
                    });
                }
                break;
            // Z21
            case "cmdPowerZ21":
                {
                    sendSystemCommand({
                        command: 'update',
                        argument: 'power',
                        argumentValue: {
                            driverName: 'z21',
                            action: 'toggle'
                        }
                    });
                }
                break;
            // Demo
            case "cmdPowerDemo":
                {
                    sendSystemCommand({
                        command: 'update',
                        argument: 'power',
                        argumentValue: {
                            driverName: 'demo',
                            action: 'toggle'
                        }
                    });
                }
                break;

            // #endregion

            case "cmdStop":
                sendSystemCommand({
                    command: 'update',
                    argument: 'power',
                    argumentValue: 'stopAllTrains'
                });
                break;

            case "cmdStopScripts":
                window.hqScriptRunner.abortAllScripts();
                break;

            case "cmdShutdown":
                handleShutdown(node);
                break;

            // #endregion

            case "cmdHelp":
                window.open(constGitWikiWebsite, "_blank");
                break;
            case "cmdAbout":
                window.open(constAboutWebsite, "_blank");
                break;
            default:
                console.warn(`Unhandled sidebar action: ${target}`);
        }

        setTimeout(() => { sidebar.unselect(target); }, 250);
    }

    function handleInitialize(eventNode) {

        w2confirm(
            `<p>Do you like to initialize your model railway?</p>
                <label style="display: block; margin-bottom: 5px;">
                    <input type="checkbox" id="chk-init-views" checked> Aktualisiere Fuhrpark, Weichen/Signale, etc.
                </label>
                <label style="display: block;">
                    <input type="checkbox" id="chk-init-accessories"> Schalte alle Weichen/Signale für Ausgangsstellung
                </label>
            `, "Initialize")

            .yes(function () {
                const initViews = document.getElementById('chk-init-views').checked;
                const initAccessories = document.getElementById('chk-init-accessories').checked;

                sendSystemCommand({
                    command: 'initialize',
                    argument: 'initialize',
                    argumentValue: {
                        initViews: initViews,
                        initAccessories: initAccessories
                    }
                });
            })
            .no(function () {
                console.log('Canceled');
            });

    }

    function handleShutdown(eventNode) {
        w2confirm('Möchtest du deine Modellbahn herunterfahren?', 'Herunterfahren')
            .yes(function () {
                sendSystemCommand({
                    command: 'update',
                    "argument": "power",
                    "argumentValue": "shutdown"
                });
            })
            .no(function () {
                // do nothing
            });
    }

    function handleAnalyzeRoutes(eventNode) {
        w2confirm('Möchtest du jetzt alle Fahrwege analysieren?<br><br>Vorhandene Deaktivierungen bleiben bestehen, neue Fahrwege werden hinzugefügt und nicht mehr vorhandene entfernt.',
                'Fahrwege analysieren')
            .yes(function () {

                // reset current list of routes
                window.routesDlg.clearGrid();

                // do not clear block list
                // not really relevant for analyzing
                // window.blocksDlg.clearGrid();

                sendRoutingCommand({
                    argument: 'function',
                    argumentValue: 'analyzeRoutes'
                });
            })
            .no(function () {
                // do nothing
            });
    }
}

function closeEditMode() {
    const fncToggleLocInfos = (state) => toggleAllLocomotiveInformation(state);
    const { planfield, toolbox } = window;

    if (!$('#grid-toggle').is(':checked'))
        toggleGrid(false);

    window.editState = false;
    planfield.setEditMode(false);
    fncToggleLocInfos(true);
    toolbox.hideToolbox();
}

function startEditMode() {
    const fncToggleLocInfos = (state) => toggleAllLocomotiveInformation(state);
    const { planfield, toolbox } = window;

    //$('#grid-toggle').prop('checked', true);

    toggleGrid(true);

    window.editState = true;
    planfield.setEditMode(true);
    fncToggleLocInfos(false);
    toolbox.showToolbox();
}
