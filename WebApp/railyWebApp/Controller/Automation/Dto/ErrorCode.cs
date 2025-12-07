// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace railyWebApp.Controller.Automation.Dto;

public enum ErrorCode
{
    // Allgemein
    UnknownError = 1,
    InternalServerError = 500,

    // Lokomotiven (1000–1999)
    LocomotiveNotFound = 1001,
    InvalidSpeed = 1002,
    DriverNotAvailable = 1003,

    // Zubehör (2000–2999)
    AccessoryNotFound = 2001,

    // Rückmelder (3000–3999)
    FeedbackNotFound = 3001,

    // Blöcke / Gleise (4000–4999)
    BlockNotFound = 4001,

    // Routen / Automatik (5000–5999)
    RoutesNotAvailable = 5001,

    // Rechte / Auth (9000–9999)
    Unauthorized = 9001,
}