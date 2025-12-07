// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿(function ($) {
    $.fn.randomVideo = function (options) {
        var settings = $.extend({
            videos: [],
            changeOnEnd: false,
            overlayColor: "rgba(255, 255, 255, 0.3)"
        }, options);

        if (!settings.videos.length) {
            console.error("No videos provided!");
            return this;
        }
        const videoContainer = this;
        // CSS automatisch hinzufügen
        const styles = `
                    * {
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }
                    .video-container {
                        position: fixed;
                        top: 0;
                        left: 0;
                        width: 100%;
                        height: 100%;
                        overflow: hidden;
                        z-index: -1;
                    }
                    .video-container video {
                        width: 100%;
                        height: 100%;
                        object-fit: cover;
                    }
                    .overlay {
                        position: fixed;
                        top: 0;
                        left: 0;
                        width: 100%;
                        height: 100%;
                        background: ${settings.overlayColor};
                    }
                    .content {
                        position: relative;
                        z-index: 1;
                        color: black;
                        text-align: center;
                        font-size: 2rem;
                        padding: 20px;
                    }
                `;
        $("<style>").text(styles).appendTo("head");
        videoContainer.addClass("video-container").html(`
                    <video id="bgVideo" autoplay loop muted playsinline></video>
                    <div class="overlay"></div>
                `);

        const videoElement = $("#bgVideo");

        function getRandomVideo() {
            var randomIndex = Math.floor(Math.random() * settings.videos.length);
            return settings.videos[randomIndex];
        }

        function setRandomVideo() {
            videoElement.empty();
            const sourceElement = $("<source>")
                .attr("src", getRandomVideo())
                .attr("type", "video/mp4");

            videoElement.append(sourceElement);
            videoElement[0].load();
        }

        setRandomVideo();

        if (settings.changeOnEnd) {
            videoElement.on("ended", function () {
                setRandomVideo();
            });
        }

        return this;
    };
})(jQuery);