using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Security.Cryptography;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Lifecycle;

namespace Kofte.Host.Features.ModuleComposition;

public static class CompositionSupervisor
{
    public static async Task<int> RunAsync(StorageMaintenanceConfiguration configuration,
        IReadOnlyList<ModuleDeployment> catalog, ModuleSelectionDocument selection,
        CompositionDiagnostics diagnostics, CancellationToken token)
    {
        var attempt = new CompositionAttempt(diagnostics, token);
        var started = Stopwatch.GetTimestamp();
        var snapshot = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        var admission = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var owners = CatalogOwners(catalog);
        var prerequisites = CatalogPrerequisites(catalog);
        var relatedOwners = owners.Concat(prerequisites).Distinct().ToArray();
        var selectedOwners = selection.Modules.Select(module => module.Owner).ToArray();
        var recordOwners = owners.Concat(selectedOwners).Distinct().ToArray();
        var protocol = new CompositionProtocol(diagnostics.Attempt, snapshot, owners, diagnostics.Mode, CatalogFields(catalog),
            selectedOwners, prerequisites, selection.Modules.Where(module => module.Enabled).Select(module => module.Owner));
        using var input = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        using var events = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        var start = Start(configuration, diagnostics.Mode);
        start.Environment[CompositionAdmission.InputKey] = input.GetClientHandleAsString();
        start.Environment[CompositionAdmission.EventKey] = events.GetClientHandleAsString();
        start.Environment[CompositionAdmission.TokenKey] = admission;
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new CompositionBoundaryException("composition_admission_invalid");
        input.DisposeLocalCopyOfClientHandle();
        events.DisposeLocalCopyOfClientHandle();
        using var io = new CancellationTokenSource();
        var stdout = CompositionProtocol.DiscardRawAsync(process.StandardOutput, io.Token);
        var stderr = CompositionProtocol.DiscardRawAsync(process.StandardError, io.Token);
        var exit = process.WaitForExitAsync();
        var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        long healthDeadline = long.MaxValue;
        var ownedGroup = false;
        var terminal = false;
        var terminalExit = 1;
        CompositionRecord? terminalRecord = null;
        var observe = ObserveAsync();
        var healthWatchdog = WatchHealthAsync();
        var remainingWork = attempt.Work - Stopwatch.GetElapsedTime(started);
        using var work = CancellationTokenSource.CreateLinkedTokenSource(token);
        work.CancelAfter(remainingWork > TimeSpan.Zero ? remainingWork : TimeSpan.Zero);
        var interrupted = Task.Delay(Timeout.InfiniteTimeSpan, work.Token);
        var published = false;
        Exception? failure = null;
        try
        {
            await CompositionAdmission.WriteAsync(input, admission, diagnostics.Attempt, snapshot, diagnostics.Mode,
                selection, catalog, work.Token);
            input.Close();
            while (true)
            {
                var pending = published ? new[] { exit, observe, interrupted, healthWatchdog } :
                    new[] { exit, observe, interrupted, publication.Task };
                var completed = await Task.WhenAny(pending);
                if (completed == publication.Task)
                {
                    published = true;
                    work.CancelAfter(Timeout.InfiniteTimeSpan);
                    continue;
                }
                if (completed == interrupted)
                    throw new CompositionBoundaryException(token.IsCancellationRequested
                        ? "composition_cancelled" : "composition_deadline_exceeded");
                if (completed == healthWatchdog) await healthWatchdog;
                if (completed == observe)
                {
                    await observe;
                    if (!terminal) throw new CompositionBoundaryException("composition_output_failed");
                    break;
                }
                if (completed == exit) break;
            }
        }
        catch (Exception error) { failure = error; }

        var cleanupCap = published ? TimeSpan.FromSeconds(15) :
            TimeSpan.FromTicks(Math.Max(0, Math.Min(attempt.CleanupReserve.Ticks,
                (attempt.Total - Stopwatch.GetElapsedTime(started)).Ticks)));
        using var cleanup = new CancellationTokenSource(cleanupCap);
        var cooperative = cleanupCap - TimeSpan.FromSeconds(5);
        if (failure is CompositionBoundaryException { Code: "composition_cancelled" or "composition_deadline_exceeded" } &&
            !process.HasExited && cooperative > TimeSpan.Zero)
        {
            try
            {
                await process.StandardInput.BaseStream.WriteAsync(new byte[] { 1 }, cleanup.Token);
                await process.StandardInput.BaseStream.FlushAsync(cleanup.Token);
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(cleanup.Token);
                stop.CancelAfter(cooperative);
                await exit.WaitAsync(stop.Token);
            }
            catch (Exception) { }
        }
        else if (failure is null && terminal && !process.HasExited && cooperative > TimeSpan.Zero)
        {
            try
            {
                using var naturalExit = CancellationTokenSource.CreateLinkedTokenSource(cleanup.Token);
                naturalExit.CancelAfter(cooperative);
                await exit.WaitAsync(naturalExit.Token);
            }
            catch (Exception) { failure = new CompositionBoundaryException("composition_cleanup_incomplete"); }
        }
        try
        {
            if (!process.HasExited)
            {
                if (ownedGroup || CompositionProcessGroup.IsOwned(process.Id))
                {
                    ownedGroup = true;
                    CompositionProcessGroup.Kill(process.Id);
                }
                else process.Kill(entireProcessTree: true);
            }
            else if (ownedGroup && !CompositionProcessGroup.HasExited(process.Id))
                CompositionProcessGroup.Kill(process.Id);
            await exit.WaitAsync(cleanup.Token);
            if (ownedGroup)
                while (!CompositionProcessGroup.HasExited(process.Id))
                    await Task.Delay(20, cleanup.Token);
            await Task.WhenAll(stdout, stderr).WaitAsync(cleanup.Token);
            try { await observe.WaitAsync(cleanup.Token); }
            catch (CompositionBoundaryException error) { failure ??= error; }
            catch (Exception) when (failure is not null) { }
        }
        catch (Exception)
        {
            io.Cancel();
            throw new CompositionBoundaryException("composition_cleanup_incomplete");
        }
        finally { io.Cancel(); }
        if (failure is not null)
        {
            if (failure is CompositionBoundaryException { Code: "composition_cancelled" } &&
                token.IsCancellationRequested && terminal && process.ExitCode == terminalExit &&
                (published || terminalExit != 0))
            {
                Project(terminalRecord!, true);
                return terminalExit;
            }
            throw failure;
        }
        if (!terminal) throw new CompositionBoundaryException("composition_output_failed");
        if (process.ExitCode != terminalExit)
            throw new CompositionBoundaryException("composition_observation_invalid");
        Project(terminalRecord!, true);
        return process.ExitCode;

        async Task ObserveAsync()
        {
            while (await protocol.ReadAsync(events, io.Token) is { } record)
            {
                if (terminal) throw new CompositionBoundaryException("composition_observation_invalid");
                var stage = Enum.GetValues<CompositionStage>().Single(v => CompositionDiagnostics.StageName(v) == record.Stage);
                var outcome = Enum.GetValues<CompositionOutcome>().Single(v => CompositionDiagnostics.OutcomeName(v) == record.Outcome);
                if (stage == CompositionStage.Admission)
                {
                    if (!CompositionProcessGroup.IsOwned(process.Id))
                        throw new CompositionBoundaryException("composition_admission_invalid");
                    ownedGroup = true;
                }
                var isTerminal = stage == CompositionStage.Cleanup && record.Owner is null;
                if (!isTerminal) Project(record, false);
                if (stage == CompositionStage.Published)
                {
                    if (diagnostics.Mode != CompositionMode.Serve || publication.Task.IsCompleted)
                        throw new CompositionBoundaryException("composition_observation_invalid");
                    diagnostics.BeginSteadyState();
                    HealthDeadline(TimeSpan.FromSeconds(30));
                    publication.TrySetResult();
                }
                if (stage == CompositionStage.Health && record.Owner is null && publication.Task.IsCompleted)
                    HealthDeadline(outcome == CompositionOutcome.Progress ? TimeSpan.FromSeconds(20) : TimeSpan.FromSeconds(30));
                if (isTerminal)
                {
                    terminal = true;
                    terminalRecord = record;
                    terminalExit = outcome switch
                    {
                        CompositionOutcome.Passed => 0,
                        CompositionOutcome.Rejected => 2,
                        CompositionOutcome.Busy => 3,
                        CompositionOutcome.ReconciliationRequired => 4,
                        _ => 1
                    };
                }
            }

        }

        void HealthDeadline(TimeSpan cap) =>
            Interlocked.Exchange(ref healthDeadline, Stopwatch.GetTimestamp() + (long)(cap.TotalSeconds * Stopwatch.Frequency));

        async Task WatchHealthAsync()
        {
            await publication.Task.WaitAsync(io.Token);
            while (true)
            {
                await Task.Delay(50, io.Token);
                if (!terminal && Stopwatch.GetTimestamp() >= Interlocked.Read(ref healthDeadline))
                    throw new CompositionBoundaryException("composition_deadline_exceeded");
            }
        }

        void Project(CompositionRecord record, bool isTerminal) =>
            diagnostics.Emit(Enum.GetValues<CompositionStage>().Single(v => CompositionDiagnostics.StageName(v) == record.Stage),
                Enum.GetValues<CompositionOutcome>().Single(v => CompositionDiagnostics.OutcomeName(v) == record.Outcome), record.Code,
                record.Owner is null ? null : recordOwners.Single(o => o.Value == record.Owner),
                record.RelatedIds.Select(id => relatedOwners.Single(o => o.Value == id)).ToArray(),
                record.StorageCorrelation, record.Durability, isTerminal, record.FieldToken, record.FieldReason);
    }

    internal static IReadOnlyList<ModuleId> CatalogOwners(IReadOnlyList<ModuleDeployment> catalog) =>
        catalog.Take(32).Select(deployment => ManifestValidator.Validate(deployment.Manifest.Span, new("1.0.0")))
            .OfType<ManifestValidationResult.Accepted>().Select(result => result.Description.Id)
            .Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray();

    internal static IReadOnlyList<ModuleId> CatalogPrerequisites(IReadOnlyList<ModuleDeployment> catalog) =>
        catalog.Take(32).Select(deployment => ManifestValidator.Validate(deployment.Manifest.Span, new("1.0.0")))
            .OfType<ManifestValidationResult.Accepted>().SelectMany(result => result.Description.Dependencies)
            .Select(dependency => dependency.Id).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray();

    internal static IReadOnlyDictionary<ModuleId, IReadOnlyList<ModuleConfigurationField>> CatalogFields(
        IReadOnlyList<ModuleDeployment> catalog) =>
        catalog.Take(32).Select(deployment => (Deployment: deployment,
                Accepted: ManifestValidator.Validate(deployment.Manifest.Span, new("1.0.0")) as ManifestValidationResult.Accepted))
            .Where(entry => entry.Accepted is not null).GroupBy(entry => entry.Accepted!.Description.Id)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key,
                group => group.Single().Deployment.ConfigurationFields);

    private static ProcessStartInfo Start(StorageMaintenanceConfiguration configuration, CompositionMode mode)
    {
        var executable = Environment.ProcessPath ?? throw new CompositionBoundaryException("composition_admission_invalid");
        var entry = Assembly.GetEntryAssembly()?.Location ??
            throw new CompositionBoundaryException("composition_admission_invalid");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(entry);
        start.ArgumentList.Add("--modules-worker");
        start.ArgumentList.Add(CompositionDiagnostics.ModeName(mode));
        configuration.ConfigureChild(start);
        start.Environment.Remove("KOFTE_MODULES_FILE");
        if (mode != CompositionMode.Prepare) start.Environment.Remove("KOFTE_POSTGRES_PROVISIONING_CONNECTION");
        start.Environment["KOFTE_HOST_URL"] = configuration.Runtime.HostUrl.AbsoluteUri;
        start.Environment["KOFTE_WEB_URL"] = configuration.Runtime.WebUrl.AbsoluteUri;
        start.Environment["KOFTE_BACKEND_ORIGIN"] = configuration.Runtime.BackendOrigin.AbsoluteUri;
        if (configuration.Runtime.UnsafeExposure) start.Environment["KOFTE_ALLOW_UNSAFE_EXPOSURE"] = "true";
        return start;
    }
}
