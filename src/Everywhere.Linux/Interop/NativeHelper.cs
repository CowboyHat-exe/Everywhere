using System.Diagnostics;
using Avalonia.Input;
using Everywhere.Extensions;
using Everywhere.Interop;

namespace Everywhere.Linux.Interop;

public class NativeHelper(IEventHelper eventHelper) : INativeHelper
{
    private static string AutostartFolder => 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/autostart");

    private static string ShortcutFile => Path.Combine(AutostartFolder, "Everywhere.desktop");

    /// <summary>
    /// Gets the proper executable path, handling AppImage environments.
    /// </summary>
    private static string GetExecutablePath()
    {
        // If running as AppImage, use the environment variable provided by the AppImage runtime
        string? appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
        if (!string.IsNullOrEmpty(appImagePath))
        {
            return appImagePath;
        }

        // Fallback to current process path
        return Process.GetCurrentProcess().MainModule?.FileName ?? "/usr/bin/Everywhere";
    }

    public bool IsInstalled => File.Exists("/usr/bin/Everywhere");

    public bool IsAdministrator => Environment.UserName == "root";

    public bool IsUserStartupEnabled
    {
        get => File.Exists(ShortcutFile);
        set
        {
            if (value)
            {
                string execPath = GetExecutablePath();
                string content =
                    $"""
                    [Desktop Entry]
                    Type=Application
                    Name=Everywhere
                    Comment=Everywhere Startup Service
                    Exec="{execPath}"
                    Icon=Everywhere
                    Terminal=false
                    Categories=Utility;
                    X-GNOME-Autostart-enabled=true
                    """;

                if (!Directory.Exists(AutostartFolder))
                    Directory.CreateDirectory(AutostartFolder);

                File.WriteAllText(ShortcutFile, content);
            }
            else if (File.Exists(ShortcutFile))
            {
                File.Delete(ShortcutFile);
            }
        }
    }

    public bool IsAdministratorStartupEnabled { get; set; }

    public bool IsLowDataModeActive => throw new NotImplementedException();

    public void RestartAsAdministrator()
    {
        throw new NotSupportedException();
    }

    public bool GetKeyState(KeyModifiers keyModifiers)
    {
        return eventHelper.GetKeyState(keyModifiers);
    }

    public Task<bool> ShowDesktopNotificationAsync(string message, string? title = null)
    {
        // Use ArgumentList to avoid shell injection via crafted notification content
        var psi = new ProcessStartInfo
        {
            FileName = "notify-send",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-u");
        psi.ArgumentList.Add("normal");
        psi.ArgumentList.Add(title ?? "Everywhere");
        psi.ArgumentList.Add(message);

        try
        {
            Process.Start(psi);
        }
        catch
        {
            // Best-effort: notify-send may not be available
        }

        return Task.FromResult(false);
    }

    public void OpenFileLocation(string fullPath)
    {
        if (fullPath.IsNullOrWhiteSpace()) return;

        // Use ArgumentList to pass the path directly without shell interpretation
        var psi = new ProcessStartInfo
        {
            FileName = "xdg-open",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(Path.GetDirectoryName(fullPath) ?? fullPath);

        try
        {
            Process.Start(psi);
        }
        catch
        {
            // Best-effort: xdg-open may not be available
        }
    }
}