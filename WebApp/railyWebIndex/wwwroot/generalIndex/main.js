// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

//
// Hilfsfunktion: Datei als Base64 kodieren
//
function encodeFileToBase64(file) {
    const reader = new FileReader();
    const deferred = $.Deferred();
    reader.onload = function () {
        const base64String = reader.result.split(',')[1];
        deferred.resolve(base64String);
    };
    reader.onerror = function (error) {
        deferred.reject(error);
    };
    reader.readAsDataURL(file);
    return deferred.promise();
}

//
// Fehler anzeigen
//
function showError(message) {
    if ($("#errorImportOverlay").is(":hidden")) {
        $("#errorImportOverlay .errorMessage").html(message);
        $("#errorImportOverlay").css({ display: "flex" });
    }
}

function getFormattedText(text) {
    return formatTextToHTML(text);
}

function formatTextToHTML(text, doLineBreaks = true) {
    if (doLineBreaks === true)
        text = text.replace(/\r?\n/g, '<br>');                              // CRLF & LF zu <br>

    text = highlightHashtags(text);

    return text
        .replace(/\[b\](.*?)\[\/b\]/gis, '<strong>$1</strong>') // Fett (case-insensitive & multiline)
        .replace(/\[i\](.*?)\[\/i\]/gis, '<em>$1</em>')         // Kursiv
        .replace(/\[u\](.*?)\[\/u\]/gis, '<u>$1</u>')           // Unterstrichen
        .replace(/\[s\](.*?)\[\/s\]/gis, '<s>$1</s>')           // Durchgestrichen
        .replace(/\[code\](.*?)\[\/code\]/gis, '<code>$1</code>') // Code
        .replace(/\[url=(.*?)\](.*?)\[\/url\]/gis, '<a href="$1" target="_blank">$2</a>') // Links
        .replace(/\[h([1-6])\](.*?)\[\/h\1\]/gis, '<h$1>$2</h$1>') // Ueberschriften h1-h6
        .replace(/\[color=(.*?)\](.*?)\[\/color\]/gis, '<span style="color:$1;">$2</span>') // Farben
        .replace(/\[quote\](.*?)\[\/quote\]/gis, '<blockquote>$1</blockquote>') // Zitate
        .replace(/\[ul\](.*?)\[\/ul\]/gis, '<ul>$1</ul>')     // Ungeordnete Liste
        .replace(/\[ol\](.*?)\[\/ol\]/gis, '<ol>$1</ol>')     // Geordnete Liste
        .replace(/\[li\](.*?)\[\/li\]/gis, '<li>$1</li>');     // Listeneintraege 
}

function highlightHashtags(text) {
    return text.replace(/#(\w+)/g, '<span class="hashtag">#$1</span>');
}
