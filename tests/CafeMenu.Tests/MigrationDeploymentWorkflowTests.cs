using System.Text.RegularExpressions;

namespace CafeMenu.Tests;

public sealed class MigrationDeploymentWorkflowTests
{
    [Fact]
    public void ApiStartup_ShouldNotRunAutomaticDatabaseMigrations()
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(FindRepositoryRoot(), "src", "CafeMenu.Api"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrMigrationPath(path));

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("Database.Migrate", content, StringComparison.Ordinal);
            Assert.DoesNotContain("MigrateAsync", content, StringComparison.Ordinal);
            Assert.DoesNotContain("EnsureCreated", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MigrationBundleScript_ShouldUseExpectedProjectStartupAndContext()
    {
        var script = ReadRepositoryFile("scripts/database/build-migration-bundle.ps1");

        Assert.Contains("dotnet", script, StringComparison.Ordinal);
        Assert.Contains("ef", script, StringComparison.Ordinal);
        Assert.Contains("migrations", script, StringComparison.Ordinal);
        Assert.Contains("bundle", script, StringComparison.Ordinal);
        Assert.Contains("--project", script, StringComparison.Ordinal);
        Assert.Contains("--startup-project", script, StringComparison.Ordinal);
        Assert.Contains("--context", script, StringComparison.Ordinal);
        Assert.Contains("src/CafeMenu.Api", script, StringComparison.Ordinal);
        Assert.Contains("CafeMenuDbContext", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationBundleScript_ShouldCheckPendingModelChangesBeforeBundleBuild()
    {
        var script = ReadRepositoryFile("scripts/database/build-migration-bundle.ps1");

        Assert.Contains("has-pending-model-changes", script, StringComparison.Ordinal);
        Assert.Contains("SkipPendingModelChangesCheck", script, StringComparison.Ordinal);
        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MigrationBundleScript_ShouldWriteGeneratedArtifactsToIgnoredDirectory()
    {
        var script = ReadRepositoryFile("scripts/database/build-migration-bundle.ps1");
        var gitignore = ReadRepositoryFile(".gitignore");
        var dockerignore = ReadRepositoryFile(".dockerignore");

        Assert.Contains("[string]$OutputDirectory = \".artifacts/migrations\"", script, StringComparison.Ordinal);
        AssertContainsLine(gitignore, ".artifacts/");
        AssertContainsLine(dockerignore, "**/.artifacts");
    }

    [Fact]
    public void MigrationBundleScript_ShouldNotContainSecretsOrHardCodedConnectionString()
    {
        var script = ReadRepositoryFile("scripts/database/build-migration-bundle.ps1");
        var factory = ReadRepositoryFile("src/CafeMenu.Api/Data/CafeMenuDbContextFactory.cs");

        Assert.DoesNotContain("Password=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Username=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", factory, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("change_me_for_local_dev", factory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetConnectionString(\"DefaultConnection\")", factory, StringComparison.Ordinal);
        Assert.Contains("AddEnvironmentVariables", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationBundleScript_ShouldPropagateCommandFailures()
    {
        var script = ReadRepositoryFile("scripts/database/build-migration-bundle.ps1");

        Assert.Contains("$LASTEXITCODE", script, StringComparison.Ordinal);
        Assert.Contains("failed with exit code", script, StringComparison.Ordinal);
        Assert.Contains("exit 1", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationBundleScript_ShouldSupportOptionalRuntimeIdentifierAndSelfContainedBundle()
    {
        var script = ReadRepositoryFile("scripts/database/build-migration-bundle.ps1");

        Assert.Contains("[string]$RuntimeIdentifier", script, StringComparison.Ordinal);
        Assert.Contains("[switch]$SelfContained", script, StringComparison.Ordinal);
        Assert.Contains("--target-runtime", script, StringComparison.Ordinal);
        Assert.Contains("--self-contained", script, StringComparison.Ordinal);
        Assert.DoesNotContain("linux-x64", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DotNetEfToolManifest_ShouldPinEfToolVersionToEfCoreRuntimeVersion()
    {
        var toolManifest = ReadRepositoryFile(".config/dotnet-tools.json");
        var apiProject = ReadRepositoryFile("src/CafeMenu.Api/CafeMenu.Api.csproj");

        Assert.Contains("\"dotnet-ef\"", toolManifest, StringComparison.Ordinal);
        Assert.Contains("\"version\": \"10.0.10\"", toolManifest, StringComparison.Ordinal);
        Assert.Contains("Microsoft.EntityFrameworkCore\" Version=\"10.0.10\"", apiProject, StringComparison.Ordinal);
        Assert.Contains("Microsoft.EntityFrameworkCore.Design\" Version=\"10.0.10\"", apiProject, StringComparison.Ordinal);
    }

    [Fact]
    public void Docs_ShouldDescribeExplicitProductionMigrationBundleWorkflow()
    {
        var databaseGuide = ReadRepositoryFile("docs/DATABASE_CONVENTIONS.md");
        var dockerGuide = ReadRepositoryFile("docs/DOCKER_GUIDE.md");
        var environmentGuide = ReadRepositoryFile("docs/ENVIRONMENT.md");
        var developmentGuide = ReadRepositoryFile("docs/DEVELOPMENT_GUIDE.md");

        Assert.Contains("EF Core Migration Bundle", databaseGuide, StringComparison.Ordinal);
        Assert.Contains("build-migration-bundle.ps1", databaseGuide, StringComparison.Ordinal);
        Assert.Contains("must not run `Database.Migrate`", databaseGuide, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings__DefaultConnection", databaseGuide, StringComparison.Ordinal);
        Assert.Contains("backup and restore readiness", databaseGuide, StringComparison.Ordinal);
        Assert.Contains("corrective forward migration", databaseGuide, StringComparison.Ordinal);

        Assert.Contains("Do not add `dotnet ef database update`", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("separate migration step", dockerGuide, StringComparison.Ordinal);
        Assert.Contains("Migration Bundle Configuration", environmentGuide, StringComparison.Ordinal);
        Assert.Contains("shell history or process listings", environmentGuide, StringComparison.Ordinal);
        Assert.Contains("Production migration deployment is separate from local development", developmentGuide, StringComparison.Ordinal);
    }

    private static bool IsGeneratedOrMigrationPath(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
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
