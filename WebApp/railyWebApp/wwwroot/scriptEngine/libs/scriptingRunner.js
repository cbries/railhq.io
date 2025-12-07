window.hqScriptRunner = (() => {
    const tasks = {}; // id -> Task-Objekt
    let nextTaskId = 1;
    const labelOfRunningTask = "laufend";

    let autoloadExecuted = false;

    async function runAutoload() {
        if (autoloadExecuted === true) {
            return;
        }
        autoloadExecuted = true;
        try {
            const res = await fetch(apiBaseScripts);
            const s = await res.json();
            const autoLoadScripts = [];
            for (let i = 0; i < s.length; ++i) {
                const ss = s[i];
                if (ss.isActive === true) {
                    autoLoadScripts.push(ss);
                }
            }

            if (autoLoadScripts.length > 0) {
                console.log("Load Skripte...");
            }

            for (let i = 0; i < autoLoadScripts.length; ++i) {
                const ss = autoLoadScripts[i];
                if (ss.isActive === true) {
                    try {
                        await loadScripting();  // Jetzt wartet runAutoload wirklich, bis loadScripting fertig ist
                    } catch (e) {
                        console.error('Failed to load scripting:', e);
                    }

                    startScript(ss.code, ss.name);
                }
            }
        } catch {
            // ignore
        }
    }

    function __appendToOutput(idOutputPane, text, type = 'log') {
        const outputContainer = document.getElementById(`scriptOutput-${idOutputPane}`);
        if (!outputContainer) {
            console.warn(`Kein Output-Container für ID scriptOutput-${idOutputPane} gefunden.`);
            return;
        }
        const span = document.createElement('span');
        span.textContent = text + "\n";
        span.style.whiteSpace = "pre-wrap";
        switch (type) {
            case 'warn':
                span.classList.add('script-warn');
                break;
            case 'error':
                span.classList.add('script-error');
                break;
            default:
                span.classList.add('script-log');
        }
        outputContainer.appendChild(span);
        outputContainer.scrollTop = outputContainer.scrollHeight;
    }

    function __isSyntaxValid(code, lineOffset = 0) {
        try {
            new Function(code);
            return { valid: true };
        } catch (e) {
            const result = {
                valid: false,
                message: e.message,
                name: e.name,
                line: null,
                column: null,
                humanReadable: []
            };

            // Versuche, Zeile und Spalte aus dem Fehlerstack zu extrahieren
            if (e.stack) {
                const match = e.stack.match(/<anonymous>:(\d+):(\d+)/);
                if (match) {
                    const rawLine = parseInt(match[1], 10);
                    result.line = rawLine - lineOffset;
                    result.column = parseInt(match[2], 10);
                }
            }

            result.humanReadable = [
                `🚫 Syntaxfehler im Skript${result.line ? ` (Zeile ${result.line}, Spalte ${result.column})` : ''}:`,
                `→ ${result.name}: ${result.message}`
            ];

            return result;
        }
    }


    async function startScript(code, scriptName = "Unbenanntes Skript") {
        // Prüfen, ob Skript schon läuft (hier kannst du z.B. nur ein Skript parallel zulassen)
        if (Object.values(tasks).some(t =>
            t.skript === scriptName
            && (t.status === 'laufend' || t.status === 'running' || t.status === 'started'))) {
            console.warn("[hqScriptRunner] Ein Skript läuft bereits, kein Mehrfachstart erlaubt.");
            return;
        }

        console.log("Start Skript: " + scriptName);

        const id = nextTaskId++;
        const abortController = new AbortController();
        const startTime = new Date();

        // Output-Container erstellen
        const outputContainer = document.createElement('div');
        outputContainer.id = `scriptOutput-${id}`;
        outputContainer.style.padding = "6px";
        outputContainer.innerHTML = `<b>Skript #${id} gestartet um ${startTime.toLocaleTimeString()}</b><br>`;
        document.getElementById('scriptOutput').appendChild(outputContainer);

        __showTaskInOutput(id);

        const scopedConsole = {
            log: (...args) => __appendToOutput(id, args.join(' '), 'log'),
            warn: (...args) => __appendToOutput(id, args.join(' '), 'warn'),
            error: (...args) => __appendToOutput(id, args.join(' '), 'error'),
        };

        const hqApi = createHqApi();
        const hqApiCode = generateDestructuringCode('api', hqApi);
        const hqHeaderCode = `if (typeof api.hqHelper.finalize !== "function") {
                api.hqHelper.finalize = async () => {};
            }
            if (typeof api.hqHelper.onError !== "function") {
                api.hqHelper.onError = async (err) => {
                    console.error("Fehler im Skript:", err);
                };
            }

            return (async () => {
                try {
                    if (hqAbortSignal.aborted) throw new DOMException("Abgebrochen", "AbortError");
                    const sleep = (ms) => new Promise((r, j) => {
                        const id = setTimeout(r, ms);
                        hqAbortSignal.addEventListener('abort', () => {
                            clearTimeout(id);
                            j(new DOMException("Abgebrochen", "AbortError"));
                        });
                    });

                    // Nutzer-Skript
                    `;
        const hqFooterCode = `
        } catch (e) {
                    try {
                        await api.hqHelper.onError(e);
                    } catch (handlerError) {
                        error("Fehler im onError-Handler:", handlerError);
                    }
                    throw e;
                } finally {
                    try {
                        await api.hqHelper.finalize();
                    } catch (cleanupError) {
                        error("Fehler im Cleanup:", cleanupError);
                    }
                }
            })();
        `;
        const generatedCode =
            hqApiCode +
            hqHeaderCode +
            code +
            hqFooterCode;

        const lineOffset =
            (hqApiCode.match(/\n/g) || []).length +
            (hqHeaderCode.match(/\n/g) || []).length;
        const resValidSyntax = __isSyntaxValid(generatedCode, lineOffset);
        if (!resValidSyntax.valid) {
            scopedConsole.error(...resValidSyntax.humanReadable);
            __updateTaskStatus(id, 'fehlerhaft');
            return;
        }

        const wrappedCode = new Function('api', 'hqAbortSignal', 'log', 'warn', 'error', generatedCode);

        const promise = wrappedCode(
            hqApi,
            abortController.signal,
            scopedConsole.log,
            scopedConsole.warn,
            scopedConsole.error
        ).then(() => {
            __updateTaskStatus(id, 'erfolgreich');
        }).catch(e => {
            if (e.name === 'AbortError') {
                __updateTaskStatus(id, 'abgebrochen');
                __appendToOutput(id, 'Skript wurde abgebrochen', 'warn');
            } else {
                __updateTaskStatus(id, 'fehlerhaft');
                __appendToOutput(id, 'Fehler: ' + e.message, 'error');
            }
        }).finally(() => {
            __refreshTaskGrid();
        });

        tasks[id] = { id, skript: scriptName, code, startTime, status: labelOfRunningTask, abortController, promise, hqApi };
        __refreshTaskGrid();
        return id;
    }

    function abortScript(id) {
        if (tasks[id]?.abortController) {
            tasks[id].abortController.abort();
        }
        if (tasks[id]?.hqApi) {
            tasks[id].hqApi.stop();
        }
    }

    function abortAllScripts() {
        Object.keys(tasks).forEach(abortScript);
    }

    function __updateTaskStatus(id, status) {
        if (tasks[id]) {
            tasks[id].status = status;
        }
        __refreshTaskGrid();
    }

    async function getServerInfoAboutScript(scriptId) {
        const res = await fetch(`${apiBaseScripts}/${scriptId}`);
        const data = await res.json();
        return data;
    }

    function initTaskGrid() {
        if (w2ui.taskGrid) return;

        $('#removeInactiveBtn').on('click', function () {
            const allRecords = w2ui['taskGrid'].records;
            const activeRecords = allRecords.filter(r => r.status === labelOfRunningTask);
            w2ui['taskGrid'].clear();
            w2ui['taskGrid'].add(activeRecords);

            // Aktualisiere das globale tasks-Objekt
            // Entferne alle tasks, die NICHT `labelOfRunningTask` sind
            for (const id in tasks) {
                if (tasks[id].status !== labelOfRunningTask) {
                    delete tasks[id];
                }
            }

            // Entferne alle <div>-Elemente mit id, die mit "scriptOutput-" beginnen
            $('div[id^="scriptOutput-"]').remove();
        });

        $('#taskGrid').w2grid({
            name: 'taskGrid',
            show: {
                toolbar: false,
                footer: false
            },
            columns: [
                { field: 'id', caption: 'ID', size: '10%' },
                { field: 'skript', caption: 'Skript', size: '40%' },
                { field: 'startTime', caption: 'Startzeit', size: '15%' },
                { field: 'status', caption: 'Status', size: '25%' },
                {
                    field: 'actions',
                    caption: 'Aktion',
                    size: '10%',
                    render: (record) => {
                        if (record.status === labelOfRunningTask) {
                            return `<div style="text-align: center;">
                        <button onclick="window.hqScriptRunner.abortScript(${record.id})" title="Abbrechen">✖</button>
                    </div>`;
                        }
                        return '<div style="text-align: center;">-</div>';
                    }
                }
            ],
            records: [],
            onClick: function (event) {
                const record = this.get(event.recid);
                const taskId = record.id;
                __showTaskInOutput(taskId);
            }
        });
    }

    function __showTaskInOutput(taskId) {
        // Alle Output-Container ausblenden
        document.querySelectorAll('[id^="scriptOutput-"]').forEach(el => {
            el.style.display = 'none';
        });

        // Den gewünschten Output-Container einblenden
        const selectedOutput = document.getElementById(`scriptOutput-${taskId}`);
        if (selectedOutput) {
            selectedOutput.style.display = 'block';
        }
    }

    async function __refreshTaskGrid() {
        if (!w2ui.taskGrid) return;
        const records = await Promise.all(
            Object.values(tasks).map(async (t) => ({
                recid: t.id,
                id: t.id,
                skript: t.skript,
                startTime: t.startTime.toLocaleTimeString(),
                status: t.status
            }))
        );
        w2ui.taskGrid.records = records;
        w2ui.taskGrid.refresh();
    }

    function generateDestructuringCode(apiObjectName, obj) {
        const keys = Object.keys(obj);
        return `const {\n    ${keys.join(',\n    ')}\n} = ${apiObjectName};\n`;
    }

    return {
        runAutoload,
        startScript,
        abortScript,
        abortAllScripts,
        getServerInfoAboutScript,
        initTaskGrid,
        refreshTaskGrid: __refreshTaskGrid,
        isRunning: () => Object.values(tasks).some(t => t.status === labelOfRunningTask),
        getTasks: () => ({ ...tasks })
    };
})();
