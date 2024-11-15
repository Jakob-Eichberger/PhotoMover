using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PhotoMover
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var settings = new Settings();

            string logFilePath = Path.Combine(Settings.ApplicationDataPath, "Log", "log.txt");
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                        .CreateLogger();

            var services = new ServiceCollection();

            services.Configure<DotNetFileSystemOptions>(opt => opt.RootPath = settings.FtpServerPath);
            services.AddFtpServer(builder => builder.UseDotNetFileSystem());
            services.Configure<FtpServerOptions>(opt => opt.ServerAddress = settings.FtpServerIpAdress);
            services.AddSingleton<IMembershipProvider, CustomMembershipProvider>();

            services.AddSingleton(settings);
            services.AddSingleton<Importer>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow(serviceProvider);
            }

            base.OnFrameworkInitializationCompleted();
        }

        public class CustomMembershipProvider(Settings settings) : IMembershipProvider
        {
            public Settings Settings { get; } = settings;

            public Task<MemberValidationResult> ValidateUserAsync(string username, string password)
            {
                if (username == Settings.FtpUserName && password == Settings.FtpPassword)
                {
                    return Task.FromResult(new MemberValidationResult(MemberValidationStatus.AuthenticatedUser, new User(username)));
                }

                return Task.FromResult(new MemberValidationResult(MemberValidationStatus.InvalidLogin));
            }
        }

        public class User : IFtpUser
        {
            private string username;

            public User(string username)
            {
                this.username = username;
            }

            public string Name => username;

            public bool IsInGroup(string groupName)
            {
                return true;
            }
        }
    }
}