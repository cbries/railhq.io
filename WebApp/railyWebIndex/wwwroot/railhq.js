// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


function isUserAgentMobile() {
    return /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
}

function isSmallScreen() {
    return window.innerWidth < 1024;
}

function checkMobile(event, url) {
    if (isUserAgentMobile()) {
        const proceed = confirm("Diese Steuerung ist nur für Desktops optimiert. Trotzdem fortfahren?");
        if (!proceed) {
            event.preventDefault();
            return false;
        }
    } else if (isSmallScreen()) {
        const proceed = confirm("Dein Browserfenster ist zu schmal. Bitte vergrößere die Ansicht für eine optimale Darstellung. Trotzdem fortfahren?");
        if (!proceed) {
            event.preventDefault();
            return false;
        }
    }
}

// Formatieren der Zeit (Beispiel "dd.MM.yyyy HH:mm:ss")
function formatDate(date, showYear = true) {
    var day = String(date.getDate()).padStart(2, '0');
    var month = String(date.getMonth() + 1).padStart(2, '0');
    var year = date.getFullYear();
    var hours = String(date.getHours()).padStart(2, '0');
    var minutes = String(date.getMinutes()).padStart(2, '0');
    var seconds = String(date.getSeconds()).padStart(2, '0');
    if (showYear === false)
        return `${day}.${month}. ${hours}:${minutes}:${seconds}`;
    return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}`;
}

//
// show planfield screenshot
//
function showPreview(apiUrl, wsName) {
    const imageUrl = `${apiUrl}/screenshot?wsName=${wsName}`;
    w2popup.open({
        title: 'Vorschau: ' + wsName,
        width: '800px',
        height: '600px',
        body: `<div style="display: flex; flex-direction: column; justify-content: center; align-items: center; height: 100%;">
        <img src="${imageUrl}" style="width: 100%; height: 100%; object-fit: contain;" />
        <!--
        <a id="download-link" href="${imageUrl}" download="screenshot.png" 
            style="margin-top: 10px; display: inline-block; padding: 8px 16px; background: #0078D7; color: white; text-decoration: none; border-radius: 4px;">
            Herunterladen
        </a>
        -->
        </div>`,
        actions: {
            Ok(event) {
                w2popup.close();
            }
        }
    });
}
