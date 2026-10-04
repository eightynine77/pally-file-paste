using System;
using System.Linq;
using System.Reflection;

namespace pallyFilePaste;

public static class AppVersion
{
    private static Assembly Assembly =>
        typeof(AppVersion).Assembly;

    private static string GetMetadata(string key) =>
        Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == key)?.Value ?? "Unknown";

    public static string GetVersion()
    {
        if (OperatingSystem.IsAndroid())
            return GetMetadata("AndroidVersion");

        if (OperatingSystem.IsWindows())
            return GetMetadata("WindowsVersion");

        if (OperatingSystem.IsLinux())
            return GetMetadata("LinuxVersion");

        return "Unknown";
    }

    public static string GetReleaseDate()
    {
        if (OperatingSystem.IsAndroid())
            return GetMetadata("AndroidReleaseDate");

        if (OperatingSystem.IsWindows())
            return GetMetadata("WindowsReleaseDate");

        if (OperatingSystem.IsLinux())
            return GetMetadata("LinuxReleaseDate");

        return "Unknown";
    }
}