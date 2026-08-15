using System.Text.RegularExpressions;

namespace CafeMenu.Tests;

public sealed class DockerProductionHardeningTests
{
    [Theory]
    [InlineData("src/CafeMenu.Api/Dockerfile", "CafeMenu.Api.dll")]
    [InlineData("src/CafeMenu.Web/Dockerfile", "CafeMenu.Web.dll")]
    public void Dockerfile_ShouldUseSecureMultiStageNonRootRuntime(string dockerfilePath, string assemblyName)
    {
        var dockerfile = ReadRepositoryFile(dockerfilePath);

        Assert.Contains("FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime", dockerfile, StringComparison.Ordinal);
        Assert.Contains("FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build", dockerfile, StringComparison.Ordinal);
        Assert.Contains("FROM runtime AS final", dockerfile, StringComparison.Ordinal);
        Assert.Contains("dotnet restore", dockerfile, StringComparison.Ordinal);
        Assert.Contains("dotnet publish", dockerfile, StringComparison.Ordinal);
        Assert.Contains("COPY --from=build /app/publish .", dockerfile, StringComparison.Ordinal);
        Assert.Contains("USER $APP_UID", dockerfile, StringComparison.Ordinal);
        Assert.Contains($"""ENTRYPOINT ["dotnet", "{assemblyName}"]""", dockerfile, StringComparison.Ordinal);
        Assert.Contains("ENV ASPNETCORE_HTTP_PORTS=8080", dockerfile, StringComparison.Ordinal);
        Assert.Contains("EXPOSE 8080", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_URLS", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HEALTHCHECK", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apt-get", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apk add", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("curl", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wget", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", dockerfile, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", dockerfile, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiDockerfile_ShouldPrepareMediaMountPointForNonRootRuntime()
    {
        var dockerfile = ReadRepositoryFile("src/CafeMenu.Api/Dockerfile");

        Assert.Contains("/var/cafemenu/media", dockerfile, StringComparison.Ordinal);
        Assert.Contains("chown -R $APP_UID:0 /var/cafemenu", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void WebDockerfile_ShouldPrepareDataProtectionMountPointForNonRootRuntime()
    {
        var dockerfile = ReadRepositoryFile("src/CafeMenu.Web/Dockerfile");

        Assert.Contains("/var/cafemenu/data-protection", dockerfile, StringComparison.Ordinal);
        Assert.Contains("chown -R $APP_UID:0 /var/cafemenu", dockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void DockerCompose_ShouldKeepDevelopmentPortsVolumesAndNativeDependencyHealthChecks()
    {
        var compose = ReadRepositoryFile("docker-compose.yml");

        Assert.Contains("ASPNETCORE_HTTP_PORTS: 8080", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_URLS", compose, StringComparison.Ordinal);
        Assert.Contains("media_data:/var/cafemenu/media", compose, StringComparison.Ordinal);
        Assert.Contains("pg_isready", compose, StringComparison.Ordinal);
        Assert.Contains("""test: ["CMD", "redis-cli", "ping"]""", compose, StringComparison.Ordinal);
        Assert.Contains("""${API_HTTP_PORT:-8080}:8080""", compose, StringComparison.Ordinal);
        Assert.Contains("""${WEB_HTTP_PORT:-8081}:8080""", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Dockerignore_ShouldExcludeLocalArtifactsAndSecretsWithoutExcludingEnvExample()
    {
        var dockerignore = ReadRepositoryFile(".dockerignore");

        AssertContainsLine(dockerignore, "**/.git");
        AssertContainsLine(dockerignore, "**/bin");
        AssertContainsLine(dockerignore, "**/obj");
        AssertContainsLine(dockerignore, "**/TestResults");
        AssertContainsLine(dockerignore, ".env");
        AssertContainsLine(dockerignore, ".env.*");
        AssertContainsLine(dockerignore, "!.env.example");
        AssertContainsLine(dockerignore, "media/");
        AssertContainsLine(dockerignore, "uploads/");
        AssertContainsLine(dockerignore, "data-protection-keys/");
        AssertContainsLine(dockerignore, "DataProtection-Keys/");
        AssertContainsLine(dockerignore, "**/*.dump");
        AssertContainsLine(dockerignore, "**/*.sql");
    }

    [Fact]
    public void DockerDocs_ShouldDescribeNonRootPortsWritablePathsAndHealthProbes()
    {
        var dockerGuide = ReadRepositoryFile("docs/DOCKER_GUIDE.md");
        var environmentGuide = ReadRepositoryFile("docs/ENVIRONMENT.md");

        Assert.Contains("non-root `APP_UID`", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("internal container port `8080`", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("/var/cafemenu/media", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("/var/cafemenu/data-protection", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("/health/live", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("/health/ready", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("do not install curl, wget", dockerGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASPNETCORE_HTTP_PORTS=8080", environmentGuide, StringComparison.Ordinal);
        Assert.Contains("non-root `APP_UID`", environmentGuide, StringComparison.Ordinal);
    }

    private static void AssertContainsLine(string content, string expectedLine)
    {
        var pattern = $"^{Regex.Escape(expectedLine)}$";

        Assert.Matches(
            new Regex(pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant),
            content);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CafeMenu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located.");
    }
}
