// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

$(document).ready(function () {
    const dropdownButton = $('.riesMenuDropDown-button0');
    const dropdownMenu = $('#dropdown');
    dropdownButton.on('click', function () {
        dropdownMenu.toggle();
    });
    dropdownMenu.on('mouseleave', function () {
        dropdownMenu.hide();
    });

    document.getElementById("menuButton").addEventListener("click", function () {
        const sidebar = document.getElementById("sidebar");
        if (sidebar.classList.contains("-translate-x-full")) {
            sidebar.classList.remove("-translate-x-full");
        } else {
            sidebar.classList.add("-translate-x-full");
        }
    });
    document.addEventListener("click", function (event) {
        const sidebar = document.getElementById("sidebar");
        const menuButton = document.getElementById("menuButton");
        if (!sidebar.contains(event.target) && !menuButton.contains(event.target)) {
            sidebar.classList.add("-translate-x-full");
        }
    });

    //
    // the following code is responsible for creating a new
    // workspace on the workspace overview page
    //
    const $input = $(".workspaceName");
    const $button = $(".createButton");

    $input.on("input", function () {
        if ($input.val().trim()) {
            $button.prop("disabled", false)
                .removeClass("text-gray-400")
                .addClass("text-blue-500");
        } else {
            $button.prop("disabled", true)
                .removeClass("text-blue-500")
                .addClass("text-gray-400");
        }
    });

    $button.click(function () {
        const workspaceName = $input.val().trim();
        if (workspaceName) {
            const baseUrl = $(this).data("url");
            window.location.href = baseUrl + workspaceName;
        }
    });

    //
    // the following code is for the workspace page
    // rename and delete of single workspaces
    //
    $(".renameWorkspaceLink").click(function () {
        const dlg = $(".renameModal");
        const btnConfirm = dlg.find('.renameConfirm');
        btnConfirm.data("wsname", $(this).data("wsname"));
        // Use dynamic service URL if available, fallback to data attribute
        btnConfirm.data("serviceurl", window.getServiceUrl ? window.getServiceUrl() + '/api/workspace' : $(this).data("serviceurl"));
        dlg.removeClass("hidden");
    });
    $(".renameCancel").click(function () { $(".renameModal").addClass("hidden"); });
    $(".renameConfirm").click(function () {
        const newName = $(".newWorkspaceName").val().trim();
        const nativeName = $(this).data("wsname");
        const serviceUrl = $(this).data("serviceurl");
        if (newName) {
            $('#loadingOverlay').removeClass("hidden");
            fetch(serviceUrl,
                    {
                        method: "POST",
                        headers: {
                            "Authorization": `Bearer ${window.__workspaceBearerToken}`,
                            "Content-Type": "application/json"
                        },
                        body: JSON.stringify({ command: "wsRename", name: nativeName, newName: newName }),
                        credentials: 'include'
                    })
                .then(async response => {
                    if (!response.ok) {
                        const errorData = await response.json().catch(() => null); // JSON auslesen, falls möglich
                        const errorMessage = errorData?.message || `Serverfehler (${response.status}): ${response.statusText}`;
                        throw new Error(errorMessage);
                    }
                    return response.json();
                })
                .then(data => {
                    $('#loadingOverlay').addClass("hidden");
                    location.reload();
                })
                .catch(error => {
                    $('#loadingOverlay').addClass("hidden");
                    let message = "Beim Umbenennen ist leider ein Fehler aufgetreten.";
                    let details = error?.message ? `<span class="text-sm block text-gray-500 mt-1">${error.message}</span>` : "";
                    showError(`<p class="errorMessage text-red-600 font-bold text-center mt-2">${message}${details}</p>`);

                });
            $(".renameModal").addClass("hidden");
        }
    });

    //
    // the following code is for the workspace page
    // rename and delete of single workspaces
    //
    $(".deleteWorkspaceLink").click(function () {
        const dlg = $(".deleteModal");
        const btnConfirm = dlg.find('.deleteConfirm');
        btnConfirm.data("wsname", $(this).data("wsname"));
        // Use dynamic service URL if available, fallback to data attribute
        btnConfirm.data("serviceurl", window.getServiceUrl ? window.getServiceUrl() + '/api/workspace' : $(this).data("serviceurl"));
        dlg.removeClass("hidden");
    });
    $(".deleteCancel").click(function () { $(".deleteModal").addClass("hidden"); });
    $(".deleteConfirm").click(function () {
        const workspaceName = $(".deleteWorkspaceName").val().trim();
        const nativeName = $(this).data("wsname");
        const serviceUrl = $(this).data("serviceurl");
        if (workspaceName === nativeName) {
            $('#loadingOverlay').removeClass("hidden");
            fetch(serviceUrl,
                {
                    method: "POST",
                    headers: {
                        "Authorization": `Bearer ${window.__workspaceBearerToken}`,
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({ command: "wsDelete", name: workspaceName }),
                    credentials: 'include'
                })
                .then(response => response.json())
                .then(data => {
                    $('#loadingOverlay').addClass("hidden");
                    location.reload(); 
                })
                .catch(error => {
                    $('#loadingOverlay').addClass("hidden");
                    let m = "Beim Löschen ist leider ein Fehler aufgetreten.";
                    if (error && error.length > 0)
                        m = m + " Details: " + error;
                    showError(m);
                });
            $(".deleteModal").addClass("hidden");
        } else {
            alert("Name stimmt nicht überein!");
        }
    });

    //
    // link to create a duplicate of a workspace
    //
    $(".duplicateWorkspaceLink").click(function() {
        const nativeName = $(this).data("wsname");
        // Use dynamic service URL if available, fallback to data attribute
        const serviceUrl = window.getServiceUrl ? window.getServiceUrl() + '/api/workspace' : $(this).data("serviceurl");
        $('#loadingOverlay').removeClass("hidden");
        fetch(serviceUrl,
                {
                    method: "POST",
                    headers: {
                        "Authorization": `Bearer ${window.__workspaceBearerToken}`,
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({ command: "wscopy", name: nativeName }),
                    credentials: 'include'
                })
            .then(response => response.json())
            .then(data => {
                $('#loadingOverlay').addClass("hidden");
                location.reload();
            })
            .catch(error => {
                $('#loadingOverlay').addClass("hidden");
                let m = "Beim Duplizieren ist leider ein Fehler aufgetreten.";
                if (error && error.length > 0)
                    m = m + " Details: " + error;
                showError(m);
            });
    });

    //
    // call refresh / update
    //
    $(".refreshWorkspaceLink").click(function () {
        const nativeName = $(this).data("wsname");
        // Use dynamic service URL if available, fallback to data attribute
        const serviceUrl = window.getServiceUrl ? window.getServiceUrl() + '/api/workspace' : $(this).data("serviceurl");
        $('#loadingOverlay').removeClass("hidden");
        fetch(serviceUrl,
                {
                    method: "POST",
                    headers: {
                        "Authorization": `Bearer ${window.__workspaceBearerToken}`,
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({ command: "wsRefresh", name: nativeName }),
                    credentials: 'include'
                })
            .then(response => response.json())
            .then(data => {
                $('#loadingOverlay').addClass("hidden");
                location.reload();
            })
            .catch(error => {
                $('#loadingOverlay').addClass("hidden");
                let m = "Beim Aktualisieren ist leider ein Fehler aufgetreten.";
                if (error && error.length > 0)
                    m = m + " Details: " + error;
                showError(m);
            });
    });

    //
    // handling for the import elements on "Workspace / Import"
    //
    $(".riesWorkspaceTile .importButton").click(function () {
        const fileInput = $(".riesWorkspaceTile .fileInput")[0];

        if (!fileInput.files.length) {
            showError("Bitte wähle eine Datei aus.");
            return;
        }

        $('#loadingOverlay').removeClass("hidden");

        const file = fileInput.files[0];

        // Datei als Base64 encodieren
        encodeFileToBase64(file)
            .then(base64Data => {
                // API-Request senden
                return $.ajax({
                    url: `${API_HOST_IMPORT}`,
                    method: "POST",
                    contentType: "application/json",
                    headers: {
                        "Authorization": `Bearer ${window.__workspaceBearerToken}`
                    },
                    data: JSON.stringify({
                        command: "import",
                        format: "rocrail",
                        data: base64Data
                    }),
                    xhrFields: {
                        withCredentials: true // Damit Cookies mitgesendet werden
                    }
                });
            })
            .done(function () {
                $('#loadingOverlay').addClass("hidden");
                location.reload();
            })
            .fail(function (jqXHR) {
                $('#loadingOverlay').addClass("hidden");
                showError(`Fehler: ${jqXHR.responseText || jqXHR.statusText}`);
            });
    });

    // Fehler-Overlay schließen
    $("#errorImportOverlay .closeError").click(function () {
        $("#errorImportOverlay").hide();
    });
});
