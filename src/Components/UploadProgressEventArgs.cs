using System;

namespace ElementPlus
{
    public class UploadProgressEventArgs : EventArgs
    {
        public IFileModel File { get; set; }

        public IFileModel[] FileList { get; set; } = Array.Empty<IFileModel>();

        public double Percent { get; set; }
    }
}
