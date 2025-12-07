const ErrorHandlerLevel = {
    Info: 1,
    Error: 2
}

class ErrorHandler {

    /**
     * Constructs an instance of the ErrorHandler class.
     * Initializes the overlay elements and sets the default level to Info.
     */
    constructor() {
        console.log("**** construct ErrorHandler");
        this.__overlay = $('div.overlay');
        this.__overlayText = $('div.overlay div.overlayText');
        this.setLevel(ErrorHandler.Info);
    }
    
    /**
     * Sets the display level of the overlay by changing its color class.
     *
     * @param {string} [level=ErrorHandlerLevel.Info] - The level to set, determining the overlay's appearance.
     */
    setLevel(level = ErrorHandlerLevel.Info) {
        const colorClasses = {
            [ErrorHandlerLevel.Info]: "infoOverlay",
            [ErrorHandlerLevel.Error]: "errorOverlay",
        };

        const colorClass = colorClasses[level] || "infoOverlay";

        this.__overlayText
            .removeClass("infoOverlay errorOverlay")
            .addClass(colorClass);
    }


    /**
     * Updates the overlay text with the provided message.
     * Displays the overlay if it's not already visible or if forced by the `forceMessage` flag.
     *
     * @param {string} msg - The message to display in the overlay.
     * @param {boolean} [forceMessage=false] - If true, updates the message even if the overlay is visible.
     */
    setText(msg, forceMessage = false) {
        const overlay = this.__overlay;
        const overlayText = this.__overlayText;

        if (!overlay.is(":visible")) {
            overlay.show();
            overlayText.html(msg);
            return;
        }

        if (forceMessage) {
            overlayText.html(msg);
        }
    }

    /**
     * Hides the overlay and clears its text content.
     */
    hide() {
        const overlay = this.__overlay;
        const overlayText = this.__overlayText;

        overlay.hide();
        overlayText.html("");
    }

}