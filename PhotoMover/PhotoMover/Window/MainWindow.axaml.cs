using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using DotNetConfig;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using FubarDev.FtpServer;

namespace PhotoMover
{
    public partial class MainWindow : Window
    {
        readonly Mutex _mutex = new(false);

        public MainWindow(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Settings = serviceProvider.GetService<Settings>()!;
            Importer = serviceProvider.GetService<Importer>()!;
            Importer.FilesMovedEvent += Importer_FilesMovedEvent;
            FtpServer = (serviceProvider.GetRequiredService<IFtpServer>() as FtpServer)!;
            DataContext = this;
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (Settings.FtpServerEnabled)
                await FtpServer.StartAsync(CancellationToken.None);
        }

        public Settings Settings { get; }

        public Importer Importer { get; }

        public ObservableCollection<MovedFile> ImportedFiles { get; } = [];

        public IServiceProvider ServiceProvider { get; }

        public FtpServer FtpServer { get; }

        private void Importer_FilesMovedEvent(MovedFile file)
        {
            _mutex.WaitOne();
            try
            {
                Dispatcher.UIThread.Invoke(new Action(() =>
                {
                    ImportedFiles.Add(file);
                }));
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private async void BtnOpenFolderDialog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new();
            if (Directory.Exists(Settings.SelectedDirectory))
            {
                ofd.Directory = Settings.SelectedDirectory;
            }
            Settings.SelectedDirectory = await ofd.ShowAsync(this);
        }

        private async void BtnSelectDestinationFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new();
            if (Directory.Exists(Settings.DestinationDirectory))
            {
                ofd.Directory = Settings.DestinationDirectory;
            }
            Settings.DestinationDirectory = await ofd.ShowAsync(this);
        }

        private async void BtnImportFiles_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Settings.IsWorking = true;
            try
            {
                ImportedFiles.Clear();
                List<FileInfo> files = await Importer.GetFilesAsync(Settings.SelectedDirectory);
                await Importer.MoveFilesAsync(files);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, nameof(BtnImportFiles_Click));
                await MessageBoxManager.GetMessageBoxStandard("Error", ex.Message, ButtonEnum.Ok).ShowAsync();
            }
            finally
            {
                Settings.IsWorking = false;
            }
        }

        private async void BtnSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await new SettingsWindow(ServiceProvider).ShowDialog(this);
        }
    }
}