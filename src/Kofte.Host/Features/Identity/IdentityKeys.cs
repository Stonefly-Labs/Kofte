using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Paths = Kofte.Infrastructure.Storage.UnixStorePaths;

[assembly: InternalsVisibleTo("Kofte.Host.Tests")]

namespace Kofte.Host.Features.Identity;

internal sealed class IdentityKeys : IDisposable
{
    private const string Domain = "kofte.identity.keys.v1";
    private readonly RingRepository repository;
    private readonly ServiceProvider services;
    private string verifiedSnapshot;
    private bool disposed;

    private IdentityKeys(RingRepository repository, string verifiedSnapshot, bool preparationChanged = false)
    {
        this.repository = repository;
        this.verifiedSnapshot = verifiedSnapshot;
        PreparationChanged = preparationChanged;
        services = Build(repository, automatic: true);
        Provider = new BoundProvider(this, services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(Domain));
    }

    internal IDataProtectionProvider Provider { get; }
    internal bool PreparationChanged { get; }
    internal IDataProtector Protector(string purpose) => Provider.CreateProtector(purpose);
    internal void Verify() => _ = Use(static () => true);

    internal static async Task<IdentityKeys> PrepareAsync(IdentityConfiguration configuration, Guid installationId,
        StorageInstallation installation, MaintenanceScope scope, OperationBudget budget, bool existingOnly = false,
        string? directoryOverride = null, Guid? restoreRingId = null)
    {
        await scope.VerifyAsync(installation, budget);
        try
        {
            void Authority() => scope.VerifyAsync(installation, budget).GetAwaiter().GetResult();
            var repository = RingRepository.Prepare(configuration, installationId, Authority, budget, existingOnly,
                directoryOverride, restoreRingId);
            var snapshot = repository.Read(budget);
            var changed = snapshot.Owner.State == "preparing";
            if (changed)
            {
                if (snapshot.Keys.Count == 0)
                {
                    using var preparation = Build(repository, automatic: false);
                    Authority();
                    var now = DateTimeOffset.UtcNow;
                    preparation.GetRequiredService<IKeyManager>().CreateNewKey(now, now.AddDays(90));
                    Authority();
                }
                Validate(repository, budget);
                repository.MarkReady(budget);
            }
            var verified = Validate(repository, budget);
            await scope.VerifyAsync(installation, budget);
            repository.FinishPreparation();
            return new(repository, verified, changed);
        }
        catch (Exception error) when (Unsafe(error)) { throw Invalid(); }
    }

    internal static IdentityKeys Open(IdentityConfiguration configuration, Guid installationId, OperationBudget budget)
        => OpenCore(configuration, installationId, budget, restoreGate: false);

    internal static IdentityKeys OpenRestored(IdentityConfiguration configuration, Guid installationId,
        OperationBudget budget) => OpenCore(configuration, installationId, budget, restoreGate: true);

    internal SortedDictionary<string, string> RecoveryInventory(OperationBudget budget)
    {
        var snapshot = repository.Read(budget);
        var files = new SortedDictionary<string, string>(snapshot.Owner.Keys, StringComparer.Ordinal)
        {
            ["identity-owner.json"] = snapshot.Fingerprint
        };
        return files;
    }

    private static IdentityKeys OpenCore(IdentityConfiguration configuration, Guid installationId,
        OperationBudget budget, bool restoreGate)
    {
        try
        {
            var repository = new RingRepository(configuration, installationId, restoreGate: restoreGate);
            var snapshot = repository.Read(budget);
            if (snapshot.Owner.State != "ready") throw Invalid();
            return new(repository, Validate(repository, budget));
        }
        catch (Exception error) when (Unsafe(error)) { throw Invalid(); }
    }

    internal void Verify(OperationBudget budget)
    {
        using (Enter(repository.Gate, budget))
        {
            try
            {
                if (disposed) throw Invalid();
                var snapshot = repository.Read(budget);
                if (snapshot.Owner.State != "ready") throw Invalid();
                if (snapshot.Fingerprint != verifiedSnapshot)
                    verifiedSnapshot = Validate(repository, budget);
                budget.Check();
            }
            catch (Exception error) when (Unsafe(error)) { throw Invalid(); }
        }
    }

    private static string Validate(RingRepository repository, OperationBudget budget)
    {
        var snapshot = repository.Read(budget);
        if (snapshot.Keys.Count == 0) throw Invalid();
        // Never ask the runtime key-ring resolver to repair an unreadable ring.
        using var validation = Build(repository, automatic: false, readOnly: true);
        var keys = validation.GetRequiredService<IKeyManager>().GetAllKeys();
        if (keys.Count != snapshot.Keys.Count) throw Invalid();
        foreach (var key in keys)
        {
            budget.Check();
            if (key.KeyId == Guid.Empty || key.IsRevoked || key.CreationDate > key.ActivationDate ||
                key.ActivationDate >= key.ExpirationDate) throw Invalid();
            var encryptor = key.CreateEncryptor() ?? throw Invalid();
            byte[] probe = [0x4b, 0x6f, 0x66, 0x74, 0x65];
            byte[] context = [0x76, 0x31];
            if (!encryptor.Decrypt(encryptor.Encrypt(probe, context), context).AsSpan().SequenceEqual(probe))
                throw Invalid();
        }
        budget.Check();
        return snapshot.Fingerprint;
    }

    private static ServiceProvider Build(RingRepository repository, bool automatic, bool readOnly = false)
    {
        var collection = new ServiceCollection();
        // Framework diagnostics include private paths and XML/crypto exception details.
        // This isolated provider must never inherit the host's logging sinks.
        collection.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        var builder = collection.AddDataProtection()
            .SetApplicationName(Domain + "/" + repository.InstallationId.ToString("D") + "/" + repository.Mode)
            .AddKeyManagementOptions(options =>
            {
                options.AutoGenerateKeys = automatic;
                options.XmlRepository = readOnly ? new ReadOnlyRepository(repository) : repository;
                options.NewKeyLifetime = TimeSpan.FromDays(90);
            });
        if (repository.Configuration.KeyEncryptionCertificate is { } certificate)
            builder.ProtectKeysWithCertificate(certificate)
                .UnprotectKeysWithAnyCertificate([.. repository.Configuration.KeyDecryptionCertificates]);
        return collection.BuildServiceProvider();
    }

    private T Use<T>(Func<T> operation)
    {
        var enclosing = IdentityDiagnostics.Current?.Budget;
        enclosing?.Check();
        var duration = enclosing?.Remaining ?? OperationBudget.Resolve;
        if (duration <= TimeSpan.Zero) throw new OperationCanceledException(enclosing?.Token ?? default);
        using var budget = new OperationBudget(duration,
            enclosing?.Token ?? CancellationToken.None);
        using (Enter(repository.Gate, budget))
        {
            Verify(budget);
            try
            {
                var result = operation();
                budget.Check();
                return result;
            }
            catch (CryptographicException) { throw new CryptographicException("identity_keys_invalid"); }
            catch (Exception error) when (Unsafe(error)) { throw Invalid(); }
        }
    }

    private static IDisposable Enter(object gate, OperationBudget budget)
    {
        while (true)
        {
            budget.Check();
            if (Monitor.TryEnter(gate))
            {
                try
                {
                    budget.Check();
                    return new GateLease(gate);
                }
                catch
                {
                    Monitor.Exit(gate);
                    throw;
                }
            }
            _ = budget.Token.WaitHandle.WaitOne(OperationBudget.ProbeInterval);
        }
    }

    private sealed class GateLease(object gate) : IDisposable
    {
        public void Dispose() => Monitor.Exit(gate);
    }

    private static void Purpose(string purpose)
    {
        if (string.IsNullOrEmpty(purpose) || purpose.Length > 1024 || purpose.Any(char.IsControl))
            throw Invalid();
    }

    public void Dispose()
    {
        lock (repository.Gate)
        {
            if (disposed) return;
            disposed = true;
            services.Dispose();
        }
    }

    private static InvalidOperationException Invalid() => new("identity_keys_invalid");
    private static bool Unsafe(Exception error) => error is IOException or UnauthorizedAccessException or
        StorePathException or JsonException or XmlException or CryptographicException or InvalidOperationException or
        ArgumentException or NotSupportedException or FormatException or OverflowException;

    private sealed class BoundProvider(IdentityKeys owner, IDataProtectionProvider inner) : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
        {
            Purpose(purpose);
            return new BoundProtector(owner, inner.CreateProtector(purpose));
        }
    }

    private sealed class BoundProtector(IdentityKeys owner, IDataProtector inner) : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            Purpose(purpose);
            return new BoundProtector(owner, inner.CreateProtector(purpose));
        }
        public byte[] Protect(byte[] plaintext) => owner.Use(() => inner.Protect(plaintext));
        public byte[] Unprotect(byte[] protectedData) => owner.Use(() => inner.Unprotect(protectedData));
    }

    private sealed class ReadOnlyRepository(RingRepository inner) : IXmlRepository
    {
        public IReadOnlyCollection<XElement> GetAllElements() => inner.GetAllElements();
        public void StoreElement(XElement element, string friendlyName) => throw Invalid();
    }

    private sealed record Owner(Guid InstallationId, Guid RingId, string Mode, string Protection, string State,
        SortedDictionary<string, string> Keys);
    private sealed record Snapshot(Owner Owner, IReadOnlyCollection<XElement> Keys, string Fingerprint);

    private sealed class RingRepository : IXmlRepository
    {
        private const string OwnerName = "identity-owner.json", NextName = "identity-owner.next";
        private const int MaximumKeys = 256, MaximumFileBytes = 65_536;
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private readonly string directory;
        private readonly bool restoreGate;
        private Guid ringId;
        private Action? authority;
        private OperationBudget? preparationBudget;
        internal object Gate { get; } = new();
        internal IdentityConfiguration Configuration { get; }
        internal Guid InstallationId { get; }
        internal string Mode { get; }
        private string Protection => Configuration.KeyEncryptionCertificate is null ? "private" : "certificate";
        private string OwnerPath => Path.Combine(directory, OwnerName);

        internal RingRepository(IdentityConfiguration configuration, Guid installationId,
            string? directoryOverride = null, bool restoreGate = false)
        {
            if (installationId == Guid.Empty || configuration.Mode == Kofte.Platform.Identity.IdentityMode.Oidc &&
                configuration.KeyEncryptionCertificate is null) throw Invalid();
            Configuration = configuration;
            InstallationId = installationId;
            Mode = configuration.Mode == Kofte.Platform.Identity.IdentityMode.Oidc ? "oidc" : "development";
            directory = directoryOverride ?? configuration.KeyRingDirectory;
            this.restoreGate = restoreGate;
        }

        internal static RingRepository Prepare(IdentityConfiguration configuration, Guid installationId,
            Action authority, OperationBudget budget, bool existingOnly, string? directoryOverride, Guid? restoreRingId)
        {
            if (restoreRingId == Guid.Empty || restoreRingId is not null && directoryOverride is null) throw Invalid();
            var repository = new RingRepository(configuration, installationId, directoryOverride)
            { authority = authority, preparationBudget = budget };
            Paths.ValidateRoot(repository.directory, allowMissing: !existingOnly);
            if (!Directory.Exists(repository.directory) || restoreRingId is not null &&
                !Directory.EnumerateFileSystemEntries(repository.directory).Any())
            {
                if (existingOnly) throw Invalid();
                Paths.ValidateRoot(Path.GetDirectoryName(repository.directory)!, allowMissing: false);
                repository.Effect(() =>
                {
                    if (OperatingSystem.IsWindows()) throw Invalid();
                    Directory.CreateDirectory(repository.directory, Paths.DirectoryMode);
                });
                repository.ringId = restoreRingId ?? Guid.NewGuid();
                repository.WriteOwner(new(installationId, repository.ringId, repository.Mode, repository.Protection,
                    "preparing", new(StringComparer.Ordinal)), initial: true);
            }
            var (owner, ownerBytes) = repository.ReadOwner();
            if (restoreRingId is { } expected && owner.RingId != expected) throw Invalid();
            repository.ringId = owner.RingId;
            if (owner.State == "preparing") repository.Resume(owner, ownerBytes, budget);
            else _ = repository.Read(budget);
            authority();
            return repository;
        }

        internal void FinishPreparation()
        {
            authority = null;
            preparationBudget = null;
        }

        internal void MarkReady(OperationBudget budget)
        {
            using (Enter(Gate, budget))
            {
                var owner = Read(budget).Owner;
                if (owner.State != "preparing" || owner.Keys.Count == 0) throw Invalid();
                WriteOwner(owner with { State = "ready" });
                IdentityDiagnostics.Checkpoint("key_ready");
            }
        }

        private void Resume(Owner owner, byte[] ownerBytes, OperationBudget budget)
        {
            var files = Entries();
            foreach (var entry in owner.Keys)
            {
                budget.Check();
                if (!files.Contains(entry.Key, StringComparer.Ordinal) ||
                    Hash(ReadFile(Path.Combine(directory, entry.Key))) != entry.Value) throw Invalid();
            }
            var changed = false;
            foreach (var name in files.Where(name => name != OwnerName && name != NextName))
            {
                budget.Check();
                var bytes = ReadFile(Path.Combine(directory, name));
                _ = ReadKey(bytes, name, owner);
                if (!owner.Keys.ContainsKey(name))
                {
                    owner.Keys.Add(name, Hash(bytes));
                    changed = true;
                }
            }
            if (files.Contains(NextName, StringComparer.Ordinal))
            {
                var path = Path.Combine(directory, NextName);
                var bytes = ReadFile(path, allowEmpty: true);
                bool incomplete;
                try { incomplete = IdentityRestoreQuarantine.IsIncompleteJournal(bytes); }
                catch (StorageException error) when (error.Code == Kofte.Platform.Modules.Storage.StorageCode.Corrupt)
                { throw Invalid(); }
                if (!incomplete)
                {
                    var next = ParseOwner(bytes);
                    if (next.RingId != owner.RingId || next.Keys.Any(entry =>
                        !owner.Keys.TryGetValue(entry.Key, out var hash) || hash != entry.Value)) throw Invalid();
                }
                else IdentityDiagnostics.Checkpoint("key_pending_incomplete");
                Effect(() =>
                {
                    if (!ReadFile(OwnerPath).AsSpan().SequenceEqual(ownerBytes) ||
                        !ReadFile(path, allowEmpty: true).AsSpan().SequenceEqual(bytes)) throw Invalid();
                    File.Delete(path);
                });
                changed = true;
            }
            if (changed) WriteOwner(owner);
            _ = Read(budget);
        }

        internal Snapshot Read(OperationBudget? budget)
        {
            using var owned = budget is null && preparationBudget is null && IdentityDiagnostics.Current is null
                ? new OperationBudget(OperationBudget.Resolve, CancellationToken.None) : null;
            budget ??= preparationBudget ?? IdentityDiagnostics.Current?.Budget ?? owned!;
            using (Enter(Gate, budget))
            {
                budget?.Check();
                Paths.ValidateRoot(directory, allowMissing: false);
                var ownerBytes = ReadFile(OwnerPath);
                var owner = ParseOwner(ownerBytes);
                if (ringId == Guid.Empty) ringId = owner.RingId;
                if (ringId != owner.RingId || owner.State == "preparing" && authority is null) throw Invalid();
                var files = Entries();
                if (files.Length != owner.Keys.Count + 1 ||
                    files.Any(name => name != OwnerName && !owner.Keys.ContainsKey(name))) throw Invalid();
                var keys = new List<XElement>(owner.Keys.Count);
                foreach (var entry in owner.Keys)
                {
                    budget?.Check();
                    var bytes = ReadFile(Path.Combine(directory, entry.Key));
                    if (Hash(bytes) != entry.Value) throw Invalid();
                    keys.Add(ReadKey(bytes, entry.Key, owner));
                }
                Paths.ValidateRoot(directory, allowMissing: false);
                budget?.Check();
                return new(owner, keys.AsReadOnly(), Hash(ownerBytes));
            }
        }

        public IReadOnlyCollection<XElement> GetAllElements() => Read(preparationBudget).Keys;

        public void StoreElement(XElement element, string friendlyName)
        {
            var enclosing = preparationBudget ?? IdentityDiagnostics.Current?.Budget;
            using var owned = enclosing is null ? new OperationBudget(OperationBudget.Resolve, CancellationToken.None) : null;
            using (Enter(Gate, enclosing ?? owned!))
            {
                try
                {
                    var snapshot = Read(preparationBudget);
                    var owner = snapshot.Owner;
                    if (owner.Keys.Count >= MaximumKeys || owner.State != "ready" && authority is null ||
                        element.Name != "key" || !Guid.TryParseExact((string?)element.Attribute("id"), "D", out var id) ||
                        id == Guid.Empty) throw Invalid();
                    var name = "key-" + id.ToString("D") + ".xml";
                    var wrapper = new XElement("identityKey",
                        new XAttribute("version", "1"), new XAttribute("installationId", InstallationId.ToString("D")),
                        new XAttribute("ringId", owner.RingId.ToString("D")), new XAttribute("purpose", Domain),
                        new XAttribute("mode", Mode), new XAttribute("protection", Protection), new XElement(element));
                    var bytes = Utf8.GetBytes(wrapper.ToString(SaveOptions.DisableFormatting));
                    _ = ReadKey(bytes, name, owner);
                    WriteNew(Path.Combine(directory, name), bytes);
                    IdentityDiagnostics.Checkpoint("key_material_written");
                    owner.Keys.Add(name, Hash(bytes));
                    WriteOwner(owner);
                    _ = Read(preparationBudget);
                }
                catch (Exception error) when (Unsafe(error)) { throw Invalid(); }
            }
        }

        private (Owner Owner, byte[] Bytes) ReadOwner()
        {
            Paths.ValidateRoot(directory, allowMissing: false);
            var bytes = ReadFile(OwnerPath);
            return (ParseOwner(bytes), bytes);
        }

        private Owner ParseOwner(byte[] bytes)
        {
            using var json = JsonDocument.Parse(bytes,
                new() { AllowDuplicateProperties = false, MaxDepth = 4 });
            var value = json.RootElement;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 8 ||
                value.EnumerateObject().Any(item => item.Name is not ("version" or "installationId" or "ringId" or
                    "purpose" or "mode" or "protection" or "state" or "keys")) ||
                Text(value, "version") != "1" || Text(value, "purpose") != Domain ||
                Text(value, "installationId") != InstallationId.ToString("D") || Text(value, "mode") != Mode ||
                Text(value, "protection") != Protection) throw Invalid();
            var ring = Text(value, "ringId");
            if (!Guid.TryParseExact(ring, "D", out var id) || id == Guid.Empty || id.ToString("D") != ring)
                throw Invalid();
            var state = Text(value, "state");
            if (state is not ("preparing" or "ready")) throw Invalid();
            var keys = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var entries = value.GetProperty("keys");
            if (entries.ValueKind != JsonValueKind.Object) throw Invalid();
            foreach (var entry in entries.EnumerateObject())
            {
                var hash = entry.Value.ValueKind == JsonValueKind.String ? entry.Value.GetString()! : "";
                if (!KeyName(entry.Name) || hash.Length != 64 || hash.Any(character =>
                    character is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
                    !keys.TryAdd(entry.Name, hash) || keys.Count > MaximumKeys) throw Invalid();
            }
            if (state == "ready" && keys.Count == 0) throw Invalid();
            return new(InstallationId, id, Mode, Protection, state, keys);
        }

        private XElement ReadKey(byte[] bytes, string name, Owner owner)
        {
            if (!KeyName(name)) throw Invalid();
            using var input = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(input, new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumFileBytes
            });
            var xml = XElement.Load(reader);
            if (xml.Name != "identityKey" || xml.Attributes().Count() != 6 ||
                (string?)xml.Attribute("version") != "1" ||
                (string?)xml.Attribute("installationId") != InstallationId.ToString("D") ||
                (string?)xml.Attribute("ringId") != owner.RingId.ToString("D") ||
                (string?)xml.Attribute("purpose") != Domain || (string?)xml.Attribute("mode") != Mode ||
                (string?)xml.Attribute("protection") != Protection || xml.Elements().Count() != 1) throw Invalid();
            var key = xml.Elements().Single();
            if (key.Name != "key" || key.Attributes().Count() != 2 || (string?)key.Attribute("version") != "1" ||
                name != "key-" + (string?)key.Attribute("id") + ".xml" || key.Elements().Count() != 4 ||
                new[] { "creationDate", "activationDate", "expirationDate", "descriptor" }
                    .Any(field => key.Elements(field).Count() != 1)) throw Invalid();
            var encrypted = key.Descendants().Count(item => item.Name.LocalName == "encryptedSecret");
            var clear = key.Descendants().Count(item => item.Name.LocalName == "masterKey");
            if (Protection == "certificate" ? encrypted != 1 || clear != 0 : encrypted != 0 || clear != 1)
                throw Invalid();
            return new XElement(key);
        }

        private string[] Entries()
        {
            var paths = Directory.EnumerateFileSystemEntries(directory).Take(MaximumKeys + 3).ToArray();
            if (paths.Length > MaximumKeys + 2) throw Invalid();
            foreach (var path in paths) Paths.ValidateFile(path);
            if (restoreGate && !paths.Any(path => Path.GetFileName(path) == IdentityRestoreQuarantine.FileName))
                throw Invalid();
            return paths.Select(path => Path.GetFileName(path))
                .Where(name => !restoreGate || name != IdentityRestoreQuarantine.FileName).ToArray();
        }

        private static bool KeyName(string name) => name.Length == 44 &&
            name.StartsWith("key-", StringComparison.Ordinal) && name.EndsWith(".xml", StringComparison.Ordinal) &&
            Guid.TryParseExact(name[4..^4], "D", out var id) && id != Guid.Empty &&
            "key-" + id.ToString("D") + ".xml" == name;

        private static string Text(JsonElement value, string name)
        {
            if (!value.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String) throw Invalid();
            return field.GetString()!;
        }

        private static byte[] ReadFile(string path, bool allowEmpty = false)
        {
            Paths.ValidateFile(path);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < (allowEmpty ? 0 : 1) || stream.Length > MaximumFileBytes) throw Invalid();
            var bytes = new byte[MaximumFileBytes + 1];
            var total = 0;
            while (total < bytes.Length)
            {
                var count = stream.Read(bytes, total, bytes.Length - total);
                if (count == 0) break;
                total += count;
            }
            if (total < (allowEmpty ? 0 : 1) || total > MaximumFileBytes) throw Invalid();
            return bytes[..total];
        }

        private void WriteOwner(Owner owner, bool initial = false)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                version = "1",
                installationId = owner.InstallationId.ToString("D"),
                ringId = owner.RingId.ToString("D"),
                purpose = Domain,
                mode = owner.Mode,
                protection = owner.Protection,
                state = owner.State,
                keys = owner.Keys
            });
            if (bytes.Length > MaximumFileBytes) throw Invalid();
            if (initial)
            {
                WriteNew(OwnerPath, bytes);
                IdentityDiagnostics.Checkpoint("key_owner_created");
            }
            else
            {
                var next = Path.Combine(directory, NextName);
                WriteNew(next, bytes, "key_inventory_partial");
                IdentityDiagnostics.Checkpoint("key_inventory_written");
                Effect(() => File.Move(next, OwnerPath, overwrite: true));
            }
        }

        private void WriteNew(string path, byte[] bytes, string? partialCheckpoint = null)
        {
            if (bytes.Length is < 1 or > MaximumFileBytes) throw Invalid();
            Effect(() =>
            {
                if (OperatingSystem.IsWindows()) throw Invalid();
                Paths.ValidateRoot(directory, allowMissing: false);
                Paths.ValidateFile(path, allowMissing: true);
                using var stream = new FileStream(path, new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    UnixCreateMode = Paths.FileMode
                });
                if (partialCheckpoint is not null)
                {
                    IdentityDiagnostics.Checkpoint("key_inventory_empty");
                    var split = bytes.Length / 2;
                    stream.Write(bytes.AsSpan(0, split));
                    stream.Flush(flushToDisk: true);
                    IdentityDiagnostics.Checkpoint(partialCheckpoint);
                    preparationBudget?.Check();
                    IdentityDiagnostics.Current?.Budget.Check();
                    stream.Write(bytes.AsSpan(split));
                }
                else stream.Write(bytes);
                stream.Flush(flushToDisk: true);
                Paths.ValidateFile(path);
            });
        }

        private void Effect(Action effect)
        {
            preparationBudget?.Check();
            IdentityDiagnostics.Current?.Budget.Check();
            authority?.Invoke();
            IdentityDiagnostics.Current?.PossibleCommit();
            effect();
            IdentityDiagnostics.Current?.Committed();
            authority?.Invoke();
            IdentityDiagnostics.Current?.Budget.Check();
            preparationBudget?.Check();
        }

        private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
