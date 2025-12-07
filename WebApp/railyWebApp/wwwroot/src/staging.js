// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class Staging {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__installed = false;
        this.__idHtml = "#overlayAufstellbockSettings";
        this.__gridName = "gridStagingSettings";
        this.__initEventHandling();
        this.__ctrlId = '';
        this.__systemInfo = null;
        this.__settingsInfo = null;
        this.__dataChanged = false;
        this.__allowUpdates = true;
        this.__listOfReservations = [];
    }

    __isBlockReserved(stageBoxId, add = false) {
        const isReserved = this.__listOfReservations.includes(stageBoxId);
        if (!isReserved && add) {
            this.__listOfReservations.push(stageBoxId);
        }
        return isReserved;
    }

    __removeBlockReservation(stageBoxId) {
        this.__listOfReservations = this.__listOfReservations.filter(id => id !== stageBoxId);
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

    install() {
        const self = this;
        if (self.__installed === true) return;
        $(document).on('click', function (ev) {
            if (self.isShown()) {
                if (!$(ev.target).closest(self.__idHtml).length) {
                    self.__onClose();
                    $(self.__idHtml).hide();
                    if (w2ui[self.__gridName])
                        w2ui[self.__gridName].destroy();
                }
            }
        });

        self.__updateGrid();

        self.__installed = true;
    }

    __onClose() {
        const self = this;

        this.__resetFb();
        window.__stagingFbOverCallback = null;
        this.__allowUpdates = true;

        const grid = w2ui[this.__gridName];
        if (!grid) return;

        //
        // onyl trigger setting change when really someting changed
        //
        if (this.__dataChanged) {
            const rows = [];
            for (let i = 0; i < grid.records.length; ++i) {
                const r = grid.records[i];
                if (r) {
                    rows.push({
                        driverName: r.driverName,
                        objectId: r.objectId,
                        length: r.length,
                        enter: r.enter,
                        occ: r.occ,
                        in: r.in
                    });
                }
            }

            self.__trigger('setting',
                {
                    command: 'update',
                    argument: 'staging',
                    argumentValue: {
                        stagingId: this.__ctrlId,
                        rows: rows
                    }
                });
        }
    }

    __resetFb() {
        $('div.ctrlItemSensors').removeClass('highlightSensor blockPlus blockMinus');
    }
    __highlightFb($menuEl) {
        const fbSensorId = $menuEl.text().trim();
        if (fbSensorId.length <= 0) return;
        $('div.ctrlItemSensors').removeClass('highlightSensor blockPlus blockMinus');
        $('#' + fbSensorId).addClass('highlightSensor');
    }

    isShown() {
        try {
            return w2ui['gridStagingSettings'] !== 'undefined'
                && w2ui['gridStagingSettings'] != null;
        } catch (error) {
            // ignore
        }
        return false;
    }

    show(target) {
        const self = this;

        window.__stagingFbOverCallback = this.__highlightFb;

        this.__dataChanged = false;
        this.__ctrlId = target.closest(".staging").attr("id");
        const $overlay = $('#overlayAufstellbockSettings');

        $overlay.css({
            left: event.pageX + 'px',
            top: event.pageY + 'px',
            width: '600px',
            height: '220px',
            display: 'block',
            position: 'absolute',
            background: 'white',
            border: '1px solid #ccc',
            'box-shadow': '0px 4px 6px rgba(0,0,0,0.1)',
            'z-index': 150
        }).show();

        if (w2ui[this.__gridName]) {
            w2ui[this.__gridName].destroy();
            w2ui[this.__gridName] = null;
        }

        const colLength = 2;
        const colLoco = 3;
        const colEnter = 4;
        const colOcc = 5;
        const colIn = 6;

        $overlay.w2grid({
            name: this.__gridName,
            header: `Aufstellblock: ${this.__ctrlId}`,
            box: '#overlayAufstellbockSettings',
            show: {
                //lineNumbers: true,
                toolbar: true,
                header: true,
                footer: false,
                toolbarAdd: true,
                toolbarDelete: true,
                toolbarEdit: false,
                toolbarSave: false
            },
            sortData: [{ field: 'position', direction: 'asc' }],
            onChange(event) {
                self.__resetFb();

                const elGrid = w2ui[self.__gridName];
                const rec = elGrid.get(event.recid);
                const column = event.column;

                self.__dataChanged = true;
                self.__allowUpdates = false;

                //
                // remove red triangle from the top right corner
                //
                setTimeout(() => {
                    let cell = $(`#grid_${self.__gridName}_data_${event.recid - 1}_${column}`);
                    cell.removeClass('w2ui-changed');
                }, 10);

                switch (column) {
                    case colLength:
                        rec.length = event.value_new;
                        elGrid.refreshCell(rec.recid, 'length');
                        break;

                    case colLoco:
                        const oldDriverName = rec.driverName;
                        const oldObjectId = rec.objectId;

                        self.__trigger('automode', {
                            command: 'update',
                            argument: 'removeLocomotiveFromBlock',
                            argumentValue: {
                                driverName: oldDriverName,
                                objectId: oldObjectId
                            }
                        });

                        const selectedLoc = event.value_new;
                        rec.occupiedBy = event.value_new.text;
                        rec.driverName = selectedLoc.driverName;
                        rec.objectId = selectedLoc.objectId;
                        elGrid.refreshCell(rec.recid, 'occupiedBy');
                        break;

                    case colEnter:
                        rec.enter = event.value_new.text;
                        elGrid.refreshCell(rec.recid, 'enter');
                        break;

                    case colOcc:
                        rec.occ = event.value_new.text;
                        elGrid.refreshCell(rec.recid, 'occ');
                        break;

                    case colIn:
                        rec.in = event.value_new.text;
                        elGrid.refreshCell(rec.recid, 'in');
                        break;
                }

                elGrid.save();
            },
            columns: [
                { field: 'recid', hidden: true },
                { field: 'step', caption: 'Position', size: '10%' },
                { field: 'length', caption: 'L&auml;nge', size: '10%', editable: { type: 'number' } },
                {
                    field: 'occupiedBy',
                    caption: 'Besetzt von:',
                    size: '25%',
                    editable: {
                        type: 'list',
                        items: __createLocomotiveList(),
                        showAll: true,
                        openOnFocus: true,
                        align: 'left'
                    }
                },
                {
                    field: 'enter',
                    caption: 'Enter',
                    size: '20%',
                    editable: __createEditableList(window.__sensorList.__sensorList)
                },
                {
                    field: 'occ',
                    caption: 'Belegt',
                    size: '20%',
                    editable: __createEditableList(window.__sensorList.__sensorList)
                },
                {
                    field: 'in',
                    caption: 'In',
                    size: '20%',
                    editable: __createEditableList(window.__sensorList.__sensorList)
                },
                { field: 'driverName', hidden: true },
                { field: 'objectId', hidden: true },
            ],
            onAdd: function (event) {
                const grid = w2ui['gridStagingSettings'];
                const len = grid.records.length
                grid.add({
                    recid: len,
                    step: len + 1,
                    length: '100',
                    occupiedBy: '',
                    enter: '',
                    occ: '',
                    in: ''
                });

                self.__dataChanged = true;

                const stageBlocks = self.__getStageBlocksOf(self.__ctrlId);

                self.__updateVisualization(this.__ctrlId, stageBlocks);
            },
            onDelete: function (event) {
                // default behaviour

                self.__dataChanged = true;
            }
        });

        $overlay.draggable();

        self.__updateGrid();

        function __createLocomotiveList() {
            const locs = window.locomotivesDlg.getLocomotives()
            const list = [{
                    recid: 0,
                    id: -1,
                    text: "-.-",
                    driverName: "",
                    objectId: -1
                }];
            for (let i = 1; i <= locs.length; ++i) {
                const loc = locs[i - 1];
                list.push({
                    id: i,
                    text: loc.name,
                    driverName: loc.driverName,
                    objectId: loc.oid
                });
            }
            return list;
        }

        function __createEditableList(items) {
            return {
                type: 'list',
                items: [{ id: -1, text: "-.-" }, ...items],
                filter: false
            };
        }
    }

    __clearGrid() {
        const elGrid = w2ui[this.__gridName];
        if (elGrid) {
            elGrid.clear(true);
            elGrid.reset();
        }
    }

    __getLocForStaging(stagingId, idx) {
        let loc = { occupiedBy: '', driverName: '', objectId: -1 };
        if (!stagingId) return loc;
        if (idx < 0) return loc;
        const locs = this.__settingsInfo.locomotives;
        for (let i = 0; i < locs.length; ++i) {
            const l = locs[i];
            if (!l) continue;
            if (l.assignedToBlock === `${stagingId}_${idx}`) {
                const recLoc = window.locomotivesDlg.getLocomotiveRecord(l.driverName, l.objectId);

                return {
                    occupiedBy: recLoc.name,
                    driverName: l.driverName,
                    objectId: l.objectId
                }
            }
        }
        return loc;
    }

    __getStageBlocksOf(stageIdentifier) {
        const stagings = this.__settingsInfo.stagings;
        for (let j = 0; j < stagings.length; ++j) {
            const stage = stagings[j];
            if (!stage) continue;
            if (stage.identifier === stageIdentifier)
                return stage;
        }
        return null;
    }

    __updateGrid() {

        if (!this.__allowUpdates) return;

        //
        // this method is used to fill the grid, when it is open
        // but anyway, it is used to provide planfield feedback of the stages
        //
        const elGrid = w2ui[this.__gridName];
        this.__clearGrid();
        const stagings = this.__settingsInfo?.stagings;
        if (!stagings) return;

        for (let j = 0; j < stagings.length; ++j) {
            const stage = stagings[j];
            if (!stage) continue;

            const blocks = stage.blocks;
            for (let i = 0; i < blocks.length; ++i) {
                const rowBlock = blocks[i];
                const stagedLoc = this.__getLocForStaging(stage.identifier, i);
                const rec = {
                    recid: i,
                    step: i + 1,
                    length: rowBlock.length,
                    occupiedBy: stagedLoc.occupiedBy,
                    driverName: stagedLoc.driverName,
                    objectId: stagedLoc.objectId,
                    enter: rowBlock.sensorEnter,
                    occ: rowBlock.sensorOcc,
                    in: rowBlock.sensorIn
                };

                if (elGrid && stage.identifier === this.__ctrlId)
                    elGrid.records.push(rec);
            }

            this.__updateVisualization(stage.identifier, blocks);

            for (let i = 0; i < blocks.length; ++i) {
                const stagedLoc = this.__getLocForStaging(stage.identifier, i);
                if (stagedLoc.occupiedBy.length > 0) {
                    this.__setBoxColor(stage.identifier, i, "red", stagedLoc.occupiedBy);
                } else {
                    const $box = $(`#${stage.identifier}_${i}`);
                    if (!$box.length) return;

                    this.__setBoxColor(stage.identifier, i, "", "");
                }
            }
        }

        if (elGrid)
            elGrid.refresh();
    }

    updateSystemInfo(systemInfo) {
        if (!systemInfo) return;
        this.__systemInfo = systemInfo;

        // to be defined
    }

    updateSettings(settings) {
        if (!settings) return;
        this.__settingsInfo = settings;
        this.__updateGrid();
    }

    /**
     * Sets the background color and tooltip text for a specific stage step.
     *
     * @param {string} stageIdentifier - The ID of the stage container.
     * @param {number} index - The index of the box within the stage.
     * @param {string} color - The background color to be applied.
     * @param {string} tooltipText - The text for the tooltip.
     */
    __setBoxColor(stageIdentifier, index, color, tooltipText) {
        const $box = $(`#${stageIdentifier}_${index}`);
        if (!$box.length) return;

        $box.data("tooltip", tooltipText);
        if (color && color.length > 0)
            $box.css("background-color", color)
    }

    resetDestination(blockIdentifier) {
        const ctrlsFinalBox = $(".stagingBox");
        for (let i = 0; i < ctrlsFinalBox.length; ++i) {
            const $box = $(ctrlsFinalBox[i]);
            if (!$box) continue;

            const stageBoxId = $box.attr("id");
            let result = this.__extractId(stageBoxId);

            this.__removeBlockReservation(stageBoxId);

            this.__setBoxColor(
                result.text,
                result.id,
                "lightgreen",
                ""
            );
        }
    }

    

    setDestination(data) {
        const finalBlock = $(`#${data.blockIdentifier}`);
        if (!finalBlock.hasClass("stagingBox")) return;
        const locomotiveRecord = data.locomotiveEntity;

        let result = this.__extractId(data.blockIdentifier);

        this.__isBlockReserved(data.blockIdentifier, true);

        this.__setBoxColor(
            result.text,
            result.id,
            "#ffeb3b",
            `${locomotiveRecord.name} (${locomotiveRecord.driverName}:${locomotiveRecord.objectId})`
        );
    }

    __extractId(input) {
        let match = input.match(/(\d+)$/);
        if (!match) return { text: input, id: null }; // Falls keine ID gefunden wurde
        let id = match[0]; // Die gefundene ID
        let text = input.slice(0, -id.length).replace(/[-_ ]+$/, ""); // Entfernt Trennzeichen am Ende
        return { text, id };
    }

    /**
     * Updates the visualization by rendering block elements inside the specified stage.
     * Each block is dynamically positioned and sized within the container.
     * 
     * @param {string} stageIdentifier - The ID of the target container.
     * @param {Array} blocks - Array of blocks to be displayed.
     */
    __updateVisualization(stageIdentifier, blocks) {
        const $stagingCtrl = $(`#${stageIdentifier}`);
        const count = blocks?.length ?? 0;

        let dummyOverlay = $stagingCtrl.find(".overlayStage");
        if (dummyOverlay) {
            dummyOverlay.off("mouseenter");
            dummyOverlay.off("mouseleave");
            dummyOverlay.off("mousemove");
            dummyOverlay.remove();
        }

        if (count === 0) return; // No blocks to render

        // Create overlay container
        const $overlay = $("<div class='overlayStage'></div>")
            .attr("style", "position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;")
            .appendTo($stagingCtrl);

        const padding = 10; // 10% padding on both sides
        const availableWidth = 100 - 2 * padding; // Width available for blocks
        const boxWidth = availableWidth / count;

        // Append block elements
        for (let i = 0; i < count; i++) {

            const stageBoxId = `${stageIdentifier}_${i}`;
            const isReserved = this.__isBlockReserved(stageBoxId);
            const stageBoxColor = isReserved === true ? '#ffeb3b' : 'lightgreen';

            $("<div>", {
                id: stageBoxId,
                class: "box stagingBox",
                text: i + 1,
            }).attr("style",
                `position:absolute;top:1px;left:${padding + i * boxWidth}%;
            width:${boxWidth}%;height:29px;background-color: ${stageBoxColor};);
            display:flex;align-items:center;justify-content:center;
            font-size:1em;border:1px dashed black;pointer-events:auto;`
            ).appendTo($overlay);
        }

        $overlay.off("mouseenter");
        $overlay.on("mouseenter", ".box", function (event) {
            $(".custom-tooltip").remove();
            const tooltipText = $(this).data("tooltip") || "";
            if (!tooltipText) return;
            $(this).data("tooltipElement", $("<div class='custom-tooltip'></div>")
                .text(tooltipText)
                .attr("style",
                    `position:absolute;background:black;color:white;padding:5px 10px;
                border-radius:5px;font-size:12px;top:${event.pageY + 10}px;
                left:${event.pageX + 10}px;z-index:1000;`
                ).appendTo("body"));
        });

        $overlay.off("mouseleave");
        $overlay.on("mouseleave", ".box", function () {
            $(this).data("tooltipElement")?.remove();
        });

        $overlay.off("mousemove");
        $overlay.on("mousemove", ".box", function (event) {
            $(this).data("tooltipElement")?.css({
                top: event.pageY + 10 + "px",
                left: event.pageX + 10 + "px"
            });
        });
    }
}