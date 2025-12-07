// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

window.__cropperLocomotive = null;
window.__cropperLocomotiveName = null;
window.__cropperLocomotiveNameHash = null;
window.__croppertPostImageUrl = null;
window.__cropperBearerToken = null;

window.__tmpDataCropper_Width = 0;
window.__tmpDataCropper_Height = 0;
window.__tmpDataCropper_fileSizeInBytes = 0;

window.__cropperFlipX = false;
window.__cropperFlipY = false;

async function generateSHA256Hash(input) {
    const encoder = new TextEncoder();
    const data = encoder.encode(input);
    const hashBuffer = await crypto.subtle.digest("SHA-256", data);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    const hashHex = hashArray.map(b => b.toString(16).padStart(2, "0")).join("");
    return hashHex;
}

function __initCropper() {
    const dropTarget = $('#image-container');

    dropTarget.on('dragover', function (event) {
        event.preventDefault();
        event.stopPropagation();
        $(this).addClass('border-blue-400 bg-blue-50');
    });

    dropTarget.on('dragleave', function (event) {
        event.preventDefault();
        event.stopPropagation();
        $(this).removeClass('border-blue-400 bg-blue-50');
    });

    dropTarget.on('drop', function (event) {
        event.preventDefault();
        event.stopPropagation();
        $(this).removeClass('border-blue-400 bg-blue-50');

        var file = event.originalEvent.dataTransfer.files[0];
        if (file && file.type.startsWith('image/')) {

            __handleImage(window.__cropperLocomotiveName, file, window.__cropperBearerToken);

        } else {
            console.log('Bitte ein Bild verwenden!');
        }
    });
}

function __loadMetadata(objectURL) {
    const img = new Image();
    img.onload = function () {
        window.__tmpDataCropper_Width = this.width;
        window.__tmpDataCropper_Height = this.height;
    };
    img.src = objectURL;
}

function __loadFileCropperHandling() {
    const image = $('#image');
    let previewReady = false;

    if (window.__cropperLocomotive) {
        window.__cropperLocomotive.destroy();
    }
    window.__cropperLocomotive = new Cropper(image[0], {
        zoomable: false,
        scalable: false,
        responsive: false,
        checkCrossOrigin: true,
        ready: function () {
            showCropperSize();

            const cropper = this;

            window.__cropperLocomotive.setData({
                x: 0,
                y: 0,
                width: window.__tmpDataCropper_Width,
                height: window.__tmpDataCropper_Height
            });

            let clone = image[0].cloneNode();
            clone.className = '';
            clone.style.cssText = `
                        display: block;
                        width: 100%;
                        min-width: 0;
                        min-height: 0;
                        max-width: none;
                        max-height: none;
                    `;

            $('.image-preview').each(function () {
                $(this).html('').append(clone.cloneNode());
            });

            previewReady = true;

            updatePreview();
        },
        crop: function (event) {
            if (!previewReady) return;
            updatePreview();
        }
    });

    function updatePreview() {
        const canvas = window.__cropperLocomotive.getCroppedCanvas();
        $('.image-preview').each(function () {
            if (canvas) {
                const previewImage = $(this).find('img')[0];
                previewImage.src = canvas.toDataURL();
                previewImage.classList.remove("hidden");
            }
        });
    }

    function showCropperSize() {
        const data = window.__cropperLocomotive.getData();
        const width = data.width;
        const height = data.height;
        $('#cropper-size').text(width.toFixed(0) + ' x ' + height.toFixed(0));
        $('#cropper-geometry').text(window.__tmpDataCropper_Width.toFixed(0) + ' x ' + window.__tmpDataCropper_Height.toFixed(0));

        const fileSizeInKB = (window.__tmpDataCropper_fileSizeInBytes / 1024).toFixed(2);
        $('#cropper-filesize').text(fileSizeInKB + ' KB');
    }

    image[0].addEventListener('cropmove',
        function () {
            showCropperSize();
        });

    $('#save-image').prop('disabled', false).off('click').on('click',
        function () {
            const canvas = window.__cropperLocomotive.getCroppedCanvas();

            if (window.__cropperFlipX === true || window.__cropperFlipY === true) {
                const context = canvas.getContext('2d');
                context.save();
                let x = 1, y = 1;

                if (window.__cropperFlipX === true) x = -1; // Spiegeln entlang der X-Achse
                if (window.__cropperFlipY === true) y = -1; // Spiegeln entlang der Y-Achse

                // Zeichnen mit der Verschiebung: 
                // Wenn x = -1, muss das Bild nach rechts verschoben werden, um den Effekt richtig darzustellen.
                if (x === -1) {
                    context.drawImage(canvas, -canvas.width, 0); // Spiegeln entlang der X-Achse
                } else {
                    context.drawImage(canvas, 0, -canvas.height); // Normale Zeichnung, falls keine Spiegelung
                }

                context.restore();
            }

            canvas.toBlob(function (blob) {

                let name = window.__cropperLocomotiveName;
                if (!name.toLowerCase().endsWith(".png")) {
                    name += ".png";
                }
                const locName = __getTrainSelectedName();
                const file = new File([blob], name, { type: "image/png" });
                const formData = new FormData();
                formData.append('fileName', locName);
                formData.append('file', file);
                __uploadImage(window.__cropperBearerToken, formData);

                window.__cropperFlipX = false;
                window.__cropperFlipY = false;
            });
        });
}

function __uploadImage(bearerToken, formData) {

    const localBearer = bearerToken;

    fetch(window.__croppertPostImageUrl, {
        method: "POST",
        body: formData,
        headers: {
            "Authorization": `Bearer ${bearerToken}` // Token hinzufügen
        },
        credentials: 'include'
    }).then(response => {
        if (!response.ok) {
            throw new Error("Fehler beim Hochladen des Bildes.");
        }
        return response.json();
    }).then(data => {

        let url = data.fileUrl;
        if (!url || url.length <= 0)
            url = data.urlPathHash;

        __handleImage(
            window.__cropperLocomotiveName,
            url,
            localBearer);
    }).catch(error => {
        console.error("Upload-Fehler:", error);
    });
}

function __loadCropper(locname, imgurl, bearerToken) {
    if (imgurl)
        __handleImage(locname, imgurl, bearerToken);
}

async function __initLocImageEditFormular() {
    const select = document.getElementById("locomotive-select");
    const previewImage = document.getElementById("preview-image2");

    try {
        const response = await fetch(`${window.__serviceUrl}/api/locimg/sharedImageList`);
        const entries = await response.json();

        if (entries.length === 0) {
            select.innerHTML = '<option value="">Keine Lokomotiven verfügbar</option>';
            return;
        }

        entries.forEach(entry => {
            const option = document.createElement("option");
            option.value = entry.data; // Base64-Bild als Value setzen
            option.innerText = entry.name; // Name der Lokomotive
            select.appendChild(option);
        });

        // Standardmäßig erstes Bild setzen
        select.value = entries[0].data;
        previewImage.src = entries[0].data;
        previewImage.classList.remove("hidden");

        // Event-Listener für Dropdown-Änderung
        select.addEventListener("change", function () {
            previewImage.src = this.value;
            previewImage.classList.remove("hidden");
        });

        $('#apply-image').on('click',
            (ev) => {
                const locName = select.options[select.selectedIndex].text;
                const randomNumber = Math.floor(Math.random() * 1000000);
                const newImageUrl = `${window.__serviceUrl}/api/locimg/sharedImageList/${locName}.png?t=${randomNumber}`;

                console.log("Apply!");
                console.log(newImageUrl);

                __loadImage(newImageUrl)
                    .then(() => {
                        __loadCropper(locName, newImageUrl, window.__croppertPostImageUrl, window.__workspaceBearerToken);
                    })
                    .catch(error => {
                        console.error("Fehler beim Laden des Bildes:", error);
                    });
            });

    } catch (error) {
        console.error("Fehler beim Abrufen der Lokomotiven:", error);
        select.innerHTML = '<option value="">Fehler beim Laden</option>';
    }

    $('#flip-x').click(function () {
        const image = $('.cropper-container');
        const currentTransform = image.css('transform');
        if (currentTransform === 'matrix(-1, 0, 0, 1, 0, 0)') {
            image.css('transform', 'scaleX(1)');
        } else {
            image.css('transform', 'scaleX(-1)');
        }
        window.__cropperFlipX = !window.__cropperFlipX;
    });

    $('#flip-y').click(function () {
        const image = $('.cropper-container');
        const currentTransform = image.css('transform');
        if (currentTransform === 'matrix(1, 0, 0, -1, 0, 0)') {
            image.css('transform', 'scaleY(1)');
        } else {
            image.css('transform', 'scaleY(-1)');
        }
        window.__cropperFlipY = !window.__cropperFlipY;
    });
}

function __handleImage(locname, file, bearerToken) {
    window.__cropperLocomotiveName = locname;
    window.__cropperBearerToken = bearerToken;

    if (file instanceof File) {
        const objectURL = URL.createObjectURL(file);
        const image = $('#image');

        window.__tmpDataCropper_fileSizeInBytes = file.size;

        const img = new Image();
        img.onload = function () {
            window.__tmpDataCropper_Width = img.width;
            window.__tmpDataCropper_Height = img.height;
            image.attr('src', objectURL).show();
            __loadFileCropperHandling();
            $('.previewContainer').removeClass("hidden");
        };

        // Lade das Bild
        img.src = objectURL;

    } else {
        fetch(file, {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${bearerToken}`
                },
                credentials: 'include'
            })
            .then(response => {

                if (response.ok) {
                    const fileSize = response.headers.get('Content-Length');
                    console.log('Dateigröße:', fileSize, 'Bytes');

                    window.__tmpDataCropper_fileSizeInBytes = parseInt(fileSize);
                }

                return response.blob();
            })
            .then(blob => {
                const objectURL = URL.createObjectURL(blob);
                const image = $('#image');
                image.attr('src', objectURL).show();
                __loadMetadata(objectURL);
                __loadFileCropperHandling();
                $('.previewContainer').removeClass("hidden");
            })
            .catch(error => {
                console.error("Error:", error);
            });
    }
}

function __getTrainSelectedName() {
    const locNameOriginal = $('#trainSelect option:selected').text()?.trim();
    const locName = locNameOriginal.split('►')[0].trim();
    return locName;
}

async function __loadFilteredData() {
    
    const locName = __getTrainSelectedName();

    $('#locNameDisplay')?.text(locName);

    const locNameHash = await generateSHA256Hash(locName);
    const randomNumber = Math.floor(Math.random() * 1000000);
    const newImageUrl = `${window.__serviceUrl}/api/locimg/file/${locNameHash}.png?t=${randomNumber}&fallback=${locName}`;
    window.__croppertPostImageUrl = `${window.__serviceUrl}/api/locimg/locup/`;

    __loadImage(newImageUrl)
        .then(() => {

            window.__cropperLocomotiveName = locName;
            window.__cropperLocomotiveName = locNameHash;
            window.__cropperBearerToken = window.__workspaceBearerToken;

            __loadCropper(locName, newImageUrl, window.__workspaceBearerToken);
        })
        .catch(error => {
            console.error("Fehler beim Laden des Bildes:", error);
        });
}

function __loadImage(imageUrl) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = (error) => reject(error);
        img.src = imageUrl;
    });
}

async function __initAsyncStuff() {
    try {
        await __initLocImageEditFormular();
        __initCropper();
    } catch (error) {
        console.error('Fehler beim Initialisieren:', error);
    }

    $('#trainSelect').change(function () {
        __loadFilteredData();
    });
}

$(document).ready(() => {
    __initAsyncStuff();
});