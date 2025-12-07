// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSample.mqttSupportMitBroker",
    description: "Stellt eine Verbindung zu einem MQTT-Broker her, abonniert Status-Themen und sendet einen Steuerbefehl."
};

await hqMqttClient.connect("wss://broker.example.com:9001/mqtt");
hqMqttClient.subscribe("zentrale/+/status", (topic, payload) => {
    console.log("MQTT Update:", topic, payload);
});
hqMqttClient.publish("zentrale/demo/befehl", JSON.stringify({ action: "stop" }));
