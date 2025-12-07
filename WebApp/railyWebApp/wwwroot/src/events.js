// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

/**
 * A lightweight event system for managing and triggering custom events.
 */
class Events {
    constructor() {
        /**
         * Stores event names as keys and their associated callback arrays as values.
         * @private
         */
        this.__triggers = {};
    }

    /**
     * Registers a callback function for a specified event.
     *
     * @param {string} event - The event name.
     * @param {Function} callback - The function to call when the event is triggered.
     */
    on(event, callback) {
        if (!this.__triggers[event]) {
            this.__triggers[event] = [];
        }
        this.__triggers[event].push(callback);
    }

    /**
     * Triggers all registered callbacks for the specified event.
     *
     * @param {string} event - The event name to trigger.
     * @param {*} [params] - Optional parameters to pass to the callback functions.
     */
    triggerHandler(event, params) {
        if (this.__triggers[event]) {
            for (const callback of this.__triggers[event]) {
                callback(params);
            }
        }
    }
}