// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace railyWebApp.Controller.Automation.Dto
{
    public static class ErrorMessages
    {
        public const string DefaultLanguage = "en";

        private static readonly Dictionary<string, Dictionary<ErrorCode, string>> Messages = new()
        {
            ["de"] = new()
        {
            { ErrorCode.InternalServerError, "Ein unerwarteter Fehler ist aufgetreten" },
            { ErrorCode.LocomotiveNotFound, "Lok wurde nicht gefunden" },
            { ErrorCode.InvalidSpeed, "Ungültiger Geschwindigkeitswert" },
            { ErrorCode.AccessoryNotFound, "Schaltartikel wurde nicht gefunden" },
            { ErrorCode.FeedbackNotFound, "Feedback/Sensor nicht gefunden" },
            { ErrorCode.BlockNotFound, "Block nicht gefunden" },
            { ErrorCode.RoutesNotAvailable, "Keine Routen vorhanden" },
            { ErrorCode.Unauthorized, "Authentifizierung erforderlich" },
        },
            ["en"] = new()
        {
            { ErrorCode.InternalServerError, "An unexpected error occurred" },
            { ErrorCode.LocomotiveNotFound, "Locomotive not found" },
            { ErrorCode.InvalidSpeed, "Invalid speed value" },
            { ErrorCode.AccessoryNotFound, "Accessory not found" },
            { ErrorCode.FeedbackNotFound, "Feedback/Sensor not found" },
            { ErrorCode.BlockNotFound, "Block not found" },
            { ErrorCode.RoutesNotAvailable, "No available routes" },
            { ErrorCode.Unauthorized, "Authentication required" },
        }
        };

        public static string GetMessage(ErrorCode code, string language = DefaultLanguage)
        {
            if (Messages.TryGetValue(language, out var dict) && dict.TryGetValue(code, out var msg))
                return msg;

            return "Unknown error.";
        }
    }
}