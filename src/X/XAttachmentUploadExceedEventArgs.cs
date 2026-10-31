using ElementPlus;
using System;

namespace ElementPlus.X
{
    public class XAttachmentUploadExceedEventArgs : EventArgs
    {
        public UploadExceedEventArgs UploadArgs { get; set; }

        public string Message { get; set; }
    }
}
