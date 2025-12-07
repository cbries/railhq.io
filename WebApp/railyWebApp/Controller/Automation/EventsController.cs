// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using railyWebApp.Controller.Automation.EventStream;
using System.Threading;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class EventsController : RailhqAutomationBase
    {
        public const string EventNameControlStation = "hqControlStation";
        public const string EventNameLocomotive = "hqLocomotive";
        public const string EventNameAccessory = "hqAccessory";
        public const string EventNameFeedback = "hqFeedback";

        private readonly IHqEventService _eventService;

        public EventsController(
            SupabaseService authService,
            IMemoryCache cache,
            IHqEventService eventService) : base(authService, cache)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Streamt Ereignisse als Server-Sent Events (SSE)
        /// </summary>
        /// <remarks>
        /// Öffnet eine dauerhafte Verbindung und streamt Ereignisse im SSE-Format.
        /// Der Client muss `text/event-stream` als Accept-Header setzen.
        /// 
        /// **Mögliche Events:**
        /// - `Locomotive`
        /// - `Accessory`
        /// - `Feedback`
        /// - (oder leer bei allgemeinen Ereignissen)
        ///
        /// Beispielantwort:
        /// ```
        /// event: Locomotive
        /// data: {"eventType":"Locomotive","timestamp":"2025-06-06T12:00:00Z", ...}
        /// ```
        /// </remarks>
        [HttpGet]
        [Produces("text/event-stream")]
        [SwaggerOperation(
            Summary = "Startet den EventStream (SSE)",
            Description = "Dauerhafter Stream von Ereignissen (SSE) für Zubehör, Loks, Rückmelder etc.",
            OperationId = "GetEventStream"
        )]
        [SwaggerResponse(200, "Liefert laufenden EventStream im text/event-stream-Format")]
        public async Task Get(CancellationToken cancellationToken)
        {
#pragma warning disable ASP0019
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no");
#pragma warning restore ASP0019

            await foreach (var evt in _eventService.SubscribeAsync(Uid, cancellationToken))
            {
                string json;
                string eventData;

                if (evt.EventType == HqEventTypes.None)
                {
                    json = JsonConvert.SerializeObject(evt);
                    eventData = $"data: {json}\n\n";
                }
                else
                {
                    var eventArea = string.Empty;
                    if (evt.EventType == HqEventTypes.ControlStation)
                        eventArea = EventNameControlStation;
                    else if (evt.EventType == HqEventTypes.Locomotive)
                        eventArea = EventNameLocomotive;
                    else if (evt.EventType == HqEventTypes.Accessory)
                        eventArea = EventNameAccessory;
                    else if (evt.EventType == HqEventTypes.Feedback)
                        eventArea = EventNameFeedback;

                    if (string.IsNullOrEmpty(eventArea))
                    {
                        json = JsonConvert.SerializeObject(evt);
                        eventData = $"data: {json}\n\n";
                    }
                    else
                    {
                        json = JsonConvert.SerializeObject(evt);
                        eventData = $"event: {eventArea}\ndata: {json}\n\n";
                    }
                }

                if (!string.IsNullOrEmpty(eventData))
                {
                    await Response.WriteAsync(eventData);
                    await Response.Body.FlushAsync();
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }
    }
}
