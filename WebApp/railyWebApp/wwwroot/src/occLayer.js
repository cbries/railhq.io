// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


class OccLayer {
    constructor() {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__divRootClass = "locomotiveInfo";
        this.__divFromClass = "locomotiveInfoFrom";
        this.__divFinalClass = "locomotiveInfoFinal";
        this.__dataDriverName = "locodriver";
        this.__dataObjectId = "locoid";

        this.__borderPaddingLeft = 0;
        this.__borderPaddingTop = 0;
        this.__borderPaddingRight = 4;

        this.__settingsInfo = null; // user settings and configurations received from the railhq.io server
        this.__dragStartBlockElement = null; // used for dragging assigned control info from blocks

        this.__countdownTimers = new Map();

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

    __generateLocomotiveInfoIdentifiers(driverName, objectId) {
        return {
            "root": `locomotiveInfo_${driverName}_${objectId}`,
            "rootFinal": `locomotiveInfoFinal_${driverName}_${objectId}`,
            "image": `locomotiveInfoImage_${driverName}_${objectId}`,
            "imageFinal": `locomotiveInfoImageFinal_${driverName}_${objectId}`
        };
    }

    __createLocomotiveInfo(info) {
        const ids = this.__generateLocomotiveInfoIdentifiers(info.driverName, info.objectId);
        const self = this;

        const ctrl = $(`#${ids.root}`);
        if (ctrl && ctrl.length > 0) return;

        const checkInfo = $(`${ids.root}`);
        if (!checkInfo || checkInfo.length < 0) return;

        const locomotiveInfo = $('<div>', {})
            .css({
                //"z-index": 50,
                //"position": "absolute",
                //"overflow": "hidden",
                //"height": "28px",
                //"max-height": "28px",
                //"line-height": "28px",
                //"display": "inline-block",
                //"vertical-align": "middle",
                //"font-weight": "bold",
                //"cursor": "pointer",
                //"text-align": "center"
            })
            .attr("id", ids.root)
            .data(this.__dataDriverName, info.driverName)
            .data(this.__dataObjectId, info.objectId);

        locomotiveInfo.attr("unselectable", "on");
        locomotiveInfo.attr("onselectstart", "return false;");
        locomotiveInfo.attr("onmousedown", "return false;");
        locomotiveInfo.addClass(this.__divRootClass);
        locomotiveInfo.addClass(this.__divFromClass);
        locomotiveInfo.addClass("noselect");

        const locLabel = $('<div>')
            .css({
                "position": "absolute",
                "top": "0",
                "left": "0"
            })
            .addClass("locInfoLabel")
            .html(info.name);
        locLabel.appendTo(locomotiveInfo);

        // Locking 
        const locLockStateCtrl = $('<i>').css({
            display: "none",
            position: "absolute",
            right: "1px",
            top: "18px",
            color: "red",
            "font-size": "0.6rem"
        })
            .addClass("fas fa-lock")
            .addClass("lockState");
        locLockStateCtrl.appendTo(locomotiveInfo);

        // Forward/Backward
        const locDirection = $('<i>').css({
            position: "absolute",
            left: "2px",
            top: "7px",
            "font-size": "0.6rem",
            "z-index": "100",
            "font-weight": "bold"
        }).text("F").addClass("locDirection");
        locDirection.appendTo(locomotiveInfo);

        // EnterSide
        const locEnterSideCtrl = $('<i>').css({ display: "none", "z-index": "100" })
            .addClass("fas fa-door-open")
            .addClass("enterSide");
        locEnterSideCtrl.appendTo(locomotiveInfo);

        // Speedinfo
        const locSpeedInfoCtrl = $('<div>').css({
            position: "absolute",
            right: "1px",
            bottom: "-10px",
            color: "black",
            "font-size": "0.45rem",
            "letter-spacing": "-1px",
            "z-index": "99"
        })
            .html("-.-")
            .addClass("speedInfo");
        locSpeedInfoCtrl.appendTo(locomotiveInfo);

        //
        // countdown for next run
        //
        const locCountdown = $('<div>').css({
            position: "absolute",
            "z-index": "100",
            "align-items": "center"
        }).addClass('locomotiveCountdown');
        // Animierte Sanduhr-Icon
        const hourglassIcon = $('<i>').addClass('fas fa-hourglass').css({
            marginRight: '8px',
            animation: 'rotateLocomotiveCountdown 2s linear infinite'
        });
        // Timer-Div für den Countdown-Wert
        const countdownText = $('<div>').css({
        }).text("0"); // Startwert
        locCountdown.append(hourglassIcon, countdownText);
        locCountdown.appendTo(locomotiveInfo);

        //
        // final locomotive info
        //
        const locomotiveInfoFinal = locomotiveInfo.clone();
        locomotiveInfoFinal.attr("id", ids.rootFinal);
        locomotiveInfoFinal.data(this.__dataDriverName, info.driverName);
        locomotiveInfoFinal.data(this.__dataObjectId, info.objectId);
        locomotiveInfoFinal.removeClass(this.__divFromClass);
        locomotiveInfoFinal.addClass(this.__divFinalClass);
        locomotiveInfoFinal.dblclick(function () {
            console.log("TODO reset assignment from final block");
            //self.__trigger("resetAssignment",
            //    {
            //        mode: 'resetAssignment',
            //        submode: 'final',
            //        oid: locData.objectId
            //    });
        });

        //
        // Load and show locomotive image if available.
        //
        const fakeId = `loadLocomotivesImagePlan_${info.objectId}`;
        loadLocomotiveImageIntoHtml(fakeId, info.name);
        const locImg = $('<img>', {
            id: `${fakeId}`,
            src: './images/dummy.png',
            alt: info.name
        }).addClass("locInfoImage");
        locImg.on("load",
            () => {
                $(this).show();
                locLabel.hide();
            }).on("error",
                () => {
                    $(this).hide();
                    locLabel.show();
                });
        locImg.appendTo(locomotiveInfo);

        locomotiveInfo.off('mouseover');
        locomotiveInfo.on('mouseover', function (ev) {
            const targetEl = $('#statusBar div.ctrlInfo');
            targetEl.html("&lt;Shift&gt;+Rechtsklick öffnet das Kontextmenü des Blocks.");
        });

        //
        // add context menu for block with assigned locomotive
        //
        locomotiveInfo.off('contextmenu');
        locomotiveInfo.on('contextmenu',
            function (ev) {
                ev.preventDefault();

                //
                // Wenn ein Kontextmenü mit gedrückter <shift>-Taste
                // geöffnet wird, dann wird geprüft ob darunter ein
                // Block liegt und wenn ja, dann wird das Kontextmenü
                // vom Block angezeigt.
                //
                if (ev.shiftKey) {
                    var mouseX = ev.pageX;
                    var mouseY = ev.pageY;
                    var foundElement = null;

                    $(".ctrlItemBlock").each(function () {
                        var $this = $(this);
                        var offset = $this.offset();
                        var width = $this.outerWidth();
                        var height = $this.outerHeight();
                        var left = offset.left;
                        var right = left + width;
                        var top = offset.top;
                        var bottom = top + height;
                        if (mouseX >= left && mouseX <= right && mouseY >= top && mouseY <= bottom) {
                            foundElement = $this;
                            return false;
                        }
                    });
                    if (foundElement) {
                        foundElement.trigger("contextmenu");
                        return;
                    }
                }

                const driverName = $(this).data(self.__dataDriverName);
                const objectId = $(this).data(self.__dataObjectId);
                const settingsRecord = self.__settingsInfo.locomotives.find(it => it.driverName === driverName && it.objectId === objectId);

                //
                // Enter-/Leaving-Side
                //
                // #region Enter-/Leaving-Side
                let cssPlusIcon = '';
                let cssMinusIcon = '';
                if (settingsRecord.enterSide === "Plus") cssPlusIcon = 'fas fa-check';
                else if (settingsRecord.enterSide === "Minus") cssMinusIcon = 'fas fa-check';
                else cssPlusIcon = 'fas';
                // #endregion

                //
                // Locking
                //
                // #region Locking
                // <i class="fas fa-lock-open"></i>
                // <i class="fas fa-lock"></i>
                const cssLockLabel = settingsRecord.isLocked === true ? 'Lokomotive entsperren' : 'Lokomotive sperren';
                const cssLockIcon = settingsRecord.isLocked === true ? 'fas fa-lock' : 'fas fa-lock-open';;

                // #endregion

                new Contextual({
                    isSticky: false,
                    items: [
                        {
                            cssIcon: 'fas fa-play',
                            enabled: window.stateData.automaticEnabled,
                            label: 'Start',
                            onClick: () => {
                                window.occLayer.__stopCountdown(driverName, objectId);
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'startLocomotive',
                                        argumentValue: {
                                            driverName: driverName,
                                            objectId: objectId
                                        }
                                    });
                            }
                        },
                        {
                            cssIcon: 'fas fa-stop',
                            enabled: window.stateData.automaticEnabled,
                            label: 'Finalisieren',
                            onClick: () => {
                                window.occLayer.__stopCountdown(driverName, objectId);
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'finalizeLocomotive',
                                        argumentValue: {
                                            driverName: driverName,
                                            objectId: objectId
                                        }
                                    });
                            }
                        },
                        { type: 'seperator' },
                        {
                            cssIcon: cssPlusIcon,
                            label: 'Enter "+"',
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'setEnterSidePlus',
                                        argumentValue: {
                                            driverName: driverName,
                                            objectId: objectId
                                        }
                                    });
                            }
                        },
                        {
                            cssIcon: cssMinusIcon,
                            label: 'Enter "-"',
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'setEnterSideMinus',
                                        argumentValue: {
                                            driverName: driverName,
                                            objectId: objectId
                                        }
                                    });
                            }
                        },
                        { type: 'seperator' },
                        {
                            cssIcon: "",
                            label: "Aufstellung umdrehen",
                            onClick: () => {
                                self.__trigger("automode",
                                    {
                                        command: 'update',
                                        argument: 'changeOrientation',
                                        argumentValue: {
                                            driverName: driverName,
                                            objectId: objectId
                                        }
                                    });
                            }
                        },
                        { type: 'seperator' },
                        {
                            cssIcon: cssLockIcon,
                            label: cssLockLabel,
                            onClick: () => {
                                self.__trigger("automode", {
                                    command: 'update',
                                    argument: 'isLockedLocomotive',
                                    argumentValue: {
                                        driverName: driverName,
                                        objectId: objectId,
                                        state: !settingsRecord.isLocked
                                    }
                                });
                            }
                        },
                        { type: 'seperator' },
                        {
                            label: "Einstellungen",
                            onClick: () => {
                                const locomotiveRecord = window.locomotivesDlg?.getLocomotiveRecord(driverName, objectId);
                                const name = locomotiveRecord?.name ?? "Name unbekannt";
                                window.locomotivesDlg?.openProperties(driverName, objectId, name);
                            }
                        }
                    ]
                });
            });

        //
        // add draggable functionality
        //
        if (!locomotiveInfo.data("ui-draggable")) {
            locomotiveInfo.draggable({
                opacity: 0.7,
                helper: "clone",
                start: function (ev, ui) {
                    const ctrl = self.__getControlUnderMouse(ev);
                    if (ctrl) {
                        ctrl.css({
                            cursor: "dragged"
                        });
                        self.__dragStartBlockElement = ctrl;
                    }
                },
                stop: function (ev, ui) {
                    let reset = false;

                    try {

                        const targetEl = self.__getControlUnderMouse(ev);
                        if (!targetEl) throw "resetAssignment";
                        if (!targetEl.hasClass("ctrlItemBlock")) throw "resetAssignment";
                        if (targetEl.hasClass(self.__divBlockedClass)) throw "reset";

                        const startEl = self.__dragStartBlockElement;
                        const startId = startEl.attr("id");
                        const targetId = targetEl.attr("id");
                        if (startId === targetId) throw "reset";

                        const startElData = startEl.data(constDataThemeItemObject);
                        const targetElData = targetEl.data(constDataThemeItemObject);

                        if (!isBlock(targetElData.editor.themeId)) throw "reset";

                        //
                        // check if AutoMode is activated
                        //
                        if (!window.stateData.automaticEnabled) {
                            self.__showErrorPopup("AutoMode is disabled.", targetEl, 0, 0);
                            return;
                        }

                        self.__trigger("automode",
                            {
                                command: 'update',
                                argument: 'sendLocomotiveToBlock',
                                argumentValue: {
                                    driverName: driverName,
                                    objectId: objectId,
                                    startBlock: startEl.attr('id'),
                                    targetBlock: targetEl.attr('id')
                                }
                            });

                    } catch (err) {
                        if (err === "reset") {
                            reset = true;
                        } else if (err === "resetAssignment") {

                            locomotiveInfo.hide();
                            locomotiveInfoFinal.hide();

                            const driverName = locomotiveInfoFinal.data(self.__dataDriverName);
                            const objectId = locomotiveInfoFinal.data(self.__dataObjectId);

                            self.__trigger('automode',
                                {
                                    command: 'update',
                                    argument: 'removeLocomotiveFromBlock',
                                    argumentValue: {
                                        driverName: driverName,
                                        objectId: objectId
                                    }
                                });
                        }
                    } finally {
                        if (reset) {
                            // TBD
                        }

                        self.__dragStartBlockElement = null;
                    }
                }
            });
        }

        //
        // add locomotive control hover
        //
        // #region Locomotive Hover

        const tplLocControl = $('div[id=tplLocomotiveInfoCtrl]');
        var locControl = tplLocControl.find('div.locInfoCtrl').clone();
        locControl.css({ "display": "none" });
        locControl.attr("id", 'locCtrl_' + info.objectId);

        const driverName = info.driverName;
        const objectId = info.objectId;

        locControl.find('#locSpeedPlus').click(function () {
            self.__trigger('changed', {
                objectId: objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: driverName,
                    value: '++'
                }
            });
        });
        locControl.find('#locSpeedMinus').click(function () {
            self.__trigger('changed', {
                objectId: objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: driverName,
                    value: '--'
                }
            });
        });
        locControl.find('#locSpeedStop').click(function () {
            self.__trigger('changed', {
                objectId: objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: driverName,
                    value: 0
                }
            });
        });
        locControl.find('#locOpenDialog').click(function () {
            self.__trigger('openLocomotiveControl', {
                driverName: driverName,
                objectId: objectId
            });
        });
        locControl.find('#locBackward').click(function () {
            self.__trigger('changed', {
                objectId: objectId,
                command: 'update',
                argument: 'direction',
                argumentValue: {
                    driverName: driverName,
                    direction: 1
                }
            });
        });
        locControl.find('#locForward').click(function () {
            self.__trigger('changed', {
                objectId: objectId,
                command: 'update',
                argument: 'direction',
                argumentValue: {
                    driverName: driverName,
                    direction: 0
                }
            });
        });

        const allBtns = locControl
            .find('.locSpeedSteps')
            .find('button')
            .not("button[data-function]");

        allBtns.each(function () {
            $(this).button();
            if ($(this).data("speedstep")) {
                $(this).click(function (ev) {
                    const speedstep = $(ev.currentTarget).data("speedstep");

                    self.__trigger('changed', {
                        objectId: objectId,
                        command: 'update',
                        argument: 'speedstep',
                        argumentValue: {
                            driverName: driverName,
                            value: speedstep
                        }
                    });
                });
            }
        });

        const __fncShowLocHover = function (locinfo) {
            clearTimeout(self.__timeoutBeforeHide);
            self.__timeoutBeforeHide = 0;
            if (self.__timeoutBeforeShow > 0) return;
            if (self.__isLocomotiveControlHoverShown(this)) return;
            // hide all other controls
            $('.locInfoCtrl').each(function () { $(this).hide(); });
            self.__timeoutBeforeShow = setTimeout(function () {
                self.__showLocomotiveControlHover($(locinfo));
            }, 250);
        }

        const __fncHideLocHover = function (locinfo) {
            clearTimeout(self.__timeoutBeforeShow);
            self.__timeoutBeforeShow = 0;
            self.__timeoutBeforeHide = setTimeout(function () {
                self.__hideLocomotiveControlHover($(locinfo));
            }, 250);
        }

        locomotiveInfo.mouseover(function () {
            __fncShowLocHover(this);
        });
        locomotiveInfo.mouseleave(function () {
            __fncHideLocHover(this);
        });

        // #endregion

        const targetCtrl = $('#gleisplan_tabPlan1 .ries-container');
        //const bodyCtrl = $('body');
        locControl.appendTo(targetCtrl);
        locomotiveInfo.appendTo(targetCtrl);
        locomotiveInfoFinal.appendTo(targetCtrl);
    }

    /**
     * Checks if the locomotive control hover is currently visible.
     * @param {jQuery} locInfoCtrl - The locomotive info element.
     * @returns {boolean} True if the control hover is visible, false otherwise.
     */
    __isLocomotiveControlHoverShown(locInfoCtrl) {
        if (!locInfoCtrl) return;
        const oid = locInfoCtrl.data(this.__dataObjectId);
        return $('#locCtrl_' + oid).is(":visible");
    }

    /**
     * Displays the locomotive control hover near the given element.
     * @param {jQuery} locInfoCtrl - The locomotive info element.
     */
    __showLocomotiveControlHover(locInfoCtrl) {
        if (!locInfoCtrl) return;
        const oid = locInfoCtrl.data(this.__dataObjectId);
        const locController = $('#locCtrl_' + oid);

        if (!locController.length) return;

        locController.show();

        // Get positioning values
        const cc = locInfoCtrl.get(0);
        const { offsetLeft: left, offsetTop: top } = cc;
        const locInfoHeight = cc.getBoundingClientRect().height;
        const rect = locController.get(0).getBoundingClientRect();

        const offsetY = (rect.height - locInfoHeight) / 2;
        const ypos = Math.max(top - offsetY, 5); // Prevent negative positions

        locController.css({
            "z-index": 150,
            top: `${ypos}px`,
            left: `${left - rect.width + 1}px`
        });

        // Prevent hiding while hovering
        locController.off("mousemove mouseleave").on("mousemove", () => {
            clearTimeout(this.__timeoutBeforeHide);
            this.__timeoutBeforeHide = 0;
        }).on("mouseleave", () => {
            this.__timeoutBeforeHide = setTimeout(() => locController.hide(), 125);
        });
    }

    /**
     * Hides the locomotive control hover.
     * @param {jQuery} locInfoCtrl - The locomotive info element.
     */
    __hideLocomotiveControlHover(locInfoCtrl) {
        if (!locInfoCtrl) return;
        const oid = locInfoCtrl.data(this.__dataObjectId);
        $('#locCtrl_' + oid).hide();
    }

    /**
     * Apply layout adjustments to the infoBlock based on the position and size of the targetBlock.
     * This function recalculates the width and positioning of the infoBlock elements, making sure
     * that the label and countdown elements fit within the container.
     * 
     * @param {HTMLElement} infoBlock - The block to apply styles to.
     * @param {HTMLElement} targetBlock - The block from which position and size are derived.
     */
    __applyInfoToBlock(infoBlock, targetBlock) {
        // Early return if either infoBlock or targetBlock is not provided
        if (!infoBlock || !targetBlock) return;

        // Get the targetBlock's position and width only once for efficiency
        const ctrlPosition = targetBlock.position();
        const ctrlWidth = ctrlPosition.width;

        // Calculate top and left position for infoBlock with border padding
        const cssTop = (ctrlPosition.top + this.__borderPaddingTop) + "px";
        const cssLeft = (ctrlPosition.left + this.__borderPaddingLeft) + "px";

        // Set the width and max-width based on targetBlock's width
        const baseWidth = (ctrlWidth <= 64) ? 2 * constItemWidth : 4 * constItemWidth;
        const cssWidth = (baseWidth - this.__borderPaddingRight) + "px";
        const cssMaxWidth = cssWidth;

        // Apply styles to infoBlock
        infoBlock.css({
            "top": cssTop,
            "left": cssLeft,
            "width": cssWidth,
            "max-width": cssMaxWidth,
            "text-align": "left"
        });

        // Apply common width, max-width, and height to both label and countdown elements
        this.__applyElementStyle(infoBlock, "div.locInfoLabel", cssWidth, cssMaxWidth);
        //this.__applyElementStyle(infoBlock, "div.countdown", cssWidth, cssMaxWidth);
    }

    /**
     * Helper function to apply common styles to elements inside the infoBlock.
     * 
     * @param {HTMLElement} infoBlock - The parent block containing the element.
     * @param {string} selector - The CSS selector for the target element.
     * @param {string} cssWidth - The width to apply.
     * @param {string} cssMaxWidth - The max-width to apply.
     */
    __applyElementStyle(infoBlock, selector, cssWidth, cssMaxWidth) {
        const element = infoBlock.find(selector);
        if (element) {
            element.css({
                "width": cssWidth,
                "max-width": cssMaxWidth,
                "height": constItemHeight
            });
        }
    }

    /**
     * Updates the state visualization of a locomotive.
     * Optimized for performance and readability by reducing DOM queries
     * and avoiding redundant operations.
     *
     * @param {Object} locomotiveRecord - The locomotive data.
     * @param {Object} ids - The element identifiers.
     */
    __updateStateVisualization(locomotiveRecord, ids) {
        const ctrlLocomotiveInfo = $('#' + ids.root);
        if (!ctrlLocomotiveInfo.length) return;

        const ctrlSpeedInfo = ctrlLocomotiveInfo.find('.speedInfo');
        const ctrlEnter = ctrlLocomotiveInfo.find('.enterSide');
        const ctrlLock = ctrlLocomotiveInfo.find('.lockState');
        const ctrlStopped = ctrlLocomotiveInfo.find('.isStoppedState');
        const ctrlImage = ctrlLocomotiveInfo.find('img');
        const ctrlDirection = ctrlLocomotiveInfo.find('.locDirection');

        if (!locomotiveRecord) {
            ctrlSpeedInfo.html("-.-");
            return;
        }

        ctrlSpeedInfo.html(`${locomotiveRecord.speedstep} | ${locomotiveRecord.speedstepMax}`);
        if (locomotiveRecord.direction === 1)
            ctrlDirection.text("B");
        else if (locomotiveRecord.direction === 0)
            ctrlDirection.text("F");

        const { driverName, oid: objectId, speedstep, direction } = locomotiveRecord;
        const settingsRecord = this.__settingsInfo?.locomotives?.find(it => it.driverName === driverName && it.objectId === objectId);
        if (!settingsRecord) return;

        //
        // Handle Enter Side Indicator
        //
        if (ctrlEnter.length) {
            ctrlEnter.hide().removeAttr('style');
            if (settingsRecord.enterSide) {
                if (settingsRecord.enterSide === "None") {
                    ctrlEnter.hide();
                } else {
                    ctrlEnter.css({
                        "position": "absolute",
                        "top": "2px",
                        "font-size": "0.8em",
                        "transform": settingsRecord.enterSide === "Minus" ? "scaleX(1)" : "scaleX(-1)",
                        [settingsRecord.enterSide === "Plus" ? "left" : "right"]: "1px"
                    }).show();
                }
            }
        }

        if (ctrlImage) {
            if (settingsRecord.orientation) {
                let left = settingsRecord.orientation === "left";
                ctrlImage.css({
                    "transform": left === true ? "scaleX(1)" : "scaleX(-1)"
                });
            }
        }

        //
        // Handle Lock State
        //
        if (ctrlLock.length) {
            const isLocked = settingsRecord.isLocked;
            ctrlSpeedInfo.toggle(!isLocked);
            ctrlLock.toggle(isLocked);
            this.__fncScaleLbl(ctrlLocomotiveInfo, 0.70);
        }

        //
        // Handle Stop State
        //
        if (ctrlStopped.length) {
            ctrlStopped.toggle(speedstep === 0);
        }
    }

    __stopAllCountdowns() {
        $('.locomotiveCountdown').hide();
        for (const timer of this.__countdownTimers.values()) {
            clearInterval(timer);
        }
        this.__countdownTimers.clear();
    }

    __stopCountdown(driverName, objectId) {
        const timerId = `${driverName}_${objectId}`;
        const timer = this.__countdownTimers.get(timerId);
        if (timer) {
            clearInterval(timer);
            this.__countdownTimers.delete(timerId);
        }
    }

    __updateCountdownVisualization(locomotiveRecord, ids) {
        if (!window.stateData?.automaticEnabled) return;
        if (window.stateData.automaticEnabled === false) return;

        const ctrlLocomotiveInfo = $('#' + ids.root);
        if (!ctrlLocomotiveInfo.length) return;

        const ctrlCountdown = ctrlLocomotiveInfo.find('.locomotiveCountdown');
        if (!ctrlCountdown) return;

        const driverName = locomotiveRecord.driverName;
        const objectId = locomotiveRecord.oid;

        const settingsRecord = this.__settingsInfo?.locomotives?.find(
            it => it.driverName === driverName && it.objectId === objectId
        );
        if (!settingsRecord) return;

        if (settingsRecord.earlistTimeForNextTrip) {
            const targetTime = new Date(settingsRecord.earlistTimeForNextTrip).getTime();
            const myCountdownIdentifier = `${driverName}_${objectId}`;

            const startCountdown = (myCountdownIdentifier, targetTime, ctrlCountdown) => {
                if (this.__countdownTimers.has(myCountdownIdentifier)) {
                    //console.log(`Countdown für ${myCountdownIdentifier} läuft bereits.`);
                    return;
                }

                const updateCountdown = () => {
                    const now = Date.now();
                    const distance = Math.floor((targetTime - now) / 1000); // in Sekunden

                    if (distance > 0) {
                        if (ctrlCountdown.css("display") !== "flex") {
                            ctrlCountdown.css("display", "flex");
                        }
                        ctrlCountdown.find('div').text(`${distance}`);
                    } else {
                        ctrlCountdown.hide();
                        try {
                            clearInterval(timer);
                        } catch (err) {
                            // ignore
                        }
                        try {
                            this.__countdownTimers.delete(myCountdownIdentifier);
                        } catch (err) {
                            // ignore
                        }
                    }
                };

                updateCountdown();
                const timer = setInterval(updateCountdown, 1000);
                this.__countdownTimers.set(myCountdownIdentifier, timer);
            };

            const stopCountdown = (myCountdownIdentifier) => {
                const timer = this.__countdownTimers.get(myCountdownIdentifier);
                if (timer) {
                    clearInterval(timer);
                    this.__countdownTimers.delete(myCountdownIdentifier);
                }
            };
            if (Date.now() < targetTime) {
                startCountdown(myCountdownIdentifier, targetTime, ctrlCountdown);
            } else {
                ctrlCountdown.hide();
                stopCountdown(myCountdownIdentifier); // Stopp-Funktion verwenden
            }
        }
    }

    __fncScaleLbl(ctrl, factor) {
        try {
            const lbl = ctrl.find("div.locInfoLabel");
            if (!lbl.is(":visible") || lbl.width() === 0 || lbl.height() === 0) return;
            lbl.boxfit();
            const spanLbl = lbl.find('span');
            const currentFontSize = parseInt(spanLbl.css("font-size"), 10);
            if (!isNaN(currentFontSize)) {
                spanLbl.css("font-size", (currentFontSize * factor) + "px");
                ctrl.data("fontIsScaled", true);
            }
        } catch (err) {
            // Fehler ignorieren
        }
    }

    resetDestination(blockIdentifier) {
        const ctrlsFinal = $("[id^='locomotiveInfoFinal_']");
        ctrlsFinal.hide();
    }

    setDestination(data) {
        // data.blockIdentifier := string
        // data.locomotiveEntity := JObject
        const locomotiveRecord = data.locomotiveEntity;
        const ids = this.__generateLocomotiveInfoIdentifiers(locomotiveRecord.driverName, locomotiveRecord.objectId);
        const infoBlockFinal = $(`#${ids.rootFinal}`);
        const finalBlock = $(`#${data.blockIdentifier}`);

        //
        // check if final block is a stagingBox or staging block
        // staging areas do not highlight the locomotives directly
        // direct controlling is not supported
        // semi-automode controls the shifts
        //
        if (this.__isStaging(finalBlock)) {
            return;
        }

        if (finalBlock.length > 0)
            this.__applyInfoToBlock(infoBlockFinal, finalBlock);
        infoBlockFinal.show();
        this.__fncScaleLbl(infoBlockFinal, 0.90);
    }

    /**
     * Initializes the locomotive assignment visualizations.
     * They are already part of the DOM but moved outside the view.
     * @param {any} locomotives
     */
    loadLocomotives(locomotives) {
        if (locomotives) {
            locomotives.forEach((locInfo) => {
                this.__createLocomotiveInfo(locInfo);
            });
        }
    }

    updateLocomotives(railyDataLocomotives) {
        if (!railyDataLocomotives) return;

        const self = this;

        railyDataLocomotives.forEach((railyObj) => {
            if (!railyObj) return;

            const { objectId: oid, driverName } = railyObj;
            const locomotiveRecord = window.locomotivesDlg?.getLocomotiveRecord(driverName, oid);
            if (locomotiveRecord) {
                const ids = self.__generateLocomotiveInfoIdentifiers(driverName, oid);
                //
                // additional information are shown, e.g. speed, enter side, locking, ...
                //
                self.__updateStateVisualization(locomotiveRecord, ids);


            }
        });
    }

    updateSettings(settings) {
        if (!settings) return;
        this.__settingsInfo = settings;

        const self = this;

        //
        // update blocks with locomotive assignments
        //
        settings.locomotives.forEach(locItem => {
            if (locItem?.assignedToBlock && locItem.assignedToBlock.length > 0) {
                const locomotiveRecord = window.locomotivesDlg?.getLocomotiveRecord(locItem.driverName, locItem.objectId);
                if (!locomotiveRecord) {
                    if (window.__sidebarRailyData) {
                        const locomotivesData = window.__sidebarRailyData.locomotives;
                        if (locomotivesData) {
                            for (let i = 0; i < locomotivesData.length; ++i) {
                                const data = locomotivesData[i];
                                if (data) {
                                    if (data.driverName === locItem.driverName && data.objectId == locItem.objectId) {
                                        locomotiveRecord = data;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                //
                // Es gibt ein Assignment aber die dazugehörige Lokomotive wurde
                // in den bekannten Entities nicht gefunden. Diese Unstimmigkeit
                // sollte dem Anwender durch eine grafische Darstellung verdeutlicht werden.
                //
                if (!locomotiveRecord &&
                    (locItem?.assignedToBlock && locItem.assignedToBlock.length > 0)) {

                    const blockName = locItem?.assignedToBlock;
                    const ctrlBlock = $(`#${blockName}`);
                    if (ctrlBlock) {
                        const driverName = locItem.driverName;
                        const objectId = locItem.objectId;

                        ctrlBlock.find(".entity-warning").remove();

                        const warningIcon =
                            $(
                                '<i class="fa fa-exclamation-triangle" style="color: red; display: block; width: 18px; height: 16px; position: relative; top: -5px; left: 0px;"></i>')
                                .attr('title',
                                    `Lokomotive '${driverName}::${objectId}' zugewiesen, aber ControlStation '${driverName}' nicht verfügbar.`)
                                .appendTo(ctrlBlock);
                        warningIcon.addClass("entity-warning");

                        warningIcon.off('click')
                        warningIcon.on('click', () => {
                            showBlockRepairPrompt(blockName);
                        });

                    } else {
                        // TODO Was ist wenn es das Assignment gibt, aber den Block nicht mehr?
                    }
                }

                if (locomotiveRecord) {

                    const ids = self.__generateLocomotiveInfoIdentifiers(locomotiveRecord.driverName, locomotiveRecord.oid);
                    // the previously generated visualization
                    const infoBlock = $(`#${ids.root}`);
                    // the block which gets the locomotive assigned
                    const assignmentBlock = $(`#${locItem.assignedToBlock}`);
                    if (assignmentBlock.length > 0) {

                        //
                        // 
                        //
                        if (self.__isStaging(assignmentBlock)) {

                            assignmentBlock.removeClass('w3-yellow');

                            if (!self.__isStaging(infoBlock))
                                infoBlock.hide();
                            return;
                        }

                        self.__applyInfoToBlock(infoBlock, assignmentBlock);

                        infoBlock.show();

                        //
                        // additional information are shown, e.g. speed, enter side, locking, ...
                        //
                        self.__updateStateVisualization(locomotiveRecord, ids);

                        //
                        // add next run countdown
                        //
                        self.__updateCountdownVisualization(locomotiveRecord, ids);
                    }
                    this.__fncScaleLbl(infoBlock, 0.90);

                }
            }
        });
    }

    __isStaging(ctrl) {
        if (ctrl && ctrl.length > 0)
            return ctrl.hasClass('staging') || ctrl.hasClass('stagingBox');
        return false;
    }

    // #region Helper

    __showErrorPopup(message, targetEl, offsetX = 5, offsetY = 5) {
        const elPop = $('#routePopUp');
        let left = parseInt(targetEl.css("left").replace("px", ""));
        left -= offsetX;
        let top = parseInt(targetEl.css("top").replace("px", ""));
        top -= offsetY;
        elPop.css({
            "left": left + "px",
            "top": top + "px",
            "background-color": "rgba(255, 0, 0, 0.6)",
            "position": "absolute",
            "font-size": "10px",
            "font-weight": "bold",
            "border-radius": "5px",
            "padding": "2px",
            "color": "white"
        });
        elPop.addClass("noselect");
        elPop.html(message);
        elPop.fadeIn("fast");
        setTimeout(function () { elPop.fadeOut("slow"); }, 2000);
    }

    __getControlUnderMouse(event) {
        const planfieldElement = window.planfield?.planfieldElement?.get(0);

        if (!planfieldElement) {
            return null;
        }

        const x = event.clientX - planfieldElement.offsetLeft;
        const y = event.clientY - planfieldElement.offsetTop;

        const controls = $('div.ctrlItem[id]');
        return getCtrlOfPosition(x, y, controls);
    }

    // #endregion
}