using System;

namespace ElementPlus
{
    public class UploadChangeEventArgs : EventArgs
    {
        public IFileModel File { get; set; }

        public IFileModel[] FileList { get; set; } = Array.Empty<IFileModel>();
    }
}
