// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus;
using libMetamodel;
using libShared;
using libShared.DataProvider;
using libTrackplan.Theming;
using libUserspace.Auth;
using libUserspace.LoggerDB;
using libUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace libUserspace
{
    public partial class Workspace
    {
        private const string SubdirTheme = "theme";
        private const string DefaultThemeName = "RailwayEssential";

        public const string SubdirLogging = "Logging";
        public const string SubdirWorkspace = "Workspaces";
        public const string SubdirFleet = "Fleet";
        public const string SubdirUserImages = "UserImages";
        private const string NamePlanfield = "planfield.json";
        private const string NameRoutes = "routes.json";
        private const string NameSettings = "settings.json";

        private string _uid;

        public string Uid
        {
            get => _uid;
            private set
            {
                if (string.IsNullOrEmpty(value)) return;
                _uid = value;
            }
        }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public string EMail { get; private set; }

        public LocomotiveLogger LocomotiveLogging { get; private set; }
        public LocomotiveMetadata LocomotiveMetadata { get; private set; }

        private DirectoryInfo _loggingDirectory = null;
        private DirectoryInfo _wsDirectory = null;

        private readonly IMemoryCache _memoryCache;

        public Workspace(string uid, HttpContext httpContext, SupabaseService supabaseService)
        {
            Uid = uid;
            _supabaseService = supabaseService;
            _memoryCache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
            LoadSessionData(httpContext);
        }

        #region Authentication & Session Management

        /// <summary>
        /// This is the shared auth token provided by the web access asp dotnet core server, i.e. "railyWebIndex".
        /// The user will enter his credentials (username/password) and will access via the JSON-based auth on the WebIndex page.
        /// When the user goes to the trackplan (i.e. WebApp) the authToken of the session is stored into the HttpContext.Session
        /// to allow access without an additional login; kind of single sign on (SSO).
        /// But the railyGateway works different and the username/password combination is required to enter the services.
        /// We will keep the Uid in any way to distinguish the workspace for the individual user.
        /// </summary>
        public string SharedAuthToken { get; private set; } = string.Empty;

        private readonly SupabaseService _supabaseService;

        private void LoadSessionData(HttpContext httpContext)
        {
            try
            {
                if (_supabaseService.CurrentUser != null)
                {
                    EMail = _supabaseService.CurrentUser.Email;
                    Uid = _supabaseService.CurrentUser.Id;
                    return;
                }
            }
            catch
            {
                // ignore
            }

            var session = httpContext?.Session;

            if (session != null)
            {
                SharedAuthToken = GetCachedValue(httpContext, SessionGlobals.AuthUserSession);
                EMail = GetCachedValue(httpContext, SessionGlobals.AuthEmail);
                Uid = GetCachedValue(httpContext, SessionGlobals.AuthUid);

                var user = _supabaseService.Validate(AccessToken).Result;
                if (user != null)
                {
                    EMail = user.Email;
                    Uid = user.Id;
                }
            }
        }

        private string GetCachedValue(HttpContext httpContext, string key, string def = "")
        {
            if (string.IsNullOrEmpty(key)) return def;
            if (_memoryCache.TryGetValue(key, out string value))
                return value;
            var ctxSession = httpContext?.Session;
            if (ctxSession == null) return def;
            {
                if (ctxSession.TryGetValue(key, out var v2))
                    return Encoding.UTF8.GetString(v2); ;
            }

            return def;
        }

        #endregion

        /// <summary>
        /// The currently loaded name of the workspace, e.g. "Basement", or "OvalTest".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The loaded theme information, e.g. "RailEssentials".
        /// </summary>
        public ThemeData Theme { get; set; } = new();

        /// <summary>
        /// All relevant data of the workspace to edit the trackplan and to start automation mode, and much more.
        /// </summary>
        public MetamodelData Metamodel { get; set; } = new();

        private IAutomaticRunner _automaticRunner = null;

        public IAutomaticRunner AutomaticRunner
        {
            get => _automaticRunner;
            set
            {
                if (_automaticRunner == null)
                {
                    _automaticRunner = value;

                    if (_automaticRunner != null)
                    {
                        _automaticRunner.StagingFinalized += (_, _) => TriggerStateUpdateToClients(true);
                        _automaticRunner.RoutingFinalized += (_, _) => TriggerStateUpdateToClients(true);
                    }
                }
            }
        }

        private bool _isLoaded = false;

        public bool IsLoaded => _isLoaded;

        public bool Unload()
        {
            if (!_isLoaded) return true;

            try
            {
                StopStateInformer();
            }
            catch
            {
                // ignore
            }

            try
            {
                Metamodel.Reset();
                Metamodel = new MetamodelData();

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);

                return false;
            }
            finally
            {
                _isLoaded = false;
            }
        }

        public async Task<bool> SaveScreenshot(string base64data)
        {
            try
            {
                if (base64data.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase))
                    base64data = base64data.Split(',')[1];
                byte[] imageBytes = Convert.FromBase64String(base64data);
                var pathToImage = Path.Combine(_wsDirectory.FullName, "screenshot.png");
                if (File.Exists(pathToImage))
                    File.Delete(pathToImage);
                await File.WriteAllBytesAsync(pathToImage, imageBytes);
                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        public async Task<bool> CreateDefaultWorkspace(string uid, string workspaceName)
        {
            try
            {
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                var pathToDir = Path.Combine(resDir, SubdirWorkspace, uid, workspaceName);
                _wsDirectory = new DirectoryInfo(pathToDir);
                if (!_wsDirectory.Exists)
                    Directory.CreateDirectory(_wsDirectory.FullName);

                //
                // create default files
                //

                var planfieldPath = Path.Combine(pathToDir, NamePlanfield);
                var routesPath = Path.Combine(pathToDir, NameRoutes);
                var settingsPath = Path.Combine(pathToDir, NameSettings);

                Uid = uid;

                return await Metamodel.CreateDummyFiles(planfieldPath, routesPath, settingsPath);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        public async Task<bool> LoadOrCreate(
            string workspaceName,
            string uid,
            Dictionary<DataProviderType, List<string>> overviewDataproviders)
        {
            if (_isLoaded) return true;

            Name = workspaceName;
            Uid = uid;

            try
            {
                var resDir = libUtilities.Filesystem.GetResourcesPath();

                _wsDirectory = new DirectoryInfo(Path.Combine(resDir, SubdirWorkspace, uid, Name));

                _loggingDirectory = new DirectoryInfo(Path.Combine(resDir, SubdirLogging, uid));
                LocomotiveLogging = new LocomotiveLogger(_loggingDirectory);
                LocomotiveMetadata = new LocomotiveMetadata(_loggingDirectory);

                var themeInfo = new FileInfo(Path.Combine(resDir, SubdirTheme, DefaultThemeName + ".json"));
                await Theme.Load(themeInfo);

                var planfieldInfo = new FileInfo(Path.Combine(resDir, SubdirWorkspace, uid, Name, NamePlanfield));
                var routesInfo = new FileInfo(Path.Combine(resDir, SubdirWorkspace, uid, Name, NameRoutes));
                var settingsInfo = new FileInfo(Path.Combine(resDir, SubdirWorkspace, uid, Name, NameSettings));

                await Metamodel.LoadPlanfield(planfieldInfo);
                await Metamodel.LoadRoutes(routesInfo);
                Metamodel.UpdateSystemInfo(uid, overviewDataproviders);
                await Metamodel.LoadSettings(settingsInfo, uid);

                _isLoaded = true;

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, "Workspace load failed");
            }

            return false;
        }
    }
}
