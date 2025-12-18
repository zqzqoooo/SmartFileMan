namespace SmartFileMan.Contracts.Common
{
    /// <summary>
    /// 标准操作结果，告诉调用者发生了什么
    /// </summary>
    public record OperationResult(bool IsSuccess, string Message, Exception? Error = null)
    {
        public static OperationResult Success(string msg = "OK") => new(true, msg);
        public static OperationResult Fail(string msg, Exception? ex = null) => new(false, msg, ex);
    }
}