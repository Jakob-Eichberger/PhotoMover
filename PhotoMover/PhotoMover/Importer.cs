using DotNetConfig;
using HarfBuzzSharp;
using ReactiveUI;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoMover
{
    public class Importer(Settings settings) : ReactiveObject
    {
        public delegate void FileMovedHandler(MovedFile file);
        public delegate void ProgressReportedHandler(int count, int max);

        private int filesCount = 0;
        private readonly Settings Settings = settings;

        public event FileMovedHandler? FilesMovedEvent;
        public event ProgressReportedHandler? ProgressReportedEvent;

        public int FilesCount { get => filesCount; set => this.RaiseAndSetIfChanged(ref filesCount, value); }

        public async Task<IOrderedEnumerable<FileInfo>> GetFilesAsync() => await Task.Run(() => new DirectoryInfo(Settings.SelectedDirectory).GetFiles(Settings.FileFilter, SearchOption.AllDirectories).OrderBy(e => e.CreationTime));

        public FileInfo GetDestinationFileInfo(FileInfo file)
        {
            return new FileInfo(Path.Combine(Settings.DestinationDirectory, file.CreationTime.ToString("yyyy MM dd"), file.Name));
        }

        public async Task MoveFilesAsync(IEnumerable<FileInfo> files) => await Task.Run(async () =>
        {
            Parallel.ForEach(files.Select(e => GetDestinationFileInfo(e)).Select(e => e.Directory).Where(e => !e.Exists), (d) => d.Create());
            FilesCount = files.Count();

            await Parallel.ForEachAsync(files, async (file, _) =>
            {
                FileInfo destinationPath = GetDestinationFileInfo(file);
                await MoveFile(file, destinationPath);
            });
        });

        public async Task MoveFile(FileInfo sourceFile, FileInfo destinationFile)
        {
            if (!destinationFile.Exists)
            {
                sourceFile.CopyTo(destinationFile.FullName);
                //await CopyFileAsync(sourceFile.FullName, destinationFile.FullName);
                FilesMovedEvent?.Invoke(new MovedFile()
                {
                    OriginFile = sourceFile,
                    DestinationFile = destinationFile
                });
            }
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await sourceStream.CopyToAsync(destinationStream);
        }
    }
}