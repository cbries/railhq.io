// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUtilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using railyWebApp.Controller.ActiveUsers;
using railyWebApp.Controller.Automation.EventStream;
using railyWebApp.Controller.Helper;
using StackExchange.Redis;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace railyWebApp
{
    public class Program
    {
        private static void InitExceptionHandling()
        {
            Logging.Log.Info("Initialize Exception handling");

            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                Logging.ExceptionLog(e.Exception);
            };
        }

        #region Configuration

        private static void InitConfiguration()
        {
            Globals.RuntimeConfiguration = GetConfig();
        }

        private static Cfg.Cfg _cfg;

        private static Cfg.Cfg GetConfig()
        {
            if (_cfg != null) return _cfg;
            var cfgCnt = GetCfgContent();
            var cfgObject = JsonConvert.DeserializeObject<Cfg.Cfg>(cfgCnt);
            _cfg = cfgObject;
            return _cfg;
        }

        public static string _getBaseDir => libShared.RailEnvironment.GetBaseDirectory();

        private static string GetCfgContent()
        {
            var pathToConfiguration = Path.Combine(_getBaseDir, Globals.AppConfigName);
            if (!File.Exists(pathToConfiguration))
                throw new Exception($"Missing file: {pathToConfiguration}");

            var cnt = File.ReadAllText(pathToConfiguration, Encoding.UTF8);
            if (string.IsNullOrEmpty(cnt))
                throw new Exception($"Configuration is empty ({pathToConfiguration}).");

            return cnt;
        }

        #endregion

        private static string GetBindingUrls()
        {
            // Check environment variable first (overrides config)
            var useTlsEnv = Environment.GetEnvironmentVariable("RAILHQ_USE_TLS");
            var useTls = !string.IsNullOrEmpty(useTlsEnv)
                ? useTlsEnv.Equals("true", StringComparison.OrdinalIgnoreCase)
                : _cfg.Host.IsTls;

            if (useTls)
                return $"https://{_cfg.Host.ListenDevice}:{_cfg.Host.ListenPort}";
            return $"http://{_cfg.Host.ListenDevice}:{_cfg.Host.ListenPort}";
        }

        public static async Task Main(string[] args)
        {
            InitConfiguration();
            InitExceptionHandling();

            if (RailEnvironment.IsRunningInContainer())
                Logging.Log.Info("Running in Container!");

            var builder = WebApplication.CreateBuilder(args);

            // Initialize host configuration from appsettings.json
            HostConfiguration.Initialize(builder.Configuration);
            Logging.Log.Info($"Host Configuration: Domain={HostConfiguration.Current.Domain}, IsLocalhost={HostConfiguration.Current.IsLocalhost}");

            #region redis & Supabase

            string cachedHost;
            int cachedPort;

            if (RailEnvironment.IsRunningInContainer())
            {
                cachedHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? builder.Configuration["redisDocker:Host"];
                cachedPort = int.Parse(builder.Configuration["redisDocker:Port"]);
            }
            else
            {
                cachedHost = builder.Configuration["redis:Host"];
                cachedPort = int.Parse(builder.Configuration["redis:Port"]);
            }

            Logging.Log.Info($"redis: {cachedHost}:{cachedPort}");

            #region Single Connection to allow DI-access to ConnectionMultiplier

            var redisMultiplexer = ConnectionMultiplexer.Connect($"{cachedHost}:{cachedPort}");
            builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = new ConfigurationOptions
                {
                    EndPoints = { $"{cachedHost}:{cachedPort}" },
                    ConnectTimeout = 15 * 1000, // Erhöht das Timeout auf 10 Sekunden
                    SyncTimeout = 15 * 1000, // Timeout für synchronisierte Abfragen
                    AbortOnConnectFail = false, // Verhindert Fehlschläge bei kurzfristigen Verbindungsproblemen
                    KeepAlive = 60 // Hält die Verbindung aktiv
                };
                options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisMultiplexer);
            });

            //builder.Services.AddStackExchangeRedisCache(options =>
            //{
            //    options.Configuration = $"{cachedHost}:{cachedPort}";
            //    options.ConfigurationOptions = new ConfigurationOptions()
            //    {
            //        AbortOnConnectFail = false,
            //        EndPoints = { $"{cachedHost}:{cachedPort}" }
            //    };
            //});

            #endregion

            #region DataProtection for Redis

            //builder.Services.AddDataProtection()
            //    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\ProgramData\railhq"))
            //    .SetApplicationName(SessionGlobals.ProtectionKeyName)
            //    .UseCryptographicAlgorithms(
            //        new Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.
            //            AuthenticatedEncryptorConfiguration
            //            {
            //                EncryptionAlgorithm = Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
            //                    .EncryptionAlgorithm.AES_256_CBC,
            //                ValidationAlgorithm = Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
            //                    .ValidationAlgorithm.HMACSHA256
            //            });

            //builder.Services.Configure<KeyManagementOptions>(options =>
            //{
            //    options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(@"C:\ProgramData\railhq"), new NullLoggerFactory());
            //    options.AutoGenerateKeys = false; // Verhindert, dass mehrere Instanzen neue Keys erzeugen
            //});

            var connectionStr = $"{cachedHost}:{cachedPort},abortConnect=false";
            builder.Services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(connectionStr), "DataProtection-Keys")
                .SetApplicationName(SessionGlobals.ProtectionKeyName);

            #endregion

            // Check if TLS is enabled via environment variable - needed for cookie policies
            var useTlsEnv = Environment.GetEnvironmentVariable("RAILHQ_USE_TLS");
            var useTls = !string.IsNullOrEmpty(useTlsEnv)
                ? useTlsEnv.Equals("true", StringComparison.OrdinalIgnoreCase)
                : _cfg.Host.IsTls;

            // Cookie SecurePolicy based on TLS setting
            var cookieSecurePolicy = useTls ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            var cookieSameSite = useTls ? SameSiteMode.None : SameSiteMode.Lax;

            builder.Services.AddSession(options =>
            {
                // Only set domain if not localhost
                if (!HostConfiguration.Current.IsLocalhost)
                {
                    options.Cookie.Domain = HostConfiguration.Current.CookieDomain;
                }
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = cookieSameSite;
                options.Cookie.Name = ".RaillHQ.Session";
                options.Cookie.MaxAge = TimeSpan.FromDays(1);
                options.IdleTimeout = TimeSpan.FromMinutes(60 * 24);
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = ".RaillHQ.Session";
                // Only set domain if not localhost
                if (!HostConfiguration.Current.IsLocalhost)
                {
                    options.Cookie.Domain = HostConfiguration.Current.Domain;
                }
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = cookieSameSite;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            builder.Services.AddMemoryCache();

            //
            // JSON-based Authentication (replaces Supabase)
            //
            var usersFilePath = Environment.GetEnvironmentVariable("USERS_FILE_PATH")
                ?? builder.Configuration["Auth:UsersFilePath"];

            if (string.IsNullOrEmpty(usersFilePath))
            {
                usersFilePath = Path.Combine(_getBaseDir, "resources", "users.json");
            }
#if DEBUG
            Logging.Log.Info($"Auth Users File: {usersFilePath}");
#endif
            builder.Services.AddSingleton(new SupabaseService(usersFilePath));
            builder.Services.AddSingleton<SupabaseHealthCheck>();

            #endregion

            #region register ActiveUser middleware

            builder.Services.AddSingleton<ActiveUserService>();

            #endregion

            #region register Event-Stream service

            builder.Services.AddSingleton<IHqEventService, HqEventService>();

            #endregion

            // ####################################################################
            // specific stuff
            //
            //builder.Services.AddCors(options =>
            //{
            //    options.AddDefaultPolicy(policy =>
            //    {
            //        //policy.WithOrigins(
            //        //    $"http://127.0.0.1:{_cfg.Host.ListenPort - 1}",
            //        //    $"https://localhost:{_cfg.Host.ListenPort}",
            //        //    $"http://127.0.0.1:{_cfg.Host.ListenPort - 1}",
            //        //    $"https://localhost:{_cfg.Host.ListenPort}")
            //        //        .AllowAnyHeader().AllowAnyMethod();
            //        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            //    });
            //});

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", policyBuilder =>
                {
                    // In local/OnPremise mode, allow any origin for easier access from any IP
                    if (HostConfiguration.Current.IsLocalhost)
                    {
                        policyBuilder.SetIsOriginAllowed(_ => true)
                            .AllowCredentials()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                    else
                    {
                        // Cloud mode: only allow specific origins
                        var httpsPort = string.Empty;
                        if (!RailEnvironment.IsRunningInContainer())
                            httpsPort = ":13443";

                        var domain = HostConfiguration.Current.Domain;

                        policyBuilder.WithOrigins(
                                $"https://{domain}{httpsPort}", $"https://www.{domain}{httpsPort}",
                                $"https://localhost{httpsPort}",
                                "https://railhq.de", "https://www.railhq.de",
                                "https://railhq.net", "https://www.railhq.net",
                                "https://railhq.app", "https://www.railhq.app",
                                "https://railhq.org", "https://www.railhq.org"
                            )
                            .AllowCredentials()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });

                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()  // Erlaubt alle Ursprünge
                        .AllowAnyMethod()    // Erlaubt alle HTTP-Methoden
                        .AllowAnyHeader();   // Erlaubt alle Header
                });

                options.AddPolicy("AllowAllOrigins", builder1 =>
                {
                    builder1.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();

            // appsettings.json mit Reload-on-Change
            builder.Host.ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            });

            #region Compression

            // Gzip-Kompression konfigurieren
            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest; // Oder: Optimal, SmallestSize
            });

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true; // Auch für HTTPS aktivieren
                options.Providers.Add<GzipCompressionProvider>(); // Gzip aktivieren
            });

            #endregion

            #region Swagger

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                options.DocInclusionPredicate((docName, apiDesc) =>
                {
                    //
                    // Füge nur die API-Aufrufe für "api/automation" zur Swagger-Dokumentation hinzu.
                    //
                    var path = apiDesc.RelativePath;
                    return path != null && path.IndexOf("/automation", StringComparison.OrdinalIgnoreCase) != -1;
                });

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "railhq.io API",
                    Version = "v1",
                    Description = Globals.RestApiDescription
                });
                //options.SwaggerDoc("v2", new OpenApiInfo { Title = "railhq.io API", Version = "v2", Description = Globals.RestApiDescription });
            });

            #endregion

            #region TLS/SSL Configuration

            // useTls already defined above for cookie policies

            if (useTls)
            {
                var certPath = libUtilities.Filesystem.GetCertPath(Globals.CertName);
                if (File.Exists(certPath))
                    Logging.Log.Info($"Using certificate: {certPath}");
                else
                    Logging.Log.Warn($"TLS enabled but certificate is missing: {certPath}");

                try
                {
                    builder.WebHost.ConfigureKestrel(options =>
                    {
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                        });
                        options.ListenAnyIP(_cfg.Host.ListenPort - 1); // HTTP fallback

                        var pfxPw = Environment.GetEnvironmentVariable("TLS_CERT_PW") ?? builder.Configuration["pfx:password"];
                        options.ListenAnyIP(_cfg.Host.ListenPort, listenOptions =>
                        {
                            listenOptions.UseHttps(certPath, pfxPw ?? string.Empty);
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
                        });
                    });
                    Logging.Log.Info($"TLS enabled: HTTPS on port {_cfg.Host.ListenPort}, HTTP fallback on port {_cfg.Host.ListenPort - 1}");
                }
                catch (Exception ex)
                {
                    ex.ShowException();
                    Logging.Log.Info($"!!! CRITICIAL --> railyWebApp is STOPPING !!!");
                    return;
                }
            }
            else
            {
                // HTTP only mode - no TLS
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(_cfg.Host.ListenPort);
                });
                Logging.Log.Info($"TLS disabled: HTTP only on port {_cfg.Host.ListenPort}");
            }

            #endregion

            var urls = GetBindingUrls();
            builder.WebHost.UseUrls(urls);

            var app = builder.Build();

            app.UseSession();

            app.UseResponseCompression();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();

                //// Deaktiviere die Komprimierung nur in der Entwicklungsumgebung
                //app.Use(async (context, next) =>
                //{
                //    context.Response.Headers.Remove("Content-Encoding");
                //    await next();
                //});
            }

            //
            // aktiviere Swagger im Produktionssystem
            //
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "railhq.io API v1");
                //options.SwaggerEndpoint("/swagger/v2/swagger.json", "railhq.io API v2");

                options.DocumentTitle = "railhq.io API Explorer";
                options.InjectStylesheet("/swagger-ui/custom.css");
            });

            //
            // Middleware für aktives User-Tracking registrieren
            // NACH UseSession() aufrufen!
            //
            app.UseMiddleware<ActiveUserMiddleware>();
            app.MapGet("/internal/active-users", async (ActiveUserService activeUserService) =>
            {
                var count = await activeUserService.GetActiveUserCount();
                return Results.Json(new { active_users = count });
            });

            // Mapping Get & Post
            #region HomeModule

            app.MapGet("/locomotive", async context =>
            {
                var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

                await HomeModule.HandleLocomotive(context, cache);
            });

            app.MapGet("/{*filepath}", async context =>
            {
                var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

                await HomeModule.HandleFiles(context, cache);
            });

            #endregion

            #region WebSocketModule

            app.MapGet(WebSocketModule.WsBrowserUri, async (HttpContext context, SupabaseService supabaseService) =>
            {
                WebSocketModule.Supabase = supabaseService;
                await WebSocketModule.HandleWsRequest(context, true, false);
            });

            app.MapGet(WebSocketModule.WsRemoteControlUri, async (HttpContext context, SupabaseService supabaseService) =>
            {
                WebSocketModule.Supabase = supabaseService;
                await WebSocketModule.HandleWsRequest(context, true, true);
            });

            app.MapGet(WebSocketModule.WsControllerUri, async (HttpContext context, SupabaseService supabaseService) =>
            {
                WebSocketModule.Supabase = supabaseService;
                await WebSocketModule.HandleWsRequest(context, false, false);
            });

            #endregion

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(5),
            });

            try
            {
                app.UseHttpsRedirection();
                app.UseRouting();
                //app.UseCors("AllowAllOrigins");
                app.UseCors("AllowSpecificOrigin");
                app.UseAuthorization();
                app.MapControllers();
                app.Run();
            }
            catch (Exception ex)
            {
                ex.ShowException();

                Logging.Log.Info($"!!! CRITICIAL --> railyWebApp is STOPPING !!!");

                return;
            }
        }
    }
}
