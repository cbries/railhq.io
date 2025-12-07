// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO;
using libShared;
using libUserspace;

namespace railyWebApp
{
    public static class Globals
    {
        public const int HTTP_401_UNAUTHORIZED = 401;
        public const int HTTP_403_FORBIDDEN = 403;

        public const string CertName = "railhq-local.pfx";

        public const string RestApiDescription = @"
Die railhq.io API ist deine leistungsstarke Schnittstelle zur Steuerung und Automatisierung deiner digitalen Modelleisenbahnanlage – komplett in der Cloud, flexibel erweiterbar und rund um die Uhr erreichbar.

Egal ob du Züge startest, Weichen stellst, Sensoren abfragst oder komplexe Automatisierungen umsetzt – mit der REST API von railhq.io hast du volle Kontrolle über deine Anlage. Entwickelt für Bastler, Profis und Entwickler gleichermaßen.

Was du bekommst:
- Echtzeitsteuerung über moderne REST-Endpunkte
- Webhooks und Events für automatisierte Abläufe
- Volle Unterstützung für Skripte, Sensoren, Weichen, Lokomotiven und mehr
- Sicherer Cloud-Zugriff mit Token-basiertem Auth-System
- Einfache Integration per HTTP(S), JavaScript oder jeder anderen Sprache

Hier ein simples Beispiel, wie du eine Lok starten kannst:

    POST /api/v1/automation/Locomotive/ecos/3/speed
    {
      ""speed"": 40
    }

Starte jetzt mit der Steuerung deiner Anlage – direkt aus deinem Code heraus.";

        private static string _appConfigName;
        private static string _httpRootDir;

        public static string AppConfigName
        {
            get
            {
                if (_appConfigName == null)
                {
                    if (RailEnvironment.IsContainerBuild())
                    {
                        _appConfigName = "railyWebAppServer.json";
                    }
                    else
                    {
                        _appConfigName = "railyWebApp.json";
                    }
                }
                return _appConfigName;
            }
        }

        public static string HttpRootDir
        {
            get
            {
                if (_httpRootDir == null)
                {
                    if (RailEnvironment.IsContainerBuild())
                    {
                        _httpRootDir = Path.Combine("/", "app", "railyWebApp", "wwwroot");
                    }
                    else
                    {
                        var baseDir = libShared.RailEnvironment.GetBaseDirectory();
                        _httpRootDir = Path.Combine(baseDir, "..", "..", "..", "wwwroot");
                    }
                }
                return _httpRootDir;
            }
        }       

        public static Cfg.Cfg RuntimeConfiguration { get; set; }

        /// <summary>
        /// key:=auth token
        /// value:=instance of `DataConsumer`
        /// </summary>
        public static readonly ConcurrentDictionary<string, DataConsumer> RegisteredDataConsumer = new();

        /// <summary>
        /// key:=auth token
        /// value:=instanc of `UserWorkspace` with all data for an user
        ///
        /// uid := the supbabase user identifier, i.e. guid format
        /// </summary>
        public static readonly ConcurrentDictionary<string, Workspace> UserWorkspaces = new();

    }
}
