using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuickShare.Helpers;
using Serilog;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace QuickShare.Services
{
    public class WebService(
        AppConfigService appConfigService,
        CertificateService certificateService,
        ISnackbarService snackbarService,
        TranslationService trService)
    {
        public bool IsRunning { get; private set; } = false;
        public event EventHandler<bool>? ServerStatusChanged;

        private WebApplicationBuilder? _webBuilder;
        private WebApplication? _web;

        public async Task StartWebServer()
        {
            if (IsRunning) return;

            certificateService.EnsureCertificateExists();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(App.LogFolder, "web-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 3,
                    fileSizeLimitBytes: null,
                    rollOnFileSizeLimit: false,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                .CreateLogger();
            _webBuilder = WebApplication.CreateBuilder();
            _webBuilder.Host.UseSerilog();

            _webBuilder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(appConfigService.NetworkConfig.Port, listenOptions =>
                {
                    listenOptions.UseHttps("server.pfx", appConfigService.AesKeyBase64);
                });
            });

            var appConfig = App.Services.GetRequiredService<AppConfigService>();
            var sqlite = App.Services.GetRequiredService<SqliteService>();
            var onlineCount = App.Services.GetRequiredService<OnlineCountService>();
            _webBuilder.Services.AddSingleton(appConfig);
            _webBuilder.Services.AddSingleton(sqlite);
            _webBuilder.Services.AddSingleton(onlineCount);

            _webBuilder.Services.AddAuthentication("Cookie")
                .AddCookie("Cookie", options =>
                {
                    options.LoginPath = "/";
                    options.ExpireTimeSpan = TimeSpan.FromHours(12);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                });
            _webBuilder.Services.AddControllers();

            // _webBuilder.Services.AddCors(options =>
            // {
            //     options.AddPolicy("AllowAll", policy => policy
            //         .AllowAnyOrigin()
            //         .AllowAnyHeader()
            //         .AllowAnyMethod());
            // });

            _web = _webBuilder.Build();
            // _web.UseCors("AllowAll");

            _web.UseDefaultFiles();
            _web.UseStaticFiles();
            _web.UseRouting();

            _web.MapWhen(
                context => context.Request.Path.StartsWithSegments("/api"),
                apiApp =>
                {
                    apiApp.UseRouting();
                    apiApp.UseAuthentication();
                    apiApp.UseAuthorization();
                    apiApp.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });

            _web.MapWhen(
                context => !context.Request.Path.StartsWithSegments("/api"),
                spaApp =>
                {
                    spaApp.UseRouting();
                    spaApp.UseEndpoints(endpoints =>
                    {
                        endpoints.MapFallbackToFile("index.html");
                    });
                });

            try
            {
                if (NetworkHelper.IsPortInUse(appConfigService.NetworkConfig.Port))
                {
                    throw new Exception($"Port {appConfigService.NetworkConfig.Port} is already in use.");
                }
                await _web.StartAsync();
                IsRunning = true;
                ServerStatusChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start web server: {ex.Message}");
                snackbarService.Show(
                    trService.Translate("Error"),
                    trService.Translate("MainWindow_Message_ServerStartupFailed"),
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.Tag24),
                    TimeSpan.FromSeconds(3));
                IsRunning = false;
                return;
            }
        }

        public async Task StopWebServer()
        {
            if (!IsRunning) return;
            IsRunning = false;
            await _web!.StopAsync();
            ServerStatusChanged?.Invoke(this, false);
            await _web.DisposeAsync();
        }
    }
}
