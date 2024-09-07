using DotNetConfig;
using ReactiveUI;

namespace PhotoMover
{
    public class Settings : ReactiveObject
    {
        private readonly Config _config;
        private bool isWorking = false;

        public Settings(Config config)
        {
            _config = config;
        }

        public string SelectedDirectory
        {
            get => _config.GetSection("Application").GetString("SelectedDirectory") ?? ""; set
            {
                _config.GetSection("Application").SetString("SelectedDirectory", value);
                this.RaisePropertyChanged(nameof(SelectedDirectory));
            }
        }

        public string FileFilter
        {
            get => _config.GetSection("Application").GetString("FileFilter") ?? "*.*"; set
            {
                _config.GetSection("Application").SetString("FileFilter", value);
                this.RaisePropertyChanged(nameof(FileFilter));
            }
        }

        public string DestinationDirectory
        {
            get => _config.GetSection("Application").GetString("DestinationDirectory") ?? ""; set
            {
                _config.GetSection("Application").SetString("DestinationDirectory", value);
                this.RaisePropertyChanged(nameof(DestinationDirectory));
            }
        }

        public bool IsWorking { get => isWorking; set => this.RaiseAndSetIfChanged(ref isWorking, value); }

    }
}