namespace SmartFileMan.Contracts.Common
{
    public record OperationResult(bool IsSuccess, string Message, Exception? Error = null)
    {
        public static OperationResult Success(string msg = "OK") => new(true, msg);
        public static OperationResult Fail(string msg, Exception? ex = null) => new(false, msg, ex);
    }
}