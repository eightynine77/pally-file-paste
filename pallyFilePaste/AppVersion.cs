using System.Linq;
using System.Reflection;

namespace pallyFilePaste;

public static class AppVersion
{
    public static string GetVersion(Assembly assembly) =>
        assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown";

    public static string GetReleaseDate(Assembly assembly) =>
        assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "ReleaseDate")?.Value ?? "Unknown";
}