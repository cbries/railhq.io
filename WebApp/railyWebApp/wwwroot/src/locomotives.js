// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// #region global Functions for LocomotiveControl

function getLocControlsUuid() {
    return window.__planfieldUuid + "_dialogStateLocomotives";;
}

function restoreLocomotiveControls() {
    const n = getLocControlsUuid();
    const ctrls = JSON.parse(localStorage.getItem(n)) || {};
    if (typeof ctrls !== "object" || ctrls === null) {
        // ignore
    } else {
        Object.keys(ctrls).forEach(accessor => {
            if (typeof accessor === "string") {
                const state = ctrls[accessor];
                if (state && state.open) {
                    const dlg = window.locomotivesDlg;
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

const GridLocomotiveColumnDescription = 2;
const GridLocomotiveColumnEnabled = 5;

const constTxtDriverName = 'Treiber';
const constTxtAdresse = 'Adresse <span style="font-size: 70%;">(1-10239)</span>';
const constMinAddress = 1;
const constMaxAddress = 10239;
const constTxtProcotol = 'Protokoll';
const constTxtName = 'Name';

class Locomotives {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.winLocomotives = null;
        this.gridLocomotives = null;
        this.__locomotiveImages = {};
        this.__recentRailyData = null;
        this.__installed = false;
        this.__gridName = "gridLocomotives";
        this.__dialogName = "dialogLocomotives";
        this.__storageNameGeometry = this.__dialogName + "_geometry";
        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);
        this.__initEventHandling();

        this.__driverList = [
            { id: 1, text: 'z21', caption: 'z21' }
        ];
        this.__protocolList = [
            { id: 1, text: 'MM14', caption: 'MM14' },
            { id: 2, text: 'MM27', caption: 'MM27' },
            { id: 3, text: 'MM128', caption: 'MM128' },
            { id: 4, text: 'DCC14', caption: 'DCC14' },
            { id: 5, text: 'DCC28', caption: 'DCC28' },
            { id: 6, text: 'DCC128', caption: 'DCC128' },
            { id: 7, text: 'MFX', caption: 'MFX' },
            { id: 8, text: 'MMFKT', caption: 'MMFKT' }
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
        const n = getLocControlsUuid();
        let dialogState = JSON.parse(localStorage.getItem(n)) || {};
        const zindex = parseInt($('#' + this.__dialogName).closest(".ui-dialog").css("z-index"));
        dialogState['dlgLocomotives'] = {
            open: open,
            zindex: zindex
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

    /**
     * Displays the locomotive control dialog.
     */
    show() {
        const dialogElement = $('#' + this.__dialogName);
        this.__windowGeometry.showWithGeometry(dialogElement);
        if (!this.__installed) this.install();
        dialogElement.dialog("open");

        // Update locomotives and refresh the grid
        this.updateLocomotives(this.__recentRailyData);
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

    getLocomotives() {
        if (!w2ui[this.__gridName]) return [];
        const grid = w2ui[this.__gridName];
        return grid.records;
    }

    /**
     * Retrieves the locomotive record from the grid based on the driver's name and object ID (oid).
     *
     * @param {string} driverName - The name of the driver to match in the grid.
     * @param {number} oid - The object ID (must be greater than 0) to match in the grid.
     * @returns {object|null} - The record object if found; otherwise, null.
     */
    getLocomotiveRecord(driverName, oid) {
        if (oid <= 0) return null;
        if (!w2ui[this.__gridName]) return null;
        const grid = w2ui[this.__gridName];
        const rec = grid.find({ driverName: driverName, oid: oid });
        if (rec && rec.length > 0) return grid.get(rec[0]);
        return null;
    }

    /**
     * Renders an image for the locomotive.
     *
     * @param {Object} record - The record containing the image data.
     * @returns {string} - The HTML string for rendering the image.
     */
    __renderImage(record) {
        const fakeId = `loadLocomotivesImage_${record.oid}`;
        loadLocomotiveImageIntoHtml(fakeId, record.name);

        // Use template literals for cleaner HTML structure
        const innerHtml = `
        <div style="height: 100%; width: 100%; text-align: center; padding-top: 3px;">
            <img id="${fakeId}" style="height: 16px; width: 32px; margin: auto;" src="./images/noimage.png" />
        </div>
        `;

        return innerHtml;
    }

    install() {
        const self = this;
        const state = this.isShown();
        if (state) return;

        this.__gridRowDoubleBlickCount = 0;
        this.__expandedPreferences = {};
        const geometry = this.__windowGeometry.recent();

        $("#" + this.__dialogName).dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            closeOnEscape: false,
            autoOpen: false,
            close: function (event) {
                self.__setDialogState(false);
            },
            resizeStop: function (event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop: function (event, ui) {
                self.__setDialogState(true);
                self.__windowGeometry.save(ui.position,
                    {
                        width: event.target.clientWidth,
                        height: event.target.clientHeight
                    });
            }
        });

        if (!w2ui[this.__gridName]) {
            const targetGridEl = $('#' + this.__gridName);
            targetGridEl.w2grid({
                name: this.__gridName,
                header: "Locomotives",
                multiSelect: false,
                show: {
                    //lineNumbers: true,
                    toolbar: true,
                    //header: true,
                    footer: true,
                    toolbarAdd: false,
                    toolbarDelete: false,
                    toolbarEdit: true,
                    toolbarSave: false
                },
                dragRow: true,
                textSearch: 'contains',
                recid: 'oid', // rename "recid" to "oid"
                searches: [
                    { field: 'name', caption: 'Name', type: 'text' }
                ],
                sortData: [{ field: 'name', direction: 'asc' }],
                columns: [
                    {
                        field: 'oid',
                        caption: '<div style="text-align: center; padding-left: 6px;"><i class="far fa-id-badge"></i></div>',
                        style: 'text-align: center',
                        size: '1%',
                        sortable: true,
                        hidden: true
                    },
                    { field: 'driverName', caption: 'Treiber', size: '5%', sortable: true },
                    {
                        field: 'locImage',
                        caption: '<div style="text-align: center; padding-left: 6px;"><i class="fas fa-camera-retro"></i></div>',
                        size: '5%',
                        style: 'text-align: center',
                        render: self.__renderImage
                    },
                    {
                        field: 'name',
                        caption: 'Name',
                        size: '20%',
                        sortable: true
                    },
                    {
                        field: 'speedstep',
                        caption: '<div style="text-align: center; padding-left: 6px;"><i class="fas fa-tachometer-alt"></i> Speed</div>',
                        style: 'text-align: center',
                        size: '5%'
                    },
                    {
                        field: 'speedstepMax',
                        caption: '<div style="text-align: center; padding-left: 6px;"><i class="fas fa-tachometer-alt"></i> (max)</div>',
                        style: 'text-align: center',
                        size: '6%'
                    },
                    {
                        field: 'protocol',
                        caption: '<div style="text-align: center; padding-left: 6px;"><i class="fas fa-server"></i> Protokoll</div>',
                        style: 'text-align: center',
                        size: '6%',
                        sortable: true
                    },
                    {
                        field: 'addr',
                        caption: '<div style="text-align: center; padding-left: 6px;"><i class="fas fa-at"></i> Addresse</div>',
                        style: 'text-align: center',
                        size: '6%',
                        sortable: true
                    },
                    {
                        field: 'funcdesc',
                        hidden: true
                    },
                    {
                        field: 'direction',
                        hidden: true
                    }
                ],
                records: [],
                onSelect: function (ev) {
                    const gridName = ev.target;
                    ev.onComplete = function () {
                        if (self.__isSelectedRecordAllowedForEdit() === true) {
                            const btnModify = w2ui[gridName].toolbar.get(gridName + '_cmdModifyLocomotive');
                            const btnRemove = w2ui[gridName].toolbar.get(gridName + '_cmdDeleteLocomotive');
                            const btnFuncs = w2ui[gridName].toolbar.get(gridName + '_cmdModifyLocomotiveFunctions');
                            w2ui[gridName].toolbar.enable(btnModify.id);
                            w2ui[gridName].toolbar.enable(btnRemove.id);
                            w2ui[gridName].toolbar.enable(btnFuncs.id);
                        }
                    };
                },
                onUnselect: function (ev) {
                    const gridName = ev.target;
                    ev.onComplete = function () {
                        const btnModify = w2ui[gridName].toolbar.get(gridName + '_cmdModifyLocomotive');
                        const btnRemove = w2ui[gridName].toolbar.get(gridName + '_cmdDeleteLocomotive');
                        const btnFuncs = w2ui[gridName].toolbar.get(gridName + '_cmdModifyLocomotiveFunctions');
                        w2ui[gridName].toolbar.disable(btnModify.id);
                        w2ui[gridName].toolbar.disable(btnRemove.id);
                        w2ui[gridName].toolbar.disable(btnFuncs.id);
                    };
                },
                onDblClick: function (event) {
                    self.__gridRowDoubleBlickCount++;

                    const elGrid = w2ui[self.__gridName];
                    const columnIdx = event.column;
                    const c = elGrid.columns[columnIdx];
                    if (c.field === "name") {
                        event.stopPropagation();
                        return;
                    }

                    const rec = elGrid.get(event.recid);
                    self.__trigger('doubleClickGridRow', rec);
                },
                onEdit: function (event) {
                    const elGrid = w2ui[self.__gridName];
                    const rec = elGrid.get(event.recid);
                    const driverName = rec.driverName;
                    const objectId = rec.oid;
                    const name = rec.name;

                    self.openProperties(driverName, objectId, name);
                }
            });
        }

        const gridTb = w2ui[self.__gridName].toolbar;
        if (gridTb) {
            gridTb.add({
                type: 'button',
                id: this.__gridName + '_cmdOpenDialog',
                text: 'Steuerung',
                icon: 'fa fa-tachometer-alt',
                onClick: function (ev) {
                    const grid = w2ui[self.__gridName];
                    const selection = grid.getSelection();
                    if (selection && selection.length > 0) {
                        const idx = selection[0];
                        const rec = grid.get(idx);
                        self.__trigger('doubleClickGridRow', rec);
                    }
                }
            });

            gridTb.add({ type: 'break' });

            // #region edit entry

            //
            // Kommando zum Hinzufügen einer neuen Zeile
            //
            gridTb.add({
                type: 'button',
                id: this.__gridName + '_cmdAddLocomotive',
                text: 'Erstellen',
                icon: 'fa fa-plus', // Font Awesome Icon für "Neu"
                tooltip: function () {
                    return 'Erstellt ein manuelle Lokomotive, falls die Kommandostation (z. B. z21) kein eigenes Lokomotiven-Management bietet.';
                },
                onClick: function (ev) {
                    self.__showAddEditLocPopup(true, null);
                }
            });

            //
            // Kommando zum Editieren eines Schaltartikel
            //
            gridTb.add({
                type: 'button',
                id: this.__gridName + '_cmdModifyLocomotive',
                text: 'Bearbeiten',
                icon: 'fa fa-edit',
                disabled: true,
                tooltip: function () {
                    return 'Editiert ein vorhandenes Lokomotiven-Objekt, z.B. Änderung der Adressierung.';
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
                        self.__showAddEditLocPopup(false, rec)
                    }
                }
            });

            //
            // Kommando zum Löschen
            //
            gridTb.add({
                type: 'button',
                id: this.__gridName + '_cmdDeleteLocomotive',
                text: 'Löschen',
                icon: 'fa fa-trash',
                disabled: true,
                tooltip: function () {
                    return 'Löscht ein Lokomotiv-Objekt und die dazugehörigen Einstellungen.';
                },
                onClick: function (ev) {
                    const grid = w2ui[self.__gridName];
                    const selected = grid.getSelection();
                    if (selected.length === 0) {
                        w2alert('Bitte eine Lokomotive auswählen.');
                        return;
                    }

                    w2confirm('Ausgewählte Lokomotive wirklich löschen?')
                        .yes(() => {
                            for (const recid of selected) {

                                const rec = grid.get(recid);

                                self.__trigger('inventar',
                                    {
                                        command: 'remove',
                                        argument: 'locomotive',
                                        argumentValue: {
                                            driverName: rec.driverName,
                                            address: rec.addr
                                        }
                                    });

                                grid.remove(recid);
                            }
                        });
                }
            });

            // #endregion

            gridTb.add({ type: 'break' });

            // #region edit functions

            //
            // Kommando zum Editieren eines Schaltartikel
            //
            gridTb.add({
                type: 'button',
                id: this.__gridName + '_cmdModifyLocomotiveFunctions',
                text: 'Funktionen',
                icon: 'fa fa-cogs',
                disabled: true,
                tooltip: function () {
                    return 'Editiert die Funktionen eines vorhandenen Lokomotiven-Objekts.';
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
                        self.__showAddEditLocFuncsPopup(rec)
                    }
                }
            });


            // #endregion
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

    __isAllowedDriverForEdit(driverName, showAlert = false) {
        if (driverName === "z21") return true;
        if (showAlert === true) {
            w2alert('Dieses Element kann nur bearbeitet werden,<br>wenn es mit einem „z21“-Treiber verknüpft ist.')
                .ok(() => {
                    console.log('ok')
                });
        }
        return false;
    }

    __showAddEditLocFuncsPopup(rec) {

        if (window.__updateLocomotiveFunctionsIcon) {
            window.__updateLocomotiveFunctionsIcon = null;
        }
        window.__updateLocomotiveFunctionsIcon = function (recid, newIcon) {
            const grid = w2ui[formName];
            const record = grid.get(recid);
            if (record) {
                record.icon = newIcon;
                grid.refreshRow(recid); // Nur die betroffene Zeile neu rendern
            }
        };

        const self = this;
        const title = 'Lokomotivfunktionen anpassen';
        const dlgEditName = `dlgedit_LocomotiveFunctions`;
        const formName = 'locomotiveFunctionsEditForm';
        const driverName = rec.driverName;
        const objectId = rec.addr;
        const userFncs = rec.funcdesc;

        w2popup.open({
            title: title,
            body: '<div id="' + formName + '" style="width: 100%; height: 100%;"></div>',
            width: 600,
            height: 400,
            showMax: true,
            onOpen: function (ev) {

                ev.onComplete = function () {

                    let records = [];
                    for (let ii = 1; ii <= 32; ++ii) {
                        records.push({ recid: ii, name: `F${ii}`, icon: '' });
                    }
                    
                    const formattedOptions = locomotiveFunctionDescriptionList.map((text, index) => ({
                        id: index + 1,
                        text: text,
                        caption: text
                    }));
                    
                    for (let itFnc of userFncs) {
                        const fname = itFnc.name;
                        for (const itRec of records) {
                            if (itRec.name === fname) {
                                itRec.description = formattedOptions[self.__getIndexOfList(formattedOptions, itFnc.description) - 1];
                                itRec.enabled = itFnc.isUsed;

                                if (itFnc.icon && itFnc.icon.length > 0) {
                                    itRec.icon = itFnc.icon;
                                } else {
                                    const icon = locomotiveFunctionIconList[itRec.description.caption];
                                    if (icon && icon.length > 0) {
                                        itRec.icon = icon;
                                    }
                                }
                            }
                        }
                    }

                    if (w2ui[formName]) {
                        w2ui[formName].destroy();
                        w2ui[formName] = null;
                    }

                    if (!w2ui[formName]) {
                        const targetGridEl = $('#' + formName);

                        targetGridEl.w2grid({
                            name: formName,
                            show: {
                                header: false,
                                footer: false,
                                toolbar: false
                            },
                            columns: [
                                { field: 'recid', caption: 'ID', sortable: false, hidden: true },
                                {
                                    field: 'name',
                                    caption: '<div style="text-align: center; padding-left: 6px;">FX</div>',
                                    size: '6%'
                                },
                                {
                                    field: 'description',
                                    caption: 'Beschreibung (Tooltip)',
                                    size: '20%',
                                    editable: {
                                        type: 'list',
                                        items: formattedOptions,
                                        filter: true
                                    }
                                },
                                {
                                    field: 'iconInput',
                                    caption: '<div style="text-align: center; padding-left: 6px;">Icon ändern</div>',
                                    size: '15%',
                                    editable: { type: 'text' },
                                    render(record) {
                                        return `<input
                                                    type="text"
                                                    value="${record.icon}"
                                                    placeholder="🔊, fa-volume-up, /img/icon.png"
                                                    style="
                                                        width: 100%;
                                                        padding: 4px 8px;
                                                        font-size: 0.95em;
                                                        border: 1px solid #ccc;
                                                        border-radius: 4px;
                                                        background-color: #f9f9f9;
                                                        color: #333;
                                                        outline: none;
                                                        box-sizing: border-box;
                                                        transition: border-color 0.2s;
                                                    "
                                                    onfocus="this.style.borderColor='#999'"
                                                    onblur="this.style.borderColor='#ccc'"
                                                    oninput="window.__updateLocomotiveFunctionsIcon(${record.recid}, this.value)"
                                                />`;
                                    }
                                },
                                {
                                    field: 'icon',
                                    caption: '<div style="text-align: center; padding-left: 6px;">Icon</div>',
                                    size: '5%',
                                    render(record) {
                                        const icon = renderLocomotiveFunctionImage(record.icon);
                                        return `
                                            <div style="
                                                display: flex;
                                                align-items: center;
                                                justify-content: center;
                                                height: 100%;
                                            ">
                                                <span style="font-size: 1.2em;">
                                                    ${icon}
                                                </span>
                                            </div>
                                        `;
                                    }
                                },
                                {
                                    field: 'enabled',
                                    caption: '<div style="text-align: center; padding-left: 6px;">Verwenden</div>',
                                    editable: { type: 'checkbox' },
                                    size: '10%'
                                }
                            ],
                            onChange: function (ev) {
                                const elGrid = w2ui[formName];
                                ev.onComplete = function () {
                                    const rec = elGrid.get(ev.recid);
                                    if (ev.column == GridLocomotiveColumnEnabled) {
                                        rec["enabled"] = ev.value_new;
                                        elGrid.set(ev.recid, rec);
                                    } else if (ev.column = GridLocomotiveColumnDescription) {
                                        let description = '';
                                        if (ev.value_new.caption) { description = ev.value_new.caption; }
                                        else { description = ev.value_new; }
                                        const icon = locomotiveFunctionIconList[description];

                                        if (icon) {
                                            rec["icon"] = icon;
                                        }

                                        rec["description"] = description;
                                        elGrid.set(ev.recid, rec);
                                    }

                                    // Sicherstellen, dass geänderter Wert verwendet wird
                                    const isEnabled = (ev.column === elGrid.getColumn('enabled')?.index)
                                        ? ev.value_new
                                        : rec.enabled;

                                    self.__trigger('inventar', {
                                        command: 'modify',
                                        argument: 'locomotiveFunction',
                                        argumentValue: {
                                            driverName: driverName,
                                            address: objectId,
                                            fncIdx: rec.recid - 1,
                                            name: rec.name,
                                            description: rec.description,
                                            isUsed: isEnabled,
                                            icon: rec.icon
                                        }
                                    });
                                }


                            }
                        });
                    }

                    w2ui[formName].records = records;
                    w2ui[formName].refresh();
                }
            }
        });
    }

    __showAddEditLocPopup(isNew, rec) {

        const self = this;
        let title = 'Lokomotive anpassen';
        if (isNew === true) title = 'Lokomotive hinzufügen';
        const dlgEditName = `dlgedit_Locomotive`;
        w2popup.open({
            title: title,
            body: '<div id="locomotiveEditForm" style="width: 100%; height: 100%;"></div>',
            width: 400,
            height: 300,
            showMax: true,
            onOpen: function (ev) {
                const self2 = self;
                ev.onComplete = function () {

                    let formData = {
                        originalDriverName: 'z21',
                        originalAddress: 'DCC128',

                        driverName: 1,
                        address: 3, // objectId, DCC-address
                        name: '',
                        protocol: 6,
                    };

                    if (rec && rec != null) {
                        formData = {
                            originalDriverName: rec.driverName,
                            originalAddress: rec.addr,

                            driverName: self.__getIndexOfList(self.__driverList, rec.driverName),
                            address: rec.addr, // objectId, DCC-address
                            protocol: self.__getIndexOfList(self.__protocolList, rec.protocol),
                            name: rec.name
                        };
                    }

                    if (w2ui[dlgEditName]) {
                        w2ui[dlgEditName].destroy();
                        w2ui[dlgEditName] = null;
                    }

                    if (!w2ui[dlgEditName]) {
                        $('#locomotiveEditForm').w2form({
                            name: dlgEditName,
                            fields: [
                                { field: 'originalDriverName', type: 'string', hidden: true },
                                { field: 'originalAddress', type: 'int', hidden: true },

                                {
                                    field: 'driverName',
                                    type: 'list',
                                    html: {
                                        caption: constTxtDriverName
                                    },
                                    options: {
                                        items: self.__driverList
                                    }
                                },
                                {
                                    field: 'address',
                                    type: 'int',
                                    html: {
                                        caption: constTxtAdresse
                                    },
                                    options: {
                                        min: constMinAddress,
                                        max: constMaxAddress
                                    }
                                },
                                {
                                    field: 'protocol',
                                    type: 'list',
                                    html: {
                                        caption: constTxtProcotol
                                    },
                                    options: {
                                        items: self.__protocolList
                                    }
                                },
                                {
                                    field: 'name',
                                    type: 'string',
                                    html: {
                                        caption: constTxtName,
                                        attr: 'maxlength="32"'
                                    }
                                }
                            ],
                            record: formData,
                            actions: {
                                Cancel: function () {
                                    w2popup.close();
                                },

                                Save: async function () {

                                    const dataToSave = {
                                        originalDriverName: this.record.originalDriverName,
                                        originalAddress: this.record.originalAddress,

                                        driverName: this.record.driverName?.text ?? '',
                                        address: this.record.address,
                                        oid: this.record.address,
                                        protocol: this.record.protocol?.text ?? '',
                                        name: this.record.name,
                                        //acctype: this.record.acctype
                                    }

                                    // #region Validiere driverName::address

                                    //
                                    // prüfe an dieser Stelle ob die Lokomotive nicht schon existiert
                                    //    bei `isNew:=true`
                                    //
                                    // prüfe ob die neue Adresse der Lokomotive nicht schon vergeben ist
                                    //    bei `isNew:=false`
                                    //
                                    if (isNew === true) {

                                        const driverName = dataToSave.driverName;
                                        const address = dataToSave.address;

                                        //
                                        // Prüfen, ob bereits eine Lok mit dieser Adresse existiert
                                        //
                                        const res = await fetch(`/api/v1/automation/fleet/entity?driverName=${driverName}&objectId=${address}`);
                                        if (res.status === 200) {
                                            w2alert("Eine Lok mit dieser Adresse existiert bereits!");
                                            return;
                                        }
                                        //
                                        // Alles gut, wir haben einen 404, d.h. die Lok existiert noch nicht als Eintrag auf der Festplatte.
                                        //
                                        if (res.status !== 404) {
                                            w2alert("Fehler beim Prüfen der Adresse");
                                            return;
                                        }
                                    }
                                    else if (isNew === false) {
                                        if (dataToSave.originalAddress !== dataToSave.address) {
                                            if (res.status === 200) {
                                                w2alert("Die neue Adresse ist bereits vergeben!");
                                                return;
                                            }

                                            if (res.status !== 404) {
                                                w2alert("Fehler beim Prüfen der Adresse");
                                                return;
                                            }

                                            // Adresse wurde geändert, ist aber frei → ok
                                        }

                                        // Adresse wurde nicht geändert → kein Check nötig
                                    }

                                    // #endregion

                                    try {
                                        // Pflichtfeldprüfung: Adresse
                                        if (!dataToSave.address || dataToSave.address.length === 0) {
                                            throw new Error('Bitte eine Adresse angeben.');
                                        }
                                        // Pflichtfeldprüfung: Name
                                        if (!dataToSave.name || dataToSave.name.trim().length === 0) {
                                            throw new Error('Bitte einen Namen angeben.');
                                        }
                                        // Optional: Maximal 32 Zeichen für den Namen
                                        if (dataToSave.name.length > 32) {
                                            throw new Error('Der Name darf maximal 32 Zeichen lang sein.');
                                        }

                                        self.__trigger('inventar',
                                            {
                                                command: 'modify',
                                                argument: 'locomotive',
                                                argumentValue: dataToSave
                                            });

                                        w2popup.close();

                                    } catch (err) {
                                        w2alert(err.message);
                                    }
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

    openProperties(driverName, objectId, entityName) {
        const self = this;
        const locSettings = self.__getLocomotiveSettings(driverName, objectId);
        const dlgEditName = `dlgedit_${driverName}_${objectId}`;

        //
        // popup dialog for locomotive extra settings
        //
        w2popup.open({
            title: `${entityName} (${driverName}::${objectId})`,
            body: '<div id="locomotiveEditForm" style="width: 100%; height: 100%;"></div>',
            width: 400,
            height: 600,
            showMax: true,
            onOpen: function (event) {
                event.onComplete = function () {

                    const formData = {
                        machineType: 0,
                        length: 30,
                        isCommuter: false,
                        doAutoAccelerate: true,
                        doAutoDeaccelerate: true,
                        minimumBlockWait: 30,
                        minimumBlockWaitAfterError: 60,
                        speed_minimum: - 1,
                        speed_staging: -1,
                        speed_entering: -1,
                        speed_traveling: -1,
                        speed_maximum: -1
                    };

                    const typeItems = [
                        { id: 1, text: 'Dampf', caption: 'Dampf' },
                        { id: 2, text: 'Elektrisch', caption: 'Elektrisch' },
                        { id: 3, text: 'Diesel', caption: 'Diesel' }
                    ];

                    if (locSettings) {

                        if (locSettings?.machineType)
                            formData.machineType = [
                                typeItems[locSettings.machineType - 1]
                            ];

                        formData.length = locSettings.length;
                        formData.isCommuter = locSettings.isCommuter;
                        formData.doAutoAccelerate = locSettings.doAutoAccelerate;
                        formData.doAutoDeaccelerate = locSettings.doAutoDeaccelerate;
                        formData.minimumBlockWait = locSettings.minimumBlockWait;
                        formData.minimumBlockWaitAfterError = locSettings.minimumBlockWaitAfterError;

                        if (locSettings.speed) {
                            formData.speed_minimum = locSettings.speed.minimum;
                            formData.speed_staging = locSettings.speed.staging;
                            formData.speed_entering = locSettings.speed.entering;
                            formData.speed_traveling = locSettings.speed.traveling;
                            formData.speed_maximum = locSettings.speed.maximum;
                        }
                    }

                    if (w2ui[dlgEditName]) {
                        w2ui[dlgEditName].destroy();
                        w2ui[dlgEditName] = null;
                    }

                    if (!w2ui[dlgEditName]) {
                        $('#locomotiveEditForm').w2form({
                            name: dlgEditName,
                            fields: [
                                //
                                // General / Allgemein
                                //
                                {
                                    field: 'machineType',
                                    type: 'enum',
                                    html: {
                                        group: 'Allgemein',
                                        span: 8,
                                        caption: 'Maschine'
                                    },
                                    options: {
                                        openOnFocus: true,
                                        items: typeItems
                                    }
                                },
                                {
                                    field: 'length',
                                    type: 'int',
                                    html: {
                                        span: 8,
                                        caption: 'L&auml;nge'
                                    },
                                    options: { arrows: true, min: 0, max: 600 }
                                },
                                {
                                    field: 'isCommuter',
                                    type: 'checkbox',
                                    html: {
                                        span: 8,
                                        caption: 'Pendellok'
                                    }
                                },
                                {
                                    field: 'doAutoAccelerate',
                                    type: 'checkbox',
                                    html: {
                                        span: 8,
                                        caption: '<div>Anfahren <span style="font-size: 60%;">(Automatikbetrieb)</span></div>'
                                    }
                                },
                                {
                                    field: 'doAutoDeaccelerate',
                                    type: 'checkbox',
                                    html: {
                                        span: 8,
                                        caption: '<div>Abbremsen <span style="font-size: 60%;">(Automatikbetrieb)</span></div>'
                                    }
                                },

                                //
                                // Wait / Wartezeiten
                                //
                                {
                                    field: 'minimumBlockWait',
                                    type: 'int',
                                    html: {
                                        group: 'Wartezeiten',
                                        span: 8,
                                        caption: 'Block'
                                    },
                                    options: { arrows: true, min: 0, max: 600 }
                                },
                                {
                                    field: 'minimumBlockWaitAfterError',
                                    type: 'int',
                                    html: {
                                        group: 'Wartezeiten',
                                        span: 8,
                                        caption: 'Block nach Fehler'
                                    },
                                    options: { arrows: true, min: 0, max: 600 }
                                },

                                //
                                // Speed / Geschwindigkeit
                                //
                                {
                                    field: 'speed_minimum',
                                    type: 'int',
                                    html: {
                                        group: 'Geschwindigkeit',
                                        caption: 'Minimum',
                                        span: 4,
                                        attr: ''
                                    },
                                    options: { arrows: true, min: 0, max: 128 }
                                },
                                {
                                    field: 'speed_staging',
                                    type: 'int',
                                    html: { caption: 'Staging', span: 4, attr: '' },
                                    options: { arrows: true, min: 0, max: 128 }
                                },
                                {
                                    field: 'speed_entering',
                                    type: 'int',
                                    html: { caption: 'Entering', span: 4, attr: '' },
                                    options: { arrows: true, min: 0, max: 128 }
                                },
                                {
                                    field: 'speed_traveling',
                                    type: 'int',
                                    html: { caption: 'Reise', span: 4, attr: '' },
                                    options: { arrows: true, min: 0, max: 128 }
                                },
                                {
                                    field: 'speed_maximum',
                                    type: 'int',
                                    html: { caption: 'Maximum', span: 4, attr: '' },
                                    options: { arrows: true, min: 0, max: 128 }
                                }
                            ],
                            record: formData,
                            actions: {
                                Save: function () {

                                    self.__trigger('setting', {
                                        command: 'update',
                                        argument: 'locomotiveExtras',
                                        argumentValue: {
                                            driverName: driverName,
                                            objectId: objectId,
                                            machineType: this.record.machineType,
                                            length: this.record.length,
                                            isCommuter: this.record.isCommuter,
                                            doAutoAccelerate: this.record.doAutoAccelerate,
                                            doAutoDeaccelerate: this.record.doAutoDeaccelerate,
                                            minimumBlockWait: this.record.minimumBlockWait,
                                            speed: {
                                                minimum: this.record.speed_minimum,
                                                staging: this.record.speed_staging,
                                                entering: this.record.speed_entering,
                                                traveling: this.record.speed_traveling,
                                                maximum: this.record.speed_maximum
                                            }
                                        }
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
                };
            }
        });
    }

    __getLocomotiveSettings(driverName, objectId) {
        if (window.settingsInfo) {
            for (let i = 0; i < window.settingsInfo.locomotives.length; ++i) {
                const locSetting = window.settingsInfo.locomotives[i];
                if (locSetting) {
                    if (locSetting.driverName === driverName && locSetting.objectId === objectId)
                        return locSetting;
                }
            }
        }

        return null;
    }

    // objectId == address == accessoryId
    removeLocomotive(driverName, objectId) {
        const self = this;
        const elGrid = w2ui[self.__gridName];
        const recs = elGrid.find({
            driverName: driverName,
            oid: objectId
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

    updateLocomotives(railyDataLocomotives) {
        if (!railyDataLocomotives) return;
        this.__recentRailyData = railyDataLocomotives;

        const elGrid = w2ui[this.__gridName];
        const listOfObjectsToAdd = [];
        if (!elGrid) return;

        railyDataLocomotives.forEach((railyObj) => {
            if (!railyObj) return;

            const { objectId: oid, driverName, name, speedstep, speedstepMax, protocol, addr, funcdesc, direction } = railyObj;
            const rec = elGrid.find({ oid });
            if (rec.length === 0) {
                listOfObjectsToAdd.push({
                    oid,
                    driverName,
                    //locImage,
                    name,
                    speedstep,
                    speedstepMax,
                    protocol,
                    addr,
                    funcdesc,
                    direction
                });
            } else {
                const row = elGrid.get(rec);
                this.updateExistingLocomotive(elGrid, railyObj, row);
            }
        });

        if (listOfObjectsToAdd.length) {
            elGrid.add(listOfObjectsToAdd);
        }
    }

    updateExistingLocomotive(elGrid, railyObj, row) {

        this.__updateRowIfChanged(row[0], 'name', railyObj.name, elGrid, railyObj.objectId);
        this.__updateRowIfChanged(row[0], 'speedstep', railyObj.speedstep, elGrid, railyObj.objectId);
        this.__updateRowIfChanged(row[0], 'speedstepMax', railyObj.speedstepMax, elGrid, railyObj.objectId);
        this.__updateRowIfChanged(row[0], 'protocol', railyObj.protocol, elGrid, railyObj.objectId);
        this.__updateRowIfChanged(row[0], 'addr', railyObj.addr, elGrid, railyObj.objectId);
        this.__updateRowIfChanged(row[0], 'funcdesc', railyObj.funcdesc, elGrid, railyObj.objectId);
        this.__updateRowIfChanged(row[0], 'direction', railyObj.direction, elGrid, railyObj.objectId);

        const locCtrlInstance = window.findLocomotivesDlg(constLocomotiveControlBaseName + railyObj.objectId);

        if (locCtrlInstance) {
            try {
                locCtrlInstance.updateSpeedState(railyObj);
            } catch {
                // ignore
            }

            try {
                locCtrlInstance.updateDirectionState(railyObj);
            } catch {
                // ignore
            }

            try {
                locCtrlInstance.updateFunctionButtons(railyObj);
            } catch {
                // ignore
            }
        }
    }

    __updateRowIfChanged(row, field, value, elGrid, oid) {
        if (row[field] !== value) {
            row[field] = value;
            elGrid.refreshCell(oid, field);
        }
    }
}
