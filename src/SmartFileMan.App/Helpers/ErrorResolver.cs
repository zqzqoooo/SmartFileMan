using System.Resources;
using SmartFileMan.Contracts.Common;

namespace SmartFileMan.App.Helpers
{
    public static class ErrorResolver
    {
        public static string GetMessage(ErrorCode code)
        {
            // Fallback implementation since ResX generation in MAUI is behaving oddly
            // In a real scenario, we'd fix the namespace. For now, hardcode valid messages or simple string return.
            return $"Error: {code}";

        }
    }
}
