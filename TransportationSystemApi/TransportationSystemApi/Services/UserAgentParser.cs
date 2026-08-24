namespace TransportationSystemApi.Services;

// Deliberately lightweight -- just enough to label login history entries,
// not a full device-fingerprinting library.
public static class UserAgentParser
{
    public static (string? Browser, string? OperatingSystem) Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return (null, null);

        string? browser =
            userAgent.Contains("Edg/") ? "Edge" :
            userAgent.Contains("OPR/") || userAgent.Contains("Opera") ? "Opera" :
            userAgent.Contains("Chrome/") ? "Chrome" :
            userAgent.Contains("Firefox/") ? "Firefox" :
            userAgent.Contains("Safari/") && userAgent.Contains("Version/") ? "Safari" :
            null;

        string? os =
            userAgent.Contains("Windows NT") ? "Windows" :
            userAgent.Contains("Mac OS X") ? "macOS" :
            userAgent.Contains("Android") ? "Android" :
            userAgent.Contains("iPhone") || userAgent.Contains("iPad") ? "iOS" :
            userAgent.Contains("Linux") ? "Linux" :
            null;

        return (browser, os);
    }
}
