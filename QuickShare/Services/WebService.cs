using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuickShare.Helpers;
using Serilog;

namespace QuickShare.Services
{
    public class WebService(
        ILogger logger,
        SqliteService sqliteService,
        AppConfigService appConfigService,
        MessageBoxService messageBoxService,
        OnlineCountService onlineCountService,
        CertificateService certificateService,
        DownloadTicketService downloadTicketService,
        RequestConfirmService requestConfirmService)
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

            _webBuilder.Services.AddSingleton(logger);
            _webBuilder.Services.AddSingleton(sqliteService);
            _webBuilder.Services.AddSingleton(appConfigService);
            _webBuilder.Services.AddSingleton(onlineCountService);
            _webBuilder.Services.AddSingleton(downloadTicketService);
            _webBuilder.Services.AddSingleton(requestConfirmService);

            _webBuilder.Services.AddAuthentication("Cookie")
                .AddCookie("Cookie", options =>
                {
                    options.LoginPath = "/";
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                });
            _webBuilder.Services.AddControllers();

            // 跨域配置
#if DEBUG
            _webBuilder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });
#endif
            _web = _webBuilder.Build();
#if DEBUG
            _web.UseCors("AllowAll");
#endif

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
                logger.Error($"Failed to start web server: {ex.Message}");
                _ = messageBoxService.ShowMessage(
                    "错误",
                    $"文件传输服务器启动失败：{ex.Message}");
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
