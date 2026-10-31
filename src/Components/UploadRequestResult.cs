namespace ElementPlus
{
    public class UploadRequestResult
    {
        public bool Succeeded { get; set; }

        public string Code { get; set; }

        public string Message { get; set; }

        public string Id { get; set; }

        public string Url { get; set; }

        /// <summary>上传接口返回的原始响应体（JSON 文本）。
        /// 业务字段不止 message/id/url 的宿主（例如还要读 fcode、创建时间）在 <see cref="ElementPlus.ElUpload.OnSuccess"/>
        /// 里读这一条自己解析，不用再为每个键加一个参数。</summary>
        public string RawResponse { get; set; }

        public static UploadRequestResult Success(string id = null, string url = null, string message = null)
        {
            return new UploadRequestResult
            {
                Succeeded = true,
                Code = "0",
                Id = id,
                Url = url,
                Message = message
            };
        }

        public static UploadRequestResult Failure(string message = null, string code = null)
        {
            return new UploadRequestResult
            {
                Succeeded = false,
                Code = code ?? "1",
                Message = message
            };
        }

        internal static UploadRequestResult FromJsResult(string[] results)
        {
            if (results == null || results.Length == 0)
            {
                return Failure("Upload failed.");
            }

            return new UploadRequestResult
            {
                Succeeded = results[0] == "0",
                Code = results[0],
                Message = results.Length > 1 ? results[1] : null,
                Id = results.Length > 2 ? results[2] : null,
                Url = results.Length > 3 ? results[3] : null,
                RawResponse = results.Length > 4 ? results[4] : null
            };
        }
    }
}
