// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// #region global Functions

function getRouteControlsUuid() {
    return window.__planfieldUuid + "_dialogStateRoutes";;
}

function restoreRoutesControls() {
    const n = getRouteControlsUuid();
    const ctrls = JSON.parse(localStorage.getItem(n)) || {};
    if (typeof ctrls !== "object" || ctrls === null) {
        // ignore
    } else {
        Object.keys(ctrls).forEach(accessor => {
            if (typeof accessor === "string") {
                const state = ctrls[accessor];
                if (state && state.open) {
                    const dlg = window.routesDlg;
                    dlg.show();
                    $('#' + dlg.__dialogName).closest(".ui-dialog").css("z-index", state.zindex);
                    $("#" + dlg.__dialogName).off("dialogfocus");
                    $("#" + dlg.__dialogName).on("dialogfocus", function () {
                        dlg.__setDialogState(true);
                    });
                }
            }
        });
    }
}

// #endregion

class Routes {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__installed = false;
        this.__gridName = "gridRoutes";
        this.__dialogName = "dialogRoutes";
        this.__storageNameGeometry = this.__dialogName + "_geometry";
        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);
        this.__recentRoutes = null;
        this.__settingsInfo = null;
        this.__initEventHandling();
    }

    // #region Window Restore

    __setDialogState(open) {
        const n = getRouteControlsUuid();
        let dialogState = JSON.parse(localStorage.getItem(n)) || {};
        dialogState['dlgRoutes'] = {
            open: open,
            zindex: parseInt($('#' + this.__dialogName).closest(".ui-dialog").css("z-index"))
        };
        localStorage.setItem(n, JSON.stringify(dialogState));
    }

    // #endregion

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

    show() {
        const el = $('#' + this.__dialogName);
        this.__windowGeometry.showWithGeometry(el);
        if (!this.__installed) this.install();
        el.dialog("open");
        w2ui[this.__gridName]?.refresh();
    }

    isShown() {
        try {
            return $('#' + this.__dialogName).dialog('isOpen');
        } catch (error) {
            // ignore
        }
        return false;
    }

    close() {
        this.__setDialogState(false);

        const dialogElement = $('#' + this.__dialogName);
        dialogElement.dialog("close");
    }

    noOfRoutes() {
        const grid = w2ui[this.__gridName];
        if (!grid) return 0;
        const records = grid.records;
        if (!records) return 0;
        return records.length;
    }

    __removeAllHighlights() {
        const planfield = window.planfield;
        if (planfield) {
            planfield.clearRouteUserHighlight();
        }
    }

    install(options = {}) {
        if (this.__installed) return;

        const self = this;
        const state = this.isShown();
        if (state) return;

        const geometry = this.__windowGeometry.recent();
        const dialogElement = $("#" + this.__dialogName);

        dialogElement.dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            closeOnEscape: false,
            autoOpen: false,
            resizeStop(event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop(event, ui) {
                self.__setDialogState(true);
                self.__windowGeometry.save(ui.position, {
                    width: event.target.clientWidth,
                    height: event.target.clientHeight
                });
            },
            close() {
                self.__removeAllHighlights();
                self.__setDialogState(false);

                const grid = w2ui[self.__gridName];
                grid.selectNone();
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Routes",
                multiSelect: false,
                show: {
                    //lineNumbers: true,
                    toolbar: true,
                    //header: true,
                    footer: true,
                    toolbarAdd: false,
                    toolbarDelete: false,
                    toolbarEdit: false,
                    toolbarSave: false,
                    multiSelect: false
                },
                textSearch: 'contains',
                recid: 'routeId', // rename "recid" to "routeId"
                searches: [
                    { field: 'name', caption: 'Name', type: 'text' }
                ],
                sortData: [{ field: 'routeId', direction: 'asc' }],
                columns: [
                    { field: 'routeId', caption: 'Route ID', sortable: false, hidden: true },
                    { field: 'native', caption: 'Native', sortable: true, hidden: true },
                    { field: 'name', caption: 'Name', size: '20%', sortable: true },
                    { field: 'uid', caption: 'Uid', sortable: true, hidden: true },
                    {
                        field: 'isDisabled',
                        caption: 'Deaktiviert',
                        size: '10%',
                        style: 'text-align: center',
                        editable: {
                            type: 'checkbox',
                            style: 'text-align: center'
                        }
                    },
                    { field: 'switches', caption: 'Weichen', size: '10%', sortable: false },
                    { field: 'sensors', caption: 'Sensoren', size: '10%', sortable: false },
                    { field: 'signals', caption: 'Signale', size: '10%', sortable: false },
                    { field: 'tracks', caption: 'Gleisstücke', size: '10%', sortable: false }
                ],
                records: [],
                onSelect: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    const planfield = window.planfield;
                    if (planfield) {
                        planfield.clearRouteUserHighlight();
                        planfield.activateRouteUserHighlight(rec.native);
                    }
                    bringToFront(self.__dialogName);
                },
                onUnselect: function (ev) {
                    self.__removeAllHighlights();
                }
            });

            const gridTb = w2ui[self.__gridName].toolbar;
            if (gridTb) {

                /**
                 * Adds a button to the toolbar to simulate route checks.
                 * When clicked, all selected routes in the grid are processed,
                 * and a simulation command ("check") is triggered for each route to
                 * simulate accessory changes required for the train to reach its destination.
                 */
                gridTb.insert('search', {
                    type: 'button',
                    id: 'itemCheckRoute',
                    text: w2utils.lang('Check'),
                    img: 'fas fa-check',
                    checked: false,
                    tooltip: () => w2utils.lang('Simulates the route by changing any relevant accessory to let the trains reach their destination.'),
                    onClick: function () {
                        const elGrid = w2ui[self.__gridName];
                        const selectedRoutes = elGrid.getSelection();

                        selectedRoutes.forEach(recid => {
                            const route = elGrid.get(recid);
                            self.__trigger("checkRoute", {
                                command: 'routing',
                                argument: 'check',
                                argumentValue: route.native
                            });
                        });
                    }
                });


                /**
                 * Adds a "Save" button to the toolbar.
                 *
                 * This button allows users to save any modifications made to the grid and send
                 * them to the server for processing. When clicked:
                 *
                 * - It retrieves the changes made in the grid (e.g., toggling a "disabled" state).
                 * - For each change, it updates the corresponding grid row and triggers an event
                 *   to notify the server of the modification.
                 * - The grid cell is refreshed to reflect the updated state.
                 * - After processing all changes, the grid's changed state is cleaned up.
                 */
                gridTb.add({
                    type: 'button',
                    id: 'cmdSave',
                    text: w2utils.lang('Save'),
                    img: 'fas fa-save',
                    tooltip: () => w2utils.lang('Applies all changes to the server.'),
                    onClick: function () {
                        const elGrid = w2ui[self.__gridName];
                        const changes = elGrid.getChanges();
                        changes.forEach(change => {
                            const { recid, isDisabled } = change;
                            if (isDisabled != null) {
                                const row = elGrid.get(recid);
                                row.isDisabled = isDisabled;

                                elGrid.refreshCell(row, 'isDisabled');

                                self.__trigger("setting", {
                                    command: 'routeDisabled',
                                    argument: 'routeDisabled',
                                    argumentValue: {
                                        name: row.name,
                                        uid: row.uid,
                                        state: isDisabled
                                    }
                                });
                            }
                        });

                        self.__cleanupChangedState();
                    }
                });
            }

            this.updateSystemInfo(this.__systemInfo);

            this.__installed = true;
        }

        this.__installed = true;
    }

    __cleanupChangedState() {
        const self = this;
        $('#' + this.__dialogName + ' td.w2ui-grid-data').each(function () {
            $(this).removeClass('w2ui-changed');
        });
    }

    updateRoutes(routes) {
        if (!routes) return;
        this.__recentRoutes = routes;
        const elGrid = w2ui[this.__gridName];
        if (!elGrid) return;

        this.clearGrid();

        const listOfObjectsToAdd = [];
        let routeId;
        const iMax = routes.length;
        for (routeId = 0; routeId < iMax; ++routeId) {
            const recs = elGrid.find({ routeId: routeId });
            const route = routes[routeId];
            const name = route.name;
            const uid = route.uid;

            let isDisabled = false;
            const routeSystemInfo = this.__getRouteSystemInfo(uid);
            if (routeSystemInfo) isDisabled = !routeSystemInfo.isEnabled;

            if (recs.length <= 0) {
                listOfObjectsToAdd.push({
                    routeId: routeId,
                    native: route,
                    name: name,
                    uid: uid,
                    isDisabled: isDisabled,
                    switches: route.switches?.length ?? 0,
                    sensors: route.sensors?.length ?? 0,
                    signals: route.signals?.length ?? 0,
                    tracks: route.tracks?.length ?? 0
                });
            } else {
                //
                // update available entry
                //
                const recid = recs[0];
                const row = elGrid.get(recid);

                if (row.isDisabled !== isDisabled) {
                    row.isDisabled = isDisabled;
                    elGrid.refreshCell(routeId, 'isDisabled');
                }
            }
        }

        if (listOfObjectsToAdd.length > 0)
            elGrid.add(listOfObjectsToAdd);
    }

    /**
     * Queries the route information in this.__systemInfo based on the routeUid.
     * @param {any} routeUid
     */
    __getRouteSystemInfo(routeUid) {
        if (!routeUid) return null;
        if (!this.__settingsInfo?.routes) return null;

        const routeOfInterest = this.__settingsInfo.routes.filter(routeInfo => routeInfo.uid === routeUid);
        if (!routeOfInterest || routeOfInterest.length !== 1) return null;
        return routeOfInterest[0];
    }

    getRouteByName(routeName) {
        if (!routeName) return null;
        if (!w2ui[this.__gridName]) return null;
        const grid = w2ui[this.__gridName];
        const rec = grid.find({ name: routeName });
        if (rec && rec.length > 0) return grid.get(rec[0]);
        return null;
    }

    clearGrid() {
        const self = this;
        if (typeof window.routes !== "undefined" && window.routes != null) {
            window.routes = [];
        }
        const elGrid = w2ui[self.__gridName];
        if (typeof elGrid === "undefined" || elGrid == null) return;
        elGrid.clear(true);
        elGrid.reset();
    }

    updateSystemInfo(systemInfo) {
        if (!systemInfo) return;
        this.__systemInfo = systemInfo;

        // to be defined
    }

    updateSettings(settings) {
        if (!settings) return;
        this.__settingsInfo = settings;

        // clears and rebuild list
        if (this.__recentRoutes)
            this.updateRoutes(this.__recentRoutes);

        // applies settings
        if (this.__settingsInfo?.routes) {
            const routeN = this.__settingsInfo?.routes.length;
            for (let routeIdx = 0; routeIdx < routeN; ++routeIdx) {
                const route = this.__settingsInfo?.routes[routeIdx];
                if (!route) continue;
                const routeName = route.name;
                const routeEnabled = route.isEnabled;
                let recRoute = this.getRouteByName(routeName);
                if (recRoute) {
                    recRoute.isDisabled = !routeEnabled;
                    const elGrid = w2ui[this.__gridName];
                    if (elGrid) {
                        elGrid.refreshCell(recRoute.routeId, 'isDisabled');
                    }
                }
            }
        }


    }
}