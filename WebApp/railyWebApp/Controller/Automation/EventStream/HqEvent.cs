// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.Entities;
using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.EventStream;

/// <summary>
/// Repräsentiert ein Ereignis (z. B. Sensorstatus, Automatikaktion oder Systemmeldung),
/// das über den Server-Sent-Events-Stream an Clients gesendet werden kann.
/// Der EventType legt fest, wie der Client das Ereignis filtern oder behandeln kann.
/// </summary>
public class HqEvent
{
    /// <summary>
    /// Gibt den Typ des Ereignisses an (z. B. "sensor", "accessory", "locomotive").
    /// Dieser Wert wird im Event-Stream verwendet, um Ereignisse gezielt im Client
    /// verarbeiten zu können (z. B. über addEventListener("sensor")).
    /// Wenn kein Typ gesetzt ist, wird das Ereignis als Standardnachricht (onmessage)
    /// empfangen.
    /// </summary>
    [JsonProperty("eventType")] public HqEventTypes EventType { get; set; } = HqEventTypes.None;

    [JsonProperty("data")] public object Data { get; set; }

    public static HqEvent Instance(IEntityBase data)
    {
        return new HqEvent
        {
            Data = data?.ToJsonObject()
        };
    }
}