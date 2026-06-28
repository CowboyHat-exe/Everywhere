using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Platform.Storage;

namespace Everywhere.Interop;

public sealed class BetterBclLauncher : ILauncher
{
    public static ILauncher Shared { get; } = new BetterBclLauncher();

    private BetterBclLauncher() { }

    public Task<bool> LaunchUriAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return Task.FromResult(uri.IsAbsoluteUri && Exec(uri.AbsoluteUri));
    }

    /// <summary>
    /// This Process based implementation doesn't handle the case, when there is no app to handle link.
    /// It will still return true in this case.
    /// </summary>
    public Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        ArgumentNullException.ThrowIfNull(storageItem);

        return storageItem.TryGetLocalPath() is { } localPath ? Task.FromResult(Exec(localPath)) : Task.FromResult(false);
    }

    private static bool Exec(string urlOrFile)
    {
        if (OperatingSystem.IsLinux())
        {
            // Use ArgumentList to pass the argument directly to xdg-open without shell interpretation,
            // avoiding command injection via crafted file paths or URIs.
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-open",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            psi.ArgumentList.Add(urlOrFile);

            try
            {
                using var process = Process.Start(psi);
            }
            catch
            {
                // Best-effort: xdg-open may not be available
            }

            return true;
        }

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? urlOrFile : "open",
                CreateNoWindow = true,
                UseShellExecute = OperatingSystem.IsWindows()
            };

            if (OperatingSystem.IsMacOS())
            {
                psi.ArgumentList.Add(urlOrFile); // Use ArgumentList to avoid issues with spaces/special characters
            }

            try
            {
                using var process = Process.Start(psi);
            }
            catch (Win32Exception e) when (OperatingSystem.IsWindows() && e.NativeErrorCode == -2147221003)
            {
                // ERROR_NO_ASSOCIATION: No application is associated with the specified file for this operation.
                // Fall back to explorer to select the file
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                    ArgumentList = { "/select,", urlOrFile },
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }

            return true;
        }

        return false;
    }
}