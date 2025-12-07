// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// globals to keep user configuration provided by server
window.settingsInfo = null;
window.systemInfo = null;
window.workspaceName = null;
window.stateData = null;

window.recentLocomotiveData = {}

// theming
window.themeData = null;
window.themeName = '';

// server communication
window.serverHandling = null;

// general error handling
window.errorHandler = new ErrorHandler();

// #region Command Handling

const refreshEntity = (data, name = "refreshEntity") => sendCommand(name, data);
const changeSetting = (data, name = "setting") => sendCommand(name, data);
const changeInventar = (data, name = "inventar") => sendCommand(name, data);

const sendAccessoryCommand = (data, name = "accessory") => sendCommand(name, data);
const sendAutoModeCommand = (data, name = "automode") => sendCommand(name, data);
const sendLocomotiveCommand = (data, name = "locomotive") => sendCommand(name, data);
const sendRoutingCommand = (data, name = "routing") => sendCommand(name, data);
const sendSystemCommand = (data, name = "system") => sendCommand(name, data);
const sendTrackplanCommand = (data, name = "trackplan") => sendCommand(name, data);
function sendCommand(commandCategory, data) {
    window.serverHandling?.send(__getCommandSkeleton({
        "command": commandCategory,
        "timestamp": Date.now(),
        "cmddata": data
    }));
}

function sendPing(data) {
    window.serverHandling?.send(data);
}

// #endregion

function __getCommandSkeleton(payloadJsonObject) {
    const skeleton = {
        "version": "0.1",
        "extensionName": "webClient",
        "timestamp": new Date().toISOString(),
        "data": {
            "type": "command",
            "payload": payloadJsonObject
        }
    };
    if (!skeleton.data.payload.objectId)
        skeleton.data.payload.objectId = -1;
    if (!skeleton.data.payload.command)
        skeleton.data.payload.command = 'update';
    return skeleton;
}

// #region Locomotive Data

const locomotiveFunctionEmojiList = {
    'Licht vorne/hinten': '💡',               // Glühbirne
    'Motor/Sound an/aus': '🔊',               // Lautsprecher mit Wellen
    'Horn / Pfeife kurz': '📯',               // Posthorn
    'Horn lang / Pfeife lang': '📯',          // Posthorn (gleiches Symbol)
    'Rangiermodus / Bremsmodus': '🚧',       // Baustelle / Vorsicht (für Rangierbetrieb)
    'Rauchgenerator ein/aus': '💨',          // Wind / Rauch
    'Entkuppler / Kupplung': '🔗',            // Kettenglied (Verbindung)
    'Führerstandsbeleuchtung': '🔦',          // Taschenlampe
    'Stummschalten / Lautsprecher aus': '🔇', // Lautsprecher mit durchgestrichenem Ton
    'Innenbeleuchtung': '🏮',                 // Lampion (für Innenlicht)
    'Kabinenlicht 2': '🏮',                   // Lampion (2. Licht)
    'Lüfter / Kühlung': '🌀',                 // Windwirbel (Lüfter)
    'Kompressor / Generator': '🔧',           // Schraubenschlüssel
    'Zugfunk / Radio': '📻',                  // Radio
    'Glocke / Glockensignal': '🔔',           // Glocke
    'Sandstreuer': '🏜️',                     // Wüste / Sand (passend)
    'Zugschlusslicht': '🚨',                  // Warnblinker
    'Spitzenlicht nur vorne': '⬆️',           // Pfeil nach oben
    'Spitzenlicht nur hinten': '⬇️',          // Pfeil nach unten
    'Warnsignal / Hupe': '⚠️',                 // Warnschild
    'Rangierpfiff': '🎵',                     // Musiknote (Signalton)
    'Lüfter 2': '🌀',                         // Windwirbel (Lüfter 2)
    'Automatik-Kupplung': '🧲',               // Magnet
    'Pantograph heben/senken': '⚡',           // Blitz (Stromabnehmer)
    'Lichtwechsel Weiß/Rot': '🔄',            // Pfeil-Kreis (Wechsel)
    'Türgeräusch': '🚪',                      // Tür
    'Bremsgeräusch': '🎶',                    // Musiknoten (Geräusche)
    'Generator-Lüfter': '⚙️',                 // Zahnrad
    'Zugführerpfiff': '👨‍✈️',                 // Zugführer (Pilot-Emoji)
    'Benutzerdefiniert 1': '⚙️',
    'Benutzerdefiniert 2': '⚙️',
    'Benutzerdefiniert 3': '⚙️'
};

const locomotiveFunctionDescriptionList = [
    'Licht vorne/hinten',
    'Motor/Sound an/aus',
    'Horn / Pfeife kurz',
    'Horn lang / Pfeife lang',
    'Rangiermodus / Bremsmodus',
    'Rauchgenerator ein/aus',
    'Entkuppler / Kupplung',
    'Führerstandsbeleuchtung',
    'Stummschalten / Lautsprecher aus',
    'Innenbeleuchtung',
    'Kabinenlicht 2',
    'Lüfter / Kühlung',
    'Kompressor / Generator',
    'Zugfunk / Radio',
    'Glocke / Glockensignal',
    'Sandstreuer',
    'Zugschlusslicht',
    'Spitzenlicht nur vorne',
    'Spitzenlicht nur hinten',
    'Warnsignal / Hupe',
    'Rangierpfiff',
    'Lüfter 2',
    'Automatik-Kupplung',
    'Pantograph heben/senken',
    'Lichtwechsel Weiß/Rot',
    'Türgeräusch',
    'Bremsgeräusch',
    'Generator-Lüfter',
    'Zugführerpfiff',
    'Benutzerdefiniert 1',
    'Benutzerdefiniert 2',
    'Benutzerdefiniert 3'
];

const locomotiveFunctionIconList = {
    'Licht vorne/hinten': 'fas fa-lightbulb',
    'Motor/Sound an/aus': 'fas fa-volume-up',
    'Horn / Pfeife kurz': 'fas fa-bullhorn',
    'Horn lang / Pfeife lang': 'fas fa-bullhorn',
    'Rangiermodus / Bremsmodus': 'fas fa-walking',
    'Rauchgenerator ein/aus': 'fas fa-wind',
    'Entkuppler / Kupplung': 'fas fa-link',
    'Führerstandsbeleuchtung': 'fas fa-lightbulb',
    'Stummschalten / Lautsprecher aus': 'fas fa-volume-mute',
    'Innenbeleuchtung': 'fas fa-lightbulb',
    'Kabinenlicht 2': 'fas fa-lightbulb',
    'Lüfter / Kühlung': 'fas fa-fan',
    'Kompressor / Generator': 'fas fa-wrench',
    'Zugfunk / Radio': 'fas fa-broadcast-tower',
    'Glocke / Glockensignal': 'fas fa-bell',
    'Sandstreuer': 'fas fa-cloud',
    'Zugschlusslicht': 'fas fa-exclamation',
    'Spitzenlicht nur vorne': 'fas fa-arrow-up',
    'Spitzenlicht nur hinten': 'fas fa-arrow-down',
    'Warnsignal / Hupe': 'fas fa-exclamation',
    'Rangierpfiff': 'fas fa-music',
    'Lüfter 2': 'fas fa-fan',
    'Automatik-Kupplung': 'fas fa-magnet',
    'Pantograph heben/senken': 'fas fa-bolt',
    'Lichtwechsel Weiß/Rot': 'fas fa-exchange-alt',
    'Türgeräusch': 'fas fa-door-closed',
    'Bremsgeräusch': 'fas fa-music',
    'Generator-Lüfter': 'fas fa-cog',
    'Zugführerpfiff': 'fas fa-user-tie',
    'Benutzerdefiniert 1': 'fas fa-cogs',
    'Benutzerdefiniert 2': 'fas fa-cogs',
    'Benutzerdefiniert 3': 'fas fa-cogs'
};

// #endregion
