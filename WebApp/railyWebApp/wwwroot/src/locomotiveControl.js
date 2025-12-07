// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const constLocomotiveControlBaseName = "locomotiveCtrl_";
const constLocomotiveMinSpeed = 0;

// #region global Functions for LocomotiveControl

function getLocomotiveControlsUuid() {
    return window.__planfieldUuid + "_dialogStateLocomotiveControls";;
}

function restoreLocomotiveControlsIndividual() {
    const n = getLocomotiveControlsUuid();
    const dlgLocoControls = JSON.parse(localStorage.getItem(n)) || {};
    if (typeof dlgLocoControls !== "object" || dlgLocoControls === null) {
        // ignore
    } else {
        Object.keys(dlgLocoControls).forEach(accessor => {
            if (typeof accessor === "string" && accessor.includes("_")) {
                const [driverName, objectId] = accessor.split("_");
                const state = dlgLocoControls[accessor];
                if (state && state.open) {
                    const locomotiveRecord = window.locomotivesDlg.getLocomotiveRecord(driverName, objectId);
                    if (locomotiveRecord) {
                        openLocomotiveControlDialog(locomotiveRecord);
                    }
                }
            }
        });
    }
}

// #endregion

class LocomotiveControl {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__tplMain = "./components/dialogLocomotiveControl/locomotiveControl.html";
        this.__baseDlgId = constLocomotiveControlBaseName;
        this.__dlgId = null;
        this.__dlgElement = null;
        this.__dlgIsClosed = true;
        this.__windowGeometry = null;
        this.__locomotiveRecord = null;
        this.__speedTrigger = true;
        this.__initEventHandling();
    }

    // #region Window Restore

    __setDialogState(open, driverName, objectId) {
        const n = getLocomotiveControlsUuid();
        let dialogState = JSON.parse(localStorage.getItem(n)) || {};
        dialogState[driverName + "_" + objectId] = {
            open: open
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
     * Updates the speed state of the locomotive on the UI.
     *
     * This method updates the speed slider and the displayed speed value based on the provided
     * locomotive data. It ensures the slider value is set to the correct speedstep and updates
     * the displayed speed value without triggering unnecessary UI updates.
     *
     * @param {Object} dataLocomotive - An object containing the locomotive data.
     *        Expected properties:
     *        {
     *          speedstep: {number} - The current speedstep of the locomotive.
     *        }
     * @returns {void}
     */
    updateSpeedState(dataLocomotive) {
        if (this.__dlgIsClosed) return;

        const speedDisplay = this.__dlgElement.find(".speedDisplay");
        speedDisplay.text(dataLocomotive.speedstep);

        const sliderEl = this.__dlgElement.find(".locomotiveSpeedometer");
        if (sliderEl.length > 0) {
            this.__speedTrigger = false;
            try {
                sliderEl[0].value = dataLocomotive.speedstep;
            } catch (err) {
                //console.error(err);
            }
            this.__speedTrigger = true;
        }
    }

    /**
     * Updates the visual state of the direction control for a locomotive.
     *
     * This method resets the colors of the forward and backward buttons and highlights
     * the currently active direction (forward or backward) based on the provided locomotive data.
     *
     * @param {Object} dataLocomotive - An object containing the locomotive data.
     *        Expected properties include:
     *        {
     *          objectId: {number} - The unique ID of the locomotive,
     *          direction: {number} - The direction of the locomotive (0 = forward, 1 = backward)
     *        }
     * @returns {void}
     */
    updateDirectionState(dataLocomotive) {
        if (this.__dlgIsClosed) return;

        const selfData = dataLocomotive;

        setTimeout((ev) => {
            //const driverName = selfData.driverName;
            const objectId = selfData.objectId ?? selfData.oid;

            const ctrl = $('#locomotiveCtrl_' + objectId);
            const cmdBackward = ctrl.find('.directionBackward:first');
            const cmdForward = ctrl.find('.directionForward:first');

            // Reset styles
            const resetStyle = { color: "black", background: "" };
            cmdBackward.css(resetStyle);
            cmdForward.css(resetStyle);

            // Update the relevant button style
            const activeStyle = { color: "green", background: "lightgreen" };
            if (selfData.direction === 1) {
                cmdBackward.css(activeStyle);
            } else if (selfData.direction === 0) {
                cmdForward.css(activeStyle);
            }
        }, 50);
    }

    /**
     * TODO
     * @param {any} locomotiveRecord
     * @returns
     */
    open(locomotiveRecord) {
        const self = this;
        const { name, speedstep, oid, driverName } = locomotiveRecord;

        this.__locomotiveRecord = locomotiveRecord;
        this.__dlgId = `${this.__baseDlgId}${oid}`;
        this.__dlgSelector = `#${this.__dlgId}`;
        this.__windowGeometry = new WindowGeometryStorage(this.__dlgId);

        this.__setDialogState(true, driverName, oid);

        const existingDialog = $(this.__dlgSelector);
        if (existingDialog.length > 0) return false;

        this.__dlgIsClosed = false;

        // Clone the template for the dialog
        const tplLocomotiveControl = $('div[id=tplLocomotiveControl]');
        const ctrl = tplLocomotiveControl.clone();
        ctrl.attr({
            id: this.__dlgId,
            title: name,
        })
            .addClass("locomotiveControlMain")
            .css({
                display: "flex"
            });

        this.__dlgElement = ctrl;

        const geometry = this.__windowGeometry.recent();
        ctrl.dialog({
            resizable: false,
            autoOpen: false,
            position: { my: "left top", at: `left+${geometry.left} top+${geometry.top}`, of: window },
            resizeStop: (event, ui) => {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop: (event, ui) => {
                self.__windowGeometry.save(ui.position, {
                    width: event.target.clientWidth,
                    height: event.target.clientHeight
                });
            },
            close: () => {
                self.__setDialogState(false,
                    this.__locomotiveRecord.driverName,
                    this.__locomotiveRecord.oid);
                ctrl.remove();
                self.__trigger('dialogClosed', { instance: this });
            }
        });

        const speedDisplay = ctrl.find('.speedDisplay');
        const speedometer = ctrl.find(".locomotiveSpeedometer");
        speedometer.attr({
            "min": constLocomotiveMinSpeed,
            "max": self.__locomotiveRecord?.speedstepMax || 0,
            "value": speedstep
        });
        speedDisplay.text(speedstep);
        speedometer.off("input");
        speedometer.on("input", function () {
            if (!self.__speedTrigger) return;
            const uiValue = parseInt($(this).val());
            const oid = self.__locomotiveRecord?.oid;
            const driverName = self.__locomotiveRecord.driverName;
            if (!oid) {
                console.warn("No locomotive record found. Cannot update speedstep.");
                return;
            }

            speedDisplay.text(uiValue);

            self.__trigger('changed', {
                objectId: oid,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: driverName,
                    value: uiValue
                }
            });
        });

        const fakeId = `img_${this.__dlgId}`;
        const locImage = ctrl.find(".locomotiveImage");
        locImage.attr({
            id: fakeId,
            src: "./images/noimage32x32.png",
            alt: name
        });

        loadLocomotiveImageIntoHtml(fakeId, name);

        const handleSpeedClick = (ev) => {
            const button = $(ev.currentTarget);
            const speedstep = button.data("speedstep");
            const oid = self.__locomotiveRecord?.oid;
            const driverName = self.__locomotiveRecord.driverName;
            if (!oid) {
                console.warn("No locomotive record found.");
                return;
            }
            self.__trigger('changed', {
                objectId: oid,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: driverName,
                    value: speedstep
                }
            });
        };

        const allBtns = ctrl.find('button').not("button[data-function]");
        allBtns.each(function () {
            const btn = $(this);
            btn.button();

            // Apply styles and bind events based on data attributes
            if (btn.data("speedstep")) {
                btn.click(handleSpeedClick);
            } else if (btn.data("direction")) {
                btn.click(() => {
                    const oid = self.__locomotiveRecord?.oid;
                    const driverName = self.__locomotiveRecord.driverName;
                    if (oid) {
                        self.__trigger('changed', {
                            objectId: oid,
                            command: 'update',
                            argument: 'direction',
                            argumentValue: {
                                driverName: driverName,
                                direction: btn.data("direction")
                            }
                        });
                    }
                });
            }
        });

        this.__appendFunctionButtons(ctrl);

        const dialogTitle = ctrl.data("ui-dialog").originalTitle;
        bringToFront(dialogTitle);

        //
        // load QR code
        //
        const loadQrCode = true;
        if (loadQrCode) {
            const hostname = window.location.hostname;
            //const port = window.location.port || (window.location.protocol === "https:" ? "443" : "80");

            // query Workspace name
            const url = new URL(window.location.href);
            const params = new URLSearchParams(url.search);
            let workspaceName = '';
            if (params.has('workspace')) {
                workspaceName = params.get('workspace');
            }

            const locControlUrl = `${window.location.protocol}//${hostname}:5001/locomotive`
                + `?driverName=${driverName}`
                + `&objectId=${oid}`
                + `&workspace=${workspaceName}`
                + `&uid=${window.__access_uid}`
                + `&hasService=${window.__access_hasService}`
                + `#accessKey=${window.__access_token}`

            const cmdOpenCfgDialog = ctrl.find('.openSettingsDialog');
            cmdOpenCfgDialog.off('click');
            cmdOpenCfgDialog.on('click',
                () => {
                    const rec = window.locomotivesDlg.getLocomotiveRecord(driverName, oid);
                    if (rec) {

                        const driverName = rec.driverName;
                        const objectId = rec.oid;
                        const name = rec.name;

                        window.locomotivesDlg.openProperties(driverName, objectId, name);
                    }
                });

            const qrOpener = ctrl.find(".qrcodeIcon");
            qrOpener.off('click');
            qrOpener.on('click',
                () => {
                    const container = $("#tplLocomotiveControlQrOverlay").find('#qrContainer');
                    container.find("canvas").remove()
                    const canvas = document.createElement("canvas");
                    $(canvas).data('qrCodeUrl', locControlUrl);
                    container.find('#qrcode')[0].appendChild(canvas);
                    QRCode.toCanvas(canvas, locControlUrl, { width: 500 }, (error) => {
                        if (error) {
                            console.error(error);
                            return;
                        } else {
                            container.parent().fadeIn();
                        }
                    });
                    $(canvas).off('click');
                    $(canvas).on('click', (ev) => {
                        const url = $(ev.target).data('qrCodeUrl');
                        if (navigator.clipboard) {
                            navigator.clipboard.writeText(url)
                                .then(() => { console.log("Clipboard succeeded") })
                                .catch(err => console.error("Clipboard failed:", err));
                        } else {
                            console.error("Clipboard API not supported");
                        }
                    });
                });
        }

        ctrl.dialog("open");

        // ✅ Overlay schließen beim Klick auf "X"
        $("#qrClose").on("click", function () {
            $("#tplLocomotiveControlQrOverlay").fadeOut();
        });

        // ✅ Klick außerhalb des QR-Codes schließt das Overlay
        $("#tplLocomotiveControlQrOverlay").on("click", function (event) {
            if (!$(event.target).closest("#qrContainer").length) {
                $(this).fadeOut();
            }
        });

        return true;
    }

    __getFunctionDescription(fncType) {
        let fncDescHuman = ListOfFunctionDescription[fncType];
        if (!fncDescHuman) return "Function";
        return fncDescHuman;
    }

    __getFunctionIcon(fncType) {
        let icon = ListOfFunctionIcons[fncType];
        if (!icon) return "<span class=\"icon-placeholder\">❓</span>";
        return "<span class=\"" + icon + "\" style=\"margin-left: 5px;\"></span>";;
    }

    __appendFunctionButtons(ctxCtrl) {
        const target = ctxCtrl.find('.locomotiveCommandField');
        const { funcdesc, oid, driverName } = this.__locomotiveRecord;

        funcdesc.forEach(({
            idx: idx,
            state: fncState,
            type: fncType,
            moment: fncMoment,

            fncIdx: fncIdx,
            name: fncName,
            description: fncDescription,
            icon: fncIcon,
            isUsed: fncIsUsed
        }) => {

            let show = fncIsUsed === true;
            if (!show)
                if (fncType === 0) return;

            if (typeof (fncIdx) == 'undefined') {
                fncIdx = idx;
            }

            const self = this;
            const className = `fncCommand_${oid}_${fncIdx}`;

            let desc = fncDescription;
            try {
                const parsed = JSON.parse(fncDescription);
                desc = parsed.caption;
            } catch (err) {
                // ignore
            }
            if (!desc || desc.length <= 0) {
                desc = this.__getFunctionDescription(fncType);

                var descIcon = this.__getFunctionIcon(fncType);
                desc += descIcon;
            }
            
            let name = fncName;
            if (!name || name.length <= 0)
                name = `${fncIdx}:${desc}`

            if (fncIcon && fncIcon.length > 0)
                name = renderLocomotiveFunctionImage(fncIcon);

            const btn = $('<button>', {
                html: name,
                class: `locomotiveCommand ${className}`,
                data: {
                    fncIdx: fncIdx,
                    fncState: fncState,
                    'tipso-title': desc
                }
            });
            btn.off('click');
            btn.on('click', (ev) => {
                const fncIdx = $(ev.currentTarget).data("fncIdx");
                const fncState = $(ev.currentTarget).data("fncState");
                const targetState = fncState ? 0 : 1;
                self.__trigger('changed', {
                    objectId: oid,
                    command: 'update',
                    argument: 'function',
                    argumentValue: {
                        driverName: driverName,
                        value: [fncIdx, targetState]
                    }
                });
            });
            btn.appendTo(target);

            setTimeout(function (ev) {
                btn.css({ "color": fncState ? "green" : "red" });
                btn.tipso({
                    size: 'tiny',
                    speed: 100,
                    delay: 250,
                    useTitle: true,
                    width: "auto",
                    background: '#333333',
                    titleBackground: '#333333'
                });
            },
                125);


        });
    }

    updateFunctionButtons(locomotiveRecord) {
        if (!locomotiveRecord) return;
        if (!locomotiveRecord.funcdesc) return;

        const objectId = locomotiveRecord.objectId;
        locomotiveRecord.funcdesc.forEach(it => {
            const btn = $(`.fncCommand_${objectId}_${it.idx}`);
            btn.css({ "color": it.state ? "green" : "red" });
            btn.data("fncState", it.state);
        });
    }

    __getGlobalLocomotiveData(driverName, objectId) {
        if (window.__sidebarRailyData) {
            const locomotivesData = window.__sidebarRailyData.locomotives;
            if (locomotivesData) {
                for (let i = 0; i < locomotivesData.length; ++i) {
                    const data = locomotivesData[i];
                    if (data) {
                        if (data.driverName === driverName && data.objectId == objectId) {
                            return data;
                        }
                    }
                }
            }
        }

        return null;
    }
}