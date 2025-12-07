// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

(function ($) {

    $.fn.hideMinimap = function () {
        return this.each(function () {
            const instance = $(this).data("gleisplan");
            if (instance?.$cfg.minimap) {
                instance.$cfg.minimap.hide();
            }
        });
    };

    $.fn.showMinimap = function () {
        return this.each(function () {
            const instance = $(this).data("gleisplan");
            if (instance?.$cfg.minimap) {
                instance.$cfg.minimap.show();
            }
        });
    };

    $.fn.getMinimap = function() {
        return this.each(function () {
            const instance = $(this).data("gleisplan");
            return instance?.$cfg.minimap;
        });
    }

    $.fn.getScrollOffset = function () {
        return this.each(function () {
            const instance = $(this).data("gleisplan");
            return instance?.$cfg.minimap;
        });
    }

    $.fn.gleisplan = function (options) {
        return this.each(function () {

            const $self = $(this);

            const instance = {
                $settings: $.extend(
                    {
                        noX: 96,
                        noY: 48,
                        cellWidth: 32,
                        cellHeight: 32,
                        minimapWidth: 160,
                        minimapHeight: 90,
                    },
                    options
                ),
                $cfg: {
                    containerRoot: $self,
                    containerRootId: $self.attr("id"),
                    container: null,
                    containerGrid: null,
                    minimap: null,
                    minimapContent: null,
                    minimapRectangle: null,
                },
                $runtime: {
                    isMinimapDragging: false,
                    startX: null,
                    startY: null,
                }
            };

            $self.data("gleisplan", instance);

            instance.$cfg.containerRoot = $(this);
            instance.$cfg.containerRootId = $(this).attr("id");
            instance.$cfg.containerRoot.addClass("ries-root");
            instance.$cfg.container = $("<div>").addClass("ries-container");
            instance.$cfg.container.appendTo(instance.$cfg.containerRoot);

            // Neues DIV mit ID "customerUi" als erstes Kind einfügen
            const customerUi = $('<div id="customerUi">').css({
                position: "absolute",
                top: 0,
                left: 0,
                width: "1px",
                height: "1px"
            });

            // Als erstes Child von container einfügen
            instance.$cfg.container.prepend(customerUi);

            instance.$cfg.containerGrid = $("<div>").addClass("ries-grid").addClass("noselect");
            instance.$cfg.container.append(instance.$cfg.containerGrid);
            instance.$cfg.container.addClass("ries-container");

            this.__mousemove = (e) => {
                if (instance.$runtime?.isMinimapDragging) {

                    const dx = e.pageX - instance.$runtime.startX;
                    const dy = e.pageY - instance.$runtime.startY;

                    let newLeft = instance.$cfg.minimapRectangle.position().left + dx;
                    let newTop = instance.$cfg.minimapRectangle.position().top + dy;

                    // Begrenzungen für das Rechteck in der Minimap
                    const minimapWidth = instance.$cfg.minimap.width();
                    const minimapHeight = instance.$cfg.minimap.height();
                    const rectWidth = instance.$cfg.minimapRectangle.width();
                    const rectHeight = instance.$cfg.minimapRectangle.height();

                    // Berechne die neuen Positionen und stelle sicher, dass das Rechteck nicht über den Rand geht
                    newLeft = Math.max(0, Math.min(newLeft, minimapWidth - rectWidth));
                    newTop = Math.max(0, Math.min(newTop, minimapHeight - rectHeight));

                    instance.$cfg.minimapRectangle.css({ left: newLeft, top: newTop });

                    // Berechne den Anteil der Position in der Minimap (zwischen 0 und 1)
                    const ratioX = newLeft / (minimapWidth - rectWidth);
                    const ratioY = newTop / (minimapHeight - rectHeight);

                    // Multipliziere das Verhältnis mit der Gesamtbreite des Inhalts, um die Scroll-Position zu berechnen
                    const scrollX = ratioX * (instance.$cfg.containerGrid.width() - instance.$cfg.container.width());
                    const scrollY = ratioY * (instance.$cfg.containerGrid.height() - instance.$cfg.container.height());

                    instance.$cfg.container.scrollLeft(scrollX);
                    instance.$cfg.container.scrollTop(scrollY);

                    instance.$runtime.startX = e.pageX;
                    instance.$runtime.startY = e.pageY;
                }
            };

            this.__mouseup = () => {
                instance.$runtime.isMinimapDragging = false;
            };

            this.__zoom = (ev) => {
                if (ev.ctrlKey) {
                    ev.preventDefault()
                }
            };

            this.__getRandomColor = () => {
                return "rgba(210, 220, 230, 0.6)";

            //    const r = Math.floor(Math.random() * 256); // 0-255
            //    const g = Math.floor(Math.random() * 256); // 0-255
            //    const b = Math.floor(Math.random() * 256); // 0-255
            //    return `rgba(${r}, ${g}, ${b}, 0.7)`;
            };

            this.__createMinimap = () => {
                instance.$cfg.minimap = $("<div>")
                    .addClass("minimap-instance")
                    .attr("id", "minimap_" + instance.$cfg.containerRootId)
                    .css({
                        position: "absolute",
                        bottom: "40px",
                        left: `60px`,
                        width: `${instance.$settings.minimapWidth}px`,
                        height: `${instance.$settings.minimapHeight}px`
                        //border: "2px solid #000",
                        //backgroundColor: this.__getRandomColor(),
                    })
                    .appendTo(instance.$cfg.containerRoot);

                instance.$cfg.minimapContent = $("<div>")
                    .addClass("minimap-content")
                    .appendTo(instance.$cfg.minimap);
                instance.$cfg.minimapRectangle = $("<div>")
                    .addClass("minimap-rectangle")
                    .appendTo(instance.$cfg.minimapContent);

                instance.$cfg.minimapRectangle.css({
                    position: "absolute",
                    backgroundColor: "red",
                    opacity: 0.5,
                    borderRadius: "2px",
                    width: "64px",
                    height: "36px",
                });

                instance.$cfg.minimapRectangle.mousedown(function (e) {
                    instance.$runtime.isMinimapDragging = true;
                    instance.$runtime.startX = e.pageX;
                    instance.$runtime.startY = e.pageY;
                });

                $(instance.$cfg.minimapContent).on("mousemove", this.__mousemove);
                $(instance.$cfg.minimapContent).on("mouseup", this.__mouseup);
                instance.$cfg.container.on("mouseup", this.__mouseup);
                instance.$cfg.container.on("wheel", this.__zoom);
            }; // __createMinimap()

            this.__updateMinimapRectangle = () => {
                const parentWidth = instance.$cfg.container.width();
                const parentHeight = instance.$cfg.container.height();
                const childWidth = instance.$cfg.containerGrid.width();
                const childHeight = instance.$cfg.containerGrid.height();

                let scale = instance.$cfg.containerGrid.data("zoom") || 1;

                const ratioX = parentWidth / (childWidth * scale);
                const ratioY = parentHeight / (childHeight * scale);

                let left =
                    (instance.$cfg.container.scrollLeft() / (childWidth * scale - parentWidth)) *
                    instance.$cfg.minimap.width();
                let top =
                    (instance.$cfg.container.scrollTop() / (childHeight * scale - parentHeight)) *
                    instance.$cfg.minimap.height();

                instance.$cfg.minimapRectangle.css({
                    left: left,
                    top: top,
                    //width: instance.$cfg.minimap.width() * ratioX,
                    //height: instance.$cfg.minimap.height() * ratioY,
                });
            };

            this.__applyConstValues = () => {
                instance.$cfg.containerGrid.css({
                    "grid-template-columns": `repeat(${instance.$settings.noX}, ${instance.$settings.cellWidth}px)`,
                    "grid-template-rows": `repeat(${instance.$settings.noY}, ${instance.$settings.cellHeight}px)`,
                    width: `${instance.$settings.noX * instance.$settings.cellWidth}px`,
                    height: `${instance.$settings.noY * instance.$settings.cellHeight}px`,
                    backgroundImage: "", // search for `toggleGrid`
                    backgroundSize: `${instance.$settings.cellWidth}px ${instance.$settings.cellHeight}px`,
                }).addClass("gleisplanBg");
            };

            this.__createMinimap();
            this.__applyConstValues();
            this.__updateMinimapRectangle();

            return instance.$cfg.containerRoot;
        });
    };
})(jQuery);;