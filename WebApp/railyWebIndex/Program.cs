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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace railyWebIndex
{
    public class Program
    {
        private static int? _httpPort;
        private static int? _httpsPort;

        /// <summary>
        /// Result is 80 or 1380.
        /// </summary>
        public static int HttpPort
        {
            get
            {
                if (_httpPort == null)
                {
                    _httpPort = RailEnvironment.IsRunningInContainer() ? 80 : 1380;
                }
                return _httpPort.Value;
            }
        }

        /// <summary>
        /// Result is 443 or 13443.
        /// </summary>
        public static int HttpsPort
        {
            get
            {
                if (_httpsPort == null)
                {
                    _httpsPort = RailEnvironment.IsRunningInContainer() ? 443 : 13443;
                }
                return _httpsPort.Value;
            }
        }

        private static void InitExceptionHandling()
        {
            Logging.Log.Info("Initialize Exception handling");

            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                Logging.ExceptionLog(e.Exception);
            };
        }

        public static async Task Main(string[] args)
        {
            InitExceptionHandling();

            if (RailEnvironment.IsRunningInContainer())
                Logging.Log.Info("Running in Container!");

            var builder = WebApplication.CreateBuilder(args);

            // Initialize Globals from configuration
            Globals.InitializeFromConfiguration(builder.Configuration);
            Logging.Log.Info($"Host Configuration: Domain={Globals.Domain}, ServiceUrl={Globals.ServiceUrl}");

            // Register HostConfiguration as singleton for DI (used in Views)
            var hostConfig = builder.Configuration.GetSection("Host").Get<HostConfiguration>() ?? new HostConfiguration();
            builder.Services.AddSingleton(hostConfig);

            #region Redis & Supabase

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
                : false;

            // Cookie SecurePolicy based on TLS setting
            var cookieSecurePolicy = useTls ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            var cookieSameSite = useTls ? SameSiteMode.None : SameSiteMode.Lax;

            builder.Services.AddSession(options =>
            {
                // Only set domain if not localhost
                if (!Globals.IsLocalhost)
                {
                    options.Cookie.Domain = Globals.CookieDomain;
                }
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = cookieSameSite;
                options.Cookie.Name = ".RailHQ.Session";
                options.Cookie.MaxAge = TimeSpan.FromDays(1);
                options.IdleTimeout = TimeSpan.FromMinutes(60 * 24);
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = ".RailHQ.Session";
                // Only set domain if not localhost
                if (!Globals.IsLocalhost)
                {
                    options.Cookie.Domain = Globals.Domain;
                }
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = cookieSameSite;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            builder.Services.AddMemoryCache();

            builder.Services.AddHttpClient<WebAppServiceApi>();
            builder.Services.AddHttpClient<WebIndexServiceApi>();

            //
            // JSON-based Authentication (replaces Supabase)
            //
            var usersFilePath = Environment.GetEnvironmentVariable("USERS_FILE_PATH")
                ?? builder.Configuration["Auth:UsersFilePath"];

            if (string.IsNullOrEmpty(usersFilePath))
            {
                usersFilePath = Path.Combine(libShared.RailEnvironment.GetBaseDirectory(), "resources", "users.json");
            }
#if DEBUG
            Logging.Log.Info($"Auth Users File: {usersFilePath}");
#endif
            builder.Services.AddSingleton(new SupabaseService(usersFilePath));

            #endregion

            // ####################################################################
            // default stuff
            //

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
                        options.ListenAnyIP(HttpPort); // HTTP fallback

                        var pfxPw = Environment.GetEnvironmentVariable("TLS_CERT_PW") ?? builder.Configuration["pfx:password"];
                        options.ListenAnyIP(HttpsPort, listenOptions =>
                        {
                            listenOptions.UseHttps(certPath, pfxPw ?? string.Empty);
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
                        });
                    });
                    Logging.Log.Info($"TLS enabled: HTTPS on port {HttpsPort}, HTTP fallback on port {HttpPort}");
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
                    options.ListenAnyIP(HttpPort);
                });
                Logging.Log.Info($"TLS disabled: HTTP only on port {HttpPort}");
            }

            #endregion

            builder.Services.AddHttpContextAccessor();

            // Antiforgery configuration - respects TLS setting
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = ".RailHQ.Antiforgery";
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = cookieSameSite;
                // Don't set domain for localhost compatibility
                if (Globals.IsLocalhost)
                {
                    options.Cookie.Domain = null;
                }
                else
                {
                    options.Cookie.Domain = Globals.CookieDomain;
                }
            });

            builder.Services.AddRazorPages();

            var app = builder.Build();

            app.UseResponseCompression();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();

                //// Deaktiviere die Komprimierung nur in der Entwicklungsumgebung
                //app.Use(async (context, next) =>
                //{
                //    context.Response.Headers.Remove("Content-Encoding");
                //    await next();
                //});
            }

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, Globals.DocumentationDirName)),
                RequestPath = Globals.DocumentationDirPath
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, Globals.DocumentationDirName)),
                RequestPath = Globals.DocumentationDirPath
            });

            try
            {
                app.UseSession();
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthorization();
                app.MapRazorPages();
                app.MapControllers();
                app.Run();
            }
            catch (Exception ex)
            {
                ex.ShowException();

                Logging.Log.Info($"!!! CRITICIAL --> railyWebIndex is STOPPING !!!");

                return;
            }
        }
    }
}
