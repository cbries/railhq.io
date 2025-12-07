// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


// #region global Functions

function getBlockControlsUuid() {
    return window.__planfieldUuid + "_dialogStateBlocks";;
}

function restoreBlocksControls() {
    const n = getBlockControlsUuid();
    const ctrls = JSON.parse(localStorage.getItem(n)) || {};
    if (typeof ctrls !== "object" || ctrls === null) {
        // ignore
    } else {
        Object.keys(ctrls).forEach(accessor => {
            if (typeof accessor === "string") {
                const state = ctrls[accessor];
                if (state && state.open) {
                    const dlg = window.blocksDlg;
                    dlg.show();
                    $('#' + dlg.__dialogName).closest(".ui-dialog").css("z-index", state.zindex);
                    $("#" + dlg.__dialogName).off('dialogfocus');
                    $("#" + dlg.__dialogName).on('dialogfocus', function () {
                        dlg.__setDialogState(true);
                    });
                }
            }
        });
    }
}

// #endregion

// #region Part for Block & Accessory Repair

function repairBlockCallback(ctrlId) {

    // entferne die Warnung
    // wenn die Reparatur klappt, dann ist alles gut
    // ansonsten kommt die Warnung sowieso wieder
    const ctrl = $('#' + ctrlId).find('.entity-warning');
    if (ctrl) ctrl.remove();

    window.planfield.__trigger('automode', {
        command: 'repair',
        argument: 'block',
        argumentValue: {
            blockId: ctrlId
        }
    });
}

function repairAccessoryCallback(ctrlId) {

    // entferne die Warnung
    // wenn die Reparatur klappt, dann ist alles gut
    // ansonsten kommt die Warnung sowieso wieder
    const ctrl = $('#' + ctrlId).find('.entity-warning');
    if (ctrl) ctrl.remove();

    window.planfield.__trigger('automode', {
        command: 'repair',
        argument: 'accessory',
        argumentValue: {
            accessoryIdentifier: ctrlId
        }
    });
}

function showAccessoryRepairPrompt(accCtrlId) {
    if (!accCtrlId) return;

    const accCtrl = $('#' + accCtrlId);
    if (accCtrl) {
        const warningIcon = accCtrl.find('.entity-warning');
        if (!warningIcon) {
            alert("Keine automatische Reparatur erforderlich.");
        } else {
            //const warningMessage = warningIcon.attr("title");
            const errors = warningIcon.data("errors");
            let errorList = "<ul>";
            for (let i = 0; i < errors.length; ++i) {
                errorList += `<li>${errors[i]}</li>`
            }
            errorList += "</ul>";
            if (errors.length <= 0) {
                w2popup.open({
                    title: 'Automatische Schaltgetär-Reparatur',
                    modal: true,
                    body: `
                        <div style="display: flex; flex-direction: column; justify-content: center; align-items: center; padding: 20px; text-align: center;">
                            <p style="font-size: 16px; font-weight: bold; margin-bottom: 10px;">
                                Es liegen keine bekannten Probleme vor, die eine Reparatur erforderlich machen.
                            </p>
                        </div>`,
                    buttons: `
                    <div style="display: flex; justify-content: center;">
                        <button class="w2ui-btn" onclick="w2popup.close();" style="background-color: #ccc;">Ok</button>
                    </div>`
                });
            } else {
                w2popup.open({
                    title: 'Automatische Schaltgetär-Reparatur',
                    modal: true,
                    body: `
                    <div style="display: flex; flex-direction: column; justify-content: center; align-items: center; padding: 20px; text-align: center;">
                        <p style="font-size: 16px; font-weight: bold; margin-bottom: 10px;">
                            Soll ein Reparaturversuch durchgeführt werden?
                        </p>
                        <div style="background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; padding: 10px; border-radius: 5px; margin-bottom: 15px; text-align: left;">
                            <strong>Folgende Warnungen existieren:</strong><br>
                            ${errorList}
                        </div>
                    </div>`,
                    buttons: `
                    <div style="display: flex; justify-content: center;">
                        <button class="w2ui-btn" onclick="w2popup.close();" style="background-color: #ccc;">Abbruch</button>
                        <button class="w2ui-btn" onclick="repairAccessoryCallback('${accCtrlId}'); w2popup.close();" style="background-color: #28a745;">Ausführen</button>
                    </div>`
                });
            }
        }
    }
}

function showBlockRepairPrompt(blockCtrlId) {
    if (!blockCtrlId) return;

    const blockCtrl = $('#' + blockCtrlId);
    if (blockCtrl) {
        const warningIcon = blockCtrl.find('.entity-warning');
        if (!warningIcon) {
            alert("Keine automatische Reparatur erforderlich.");
        } else {
            const warningMessage = warningIcon.attr("title");
            if (!warningMessage) {
                w2popup.open({
                    title: 'Automatische Block-Reparatur',
                    modal: true,
                    body: `
                        <div style="display: flex; flex-direction: column; justify-content: center; align-items: center; padding: 20px; text-align: center;">
                            <p style="font-size: 16px; font-weight: bold; margin-bottom: 10px;">
                                Es liegen keine bekannten Probleme vor, die eine Reparatur erforderlich machen.
                            </p>
                        </div>`,
                    buttons: `
                    <div style="display: flex; justify-content: center;">
                        <button class="w2ui-btn" onclick="w2popup.close();" style="background-color: #ccc;">Ok</button>
                    </div>`
                });
            } else {
                w2popup.open({
                    title: 'Automatische Block-Reparatur',
                    modal: true,
                    body: `
                    <div style="display: flex; flex-direction: column; justify-content: center; align-items: center; padding: 20px; text-align: center;">
                        <p style="font-size: 16px; font-weight: bold; margin-bottom: 10px;">
                            Soll ein Reparaturversuch durchgeführt werden?
                        </p>
                        <div style="background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; padding: 10px; border-radius: 5px; margin-bottom: 15px;">
                            <strong>Folgende Warnungen existieren:</strong><br>
                            ${warningMessage}
                        </div>
                    </div>`,
                    buttons: `
                    <div style="display: flex; justify-content: center;">
                        <button class="w2ui-btn" onclick="w2popup.close();" style="background-color: #ccc;">Abbruch</button>
                        <button class="w2ui-btn" onclick="repairBlockCallback('${blockCtrlId}'); w2popup.close();" style="background-color: #28a745;">Ausführen</button>
                    </div>`
                });
            }
        }
    }
}

// #endregion

class SensorList {
    constructor() {
        console.debug(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__sensorList = [];
    }

    refreshSensorList(ctrlInstance) {
        try {
            const themeItemData = ctrlInstance.data(constDataThemeItemObject);
            if (!isFeedback(themeItemData.editor.themeId)) return false;

            const sensorId = ctrlInstance.attr("id");
            if (!this.__sensorList.includes(sensorId)) {
                this.__sensorList.push(sensorId);

                // Human-readable sort
                const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });
                this.__sensorList.sort(collator.compare);
            }

            return true;
        } catch (err) {
            console.error("Error in refreshSensorList:", err);
        }

        return false;
    }

    removeFromSensorList(ctrlIdentifier) {
        try {
            const iMax = this.__sensorList.length;

            for (let i = 0; i < iMax; i++) {
                if (this.__sensorList[i] === ctrlIdentifier) {
                    this.__sensorList.splice(i, 1);
                    return true;
                }
            }
        } catch (err) {
            console.error("Error in removeFromSensorList:", err);
        }

        return false;
    }
}

class SignalList {
    constructor() {
        console.debug(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__signalList = [];
    }

    refreshSignalList(ctrlInstance) {
        try {
            const themeItemData = ctrlInstance.data(constDataThemeItemObject);
            if (!isSignal(themeItemData.editor.themeId)) return false;

            const signalId = ctrlInstance.attr("id");
            if (!this.__signalList.includes(signalId)) {
                this.__signalList.push(signalId);

                // Human-readable sort
                const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });
                this.__signalList.sort(collator.compare);
            }

            return true;
        } catch (err) {
            console.error("Error in refreshSignalList:", err);
        }

        return false;
    }

    removeFromSignalList(ctrlIdentifier) {
        try {
            const iMax = this.__signalList.length;

            for (let i = 0; i < iMax; i++) {
                if (this.__signalList[i] === ctrlIdentifier) {
                    this.__signalList.splice(i, 1);
                    return true;
                }
            }
        } catch (err) {
            console.error("Error in removeFromSignalList:", err);
        }

        return false;
    }
}

window.__sensorList = new SensorList();
window.__signalList = new SignalList();

const COLUMN_SENSOR_ENTER = 2;
const COLUMN_SENSOR_IN = 3;
const COLUMN_SIGNAL = 4;
const COLUMN_VORSIGNAL = 5;

class Blocks {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__installed = false;
        this.winLocomotives = null;
        this.gridLocomotives = null;
        this.__gridName = "gridBlocks";
        this.__dialogName = "dialogBlocks";
        this.__storageNameGeometry = this.__dialogName + "_geometry";
        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);
        this.__initEventHandling();
        this.__systemInfo = null;
        this.__settingsInfo = null;
    }

    // #region Window Restore

    __setDialogState(open) {
        const n = getBlockControlsUuid();
        let dialogState = JSON.parse(localStorage.getItem(n)) || {};
        dialogState['dlgBlocks'] = {
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
        w2ui[this.__gridName].refresh();
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

    install() {
        const self = this;
        const state = this.isShown();
        if (state) return;
        if (this.__installed) return;
        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            closeOnEscape: false,
            autoOpen: false,
            dragStop: function (event, ui) {
                self.__setDialogState(true);
                self.__windowGeometry.save(ui.position, {
                    width: event.target.clientWidth,
                    height: event.target.clientHeight
                });
            },
            close: function (event, ui) {
                self.__setDialogState(false);

                const elGrid = w2ui[self.__gridName];
                elGrid.selectNone();
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Blocks and Sensors",
                multiSelect: false,
                show: {
                    toolbar: true,
                    footer: true,
                    toolbarAdd: false,
                    toolbarDelete: false,
                    toolbarEdit: false,
                    toolbarSave: false,
                    multiSelect: false
                },
                textSearch: 'contains',
                searches: [{ field: 'blockId', caption: 'Block', type: 'text' }],
                sortData: [{ field: 'blockId', direction: 'asc' }],
                columns: [
                    { field: 'recid', caption: 'ID', sortable: false, hidden: true },
                    {
                        field: 'blockId',
                        caption: 'Block',
                        size: '15%',
                        sortable: true
                    },
                    {
                        field: 'sensorEnter',
                        caption: 'Enter',
                        size: '10%',
                        sortable: true,
                        editable: __createEditableSensorList(window.__sensorList)
                    },
                    {
                        field: 'sensorIn',
                        caption: 'In',
                        size: '10%',
                        sortable: false,
                        editable: __createEditableSensorList(window.__sensorList)
                    },
                    {
                        field: 'signal',
                        caption: 'Signal (Abfahrt)',
                        size: '10%',
                        sortable: false,
                        editable: __createEditableSignalList(window.__signalList)
                    },
                    {
                        field: 'vorsignal',
                        caption: 'Vorsignal  (Abfahrt)',
                        size: '10%',
                        sortable: false,
                        editable: __createEditableSignalList(window.__signalList)
                    }
                ],
                records: [],
                onSelect: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    self.__trigger('highlight',
                        {
                            blockId: rec.blockId.split("[")[0],
                            sensorEnter: rec.sensorEnter,
                            sensorIn: rec.sensorIn,
                            signal: rec.signal,
                            vorsignal: rec.vorsignal
                        });
                    bringToFront(self.__dialogName);
                },
                onUnselect: function (ev) {
                    self.__trigger('unhighlight', {});
                },
                onChange: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    const column = ev.column;

                    if (column === COLUMN_SENSOR_ENTER) {
                        rec.sensorEnter = ev.value_new.text;
                    } else if (column === COLUMN_SENSOR_IN) {
                        rec.sensorIn = ev.value_new.text;
                    } else if (column === COLUMN_SIGNAL) {
                        rec.signal = ev.value_new.text;
                    } else if (column === COLUMN_VORSIGNAL) {
                        rec.vorsignal = ev.value_new.text;
                    }

                    if (column >= COLUMN_SENSOR_ENTER && column <= COLUMN_VORSIGNAL) {

                        self.__saveBlockSettings({
                            recid: rec.recid,
                            blockId: rec.blockId,
                            sensorEnterItemId: rec.sensorEnter,
                            sensorInItemId: rec.sensorIn,
                            signal: rec.signal,
                            vorsignal: rec.vorsignal
                        });

                        elGrid.refreshCell(rec.recid, 'sensorEnter');
                        elGrid.refreshCell(rec.recid, 'sensorIn');
                        elGrid.refreshCell(rec.recid, 'signal');
                        elGrid.refreshCell(rec.recid, 'vorsignal');

                        elGrid.save();

                        self.__trigger('highlight',
                            {
                                blockId: rec.blockId,
                                sensorEnter: rec.sensorEnter,
                                sensorIn: rec.sensorIn,
                                signal: rec.signal,
                                vorsignal: rec.vorsignal
                            });
                    }
                }
            });
        }

        function __createEditableSensorList(items) {
            return {
                type: 'list',
                items: items.__sensorList,
                filter: false
            };
        }

        function __createEditableSignalList(items) {
            return {
                type: 'list',
                items: items.__signalList,
                filter: false
            };
        }

        this.__installed = true;
    }

    // searches in this.__settingsInfo.sensors
    __getSensorByName(sensorName) {
        if (!sensorName) return null;
        if (!this.__settingsInfo?.sensors) return null;
        const sensor = this.__settingsInfo?.sensors.filter(sensor => sensor.name == sensorName);
        if (sensor && sensor.length > 0)
            return sensor[0];
        return null;
    }

    __saveFeedbackSettings(sensorData) {
        let data = {};
        if (sensorData.sensorInItemId) {
            data = {
                sensorEnter: {
                    id: sensorData.sensorEnterItemId,
                    addr: sensorData.sensorEnterAddr
                }
            }
        } else if (sensorData.sensorInItemId) {
            data = {
                sensorIn: {
                    id: sensorData.sensorInItemId,
                    addr: sensorData.sensorInAddr
                }
            }
        } else {
            return;
        }

        this.__trigger('setting', {
            command: 'update',
            argument: 'sensor',
            argumentValue: data
        });
    }

    __saveBlockSettings(blockData) {
        this.__trigger('setting', {
            command: 'update',
            argument: 'block',
            argumentValue: {
                blockIdentifier: blockData.blockId,
                sensorEnter: blockData.sensorEnterItemId,
                sensorIn: blockData.sensorInItemId,
                signal: blockData.signal,
                vorsignal: blockData.vorsignal
            }
        });
    }

    __refreshSensorCells() {
        const elGrid = w2ui[this.__gridName];
        if (elGrid) {
            const editableItems = window.__sensorList.__sensorList;
            elGrid.columns[COLUMN_SENSOR_ENTER].editable.items = editableItems;
            elGrid.columns[COLUMN_SENSOR_IN].editable.items = editableItems;
        } else {
            console.error(`Grid "${this.__gridName}" not found.`);
        }
    }

    __refreshSignalCells() {
        const elGrid = w2ui[this.__gridName];
        if (elGrid) {
            const editableItems = window.__signalList.__signalList;
            elGrid.columns[COLUMN_SIGNAL].editable.items = editableItems;
            elGrid.columns[COLUMN_VORSIGNAL].editable.items = editableItems;
        } else {
            console.error(`Grid "${this.__gridName}" not found.`);
        }
    }

    __refreshBlockColumn(ctrlInstance) {
        try {
            const themeItemData = ctrlInstance.data(constDataThemeItemObject);
            if (!isBlock(themeItemData.editor.themeId)) return false;

            const elGrid = w2ui[this.__gridName];
            if (elGrid) {
                const blockId = ctrlInstance.attr("id");
                const blockIdPlus = `${blockId}[+]`;
                const blockIdMinus = `${blockId}[-]`;

                const rec = elGrid.find({ blockId });

                // create new row when a new block control is created
                if (rec.length === 0) {
                    const n = elGrid.records.length;
                    elGrid.add({
                        recid: (1 + n),
                        blockId: blockIdPlus,
                        sensorEnter: '',
                        sensorIn: ''
                    });
                    elGrid.add({
                        recid: (2 + n),
                        blockId: blockIdMinus,
                        sensorEnter: '',
                        sensorIn: ''
                    });
                }
            } else {
                console.error(`Grid "${this.__gridName}" not found.`);
            }

            return true;
        } catch (err) {
            console.error("Error in __refreshBlockColumn:", err);
        }

        return false;
    }

    __removeBlock(ctrlIdentifier) {
        return true;
    }

    controlCreated(ctrlInstance) {
        if (window.__sensorList.refreshSensorList(ctrlInstance))
            this.__refreshSensorCells();

        if (window.__signalList.refreshSignalList(ctrlInstance))
            this.__refreshSignalCells();

        this.__refreshBlockColumn(ctrlInstance);
        // TODO do we need more update?
    }

    controlRemoved(ctrlIdentifier) {
        if (window.__sensorList.removeFromSensorList(ctrlInstance))
            this.__refreshSensorCells();

        this.__removeBlock(ctrlIdentifier);
        // TODO do we need more update?
    }

    clearGrid() {
        const elGrid = w2ui[this.__gridName];

        if (elGrid) {
            this.__recentSensorsData = [];
            elGrid.clear(true);
            elGrid.reset();
        } else {
            console.error(`Grid "${this.__gridName}" not found.`);
        }
    }

    updateEntries(blockSensors) {
        const elGrid = w2ui[this.__gridName];
        if (!elGrid) return;

        for (let i = 0; i < blockSensors.length; ++i) {
            try {
                const blockSensor = blockSensors[i];
                const blockId = blockSensor.identifier;
                const sensorEnter = blockSensor.sensorEnter;
                const sensorIn = blockSensor.sensorIn;
                const signal = blockSensor.signal;
                const vorsignal = blockSensor.vorsignal;

                const rec = elGrid.find({ blockId });
                if (rec.length === 0) continue;

                const row = elGrid.get(rec);
                row[0].sensorEnter = sensorEnter;
                row[0].sensorIn = sensorIn;
                row[0].signal = signal;
                row[0].vorsignal = vorsignal;

                elGrid.refreshCell(rec, 'sensorEnter');
                elGrid.refreshCell(rec, 'sensorIn');
                elGrid.refreshCell(rec, 'signal');
                elGrid.refreshCell(rec, 'vorsignal');

            } catch (err) {
                // ignore
            }
        }

        // do not use, avoid collapse of rows
        //elGrid.save();
    }

    updateSystemInfo(systemInfo) {
        if (!systemInfo) return;
        this.__systemInfo = systemInfo;

        // to be defined
    }

    updateSettings(settings) {
        if (!settings) return;
        this.__settingsInfo = settings;

        // to be defined
    }
}