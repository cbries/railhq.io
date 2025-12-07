// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class EditMenuBar {

    /**
     * 
     */
    constructor(owner) {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.editMenu = $('div#planfieldItemEdit');
        this.cmdRotLeft = this.editMenu.find('div.rot-left');
        this.cmdRotRight = this.editMenu.find('div.rot-right');
        this.cmdRemove = this.editMenu.find('div.delete');
        this.cmdUngroup = this.editMenu.find('div.ungroup');
        this.__owner = owner;
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
     *  
     * @param {any} mode 0 := rotate right, 1 := rotate left
     * @param {any} jqueryElement
     * @return true := rotation applied, false := nothing has changed
     */
    rot(mode, jqueryElement, isInitialization = false) {
        const themeJsonData = jqueryElement.data(constDataThemeItemObject)
        const themeId = themeJsonData.editor.themeId;
        const themeData = this.__owner.__getThemeInformationById(themeId);
        
        var themeTransformOrigin = null;
        var currentDimensionIndex = jqueryElement.data(constDataThemeDimensionIndex);
        if (!currentDimensionIndex) currentDimensionIndex = 0;

        var dimensions = { w: 1, h: 1 };
        var noOfDimensions = 1;
        if (themeData?.dimensions && themeData.dimensions.length > 0) {
            dimensions = themeData.dimensions[0];
            noOfDimensions = themeData.dimensions.length;
        }

        if (dimensions.w === 1 && dimensions.h === 1) {
            themeTransformOrigin = "center center";
            if (mode === 0) {
                ++currentDimensionIndex;
                if (currentDimensionIndex > 3)
                    currentDimensionIndex = 0;
            }
            else if (mode === 1) {
                --currentDimensionIndex;
                if (currentDimensionIndex < 0)
                    currentDimensionIndex = 3;
            }
        }
        else {
            themeTransformOrigin = "top left";
            if (mode === 0) {
                if (currentDimensionIndex === 1)
                    currentDimensionIndex = 0;
                else
                    currentDimensionIndex = 1;
            }
            else if (mode === 1) {
                if (currentDimensionIndex === 0)
                    currentDimensionIndex = 1;
                else
                    currentDimensionIndex = 0;
            }
        }

        jqueryElement.data(constDataThemeDimensionIndex, currentDimensionIndex);

        if (noOfDimensions > 1) {
            var currentLeft = null;
            var newLeft = null;

            const isZeroDegree = (currentDimensionIndex % 4) === 0;

            if (!isInitialization) {
                if (mode > -1) { // only move ctrl when rotation is applied
                    if (isZeroDegree) {
                        currentLeft = this.currentSelection.css("left");
                        newLeft = "calc( " + currentLeft + " - " + constItemWidth + "px)";
                        this.currentSelection.css("left", newLeft);
                    } else {
                        currentLeft = this.currentSelection.css("left");
                        newLeft = "calc( " + currentLeft + " + " + constItemWidth + "px)";
                        this.currentSelection.css("left", newLeft);
                    }
                }
            } else {
                if (isZeroDegree) {
                    // do not move the ctrl during initialization when the rotation is 0deg
                } else {
                    currentLeft = this.currentSelection.css("left");
                    newLeft = "calc( " + currentLeft + " + " + constItemWidth + "px)";
                    this.currentSelection.css("left", newLeft);
                }
            }
        }
        const themeTransform = "rotate(" + (currentDimensionIndex * 90) + "deg)";
        this.currentSelection.css("transform-origin", themeTransformOrigin);
        this.currentSelection.css("transform", themeTransform);

        //
        // Hier an der Stelle können wir das Label anpassen...
        //
        if (this.currentSelection.hasClass("connector")) {
            const connectorId = this.currentSelection.data(constDataConnectorId);
            this.__owner.__updateConnectorVisualization(this.currentSelection, connectorId);
        }

        if (!isInitialization) {
            this.updateEditMenuPositionAndEvents(jqueryElement);
            if (noOfDimensions === 1) {
                
                window.toolbox.rotate({
                    themeId: themeId,
                    themeDimIdx: currentDimensionIndex,
                    transformOrigin: themeTransformOrigin,
                    transform: themeTransform
                });
            }

            window.planfield.sendControl(this.currentSelection);
        }

        return true;
    }

    setCurrentSelection(jqueryElement) {
        this.currentSelection = jqueryElement;
        this.__unbindMenuBar();
        const self = this;

        this.cmdRotLeft.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            const res = self.rot(1, self.currentSelection);
            if (!res) return;
        });

        this.cmdRotRight.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            const res = self.rot(0, self.currentSelection);
            if (!res) return;
        });

        this.cmdRemove.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();

            const itemId = self.currentSelection.attr("id");

            self.removeControl(itemId);
        });

        this.cmdUngroup.click(function (e) {
            e = e || window.event;
            e.preventDefault();
            e.stopPropagation();
            window.planfield?.clearCtrlSelection();
        });
    }

    removeControl(itemId) {
        if (!itemId) return;
        const self = this;
        self.hideEditMenu();
        self.currentSelection.remove();
        window.planfield?.removeControl(itemId);
    }

    __unbindMenuBar() {
        this.cmdRemove.unbind("click");
        this.cmdRotLeft.unbind("click");
        this.cmdRotRight.unbind("click");
    }
    
    updateEditMenuPositionAndEvents(jqueryElementCtrl) {
        if (!jqueryElementCtrl || jqueryElementCtrl.length === 0) return;

        const nativeCtrl = jqueryElementCtrl.get(0);
        const rect = nativeCtrl.getBoundingClientRect();

        const currentCtrlWidth = rect.width;
        const currentCtrlX = rect.left + window.scrollX; // Absolute X-Position
        const currentCtrlY = rect.top + window.scrollY;  // Absolute Y-Position

        const menuWidth = this.editMenu.outerWidth();
        const menuHeight = this.editMenu.outerHeight();

        const viewportWidth = window.innerWidth; // Fensterbreite
        const padding = 10; // Abstand vom Control

        let x, y;

        if (currentCtrlX + currentCtrlWidth + menuWidth + padding < viewportWidth) {
            x = currentCtrlX + currentCtrlWidth + padding;
        } else {
            x = currentCtrlX - menuWidth - padding;
        }
        y = currentCtrlY + (rect.height / 2) - (menuHeight / 2);
        this.editMenu.css({ left: x + "px", top: y + "px" });
    }


    hideEditMenu() {
        this.__unbindMenuBar();
        this.editMenu.hide();
    }

    showEditMenu() {
        this.editMenu.show();
    }
}