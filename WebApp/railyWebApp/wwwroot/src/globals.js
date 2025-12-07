// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const constItemWidth = 32;
const constItemHeight = 32;
const constDataThemeDimensionIndex = "theme-dimension-index";
const constDataThemeItemObject = "theme-item-object";
const constDataThemeId = "theme-id";
const constDataConnectorId = "connector-id";
const constDataSensorProvider = "sensor-provider";
const constDataSensorAddress = "sensor-address";
const constDataStateLock = "is-locked";
const constDataStateEnabled = "is-enabled";
const constDataStateIsMaintenance = "is-maintenance";
const constDataStateCommuterPlus = "is-commuter-plus";
const constDataStateCommuterMinus = "is-commuter-minus";
const constKeyEnter = 13;
const constKeyEscape = 27;

// Configurable URLs - can be overridden via window.railhqConfig (set by server)
const constGitWebsite = (window.railhqConfig && window.railhqConfig.websiteUrl) || "https://railhq.io/";
const constGitWikiWebsite = (window.railhqConfig && window.railhqConfig.wikiUrl) || "https://railhq.io/Support";
const constAboutWebsite = "https://www.riesolution.de";

const BgNeutralButtons = "bg-gray-300";
const BgOffFncButtons = "bg-red-400";
const BgOnFncButtons = "bg-green-400";
const BgOffPowerButtons = "bg-red-500";
const BgOnPowerButtons = "bg-green-500";

const ListOfFunctionDescription = {
    2: "Function",
    3: "Light",
    4: "Light", // Light_0
    5: "Light", // Light_1
    7: "Sound",
    8: "Music",
    9: "Announce",
    10: "Routing Speed",
    11: "abv",
    32: "Coupler",
    33: "Steam",
    34: "Panto",
    35: "Highbeam",
    36: "Bell",
    37: "Horn",
    38: "Whistle",
    39: "Door Sound",
    40: "Fan",
    42: "Shovel Work Sound",
    44: "Shift",
    260: "Interior Lighting",
    261: "Plate Light",
    263: "Brakesound",
    299: "Crane Raise Lower",
    555: "Hook Up Down",
    773: "Wheel Light",
    811: "Turn",
    1031: "Steam Blow",
    1033: "Radio Sound",
    1287: "Coupler Sound",
    1543: "Track Sound",
    1607: "Notch up",
    1608: "Notch down",
    2055: "Thunderer Whistle",
    3847: "Buffer Sound"
};

const ListOfFunctionIcons = {
    2: 'fas fa-cogs', // Generic Function
    3: 'fas fa-lightbulb',
    4: 'fas fa-lightbulb',
    5: 'fas fa-lightbulb',
    7: 'fas fa-volume-up',
    8: 'fas fa-music',
    9: 'fas fa-door-closed',
    10: 'fas fa-tachometer-alt',
    11: 'fas fa-cogs', // unknown "abv"
    32: 'fas fa-link',
    33: 'fas fa-wind',
    34: 'fas fa-bolt',
    35: 'fas fa-highlighter', // Highbeam as workaround
    36: 'fas fa-bell',
    37: 'fas fa-bullhorn',
    38: 'fas fa-bullhorn',
    39: 'fas fa-door-closed',
    40: 'fas fa-fan',
    42: 'fas fa-shovel', // kein offizielles Icon, evtl. 'fa-tools' oder 'fa-hard-hat'
    44: 'fas fa-exchange-alt',
    260: 'fas fa-lightbulb',
    261: 'fas fa-lightbulb',
    263: 'fas fa-music',
    299: 'fas fa-arrow-up-down', // Crane raise/lower workaround
    555: 'fas fa-anchor', // Hook symbolisch
    773: 'fas fa-lightbulb',
    811: 'fas fa-sync-alt', // Turn
    1031: 'fas fa-smog', // Steam blow
    1033: 'fas fa-broadcast-tower',
    1287: 'fas fa-volume-up', // Coupler Sound
    1543: 'fas fa-train', // Track sound
    1607: 'fas fa-arrow-up',
    1608: 'fas fa-arrow-down',
    2055: 'fas fa-bullhorn', // Thunderer whistle
    3847: 'fas fa-compact-disc' // Buffer sound als Ersatz
};
