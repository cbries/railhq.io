// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class Toolbox {
    constructor(selector) {
        console.log(`%c**** Construct ${this.constructor.name}`, "color: blue; font-weight: bold;");

        this.__installed = false;
        this.__dialogName = "toolboxPopup2";
        this.__storageNameGeometry = this.__dialogName + "_geometry";
        this.__windowGeometry = new WindowGeometryStorage(this.__dialogName);
        this.__toolbox = $('#toolbox');
        this.recentItem = null;

        this.isDragging = false;
        this.isResizing = false;
        this.offsetX = 0;
        this.offsetY = 0;

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

    showToolbox() {
        if (this.isShown()) return;
        if (!this.__installed) this.install();
        const el = $('#' + this.__dialogName);
        if (el) {
            this.__windowGeometry.showWithGeometry(el);
            el.dialog("open");
        }
    }

    isShown() {
        try {
            return $('#' + this.__dialogName).dialog('isOpen');
        } catch (error) {
            // ignore
        }
        return false;
    }

    hideToolbox() {
        const dialogElement = $("#" + this.__dialogName);
        dialogElement.dialog('close');
    }

    rotate(data) {
        const $self = this;
        const ctrl = $("div.toolboxItem").filterByData(constDataThemeId, data.themeId);
        if (typeof ctrl === "undefined") return;
        if (ctrl == null) return;

        const ctrlImg = ctrl.find("img");
        if (typeof ctrlImg === "undefined") return;
        if (ctrlImg == null) return;

        ctrl.data(constDataThemeDimensionIndex, data.themeDimIdx);

        ctrlImg.css({
            "transform-origin": data.transformOrigin,
            "transform": data.transform
        });

        const jsonObj = $self.__getJsonDataOfItem(ctrl);
        ctrl.data(constDataThemeItemObject, jsonObj);
        $self.recentItem = jsonObj;
    }

    __getJsonDataOfItem(toolboxItem) {
        const jsonObj = toolboxItem.data(constDataThemeItemObject);
        if (typeof jsonObj.editor === "undefined")
            jsonObj.editor = {};
        jsonObj.editor.themeId = toolboxItem.data(constDataThemeId);
        jsonObj.editor.themeDimIdx = toolboxItem.data(constDataThemeDimensionIndex);
        return jsonObj;
    }

    __collapseCategory(headTitle) {
        headTitle.data("railway-collapsed", "");
        headTitle.parent().find("div.toolboxItem").each(function () {
            $(this).hide();
        });
        const img = headTitle.find('img');
        img.attr("src", "images/expand.png");
    }

    __expandCategory(headTitle) {
        headTitle.removeData("railway-collapsed");
        headTitle.parent().find("div.toolboxItem").each(function () {
            $(this).show();
        });
        const img = headTitle.find('img');
        img.attr("src", "images/collapse.png");
    }

    install() {
        if (this.__installed) return;
        this.__installed = true;

        const self = this;
        const state = this.isShown();
        if (state) return;

        const geometry = this.__windowGeometry.recent();
        const dialogElement = $("#" + this.__dialogName);

        if (!geometry.width) geometry.width = 190;
        if (!geometry.height) geometry.height = 390;

        dialogElement.dialog({
            height: geometry.height,
            width: geometry.width,
            left: geometry.left,
            top: geometry.top,
            closeOnEscape: true,
            autoOpen: false,
            resizeStop(event, ui) {
                self.__windowGeometry.save(ui.position, ui.size);
            },
            dragStop(event, ui) {
                self.__windowGeometry.save(ui.position, {
                    width: event.target.clientWidth,
                    height: event.target.clientHeight
                });
            },
            close() {
                self.__trigger("close", null);
            }
        });

        window.themeData.forEach(item => {
            var headItem = $('<div>', {}).css({})
                .addClass("toolboxHead")
                .appendTo(dialogElement.find('.toolboxEntries'));

            var headTitle = $('<div>').addClass("toolboxTitle");
            headTitle.html(item.category);

            headTitle.click(function () {
                var collapsed = headTitle.data("railway-collapsed");
                if (typeof collapsed === "undefined" && collapsed == null)
                    collapsed = false;
                else
                    collapsed = true;
                if (collapsed) {
                    self.__expandCategory(headTitle);
                } else {
                    self.__collapseCategory(headTitle);
                }
            });

            var imgExpandCollapse = $('<img>');
            imgExpandCollapse.appendTo(headTitle);
            headTitle.appendTo(headItem);

            var itemContainer = $('<div>').appendTo(headItem);
            itemContainer.addClass("toolboxContainer");

            item.objects.forEach(obj => {
                // Skip invalid or invisible objects
                if (typeof obj.visible !== "undefined" && obj.visible === false) return;

                // Create draggable item
                const toolboxItem = $('<div>', { html: "" })
                    .addClass("toolboxItem")
                    .attr({ draggable: "true" });

                // Attach data attributes
                toolboxItem.data(constDataThemeId, obj.id);
                toolboxItem.data(constDataThemeItemObject, obj);

                // Append item to container
                toolboxItem.appendTo(itemContainer);

                let extraInfo = "W: 1, H: 1";
                if (obj.dimensions && obj.dimensions.length > 0) {
                    const dim0 = obj.dimensions[0];
                    const w = dim0.w;
                    const h = dim0.h;
                    extraInfo = `W: ${w}, H: ${h}`;
                }

                // Create item icon
                const iconPath = `theme/${window.themeName}/${obj.basename}.png`;
                const itemIcon = $('<img>')
                    .addClass("imgWithTooltip")
                    .attr({
                        "data-tipso-title": `${obj.name}<br>(${extraInfo})`,
                        draggable: "false",
                        src: iconPath
                    });

                //
                // change the size of the controls in the toolbox
                //
                // TODO the size is not really nice in the moment
                //
                //let rotIndex = toolboxItem.data(constDataThemeDimensionIndex);
                //if (!rotIndex) rotIndex = 0;
                //let ctrlDimension = null;
                //const dimensions = obj.dimensions;
                //if (dimensions.length > 0) {
                //    ctrlDimension = dimensions[rotIndex];
                //}
                //if (ctrlDimension) {
                //    const ww = ctrlDimension.w * 24;
                //    const hh = ctrlDimension.h * 24;
                //    itemIcon.css({
                //        width: ww + "px",
                //        height: hh + "px"
                //    });
                //}

                // Append icon to item
                itemIcon.appendTo(toolboxItem);
            });

            // expand/collapse categories
            if (item.category === "Signal" || item.category === "Block") {
                this.__collapseCategory(headTitle);
            } else {
                this.__expandCategory(headTitle);
            }
        });

        const toolboxItems = $('div.toolboxItem');
        for (let idx = 0; idx < toolboxItems.length; ++idx) {
            const item = toolboxItems[idx];
            item.ondragstart = (ev) => {
                const toolboxItem = $(ev.target);
                const jsonObj = self.__getJsonDataOfItem(toolboxItem);
                jsonObj.editor.offsetX = ev.offsetX;
                jsonObj.editor.offsetY = ev.offsetY;
                toolboxItem.data(constDataThemeItemObject, jsonObj);
                const data = JSON.stringify(jsonObj);
                ev.dataTransfer.setData("text/plain", data);
                self.recentItem = jsonObj;

                if (window.planfield) {
                    window.planfield.clearCtrlSelection();
                }
            };
        }

        $('.imgWithTooltip').tipso({
            size: 'tiny',
            speed: 100,
            delay: 500,
            useTitle: true,
            width: "auto",
            background: '#333333',
            titleBackground: '#333333'
        });
    }
}