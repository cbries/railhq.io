// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


// #region Nachfolgend der Teil für die StateMachine zum Anzeigen des Ladevorgangs.

// Schritt-Konstanten
const STEP_CONTROLSTATION = "ControlStation einrichten";
const STEP_LOKOMOTIVEN = "Lokomotiven hinzufügen";
const STEP_SCHALTARTIKEL = "Schaltartikel konfigurieren";
const STEP_THEMING = "Design & Layout anpassen";
const STEP_GLEISPLAN = "Gleisplan erstellen";
const STEP_RUECKMELDEBAUSTEINE = "Rückmeldebausteine einrichten";
const STEP_BLOCKINFORMATIONEN = "Blockinformationen festlegen";
const STEP_ROUTEINFORMATIONEN = "Routen konfigurieren";
const STEP_ALLGEMEINE_EINSTELLUNGEN = "Allgemeine Einstellungen anpassen";
const STEP_REKONSTRUKTION_SITZUNG = "Vorherige Sitzung wiederherstellen";

function initLoadingStateMachine() {
    if (window.loadingMachine) return;
    window.loadingMachine = loadingMachine = $("#loading-overlay").loadingStateMachine();
    window.loadingMachine.startLoadingStateMachine();
}

function applyLoadingState(stepName) {
    if (!window.loadingMachine) return;
    window.loadingMachine.applyLoaded(stepName);
}

(function ($) {
    const steps = [
        STEP_CONTROLSTATION,
        STEP_LOKOMOTIVEN,
        STEP_SCHALTARTIKEL,
        STEP_THEMING,
        STEP_GLEISPLAN,
        STEP_RUECKMELDEBAUSTEINE,
        STEP_BLOCKINFORMATIONEN,
        STEP_ROUTEINFORMATIONEN,
        STEP_ALLGEMEINE_EINSTELLUNGEN,
        STEP_REKONSTRUKTION_SITZUNG
    ];
    const loadingTimeoutSecs = 20;
    const loadingTimeout = loadingTimeoutSecs * 1000;
    $.fn.loadingStateMachine = function () {

        const $overlay = $(this);
        let stepStates = {};
        let totalSteps = steps.length;
        steps.forEach((step, index) => {
            const stepElement = `<li><div class="step-circle step-${index}"></div><span>${step}</span></li>`;
            $(".loading-steps").append(stepElement);
            stepStates[step] = false;
        });
        const updateProgress = () => {
            const completedSteps = Object.values(stepStates).filter(state => state === true).length;
            const progress = (completedSteps / totalSteps) * 100;
            $(".progress-bar").css("width", progress + "%");
        };
        const setStateReady = (name) => {
            if (stepStates.hasOwnProperty(name)) {
                stepStates[name] = true;
                const stepIndex = steps.indexOf(name);
                $(`.step-${stepIndex}`).addClass("step-completed").html('✔');
                updateProgress();
            }
        };
        setTimeout(() => {
            if (Object.values(stepStates).some(state => state === false)) {
                $overlay.fadeOut(400, function () {
                    $(this).off().remove();
                });

                showWarning("Die Initialisierung der Steuerung wurde nach 10 Sekunden abgebrochen. "
                    + "Einige Daten konnten nicht geladen werden, was zu Einschränkungen führen kann. "
                    + "Bitte überprüfe die Verbindung oder starte die Initialisierung erneut. "
                    + "Überprüfe bitte auch die Anmeldedaten deines <i>railhq.io - Gateway</i>.", 10);
                
            }
        }, loadingTimeout);
        const interval = setInterval(() => {
            if (Object.values(stepStates).every(state => state === true)) {
                clearInterval(interval);
                setTimeout(() => {
                    $overlay.fadeOut(400, function () {
                        $(this).off().remove();                        
                    });
                }, 250);
            }
        }, 250);
        return {
            startLoadingStateMachine: () => {
                //$overlay.css("visibility", "visible");
            },
            applyLoaded: (name) => {
                setStateReady(name);
            },
            isFinished: () => {
                const completedSteps = Object.values(stepStates).filter(state => state === true).length;
                return completedSteps == totalSteps;
            }
        };
    };

})(jQuery);

// #endregion

function getCleanW2uiIdentifier(txt) {
    txt = txt.replace("-", "_");
    txt = txt.replace("+", "_");
    txt = txt.replace(":", "_");
    return txt;
}

function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function bringToFront(dialogName) {
    const allDialogs = $('.ui-dialog');
    let targetDialog = null;
    let highestZIndex = 0;

    allDialogs.each(function () {
        const dialog = $(this);

        // Get current z-index, default to 0 if invalid
        const zIndex = parseInt(dialog.css("z-index")) || 0;

        // Update highest z-index
        if (zIndex > highestZIndex) {
            highestZIndex = zIndex;
        }

        // Match dialog by aria-describedby or originalTitle
        const ariaDescribedBy = dialog.attr("aria-describedby");
        const dialogTitle = dialog.data("ui-dialog")?.originalTitle;
        if (ariaDescribedBy === dialogName || dialogTitle === dialogName) {
            targetDialog = dialog;
        }
    });

    // Bring target dialog to front
    if (targetDialog) {
        targetDialog.css("z-index", highestZIndex + 10);
    }
}

function getWidthOfText(txt, fontname, fontsize) {
    if (getWidthOfText.c === undefined) {
        getWidthOfText.c = document.createElement('canvas');
        getWidthOfText.ctx = getWidthOfText.c.getContext('2d');
    }
    var fontspec = fontsize + ' ' + fontname;
    if (getWidthOfText.ctx.font !== fontspec)
        getWidthOfText.ctx.font = fontspec;
    return getWidthOfText.ctx.measureText(txt).width;
}

function addJqueryExtensions() {
    $.fn.filterByData = function (prop, val) {
        return this.filter(
            function () { return $(this).data(prop) == val; }
        );
    }
}

/**
 * Formats an ISO date string into "Heute, HH:mm:ss" if it's today's date,
 * or "dd.MM.yyyy, HH:mm:ss" otherwise.
 *
 * @param {string} isoString - The ISO date string to format (e.g., '2025-01-25T08:08:26.7405632+01:00').
 * @returns {string} The formatted date string.
 * @example
 * const formatted = formatDateFromISO('2025-01-25T08:08:26.7405632+01:00');
 * console.log(formatted); // Output: "Heute, 08:08:26" (if the date is today)
 *                         // or "25.01.2025, 08:08:26" (if not today)
 */
function formatDateFromISO(isoString) {
    const date = new Date(isoString);
    const now = new Date();

    // Helper function to ensure two-digit formatting with leading zeros
    const padZero = (value) => String(value).padStart(2, '0');

    // Check if the date is today's date
    const isToday =
        date.getDate() === now.getDate() &&
        date.getMonth() === now.getMonth() &&
        date.getFullYear() === now.getFullYear();

    // Extract time components
    const hours = padZero(date.getHours());
    const minutes = padZero(date.getMinutes());
    const seconds = padZero(date.getSeconds());

    if (isToday) {
        return `Today, ${hours}:${minutes}:${seconds}`;
    }

    // Extract date components for non-today dates
    const day = padZero(date.getDate());
    const month = padZero(date.getMonth() + 1);
    const year = date.getFullYear();

    return `${day}.${month}.${year}, ${hours}:${minutes}:${seconds}`;
}

//If you write your own code, remember hex color shortcuts (eg., #fff, #000)
/*
returned value: (String)
rgba(251,175,255,1)
hexToRgbA('#fbafff')
*/
function hexToRgbA(topic, hex) {
    if (hex.charAt(0) !== '#')
        hex = '#' + hex;
    var c;
    if (/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) {
        c = hex.substring(1).split('');
        if (c.length === 3) {
            c = [c[0], c[0], c[1], c[1], c[2], c[2]];
        }
        c = '0x' + c.join('');

        var r = topic + 'R';
        var g = topic + 'G';
        var b = topic + 'B';
        var w = topic + 'W';

        var o = {};
        o[r] = (c >> 16) & 255;
        o[g] = (c >> 8) & 255;
        o[b] = c & 255;
        o[w] = 1023;

        return o;
    }
    throw new Error('Bad Hex');
}

function toggleAllLocomotiveInformation(state) {
    const infos = $('div.locomotiveInfo');
    let i;
    const iMax = infos.length;
    for (i = 0; i < iMax; ++i) {
        if (state === true) {
            $(infos[i]).show();
        } else {
            $(infos[i]).hide();
        }
    }
}

function toggleAllLocomotiveInformationOpacity(state) {
    const infos = $('div.locomotiveInfo');
    let i;
    const iMax = infos.length;
    for (i = 0; i < iMax; ++i) {
        if (state === true) {
            $(infos[i]).css({ opacity: "1.0" });
        } else {
            $(infos[i]).css({ opacity: "0.2" });
        }
    }
}

function toggleAllLabelInformation(state) {
    $.localStorage.setItem("labelShown", JSON.stringify({ shown: state }));
    $('div.elementLabel').toggle(state);
}

/**
 * Of
 *   "../../hello/world/ries.jpg"
 * it will return:
 *   "../../hello/world/"
 * @param {any} pathToFile
 */
function getDirpathOf(pathToFile) {
    if (!pathToFile) return '';
    const lastIdx = pathToFile.replace("\\", "/").lastIndexOf('/');
    const dirpath = pathToFile.substr(0, lastIdx + 1);
    return dirpath;
}

function getLabelOffsetBy(jsonData) {
    const themeId = jsonData.editor.themeId;
    const offset = {
        "font-size": "7px",
        "z-index": 10000,
        "position": "relative",
        //"top": "-12px",
        "left": "0px"
    };

    switch (themeId) {
        case 10: return null;
        case 11: return null;
        case 13: return null;
        case 14: return null;
        case 17: return null;
        case 18: return null;
        case 19: return null;
        case 1013: return null;
    }

    return offset;
}

var _htmlEntities = (function (str) {
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
});

window.__cachedLocomotiveImages = {};

function loadLocomotiveImageIntoHtml(targetImageId, locname, options =
    {
        percentage: "100%",
        uid: ""
    }) {
    if (typeof window.__cachedLocomotiveImages[locname] !== "undefined" &&
        window.__cachedLocomotiveImages[locname] != null) {
        const targetImg = $('img#' + targetImageId);
        if (targetImg.length > 0)
            targetImg.get(0).src = window.__cachedLocomotiveImages[locname];
    }

    const newImg = new Image;
    if (typeof options?.percentage === "undefined" || options?.percentage == null)
        options.percentage = "100%";

    newImg.onload = function () {
        try {
            const targetImg = $('img#' + targetImageId);
            targetImg.get(0).src = this.src;
            targetImg.css({
                width: options?.percentage
            });
            window.__cachedLocomotiveImages[locname] = this.src;
        } catch (err) {
            console.log("Image load failed: " + err)
        }
    }
    locname = locname.replace("/", "_");
    locname = locname.replace(".", "_");
    let url = `${window.__httpProtocol}://${window.__urlHq}:${window.__urlPortHq}/api/locimg/file/${locname}`;
    if (options.uid && options.uid.length > 0)
        url += `?uid=${options.uid}`;
    newImg.src = url;
}

/**
 * Generates a unique item identifier based on the provided themeId and existing IDs.
 * 
 * The function first determines a prefix for the ID based on the themeId:
 * - Theme IDs between 10-25 use the prefix "TK"
 * - Theme IDs between 50-75 use the prefix "SW"
 * - Theme IDs between 100-125 use the prefix "SE"
 * - Theme IDs between 150-175 use the prefix "BK"
 * - Theme IDs between 200-225 use the prefix "FB"
 * - Otherwise, the default prefix is "Item".
 * 
 * It then iterates through potential suffixes (starting from 0) and checks if the generated ID
 * is already in use. The first unused ID is returned.
 * 
 * @param {number} themeId - The theme identifier to determine the prefix.
 * @returns {string} A unique identifier for the item.
 */
function generateUniqueItemIdentifier(themeId) {
    const idArray = $('div.ctrlItem[id]')
        .map(function () { return this.id; })
        .get();

    let newIdPrefix = "Item";

    if (themeId) {
        if (themeId >= 10 && themeId <= 25) newIdPrefix = "Track";
        else if (themeId >= 50 && themeId <= 75) newIdPrefix = "Switch";
        else if (themeId >= 100 && themeId <= 125) newIdPrefix = "Signal";
        else if (themeId >= 150 && themeId <= 175) newIdPrefix = "Block";
        else if (themeId >= 200 && themeId <= 225) newIdPrefix = "Sensor";
    }

    const idArraySet = new Set(idArray); // Convert array to a Set for faster lookups

    for (let i = 0; ; i++) {
        const candidateId = `${newIdPrefix}_${i}`;
        if (!idArraySet.has(candidateId)) {
            return candidateId;
        }
    }
}

/**
 * Queries and returns the control under coord(x, y).
 * @param x x-coord to check for a ctrl 
 * @param y y-coord to check for a ctrl
 * @param listOfCtrls The list of controls in which the search is done.
 */
function getCtrlOfPosition(x, y, listOfCtrls) {
    if (x == null) return null;
    if (y == null) return null;
    if (listOfCtrls == null) return null;
    let i;
    const iMax = listOfCtrls.length;
    const planfieldRect = $('#gleisplan_tabPlan1')[0].getBoundingClientRect();

    for (i = 0; i < iMax; ++i) {
        const c = listOfCtrls[i];
        if (typeof c === "undefined" || c === null) continue;
        const clientRect = c.getBoundingClientRect();
        const yStart = clientRect.y - planfieldRect.y;
        const xStart = clientRect.x - planfieldRect.x;
        const w = clientRect.width;
        const h = clientRect.height;
        const xEnd = xStart + w;
        const yEnd = yStart + h;
        if (x >= xStart && x <= xEnd
            && y >= yStart && y <= yEnd) {
            return $(c);
        }
    }
    return null;
}

function getCtrlOfCoord(x, y, listOfCtrls) {
    if (x == null || y == null || listOfCtrls == null) return null;
    if (listOfCtrls.length === 0) return null;

    const ctrlsArray = Array.isArray(listOfCtrls) ? listOfCtrls : $(listOfCtrls).toArray();

    for (const ctrl of ctrlsArray) {
        const c0 = $(ctrl);
        const c0data = c0.data(constDataThemeItemObject);
        const coord = c0data?.coord;

        if (coord?.x === x && coord?.y === y) {
            return c0;
        }
    }

    return null;
}

/**
 * Queries and returns the control under the ev position.
 * @param ctx jQuery element used to calculate the relative position.
 * @param ev Mostly a mouse event. Web reference: https://api.jquery.com/Types/#Event
 * @param listOfCtrls The list of controls in which the search is done.
 */
function getCtrlOfEvent(ctx, ev, listOfCtrls) {
    const rect = ctx.get(0).getBoundingClientRect();
    const scrollTop = window.scrollY || document.documentElement.scrollTop;
    const scrollLeft = window.scrollX || document.documentElement.scrollLeft;

    return getCtrlOfPosition(
        parseInt(ev.pageX - rect.left + scrollLeft),
        parseInt(ev.pageY - rect.top + scrollTop),
        listOfCtrls
    );
}

/**
 * Returns the JSON {"x": .., "y": ..} coord of the click.
 * Thats not the mouse coord, it is a index-based coord, e.g.
 * {"x":0,"y":0} := first field
 * {"x":5,"y":2} := field of column 6 and row 3
 * The field size is defined by the global constants:
 *   constItemWidth
 *   constItemHeight
 * @param ctx jQuery element of the click area.
 * @param e mouse event, e.g. document.onmousemove
 */
function getElementCoord(ctx, e) {
    try {
        const c = ctx.get(0);
        const ctxPosY = c.offsetTop;
        const ctxPosX = c.offsetLeft;
        const x = e.pageX - ctxPosX;
        const y = e.pageY - ctxPosY;
        var coordX = (x / constItemWidth);
        var coordY = (y / constItemHeight);
        coordX = Math.floor(coordX);
        coordY = Math.floor(coordY);
        if (coordX < 0) coordX = 0;
        if (coordY < 0) coordY = 0;
        return { x: coordX, y: coordY };
    } catch (err) {
        console.error(err);
    }

    return { x: 0, y: 0 };
}

function getElementStartCoord(ctx, xx, yy) {
    const ctxPosX = ctx.get(0).offsetLeft;
    const ctxPosY = ctx.get(0).offsetTop;
    const x = xx - ctxPosX;
    const y = yy - ctxPosY;
    var coordX = (x / constItemWidth);
    var coordY = (y / constItemHeight);
    coordX = Math.floor(coordX);
    coordY = Math.floor(coordY);
    if (coordX < 0) coordX = 0;
    if (coordY < 0) coordY = 0;
    return { x: coordX, y: coordY };
}

/**
 * Calculates the pixel coordinates of the index-based coord
 * returned be getElementCoord()
 * @param {any} coord
 * @see getElementCoord
 */
function coord2pixel(coord) {
    if (coord == null) return;
    return {
        x: coord.x * constItemWidth,
        y: coord.y * constItemHeight
    };
}

function isButton(themeId) {
    if (themeId === 71) return true;
    if (themeId === 72) return true;
    if (themeId === 73) return true;
    return false;
}

function isSignal(themeId) {
    if (!themeId) return false;
    if (themeId >= 100 && themeId <= 125)
        return true;
    return false;
}

function isSwitch(themeId) {
    if (!themeId) return false;
    if (themeId === 50) return true;
    if (themeId === 51) return true;
    if (themeId === 52) return true;
    if (themeId === 53) return true;
    if (themeId === 58) return true;
    if (themeId === 59) return true;
    return false;
}

function isSwitchOrAccessory(themeId) {
    if (!themeId) return false;
    if (themeId >= 50 && themeId < 150
        && (themeId !== 54 && themeId !== 55))
        return true;
    return false;
}

function isAccessory(themeId) {
    if (!themeId) return false;

    // Bahnübergänge
    if (themeId >= 250 && themeId <= 260)
        return true;

    return false;
}

function isDecoupler(themeId) {
    if (!themeId) return false;
    // decoupler
    if (themeId === 70) return true;
    return false;
}

function isBlock(themeId) {
    if (themeId === 150) return true;
    if (themeId === 151) return true;
    return false;
}

function isStaging(themeId) {
    if (themeId === 152) return true;
    return false;
}

function isFeedback(themeId) {
    if (!themeId) return false;
    if (themeId >= 200 && themeId <= 210)
        return true;
    return false;
}

function containsEmoji(str) {
    if (typeof str !== 'string') return false;
    // Alle Emojis aus der Liste zusammensetzen
    const allEmojis = Object.values(locomotiveFunctionEmojiList);
    return allEmojis.some(emoji => str.includes(emoji));
}

function renderLocomotiveFunctionImage(renderInfo) {

    /// Beim Build/Linting/Minimizing bekomme ich diese Meldung:
    //  [x] input(746,57-69): run-time error JS1013: Syntax error in regular expression: /\p{Emoji}/u
    // ChatGPT mein, dass dies bei mir mit einer altern Version von node/npm/jsengine nicht funktioniert.
    // Ein kleiner Workaround ist nachfolgend eingebaut.
    //// Unicode Emoji (auch kombiniert, z. B. ✌️ oder 🧑‍✈️)
    //const isUnicode = typeof renderInfo === 'string' && /\p{Emoji}/u.test(renderInfo);
    const isUnicode = containsEmoji(renderInfo);

    // FontAwesome Icon (z. B. "fas fa-lightbulb")
    const isFontAwesome = typeof renderInfo === 'string' && renderInfo.startsWith('fas');

    // Bilddatei (lokal, z. B. "licht.png")
    const isLocalImage = typeof renderInfo === 'string' &&
        /\.(png|jpe?g|gif|svg)$/i.test(renderInfo) &&
        !renderInfo.startsWith('http');

    // Bild-URL (extern)
    const isUrlImage = typeof renderInfo === 'string' &&
        /^https?:\/\/.+\.(png|jpe?g|gif|svg)$/i.test(renderInfo);

    if (isUnicode) {
        return `<span class="unicode-icon">${renderInfo}</span>`;
    } else if (isFontAwesome) {
        return `<i class="${renderInfo}"></i>`;
    } else if (isLocalImage) {
        return `<img src="/img/icons/${renderInfo}" alt="" class="icon-img" />`;
    } else if (isUrlImage) {
        return `<img src="${renderInfo}" alt="" class="icon-img" />`;
    } else {
        return `<span class="icon-placeholder">❓</span>`;
    }
}