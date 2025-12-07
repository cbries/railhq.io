// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


window.__access_token = "##access_token##";
window.__access_email = "##access_email##";
window.__access_uid = "";

window.__urlHq = "{{GENERATE_NET_HOST}}";
window.__urlPortHq = "##GENERATE_NET_PORT##";
window.__wsProtocol = "##GENERATE_WS_PROTOCOL##";
window.__httpProtocol = "##GENERATE_HTTP_PROTOCOL##";

window.instanceArguments = {
    driverName: '',
    objectId: '',
    workspace: '',
    uid: ''
};

window.ctrls = {
    backward: null,
    forward: null,
    speedstepSlider: null,
    speedstep0: null,
    speedstep1: null,
    speedstep2: null,
    speedstep3: null,
    speedstep4: null,
    imgLocomotive: null,
    cmdEmergencyStop: null,
    cmdPowerOff: null,
    locCarousell: null
};

function getAccessKeyFromString(url) {
    const hashIndex = url.indexOf("#");
    if (hashIndex === -1) return null;
    const hash = url.substring(hashIndex + 1);
    const params = new URLSearchParams(hash);
    return params.get("accessKey");
}

function getAccessToken() {
    if (!window.__access_token || window.__access_token.length <= 0) {
        let accessToken = getAccessKeyFromString(window.location.href);
        if (accessToken) {
            localStorage.setItem("accessToken", accessToken);
        } else {
            accessToken = localStorage.getItem("accessToken") || "";
        }

        window.__access_token = accessToken;
    }

    return window.__access_token;
}

function doControl() {
    initControl();
    $("#circularSlider").roundSlider({
        sliderType: "min-range",
        editableTooltip: false,
        radius: 100,
        width: 35,
        value: 0,
        handleSize: 0,
        handleShape: "square",
        circleShape: "pie",
        startAngle: 315,
        tooltipFormat: "changeTooltip",
        drag: function (event) {  // LIVE-UPDATE BEIM BEWEGEN
            $("#speedDisplay").text(event.value);
        },
        change: function (event) {
            $("#speedDisplay").text(event.value);
        }
    });
    doLocScrollDelayed(window.instanceArguments.objectId);
}

$(document).ready(function () {
    //console.log("Locomotive control loaded!");
    //console.log("Access token: " + getAccessToken());

    //
    // prüfe ob eine UID in der URL steht, wenn ja, dann nehmen wir diese direkt
    //
    let currentUrl = window.location.href;
    let urlParams = new URLSearchParams(new URL(currentUrl).search);
    let uid = urlParams.get('uid');
    if (uid && uid.length > 0) {
        window.__access_uid = uid;
        doControl();
    }

    //
    // bevor wir irendwas machen laden wir erstmal
    // die user information, so dass wir problemlos
    // im ganzen Control alles machen können
    //
    fetch(`/api/auth/getuid?jwt=${getAccessToken()}`)
        .then(response => response.json())
        .then(data => {
            window.__access_uid = data.uid;
            doControl();
        });
});

function changeTooltip(e) {
    var val = e.value, speed;
    if (val < 20) speed = "Slow";
    else if (val < 40) speed = "Normal";
    else if (val < 70) speed = "Speed";
    else speed = "HighSpeed";

    return val; // + " " + "<div>" + speed + "<div>";
}

/**
 * Server
 */
// #region Server

const requestParams = new URLSearchParams(window.location.search);
window.instanceArguments.workspace = requestParams.get('workspace')
window.instanceArguments.driverName = requestParams.get('driverName');
window.instanceArguments.objectId = parseInt(requestParams.get('objectId'));
window.instanceArguments.uid = requestParams.get('uid');
const wsSubpath = '/ws/remotecontrol' + ((window.instanceArguments.workspace && window.instanceArguments.workspace.length > 0) ? `?workspace=${window.instanceArguments.workspace}` : '');
window.serverHandling = new ServerHandling({
    wsAddr: "{{GENERATE_NET_HOST}}",
    wsPort: "##GENERATE_NET_PORT##",
    wsSubpath: wsSubpath
});
window.serverHandling.establishConnection();
window.serverHandling.on('fatalError', (ev) => fatalErrorReceived(ev.data));
window.serverHandling.on('dataReceived', (ev) => dataReceivedHandle(ev.data));
window.serverHandling.on('updateState', (ev) => dataReceivedHandle(ev.data));
window.serverHandling.on('initialization', (ev) => dataReceivedHandle(ev.data));

// #endregion

let __initialized = false;
let __dataInitialized = false;

function fatalErrorReceived(jsonData) {
    if (!jsonData.info) return;
    const info = jsonData.info;
    showError(info);
    window.serverHandling.disableReconnectHandler();
}

// Overlay anzeigen
function showError(message = "Fatal error - no additional information available!") {
    if (message.code) {
        var code = message.code;
        var errmsg = message.message;
        document.querySelector("#errorOverlay p").textContent = errmsg;
    } else {
        document.querySelector("#errorOverlay p").textContent = message;
    }
    document.getElementById("errorOverlay").classList.remove("hidden");
}

function dataReceivedHandle(jsonData) {

    if (jsonData.railyData?.locomotives) {
        applyData(jsonData.railyData);
        updateForm(jsonData.railyData);
        // ...
    }

    if (jsonData.entityData) {
        updateForm(jsonData.entityData);
        updateFormSystem(jsonData.Entity);
    }

    if (jsonData.settings) {
        updateSettings(jsonData.settings);
    }

    if (jsonData.stateData) {
        updateStatusBarAutoModeInfo(jsonData.stateData)
    }

    if (jsonData.railyData) {
        const railyData = jsonData.railyData;

        let ecosIsOnline = false;
        let demoIsOnline = false;

        // ESU ECoS
        if (railyData?.ecosbase) {
            const ecosbase = railyData.ecosbase;
            ecosIsOnline = ecosbase.status === "GO";
        }

        // Demo
        if (railyData?.demobase) {
            const demobase = railyData.demobase;
            demoIsOnline = demobase.status === "GO";
        }

        // Wenn nur eine einzige angeschlossende CommandStation online
        // ist, dann wird der Button entsprechend als aktive angezeigt.
        window.ctrls.cmdPowerOff.removeClass("bg-black");
        window.ctrls.cmdPowerOff.removeClass(BgOnPowerButtons);
        window.ctrls.cmdPowerOff.removeClass(BgOffPowerButtons);
        if (ecosIsOnline === true || demoIsOnline === true) {
            window.ctrls.cmdPowerOff.html("⚡ GO");
            window.ctrls.cmdPowerOff.addClass(BgOnPowerButtons + " !important");
        } else {
            window.ctrls.cmdPowerOff.html("⚡ STOP");
            window.ctrls.cmdPowerOff.addClass(BgOffPowerButtons + " !important");
        }
    }
}

function getLocomotiveData(data) {
    if (!data) return null;
    const refData = window.instanceArguments;
    for (let i = 0; i < data.locomotives.length; ++i) {
        const locData = data.locomotives[i];
        if (!locData) continue;
        if (locData.driverName === refData.driverName && locData.objectId == refData.objectId)
            return locData;
    }
    return null;
}

function updateFormSystem(data) {
    // to be defined
}

function updateForm(data) {
    // Steps:
    //   update speedstep slider
    //   update direction state
    //   update functions button

    let locData = null;
    if (typeof data === "object" && data.locomotives) {
        locData = getLocomotiveData(data);
    } else {
        locData = data;
    }

    // data of entity does not fit
    if (locData.driverName !== window.instanceArguments.driverName ||
        locData.objectId !== window.instanceArguments.objectId)
        return;

    // slider
    const currentSpeedstepSliderValue = window.ctrls.speedstepSlider.roundSlider("option", "value");
    if (currentSpeedstepSliderValue !== locData.speedstep)
        window.ctrls.speedstepSlider.roundSlider("option", "value", locData.speedstep);

    $("#speedDisplay").text(locData.speedstep);

    // direction
    window.ctrls.forward.removeClass(BgNeutralButtons);
    window.ctrls.forward.removeClass(BgOffFncButtons);
    window.ctrls.forward.removeClass(BgOnFncButtons);
    window.ctrls.backward.removeClass(BgNeutralButtons);
    window.ctrls.backward.removeClass(BgOffFncButtons);
    window.ctrls.backward.removeClass(BgOnFncButtons);
    if (locData.direction === 0) { // forward
        window.ctrls.forward.addClass(BgOnFncButtons);
    } else { // backward
        window.ctrls.backward.addClass(BgOnFncButtons);
    }

    // functions
    for (let i = 0; i < locData.funcdesc.length; ++i) {
        const funcdesc = locData.funcdesc[i];
        if (!funcdesc) continue;
        const idx = funcdesc.idx;
        let state = funcdesc.state;
        //const type = funcdesc.type;
        const ctrlId = `fnc${idx}`;

        const ctrl = $(`#${ctrlId}`);
        if (ctrl) {

            ctrl.data("state", state);

            if (state === true) {
                ctrl.removeClass(BgOffFncButtons);
                ctrl.addClass(BgOnFncButtons);
            } else {
                ctrl.removeClass(BgOnFncButtons);
                ctrl.addClass(BgOffFncButtons);
            }
        }
    }
}

let assignedLocomotives = [];

function updateSettings(data) {
    assignedLocomotives = [];
    for (let i = 0; i < data.locomotives.length; ++i) {
        const locData = data.locomotives[i];
        if (locData.assignedToBlock && locData.assignedToBlock.length > 0) {
            assignedLocomotives.push({
                driverName: locData.driverName,
                objectId: locData.objectId
            });

            // show reserved indicator
            if (window.instanceArguments.objectId === locData.objectId &&
                window.instanceArguments.driverName === locData.driverName) {
                $('.reservedIndicator').show();
                $('.reservedBlockName').html(locData.assignedToBlock);
            }
        }
    }
}

function updateStatusBarAutoModeInfo(stateData) {
    if (!stateData) return;

    const divCtrl = $('.automodeInfo');
    if (!stateData.automaticEnabled && stateData.runningRoutes === 0) {
        divCtrl.text('');
        return;
    }

    const stoppedDt = stateData.startedDt;
    const stoppedDate = new Date(stoppedDt);
    const now = new Date();
    const deltaMs = now - stoppedDate;
    const deltaSeconds = Math.floor(Math.abs(deltaMs) / 1000);
    const minutes = Math.floor(deltaSeconds / 60);
    const seconds = deltaSeconds % 60;
    const m3 = `AutoMode runs ${minutes} min ${seconds} sec`;
    divCtrl.text(`${m3}`);
}

function applyData(railyData) {
    if (__dataInitialized === true) return;

    const locData = getLocomotiveData(railyData);

    // #region load locomotive image

    const fakeId = `img_${locData.driverName}_${locData.objectId}_head`;
    window.ctrls.imgLocomotive.attr({
        id: fakeId,
        src: "./images/noimage32x32.png",
        alt: locData.name
    });
    loadLocomotiveImageIntoHtml(fakeId, locData.name,
        {
            uid: window.__access_uid
        });

    // #endregion

    // #region general information

    $('.locname').html(locData.name);
    $('.locdriver').html(locData.driverName);
    $('.locobjectid').html(locData.objectId);

    let addr = locData.addr;
    if (!addr || addr <= 0) addr = "MFX";
    $('.locaddr').html(addr);

    // #endregion

    // #region load functions

    const cmdField = $('.locomotiveCommandField');
    for (let i = 0; i < locData.funcdesc.length; ++i) {
        const funcdesc = locData.funcdesc[i];
        if (!funcdesc) continue;
        const idx = funcdesc.idx;
        let state = funcdesc.state;
        const type = funcdesc.type;

        let fnctxt = ListOfFunctionDescription[type];
        if (!fnctxt) fnctxt = `F${idx}`;

        let fncstate = BgOffFncButtons;
        if (state === true) fncstate = BgOnFncButtons;
        else state = false;

        // <button class="mt-8 px-4 py-4 bg-red-500 text-white rounded-lg shadow-md">F0</button>
        const fncbtn = $('<button>',
            {
                text: `${fnctxt}`,
                id: `fnc${idx}`,
                click: function (ev) {
                    const fid = parseInt($(ev.target).attr("id").replace("fnc", ""));
                    const state = $(ev.target).data("state");

                    // state negieren zum Schalten, zudem muss es 1 oder 0 sein, kein Boolean
                    const istate = state === true ? 0 : 1;

                    sendLocomotiveCommand({
                        objectId: locData.objectId,
                        command: 'update',
                        argument: 'function',
                        argumentValue: {
                            driverName: locData.driverName,
                            value: [fid, istate]
                        }
                    });
                }
            })
            .addClass(`mt-8 px-4 py-4 ${fncstate} text-white rounded-lg shadow-md`)
            .css({
                margin: "5px",
                "margin-top": "20px"
            });

        fncbtn.data("state", state);

        fncbtn.appendTo(cmdField);
    }

    // #endregion

    // #region init slider

    const speedstep = locData.speedstep;
    const speedstepMax = locData.speedstepMax;

    window.ctrls.speedstepSlider.roundSlider("option", "max", speedstepMax);
    window.ctrls.speedstepSlider.roundSlider("option", "value", speedstep);
    window.ctrls.speedstepSlider.off('change');
    window.ctrls.speedstepSlider.on('change',
        (ev) => {
            let speedValue = $(ev.target).roundSlider("option", "value");
            if (speedValue <= 0) speedValue = 0;
            else if (speedValue >= speedstepMax) speedValue = speedstepMax;

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: locData.driverName,
                    value: speedValue
                }
            });

        });

    // #endregion

    // #region init commands

    window.ctrls.backward.off('click');
    window.ctrls.backward.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'direction',
                argumentValue: {
                    driverName: locData.driverName,
                    direction: 1
                }
            });

        });

    window.ctrls.forward.off('click');
    window.ctrls.forward.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'direction',
                argumentValue: {
                    driverName: locData.driverName,
                    direction: 0
                }
            });

        });

    window.ctrls.speedstep0.off('click');
    window.ctrls.speedstep0.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: locData.driverName,
                    value: 0
                }
            });

        });

    window.ctrls.speedstep1.off('click');
    window.ctrls.speedstep1.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: locData.driverName,
                    value: "levelMinimum"
                }
            });

        });

    window.ctrls.speedstep2.off('click');
    window.ctrls.speedstep2.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: locData.driverName,
                    value: "levelEnter"
                }
            });

        });

    window.ctrls.speedstep3.off('click');
    window.ctrls.speedstep3.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: locData.driverName,
                    value: "levelCruise"
                }
            });

        });

    window.ctrls.speedstep4.off('click');
    window.ctrls.speedstep4.on('click',
        (ev) => {

            sendLocomotiveCommand({
                objectId: locData.objectId,
                command: 'update',
                argument: 'speedstep',
                argumentValue: {
                    driverName: locData.driverName,
                    value: "levelMax"
                }
            });

        });

    window.ctrls.cmdEmergencyStop.off('click');
    window.ctrls.cmdEmergencyStop.on('click',
        (ev) => {

            sendSystemCommand({
                command: 'update',
                argument: 'power',
                argumentValue: 'stopAllTrains'
            });

        });

    window.ctrls.cmdPowerOff.off('click');
    window.ctrls.cmdPowerOff.on('click',
        (ev) => {

            sendSystemCommand({
                command: 'update',
                argument: 'power',
                argumentValue: 'toggle'
            });

        });

    // #endregion

    // #region loader locomotive scroller

    railyData.locomotives.sort((a, b) => a.name.toLowerCase().localeCompare(b.name.toLowerCase()));

    for (let i = 0; i < railyData.locomotives.length; ++i) {
        const loc = railyData.locomotives[i];
        if (!loc) continue;
        const newCtrl = $('<div>',
            {
                class: 'locTile flex-shrink-0 w-24 flex flex-col justify-end text-center'
            });
        const fakeImageId = `img_${loc.driverName}_${loc.objectId}`;
        const newImg = $('<img>',
            {
                class: 'w-full rounded-lg shadow-md',
                id: fakeImageId,
                src: "./images/noimage32x32.png",
                alt: loc.name,
                style: "max-height: 32px;"
            });
        const newP = $('<p>',
            {
                class: 'mt-1 text-sm text-center break-word',
                text: loc.name
            });
        newImg.appendTo(newCtrl);
        newP.appendTo(newCtrl);

        newCtrl.appendTo(window.ctrls.locCarousell);

        newCtrl.on('click',
            (ev) => {

                window.location.href = `/locomotive?driverName=${loc.driverName}`
                    + `&objectId=${loc.objectId}`
                    + `&workspace=${window.instanceArguments.workspace}`
                    + `&uid=${window.__access_uid}`;
            });

        loadLocomotiveImageIntoHtml(fakeImageId, loc.name,
            {
                uid: window.__access_uid
            });
    }

    // #endregion
}

function initControl() {
    if (__initialized === true) return;
    __initialized = true;
    window.ctrls.backward = $('.directionBackward');
    window.ctrls.forward = $('.directionForward');
    window.ctrls.speedstepSlider = $("#circularSlider");
    window.ctrls.speedstep0 = $('.speedLevel0');
    window.ctrls.speedstep1 = $('.speedLevel1');
    window.ctrls.speedstep2 = $('.speedLevel2');
    window.ctrls.speedstep3 = $('.speedLevel3');
    window.ctrls.speedstep4 = $('.speedLevel4');
    window.ctrls.imgLocomotive = $('.locomotiveImage');
    window.ctrls.cmdEmergencyStop = $('.emergencyStop');
    window.ctrls.cmdPowerOff = $('.powerOff');
    window.ctrls.locCarousell = $('.locCarousell');
}

function doLocScrollDelayed(currentLocId) {
    if (scrollToCurrentLoc(window.instanceArguments.objectId) === false) {
        setTimeout(function () {
            doLocScrollDelayed(currentLocId);
        },
            1000);
    }
}

function scrollToCurrentLoc(currentLocId) {
    const container = document.querySelector('.scrollable-container');
    const currentLoc = document.querySelector(`#img_ecos_${currentLocId}`);

    if (container && currentLoc) {

        if (container && currentLoc) {
            currentLoc.parentElement.scrollIntoView({
                behavior: 'smooth',
                block: 'nearest',
                inline: 'center'
            });
        }
    }

    return currentLoc != null && typeof currentLoc !== "undefined";
}
