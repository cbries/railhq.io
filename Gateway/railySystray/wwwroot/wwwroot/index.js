
import { w2form, w2ui, w2popup, w2alert } from "/libs/w2ui-2.0/w2ui-2.0.es6.min.js"
//import { w2form, w2ui, w2popup, w2alert } from "http://{{GENERATE_NET_HOST}}:"##GENERATE_NET_PORT##"/libs/w2ui-2.0/w2ui-2.0.es6.min.js"

window._ws = null;
window._form = null;

// general error handling
window.errorHandler = new ErrorHandler();

function wsOnOpen() {
    console.log('WebSocket-Connection established.');

    sendRequestPeriodically();
}

function updateStateLog(jsonData) {
    if (!jsonData) return;

    window.__stateData = jsonData;

    if (jsonData.gateway) {
        $("#gatewayInfo").html(`
                    <strong>Server:</strong> ${jsonData.gateway.address} <br>
                    <strong>Status:</strong> ${jsonData.gateway.connected ? "Verbunden ✅" : "Nicht verbunden ❌"}
                `);

        $("#logContent").val(jsonData.gateway.recentLog.join("\n"));
    }

    if (jsonData.extensions) {
        let extHtml = "<strong>Erweiterungen:</strong><ul>";
        $.each(jsonData.extensions, function (index, ext) {
            extHtml += `
                        <li>
                            ${ext.name}
                            <span style="color: ${ext.running ? "green" : "red"};">●</span>
                        </li>
                    `;
        });
        extHtml += "</ul>";
        $("#extensionsList").html(extHtml);
    }
}

function updateEcosPage(data) {
    if (!data) return;
    const ecosState = data?.extensions.filter(it => it.name === "ecos")[0];
    $('#ecos\\.state\\.connected').prop('checked', ecosState.connected);
}

function updateZ21Page(data) {
    if (!data) return;
    const ecosState = data?.extensions.filter(it => it.name === "z21")[0];
    $('#z21\\.state\\.connected').prop('checked', ecosState.connected);
}

function wsOnMessage(ev) {
    const data = JSON.parse(event.data);
    if (!data?.command) return;
    if (data.command === "update") {
        const record = data.message;
        window._form.record = record;
        window._form.refresh();
    } else if (data.command === "state") {
        //
        // updates the log of server information / state
        //
        updateStateLog(data);

        //
        // update pages
        //
        updateEcosPage(data);
        updateZ21Page(data);

    } else if (data.command === "validateAuth") {
        //
        // update ui for server authentication validation
        //
        const responseDiv = $("#response");
        if (data.accessToken && data.accessToken.length > 0 && data.message === "success") {
            const message = data.message || "Login erfolgreich";
            responseDiv.css({ "background-color": "#d4edda", "color": "#155724" })
                .text("Erfolg: " + message)
                .show();
        } else {
            let errorMessage = "Unbekannter Fehler";
            if (data.message)
                errorMessage = data.message;
            responseDiv.css({ "background-color": "#f8d7da", "color": "#721c24" })
                .text("Fehler: " + errorMessage)
                .show();
        }
    }
}

function wsOnError(err) {
    //console.error('WebSocket-Failure:', err);
    window.errorHandler.setLevel(ErrorHandlerLevel.Error);
    window.errorHandler.setText("Konfigurationsseite nicht erreichbar. (Interner Status: " + event.reason + ", " + event.code + ")", true);
}

function wsOnClose(ev) {
    //console.log('WebSocket-Connection closed:', event.code, event.reason);
    let reason = event.reason;
    if (!reason) reason = "unbekannter Fehler";
    else if (reason.length <= 0) reason = "unbekannter Fehler";

    window.errorHandler.setLevel(ErrorHandlerLevel.Info);
    window.errorHandler.setText("Konfigurationsseite nicht erreichbar.<br>Bitte prüfe ob dein <b>railhq.io - Gateway</b> gestartet ist. (Interner Status: " + reason + ", " + event.code + ")", true);
}

function sendRequestPeriodically() {
    setInterval(() => {
        if (window._ws && window._ws.readyState === WebSocket.OPEN) {
            const requestMessage = { command: "state" };
            window._ws.send(JSON.stringify(requestMessage));
        } else {
            // websocket not open
        }
    }, 1000);
}

function sendRequestValidateAuth(
    username,
    password,
    targetHost,
    targetPort) {
    if (window._ws && window._ws.readyState === WebSocket.OPEN) {
        const requestMessage = {
            command: "validateAuth",
            username: username,
            password: password,
            host: targetHost,
            port: targetPort
        };
        window._ws.send(JSON.stringify(requestMessage));
    } else {
        // websocket not open
    }
}

$(document).ready(() => {
    loadForm();

    //window._ws = new WebSocket('ws://{{GENERATE_NET_HOST}}:"##GENERATE_NET_PORT##"/ws');
    window._ws = new WebSocket(`ws://${window.location.hostname}:8090/ws`);
    window._ws.onopen = wsOnOpen;
    window._ws.onmessage = wsOnMessage;
    window._ws.onerror = wsOnError;
    window._ws.onclose = wsOnClose;

    $("#showLogOverlay").on("click", function (event) {
        event.preventDefault();
        $("#logOverlay").css("display", "flex");
        const jsonData = window.__stateData;
        updateStateLog(jsonData);
    });

    $("#closeLogOverlay").on("click", function () {
        $("#logOverlay").css("display", "none");
    });

    const savedLang = localStorage.getItem("language");
    if (savedLang) {
        window.setLanguage(savedLang);
    } else {
        window.setLanguage("de");
    }
});

function setLanguage(lang) {
    localStorage.setItem("language", lang);

    if (lang === 'de') {
        //
        // railhq.io
        //
        $('#auth\\.username').closest('.w2ui-field').find('label').text('Benutzername');
        $('#auth\\.password').closest('.w2ui-field').find('label').text('Passwort');

        $('#server\\.enabled').closest('.w2ui-field').find('label').text('Aktiviert');
        $('#server\\.timeoutSeconds').closest('.w2ui-field').find('label').text('Zeitlimit');
        $('#server\\.host').closest('.w2ui-field').find('label').text('Host');
        $('#server\\.port').closest('.w2ui-field').find('label').text('Port');

        $($('.page-0').find('.w2ui-group-title')[0]).text("Authentifizierung")
        $($('.page-0').find('.w2ui-group-title')[1]).text("Verbindung")
        $('button[name=custom]').text('Teste Authentifizierung');
        $('button[name=Save]').text('Speichern');

        //
        // ECoS
        //
        $('#ecos\\.enabled').closest('.w2ui-field').find('label').text('Aktiviert');
        $('#ecos\\.state\\.connected').closest('.w2ui-field').find('label').text('Verbunden');

        $($('.page-1').find('.w2ui-group-title')[0]).text("Einstellungen")
        $($('.page-1').find('.w2ui-group-title')[1]).text("Status")

        //
        // Z21
        //
        $('#z21\\.enabled').closest('.w2ui-field').find('label').text('Aktiviert');
        $('#z21\\.state\\.connected').closest('.w2ui-field').find('label').text('Verbunden');

        $($('.page-2').find('.w2ui-group-title')[0]).text("Einstellungen")
        $($('.page-2').find('.w2ui-group-title')[1]).text("Status")

        //
        // HSI-88-USB
        //
        $('#hsi\\.enabled').closest('.w2ui-field').find('label').text('Aktiviert');
        $('#hsi\\.left').closest('.w2ui-field').find('label').text('Links');
        $('#hsi\\.middle').closest('.w2ui-field').find('label').text('Mitte');
        $('#hsi\\.right').closest('.w2ui-field').find('label').text('Rechts');

        $('#hsi\\.devicePath').closest('.w2ui-field').find('label').text('Gerätepfad');
        $('#hsi\\.checkIntervalMs').closest('.w2ui-field').find('label').html('Debounce<br>Intervall (ms)');
        $('#hsi\\.onMs').closest('.w2ui-field').find('label').html('Auslöser<br>An (ms)');
        $('#hsi\\.offMs').closest('.w2ui-field').find('label').html('Auslöser<br>Aus (ms)');

        $($('.page-3').find('.w2ui-group-title')[0]).text("Einstellungen")
        $($('.page-3').find('.w2ui-group-title')[1]).contents().filter(function () { return this.nodeType === 3; }).first().replaceWith("Einstellungen (erweitert)")
    }
    else if (lang === 'en') {
        // railhq.io
        $('#auth\\.username').closest('.w2ui-field').find('label').text('Username');
        $('#auth\\.password').closest('.w2ui-field').find('label').text('Password');

        $('#server\\.enabled').closest('.w2ui-field').find('label').text('Enabled');
        $('#server\\.timeoutSeconds').closest('.w2ui-field').find('label').text('Timeout');
        $('#server\\.host').closest('.w2ui-field').find('label').text('Host');
        $('#server\\.port').closest('.w2ui-field').find('label').text('Port');

        $($('.page-0').find('.w2ui-group-title')[0]).text("Authentication")
        $($('.page-0').find('.w2ui-group-title')[1]).text("Connection")
        $('button[name=custom]').text('Test Authentication');
        $('button[name=Save]').text('Save');

        // ECoS
        $('#ecos\\.enabled').closest('.w2ui-field').find('label').text('Enabled');
        $('#ecos\\.state\\.connected').closest('.w2ui-field').find('label').text('Connected');

        $($('.page-1').find('.w2ui-group-title')[0]).text("Settings");
        $($('.page-1').find('.w2ui-group-title')[1]).text("State");

        //
        // Z21
        //
        $('#z21\\.enabled').closest('.w2ui-field').find('label').text('Enabled');
        $('#z21\\.state\\.connected').closest('.w2ui-field').find('label').text('Connected');

        $($('.page-2').find('.w2ui-group-title')[0]).text("Settings")
        $($('.page-2').find('.w2ui-group-title')[1]).text("State")

        //
        // HSI-88-USB
        //
        $('#hsi\\.enabled').closest('.w2ui-field').find('label').text('Enabled');
        $('#hsi\\.left').closest('.w2ui-field').find('label').text('Left');
        $('#hsi\\.middle').closest('.w2ui-field').find('label').text('Middle');
        $('#hsi\\.right').closest('.w2ui-field').find('label').text('Right');

        $('#hsi\\.devicePath').closest('.w2ui-field').find('label').text('Device Path');
        $('#hsi\\.checkIntervalMs').closest('.w2ui-field').find('label').html('Debounce<br>Interval (ms)');
        $('#hsi\\.onMs').closest('.w2ui-field').find('label').html('Trigger<br>On (ms)');
        $('#hsi\\.offMs').closest('.w2ui-field').find('label').html('Trigger<br>Off (ms)');

        $($('.page-3').find('.w2ui-group-title')[0]).text("Settings");
        $($('.page-3').find('.w2ui-group-title')[1]).contents().filter(function () { return this.nodeType === 3; }).first().replaceWith("Settings (advanced)");
    }

    document.querySelectorAll("[data-lang-" + lang + "]").forEach(el => {
        el.innerText = el.getAttribute("data-lang-" + lang);
    });
}

window.setLanguage = setLanguage;

function loadForm() {

    window._form = new w2form({
        box: '#form',
        name: 'form',

        fields: {
            "railhq.io": {
                type: 'tab',
                span: 5,
                fields: {
                    "Authentication": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        fields: {
                            "auth.username": {
                                type: 'text',
                                required: true,
                                label: 'Username'
                            },
                            "auth.password": {
                                type: 'password',
                                required: true,
                                label: 'Password'
                            }
                        }
                    },
                    "Connection": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        fields: {
                            "server.enabled": {
                                type: 'toggle',
                                required: false,
                                label: 'Enabled'
                            },
                            "server.timeoutSeconds": {
                                type: 'number',
                                required: true,
                                label: 'Timeout (sec)'
                            },
                            "server.host": {
                                type: 'text',
                                required: true,
                                label: 'Host'
                            },
                            "server.port": {
                                type: 'int',
                                required: true,
                                label: 'Port',
                                attr: 'style="width: 75px"',
                                options: { arrows: true, min: 0, max: 65535 }
                            }
                        }
                    }
                }
            },

            "ECoS": {
                type: 'tab',
                fields: {
                    "Settings": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        span: 4,
                        fields: {
                            "ecos.enabled": {
                                type: 'toggle',
                                required: false,
                                label: 'Enabled'
                            },
                            'ecos.host': {
                                type: 'text',
                                required: false,
                                label: 'IP'
                            },
                            'ecos.port': {
                                type: 'int',
                                required: false,
                                label: 'Port',
                                attr: 'style="width: 75px"',
                                options: { arrows: true, min: 0, max: 65535 }
                            }
                        }
                    },
                    "State": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        span: 4,
                        fields: {
                            "ecos.state.connected": {
                                type: 'toggle',
                                required: false,
                                label: 'Connected'
                            }
                        }
                    }
                }
            },

            "Z21": {
                type: 'tab',
                fields: {
                    "Settings": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        span: 4,
                        fields: {
                            "z21.enabled": {
                                type: 'toggle',
                                required: false,
                                label: 'Enabled'
                            },
                            'z21.host': {
                                type: 'text',
                                required: false,
                                label: 'IP'
                            },
                            'z21.port': {
                                type: 'int',
                                required: false,
                                label: 'Port',
                                attr: 'style="width: 75px"',
                                options: { arrows: true, min: 0, max: 65535 }
                            }
                        }
                    },
                    "State": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        span: 4,
                        fields: {
                            "z21.state.connected": {
                                type: 'toggle',
                                required: false,
                                label: 'Connected'
                            }
                        }
                    }
                }
            },

            "HSI-88-USB": {
                type: 'tab',
                fields: {
                    "Settings": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        span: 4,
                        fields: {
                            "hsi.enabled": {
                                type: 'toggle',
                                required: false,
                                label: 'Enabled'
                            },

                            'hsi.left': {
                                type: 'int',
                                required: false,
                                label: 'Left',
                                options: { arrows: true, min: 0, max: 31 }
                            },
                            'hsi.middle': {
                                type: 'int',
                                required: false,
                                label: 'Middle',
                                options: { arrows: true, min: 0, max: 31 }
                            },
                            'hsi.right': {
                                type: 'int',
                                required: false,
                                label: 'Right',
                                options: { arrows: true, min: 0, max: 31 }
                            }
                        }
                    },
                    "Settings (advanced)": {
                        type: 'group',
                        attr: 'style="width: 150px"',
                        span: 4,
                        collapsible: true,
                        style: 'display: none;',
                        fields: {
                            'hsi.devicePath': {
                                type: 'string',
                                required: false,
                                label: 'Device Path'
                            },
                            'hsi.checkIntervalMs': {
                                type: 'int',
                                required: true,
                                label: 'Debounce<br>Interval (ms)',
                                options: { arrows: true, min: 20, max: 750 }
                            },
                            'hsi.onMs': {
                                type: 'int',
                                required: true,
                                label: 'Trigger<br>On (ms)',
                                options: { arrows: true, min: 20, max: 2500 }
                            },
                            'hsi.offMs': {
                                type: 'int',
                                required: true,
                                label: 'Trigger<br>Off (ms)',
                                options: { arrows: true, min: 20, max: 2500 }
                            }
                        }
                    }
                }
            }
        },
        actions: {
            custom: {
                text: 'Validate Server',
                class: 'w2ui-btn-green',
                style: 'text-transform: uppercase',
                async onClick(event) {
                    if (w2ui.form.validate().length === 0) {
                        const data = this.getCleanRecord();
                        const username = data.auth.username;
                        const password = data.auth.password;
                        const host = data.server.host;
                        const port = data.server.port;
                        sendRequestValidateAuth(username, password, host, port);
                    }
                }
            },
            Save(event) {
                if (w2ui.form.validate().length === 0) {
                    const jsonStr = JSON.stringify(this.getCleanRecord(), null, 4);
                    window._ws.send(jsonStr);
                }
            }
        }
    });

    $('#ecos\\.state\\.connected').change(function () {
        if ($(this).is(':checked')) {
            console.log("Trigger connect to ECoS...");
            const requestMessage =
            {
                command: "triggerEcosConnect"
            };
            window._ws.send(JSON.stringify(requestMessage));
        }
    });

    $('#z21\\.state\\.connected').change(function () {
        if ($(this).is(':checked')) {
            console.log("Trigger connect to Z21...");
            const requestMessage =
            {
                command: "triggerZ21Connect"
            };
            window._ws.send(JSON.stringify(requestMessage));
        }
    });
}
