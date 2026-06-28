using System.Text.RegularExpressions;

namespace Everywhere.Common;

/// <summary>
/// Shared utility for parsing version strings from update package file names.
/// Eliminates duplicated TryParseUpdatePackageVersion logic across platform implementations.
/// </summary>
public static class UpdateVersionParser
{
    /// <summary>
    /// Attempts to extract a version from an update package file name using the provided regex.
    /// The regex must contain a named capture group called "version".
    /// </summary>
    /// <param name="fileName">The file name to parse.</param>
    /// <param name="versionRegex">A compiled regex with a "version" named group.</param>
    /// <param name="version">The parsed version, or null if parsing fails.</param>
    /// <returns>True if a valid version was extracted; otherwise false.</returns>
    public static bool TryParse(string fileName, Regex versionRegex, out Version? version)
    {
        var match = versionRegex.Match(fileName);
        if (match.Success && Version.TryParse(match.Groups["version"].Value, out version))
        {
            return true;
        }

        version = null;
        return false;
    }
}
