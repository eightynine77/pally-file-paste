using System.Linq;
using System.Reflection;

namespace pallyFilePaste;

public static class AppVersion
{
    private static Assembly Assembly =>
        typeof(AppVersion).Assembly;

    public static string GetVersion() =>
        Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown";

    public static string GetReleaseDate() =>
        Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "ReleaseDate")?.Value ?? "Unknown";
}