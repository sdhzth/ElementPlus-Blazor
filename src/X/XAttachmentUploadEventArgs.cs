using ElementPlus;
using System;

namespace ElementPlus.X
{
    public class XAttachmentUploadEventArgs : EventArgs
    {
        public XAttachmentItem Item { get; set; }

        public IFileModel File { get; set; }

        public UploadRequestResult Response { get; set; }

        public Exception Exception { get; set; }
    }
}
