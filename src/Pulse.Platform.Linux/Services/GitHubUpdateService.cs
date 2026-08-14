using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.Platform.Linux.Services;

public enum UpdateAvailability
{
    Current,
    Ahead,
    Available,
    UnsupportedArchitecture,
    Unavailable
}

public sealed record PulseUpdateResult(
    UpdateAvailability Availability,
    string CurrentVersion,
    string? LatestVersion,
    string Message,
    string? ReleaseNotes = null,
    string? ReleasePageUrl = null,
    string? PackageAssetName = null,
    string? PackageDownloadUrl = null,
    string? ChecksumsDownloadUrl = null);

public sealed record PulseUpdateDownloadResult(bool Succeeded, string Message, string? PackagePath = null);

public sealed class GitHubUpdateService
{
    public const string RepositoryOwner = "yccmmy9n4c-cell";
    public const string RepositoryName = "Pulse";
    public const string ReleasesPageUrl = "https://github.com/yccmmy9n4c-cell/Pulse/releases";
    private const string ReleasesApiUrl = "https://api.github.com/repos/yccmmy9n4c-cell/Pulse/releases?per_page=100";
    private readonly HttpClient _httpClient;

    public GitHubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Pulse-Supernova-Linux", AppInfo.Version));
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<PulseUpdateResult> CheckAsync(
        string currentVersion,
        Architecture architecture,
        CancellationToken cancellationToken = default)
    {
        var architectureName = architecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            _ => null
        };
        if (architectureName is null)
        {
            return new(UpdateAvailability.UnsupportedArchitecture, currentVersion, null,
                $"Pulse updates do not yet support the {architecture} architecture.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            };
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new(UpdateAvailability.Unavailable, currentVersion, null,
                    $"GitHub returned HTTP {(int)response.StatusCode}. Check the connection and try again.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return EvaluateReleaseList(json, currentVersion, architectureName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(UpdateAvailability.Unavailable, currentVersion, null,
                "The GitHub update check timed out. Check the connection and try again.");
        }
        catch (HttpRequestException ex)
        {
            return new(UpdateAvailability.Unavailable, currentVersion, null,
                $"Pulse could not reach GitHub. {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new(UpdateAvailability.Unavailable, currentVersion, null,
                $"GitHub returned update information Pulse could not read. {ex.Message}");
        }
    }

    public static PulseUpdateResult EvaluateReleaseList(string json, string currentVersion, string debianArchitecture)
    {
        var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json) ?? [];
        if (!Version.TryParse(currentVersion, out var installedVersion))
        {
            return new(UpdateAvailability.Unavailable, currentVersion, null,
                "Pulse could not interpret its installed version.");
        }

        var candidates = releases
            .Where(release => !release.Draft)
            .Select(release => CreateCandidate(release, debianArchitecture))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.Version)
            .ToArray();
        if (candidates.Length == 0)
        {
            return new(UpdateAvailability.Unavailable, currentVersion, null,
                $"No published Pulse Linux package for {debianArchitecture} was found. The repository may not have a compatible release yet.",
                ReleasePageUrl: ReleasesPageUrl);
        }

        var latest = candidates[0];
        if (latest.Version < installedVersion)
        {
            return new(UpdateAvailability.Ahead, currentVersion, latest.Version.ToString(4),
                $"Installed Pulse Supernova Linux {currentVersion} is newer than the newest published compatible version {latest.Version.ToString(4)}.",
                latest.Release.Body, latest.Release.HtmlUrl);
        }

        if (latest.Version == installedVersion)
        {
            return new(UpdateAvailability.Current, currentVersion, latest.Version.ToString(4),
                $"Pulse Supernova Linux {currentVersion} is current.", latest.Release.Body, latest.Release.HtmlUrl);
        }

        return new(UpdateAvailability.Available, currentVersion, latest.Version.ToString(4),
            $"Pulse Supernova Linux {latest.Version.ToString(4)} is available.",
            latest.Release.Body, latest.Release.HtmlUrl, latest.Package.Name,
            latest.Package.BrowserDownloadUrl, latest.Checksums.BrowserDownloadUrl);
    }

    public async Task<PulseUpdateDownloadResult> DownloadAndVerifyAsync(
        PulseUpdateResult update,
        string downloadsDirectory,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (update.Availability != UpdateAvailability.Available ||
            string.IsNullOrWhiteSpace(update.PackageAssetName) ||
            string.IsNullOrWhiteSpace(update.PackageDownloadUrl) ||
            string.IsNullOrWhiteSpace(update.ChecksumsDownloadUrl))
        {
            return new(false, "No verified update package is ready to download.");
        }

        Directory.CreateDirectory(downloadsDirectory);
        var packagePath = Path.Combine(downloadsDirectory, update.PackageAssetName);
        var temporaryPath = packagePath + ".part";
        try
        {
            var checksumText = await _httpClient.GetStringAsync(update.ChecksumsDownloadUrl, cancellationToken);
            var expectedHash = FindExpectedHash(checksumText, update.PackageAssetName);
            if (expectedHash is null)
            {
                return new(false, "The release checksum file does not contain the selected package. Nothing was installed.");
            }

            using var response = await _httpClient.GetAsync(update.PackageDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    received += read;
                    if (total is > 0)
                    {
                        progress?.Report((int)Math.Clamp(received * 100 / total.Value, 0, 100));
                    }
                }
            }

            string actualHash;
            await using (var packageStream = File.OpenRead(temporaryPath))
            {
                actualHash = Convert.ToHexString(await SHA256.HashDataAsync(
                    packageStream, cancellationToken)).ToLowerInvariant();
            }
            if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(temporaryPath);
                return new(false, "Checksum verification failed. Pulse deleted the incomplete package and will not open it.");
            }

            File.Move(temporaryPath, packagePath, true);
            progress?.Report(100);
            return new(true, $"The verified update was saved to {packagePath}.", packagePath);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            DeleteTemporaryFile(temporaryPath);
            return new(false, "The update download timed out. No package was installed.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            DeleteTemporaryFile(temporaryPath);
            return new(false, $"Pulse could not download the update. {ex.Message}");
        }
    }

    private static ReleaseCandidate? CreateCandidate(GitHubRelease release, string architecture)
    {
        var version = ReadVersion(release.TagName) ?? ReadVersion(release.Name);
        if (version is null)
        {
            return null;
        }

        var expectedPackage = $"pulse-platform_{version.ToString(4)}_{architecture}.deb";
        var assets = release.Assets ?? [];
        var package = assets.FirstOrDefault(asset => asset.Name.Equals(expectedPackage, StringComparison.Ordinal));
        var checksums = assets.FirstOrDefault(asset => asset.Name.Equals("SHA256SUMS", StringComparison.Ordinal));
        return package is null || checksums is null ? null : new(version, release, package, checksums);
    }

    private static Version? ReadVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var token in value.Split([' ', '-', '_', 'v'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Version.TryParse(token, out var version) && version.Revision >= 0)
            {
                return version;
            }
        }

        return null;
    }

    private static string? FindExpectedHash(string checksumText, string packageName)
    {
        foreach (var line in checksumText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var checksumEntryName = Path.GetFileName(pieces.Length >= 2 ? pieces[1].TrimStart('*') : string.Empty);
            if (pieces.Length >= 2 && checksumEntryName.Equals(packageName, StringComparison.Ordinal) &&
                pieces[0].Length == 64 && pieces[0].All(Uri.IsHexDigit))
            {
                return pieces[0].ToLowerInvariant();
            }
        }

        return null;
    }

    private static void DeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // The original download error remains the useful user-facing result.
        }
    }

    private sealed record ReleaseCandidate(Version Version, GitHubRelease Release, GitHubAsset Package, GitHubAsset Checksums);

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("html_url")] string? HtmlUrl,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset>? Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl,
        [property: JsonPropertyName("size")] long Size);
}
