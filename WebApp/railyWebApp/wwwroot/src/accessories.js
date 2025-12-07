// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


// #region global Functions

const constDialogSerializationAccessoriesName = window.__planfieldUuid + "_dialogStateAcessories";
const ColumnNoAssignedVisItem = 6;
function restoreAccessoriesControls() {
    const ctrls = JSON.parse(localStorage.getItem(constDialogSerializationAccessoriesName)) || {};
    if (typeof ctrls !== "object" || ctrls === null) {
        // ignore
    } else {
        Object.keys(ctrls).forEach(accessor => {
            if (typeof accessor === "string") {
                const state = ctrls[accessor];
                if (state && state.open) {
                    const dlg = window.accessoriesDlg;
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
class Accessories {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__installed = false;
        this.__gridName = "gridAccessories";
        this.__dialogName = "dialogAccessories";
        this.__storageNameGeometry = this.__dialogName + "_geometry";
        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);
        this.__initEventHandling();
        this.__settingsInfo = null;
        this.__recentlyHighlighted = {};

        this.__driverList = [
            { id: 1, text: 'z21', caption: 'z21' }
        ];
        this.__protocolList = [
            { id: 1, text: 'DCC', caption: 'DCC' },
            { id: 2, text: 'MM', caption: 'MM' }
        ];
    }

    __getIndexOfList(items, pattern) {
        for (let i = 0; i < items.length; ++i) {
            const itm = items[i];
            if (itm.text === pattern) return itm.id;
            if (itm.caption === pattern) return itm.id;
        }
        return 0;
    }

    // #region Window Restore

    __setDialogState(open) {
        let dialogState = JSON.parse(localStorage.getItem(constDialogSerializationAccessoriesName)) || {};
        dialogState['dlgAccessories'] = {
            open: open,
            zindex: parseInt($('#' + this.__dialogName).closest(".ui-dialog").css("z-index"))
        };
        localStorage.setItem(constDialogSerializationAccessoriesName, JSON.stringify(dialogState));
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

    getRecords() {
        const elGrid = w2ui[this.__gridName];
        return elGrid.records;
    }

    getAccessoryRecord(driverName, oid) {
        if (oid <= 0) return null;
        if (!w2ui[this.__gridName]) return null;
        const grid = w2ui[this.__gridName];
        const rec = grid.find({ driverName: driverName, oid: oid });
        if (rec && rec.length > 0) return grid.get(rec[0]);
        return null;
    }

    getAccessoryRecordByIdentifier(driverName, identifier) {
        if (!identifier || identifier.length <= 0) return null;
        if (!w2ui[this.__gridName]) return null;
        const grid = w2ui[this.__gridName];
        const rec = grid.find({ driverName: driverName, identifier: identifier });
        if (rec && rec.length > 0) return grid.get(rec[0]);
        return null;
    }

    isDriverAvailable(driverName) {
        if (!driverName || driverName.length <= 0) return null;
        if (!w2ui[this.__gridName]) return null;
        const grid = w2ui[this.__gridName];
        const rec = grid.find({ driverName: driverName });
        return rec && rec.length > 0;
    }

    install() {
        const self = this;
        const state = this.isShown();
        if (state) return;

        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            closeOnEscape: false,
            autoOpen: false,
            resizeStop: function (event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop: function (event, ui) {
                self.__setDialogState(true);
                self.__windowGeometry.save(ui.position, {
                    width: event.target.clientWidth,
                    height: event.target.clientHeight
                });
            },
            close: function () {
                self.__setDialogState(false);

                const elGrid = w2ui[self.__gridName];
                elGrid.selectNone();
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Accessories",
                show: {
                    //lineNumbers: true,
                    toolbar: true,
                    //header: true,
                    footer: true,
                    toolbarAdd: false,
                    toolbarDelete: false,
                    toolbarEdit: false,
                    toolbarSave: false
                },
                textSearch: 'contains',
                recid: 'accessoryId', // rename "recid" to "accessoryId"
                searches: [
                    { field: 'identifier', caption: 'Identifizierer', type: 'text' }
                ],
                sortData: [{ field: 'identifier', direction: 'asc' }],
                onSort: function (event) {
                    const sortField = this.sortData[0]?.field;
                    const sortDirection = this.sortData[0]?.direction;
                    if (sortField === 'identifier') {
                        const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });
                        this.records.sort((a, b) => {
                            return collator.compare(a.identifier, b.identifier) * (sortDirection === 'asc' ? 1 : -1);
                        });
                        event.preventDefault();
                        this.refresh();

                        if (sortDirection === "asc")
                            this.sortData[0].direction = "desc";
                        else
                            this.sortData[0].direction = "asc";
                    }

                },
                columns: [
                    { field: 'accessoryId', caption: 'Schaltartikel ID', size: '1%', sortable: false, hidden: true },
                    { field: 'driverName', caption: 'Treiber', size: '3%', sortable: true },
                    { field: 'address', caption: 'Adresse', size: '3%', sortable: true },
                    { field: 'protocol', caption: 'Protokoll', size: '6%', sortable: true },
                    { field: 'identifier', caption: 'Identifizierer', size: '6%', sortable: true },
                    {
                        field: 'type',
                        caption: 'Type',
                        size: '5%',
                        hidden: true
                    },
                    {
                        field: 'assignedVisItem',
                        caption: 'Plan Zuordnung',
                        size: '5%',
                        sortable: true,
                        editable: {
                            type: 'list',
                            items: this._localList,
                            filter: false
                        }
                    },
                    { field: 'state', caption: 'Status', size: '5%' },
                    { field: 'addr0', caption: 'Addr0', size: '5%', sortable: true, },
                    { field: 'addr1', caption: 'Addr1', size: '5%', sortable: true, },
                    { field: 'addr2', caption: 'Addr2', size: '5%', sortable: true, },
                    { field: 'addr3', caption: 'Addr3', size: '5%', sortable: true, },
                    { field: 'maxAddr', caption: 'max. Addresses', size: '1%', hidden: true }
                ],
                records: [],
                onSelect: function (ev) {
                    const gridName = ev.target;
                    ev.onComplete = function () {
                        if (self.__isSelectedRecordAllowedForEdit() === true) {
                            const btnModify = w2ui[gridName].toolbar.get(gridName + '_cmdModifyAccessory');
                            const btnRemove = w2ui[gridName].toolbar.get(gridName + '_cmdDeleteAccessory');
                            w2ui[gridName].toolbar.enable(btnModify.id);
                            w2ui[gridName].toolbar.enable(btnRemove.id);
                        }
                    };
                },
                onUnselect: function (ev) {
                    const gridName = ev.target;
                    ev.onComplete = function () {
                        const btnModify = w2ui[gridName].toolbar.get(gridName + '_cmdModifyAccessory');
                        const btnRemove = w2ui[gridName].toolbar.get(gridName + '_cmdDeleteAccessory');
                        w2ui[gridName].toolbar.disable(btnModify.id);
                        w2ui[gridName].toolbar.disable(btnRemove.id);
                    };
                },
                onChange: function (ev) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(ev.recid);
                    const column = ev.column;

                    // assignedVisItem
                    if (column === ColumnNoAssignedVisItem) {
                        self.__trigger('setting',
                            {
                                command: 'update',
                                argument: 'accessory',
                                argumentValue: {
                                    accessoryDriver: rec.driverName,
                                    accessoryIdentifier: rec.identifier,
                                    planfieldControlIdentifier: ev.value_new.text
                                }
                            });

                        rec.assignedVisItem = ev.value_new.text;

                        elGrid.refreshCell(rec, 'assignedVisItem');
                        elGrid.save();
                    }
                }
            });

            const gridTb = w2ui[this.__gridName].toolbar;
            if (gridTb) {
                //
                // button to switch the accessory state
                //
                gridTb.insert('search',
                    {
                        type: 'button',
                        id: 'itemExecute',
                        text: 'Execute',
                        img: 'fas fa-exchange-alt',
                        tooltip: function () {
                            return 'Schaltet den aktuell ausgewählten Schaltartikel in den nächsten Zustand.';
                        },
                        onClick: function () {
                            const elGrid = w2ui[self.__gridName];
                            const sel = elGrid.getSelection();

                            let j;
                            const jMax = sel.length;
                            for (j = 0; j < jMax; ++j) {
                                const recid = sel[j];
                                const rec = elGrid.get(recid);
                                
                                const maxAddresses = rec.maxAddr;
                                let addrIndex = rec.state;

                                addrIndex++;
                                if (addrIndex >= maxAddresses)
                                    addrIndex = 0;

                                self.__trigger("accessoryExecute", {
                                    "objectId": rec.accessoryId,
                                    "command": "update",
                                    "argument": "targetState",
                                    "argumentValue": {
                                        driverName: rec.driverName,
                                        addrIndex: addrIndex
                                    }
                                });
                            }
                        }
                    });

                gridTb.add({ type: 'break' });

                //
                // Kommando zum Hinzufügen einer neuen Zeile
                //
                gridTb.add({
                    type: 'button',
                    id: this.__gridName + '_cmdAddAccessory',
                    text: 'Erstellen',
                    icon: 'fa fa-plus', // Font Awesome Icon für "Neu"
                    tooltip: function () {
                        return 'Erstellt ein manuelles Schaltobjekt für den Gleisplan, falls die Kommandostation (z. B. z21) kein eigenes Schaltartikel-Management bietet.';
                    },
                    onClick: function (ev) {
                        self.__showAddEditAccPopup(true, null);
                    }
                });

                //
                // Kommando zum Editieren eines Schaltartikel
                //
                gridTb.add({
                    type: 'button',
                    id: this.__gridName + '_cmdModifyAccessory',
                    text: 'Bearbeiten',
                    icon: 'fa fa-edit',
                    disabled: true,
                    tooltip: function () {
                        return 'Editiert ein vorhandenes Schaltartikel-Objekt, z.B. Änderung der Adressierung.';
                    },
                    onClick: function (ev) {

                        //
                        // wenn eine Reihe selektiert ist,
                        // und der selektierte Eintrag editierbar
                        // ist (also "z21" Treiber), dann öffnet
                        // sich ein Dialog zum Anpassen
                        //
                        const grid = w2ui[self.__gridName];
                        const selection = grid.getSelection();
                        if (selection && selection.length > 0) {
                            const idx = selection[0];
                            const rec = grid.get(idx);
                            self.__showAddEditAccPopup(false, rec)
                        }
                    }
                });

                //
                // Kommando zum Löschen
                //
                gridTb.add({
                    type: 'button',
                    id: this.__gridName + '_cmdDeleteAccessory',
                    text: 'Löschen',
                    icon: 'fa fa-trash',
                    disabled: true,
                    tooltip: function () {
                        return 'Löscht ein Schaltartikel-Objekt und die dazugehörigen Einstellungen.';
                    },
                    onClick: function (ev) {
                        const grid = w2ui[self.__gridName];
                        const selected = grid.getSelection();
                        if (selected.length === 0) {
                            w2alert('Bitte einen Schaltartikel auswählen.');
                            return;
                        }

                        w2confirm('Ausgewählten Schaltartikel wirklich löschen?')
                            .yes(() => {
                                for (const recid of selected) {

                                    const rec = grid.get(recid);

                                    self.__trigger('inventar',
                                        {
                                            command: 'remove',
                                            argument: 'accessory',
                                            argumentValue: {
                                                driverName: rec.driverName,
                                                address: rec.accessoryId
                                            }
                                        });

                                    grid.remove(recid);
                                }
                            });
                    }
                });
            }
        }

        this.__installed = true;
    }

    __isSelectedRecordAllowedForEdit() {
        const grid = w2ui[this.__gridName];
        const selection = grid.getSelection();
        if (selection && selection.length > 0) {
            const idx = selection[0];
            const rec = grid.get(idx);
            if (rec.driverName === "z21") {
                return true;
            }
        }
        return false;
    }

    __showAddEditAccPopup(isNew, rec) {

        const self = this;
        let title = 'Schaltartikel anpassen';
        if (isNew === true) title = 'Schaltartikel hinzufügen';
        const dlgEditName = `dlgedit_Accessory`;
        w2popup.open({
            title: title,
            body: '<div id="accessoryEditForm" style="width: 100%; height: 100%;"></div>',
            width: 400,
            height: 300,
            showMax: true,
            onOpen: function (ev) {
                const self2 = self;
                ev.onComplete = function () {

                    let formData = {
                        originalDriverName: 'z21',
                        originalAddress: 'DCC',

                        driverName: 1,
                        address: 1, // objectId, DCC-address
                        name: '',
                        protocol: 1,
                        acctype: 'SWITCH'
                    };

                    if (rec && rec != null) {
                        formData = {
                            originalDriverName: rec.driverName,
                            originalAddress: rec.accessoryId,

                            driverName: self.__getIndexOfList(self.__driverList, rec.driverName),
                            address: rec.accessoryId, // objectId, DCC-address
                            name: rec.identifier,
                            protocol: self.__getIndexOfList(self.__protocolList, rec.protocol),
                            acctype: rec.type
                        };
                    }

                    if (w2ui[dlgEditName]) {
                        w2ui[dlgEditName].destroy();
                        w2ui[dlgEditName] = null;
                    }

                    if (!w2ui[dlgEditName]) {
                        $('#accessoryEditForm').w2form({
                            name: dlgEditName,
                            fields: [
                                { field: 'originalDriverName', type: 'string', hidden: true },
                                { field: 'originalAddress', type: 'int', hidden: true },

                                {
                                    field: 'driverName',
                                    type: 'list',
                                    html: {
                                        caption: 'Treiber'
                                    },
                                    options: {
                                        items: self.__driverList
                                    }
                                },
                                {
                                    field: 'address',
                                    type: 'int',
                                    html: {
                                        caption: 'Adresse'
                                    },
                                    options: {
                                        min: 1,    // optional
                                        //max: 9999  // optional
                                    }
                                },
                                {
                                    field: 'protocol',
                                    type: 'list',
                                    html: {
                                        caption: 'Protokoll'
                                    },
                                    options: {
                                        items: self.__protocolList
                                    }
                                },
                                {
                                    field: 'name',
                                    type: 'string',
                                    html: {
                                        caption: 'Name'
                                    }
                                }
                            ],
                            record: formData,
                            actions: {
                                Save: function () {

                                    const dataToSave = {
                                        originalDriverName: this.record.originalDriverName,
                                        originalAddress: this.record.originalAddress,

                                        driverName: this.record.driverName.text,
                                        address: this.record.address,
                                        protocol: this.record.protocol.text,
                                        name: this.record.name,
                                        acctype: this.record.acctype
                                    }

                                    console.log(dataToSave);

                                    self.__trigger('inventar',
                                        {
                                            command: 'modify',
                                            argument: 'accessory',
                                            argumentValue: dataToSave
                                        });

                                    w2popup.close();
                                }
                            }
                        });
                    }

                    //
                    // call to apply some ui modifications
                    //
                    setTimeout((ev) => {
                        $('.w2ui-form .w2ui-page').css({
                            padding: 0
                        });
                        $('.w2ui-column-container').css({
                            'display': '',
                        });
                    }, 50);
                }
            }
        });
    }

    _localList() {
        const elGrid = w2ui["gridAccessories"];
        const usedPlanItems = [];
        for (let i = 0; i < elGrid.records.length; ++i) {
            const rec = elGrid.records[i];
            if (rec) {
                if (rec.assignedVisItem && rec.assignedVisItem.length > 0)
                    usedPlanItems.push(rec.assignedVisItem);
            }
        }

        const accessoryItems = $('.ctrlItemAccessory');
        const entries = [];
        entries.push(' ');
        accessoryItems.each((idx, el) => {
            const id = $(el).attr("id");
            if (!usedPlanItems.includes(id))
                entries.push(id);
        });
        return entries;
    }

    // objectId == address == accessoryId
    removeAccessory(driverName, objectId) {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        const recs = elGrid.find({
            driverName: driverName,
            accessoryId: objectId
        });
        if (recs.length > 0) {
            let j;
            const jMax = recs.length;
            for (j = 0; j < jMax; ++j) {
                const recid = recs[j];
                elGrid.remove(recid);
            }
        }
    }

    updateAccessories(model) {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        const listOfObjectsToAdd = [];
        if (!elGrid) return;

        const noOfKeys = model.length;
        for (let idx = 0; idx < noOfKeys; ++idx) {
            const accessory = model[idx];
            const objectId = parseInt(accessory.objectId);
            //if (!(ecosObjectId >= 20000 && ecosObjectId < 30000)) {
            //    continue;
            //}

            // Nur Signale und Weichen!
            if (accessory.mode !== "SWITCH") continue;

            const rec = elGrid.find({ accessoryId: objectId });
            if (rec.length <= 0) {

                const accData = {
                    accessoryId: objectId,
                    driverName: accessory.driverName,
                    address: objectId,
                    protocol: accessory.protocol,
                    identifier: accessory.name1,
                    assignedVisItem: '',
                    type: accessory.mode,
                    state: accessory.state,
                    maxAddr: accessory.gates
                };

                const addrext = accessory.addrext;
                accData.addr0 = addrext[0];
                accData.addr1 = addrext[1];
                if (addrext.length > 2) accData.addr2 = addrext[2];
                else accData.addr2 = null;
                if (addrext.length > 3) accData.addr3 = addrext[3];
                else accData.addr3 = null;

                listOfObjectsToAdd.push(accData);

            } else {
                const row = elGrid.get(rec);
                const oldState = row[0].state;
                if (oldState !== accessory.state) {
                    row[0].state = accessory.state;
                    elGrid.refreshCell(rec, 'state');
                }
            }
        }

        if (listOfObjectsToAdd.length > 0) {
            elGrid.add(listOfObjectsToAdd);
        }
    }

    // searches in this.__settingsInfo.accessories
    __getAccessoryByName(accName) {
        if (!accName) return null;
        if (!this.__settingsInfo?.accessories) return null;
        const acc = this.__settingsInfo?.accessories.filter(acc => acc.accessoryIdentifier == accName);
        if (acc && acc.length > 0)
            return acc[0];
        return null;
    }

    updateSettings(settings) {
        if (!settings) return;
        this.__settingsInfo = settings;

        if (settings.accessories) {
            const grid = w2ui[this.__gridName];
            if (!grid) return;

            // alle vorherigen Zuordnungen löschen
            // neue werden daraufhin gesetzt
            const recs = grid.records;
            for (let i = 0; i < recs.length; ++i) {
                const rec = recs[i];
                if (rec && rec.assignedVisItem && rec.assignedVisItem.length > 0) {
                    rec.assignedVisItem = '';
                    grid.refreshCell(rec, 'assignedVisItem');
                }
            }

            $(settings.accessories).each((idx, el) => {
                if (!el) return;
                const recid = grid.find({
                    identifier: el.accessoryIdentifier,
                    driverName: el.accessoryDriver
                })[0];
                const rec = grid.get(recid);
                if (rec) {
                    rec.assignedVisItem = el.planfieldControlIdentifier;
                    grid.refreshCell(rec, 'assignedVisItem');
                }
            });

            grid.save();
        }
    }
}