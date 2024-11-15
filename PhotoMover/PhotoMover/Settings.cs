using DotNetConfig;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Serilog;
using System;
using System.IO;

namespace PhotoMover
{
    public class Settings : ReactiveObject
    {
        private readonly Config _config = Config.Build(Path.Combine(ApplicationDataPath, "Config"));
        private bool isWorking = false;
        private bool requiresAppRestart = false;

        public Settings()
        {
        }

        public static string ApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

        public static string AppName => System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Name ?? "PhotoMover";

        public bool RequiresAppRestart
        {
            get => requiresAppRestart;
            set => this.RaiseAndSetIfChanged(ref requiresAppRestart, value);
        }

        public string SelectedDirectory
        {
            get => GetString("Application", "SelectedDirectory", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            set
            {
                _config.GetSection("Application").SetString("SelectedDirectory", value);
                this.RaisePropertyChanged(nameof(SelectedDirectory));
            }
        }

        public string FileFilter
        {
            get => GetString("Application", "FileFilter", "*.*");
            set
            {
                _config.GetSection("Application").SetString("FileFilter", value);
                this.RaisePropertyChanged(nameof(FileFilter));
            }
        }

        public string Grouping
        {
            get => GetString("Application", "Grouping", "{{DateTime}}");
            set
            {
                _config.GetSection("Application").SetString("Grouping", value);
                this.RaisePropertyChanged(nameof(Grouping));
            }
        }

        public string DestinationDirectory
        {
            get => GetString("Application", "DestinationDirectory", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            set
            {
                _config.GetSection("Application").SetString("DestinationDirectory", value);
                this.RaisePropertyChanged(nameof(DestinationDirectory));
            }
        }

        public bool FtpServerEnabled
        {
            get => GetBoolean("FTP", "FtpServerEnabled", false);
            set
            {
                _config.GetSection("FTP").SetBoolean("FtpServerEnabled", value);
                this.RaisePropertyChanged(nameof(FtpServerEnabled));
                RequiresAppRestart = true;
            }
        }

        public string FtpServerIpAdress
        {
            get => GetString("FTP", "IpAddress", "127.0.0.1");
            set
            {
                _config.GetSection("FTP").SetString("IpAddress", value);
                this.RaisePropertyChanged(nameof(FtpServerIpAdress));
                RequiresAppRestart = true;
            }
        }

        public string FtpServerPath
        {
            get => GetPath("FTP", "FtpServerPath", Path.Combine(ApplicationDataPath, "FtpFolder"));

            set
            {
                _config.GetSection("FTP").SetString("FtpServerPath", value);
                this.RaisePropertyChanged(nameof(FtpServerPath));
                RequiresAppRestart = true;
            }
        }

        public string FtpUserName
        {
            get => GetString("FTP", "FtpUserName", "ftp");

            set
            {
                _config.GetSection("FTP").SetString("FtpUserName", value);
                this.RaisePropertyChanged(nameof(FtpUserName));
                RequiresAppRestart = true;
            }
        }

        public string FtpPassword
        {
            get => GetString("FTP", "FtpPassword", "");

            set
            {
                _config.GetSection("FTP").SetString("FtpPassword", value);
                this.RaisePropertyChanged(nameof(FtpPassword));
                RequiresAppRestart = true;
            }
        }

        public bool IsWorking { get => isWorking; set => this.RaiseAndSetIfChanged(ref isWorking, value); }


        private string GetString(string section, string variable, string defaultValue = "")
        {
            string value = defaultValue;
            try
            {
                value = _config.GetSection(section).GetString(variable) ?? defaultValue;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(GetString));
                _config.GetSection(section).SetString(variable, defaultValue);
            }
            return value;
        }

        private bool GetBoolean(string section, string variable, bool defaultValue = false)
        {
            bool value = defaultValue;
            try
            {
                value = _config.GetSection(section).GetBoolean(variable) ?? defaultValue;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(GetBoolean));
                _config.GetSection(section).SetBoolean(variable, defaultValue);
            }
            return value;
        }

        private string GetPath(string section, string variable, string defaultPath)
        {
            string path = GetString(section, variable, defaultPath);
            if (string.IsNullOrWhiteSpace(path))
                path = defaultPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

    }
}