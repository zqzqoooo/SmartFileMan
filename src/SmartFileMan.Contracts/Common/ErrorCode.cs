namespace SmartFileMan.Contracts.Common
{
    public enum ErrorCode
    {
        // 0 - 999: System Errors
        Success = 0,
        UnknownError = 1,
        OperationCancelled = 2,

        // 1000 - 1999: File Operation Errors
        FileNotFound = 1001,
        AccessDenied = 1002,
        FileLocked = 1003,
        PathTooLong = 1004,
        DiskFull = 1005,

        // 2000 - 2999: Plugin Errors
        PluginLoadFailed = 2001,
        PluginSignatureInvalid = 2002,
        PluginCrashed = 2003,
        PluginTimeout = 2004,

        // 3000 - 3999: Data/Logic Errors
        InvalidConfiguration = 3001,
        DatabaseError = 3002
    }
}
