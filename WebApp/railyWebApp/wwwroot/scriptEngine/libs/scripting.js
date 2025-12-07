
//
// globale Einstellungen für das Skripting
//

const scriptingTypeFiles = [
    "railhq-helper.d.ts",
    "railhq-global.d.ts",
    "railhq-accessory.d.ts",
    "railhq-controlstation.d.ts",
    "railhq-locomotive.d.ts",
    "railhq-sensors.d.ts",
    "railhq-mqtt.d.ts",
    "railhq-ui.d.ts",
    "railhq-hqapi.d.ts",
    "locomotive-status.d.ts",
    "accessory-status.d.ts"
]

const apiBaseScripts = '/api/v1/automation/scripts';
const apiBaseLocomotive = '/api/v1/automation/locomotive';
const apiBaseAccessory = '/api/v1/automation/accessory';
const apiBaseControlStation = '/api/v1/automation/station';

// "https://railhq.io:5001/api/v1/automation/events";
const apiBaseEvents = "/api/v1/automation/events";

const apiEventNameControlStation = "hqControlStation";
const apiEventNameLocomotive = "hqLocomotive";
const apiEventNameAccessory = "hqAccessory";
const apiEventNameFeedback = "hqFeedback";

const apiEventNameCsCutom = "controlStationChange";
const apiEventNameLocomotiveCutom = "locomotiveChange";
const apiEventNameAccessoryCutom = "accessoryChange";

const defaultCode = `
// Standard-Skriptvorlage für die Modelleisenbahn-Steuerung
// Generiert am: ${(new Date()).toLocaleString()}
//
// Diese Vorlage bietet einen Einstiegspunkt für eigene Automatisierungen.
// Sie zeigt, wie du die API (hqLocomotive) und Hilfsfunktionen (hqHelper) nutzt.
// Ergänze hier deinen individuellen Steuerungs-Code.

const br212Name = "BR_212_215-8";

const allLocs = await hqLocomotive.getAllLocomotives();
for(let i=0; i < allLocs.length; ++i) {
    if(allLocs[i].name === br212Name) {
        const status = await hqLocomotive.getStatus(allLocs[i].driverName, allLocs[i].address);
        log(allLocs[i].name);
        log(allLocs[i].name + " (" + status.driverName + "::" + status.address + ") fährt mit Geschwindigkeit " + status.speed);
    }
}

await hqHelper.sleepSec(1);
`;

const defaultCodeEmpty = `
// Standard-Skriptvorlage für die Modelleisenbahn-Steuerung
// Generiert am: ${(new Date()).toLocaleString()}

//
// Füge hier Deinen Code hinzu:
// 
log("Lokführer-Modus aktiviert 🚂💨");

// Halte das Skript am Leben, bis "Stop" gedrückt wurde
while (!api.shouldStop?.()) {
    await new Promise(resolve => setTimeout(resolve, 500));
}
`;

// #region EventHandling des Event-Streams

const controlStationEvents = new EventTarget();
const locomotiveEvents = new EventTarget();
const accessoryEvents = new EventTarget();
const feedbackEvents = new EventTarget();

// Helper function to get access token from localStorage
function getEventStreamToken() {
    return localStorage.getItem("accessToken") || "";
}

window.addEventListener("load",
    () => {
        // EventSource cannot send Authorization headers, so we pass token as query parameter
        const token = getEventStreamToken();
        const eventUrl = token ? `${apiBaseEvents}?token=${encodeURIComponent(token)}` : apiBaseEvents;
        const source = new EventSource(eventUrl);

        //
        // ControlStation
        //
        source.addEventListener(apiEventNameControlStation, (event) => {
            try {
                let data = JSON.parse(event.data);
                if (data.data) data = data.data;
                const { driverName, objectId } = data;
                const csEvent = new CustomEvent(apiEventNameCsCutom,
                    {
                        detail: {
                            driverName: driverName,
                            data: data
                        }
                    });

                controlStationEvents.dispatchEvent(csEvent);
            } catch (e) {
                console.error("Fehler beim Verarbeiten des ControlStation-Event:", e);
            }
        });

        //
        // Locomotive
        //
        source.addEventListener(apiEventNameLocomotive, (event) => {
            try {
                let data = JSON.parse(event.data);
                if (data.data) data = data.data;
                const { driverName, objectId } = data;
                const locomotiveEvent = new CustomEvent(apiEventNameLocomotiveCutom,
                    {
                        detail: {
                            driverName: driverName,
                            address: objectId,
                            data: data
                        }
                    });

                locomotiveEvents.dispatchEvent(locomotiveEvent);
            } catch (e) {
                console.error("Fehler beim Verarbeiten des Locomotive-Event:", e);
            }
        });

        //
        // Accessory
        //
        source.addEventListener(apiEventNameAccessory, (event) => {
            try {
                let data = JSON.parse(event.data);
                if (data.data) data = data.data;
                const { driverName, addr } = data;
                const accessoryEvent = new CustomEvent(apiEventNameAccessoryCutom,
                    {
                        detail: {
                            driverName: driverName,
                            address: addr,
                            data: data
                        }
                    });

                accessoryEvents.dispatchEvent(accessoryEvent);

            } catch (e) {
                console.error("Fehler beim Verarbeiten des Accessory-Event:", e);
            }
        });

        //
        // Feedback
        //
        source.addEventListener(apiEventNameFeedback, (event) => {
            try {
                let data = JSON.parse(event.data);
                if (data.data) data = data.data;
                const { driverName, port, pins, hex, binary } = data;
                const pinStates = parseBinaryToPinArray(binary, pins);
                for (let pin = 1; pin <= pins; pin++) {
                    const pinState = pinStates[pin - 1]; // indexbasiert
                    const eventId = createSensorId(driverName, port, pin);
                    const sensorEvent = new CustomEvent(eventId,
                        {
                            detail: {
                                driver: driverName,
                                port: port,
                                pin: pin,
                                state: pinState
                            }
                        });

                    feedbackEvents.dispatchEvent(sensorEvent);
                }

            } catch (e) {
                console.error("Fehler beim Verarbeiten des Feedback-Event:", e);
            }
        });

        //
        // ALL -- massive flood
        //
        //source.onmessage = function (event) {
        //    try {
        //        let data = JSON.parse(event.data);
        //        if (data.data) data = data.data;
        //        console.log("Event:", data);
        //    } catch (e) {
        //        console.error("Fehler beim Verarbeiten des Events:", e);
        //    }
        //};

        source.onerror = function (err) {
            console.error("Verbindung zum Event-Stream unterbrochen:", err);
        };
    });

//
// Hilfsfunktion: Wandelt Binärstring in ein Array von Booleans (Pin 1 = ganz rechts)
//
function parseBinaryToPinArray(binaryStr, expectedLength = 16) {
    return binaryStr
        .padStart(expectedLength, "0")
        .split("")
        .map(bit => bit === "1")
        .reverse(); // Pin 1 ist rechts → daher umdrehen
}

function createSensorId(driverName, sensorPort, sensorPin) {
    return `intern-sensor-${driverName}-${sensorPort}-${sensorPin}`;
}

// #endregion

window.__scriptingIsLoaded = false;
window.__scriptingFunctions = {};
window.__scriptingEditor = null;

async function loadAndSetTypedefs() {
    const libs = [];
    for (const filename of scriptingTypeFiles) {
        const url = `/scriptEngine/typedefs/${filename}`;
        try {
            const response = await fetch(url);
            if (!response.ok) {
                console.error(`Fehler beim Laden von ${url}:`, response.statusText);
                continue;
            }
            const content = await response.text();
            libs.push({
                content,
                filePath: `file:///riesolution/@types/${filename}`
            });
        } catch (e) {
            console.error(`Fetch-Fehler bei ${url}:`, e);
        }
    }
    if (libs.length > 0) {
        monaco.languages.typescript.javascriptDefaults.setExtraLibs(libs);
        console.log('Typedefinitionen geladen und gesetzt:', libs.map(l => l.filePath));
    } else {
        console.warn('Keine Typedefinitionen geladen.');
    }
}

async function loadSnippets() {
    const basePath = '/scriptEngine/snippets';
    const indexResponse = await fetch(`${basePath}/snippetIndex.json`);
    const files = await indexResponse.json();
    const snippets = [];
    for (const file of files) {
        console.log(`Load snippet: ${file}`);
        const response = await fetch(`${basePath}/${file}`);
        const text = await response.text();

        // snippetMetainformation mit RegExp extrahieren
        const metaMatch = text.match(/const snippetMetainformation\s*=\s*({[\s\S]*?});/);
        let meta = null;
        if (metaMatch) {
            // Eval nur für Objektliteral (vorsichtig!)
            meta = eval('(' + metaMatch[1] + ')');
        }

        // Snippet-Code ist der Rest nach meta-Objekt
        const snippetCode = text.replace(metaMatch[0], '').trim();

        if (meta) {
            snippets.push({
                label: meta.label,
                description: meta.description,
                insertText: snippetCode
            });
        }
    }

    return snippets;
}

async function loadMonacoSamples() {
    await loadSnippets().then(snippets => {
        monaco.languages.registerCompletionItemProvider('javascript', {
            provideCompletionItems: function (model, position) {
                const suggestions = snippets.map(s => ({
                    label: s.label,
                    kind: monaco.languages.CompletionItemKind.Snippet,
                    insertText: s.insertText,
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    documentation: s.description,
                }));
                return { suggestions };
            }
        });
    });
}

window.__scriptingFunctions.askScriptName = function askScriptName(originalData, callback) {
    w2popup.open({
        title: 'Skriptname & Beschreibung',
        body: `
        <div style="padding: 5px;">
            <div style="margin-bottom: 10px;">
                <input id="scriptNameInput" type="text"
                    style="width: 100%; padding: 5px; font-size: 14px; box-sizing: border-box; border: 1px solid #ccc; border-radius: 3px;"
                    placeholder="Name eingeben...">
            </div>
            <div>
                <textarea id="scriptDescriptionInput"
                    style="width: 100%; height: 100px; padding: 5px; font-size: 14px; box-sizing: border-box; border: 1px solid #ccc; border-radius: 3px; resize: none;"
                    placeholder="Beschreibung eingeben..."></textarea>
            </div>
            <div style="display: none; align-items: center;" id="scriptExampleSourceContainer">
                <input type="checkbox" id="disableExampleSource" style="margin-right: 6px;">
                <label for="disableExampleSource" style="margin: 0; font-weight: normal; padding-top: 1px;">Kein Beispiel-Code einfügen</label>
            </div>
        </div>
    `,
        buttons:
            '<button class="w2ui-btn" id="btnCancel">Abbrechen</button> <button class="w2ui-btn" id="btnOk">OK</button>',
        width: 300,
        height: 280,
        onOpen: function (event) {

            event.onComplete = function () {

                $('#scriptNameInput').val(originalData?.name || '');
                $('#scriptDescriptionInput').val(originalData?.description || '');

                if (originalData.isRename && originalData.isRename === true) {
                    //$('#scriptExampleSourceContainer').hide();
                } else {
                    $('#scriptExampleSourceContainer').css('display', 'flex');
                }

                $('#scriptNameInput').focus();

                $('#btnCancel').on('click',
                    function () {
                        w2popup.close();
                    });

                $('#btnOk').on('click',
                    function () {
                        const name = $('#scriptNameInput').val().trim();
                        const description = $('#scriptDescriptionInput').val().trim();
                        const skipExampleSource = document.getElementById('disableExampleSource').checked;
                        if (name === '') {
                            alert('Bitte einen Namen eingeben.');
                            return;
                        }
                        w2popup.close();
                        if (callback) callback({ name, description, skipExampleSource });
                    });

                // Enter in Textfeld: OK auslösen
                $('#scriptNameInput').on('keydown',
                    function (e) {
                        if (e.key === 'Enter') {
                            $('#btnOk').click();
                        }
                    });
            };
        }
    });
};

window.__scriptingFunctions.fetchScripts = async function fetchScripts() {
    const res = await fetch(apiBaseScripts);
    const scripts = await res.json();
    window.__scriptingFunctions.__renderScripts(scripts);

    // Wenn Skripte vorhanden sind, das erste Skript automatisch laden
    if (scripts.length > 0) {
        window.__scriptingFunctions.loadScript(scripts[0].id);
    } else {
        // Kein Skript vorhanden – Editor leeren mit Hinweistext
        window.__scriptingEditor.setValue(`// Kein Skript vorhanden.
// Klicke links auf das grüne "+"-Symbol, um schnell ein neues Skript zu erstellen.
`);
    }
}

window.__scriptingFunctions.renameScript = async function renameScript(id) {

    // frage nach den aktuellen Daten des Skript mit `id`
    const data = await window.hqScriptRunner.getServerInfoAboutScript(id);
    let renameName = '';
    let renameDescription = '';
    if (data) {
        renameName = data.name || '';
        renameDescription = data.description || '';
    }

    window.__scriptingFunctions.askScriptName({
        name: renameName,
        description: renameDescription,
        isRename: true
    },
        async function (scriptData, description) {
            if (!scriptData && !scriptData.name) return;
            await fetch(`${apiBaseScripts}/${id}/rename`,
                {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ newName: scriptData.name })
                });
            await window.__scriptingFunctions.fetchScripts();
        });
}

window.__scriptingFunctions.deleteScript = async function deleteScript(id) {

    // frage nach den aktuellen Daten des Skript mit `id`
    const data = await window.hqScriptRunner.getServerInfoAboutScript(id);
    let deleteName = '';
    if (data) {
        deleteName = data.name || '';
    }

    w2popup.open({
        title: 'Skript löschen',
        body: `<div style="padding: 15px;">Willst du das Skript <b>${deleteName}</b> löschen?</div>`,
        buttons:
            '<button class="w2ui-btn" id="btnCancel">Abbrechen</button> <button class="w2ui-btn" id="btnOk">Löschen</button>',
        width: 280,
        height: 180,
        onOpen: function (event) {
            event.onComplete = function () {
                $('#btnCancel').on('click', () => w2popup.close());
                $('#btnOk').on('click',
                    async () => {
                        w2popup.close();
                        try {
                            await fetch(`${apiBaseScripts}/${id}`, { method: 'DELETE' });
                            if (window.__scriptingEditor) {
                                const model = window.__scriptingEditor.getModel();
                                const scriptId = model.scriptId;
                                if (scriptId === id) {
                                    window.__scriptingEditor.setModel(null);
                                    model.dispose();
                                }
                            }
                            await window.__scriptingFunctions.fetchScripts();
                        } catch (err) {
                            alert('Fehler beim Löschen des Skripts.');
                            console.error(err);
                        }
                    });
            };
        }
    });
};

window.__scriptingFunctions.activateScript = async function activateScript(id) {
    await fetch(`${apiBaseScripts}/${id}/activate`, { method: 'POST' });
    await window.__scriptingFunctions.fetchScripts();
    await window.__scriptingFunctions.loadScript(id);
}

window.__scriptingFunctions.deactivateScript = async function deactivateScript(id) {
    await fetch(`${apiBaseScripts}/${id}/deactivate`, { method: 'POST' });
    await window.__scriptingFunctions.fetchScripts();
    await window.__scriptingFunctions.loadScript(id);
}

window.__scriptingFunctions.__dehighlightListItems = function __dehighlightListItems() {
    document.querySelectorAll('#scriptList li').forEach(li => {
        li.classList.remove('selectedScript');
    });
}

window.__scriptingFunctions.__highlightListItem = function __highlightListItem(scriptId) {
    document.querySelectorAll('.railhq-script-' + scriptId).forEach(li => {
        li.classList.add('selectedScript');
    })
}

window.__scriptingFunctions.loadScript = async function loadScript(id) {
    const scriptData = await window.hqScriptRunner.getServerInfoAboutScript(id);
    const scriptName = scriptData.name;
    const scriptCode = scriptData.code;

    // show scriptName in label


    const uri = monaco.Uri.parse(`inmemory://model/${scriptName}.js`);
    let model = monaco.editor.getModel(uri);
    if (!model) {
        model = monaco.editor.createModel(scriptCode, 'javascript', uri);
        model.scriptId = id;
    }
    window.__scriptingEditor.setModel(model);

    window.__scriptingFunctions.__updateFilenameLabel();

    window.__scriptingFunctions.__dehighlightListItems();
    window.__scriptingFunctions.__highlightListItem(id);
}

window.__scriptingFunctions.saveScriptContent = async function saveScriptContent() {
    if (__isModified === false) return; // nichts tun wenn es keine Änderungen gab

    const model = window.__scriptingEditor.getModel();
    if (!model) {
        window.__scriptingFunctions.__setStatusMessage("Kein Skript ausgewählt.");
        return;
    }
    const scriptCode = model.getValue();
    const scriptId = model.scriptId;
    await fetch(`${apiBaseScripts}/${scriptId}/content`,
        {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code: scriptCode })
        });
    window.__scriptingFunctions.__setStatusMessage("Gespeichert!");
    window.__scriptingFunctions.__updateFilenameLabel();
}

window.__scriptingFunctions.__renderScripts = function (scripts) {
    const list = document.getElementById('scriptList');
    list.innerHTML = '';

    if (scripts == null) return list;
    if (!Array.isArray(scripts)) return list;
    if (scripts.length == 0) return list;

    scripts.forEach(script => {
        const li = document.createElement('li');
        li.className = `script-item railhq-script-${script.id}`;

        li.dataset.id = script.id;
        li.innerHTML = `
  <span class="drag-handle" title="Ziehen zum Sortieren">
    <i class="fas fa-grip-lines"></i>
  </span>
  <span class="script-name" onclick="window.__scriptingFunctions.loadScript(${script.id})">
    ${script.name}
  </span>
  <div class="script-actions">
    <button onclick="window.__scriptingFunctions.renameScript(${script.id
            })" title="Umbenennen" class="btn rename-btn" type="button">
      <i class="fas fa-edit"></i>
    </button>
    <button
      onclick="window.__scriptingFunctions.${script.isActive ? 'deactivate' : 'activate'}Script(${script.id})" title="${script.isActive
                ? 'Deaktiviere das Starten beim Laden des Gleisplans'
                : 'Starte Skript beim Laden des Gleisplans'}" 
      class="btn ${script.isActive ? 'active-btn' : 'inactive-btn'}"
      type="button">
      <i class="fas ${script.isActive ? 'fa-toggle-off' : 'fa-toggle-on'}"></i>
    </button>
    <button onclick="window.__scriptingFunctions.deleteScript(${script.id
            })" title="Entfernen" class="btn delete-btn" type="button">
      <i class="fas fa-trash"></i>
    </button>
  </div>
`;

        list.appendChild(li);
    });

    window.__scriptingFunctions.__makeScriptsListSortable();
}

window.__scriptingFunctions.__makeScriptsListSortable = function () {
    $("#scriptList").sortable({
        handle: ".drag-handle",
        update: function (event, ui) {
            const orderedIds = $("#scriptList")
                .children("li")
                .map(function (index, el) {
                    return {
                        id: parseInt(el.dataset.id),
                        order: $("#scriptList").children().length - index // höher = oben
                    };
                }).get();

            $.ajax({
                url: `${apiBaseScripts}/reorder`,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify(orderedIds),
                success: function () {
                    console.log("Sortierung gespeichert");
                },
                error: function () {
                    w2alert('Fehler beim Speichern der Sortierung, bitte dem Support melden.',
                        'Fehler beim Sortieren');
                    console.error("Fehler beim Speichern der Sortierung");
                }
            });
        }
    });
}

window.__scriptingFunctions.__setStatusMessage = function (msg, timeout = 3000) {
    const status = document.getElementById('statusBarScripting');
    status.textContent = msg;
    if (timeout > 0) {
        setTimeout(() => {
            if (status.textContent === msg) {
                status.textContent = 'Bereit';
            }
        },
            timeout);
    }
}

let __isModified = false;

window.__scriptingFunctions.__updateFilenameLabel = function (isModified) {
    __isModified = isModified ? isModified : false;
    const filenameLabel = document.getElementById('filenameLabel');
    const model = window.__scriptingEditor.getModel();
    const fileName = model._associatedResource.path.replace('/', '').replace('.js', '');
    filenameLabel.textContent = fileName + (__isModified ? '*' : '');
}

async function loadScripting() {
    return new Promise((resolve, reject) => {

        if (window.__scriptingIsLoaded === true) {
            resolve();
            return;
        }
        window.__scriptingIsLoaded = true;

        let editor;

        window.MonacoEnvironment = {
            getWorkerUrl: function (moduleId, label) {
                if (label === 'javascript' || label === 'typescript') {
                    return '/scriptEngine/monaco/vs/language/typescript/tsWorker.js';
                }
                return '/scriptEngine/monaco/vs/base/worker/workerMain.js';
            }
        };

        require.config({ paths: { 'vs': '/scriptEngine/monaco/vs' } });
        self.MonacoEnvironment = {
            getWorkerUrl: function (moduleId, label) {
                return '/scriptEngine/monaco/vs/base/worker/workerMain.js';
            }
        };
        require(['vs/editor/editor.main'],
            async function () {
                editor = monaco.editor.create(document.getElementById('editorScripting'),
                    {
                        value: '',
                        language: 'javascript',
                        theme: 'vs-light',
                        automaticLayout: true,

                        // Bedienkomfort und Benutzerfreundlichkeit
                        //wordWrap: 'on', // Zeilenumbruch bei langen Zeilen
                        //minimap: { enabled: false }, // Minimiert Ablenkung
                        scrollBeyondLastLine: false, // Kein unnötiger Leerraum am Ende
                        lineNumbers: 'on', // Zeilennummern anzeigen
                        tabSize: 4, // Bessere Lesbarkeit (besonders für JS)
                        insertSpaces: true, // Tabs als Leerzeichen
                        //renderWhitespace: 'boundary', // Nur sichtbare Leerzeichen anzeigen
                        cursorSmoothCaretAnimation: true, // Weiche Cursor-Animation

                        // Look & Feel / Visuals
                        fontSize: 14, // Gut lesbare Schriftgröße
                        fontFamily: 'Fira Code, monospace', // Moderne Programmier-Schrift
                        fontLigatures: true, // Schönere Darstellung von ===, => etc.
                        renderLineHighlight: 'line', // Zeile mit Cursor hervorheben

                        // Intelligentere Eingabehilfen
                        suggestOnTriggerCharacters: true, // Wichtig für Autocomplete mit Klammern
                        snippetSuggestions: "inline",
                        hover: {
                            enabled: true
                        },
                        inlineSuggest: {
                            enabled: true
                        },
                        quickSuggestions: {
                            other: true,
                            comments: false,
                            strings: true
                        },
                        acceptSuggestionOnEnter: 'smart', // Nur mit Enter bestätigen, wenn sinnvoll
                        suggestSelection: 'first', // Immer ersten Vorschlag vorausgewählt
                        parameterHints: {
                            enabled: true
                        },
                        autoClosingBrackets:
                            "always", // Automatische Klammern (nicht nur bei Autocomplete, sondern generell)

                        // Editor-Verhalten
                        formatOnPaste: true, // Automatisch Formatieren beim Einfügen
                        formatOnType: true, // Formatieren beim Tippen (z. B. nach `;`)
                        codeLens: true, // Unterstützt zusätzliche Infos (z. B. Referenzen)
                    });

                window.__scriptingEditor = editor;

                editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS,
                    () => {
                        window.__scriptingFunctions.saveScriptContent();
                    });

                editor.onDidChangeModelContent(() => {
                    window.__scriptingFunctions.__updateFilenameLabel(true);
                });

                let currentFontSize = window.__scriptingEditor.getOption(monaco.editor.EditorOption.fontSize) || 14;
                const updateFontSize = function (newSize) {
                    const minFontSize = 8;
                    const maxFontSize = 40;
                    if (newSize < minFontSize || newSize > maxFontSize) return;
                    currentFontSize = newSize;
                    window.__scriptingEditor.updateOptions({ fontSize: currentFontSize });
                }

                // Text-Zoom
                document.getElementById('zoomInBtn').addEventListener('click', () => {
                    updateFontSize(currentFontSize + 1);
                });
                document.getElementById('zoomOutBtn').addEventListener('click', () => {
                    updateFontSize(currentFontSize - 1);
                });

                editor.onDidChangeCursorPosition(updateCursorPosition);
                updateCursorPosition();

                await loadAndSetTypedefs();
                await loadMonacoSamples();

                // Optionale Konfiguration: IntelliSense etwas schärfer machen
                monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
                    allowNonTsExtensions: true,
                    target: monaco.languages.typescript.ScriptTarget.ES2020,
                    checkJs: true,
                });

                monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
                    noSemanticValidation: true,
                    noSyntaxValidation: false
                });

                // Editor ist jetzt bereit → Jetzt Skripte laden
                window.__scriptingFunctions.fetchScripts();

                window.hqScriptRunner.initTaskGrid();

                setTimeout(() => {
                    document.getElementById('createButton').addEventListener('click', createScript);
                    document.getElementById('saveButton')
                        .addEventListener('click', window.__scriptingFunctions.saveScriptContent);
                    window.addEventListener('resize',
                        () => {
                            if (editor) editor.layout();
                        });
                    document.getElementById('editorScripting').style.display = '';
                    document.getElementById('loader').style.display = 'none';
                    document.getElementById('runButton').addEventListener('click',
                        async () => {
                            window.__scriptingFunctions.saveScriptContent();

                            const model = window.__scriptingEditor.getModel();
                            const markers = monaco.editor.getModelMarkers({ resource: model.uri });
                            const syntaxErrors = markers.filter(m => m.severity === monaco.MarkerSeverity.Error);
                            const code = model.getValue();
                            const scriptId = model.scriptId;
                            const scriptData = await window.hqScriptRunner.getServerInfoAboutScript(scriptId);
                            const scriptName = scriptData ? scriptData.name || '' : '';

                            // Wenn Syntaxfehler vorhanden sind, frage nach Bestätigung
                            if (syntaxErrors.length > 0) {
                                const errorMessages = syntaxErrors.map(err =>
                                    `🔴 Zeile ${err.startLineNumber}, Spalte ${err.startColumn}: ${err.message}`
                                ).join('<br>');

                                w2confirm({
                                    title: 'Syntaxfehler im Skript',
                                    msg: `
                <div style="color:#b94a48;margin-bottom:8px;">
                    Das Skript enthält ${syntaxErrors.length} Syntaxfehler:
                </div>
                <div style="font-family:monospace;font-size:90%;max-height:200px;overflow:auto;">
                    ${errorMessages}
                </div>
                <br>Möchtest du es trotzdem ausführen?
            `,
                                    no_text: "Abbrechen",
                                    yes_text: "Ja, trotzdem ausführen"
                                }).yes(() => {
                                    window.hqScriptRunner.startScript(code, scriptName);
                                });

                                return; // Warten auf Benutzereingabe – nicht direkt ausführen
                            }

                            // Keine Syntaxfehler → direkt ausführen
                            window.hqScriptRunner.startScript(code, scriptName);
                        });

                    installScriptingDragger();

                    resolve();
                },
                    250);
            });

        async function createScript() {
            window.__scriptingFunctions.askScriptName({},
                async function (scriptData) {
                    if (!scriptData && !scriptData.name) return;
                    const defaultSampleCode = scriptData.skipExampleSource ? defaultCodeEmpty : defaultCode;
                    const res = await fetch(apiBaseScripts,
                        {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({
                                name: scriptData.name,
                                description: scriptData.description,
                                code: defaultSampleCode
                            })
                        });
                    if (!res.ok) {
                        console.error('Fehler beim Erstellen des Skripts');
                        return;
                    }

                    const data = await res.json();
                    const newId = data.id;

                    await window.__scriptingFunctions.fetchScripts();
                    await window.__scriptingFunctions.loadScript(newId);
                });
        }

        function updateCursorPosition() {
            const pos = editor.getPosition();
            $('#cursorPosition').text(`Zeile ${pos.lineNumber}, Spalte ${pos.column}`);
        }

        function installScriptingDragger() {
            const dragbar = document.getElementById('dragbarScripting');
            const editor = document.getElementById('editorScripting');
            const output = document.getElementById('scriptOutput');
            const container = document.querySelector('.main-content');

            let isDragging = false;
            let dragOffsetY = 0;

            dragbar.addEventListener('mousedown',
                (e) => {
                    isDragging = true;
                    // Abstand der Maus zum oberen Rand des Splitters merken
                    dragOffsetY = e.clientY - dragbar.getBoundingClientRect().top;
                    document.body.style.userSelect = 'none';
                });

            document.addEventListener('mouseup',
                () => {
                    if (isDragging) {
                        isDragging = false;
                        document.body.style.userSelect = '';
                    }
                });

            document.addEventListener('mousemove',
                (e) => {
                    if (!isDragging) return;
                    const strangeOffsetDuringDrag = 35;
                    const containerRect = container.getBoundingClientRect();
                    const splitterHeight = dragbar.offsetHeight;
                    let newEditorHeight = e.clientY - containerRect.top - dragOffsetY;
                    editor.style.height = newEditorHeight + 'px';
                    output.style.height =
                        (containerRect.height - newEditorHeight - splitterHeight - strangeOffsetDuringDrag) + 'px';
                });
        }


    });
}