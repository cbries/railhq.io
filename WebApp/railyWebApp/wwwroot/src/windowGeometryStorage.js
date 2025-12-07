// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class WindowGeometryStorage {

    constructor(name, defaultGeometry = {
        left: 100,
        top: 100,
        width: 450,
        height: 500
    }) {
        this.__fieldName = name;
        this.__defaultGeometry = defaultGeometry;
    }

    save(position, size) {
        const geometry = {
            left: position.left,
            top: position.top,
            width: size.width,
            height: size.height
        }
        $.localStorage.setItem(this.__fieldName, JSON.stringify(geometry));
    }

    recent() {
        const geometry = $.localStorage.getItem(this.__fieldName);
        if (!geometry) {
            return this.__defaultGeometry;
        }
        try {
            return JSON.parse(geometry);
        } catch (e) {
            console.error("Failed to parse geometry data:", e);
            return this.__defaultGeometry;
        }
    }
    
    showWithGeometry(jqueryEl, useSize = true) {
        const { height, width, left, top } = this.recent();

        if (useSize) {
            jqueryEl.dialog("option", "height", height);
            jqueryEl.dialog("option", "width", width);
        }

        jqueryEl.dialog("option", "position", {
            my: "left top",
            at: `left+${left} top+${top}`,
            of: window
        });
    }

}

