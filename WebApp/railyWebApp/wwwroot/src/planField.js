// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


const constPlanEditModeSerializationName = window.__planfieldUuid + "_editMode";

const constTxtAnsteuerungIndex = 'Ansteuerung  <span style="font-size: 70%;">(Index)</span>';
const constTxtAnsteuerungPin = 'Ansteuerung  <span style="font-size: 70%;">(Modul:Pin)</span>';

// used to load edit mode with Toolbar when previous session was in edit mode
async function restoreEditMode() {
    while (!window.themeData || window.themeData.length === 0) {
        await new Promise(resolve => setTimeout(resolve, 250));
    }
    const state = JSON.parse(localStorage.getItem(constPlanEditModeSerializationName)) || false;
    if (state === true && window.planfield) {
        try {
            startEditMode();

            w2ui.sidebar.expand('level-2');

        } catch {
            restoreEditMode();
        }
    }
}

// used to persist current edit mode (i.e. Layout)
function saveEditMode(state) {
    localStorage.setItem(constPlanEditModeSerializationName, state);
}

/**
 * Loads an image asynchronously.
 *
 * This function creates a new Image object, sets its source to the provided URL,
 * and returns a Promise that resolves when the image has loaded successfully.
 * If an error occurs (e.g., the image cannot be found), the Promise rejects with an error message.
 *
 * @param {string} src - The URL of the image to load.
 * @returns {Promise<HTMLImageElement>} A Promise that resolves to the loaded image element.
 */
function loadImageAsync(src) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.src = src;
        img.onload = () => resolve(img);
        img.onerror = () => reject(new Error("Image could not be loaded: " + src));
    });
}

/**
 * Adds an image to a specified target element.
 *
 * This asynchronous function constructs the image path based on the given theme data,
 * loads the image using `loadImageAsync`, and appends it to the specified HTML element.
 * If the image cannot be loaded, an error is logged to the console.
 *
 * @param {Object} themeData - The theme data containing the image basename.
 * @param {string} targetCtrl - The target HTML element where the image should be added.
 * @returns {Promise<void>} A Promise that resolves when the image is successfully added or logs an error if it fails.
 */
async function addImage(themeData, targetCtrl) {
    const icon = themeData.basename + ".png";
    const imagePath = `theme/${window.themeName}/${icon}`;
    // Erstelle das Dummy-Image mit Blur-Effekt
    const dummyImg = document.createElement("div");
    dummyImg.style.width = targetCtrl.css("width");
    dummyImg.style.height = targetCtrl.css("height");
    dummyImg.style.background = "#ccc";
    dummyImg.style.filter = "blur(10px)";
    dummyImg.style.display = "flex";
    dummyImg.style.alignItems = "center";
    dummyImg.style.justifyContent = "center";
    dummyImg.innerText = "Loading...";
    const targetElement = await waitForElement(targetCtrl);
    targetElement?.appendChild(dummyImg);
    try {
        const img = await loadImageAsync(imagePath);
        img.style.transition = "opacity 0.5s ease-in-out";
        img.style.opacity = "0"; // Anfangs unsichtbar
        img.style.top = "0px";
        img.style.left = "0px";
        img.style.position = "absolute";
        targetElement.replaceChild(img, dummyImg);
        setTimeout(() => {
            img.style.opacity = "1";
        }, 50);
    } catch (error) {
        console.log(error);
    }
}

async function addImageDirect(themeData, targetCtrl) {
    const icon = themeData.basename + ".png";
    const imagePath = `theme/${window.themeName}/${icon}`;
    const targetElement = await waitForElement(targetCtrl);
    try {
        const img = await loadImageAsync(imagePath);
        //img.style.transition = "opacity 0.5s ease-in-out";
        //img.style.opacity = "0"; // Anfangs unsichtbar
        img.style.top = "0px";
        img.style.left = "0px";
        img.style.position = "absolute";
        targetElement.appendChild(img);
        //setTimeout(() => {
        //    img.style.opacity = "1";
        //}, 50);
    } catch (error) {
        console.log(error);
    }
}


// Funktion, die wartet, bis das Element verfügbar ist
function waitForElement(targetCtrl, timeout = 5000) {
    return new Promise((resolve, reject) => {
        const startTime = Date.now();
        const check = () => {
            if (targetCtrl[0]) {
                resolve(targetCtrl[0]);
            } else if (Date.now() - startTime > timeout) {
                reject(new Error(`Timeout: Element #${id} nicht gefunden`));
            } else {
                setTimeout(check, 50);
            }
        };
        check();
    });
}

// Beispiel fuer eine Asynchrone Bildlade-Funktion
function loadImageAsync(src) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.src = src;
        img.onload = () => resolve(img);
        img.onerror = reject;
    });
}

class Planfield {

    __getBoundingCoord($el) {
        const rect = $el.get(0).getBoundingClientRect();
        const coordX = parseInt(rect.left / constItemWidth);
        const coordY = parseInt(rect.top / constItemHeight);
        return { x: coordX, y: coordY };
    }

    __getBoundingBoxOfAllControls() {
        const ctrls = $('div.ctrlItem');

        const offset = $('.ries-container')[0].getBoundingClientRect();

        if (ctrls.length > 0) {
            let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;

            ctrls.each(function () {
                let rect = this.getBoundingClientRect();
                minX = Math.min(minX, rect.left);
                minY = Math.min(minY, rect.top);
                maxX = Math.max(maxX, rect.right);
                maxY = Math.max(maxY, rect.bottom);
            });

            let boundingBox = {
                x: minX - offset.x,
                y: minY - offset.y,
                width: maxX - minX,
                height: maxY - minY
            };

            return boundingBox;
        }

        return null;
    }

    /**
 * Displays a fake progress indicator with a spinning icon, countdown, and progress bar.
 */
    showFakeProgress() {
        // Erstelle das Overlay mit Spinner, Countdown & Progressbar
        const overlay = document.createElement("div");
        overlay.id = "screenshot-progress-overlay";
        overlay.innerHTML = `
        <div class="progress-container">
            <div class="spinner"></div>
            <p>Screenshot wird vorbereitet...... <span id="countdown">10</span>s</p>
            <div class="progress-bar">
                <div class="progress-fill" id="progress-fill"></div>
            </div>
        </div>
    `;
        document.body.appendChild(overlay);

        // Countdown und Progress-Bar starten
        let timeLeft = 10;
        const countdownElement = document.getElementById("countdown");
        const progressFill = document.getElementById("progress-fill");

        const interval = setInterval(() => {
            timeLeft--;
            countdownElement.textContent = timeLeft;
            progressFill.style.width = `${(10 - timeLeft) * 10}%`;

            if (timeLeft <= 0) {
                clearInterval(interval);
            }
        }, 1000);
    }

    /**
     * Removes the fake progress indicator from the DOM.
     */
    hideFakeProgress() {
        const overlay = document.getElementById("screenshot-progress-overlay");
        if (overlay) {
            overlay.remove();
        }
    }

    createScreenshot() {
        const self = this;

        this.showFakeProgress();

        const boundingBox = this.__getBoundingBoxOfAllControls();
        if (boundingBox == null) return;
        addDebugText("Screenshot wird vorbereitet...");

        requestAnimationFrame(() => {
            setTimeout(() => {

                const plan = $('#gleisplan_tabPlan1').find('.ries-grid');
                html2canvas(plan[0],
                    {
                        onclone: function () {
                            addDebugText("Rendern laeuft...");
                        },
                        ...boundingBox
                    }
                ).then(canvas => {
                    const imageData = canvas.toDataURL("image/png");

                    self.__trigger('setting', {
                        command: 'update',
                        argument: 'screenshot',
                        argumentValue: {
                            workspaceName: window.workspaceName,
                            imageData: imageData
                        }
                    });

                    self.hideFakeProgress();

                    addDebugText("Fertig!");

                    w2popup.open({
                        title: 'Screenshot gespeichert!',
                        width: 600,
                        height: 430,
                        modal: true,
                        body: `<div style="display: flex; flex-direction: column; justify-content: center; align-items: center; height: 100%;">
                <img id="screenshot-img" src="${imageData}" style="max-width: 100%; max-height: 100%; object-fit: contain;">
                <a id="download-link" href="${imageData}" download="screenshot.png" 
                   style="margin-top: 10px; display: inline-block; padding: 8px 16px; background: #0078D7; color: white; text-decoration: none; border-radius: 4px;">
                   Herunterladen
                </a>
           </div>`,
                        buttons: `<button class="w2ui-btn" onclick="w2popup.close();">Schlie&szlig;en</button>`
                    });

                }).catch(error => {
                    addDebugText("Fehler beim Screenshot: " + error);
                });

            }, 10);
        });
    }

    /**
     *  
     * @param {any} options : {
     */
    constructor(options = {}) {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.planfieldElement = null;
        this.isEditMode = false;
        this.currentSelection = null;
        this.currentSelectionMoved = false;
        this.currentSelectionMovedItem = null;
        this.options = options;
        this.__settingsInfo = null;
        this.__recentAccessories = null;
        this.__recentSensorsData
        this.__initEventHandling();
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

    /**
     * Toggles the edit mode state for the plan field and all associated elements.
     * 
     * When the edit mode is enabled, the plan field background is updated and drag functionality
     * is initialized for each control element. When disabled, the background is removed and drag 
     * functionality is cleared.
     * 
     * The function also updates the edit mode state for all text field element instances.
     * 
     * @param {boolean} state - The desired edit mode state (true for enabling, false for disabling).
     */
    setEditMode(state) {
        this.isEditMode = state;
        this.clearCtrlSelection();

        saveEditMode(state);

        // Toggle planfield background based on state
        this.planfieldElement.toggleClass("planfieldBackground", state);

        // Update edit mode for each text field element instance
        window.textfieldElementInstances.forEach(instance => instance.setEditMode(state));

        // Initialize or remove drag functionality for each control item
        const ctrls = $('div.ctrlItem');
        ctrls.each((_, c) => {
            const ctrl = $(c);
            if (state) {
                this.__initDragFor(ctrl);
            } else {
                this.__removeDragFor(ctrl);
            }
        });

        if (this.isEditMode === true) {
            this.__removeValidateIndicators();
            this.__removeAccessoriesIndicators();
            this.__removeBlockIndicators();
        } else {
            this.validateSensors();
            this.validateAccessories();
            this.validateBlocks();
        }
    }

    /**
     * Checks if a given control element is selected by verifying if the associated image has the 'selected' class.
     * 
     * @param {jQuery | HTMLElement} jqueryElement - The jQuery element or native DOM element to check.
     * @returns {boolean} Returns true if the element is selected, otherwise false.
     */
    __isCtrlSelected(jqueryElement) {
        if (!jqueryElement) return false;
        const $element = $(jqueryElement);
        const imgElement = $element.find("img").first();
        return imgElement.length > 0 && imgElement.hasClass("selected");
    }

    __getCtrl(item, ctrls) {
        const x = item.x;
        const y = item.y;
        const xpx = x * constItemWidth + (constItemWidth / 2);
        const ypx = y * constItemHeight + (constItemHeight / 2);
        const ctrl = getCtrlOfPosition(xpx, ypx, ctrls);
        if (ctrl instanceof jQuery) return ctrl;
        return $(ctrl);
    }

    /**
     * Clears the route highlight by removing the 'highlightRoute' class from all control items.
     * 
     * This function iterates through all `div.ctrlItem` elements and removes the 'highlightRoute' class
     * from those elements that have it.
     */
    clearRouteUserHighlight() {
        const ctrls = $('div.ctrlItem');
        if (!ctrls.length) return;
        ctrls.removeClass('highlightRoute');
    }

    /**
     * Highlights route elements (tracks, sensors, signals, and switches) by adding the 'highlightRoute' class.
     * This function iterates through the provided elements and applies the highlight to their respective control elements.
     * 
     * @param {Object} elements - Contains arrays of tracks, sensors, signals, and switches.
     * Each array should contain the respective elements to be highlighted.
     */
    activateRouteUserHighlight(elements) {
        const self = this;
        const ctrls = $('div.ctrlItem');

        const highlightElements = (elementArray) => {
            elementArray.forEach(element => {
                const jqCtrl = self.__getCtrl(element, ctrls);
                jqCtrl.addClass('highlightRoute');
            });
        };

        highlightElements(elements.tracks);
        highlightElements(elements.sensors);
        highlightElements(elements.signals);
        highlightElements(elements.switches);
    }

    /**
     * Activates the route visualization by updating the image source of the elements related to the route (tracks, sensors, signals, switches).
     * This function combines all route elements into a single array and updates the image source for each corresponding control element.
     * 
     * @param {Object} route - Contains arrays of tracks, sensors, signals, and switches, which are to be visualized.
     * Each array should contain the respective elements to be visualized.
     */
    activateRouteVisualization(route) {

        // Combine all route elements into a single array
        const allTrackElements = [
            ...route.tracks,
            ...route.sensors,
            ...route.signals,
            ...route.switches
        ];

        const self = this;
        const ctrls = $('div.ctrlItem');

        allTrackElements.forEach(el => {
            if (el == null) return;
            const jqCtrl = self.__getCtrl(el, ctrls);
            const img = jqCtrl.find("img");
            if (!img || img.length === 0) return;
            let imgsrc = img.attr("src");
            if (imgsrc.includes("-route.png")) return;
            img.attr("src", imgsrc.replace(".png", "-route.png"));
        });
    }

    /**
     * Deactivates the route visualization by restoring the image source of the elements related to the route (tracks, sensors, signals, switches).
     * This function combines all route elements into a single array and updates the image source for each corresponding control element, 
     * reverting the images to their original state.
     * 
     * @param {Object} route - Contains arrays of tracks, sensors, signals, and switches, which are to be deactivated.
     * Each array should contain the respective elements to be visualized.
     */
    deactivateRouteVisualization(route) {
        // Combine all route elements into a single array
        const allTrackElements = [
            ...route.tracks,
            ...route.sensors,
            ...route.signals,
            ...route.switches
        ];

        const self = this;
        const ctrls = $('div.ctrlItem');

        allTrackElements.forEach(el => {
            if (el == null) return;
            const jqCtrl = self.__getCtrl(el, ctrls);
            const img = jqCtrl.find("img");
            if (!img || img.length === 0) return;
            let imgsrc = img.attr("src");
            if (!imgsrc.includes("-route.png")) return;
            img.attr("src", imgsrc.replace("-route.png", ".png"));
        });
    }

    /**
     * Clears the selection for all control items by removing the 'selected' state from each one.
     * 
     * This function finds all control items (`div.ctrlItem`), checks whether they are selected,
     * and calls `__unselectCtrl` to remove the selection from those that are selected.
     */
    clearCtrlSelection() {
        const ctrls = $('div.ctrlItem');
        if (!ctrls.length) return;
        ctrls.each((_, itm) => {
            if (this.__isCtrlSelected(itm)) {
                this.__unselectCtrl($(itm));
            }
        });
    }

    __unselectCtrl(jqueryElement) {
        if (typeof jqueryElement === "undefined") return;
        if (jqueryElement == null) return;

        let newCtrlImg = jqueryElement.find("img");
        if (typeof newCtrlImg === "undefined")
            newCtrlImg = null;

        if (newCtrlImg == null) return;

        newCtrlImg.removeClass("selected");
        this.editMenubar.hideEditMenu();
        this.currentSelection = null;
    }

    __selectCtrl(jqueryElement, options = {}) {
        if (typeof jqueryElement === "undefined") return;
        if (jqueryElement == null) return;

        this.clearCtrlSelection();

        const newCtrlImg = jqueryElement.find("img");
        if (typeof newCtrlImg !== "undefined" && newCtrlImg != null)
            newCtrlImg.addClass("selected");

        this.currentSelection = jqueryElement;
        if (typeof options.startEditMode === "undefined")
            options.startEditMode = true;
        if (options.startEditMode) {
            this.editMenubar.setCurrentSelection(jqueryElement);
            this.editMenubar.updateEditMenuPositionAndEvents(jqueryElement);
            this.editMenubar.showEditMenu();
        }
    }

    /**
     * 
     */
    install() {
        const self = this;

        this.editMenubar = new EditMenuBar(this);

        this.planfieldTabs = $('#gleisplaeneTabs');
        this.planfieldTabs.gleisplanTabs();
        this.planfieldElement = $('#gleisplan_tabPlan1');
        this.planfieldElement.on('contextmenu',
            (ev) => {
                ev.preventDefault();
            });
        this.planfieldElement.click(function (ev) {
            if (self.currentSelectionMoved) {
                ev.preventDefault();
                ev.stopPropagation();
                self.currentSelectionMoved = false;
                return;
            }

            //const containerX = ev.offsetX;
            //const containerY = ev.offsetY;
            //console.log(`X=${containerX}, Y=${containerY}  (pos)`);

            const ctrl = getCtrlOfEvent($(this).find('.ries-container'), ev, $('div.ctrlItem'));

            if (ctrl != null && self.isEditMode === false) {
                const themeItemObject = ctrl.data(constDataThemeItemObject);
                const themeData = self.__getThemeInformationById(themeItemObject.editor.themeId);

                if (themeData && themeData.clickable) {
                    //
                    // show click spinner for some seconds
                    //
                    if (!self.isEditMode) {

                        const coord = getElementCoord(self.planfieldElement, ev);
                        self.__trigger('itemClicked',
                            {
                                command: 'itemClicked',
                                argument: 'execute',
                                argumentValue: {
                                    ctrlIdentifier: ctrl.attr('id'),
                                    x: coord.x,
                                    y: coord.y
                                }
                            });
                    }
                }
            } else {

                if (self.isEditMode) {

                    if (!ctrl) {
                        const tb = window.toolbox;
                        if (tb.recentItem) {
                            self.__createControlWithEv(tb.recentItem, ev);
                        }
                    } else {

                        if (self.currentSelection)
                            return;

                        const tb = window.toolbox;
                        try {
                            if (tb?.recentItem) {
                                self.__createControlWithEv(tb.recentItem, ev);
                            }
                        }
                        catch (err) {
                            console.log(err);
                        }

                    }
                }
            }
        });

        this.planfieldElement.get(0).ondragover = ev => {
            if (!self.isEditMode) return;
            ev.preventDefault();
        }
        this.planfieldElement.get(0).ondrop = ev => {
            if (!this.isEditMode) return;

            //const containerX = ev.offsetX;
            //const containerY = ev.offsetY;
            //console.log(`X=${containerX}, Y=${containerY}  (drop)`);

            self.__createControlOnDrop(ev, {
                editMode: false,
                offset: { x: ev.offsetX, y: ev.offsetY }
            });
        }

        $(document).keyup(function (e) {

            const $self = self;

            if (!$self.isEditMode || !$self.currentSelection) return;

            if (e.keyCode === 27) // esc
                $self.clearCtrlSelection();

            let moveX = 0;
            let moveY = 0;
            let wasMove = false;

            switch (e.key) {
                // rotate clockwise
                case "r":
                    if (!$self.planfieldElement.hasClass("elEditorRoot")) // für Texteditoren ist Rotieren nicht erlaubt
                        $self.editMenubar.rot(0, $self.currentSelection, $self.__isInitialization);
                    break;

                // rotate counter clockwise
                case "R":
                    if (!$self.planfieldElement.hasClass("elEditorRoot")) // für Texteditoren ist Rotieren nicht erlaubt
                        $self.editMenubar.rot(1, $self.currentSelection, $self.__isInitialization);
                    break;

                // moving
                case "ArrowLeft": // LINKS - Element nach links verschieben
                    moveX = -1;
                    wasMove = true;
                    break;
                case "ArrowRight": // RECHTS - Element nach rechts verschieben
                    moveX = 1;
                    wasMove = true;
                    break;
                case "ArrowUp": // OBEN - Element nach oben verschieben
                    moveY = -1;
                    wasMove = true;
                    break;
                case "ArrowDown": // UNTEN - Element nach unten verschieben
                    moveY = 1;
                    wasMove = true;
                    break;

                // add/delete
                case "Delete": // DELETE - Element löschen
                    if ($self.currentSelection) {
                        const itemId = $self.currentSelection.attr("id");
                        $self.editMenubar.removeControl(itemId);
                    }
                    break;
            }

            if (wasMove === true)
                $self.__moveSelectedControlByGrid(moveX, moveY);
        });

        restoreEditMode();
    }

    __moveSelectedControlByGrid(dx, dy) {
        const $self = this;

        if (!$self.currentSelection) return;

        const elmnt = $self.currentSelection.get(0);
        const elementId = $self.currentSelection.attr("id");

        // Aktuelle Position holen
        let top = parseInt(elmnt.style.top, 10) || 0;
        let left = parseInt(elmnt.style.left, 10) || 0;

        // Neue Position berechnen
        top += dy * constItemHeight;
        left += dx * constItemWidth;

        // Grenzen prüfen (optional, falls nötig)
        if (top < 0) top = 0;
        if (left < 0) left = 0;

        // Erst das alte Control beim Server entfernen
        $self.removeControl(elementId);

        // Position setzen
        elmnt.style.top = top + "px";
        elmnt.style.left = left + "px";

        // Falls es sich nicht um ein Textfeld handelt, Menü-Position aktualisieren
        const isTextFieldCtrl = $self.currentSelection.hasClass("elEditorRoot");
        if (!isTextFieldCtrl) {
            $self.editMenubar.updateEditMenuPositionAndEvents($self.currentSelection);
        }

        // Control an neuer Position an den Server senden
        $self.sendControl($self.currentSelection);
    };

    __initBlockDrop(jqueryEl) {
        if (typeof jqueryEl === "undefined") return;
        if (jqueryEl === null) return;

        const self = this;
        const childs = jqueryEl.find("img");
        let i;
        const iMax = childs.length;
        for (i = 0; i < iMax; ++i) {
            $(childs[i]).droppable({
                accept: "div.locomotiveInfo",
                //drop: function (event, ui) {
                //    console.log("TODO loc dropped: " + )
                //}
            });
        }

        jqueryEl.get(0).ondragover = function (ev) {
            ev.preventDefault();
        };

        jqueryEl.get(0).ondrop = function (ev) {
            try {
                ev.preventDefault();

                const data = ev.dataTransfer.getData("text");
                const jsonData = JSON.parse(data);
                const srcGrid = w2ui[jsonData.sourceGrid];
                const gridRecId = parseInt(jsonData.recid);
                if (gridRecId) {
                    //const grid = w2ui['gridLocomotives'];
                    const rec = srcGrid.find({ oid: gridRecId });
                    const row = srcGrid.get(rec[0]);

                    self.__trigger('automode', {
                        command: 'update',
                        argument: 'assignLocomotiveToBlock',
                        argumentValue: {
                            objectId: row.oid,
                            driverName: row.driverName,
                            blockName: jqueryEl.attr('id')
                        }
                    });
                }

                srcGrid.selectNone();
                srcGrid.unselect(jsonData.recid);
            }
            catch (ev) {
                // ignore
            }
        };
    }

    __getBlockCtrlOf(fbData, blockCtrls) {
        const blockId = fbData.BlockId;
        for (let i = 0; i < blockCtrls.length; ++i) {
            const block = $(blockCtrls[i]);
            const data = block.data(constDataThemeItemObject);
            const blockId = data.identifier;
            if (fbData.BlockId.startsWith(blockId))
                return block;
        }
        return null;
    }

    /**
     * Updates sensor visualizations based on the provided sensor data.
     *
     * This method processes binary sensor states and updates the corresponding
     * UI elements to reflect the current state of each sensor.
     *
     * @param {Object} sensorData - The sensor data object containing `port` and `binaryState`.
     */
    updateSensors(sensorData) {
        if (sensorData) this.__recentSensorsData = sensorData;
        const recentSensor = this.__recentSensorsData;
        if (!recentSensor) {
            // TODO: Reset all visuals
            return;
        }

        const isModulePinAddress = recentSensor.driverName === "z21";

        const changes = [];
        let sensorBaseAddress = 0;
        if (isModulePinAddress === true) {

        } else {
            sensorBaseAddress = (recentSensor.port - 1) * 16;
        }
        
        const binaryStateArray = recentSensor.binaryState.split("").map(Number); // Assumes binaryState is a string
        const controlItems = $('div.ctrlItemSensors');

        // Iterate over sensor bits
        binaryStateArray.reverse().forEach((binState, jj) => {
            let addr = 0;
            if (isModulePinAddress === true) {
                addr = `${recentSensor.port}:${jj + 1}`;
            } else {
                addr = sensorBaseAddress + jj + 1;
            }

            const isSensorOn = binState === 1;

            // Process matching control items
            controlItems.each((_, element) => {
                const ctrl = $(element);
                let sensorAddress = 0;
                if (isModulePinAddress === true) {
                    sensorAddress = ctrl.data(constDataSensorAddress);
                } else {
                    sensorAddress = parseInt(ctrl.data(constDataSensorAddress), 10);
                }

                if (sensorAddress === addr) {
                    const themeItemObject = ctrl.data(constDataThemeItemObject); // Fixed key
                    const themeData = this.__getThemeInformationById(themeItemObject.editor.themeId);

                    this.__processSensorImage(ctrl, themeData, isSensorOn, changes);
                }
            });
        });

        // Apply all changes in bulk
        changes.forEach(change => {
            const currentSrc = change.ctrl[0].getAttribute("src");
            if (currentSrc !== change.src) {
                change.ctrl[0].setAttribute("src", change.src);
            }
        });
    }

    /**
     * Processes and queues updates for a sensor image.
     *
     * @param {Object} ctrl - The jQuery-wrapped control element.
     * @param {Object} themeData - Theme data for the control.
     * @param {boolean} isSensorOn - Whether the sensor is in the "on" state.
     * @param {Array} changes - Array to store pending changes.
     */
    __processSensorImage(ctrl, themeData, isSensorOn, changes) {
        const img = ctrl.find("img");
        if (!img.length) return;

        const imgSrc = img.attr("src");
        const dirPath = getDirpathOf(imgSrc);
        const baseName = themeData.basename.replace(/-on|-off/g, "");
        const newImgEnding = isSensorOn ? "-on" : "-off";
        const newImgSrc = `${dirPath}${baseName}${newImgEnding}.png`;

        if (imgSrc !== newImgSrc) {
            changes.push({
                ctrl: img,
                src: `${newImgSrc}`
            });
        }
    }

    updateSettings(settings) {
        if (!settings) return;
        this.__settingsInfo = settings;

        //
        // update data for blocks/stages
        //
        settings.stagings.forEach(stage => {
            if (stage) {
                const stageId = stage.identifier;
                const $ctrl = $('#' + stageId);
                if ($ctrl) {
                    $ctrl.data(constDataStateEnabled, stage.isEnabled);
                    $ctrl.data(constDataStateLock, stage.isLocked);
                }
            }
        });
        settings.blockSensors.forEach(it => {
            const id = it.identifier.replace(/\[\+\]$|\[\-\]$/g, '');
            if (it) {
                const $ctrl = $('#' + id);
                if ($ctrl) {
                    $ctrl.data(constDataStateEnabled, it.isEnabled);
                    $ctrl.data(constDataStateLock, it.isLocked);

                    if (it.identifier.endsWith("[+]"))
                        $ctrl.data(constDataStateCommuterPlus, it.isCommuterAllowedPlus);
                    if (it.identifier.endsWith("[-]"))
                        $ctrl.data(constDataStateCommuterMinus, it.isCommuterAllowedMinus);
                }
            }
        });

        //
        // update sensors with addresses
        //
        settings.sensors.forEach(sensor => {
            if (sensor) {
                const name = sensor.name;
                const provider = sensor.provider;
                const address = sensor.address;
                const fbCtrl = $('div.ctrlItemSensors[id="' + name + '"]');
                if (fbCtrl) {
                    fbCtrl.data(constDataSensorProvider, provider);
                    fbCtrl.data(constDataSensorAddress, address);
                }
            }
        });

        //
        // update accessories
        //
        settings.accessories.forEach(acc => {
            if (acc) {
                const ctrlId = acc.planfieldControlIdentifier;
                if (!ctrlId || ctrlId.length <= 0) return;
                const $ctrl = $('#' + ctrlId);
                if ($ctrl) {
                    $ctrl.data(constDataStateIsMaintenance, acc.isMaintenaceEnabled);

                    //
                    // Wir prüfen hier ob das Accessory einen Treiber zugewiesen hat,
                    // welches aktuell nicht verfügbar ist.
                    //
                    const accRecord = window.accessoriesDlg?.getAccessoryRecordByIdentifier(acc.accessoryDriver, acc.accessoryIdentifier);
                    if (!accRecord &&
                        (acc?.planfieldControlIdentifier && acc?.planfieldControlIdentifier.length > 0)) {

                        const driverName = acc.accessoryDriver;
                        const accId = acc.accessoryIdentifier;

                        $ctrl.find(".entity-warning").remove();

                        //
                        // Schau genauer um die passende Fehlermeldung anzuzeigen.
                        //
                        let warningIcon = null;
                        if (window.accessoriesDlg.isDriverAvailable(acc.accessoryDriver) === false) {

                            warningIcon =
                                $(
                                    '<i class="fa fa-exclamation-triangle" style="color: red; display: block; width: 18px; height: 16px; position: relative; top: -5px; left: 0px;"></i>')
                                    .attr('title', `Schaltartikel '${driverName}::${accId}' zugewiesen, aber ControlStation '${driverName}' nicht verfügbar.`)
                                    .data('errors', [
                                        `ControlStation ${driverName} nicht verfügbar`
                                    ])
                                    .appendTo($ctrl);

                        } else {

                            warningIcon =
                                $(
                                    '<i class="fa fa-exclamation-triangle" style="color: red; display: block; width: 18px; height: 16px; position: relative; top: -5px; left: 0px;"></i>')
                                    .attr('title', `Schaltartikel '${driverName}::${accId}' zugewiesen. Schaltartikel '${accId}' nicht verfügbar`)
                                    .data('errors', [
                                        `ControlStation '${driverName}' verfügbar`,
                                        `Schaltartikel '${accId}' nicht verfügbar`
                                    ])
                                    .appendTo($ctrl);

                        }

                        warningIcon.addClass("entity-warning");

                        warningIcon.off('click');
                        warningIcon.on('click', () => {
                            showAccessoryRepairPrompt(ctrlId);
                        });
                    } else {
                        // TODO Was ist wenn es das Assignment gibt, aber das Accessory nicht mehr?
                    }
                }
            }
        });

        this.validateSensors();
        this.validateAccessories();
        this.validateBlocks();

        this.updateAccessories(this.__recentAccessories);
    }

    //
    // validate all accessories
    //
    validateAccessories() {
        if (this.isEditMode === true) return;
        const controlItems = $('div.ctrlItemAccessory');
        controlItems.removeClass('maintenance-indicator');
        const invalidElements = controlItems
            .filter((_, el) => {
                const value = $(el).data(constDataStateIsMaintenance) ?? false;
                return value;
            });
        invalidElements.addClass('maintenance-indicator');
    }

    __removeAccessoriesIndicators() {
        const controlItems = $('div.ctrlItemAccessory');
        controlItems.removeClass('maintenance-indicator');
    }

    //
    // validate all sensors
    //
    validateSensors() {
        if (this.isEditMode === true) return;
        const controlItems = $('div.ctrlItemSensors');
        controlItems.removeClass('invalid-indicator');
        const invalidElements = controlItems
            .filter((_, el) => {
                const value = parseInt($(el).data(constDataSensorAddress));
                return isNaN(value) || value === undefined || value < 1 || value > 992;
            });
        invalidElements.addClass('invalid-indicator');
    }

    __removeValidateIndicators() {
        const controlItems = $('div.ctrlItemSensors');
        controlItems.removeClass('invalid-indicator');
    }

    //
    // validate all blocks
    //
    validateBlocks() {
        if (this.isEditMode === true) return;
        const controlItems = $('div.ctrlItemBlock');

        controlItems.removeClass('disabled-indicator');
        controlItems.removeClass('locked-indicator');

        const disabledElements = controlItems
            .filter((_, el) => {
                const isEnabled = $(el).data(constDataStateEnabled) ?? true;
                return !isEnabled;
            });
        disabledElements.addClass('disabled-indicator');

        const lockedElements = controlItems
            .filter((_, el) => {
                const isLocked = $(el).data(constDataStateLock) ?? false;
                return isLocked;
            });
        lockedElements.addClass('locked-indicator');
    }

    __removeBlockIndicators() {
        const controlItems = $('div.ctrlItemBlock');
        controlItems.removeClass('disabled-indicator');
        controlItems.removeClass('locked-indicator');
    }

    updateAccessories(accessories) {
        this.__recentAccessories = accessories;
        if (!accessories) return;
        const self = this;
        const noOfAccessories = accessories.length;

        for (var i = 0; i < noOfAccessories; ++i) {
            const acc = accessories[i];
            if (!acc) continue;

            const accSettings = this.__settingsInfo?.accessories?.find(it => {
                return it.accessoryIdentifier === acc.name1 && it.accessoryDriver === acc.driverName
            })
            if (!accSettings) continue;

            const accVis = $('div.ctrlItemAccessory[id="' + accSettings.planfieldControlIdentifier + '"]')
            if (!accVis) continue;

            /*
             * acc := the data from server about the accessory
             * Sample data: {"name1":"sw77","name2":"Artikel","name3":">0077<","objectId":20044,"addrext":["77g","77r"],"addr":"77","protocol":"DCC","type":"Accessory","mode":"SWITCH","state":"1","switching":"1"}
             */

            /*
             * accSettings := the mapping of the hardware to the visual element in the planfield
             * Sample data: { "accessoryIdentifier": "sw77", "planfieldControlIdentifier": "Switch_1" }
             */

            /*
             * accVis := the visual in the planfield
             * Sample data:
             *    accVis.attr("id") := the `planfieldControlIdentifier` e.g. "Switch_1"
             */

            //console.log(`${accVis.attr("id")} is switching: ${acc.switching}`);

            if (typeof acc.switching === "string" && acc.switching.length > 0 && !self.isEditMode) {
                const spinnerCloneId = `spinner_${accSettings.planfieldControlIdentifier}`;
                if (parseInt(acc.switching) === 1) {
                    const spinner = $('div.lds-facebookOriginal');
                    const spinnerClone = spinner.clone();
                    spinnerClone.removeClass("lds-facebookOriginal");
                    spinnerClone.addClass("lds-facebook");
                    spinnerClone.attr('id', spinnerCloneId)
                    const position = accVis.offset();
                    const elementWidth = spinnerClone.outerWidth();
                    const elementHeight = spinnerClone.outerHeight();
                    spinnerClone.css({
                        top: position.top - (elementHeight / 2),
                        left: position.left - (elementWidth / 2)
                    });
                    spinnerClone.insertAfter(spinner);
                    if (spinnerClone.is(":visible")) {
                        // ignore
                    } else {
                        spinnerClone.show();
                    }
                } else {
                    const c = $('div#' + spinnerCloneId);
                    if (c) {
                        c.hide();
                        c.remove();
                    }
                }
            }

            // general stuff
            const img = accVis.find("img");
            if (!img || img.length <= 0) continue;
            const imgsrc = img.attr("src");
            const dirpath = getDirpathOf(imgsrc);

            // prüfe vor der Ersetzung vom Image
            // ob das aktuelle Image
            // eine Route
            // oder Occ
            // war
            const isRoute = imgsrc.includes("-route");
            const isOcc = imgsrc.includes("-occ");

            const themeId = accVis.data(constDataThemeItemObject)?.editor?.themeId;
            const themeInfo = window.themeData.flatMap(group => group.objects).find(item => item.id === themeId);
            const themeBasename = themeInfo?.basename;

            if (!themeBasename)
                continue;

            let newimgfname = themeBasename;

            const addrIdx = parseInt(acc.state);
            const addrTxt = acc.addrext[addrIdx];
            const addr = acc.addr;

            // helper functions
            function __processDecoupler(addrIdx, baseName) {
                return addrIdx === 0 ? `${baseName}-on` : baseName;
            }

            function __processSignal(addrIdx, baseName) {
                const cleanBaseName = baseName.replace("-r", "").replace("-g", "");
                return addrIdx === 0 ? `${cleanBaseName}-r` : `${cleanBaseName}-g`;
            }

            function __processAccessory(addrIdx, baseName) {
                return addrIdx === 0 ? `${baseName}-on` : `${baseName}-off`;
            }

            function __processButton(addrIdx, baseName) {
                const cleanBaseName = baseName.replace("-on", "").replace("-off", "");
                return addrIdx === 0 ? `${cleanBaseName}-on` : `${cleanBaseName}-off`;
            }

            function __processSwitchOrAccessory(addrIdx, baseName) {
                if (baseName === "threeway") {
                    if (addrIdx === 1) return `${baseName}-tr`
                    if (addrIdx === 2) return `${baseName}-tl`
                    return baseName;
                }

                if (baseName.startsWith("dcrossing")) {
                    if (addrIdx === 0) return `${baseName}`;
                    if (addrIdx === 1) return `${baseName}-t`;
                    if (addrIdx === 2) return `${baseName}-tl`;
                    if (addrIdx === 3) return `${baseName}-tr`;
                }

                return addrIdx === 0 ? baseName : `${baseName}-t`;
            }

            // main part to distiguish visualizations
            if (isDecoupler(themeId)) {
                newimgfname = __processDecoupler(addrIdx, newimgfname);
            } else if (isAccessory(themeId)) {
                newimgfname = __processAccessory(addrIdx, newimgfname);
            } else if (isButton(themeId)) {
                newimgfname = __processButton(addrIdx, newimgfname);
            } else if (isSwitchOrAccessory(themeId) || isSignal(themeId)
            ) {
                let fnc = null;
                if (isSignal(themeId)) fnc = __processSignal;
                else if (isSwitchOrAccessory(themeId)) fnc = __processSwitchOrAccessory;

                // der Fahrtweg kann invertiert sein
                // nur fuer 2-fach Weichen getestet
                // TODO Konzept fuer z.B. Kreuzungen oder Dreiwegweichen ist nicht klar
                if (accSettings && accSettings.invertUi === true) {
                    if (addrIdx === 0) {
                        newimgfname = fnc(1, newimgfname);
                    } else if (addrIdx === 1) {
                        newimgfname = fnc(0, newimgfname);
                    }
                } else {
                    newimgfname = fnc(addrIdx, newimgfname);
                }
            }

            newimgfname += ".png";
            let imgName = dirpath + newimgfname;
            if (img.get(0)) {

                const alreadyModified = imgName.includes("-route") || imgName.includes("-occ");

                if ((isRoute || isOcc) && !alreadyModified) {
                    if (isRoute) {
                        imgName = imgName.replace(".png", "-route.png");
                    } else if (isOcc) {
                        imgName = imgName.replace(".png", "-occ.png");
                    }
                }

                img.get(0).src = imgName;
            }
        }
    }

    __createControlOnDrop(ev, options = {}) {
        ev.preventDefault();
        if (ev.dataTransfer) {
            try {
                const data = ev.dataTransfer.getData("text/plain")?.trim();
                if (data.length <= 0) return;
                const jsonData = JSON.parse(data);
                this.__createControlWithEv(jsonData, ev, options);
            } catch (err) {
                console.log(err);
            }
        }
    }

    __createControlWithEv(jsonData, ev, options = {}) {
        const coord = getElementCoord(this.planfieldElement, ev);

        let coordOffsetX = 0;
        let coordOffsetY = 0;

        if (jsonData?.editor) {

            if (options?.offset) {
                coordOffsetX = parseInt(options.offset.x / constItemWidth);
                coordOffsetY = parseInt(options.offset.y / constItemHeight);
            } else {
                coordOffsetX = parseInt(ev.offsetX / constItemWidth);
                coordOffsetY = parseInt(ev.offsetY / constItemHeight);

                if (options) {
                    options.offset = { x: ev.offsetX, y: ev.offsetY };
                }
            }

            coord.x = coordOffsetX;
            coord.y = coordOffsetY;
        }

        this.__createControl(jsonData, coord, options);
    }

    __getThemeInformationById(themeId) {
        for (let i = 0; i < this.themeData.length; ++i) {
            const themeData = this.themeData[i];
            if (!themeData.objects) continue;
            for (let j = 0; j < themeData.objects.length; ++j) {
                const themeObject = themeData.objects[j];
                if (themeObject.id === themeId)
                    return themeObject;
            }
        }
        return null;
    }

    __isConnectorControl(themeId) {
        return themeId === 17 || themeId === 18 || themeId === 19;
    }

    __updateConnectorVisualization(ctrl, connectorId) {
        if (!ctrl) return;

        const themeId = ctrl.data(constDataThemeId);
        var dimensionIndex = ctrl.data(constDataThemeDimensionIndex);
        if (!dimensionIndex) dimensionIndex = 0;

        let top = '50%';
        let left = '50%';
        let transform = 'translate(-50%, -50%)';

        if (themeId === 17) {
            if (dimensionIndex === 0) {
                top = '45%'; left = '70%';
            } else if (dimensionIndex === 1) {
                top = '45%'; left = '70%';
                transform = "translate(-50%, -40%) rotate(270deg)";
            } else if (dimensionIndex === 2) {
                top = '51%'; left = '72%';
                transform = "translate(-50%, -50%) scale(-1)";
            } else if (dimensionIndex === 3) {
                top = '45%'; left = '70%';
                transform = "translate(-40%, -45%) rotate(90deg)";
            }
        } else if (themeId === 18) {

            if (dimensionIndex === 0) {
                top = '67%'; left = '69%';;
            } else if (dimensionIndex === 1) {
                top = '67%'; left = '69%';
            } else if (dimensionIndex === 2) {
                top = '67%'; left = '69%';
            } else if (dimensionIndex === 3) {
                top = '67%'; left = '69%';
            }

        } else if (themeId === 19) {

            if (dimensionIndex === 0) {
                top = '27%'; left = '69%';
            } else if (dimensionIndex === 1) {
                top = '27%'; left = '69%';
            } else if (dimensionIndex === 2) {
                top = '27%'; left = '69%';
            } else if (dimensionIndex === 3) {
                top = '27%'; left = '69%';
            }
        }

        let connectorDiv = ctrl[0].querySelector('.connector-visualization');

        if (!connectorDiv) {
            connectorDiv = document.createElement('div');
            connectorDiv.classList.add('connector-visualization');
            Object.assign(connectorDiv.style, {
                position: 'absolute',
                top: top,
                left: left,
                transform: transform,
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                width: '20px',
                height: '20px',
                borderRadius: '50%',
                fontSize: '12px',
                fontWeight: 'bold',
                color: 'yellow',
                'z-index': 10,
                'pointer-events': 'none'
            });

            ctrl[0].appendChild(connectorDiv);
        } else {
            Object.assign(connectorDiv.style, {
                top: top,
                left: left,
                transform: transform
            });
        }

        connectorDiv.textContent = connectorId;

        // Falls bereits ein Punkt existiert, entfernen wir ihn zuerst
        let dot = connectorDiv.querySelector('.dot-indicator');
        if (dot) {
            dot.remove();
        }

        // Falls connectorId eine 6 oder 9 ist, füge einen Punkt hinzu
        if (connectorId === 6 || connectorId === 9) {
            dot = document.createElement('div');
            dot.classList.add('dot-indicator');
            Object.assign(dot.style, {
                position: 'absolute',
                bottom: '3px',
                left: '80%',
                transform: 'translateX(-50%)',
                width: '2px',
                height: '2px',
                backgroundColor: 'yellow',
                borderRadius: '50%'
            });

            connectorDiv.appendChild(dot);
        }
    }

    async __createControl(jsonData, coord, options = {}) {
        const self = this;

        let knownControls = $('div.ctrlItem[id]');
        let knownCtrl = getCtrlOfCoord(coord.x, coord.y, knownControls);
        if (typeof knownCtrl !== "undefined" && knownCtrl != null) {
            return;
        }

        if (jsonData?.editor == null) return;

        const pixelCoord = coord2pixel(coord);

        if (typeof options.editMode === "undefined")
            options.editMode = false;

        let data = JSON.stringify(jsonData);
        data = window.btoa(data);

        const themeData = this.__getThemeInformationById(jsonData.editor.themeId);

        let dimensions = themeData.dimensions;
        if (!dimensions || dimensions.length <= 0)
            dimensions = [{ w: 1, h: 1 }];

        const w = dimensions[0].w * constItemWidth;
        const h = dimensions[0].h * constItemHeight;

        if (typeof jsonData.editor === "undefined")
            jsonData.editor = {};
        if (typeof jsonData.editor.themeDimIdx === "undefined")
            jsonData.editor.themeDimIdx = 0;

        let newId = "";
        if (jsonData.identifier && this.__isInitialization) {
            newId = jsonData.identifier;
        }

        //
        // query unique identifier from server
        //
        if (newId.length === 0) {
            sendTrackplanCommand({
                command: 'update',
                argument: 'request',
                argumentValue: {
                    methodName: 'uniqueIdentifier',
                    methodParameter: jsonData.editor.themeId
                }
            });

            const response = await window.serverHandling.wsReplies.readMessage();
            const jsonResponse = JSON.parse(response);
            if (jsonResponse.uniqueIdentifier) {
                newId = jsonResponse.uniqueIdentifier;
            }
        }

        let newCtrl = null;

        if (jsonData.editor.themeId === 1010) {
            // textfield element

            let innerHtml = null;
            let fontSizeValue = null;
            if (jsonData.editor.innerHtml) innerHtml = jsonData.editor.innerHtml;
            if (jsonData.editor.fontSize) fontSizeValue = jsonData.editor.fontSize;

            const textfieldElement = new TextfieldElement();
            textfieldElement.install({
                fontSize: fontSizeValue,
                uniqueId: jsonData.identifier
            });

            window.textfieldElementInstances.push(textfieldElement);

            newCtrl = textfieldElement.__element;
            newCtrl.addClass("ctrlItem");
            newCtrl.attr("draggable", true);
            newCtrl.css({
                left: pixelCoord.x + "px",
                top: pixelCoord.y + "px",
                width: w + "px",
                height: h + "px",
                "z-index": 0
            });
            newCtrl.data(constDataThemeItemObject, jsonData);
            newCtrl.data(constDataThemeDimensionIndex, jsonData.editor.themeDimIdx);

            const targetEl = newCtrl.find('.elEditor');
            if (jsonData.editor.innerHtml && innerHtml !== null) {
                targetEl.html(innerHtml);
            }
            if (jsonData.editor.outerHtml && fontSizeValue !== null) {
                targetEl.css("font-size", fontSizeValue);
            }
            if (jsonData.editor.size && sizeInit !== null) {
                targetEl.css({
                    width: sizeInit.width,
                    height: sizeInit.height
                });
                targetEl.parent().css({
                    width: sizeInit.width,
                    height: sizeInit.height
                });
            }

            if (!this.__isInitialization)
                textfieldElement.setEditMode(true);
        }
        else {
            //
            // general approach to create track element
            //

            newCtrl = $('<div>')
                .addClass("ctrlItem")
                .attr("id", newId)
                .css({
                    position: 'absolute',
                    left: pixelCoord.x + "px",
                    top: pixelCoord.y + "px",
                    width: w + "px",
                    height: h + "px"
                });

            newCtrl.data(constDataThemeId, jsonData.editor.themeId);
            newCtrl.data(constDataThemeItemObject, jsonData);
            newCtrl.data(constDataThemeDimensionIndex, jsonData.editor.themeDimIdx);

            const themeId = jsonData.editor.themeId;
            const themeData = this.__getThemeInformationById(themeId);
            if (isBlock(themeId)) {
                newCtrl.addClass("ctrlItemBlock");
            } else if (isSwitchOrAccessory(themeId)
                || isAccessory(themeId)
                || isSignal(themeId)
                || isDecoupler(themeId)) {
                newCtrl.addClass("ctrlItemAccessory");
            }
            else if (isFeedback(themeId)) {
                newCtrl.addClass("ctrlItemSensors");
            }

            if (window.planfield.__isInitialization)
                addImage(themeData, newCtrl);
            else
                addImageDirect(themeData, newCtrl);

            //
            // Label for the Element
            //
            const labelOffset = getLabelOffsetBy(jsonData);
            if (labelOffset) {
                const isVisible = window.labelShown === true;
                labelOffset.display = isVisible ? "visible" : "none";
                const lblBg = document.createElement("div");
                Object.assign(lblBg.style, labelOffset);
                lblBg.classList.add("elementLabel");
                const newLbl = document.createElement("div");
                newLbl.textContent = newId;
                newLbl.classList.add("elementLabelTxt");
                lblBg.appendChild(newLbl);
                newCtrl[0].appendChild(lblBg);
            }

            //
            // specific handling for connector
            //
            if (this.__isConnectorControl(themeId)) {
                newCtrl.data(constDataConnectorId, jsonData.editor.connectorId);
                newCtrl.addClass("connector");

                const $selfCtx = this;
                $selfCtx.__updateConnectorVisualization(newCtrl, jsonData.editor.connectorId);

                newCtrl.off("contextmenu");
                newCtrl.on("contextmenu",
                    function (ev) {
                        ev.preventDefault();

                        const connectorCtrls = $('div.ctrlItem').filter(function () {
                            const themeId = $(this).data(constDataThemeId);
                            if ($selfCtx.__isConnectorControl(themeId))
                                return true;
                            return false;
                        });

                        //
                        // the idea, connectors need two partner
                        // wenn wir eine gerade Anzahl an Connectors haben,
                        // dann brauchen wir auch nur die Hälfte an IDs
                        // Wahlfreiheit für irgendwelchen IDs ist gar nicht notwenig
                        // 2 Connectors => id 1
                        // 10 Connectors => ids 1, 2, 3, 4, 5
                        //

                        const cidOfCtrl = newCtrl.data(constDataConnectorId) ?? 0;

                        const maxIds = parseInt(connectorCtrls.length / 2) + 1;
                        const entries = [];
                        for (let connectorId = 1; connectorId <= maxIds; ++connectorId) {

                            let label = '';
                            let cssIcon = '';
                            if (connectorId === cidOfCtrl) {
                                cssIcon += "fas fa-link";
                            }
                            if (connectorId === 1)
                                label += `Default(${connectorId})`
                            else
                                label += `Connector(${connectorId})`;

                            entries.push(
                                {
                                    cssIcon: cssIcon,
                                    enabled: true,
                                    label: label,
                                    onClick: () => {
                                        newCtrl.data(constDataConnectorId, connectorId)

                                        $selfCtx.__updateConnectorVisualization(newCtrl, connectorId);

                                        self.__trigger("connectorChanged",
                                            {
                                                command: 'update',
                                                argument: 'assigment',
                                                argumentValue: {
                                                    controlId: newCtrl.attr('id'),
                                                    connectorId: connectorId
                                                }
                                            });
                                    }
                                });
                        }

                        new Contextual({
                            isSticky: false,
                            items: entries
                        });
                    });
            }

            //
            // specific handling for block controls
            //
            if (isBlock(themeId)) {
                const $self = this;
                newCtrl.off("contextmenu");
                newCtrl.on("contextmenu",
                    function (ev) {
                        ev.preventDefault();
                        const $selfEv = ev;
                        const $selfCtrl = newCtrl;
                        const ctrlId = $selfCtrl.attr("id");
                        const entries = [];

                        //
                        // Enable/Disable
                        //
                        const isEnabled = newCtrl.data(constDataStateEnabled) ?? true;
                        const cssEnableLabel = isEnabled === true ? 'Deaktivieren ' + ctrlId : 'Aktivieren ' + ctrlId;
                        const cssEnableIcon = isEnabled === true ? 'fas fa-toggle-on' : 'fas fa-toggle-off';
                        entries.push({
                            cssIcon: cssEnableIcon,
                            label: cssEnableLabel,
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isBlockEnabled',
                                        argumentValue: {
                                            blockIdentifier: ctrlId + "[+]",
                                            state: !isEnabled
                                        }
                                    });
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isBlockEnabled',
                                        argumentValue: {
                                            blockIdentifier: ctrlId + "[-]",
                                            state: !isEnabled
                                        }
                                    });
                            }
                        });

                        //
                        // Lock/Unlock
                        //
                        const isLocked = newCtrl.data(constDataStateLock) ?? false;
                        const cssLockLabel = isLocked === true ? 'Entsperren ' + ctrlId : 'Sperren ' + ctrlId;
                        const cssLockIcon = isLocked === true ? 'fas fa-lock' : 'fas fa-lock-open';
                        entries.push({
                            cssIcon: cssLockIcon,
                            label: cssLockLabel,
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isBlockLocked',
                                        argumentValue: {
                                            blockIdentifier: ctrlId + "[+]",
                                            state: !isLocked
                                        }
                                    });
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isBlockLocked',
                                        argumentValue: {
                                            blockIdentifier: ctrlId + "[-]",
                                            state: !isLocked
                                        }
                                    });
                            }
                        });

                        entries.push({ type: 'seperator' });

                        //
                        // Commuter Traffic
                        //
                        const isCommuterAllowed0 = newCtrl.data(constDataStateCommuterPlus) ?? false;
                        const cssCommuterLabel0 = "Pendelverkehr [+]";
                        const cssCommuterIcon0 = isCommuterAllowed0 === true ? 'fas fa-toggle-on' : 'fas fa-toggle-off';
                        entries.push({
                            // Commuter traffic
                            cssIcon: cssCommuterIcon0,
                            label: cssCommuterLabel0,
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isCommuterAllowedPlus',
                                        argumentValue: {
                                            blockIdentifier: ctrlId,
                                            state: !isCommuterAllowed0
                                        }
                                    });
                            }
                        });
                        const isCommuterAllowed1 = newCtrl.data(constDataStateCommuterMinus) ?? false;
                        const cssCommuterLabel1 = "Pendelverkehr [-]";
                        const cssCommuterIcon1 = isCommuterAllowed1 === true ? 'fas fa-toggle-on' : 'fas fa-toggle-off';
                        entries.push({
                            // Commuter traffic
                            cssIcon: cssCommuterIcon1,
                            label: cssCommuterLabel1,
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isCommuterAllowedMinus',
                                        argumentValue: {
                                            blockIdentifier: ctrlId,
                                            state: !isCommuterAllowed1
                                        }
                                    });
                            }
                        });

                        entries.push({ type: 'seperator' });

                        //
                        // automatische Reparatur
                        //
                        entries.push({
                            cssIcon: 'fa fa-wrench',
                            enabled: true,
                            label: 'Reparatur',
                            onClick: () => {
                                showBlockRepairPrompt(ctrlId);
                            }
                        });

                        entries.push({ type: 'seperator' });

                        //
                        // Zielrückmelder anfahren
                        //
                        entries.push({
                            cssIcon: 'fas fa-bullseye',
                            enabled: true,
                            label: 'Zielrückmelder anfahren',
                            onClick: () => {

                                // Suche alle Sensoren die einem Block zugeordnet sind.
                                let assignedSensors = [];
                                if (window.settingsInfo?.blockSensors) {
                                    for (let ii = 0; ii < window.settingsInfo.blockSensors.length; ++ii) {
                                        const b = window.settingsInfo.blockSensors[ii];
                                        if (b && b.identifier.startsWith(ctrlId)) {
                                            assignedSensors.push(b);
                                        }
                                    }
                                }

                                // Suche die Lokomotive die dem Block zugeordnet ist.
                                let assignedLocomotive = "";
                                if (window.settingsInfo?.locomotives) {
                                    for (let ii = 0; ii < window.settingsInfo.locomotives.length; ++ii) {
                                        const itLoc = window.settingsInfo.locomotives[ii];
                                        if (itLoc && itLoc.assignedToBlock === ctrlId) {
                                            assignedLocomotive = itLoc;
                                        }
                                    }
                                }

                                if(typeof(assignedLocomotive) === "string"
                                    && (assignedLocomotive.length <= 0 || assignedLocomotive === ""
                                    )){
                                        w2alert("Der Block besitzt keine Lokzuweisung.");
                                        return;
                                    }

                                //
                                // lokale Funktion zum Befüllen der Sensor-Select-Auswahl
                                //
                                function populateFeedbackTargets(sensorBlocks) {
                                    const feedbackSelect = document.getElementById('feedback-target');
                                    feedbackSelect.innerHTML = ''; // Clear existing options

                                    const uniqueSensors = new Set();

                                    sensorBlocks.forEach(block => {
                                        ['sensorEnter', 'sensorIn', 'sensorOcc'].forEach(key => {
                                            const sensor = block[key];
                                            if (sensor && sensor.trim() !== '') {
                                                uniqueSensors.add(sensor);
                                            }
                                        });
                                    });

                                    Array.from(uniqueSensors).sort().forEach(sensor => {
                                        // setze Default Sensor wenn noch keiner gesetzt wurde
                                        const currentId = window.__positioningAbortController.sensorId;
                                        if (currentId.length <= 0) {
                                            window.__positioningAbortController.sensorId = sensor;
                                        }

                                        const opt = document.createElement('option');
                                        opt.value = sensor;
                                        opt.textContent = sensor;
                                        feedbackSelect.appendChild(opt);
                                    });

                                    // registriere Event für Wechsel
                                    feedbackSelect.addEventListener('change', function () {
                                        window.__positioningAbortController.sensorId = this.value;
                                    });
                                }

                                function startPositioning() {
                                    const {
                                        driverName,
                                        objectId,
                                        speedValue,
                                        sensorId
                                    } = window.__positioningAbortController;

                                    const sensorImg = document.querySelector(`#${sensorId} img`);

                                    if (sensorImg && sensorImg.src.includes('sensor-on.png')) {
                                        document.getElementById('status-message').textContent = 'Sensor bereits aktiv. Lok wird nicht gestartet.';
                                        setSpeed(driverName, objectId, 0); // Zur Sicherheit stoppen
                                        return;
                                    }

                                    // UI-Status zurücksetzen
                                    document.getElementById('status-message').textContent = 'Fahrt gestartet...';

                                    awaitSensorAndStop(driverName, objectId, speedValue, sensorId).then((result) => {
                                        if (result === 'reached') {
                                            document.getElementById('status-message').textContent = 'Ziel erreicht.';
                                        } else if (result === 'aborted') {
                                            document.getElementById('status-message').textContent = 'Vorgang abgebrochen.';
                                        }
                                    });
                                }

                                async function awaitSensorAndStop(driverName, objectId, speedValue, sensorId) {
                                    window.__positioningAbortController.aborted = false;
                                    window.locomotivePosition_setSpeed(driverName, objectId, speedValue);

                                    const sensorSelector = `#${sensorId}`;
                                    const checkInterval = 300;

                                    return new Promise((resolve) => {
                                        const interval = setInterval(() => {
                                            if (window.__positioningAbortController.aborted) {
                                                clearInterval(interval);
                                                window.locomotivePosition_setSpeed(driverName, objectId, 0);
                                                resolve("aborted");
                                                return;
                                            }

                                            const sensorImg = document.querySelector(`${sensorSelector} img`);
                                            if (sensorImg && sensorImg.src.includes("sensor-on.png")) {
                                                clearInterval(interval);
                                                window.locomotivePosition_setSpeed(driverName, objectId, 0);
                                                resolve("reached");
                                            }
                                        }, checkInterval);
                                    });
                                }

                                function abortPositioning() {
                                    window.__positioningAbortController.aborted = true;
                                }

                                window.locomotivePosition_startPositioning = null;
                                window.locomotivePosition_startPositioning = startPositioning;

                                window.locomotivePosition_abortPositioning = null;
                                window.locomotivePosition_abortPositioning = abortPositioning;

                                window.locomotivePosition_toggleDirection = null;
                                window.locomotivePosition_toggleDirection = function () {
                                    const driverName = window.__positioningAbortController.driverName;
                                    const objectId = window.__positioningAbortController.objectId;
                                    const speedValue = window.__positioningAbortController.speedValue;
                                    const recLoc = window.locomotivesDlg.getLocomotiveRecord(driverName, objectId);
                                    if (recLoc) {
                                        let targetDirection = 0;
                                        if (recLoc.direction === 1) {
                                            targetDirection = 0;
                                        } else {
                                            targetDirection = 1;
                                        }

                                        // prüfe aktuell Geschwindigkeit
                                        // setze nur wenn vorher eine Geschwindgkeit vorhanden war
                                        let recentSpeed = recLoc.speedstep;

                                        sendLocomotiveCommand({
                                            objectId: objectId,
                                            command: 'update',
                                            argument: 'direction',
                                            argumentValue: {
                                                driverName: driverName,
                                                direction: targetDirection
                                            }
                                        });

                                        if (recentSpeed > 0) {
                                            sendLocomotiveCommand({
                                                objectId: objectId,
                                                command: 'update',
                                                argument: 'speedstep',
                                                argumentValue: {
                                                    driverName: driverName,
                                                    value: speedValue
                                                }
                                            });
                                        }
                                    }
                                }

                                window.locomotivePosition_setSpeed = null;
                                window.locomotivePosition_setSpeed = function (driverName, objectId, speedValue) {
                                    sendLocomotiveCommand({
                                        objectId: objectId,
                                        command: 'update',
                                        argument: 'speedstep',
                                        argumentValue: {
                                            driverName: driverName,
                                            value: speedValue
                                        }
                                    });
                                }

                                window.__positioningAbortController = {
                                    aborted: false,
                                    driverName: assignedLocomotive.driverName,
                                    objectId: assignedLocomotive.objectId,
                                    speedValue: assignedLocomotive.speed.minimum,
                                    sensorId: ''
                                };

                                w2popup.open({
                                    title: `Zielrückmelder anfahren in ${ctrlId}`,
                                    modal: true,
                                    body: `
    <div style="display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center;">
        <div style="display: flex; flex-direction: column; align-items: stretch; gap: 15px; width: 100%; max-width: 400px;">
            <div style="display: flex;">
                <strong style="min-width: 150px;">Lok:</strong>
                <span id="dialog-lok-id"></span>
            </div>
            <div style="display: flex;">
                <strong style="min-width: 150px;">Block:</strong>
                <span id="dialog-block-id"></span>
            </div>
            <div style="display: flex; align-items: center; width: 100%;">
                <label for="feedback-target" style="white-space: nowrap; min-width: 150px;">Ziel-Rückmelder:</label>
                <select id="feedback-target" style="flex: 1; padding: 4px;"></select>
            </div>
        </div>

        <div style="margin-top: 15px; display: flex; gap: 10px;">
            <button onclick="locomotivePosition_toggleDirection()" title="Fahrtrichtung wechseln">
                <i class="fas fa-exchange-alt"></i>
            </button>
            <button onclick="locomotivePosition_abortPositioning()" title="Abbrechen">
                <i class="fas fa-stop"></i>
            </button>
            <button onclick="locomotivePosition_startPositioning()" title="Start">
                <i class="fas fa-play"></i>
            </button>
        </div>

        <p id="status-message" style="margin-top: 15px; font-style: italic;"></p>
    </div>`,
                                    buttons: `
                    <div style="display: flex; justify-content: center;">
                        <button class="w2ui-btn" onclick="w2popup.close();" style="background-color: #ccc;">Ok</button>
                    </div>`,
                                    onOpen(event) {
                                        event.onComplete = function () {
                                            const driverName = assignedLocomotive.driverName;
                                            const objectId = assignedLocomotive.objectId;

                                            const recLoc = window.locomotivesDlg.getLocomotiveRecord(driverName, objectId);
                                            $('#dialog-lok-id').text(recLoc.name);
                                            $('#dialog-block-id').text(ctrlId);

                                            populateFeedbackTargets(assignedSensors);
                                        };
                                    }
                                });

                            }
                        });

                        //
                        // Properties
                        //
                        entries.push({
                            cssIcon: null,
                            enabled: true,
                            label: 'Einstellungen',
                            onClick: () => {

                                const dlgEditName = `dlgeditBlock_${getCleanW2uiIdentifier(ctrlId)}`;

                                //
                                // popup dialog for Block
                                //
                                w2popup.open({
                                    title: `${ctrlId}`,
                                    body: '<div id="blockEditForm" style="width: 100%; height: 100%;"></div>',
                                    width: 400,
                                    height: 300,
                                    onOpen: function (event) {
                                        event.onComplete = function () {

                                            const formData = {
                                                blockLength: 50,
                                                blockStartDelay: 5,
                                                blockSignalsToRedDelay: 15
                                            };

                                            let selectedBlockSensor = null;
                                            if (window.settingsInfo?.blockSensors) {
                                                for (let ii = 0; ii < window.settingsInfo.blockSensors.length; ++ii) {
                                                    const b = window.settingsInfo.blockSensors[ii];
                                                    if (b && b.identifier.startsWith(ctrlId)) {
                                                        selectedBlockSensor = b;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (selectedBlockSensor) {
                                                formData.blockLength = selectedBlockSensor.length ?? 50;
                                                formData.blockStartDelay = selectedBlockSensor.startDelay ?? 5;
                                                formData.blockSignalsToRedDelay = selectedBlockSensor.signalsToRedDelay ?? 15;
                                            }

                                            if (w2ui[dlgEditName]) {
                                                w2ui[dlgEditName].destroy();
                                                w2ui[dlgEditName] = null;
                                            }

                                            if (!w2ui[dlgEditName]) {
                                                $('#blockEditForm').w2form({
                                                    name: dlgEditName,
                                                    fields: [
                                                        {
                                                            field: 'blockLength',
                                                            type: 'int',
                                                            html: { span: 8, caption: 'Länge' },
                                                            options: { arrows: true, min: 0, max: 500 }
                                                        },
                                                        {
                                                            field: 'blockStartDelay',
                                                            type: 'int',
                                                            html: { span: 8, caption: 'Abfahrverzögerung' },
                                                            options: { arrows: true, min: 1, max: 30 }
                                                        },
                                                        {
                                                            field: 'blockSignalsToRedDelay',
                                                            type: 'int',
                                                            html: { span: 8, caption: 'Signale-zu-Rot <span style="font-size: 60%;">(Wartezeit)</span>' },
                                                            options: { arrows: true, min: 5, max: 30 }
                                                        }
                                                    ],
                                                    record: formData,
                                                    actions: {
                                                        Save: function () {

                                                            self.__trigger('setting', {
                                                                command: 'update',
                                                                argument: 'blockExtras',
                                                                argumentValue: {
                                                                    blockIdentifier: ctrlId,
                                                                    length: parseInt(this.record.blockLength),
                                                                    startDelay: parseInt(this.record.blockStartDelay),
                                                                    signalsToRedDelay: parseInt(this.record.blockSignalsToRedDelay)
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
                                    } // onOpen
                                });
                            }
                        });

                        //
                        // Contextual
                        //
                        new Contextual({
                            isSticky: false,
                            items: entries
                        });

                    });
            }

            //
            // specific handling for staging controls
            //
            if (isStaging(themeId)) {
                const $self = this;
                newCtrl.addClass("staging");
                newCtrl.off("contextmenu");
                newCtrl.on("contextmenu",
                    function (ev) {
                        ev.preventDefault();
                        const $selfEv = ev;
                        const $selfCtrl = newCtrl;
                        const ctrlId = $selfCtrl.attr("id");
                        const entries = [];

                        //
                        // Enable/Disable
                        //
                        const isEnabled = newCtrl.data(constDataStateEnabled) ?? true;
                        const cssEnableLabel = isEnabled === true ? 'Deaktivieren ' + ctrlId : 'Aktivieren ' + ctrlId;
                        const cssEnableIcon = isEnabled === true ? 'fas fa-toggle-on' : 'fas fa-toggle-off';
                        entries.push({
                            cssIcon: cssEnableIcon,
                            label: cssEnableLabel,
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isBlockEnabled',
                                        argumentValue: {
                                            blockIdentifier: ctrlId,
                                            state: !isEnabled
                                        }
                                    });
                            }
                        });

                        //
                        // Lock/Unlock
                        //
                        const isLocked = newCtrl.data(constDataStateLock) ?? false;
                        const cssLockLabel = isLocked === true ? 'Entsperren ' + ctrlId : 'Sperren ' + ctrlId;
                        const cssLockIcon = isLocked === true ? 'fas fa-lock' : 'fas fa-lock-open';
                        entries.push({
                            cssIcon: cssLockIcon,
                            label: cssLockLabel,
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'isBlockLocked',
                                        argumentValue: {
                                            blockIdentifier: ctrlId,
                                            state: !isLocked
                                        }
                                    });
                            }
                        });

                        // default entries at the end
                        entries.push({ type: 'seperator' });
                        entries.push({
                            cssIcon: null,
                            enabled: true,
                            label: 'Stages',
                            onClick: () => {
                                $self.__openGridOverlay($selfEv);
                            }
                        });

                        new Contextual({
                            isSticky: false,
                            items: entries
                        });

                    });
            }

            //
            // specific handling for accessories (i.e. switches, signals, sensors)
            //
            if (isAccessory(themeId) || isSwitchOrAccessory(themeId) || isSignal(themeId) || isDecoupler(themeId) || isFeedback(themeId)) {
                const $self = this;
                newCtrl.off("contextmenu");
                newCtrl.on("contextmenu",
                    (ev) => {
                        ev.preventDefault();
                        const $selfEv = ev;
                        const $selfCtrl = newCtrl;
                        const ctrlId = $selfCtrl.attr("id");
                        const entries = [];

                        //
                        // Feedback
                        //
                        if (isFeedback(themeId)) {
                            entries.push({
                                cssIcon: null,
                                enabled: true,
                                label: 'Einstellungen',
                                onClick: () => {

                                    const sensorProvider = $selfCtrl.data(constDataSensorProvider);
                                    const sensorAddress = $selfCtrl.data(constDataSensorAddress);

                                    const cleanedId = getCleanW2uiIdentifier(`${sensorProvider}_${sensorAddress}`);
                                    const dlgEditName = `dlgeditFeedback_${cleanedId}`;

                                    //
                                    // create list of blocks/stages where the fb is assigned
                                    //
                                    const fbUsedBy = [];
                                    const checkAndPush = (it) => {
                                        if (it?.sensorEnter === ctrlId || it?.sensorIn === ctrlId) {
                                            fbUsedBy.push(it.identifier);
                                        }
                                    };
                                    window.settingsInfo.blockSensors.forEach(checkAndPush);
                                    window.settingsInfo.stagings.flatMap(staging => staging?.blocks || []).forEach(checkAndPush);

                                    //
                                    // popup dialog for Feedback
                                    //
                                    w2popup.open({
                                        title: `${ctrlId}`,
                                        body: '<div id="feedbackEditForm" style="width: 100%; height: 100%;"></div>',
                                        width: 400,
                                        height: 300,
                                        showMax: true,
                                        onOpen: function (event) {
                                            event.onComplete = function () {

                                                const formData = {
                                                    feedbackDriverName: '',
                                                    feedbackAddress: 0,
                                                    feedbackUsedByBlock: ''
                                                };

                                                const typeItems = [
                                                    { id: 1, text: 'ecos', caption: 'ecos' },
                                                    { id: 2, text: 's88', caption: 's88' },
                                                    { id: 3, text: 'z21', caption: 'z21' }
                                                ];

                                                if (sensorProvider) {
                                                    for (let i = 0; i < typeItems.length; ++i) {
                                                        if (typeItems[i].text === sensorProvider) {
                                                            formData.feedbackDriverName = typeItems[i];
                                                        }
                                                    }
                                                }

                                                let ansteuerungCaption = 'Ansteuerung';
                                                if (formData.feedbackDriverName.caption === "ecos"
                                                    || formData.feedbackDriverName.caption === "s88") {
                                                    ansteuerungCaption = constTxtAnsteuerungIndex;
                                                }
                                                else if (formData.feedbackDriverName.caption === "z21") {
                                                    ansteuerungCaption = constTxtAnsteuerungPin;
                                                }

                                                if (sensorAddress) formData.feedbackAddress = sensorAddress;

                                                if (fbUsedBy.length > 0) {
                                                    formData.feedbackUsedByBlock = fbUsedBy.join(", ");
                                                }

                                                if (w2ui[dlgEditName]) {
                                                    w2ui[dlgEditName].destroy();
                                                    w2ui[dlgEditName] = null;
                                                }

                                                if (!w2ui[dlgEditName]) {
                                                    $('#feedbackEditForm').w2form({
                                                        name: dlgEditName,
                                                        fields: [
                                                            {
                                                                field: 'feedbackDriverName', type: 'list',
                                                                html: {
                                                                    span: 8,
                                                                    caption: 'Treiber'
                                                                },
                                                                options: { items: typeItems }
                                                            },
                                                            {
                                                                field: 'feedbackAddress',
                                                                type: 'string',
                                                                html: {
                                                                    span: 8,
                                                                    caption: ansteuerungCaption,
                                                                    attr: 'style="text-align: center; letter-spacing: 3px; font-family: monospace;"'
                                                                },
                                                                options: { arrows: true, min: 0, max: (16 * 62) }
                                                            },
                                                            {
                                                                field: 'feedbackUsedByBlock', type: 'textarea',
                                                                html: {
                                                                    span: 8,
                                                                    caption: 'Verwendung',
                                                                    attr: 'style="width: 200px; height: 80px; resize: none; background-color: #fff;;" readonly'
                                                                }
                                                            }
                                                        ],
                                                        record: formData,
                                                        onChange: function (ev) {
                                                            if (ev.target === 'feedbackDriverName') {
                                                                const selected = ev.value_new;
                                                                const identifier = (selected?.caption || selected?.text || '').trim().toLowerCase();
                                                                let newText = 'Ansteuerung';
                                                                switch (identifier) {
                                                                    case 's88':
                                                                    case 'ecos':
                                                                        newText = constTxtAnsteuerungIndex;
                                                                        break;
                                                                    case 'z21':
                                                                        newText = constTxtAnsteuerungPin;
                                                                        break;
                                                                    default:
                                                                        newText = '??';
                                                                }

                                                                const labelEl = $('#' + ev.target)
                                                                    .closest('.w2ui-page')
                                                                    .find('label')
                                                                    .filter(function () {
                                                                        return $(this).html().includes('Ansteuerung');
                                                                    }); 

                                                                if (labelEl.length > 0) {
                                                                    labelEl.html(newText); // z. B. "Ansteuerung (Index)"
                                                                }
                                                            }
                                                        },
                                                        actions: {
                                                            Save: function () {

                                                                self.__trigger('setting', {
                                                                    command: 'update',
                                                                    argument: 'sensorAddress',
                                                                    argumentValue: {
                                                                        name: ctrlId,
                                                                        provider: this.record.feedbackDriverName.text,
                                                                        address: this.record.feedbackAddress
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
                                        } // onOpen
                                    });
                                }
                            });
                        }
                        // Switch or Accessories or Signal or Decoupler
                        else if (isSwitchOrAccessory(themeId) || isSignal(themeId) || isDecoupler(themeId)) {
                            //
                            // Maintenance / Wartung
                            //
                            const isMaintenance = newCtrl.data(constDataStateIsMaintenance) ?? false;
                            const cssEnableLabel = isMaintenance === true ? 'Maintenance ' + ctrlId : 'Maintenance ' + ctrlId;
                            const cssEnableIcon = isMaintenance === true ? 'fas fa-toggle-on' : 'fas fa-toggle-off';
                            entries.push({
                                cssIcon: cssEnableIcon,
                                label: cssEnableLabel,
                                onClick: () => {
                                    self.__trigger("automode",
                                        {
                                            command: 'update',
                                            argument: 'isMaintenaceEnabled',
                                            argumentValue: {
                                                planfieldControlIdentifier: ctrlId,
                                                isMaintenance: !isMaintenance
                                            }
                                        });
                                }
                            });
                            entries.push({ type: 'seperator' });

                            //
                            // automatische Reparatur
                            //
                            entries.push({
                                cssIcon: 'fa fa-wrench',
                                enabled: true,
                                label: 'Reparatur',
                                onClick: () => {
                                    showAccessoryRepairPrompt(ctrlId);
                                }
                            });
                            entries.push({ type: 'seperator' });

                            //
                            // Switches / Accessories / Signals
                            //                          
                            entries.push({
                                cssIcon: null,
                                enabled: true,
                                label: 'Einstellungen',
                                onClick: () => {

                                    const itemObj = newCtrl.data(constDataThemeItemObject);
                                    const identifier = itemObj.identifier ?? newCtrl.attr("id");

                                    let hideInvertOption = false;
                                    if (itemObj.editor.themeId === 52) {
                                        hideInvertOption = true;
                                    }

                                    const ecosObjectItems = [];
                                    const demoObjectItems = [];
                                    const z21ObjectItems = [];

                                    const typeItems = [
                                        { id: 1, text: 'ecos', caption: 'ecos' },
                                        { id: 2, text: 'demo', caption: 'demo' },
                                        { id: 3, text: 'z21', caption: 'z21' }
                                    ];

                                    const accRecords = window.accessoriesDlg.getRecords();
                                    for (let i = 0; i < accRecords.length; ++i) {
                                        const accIt = accRecords[i];
                                        if (accIt.driverName === "ecos") {
                                            ecosObjectItems.push({
                                                id: i + 1,
                                                text: accIt.identifier,
                                                caption: accIt.identifier
                                            });
                                        } else if (accIt.driverName === "demo") {
                                            demoObjectItems.push({
                                                id: i + 1,
                                                text: accIt.identifier,
                                                caption: accIt.identifier
                                            });
                                        } else if (accIt.driverName === "z21") {
                                            z21ObjectItems.push({
                                                id: i + 1,
                                                text: accIt.identifier,
                                                caption: accIt.identifier
                                            });
                                        }
                                    }

                                    let accIdx = -1;
                                    let accInstance = null;

                                    for (let i = 0; i < window.settingsInfo.accessories.length; ++i) {
                                        const accIt = window.settingsInfo.accessories[i];
                                        if (accIt) {
                                            if (accIt.planfieldControlIdentifier === identifier) {
                                                accIdx = i;
                                                accInstance = accIt;
                                                break;
                                            }
                                        }
                                    }

                                    const dlgEditName = `dlgeditAccessory_${getCleanW2uiIdentifier(identifier)}`;

                                    /**
                                     * Sample data for accInstance:
                                     * 
                                     *  accessoryDriver: "ecos"
                                     *  accessoryIdentifier: "O2_2"
                                     *  isMaintenaceEnabled: false
                                     *  planfieldControlIdentifier: "O2_2"
                                     */

                                    //
                                    // popup dialog for accessory properties
                                    //
                                    w2popup.open({
                                        title: `${ctrlId}`,
                                        body: '<div id="accessoryEditForm" style="width: 100%; height: 100%;"></div>',
                                        width: 400,
                                        height: 300,
                                        showMax: true,
                                        onOpen: function (event) {
                                            event.onComplete = function () {

                                                const formData = {
                                                    accessoryDriver: '',
                                                    accessoryIdentifier: 0,
                                                    accessoryInvert: false,
                                                    accessoryInvertUi: false
                                                };

                                                if (accInstance) {
                                                    for (let i = 0; i < typeItems.length; ++i) {
                                                        if (typeItems[i].text === accInstance.accessoryDriver) {
                                                            formData.accessoryDriver = typeItems[i];
                                                        }
                                                    }

                                                    for (let i = 0; i < ecosObjectItems.length; ++i) {
                                                        if (ecosObjectItems[i].text === accInstance.accessoryIdentifier) {
                                                            formData.accessoryIdentifier = ecosObjectItems[i];
                                                            formData.accessoryInvert = accInstance.invert;
                                                            formData.accessoryInvertUi = accInstance.invertUi;
                                                            break;
                                                        }
                                                    }

                                                    // in der Liste der ECoS entities wurde keine Auswahl gefunden
                                                    // wir schauen mal ob eine demo-Auswahl besteht
                                                    if (formData.accessoryIdentifier == 0) {
                                                        for (let i = 0; i < demoObjectItems.length; ++i) {
                                                            if (demoObjectItems[i].text === accInstance.accessoryIdentifier) {
                                                                formData.accessoryIdentifier = demoObjectItems[i];
                                                                formData.accessoryInvert = accInstance.invert;
                                                                formData.accessoryInvertUi = accInstance.invertUi;
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    // in der Liste der ECoS und demos entities wurde keine Auswahl gefunden
                                                    // wir schauen mal ob eine demo-Auswahl besteht
                                                    if (formData.accessoryIdentifier == 0) {
                                                        for (let i = 0; i < z21ObjectItems.length; ++i) {
                                                            if (z21ObjectItems[i].text === accInstance.accessoryIdentifier) {
                                                                formData.accessoryIdentifier = z21ObjectItems[i];
                                                                formData.accessoryInvert = accInstance.invert;
                                                                formData.accessoryInvertUi = accInstance.invertUi;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }

                                                if (w2ui[dlgEditName]) {
                                                    w2ui[dlgEditName].destroy();
                                                    w2ui[dlgEditName] = null;
                                                }

                                                if (!w2ui[dlgEditName]) {
                                                    $('#accessoryEditForm').w2form({
                                                        name: dlgEditName,
                                                        fields: [
                                                            {
                                                                field: 'accessoryDriver',
                                                                type: 'list',
                                                                html: { caption: 'Treiber' },
                                                                options: { items: typeItems }
                                                            },
                                                            {
                                                                field: 'accessoryIdentifier',
                                                                type: 'list',
                                                                html: { caption: 'Identifizierer' },
                                                                options: { items: [...ecosObjectItems, ...demoObjectItems, ...z21ObjectItems] }
                                                            },
                                                            {
                                                                field: 'accessoryInvert',
                                                                type: 'toggle',
                                                                hidden: hideInvertOption,
                                                                html: { caption: 'Invertieren (Exec)' },
                                                                options: { items: formData.accessoryInvert },
                                                                tooltip: 'Wenn aktiviert, wird die Schaltung umgekehrt ausgeführt.'
                                                            },
                                                            {
                                                                field: 'accessoryInvertUi',
                                                                type: 'toggle',
                                                                hidden: hideInvertOption,
                                                                html: { caption: 'Invertieren (UI)' },
                                                                options: { items: formData.accessoryInvertUi },
                                                                tooltip: 'Wenn aktiviert, wird die Darstellung umgekehrt angezeigt.'
                                                            }
                                                        ],
                                                        record: formData,
                                                        actions: {
                                                            Save: function () {

                                                                self.__trigger('setting', {
                                                                    command: 'update',
                                                                    argument: 'accessoryAddress',
                                                                    argumentValue: {
                                                                        planfieldControlIdentifier: ctrlId,
                                                                        provider: this.record.accessoryDriver.text,
                                                                        address: this.record.accessoryIdentifier.text,
                                                                        invert: this.record.accessoryInvert,
                                                                        invertUi: this.record.accessoryInvertUi
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
                            });
                        }

                        new Contextual({
                            isSticky: false,
                            items: entries
                        });
                    });
            }
        }

        if (newCtrl === null) return;

        // setze coord, als die Koordinaten im Designer Grid
        // keine Pixelwerte!
        if (true) {
            const localCtrlData = newCtrl.data(constDataThemeItemObject);
            if (localCtrlData) {
                if (!localCtrlData?.coord) {
                    localCtrlData.coord = coord;
                } else {
                    if (localCtrlData) {
                        localCtrlData.coord = coord;
                    }
                }
            }
        }

        // init drop and ctx menu for block
        if (isBlock(jsonData.editor.themeId)) {
            this.__initBlockDrop(newCtrl);
        }

        const parentForControl = this.planfieldElement.find(".ries-grid");
        newCtrl.appendTo(parentForControl);

        newCtrl.off('click');
        newCtrl.on('click', function (ev) {
            const isTextFieldCtrl = $(this).hasClass("elEditorRoot");
            if (isTextFieldCtrl) return;
            if (self.isEditMode) {
                if (!self.currentSelectionMoved) {
                    if (self.__isCtrlSelected(newCtrl)) {
                        self.__unselectCtrl(newCtrl);

                        //
                        // A T T E N T I O N
                        // -----------------
                        // Ist hier irgendwie doof, wenn wir nachgelagerte click-Evnts einfach
                        // abschalten; denn es könnte sich auch die Reihenfolge der Registrierungen
                        // der click-Events ändern, dann könnte es falschem Verhalten führen.
                        //
                        ev.stopImmediatePropagation();
                    } else {
                        self.clearCtrlSelection();
                        self.__selectCtrl(newCtrl);
                    }
                }
            }
        });

        newCtrl.off('mouseover');
        newCtrl.on('mouseover', function (ev) {
            const themeItemJson = $(this).data(constDataThemeItemObject);
            let coord = themeItemJson.coord;
            if (!coord)
                coord = getElementCoord(self.planfieldElement, ev);
            const ctrlId = $(this).attr("id");
            const infoText = `${ctrlId} (${coord.x}, ${coord.y})`;
            const targetEl = $('#statusBar div.ctrlInfo');
            targetEl.html(infoText);
        });

        newCtrl.off('mouseleave');
        newCtrl.on('mouseleave', function (ev) {
            const targetEl = $('#statusBar div.ctrlInfo');
            targetEl.html("");
        });
        if (this.isEditMode)
            this.__initDragFor(newCtrl);
        if (options.editMode) {
            var isTextFieldCtrl = newCtrl.hasClass("elEditorRoot");
            if (isTextFieldCtrl)
                ; // TODO enable edit mode but only with "remove" command
            else
                this.__selectCtrl(newCtrl);
        }
        this.editMenubar.setCurrentSelection(newCtrl);
        this.editMenubar.rot(-1, newCtrl, this.__isInitialization);

        this.editMenubar.on('commandClicked',
            (ev) => {
                console.log("command clicked" + $(this).attr('id'));
            });

        if (!this.currentSelectionMoved) {
            this.clearCtrlSelection();
            this.__selectCtrl(newCtrl);
            this.editMenubar.hideEditMenu();
        }

        this.__trigger('controlCreated', { instance: newCtrl });

        this.validateSensors();
        this.validateAccessories();
        this.validateBlocks();
    }

    __openGridOverlay(event) {
        const target = $(event.target);
        window.stagingDlg.show(target);
    }

    __removeDragFor(jqueryEl) {
        if (jqueryEl === null) return;
        const elmnt = jqueryEl.get(0);
        elmnt.ondragstart = null;
    }

    __initDragFor(jqueryEl) {
        var elmnt = jqueryEl.get(0);
        elmnt.ondragstart = dragStart;
        var ctx = jqueryEl.parent();

        const self = this;

        let deltaCoord = null;

        function dragStart(e) {
            if (!self.isEditMode)
                return;

            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            self.currentSelectionMoved = false;
            self.currentSelectionMovedItem = null;

            let coord = { x: 0, y: 0 };

            const isTextFieldCtrl = $(this).hasClass("elEditorRoot");
            if (isTextFieldCtrl) {
                const elToSelect0 = $(this);
                coord = elToSelect1.data(constDataThemeItemObject).coord;

                self.currentSelectionMoved = true;
                self.currentSelectionMovedItem = elToSelect0;

                self.__selectCtrl(elToSelect0, {
                    startEditMode: false
                });
            } else {
                const elToSelect1 = $(e.target).parent();
                coord = elToSelect1.data(constDataThemeItemObject).coord;
                self.currentSelectionMoved = true;
                self.currentSelectionMovedItem = elToSelect1;
                self.__selectCtrl(elToSelect1);
            }

            document.onmouseup = closeDragElement;
            document.onmousemove = elementDrag;

            const parent = $(e.target).parent();
            const coordItemStart = self.__getBoundingCoord(parent);

            deltaCoord = {
                x: parseInt(coord.x - coordItemStart.x),
                y: parseInt(coord.y - coordItemStart.y)
            };
        }

        function elementDrag(e) {
            if (!self.isEditMode) return;

            const container = $('#gleisplan_tabPlan1').find('.ries-container')[0];

            const containerRect = container.getBoundingClientRect();
            const mouseX = event.clientX - containerRect.left + container.scrollLeft;
            const mouseY = event.clientY - containerRect.top + container.scrollTop;

            const coordX = parseInt(mouseX / constItemWidth);
            const coordY = parseInt(mouseY / constItemHeight);

            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            if (self.currentSelection) {
                const isTextFieldCtrl = self.currentSelection.hasClass("elEditorRoot");
                const top = coordY * constItemHeight;
                const left = coordX * constItemWidth;

                //const themeItemJson = self.currentSelection.data(constDataThemeItemObject);
                //if (themeItemJson) {
                //    let ccc = themeItemJson.coord;
                //    if (ccc) {
                //        ccc = { x: coordX, y: coordY };

                //        self.currentSelection.data(constDataThemeItemObject, themeItemJson);
                //    }
                //}

                elmnt.style.top = top + "px";
                elmnt.style.left = left + "px";

                if (isTextFieldCtrl) {
                    // do not open edit editMenubar
                } else {
                    self.editMenubar.updateEditMenuPositionAndEvents(self.currentSelection);
                }
            }
        }

        function closeDragElement(ev) {
            if (!self.isEditMode) return;

            document.onmouseup = null;
            document.onmousemove = null;

            if (!self.__isInitialization) {

                const container = $('#gleisplan_tabPlan1').find('.ries-container')[0];
                const containerRect = container.getBoundingClientRect();
                const mouseX = event.clientX - containerRect.left + container.scrollLeft;
                const mouseY = event.clientY - containerRect.top + container.scrollTop;
                const coordX = parseInt(mouseX / constItemWidth);
                const coordY = parseInt(mouseY / constItemHeight);

                const themeItemJson = self.currentSelection.data(constDataThemeItemObject);

                // die alten Koordinaten
                const originalCoord = themeItemJson.coord;

                // setze neue Koordinaten
                themeItemJson.coord = {
                    x: coordX,
                    y: coordY
                };
                self.currentSelectionMovedItem.data(constDataThemeItemObject, themeItemJson);

                //self.removeControl(self.currentSelectionMovedItem.attr("id"));
                //self.sendControl(self.currentSelectionMovedItem);

                self.moveControl(
                    self.currentSelectionMovedItem.attr("id"),
                    originalCoord,
                    themeItemJson.coord
                    );
            }
            self.currentSelectionMovedItem = null;
        }
    }

    /**
     *      * 
     * @param {object} data
     */
    createPlanfield(data = {}) {
        this.__isInitialization = true;

        this.themeData = data.themeData;
        this.planfield = data.planfield;

        Object.keys(this.planfield).forEach(key => {
            if (this.planfield.hasOwnProperty(key)) {
                const itemCtrl = this.planfield[key];
                this.__createControl(itemCtrl, itemCtrl.coord, { editMode: false });
            }
        });

        this.__isInitialization = false;
    }

    /**
     * Sends updated control data based on the provided jQuery element.
     * The data includes various properties such as identifier, coordinates, theme dimensions,
     * and if the element is a text field, its inner and outer HTML as well as its size.
     * 
     * @param {jQuery} jqueryElement - The jQuery element representing the control.
     * @returns {boolean} - Returns false if the connection is not established, otherwise sends the data.
     */
    async sendControl(jqueryElement) {

        const $self = this;

        const fnc2 = function (jqueryElement) {
            try {
                const ctrlData = jqueryElement.data(constDataThemeItemObject);
                if (ctrlData && !ctrlData.coord) {
                    ctrlData.coord = $self.__getBoundingCoord(jqueryElement);;
                }

                if (ctrlData?.editor?.themeId) {
                    const themeDimIdx = jqueryElement.data(constDataThemeDimensionIndex);
                    return {
                        identifier: jqueryElement.attr("id"),
                        coord: ctrlData.coord,
                        themeId: ctrlData?.editor?.themeId,
                        themeDimIdx: themeDimIdx
                    }
                }
            } catch (err) {
                console.log(err);
            }
        }

        const argumentValueObject = fnc2(jqueryElement);
        if (argumentValueObject) {
            const dataToSend = {
                command: 'update',
                argument: 'updateControl',
                argumentValue: argumentValueObject
            };

            // in case the control is a textfield
            // some attributes must be altered
            if (jqueryElement.hasClass("elEditorRoot")) {
                if (!dataToSend.argumentValue.editor)
                    dataToSend.argumentValue.editor = {};
                const html = jqueryElement.find('.elEditor').html();
                const fontSize = jqueryElement.find(".elEditor").css("font-size");
                dataToSend.argumentValue.editor.innerHtml = html;
                dataToSend.argumentValue.editor.fontSize = fontSize;
            }

            sendTrackplanCommand(dataToSend);
        }
    }

    /**
     * Removes the control specified by the given item ID by sending a removal request over WebSocket.
     * 
     * @param {string} itemId - The ID of the control to be removed.
     * @returns {boolean} - Returns false if the connection is not established, otherwise sends the removal request.
     */
    removeControl(itemId) {
        sendTrackplanCommand({
            command: 'update',
            argument: 'removeControl',
            argumentValue: itemId
        });
    }

    moveControl(itemId, currentCoord, targetCoord) {
        sendTrackplanCommand({
            command: 'update',
            argument: 'moveControl',
            argumentValue: {
                "id": itemId,
                "currentCoord": currentCoord,
                "targetCoord": targetCoord
            }
        });
    }

    /**
     * 
     * @param {any} blockId
     * @param {any} fbEnter
     * @param {any} fbIn
     * @param {any} signal
     * @param {any} vorsignal
     */
    highlightBlock(blockId, fbEnter, fbIn, signal, vorsignal) {
        toggleAllLocomotiveInformationOpacity(false);
        const ctrlsFeedback = $('div.ctrlItemSensors');
        ctrlsFeedback.each(function () {
            const id = $(this).attr('id');
            if (id === fbEnter || id === fbIn) {
                $(this).addClass('highlightSensor');
                if (id === fbEnter)
                    $(this).addClass('blockPlus');
                else if (id === fbIn)
                    $(this).addClass('blockMinus');
            }
        });

        const ctrlsSignals = $('div.ctrlItemAccessory');
        ctrlsSignals.each(function () {
            const id = $(this).attr('id');
            if (id === signal || id === vorsignal) {
                $(this).addClass('highlightSignal');
                if (id === signal)
                    $(this).addClass('blockSignal');
                else if (id === vorsignal)
                    $(this).addClass('blockVorsignal');
            }
        });

        const ctrlsBlocks = $('div.ctrlItemBlock');
        ctrlsBlocks.each(function () {
            const id = $(this).attr('id');
            if (id === blockId) {
                $(this).find('img').css('transition', '');
                $(this).addClass('highlightSensorBlock');
                $(this).find('img').css({
                    opacity: 0.0
                });
            }
        });
    }

    /**
     *
     */
    unhighlightAllBlocks() {
        toggleAllLocomotiveInformationOpacity(true);

        const ctrlsFeedback = $('div.ctrlItemSensors');
        ctrlsFeedback.each(function () {
            $(this).removeClass('highlightSensor');
            $(this).removeClass('blockPlus');
            $(this).removeClass('blockMinus');
        });

        const ctrlsSignals = $('div.ctrlItemAccessory');
        ctrlsSignals.each(function () {
            $(this).removeClass('highlightSignal');
            $(this).removeClass('blockSignal');
            $(this).removeClass('blockVorsignal');
        });

        const ctrlsBlocks = $('div.ctrlItemBlock');
        ctrlsBlocks.each(function () {
            $(this).removeClass('highlightSensorBlock');
            $(this).find('img').css({
                opacity: 1.0
            });
        });
    }
}