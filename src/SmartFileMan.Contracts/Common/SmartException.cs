using System;

namespace SmartFileMan.Contracts.Common
{
    public class SmartException : Exception
    {
        public ErrorCode Code { get; }
        public object? ContextData { get; }

        public SmartException(ErrorCode code, string message = null, Exception inner = null, object? contextData = null)
            : base(message ?? $"Error Code: {code}", inner)
        {
            Code = code;
            ContextData = contextData;
        }
    }
}
