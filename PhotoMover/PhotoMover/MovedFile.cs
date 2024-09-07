using ReactiveUI;
using System.IO;

namespace PhotoMover
{
    public class MovedFile : ReactiveObject
    {
        private FileInfo originFile;
        private FileInfo destinationFile;

        public FileInfo OriginFile { get => originFile; set => this.RaiseAndSetIfChanged(ref originFile, value); }
        public FileInfo DestinationFile { get => destinationFile; set => this.RaiseAndSetIfChanged(ref destinationFile, value); }
    }
}