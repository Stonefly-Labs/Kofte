using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Kofte.Host.IntegrationTests;

internal sealed record OwnedDockerResource(string Kind, string Id);

internal sealed class PostgresService(string runId, string password) : IAsyncDisposable
{
    private readonly string project = "kofte-test-" + runId;
    private readonly string compose = Path.Combine(FindRepository(), "deploy", "docker", "compose.test.yml");
    private bool creationStarted;
    private bool recorded;
    public IReadOnlyList<OwnedDockerResource> Resources { get; private set; } = [];
    public int Port { get; private set; }

    internal static string FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Kofte.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("test_repository_missing");
    }

    private ProcessStartInfo DockerStart(string[] args, bool input = false)
    {
        var start = new ProcessStartInfo("docker")
        {
            RedirectStandardInput = input,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        foreach (var key in start.Environment.Keys.Where(key => key.StartsWith("KOFTE_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(key);
        start.Environment["KOFTE_PG_RUN"] = runId;
        start.Environment["KOFTE_PG_PASSWORD"] = password;
        return start;
    }

    private async Task<string> DockerAsync(string[] args, CancellationToken token)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(90));
        var start = DockerStart(args);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("postgres_docker_start_failed");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(budget.Token);
            var text = await output;
            await error;
            if (process.ExitCode != 0) throw new InvalidOperationException("postgres_docker_command_failed");
            return text.Trim();
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await output;
            await error;
        }
    }

    internal async Task RunToolAsync(string tool, string database, string role, Stream archive,
        CancellationToken token)
    {
        if (tool is not ("pg_dump" or "pg_restore"))
            throw new InvalidOperationException("postgres_tool_invalid");
        await ValidateOwnershipAsync(token);
        var container = Resources.Single(resource => resource.Kind == "container").Id;
        var restore = tool == "pg_restore";
        var arguments = new List<string> { "exec" };
        if (restore) arguments.Add("--interactive");
        arguments.AddRange([container, tool, "--host=/var/run/postgresql", "--username=kofte_admin",
            "--dbname=" + database, "--no-owner", "--no-privileges"]);
        arguments.AddRange(restore
            ? ["--exit-on-error", "--clean", "--if-exists", "--role=" + role]
            : ["--format=custom", "--schema=kofte_system", "--no-comments"]);
        using var process = Process.Start(DockerStart([.. arguments], restore))
            ?? throw new InvalidOperationException("postgres_tool_start_failed");
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(60));
        var errors = process.StandardError.ReadToEndAsync();
        var output = restore ? process.StandardOutput.ReadToEndAsync() : null;
        async Task CopyAsync()
        {
            if (restore)
            {
                try { await archive.CopyToAsync(process.StandardInput.BaseStream, budget.Token); }
                finally { process.StandardInput.Close(); }
            }
            else await process.StandardOutput.BaseStream.CopyToAsync(archive, budget.Token);
        }
        var copy = CopyAsync();
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(budget.Token), copy);
            await errors;
            if (output is not null) await output;
            if (process.ExitCode != 0)
                throw new InvalidOperationException("postgres_tool_failed");
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            try { await copy; }
            finally
            {
                await errors;
                if (output is not null) await output;
            }
        }
    }

    private Task<string> ComposeAsync(string[] args, CancellationToken token) =>
        DockerAsync(["compose", "--project-name", project, "--file", compose, .. args], token);

    private async Task<List<OwnedDockerResource>> DiscoverAsync(CancellationToken token)
    {
        var resources = new List<OwnedDockerResource>();
        foreach (var kind in new[] { "container", "volume", "network" })
        {
            var args = kind switch
            {
                "container" => new[] { "container", "ls", "--all", "--quiet", "--no-trunc" },
                "network" => ["network", "ls", "--quiet", "--no-trunc"],
                _ => ["volume", "ls", "--quiet"]
            };
            var output = await DockerAsync([.. args, "--filter", "label=com.docker.compose.project=" + project], token);
            resources.AddRange(output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => new OwnedDockerResource(kind, id)));
        }
        return resources;
    }

    private async Task VerifyLabelsAsync(IReadOnlyList<OwnedDockerResource> resources, CancellationToken token)
    {
        foreach (var resource in resources)
        {
            var field = resource.Kind == "container" ? ".Config.Labels" : ".Labels";
            var label = await DockerAsync([resource.Kind, "inspect", "--format",
                "{{index " + field + " \"io.kofte.bootstrap.run\"}}", resource.Id], token);
            if (label != runId) throw new InvalidOperationException("postgres_container_ownership_refused");
        }
    }

    public async Task StartAsync(CancellationToken token)
    {
        var endpoint = await DockerAsync(["context", "inspect", "--format", "{{.Endpoints.docker.Host}}"], token);
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_CONTEXT")))
            endpoint = Environment.GetEnvironmentVariable("DOCKER_HOST") ?? endpoint;
        if (!endpoint.StartsWith("unix://", StringComparison.Ordinal))
            throw new InvalidOperationException("postgres_local_engine_required");
        if ((await DiscoverAsync(token)).Count != 0)
            throw new InvalidOperationException("postgres_project_preexists");
        creationStarted = true;
        await ComposeAsync(["up", "--detach", "--wait", "--wait-timeout", "60", "--pull", "never"], token);
        Resources = await DiscoverAsync(token);
        await VerifyLabelsAsync(Resources, token);
        if (Resources.Count(resource => resource.Kind == "container") != 1 ||
            Resources.Count(resource => resource.Kind == "volume") != 1 ||
            Resources.Count(resource => resource.Kind == "network") != 1)
            throw new InvalidOperationException("postgres_resource_inventory_invalid");
        recorded = true;
        var published = await ComposeAsync(["port", "postgres", "5432"], token);
        if (!Regex.IsMatch(published, @"\A127\.0\.0\.1:[0-9]+\z") ||
            !int.TryParse(published.Split(':')[1], out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("postgres_loopback_port_invalid");
        Port = port;
        var container = Resources.Single(resource => resource.Kind == "container").Id;
        foreach (var tool in new[] { "pg_dump", "pg_restore" })
        {
            var version = await DockerAsync(["exec", container, tool, "--version"], token);
            if (!Regex.IsMatch(version, @"\Apg_(dump|restore) \(PostgreSQL\) 17\."))
                throw new InvalidOperationException("postgres_client_version_invalid");
        }
    }

    public async Task ValidateOwnershipAsync(CancellationToken token)
    {
        if (!creationStarted) throw new InvalidOperationException("postgres_container_ownership_refused");
        var actual = await DiscoverAsync(token);
        await VerifyLabelsAsync(actual, token);
        if (recorded && !actual.OrderBy(item => item.Kind).ThenBy(item => item.Id)
            .SequenceEqual(Resources.OrderBy(item => item.Kind).ThenBy(item => item.Id)))
            throw new InvalidOperationException("postgres_container_ownership_refused");
    }

    internal async Task SetPausedAsync(bool paused, CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        var container = Resources.Single(resource => resource.Kind == "container").Id;
        var state = await DockerAsync(["inspect", "--format", "{{.State.Paused}}", container], token);
        if (state is not ("true" or "false"))
            throw new InvalidOperationException("postgres_container_state_invalid");
        if ((state == "true") != paused)
            await DockerAsync([paused ? "pause" : "unpause", container], token);
    }

    public ValueTask DisposeAsync() => new(CleanupAsync(CancellationToken.None));

    internal async Task CleanupAsync(CancellationToken token)
    {
        if (!creationStarted) return;
        using var cleanup = CancellationTokenSource.CreateLinkedTokenSource(token);
        cleanup.CancelAfter(TimeSpan.FromSeconds(30));
        await ValidateOwnershipAsync(cleanup.Token);
        await ComposeAsync(["down", "--volumes"], cleanup.Token);
        if ((await DiscoverAsync(cleanup.Token)).Count != 0)
            throw new InvalidOperationException("postgres_container_cleanup_incomplete");
        creationStarted = false;
    }
}
