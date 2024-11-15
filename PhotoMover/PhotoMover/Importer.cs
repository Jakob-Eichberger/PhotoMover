using Avalonia.Logging;
using DynamicData.Kernel;
using ExifLibrary;
using ReactiveUI;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoMover
{
    public class Importer : ReactiveObject
    {
        public delegate void FileMovedHandler(MovedFile file);
        public delegate void ProgressReportedHandler(int count, int max);
        public event FileMovedHandler? FilesMovedEvent;
        public event ProgressReportedHandler? ProgressReportedEvent;
        private int filesCount = 0;
        private readonly Settings Settings;
        private Task FtpFilesQueueWatcherTask;
        readonly Mutex _mutex = new(false);

        public Importer(Settings settings)
        {
            Settings = settings;
            FtpFilesQueueWatcherTask = new Task(MoveFilesInFtpFilesQueue);
            FileSystemWatcher.Path = settings.FtpServerPath;
            FileSystemWatcher.IncludeSubdirectories = true;
            FileSystemWatcher.Created += FileSystemWatcher_Created;
            FileSystemWatcher.EnableRaisingEvents = Settings.FtpServerEnabled;
            if (Settings.FtpServerEnabled)
            {
                FtpFilesQueueWatcherTask.Start();
            }
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            _mutex.WaitOne();
            try
            {
                FtpFilesQueue.Add(new FileInfo(e.FullPath));
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public ObservableCollection<FileInfo> FtpFilesQueue { get; } = [];

        public FileSystemWatcher FileSystemWatcher { get; set; } = new();

        public int FilesCount { get => filesCount; set => this.RaiseAndSetIfChanged(ref filesCount, value); }

        public async Task<List<FileInfo>> GetFilesAsync(string path) => await Task.Run(() => new DirectoryInfo(path).GetFiles(Settings.FileFilter, SearchOption.AllDirectories).OrderBy(e => e.CreationTime).ToList());

        private async void MoveFilesInFtpFilesQueue()
        {
            while (true)
            {
                var files = FtpFilesQueue.Where(e => !IsFileLocked(e) && e.Exists).ToList();
                await MoveFilesAsync(files);
                files.ForEach(e=>e.Delete());
                if (files.Any())
                {
                    _mutex.WaitOne();
                    try
                    {
                        files.ForEach(e => FtpFilesQueue.Remove(e));
                    }
                    finally
                    {
                        _mutex.ReleaseMutex();
                    }
                }
                await Task.Delay(new TimeSpan(0, 0, 5));
            }
        }

        public FileInfo GetDestinationFileInfo(FileInfo file)
        {
            var exif = ImageFile.FromFile(file.FullName).Properties.AsArray();
            List<string> paths = Settings.Grouping.Split("\\").Select(e => CreatePath(e, exif)).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            paths.Insert(0, Settings.DestinationDirectory);
            paths.Add(file.Name);
            var path = Path.Combine(paths.ToArray());
            return new FileInfo(path);
        }

        private static string CreatePath(string e, ExifProperty[] exifProperties)
        {
            try
            {
                var exif = Enum.Parse<ExifTag>(e.Replace("{{", "").Replace("}}", ""));
                object obj = exifProperties.First(e => e.Tag == exif).Value;
                return obj switch
                {
                    DateTime dt => dt.ToString("yyyy MM dd"),
                    _ => obj?.ToString() ?? ""
                };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Could not get property value for symbole \"{e}\"");
                return string.Empty;
            }
        }

        public async Task MoveFilesAsync(IEnumerable<FileInfo> files) => await Task.Run(() =>
        {
            FilesCount = files.Count();
            foreach (var file in files)
            {
                FileInfo destinationPath = GetDestinationFileInfo(file);
                if (!destinationPath.Directory.Exists)
                    destinationPath.Directory.Create();
                MoveFile(file, destinationPath);
            }
        });

        public void MoveFile(FileInfo sourceFile, FileInfo destinationFile)
        {
            if (!destinationFile.Exists)
            {
                sourceFile.CopyTo(destinationFile.FullName);
                FilesMovedEvent?.Invoke(new MovedFile()
                {
                    OriginFile = sourceFile,
                    DestinationFile = destinationFile
                });
            }
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (Exception)
            {
                return true;
            }
            return false;
        }
    }
}