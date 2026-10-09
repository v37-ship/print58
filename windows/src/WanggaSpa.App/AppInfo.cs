using System.IO;
using System.Reflection;

namespace WanggaSpa.App;

public static class AppInfo
{
    public const string ReleasesUrl = "https://github.com/v37-ship/print58/releases";

    public static string Version =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";

    public static string Runtime =>
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public static string BuildTime
    {
        get
        {
            try
            {
                var path = Assembly.GetExecutingAssembly().Location;
                return string.IsNullOrEmpty(path) ? "?"
                    : File.GetLastWriteTime(path).ToString("dd/MM/yyyy HH:mm");
            }
            catch { return "?"; }
        }
    }
}
