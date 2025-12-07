// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

(function($) {
    $.fn.imagePasteDragDrop = function(options) {
        const settings = $.extend({
            editorClass: 'editor',    // Standard Editor-Element-Klasse
            overlayClass: 'overlay',  // Standard Overlay-Element-Klasse
            imgMaxWidth: 100,         // Maximale Breite der Bilder im Editor
            imgMaxHeight: 100,        // Maximale Höhe der Bilder im Editor
        }, options);

        const $editor = this;
        const $overlay = $('<div>', { class: settings.overlayClass }).appendTo('body');
        const $overlayImg = $('<img>', { id: 'overlayImg' }).appendTo($overlay);
        const $closeBtn = $('<button>', { id: 'closeBtn', text: 'Schließen' }).appendTo($overlay);

        // Stile für Overlay und Close-Button
        $overlay.css({
            position: 'fixed',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            display: 'none',
            justifyContent: 'center',
            alignItems: 'center',
            zIndex: 1000,
            padding: '20px',
        });

        $overlayImg.css({
            maxWidth: '90%',
            maxHeight: '90%',
            objectFit: 'contain',
        });

        $closeBtn.css({
            position: 'absolute',
            top: '10px',
            right: '10px',
            backgroundColor: 'rgba(255, 255, 255, 0.7)',
            border: 'none',
            padding: '5px 10px',
            cursor: 'pointer',
            fontSize: '14px',
            borderRadius: '5px',
        });

        function handleImage(file) {
            const reader = new FileReader();
            reader.onload = function(e) {
                const $img = $('<img>', {
                    src: e.target.result,
                    style: `max-width: ${settings.imgMaxWidth}px; max-height: ${settings.imgMaxHeight}px; cursor: pointer;`,
                    click: function() {
                        // Öffne das Overlay bei Klick
                        $overlay.show();
                        $overlayImg.attr('src', $(this).attr('src'));
                    }
                }).appendTo($editor);

                // Resize-Handle hinzufügen
                const $resizeHandle = $('<div>', { class: 'resize-handle' }).appendTo($img);
                $img.css('position', 'relative');

                let isResizing = false;

                $resizeHandle.on('mousedown', function(e) {
                    isResizing = true;
                    const imgWidth = $img.width();
                    const imgHeight = $img.height();
                    const startX = e.clientX;
                    const startY = e.clientY;

                    $(document).on('mousemove', function(e) {
                        if (isResizing) {
                            const newWidth = imgWidth + e.clientX - startX;
                            const newHeight = imgHeight + e.clientY - startY;
                            $img.width(newWidth).height(newHeight);
                        }
                    });

                    $(document).on('mouseup', function() {
                        isResizing = false;
                        $(document).off('mousemove mouseup');
                    });
                });
            };
            reader.readAsDataURL(file);
        }

        // Paste Handler
        $editor.on('paste', function(event) {
            event.preventDefault();
            const items = event.originalEvent.clipboardData.items;
            $.each(items, function(index, item) {
                if (item.type.startsWith("image/")) {
                    handleImage(item.getAsFile());
                } else {
                    const text = event.originalEvent.clipboardData.getData("text/plain");
                    document.execCommand("insertText", false, text);
                }
            });
        });

        // Drag&Drop Handler
        $editor.on('dragover', function(event) {
            event.preventDefault();
            $editor.css('border', '2px dashed #666');
        });

        $editor.on('dragleave', function() {
            $editor.css('border', '1px solid #ccc');
        });

        $editor.on('drop', function(event) {
            event.preventDefault();
            $editor.css('border', '1px solid #ccc');
            const files = event.originalEvent.dataTransfer.files;
            $.each(files, function(index, file) {
                if (file.type.startsWith("image/")) {
                    handleImage(file);
                }
            });
        });

        // Overlay schließen
        $overlay.on('click', function(event) {
            if (event.target === $overlay[0] || event.target === $overlayImg[0]) {
                $overlay.hide();
            }
        });

        // Close Button
        $closeBtn.on('click', function() {
            $overlay.hide();
        });

        return this;
    };
}(jQuery));
