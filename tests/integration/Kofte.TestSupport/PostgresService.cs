using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Kofte.TestSupport;

internal sealed record OwnedDockerResource(string Kind, string Id);

internal sealed class PostgresService(string runId, string password) : IAsyncDisposable
{
    private readonly string project = "kofte-test-" + runId;
    private readonly string compose = Path.Combine(OwnedProcesses.FindRepository(), "deploy", "docker", "compose.test.yml");
    private bool creationStarted;
    private bool recorded;
    public IReadOnlyList<OwnedDockerResource> Resources { get; private set; } = [];
    public int Port { get; private set; }

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
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment["KOFTE_PG_RUN"] = runId;
        start.Environment["KOFTE_PG_PASSWORD"] = password;
        return start;
    }

    private async Task<string> DockerAsync(string[] args, CancellationToken token)
    {
        var result = await OwnedProcesses.RunAsync(DockerStart(args), TimeSpan.FromSeconds(90), token);
        if (result.ExitCode != 0) throw new InvalidOperationException("postgres_docker_command_failed");
        return result.Stdout.Trim();
    }

    internal async Task RunToolAsync(string tool, string database, string role, Stream archive,
        CancellationToken token, bool fullDatabase = false)
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
            : fullDatabase ? ["--format=custom", "--no-comments"]
            : ["--format=custom", "--schema=kofte_system", "--no-comments"]);
        await using var process = OwnedProcesses.Start(DockerStart([.. arguments], restore), captureOutput: restore);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(60));
        async Task CopyAsync()
        {
            if (restore)
            {
                try { await archive.CopyToAsync(process.StandardInput.BaseStream, budget.Token); }
                finally { process.StandardInput.Close(); }
            }
            else await process.StandardOutput.CopyToAsync(archive, budget.Token);
        }
        var copy = CopyAsync();
        try
        {
            var wait = process.WaitAsync(budget.Token);
            await Task.WhenAll(wait, copy);
            if ((await wait).ExitCode != 0)
                throw new InvalidOperationException("postgres_tool_failed");
        }
        finally
        {
            await process.StopAsync();
            await copy;
        }
    }

    internal async Task ValidateLegacyArchiveAsync(Stream archive, CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        var container = Resources.Single(resource => resource.Kind == "container").Id;
        await using var process = OwnedProcesses.Start(DockerStart(
            ["exec", "--interactive", container, "pg_restore", "--list"], input: true));
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            await archive.CopyToAsync(process.StandardInput.BaseStream, budget.Token);
            process.StandardInput.Close();
            var result = await process.WaitAsync(budget.Token);
            if (result.ExitCode != 0 || result.Stdout.Contains("__kofte_", StringComparison.OrdinalIgnoreCase) ||
                result.Stdout.Contains("__EFMigrations", StringComparison.OrdinalIgnoreCase) ||
                result.Stdout.Split('\n').Where(line => line.Length > 0 && line[0] != ';')
                    .Any(line => line.Contains(" SCHEMA ", StringComparison.Ordinal) &&
                        !Regex.IsMatch(line, @" SCHEMA - kofte_system \S+\z", RegexOptions.CultureInvariant)))
                throw new InvalidOperationException("postgres_registry_requires_complete_recovery_set");
        }
        finally { await process.StopAsync(); }
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
