// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿// ReSharper disable InconsistentNaming

namespace libEsuEcos
{
    public class Globals
    {
        public const string EsuEcosIdentifier = "ecos";

        public const int ID_EV_ECoS = 1;
        public const int ID_EV_Programmiergleis = 5;
        public const int ID_EV_LokManager = 10;
        public const int ID_EV_SchaltartikelManager = 11;
        public const int ID_EV_Pendelzugsteueuerung = 12;
        public const int ID_EV_Devicemanager = 20;
        public const int ID_EV_Sniffer = 25;
        public const int ID_EV_FeedbackManager = 26;
        public const int ID_EV_Booster = 27;
        public const int ID_EV_Stellpult = 31;
        // Listenobjekt Lok (id=dynamisch)
        // Listenobjekt Feedback-Modul (id=dynamisch)
        // Listenobjekt Pendelzugstrecke (id=dynamisch)

        // Dokumentation: Die ECoS verwaltet bis zu 16384 Loks.
        public const int MaxNoOfLocomotives = 16384;

        public static bool IsLocomotiveId(int objectId)
        {
            if (objectId == -1) return false;
            return objectId >= 1000 && objectId < 1000 + libEsuEcos.Globals.MaxNoOfLocomotives;
        }

        public static bool IsAccessorySwitch(int objectId)
        {
            if (objectId == -1) return false;
            return objectId >= 20000 && objectId < 30000;
        }
    }
}
