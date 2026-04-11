using DotNet.Testcontainers.Builders;
using MarathonTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using Testcontainers.MsSql;

namespace MarathonTraining.Api.IntegrationTests.Hooks;

/// <summary>
/// Manages a single SQL Server Testcontainer for the entire test run.
/// Starting one container and reusing it across all scenarios is far cheaper than
/// spinning up a new container per scenario. Data isolation between scenarios is
/// achieved by the per-scenario <see cref="StravaAuthHooks"/> cleanup instead.
/// </summary>
/// <remarks>
/// <para>
/// We use the <c>mcr.microsoft.com/azure-sql-edge</c> image rather than
/// <c>mcr.microsoft.com/mssql/server</c> because Azure SQL Edge ships a multi-arch
/// manifest that includes native <c>linux/arm64</c> support. The standard SQL Server
/// 2022 images are <c>linux/amd64</c>-only and crash under QEMU emulation on ARM64
/// hosts. Azure SQL Edge is fully wire-protocol-compatible with SQL Server so EF
/// Core's <c>UseSqlServer</c> provider works against it without changes.
/// </para>
/// <para>
/// The default <see cref="MsSqlBuilder"/> wait strategy executes
/// <c>find /opt/mssql-tools*/bin/sqlcmd</c> inside the container, but Azure SQL Edge
/// does not ship <c>sqlcmd</c> at that path, causing a <see cref="NotSupportedException"/>.
/// We override the wait strategy to wait for TCP port 1433 to be reachable, then retry
/// <see cref="AppDbContext"/> operations until the SQL engine is fully ready.
/// </para>
/// </remarks>
[Binding]
public static class ContainerHooks
{
    // mcr.microsoft.com/azure-sql-edge is multi-arch: arm64/v8 + amd64.
    // This resolves the QEMU emulation failures seen when running the amd64-only
    // SQL Server 2022 image on an arm64 host.
    private const string SqlImage = "mcr.microsoft.com/azure-sql-edge:latest";

    private static MsSqlContainer? _sqlContainer;

    /// <summary>
    /// Connection string pointing at the running Testcontainer.
    /// Available after <see cref="StartSqlServerAsync"/> completes.
    /// </summary>
    public static string ConnectionString =>
        _sqlContainer?.GetConnectionString()
        ?? throw new InvalidOperationException(
            "The SQL Server Testcontainer has not been started. " +
            "Ensure ContainerHooks.StartSqlServerAsync ran before accessing ConnectionString.");

    [BeforeTestRun]
    public static async Task StartSqlServerAsync()
    {
        // Override the default MsSqlBuilder wait strategy (which looks for sqlcmd) with a
        // simple TCP port check. Azure SQL Edge does not have sqlcmd at /opt/mssql-tools*/bin/,
        // so the built-in strategy would throw. Port 1433 becoming available is a reliable
        // signal that the container networking is up; the retry loop below handles the brief
        // window between the port opening and the engine accepting queries.
        _sqlContainer = new MsSqlBuilder()
            .WithImage(SqlImage)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .WithCleanUp(true)
            .Build();

        await _sqlContainer.StartAsync();

        // Port 1433 opens before the SQL engine finishes initialising.
        // Retry EnsureCreatedAsync until it succeeds (typically within ~10 seconds).
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await using var db = new AppDbContext(options);
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch (Exception) when (attempt < 29)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    [AfterTestRun]
    public static async Task StopSqlServerAsync()
    {
        if (_sqlContainer is not null)
            await _sqlContainer.DisposeAsync();
    }
}
