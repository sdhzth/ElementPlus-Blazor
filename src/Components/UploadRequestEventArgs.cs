using System;

namespace ElementPlus
{
    public class UploadRequestEventArgs : EventArgs
    {
        public IFileModel File { get; set; }

        public IFileModel[] FileList { get; set; } = Array.Empty<IFileModel>();

        public UploadRequestResult Response { get; set; }
    }
}
