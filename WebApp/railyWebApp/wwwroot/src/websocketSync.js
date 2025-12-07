// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿class WebSocketSync {
    constructor() {
        this.messageQueue = [];
        this.pendingResolvers = [];
    }

    appendMessage(jsonData) {
        if (jsonData.command === "reply") {
            if (this.pendingResolvers.length > 0) {
                const resolver = this.pendingResolvers.shift();
                resolver(event.data);
            } else {
                this.messageQueue.push(event.data);
            }
        }
    }

    async readMessage() {
        if (this.messageQueue.length > 0) {
            return this.messageQueue.shift();
        }
        return new Promise((resolve) => {
            this.pendingResolvers.push(resolve);
        });
    }

    sendMessage(message) {
        this.ws.send(message);
    }
}
