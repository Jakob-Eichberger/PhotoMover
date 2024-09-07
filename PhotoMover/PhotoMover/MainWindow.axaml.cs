using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using DotNetConfig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

namespace PhotoMover
{
    public partial class MainWindow : Window
    {
        Mutex _mutex = new Mutex(false);

        public MainWindow()
        {
            Config config = Config.Build();
            Settings = new Settings(config);
            Importer = new Importer(Settings);
            Importer.FilesMovedEvent += Importer_FilesMovedEvent;
            DataContext = this;
            InitializeComponent();
        }

        public Settings Settings { get; }

        public Importer Importer { get; }

        public ObservableCollection<MovedFile> ImportedFiles { get; } = [];

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
                var files = await Importer.GetFilesAsync();
                await Importer.MoveFilesAsync(files);
            }
            //catch (Exception)
            //{

            //    throw;
            //}
            finally
            {
                Settings.IsWorking = false;

            }
        }
    }
}