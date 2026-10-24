using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class CompositionPreflightTests
{
    [Fact]
    public async Task DisabledSettingsAreEnvelopeOnlyAndNoMutatingCallbackRunsDuringPreflight()
    {
        var calls = 0;
        var enabled = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configureServices: (_, _) => calls++, createRuntime: (_, _) => throw new InvalidOperationException());
        var disabled = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.b"),
            configurationFields: [new("required", ["required"], ModuleSettingType.String, true)],
            configurationValidator: new NeverValidator(),
            configureServices: (_, _) => calls++, createRuntime: (_, _) => throw new InvalidOperationException());
        var selection = Selection("""[{"id":"org.test.a","enabled":true,"settings":{}},{"id":"org.test.b","enabled":false,"settings":{"unrecognized":"private"}}]""");
        var result = await Resolve([disabled, enabled], selection);
        Assert.Equal(["org.test.a"], result.Composition.Order.Select(id => id.Value));
        Assert.Single(result.Configurations);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GraphFailurePrecedesSettingsAndSettingsFailurePrecedesClaims()
    {
        var invalidGraph = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a", "org.test.missing"),
            configurationValidator: new NeverValidator());
        var failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve([invalidGraph],
            Selection("""[{"id":"org.test.a","enabled":true,"settings":{}}]""")));
        Assert.Equal("composition_dependency_missing", failure.Code);
        var invalidSettings = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            claims: [new("org.test.a", "route", "private", Method: "GET", Route: "/health")],
            configurationFields: [new("required", ["required"], ModuleSettingType.String, true)]);
        failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve([invalidSettings],
            Selection("""[{"id":"org.test.a","enabled":true,"settings":{}}]""")));
        Assert.Equal("composition_setting_invalid", failure.Code);
    }

    [Fact]
    public async Task RejectionRetainsExactOrderedOwnersRelatedIdentitiesAndNestedManifestFields()
    {
        var records = new List<CompositionRecord>();
        var failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve(
            [new(HostCompositionTestData.Manifest("org.test.b", "org.test.missing-b")),
                new(HostCompositionTestData.Manifest("org.test.a", "org.test.missing-a"))],
            Selection("""[{"id":"org.test.b","enabled":true,"settings":{}},{"id":"org.test.a","enabled":true,"settings":{}}]"""),
            records));
        Assert.Equal(["org.test.a", "org.test.b"], failure.Diagnostics.Select(d => d.Owner!.Value));
        Assert.All(failure.Diagnostics, d => Assert.Equal("composition_dependency_missing", d.Code));
        Assert.Equal(["org.test.missing-a"], failure.Diagnostics[0].RelatedIds.Select(id => id.Value));
        Assert.Equal(["org.test.missing-b"], failure.Diagnostics[1].RelatedIds.Select(id => id.Value));
        Assert.Equal(["org.test.a", "org.test.b"], records.Where(r => r.Outcome == "rejected").Select(r => r.Owner));
        var mutableView = Assert.IsAssignableFrom<IList<CompositionDiagnostic>>(failure.Diagnostics);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Clear());
        var malformed = JsonNode.Parse(HostCompositionTestData.Manifest("org.test.a"))!;
        malformed["version"] = "invalid-private-value";
        malformed["displayName"] = "";
        malformed["hostContract"] = "private-invalid-range";
        failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve(
            [new(Encoding.UTF8.GetBytes(malformed.ToJsonString()))], Selection("[]")));
        Assert.Equal(["version_invalid", "display_name_invalid", "range_invalid"],
            failure.Diagnostics.Select(d => d.ManifestDiagnostic!.Code));
        Assert.Equal(["/version", "/displayName", "/hostContract"],
            failure.Diagnostics.Select(d => d.ManifestDiagnostic!.Path));
        Assert.DoesNotContain("private", JsonSerializer.Serialize(failure.Diagnostics));
        Assert.Empty(failure.Settings);
    }

    [Fact]
    public async Task SettingsRetainOnlyKnownFieldReasonAndOwnerAndTruncateExplicitly()
    {
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: [new("server", ["server"], ModuleSettingType.Object)]);
        var records = new List<CompositionRecord>();
        var failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve([deployment],
            Selection("""[{"id":"org.test.a","enabled":true,"settings":{"server":{"private-key":"secret-value"}}}]"""), records));
        var setting = Assert.Single(failure.Settings);
        Assert.Equal("org.test.a", setting.Owner.Value);
        Assert.Equal("server", setting.Field);
        Assert.Equal(ModuleSettingFailureReason.Unknown, setting.Reason);
        Assert.Empty(failure.Diagnostics);
        Assert.DoesNotContain("private", JsonSerializer.Serialize(failure.Settings));
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(failure.Settings));
        var projected = Assert.Single(records, record => record.Code == "composition_setting_invalid");
        Assert.Equal("server", projected.FieldToken);
        Assert.Equal("unknown", projected.FieldReason);
        Assert.DoesNotContain("private-key", JsonSerializer.Serialize(records));
        Assert.DoesNotContain("secret-value", JsonSerializer.Serialize(records));
        records.Clear();
        deployment = new(HostCompositionTestData.Manifest("org.test.a"), configurationFields: Enumerable.Range(0, 128)
            .Select(index => new ModuleConfigurationField("field" + index, ["field" + index], ModuleSettingType.String, true)));
        failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve([deployment],
            Selection("""[{"id":"org.test.a","enabled":true,"settings":{}}]"""), records));
        Assert.Equal(63, failure.Settings.Count);
        Assert.True(failure.SettingsTruncated);
        Assert.Equal(64, records.Count(r => r.Outcome == "rejected"));
        Assert.All(records.Where(record => record.Code == "composition_setting_invalid"),
            record => Assert.Contains(deployment.ConfigurationFields, field => field.Token == record.FieldToken));
        Assert.Equal("composition_diagnostics_truncated", records[^1].Code);
        Assert.Null(records[^1].FieldToken);
        Assert.Null(records[^1].FieldReason);
    }

    [Fact]
    public async Task RequiredSettingsCannotFallBackToEnvironmentAndTwoFieldConstraintHasNoMutatingEffects()
    {
        const string environment = "KOFTE_MODULES_ORG_TEST_A_REQUIRED";
        var previous = Environment.GetEnvironmentVariable(environment);
        var calls = 0;
        var authorCalls = 0;
        var minimum = new ModuleConfigurationField("minimum", ["minimum"], ModuleSettingType.Number, true);
        var maximum = new ModuleConfigurationField("maximum", ["maximum"], ModuleSettingType.Number, true);
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: [minimum, maximum],
            configurationValidator: new InspectValidator(configuration =>
            {
                authorCalls++;
                var settings = configuration.Snapshot();
                return settings.GetProperty("minimum").GetInt32() > settings.GetProperty("maximum").GetInt32()
                    ? [new(maximum, ModuleSettingFailureReason.Constraint)] : [];
            }),
            configureServices: (_, _) => calls++, createRuntime: (_, _) =>
            {
                calls++;
                throw new InvalidOperationException();
            });
        try
        {
            Environment.SetEnvironmentVariable(environment, """{"minimum":1,"maximum":10}""");
            var failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve([deployment],
                Selection("""[{"id":"org.test.a","enabled":true,"settings":{}}]""")));
            Assert.Equal(["maximum", "minimum"], failure.Settings.Select(issue => issue.Field));
            Assert.All(failure.Settings, issue => Assert.Equal(ModuleSettingFailureReason.Required, issue.Reason));
            Assert.Equal(0, authorCalls);
            failure = await Assert.ThrowsAsync<CompositionPreflightException>(() => Resolve([deployment],
                Selection("""[{"id":"org.test.a","enabled":true,"settings":{"minimum":10,"maximum":2}}]""")));
            var constraint = Assert.Single(failure.Settings);
            Assert.Equal("maximum", constraint.Field);
            Assert.Equal(ModuleSettingFailureReason.Constraint, constraint.Reason);
            Assert.Equal(1, authorCalls);
            Assert.Equal(0, calls);
        }
        finally { Environment.SetEnvironmentVariable(environment, previous); }
    }

    [Fact]
    public async Task ReadSelectionRetainsExactOriginalSettingsAfterPrivateFileReplacement()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        var root = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "test-runs",
            "composition-snapshot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(root, "modules.json");
        const string original = """{ "name" : "\u0061", "port" : 17 }""";
        try
        {
            File.WriteAllText(path, """{"schemaVersion":"1","modules":[{"id":"org.test.a","enabled":true,"settings":""" +
                original + "}]}");
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            var selection = ModuleSelectionReader.Read(path, root);
            File.WriteAllText(path, """{"schemaVersion":"1","modules":[]}""");
            var observed = 0;
            var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
                configurationFields: [new("name", ["name"], ModuleSettingType.String),
                    new("port", ["port"], ModuleSettingType.Number)],
                configurationValidator: new InspectValidator(configuration =>
                {
                    Assert.Equal(original, configuration.Snapshot().GetRawText());
                    observed++;
                    return [];
                }));
            var resolved = await Resolve([deployment], selection);
            Assert.Equal(1, observed);
            Assert.Equal(original, resolved.Configurations[HostCompositionTestData.Id("org.test.a")].Snapshot().GetRawText());
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static ModuleSelectionDocument Selection(string entries) =>
        ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes("""{"schemaVersion":"1","modules":""" + entries + "}"));
    private static Task<CompositionPreflightResult> Resolve(ModuleDeployment[] catalog, ModuleSelectionDocument selection,
        List<CompositionRecord>? records = null)
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", record => records?.Add(record));
        return CompositionPreflight.ResolveAsync(catalog, selection, new(diagnostics), diagnostics,
            TestContext.Current.CancellationToken);
    }
    private sealed class NeverValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token) =>
            throw new InvalidOperationException("must not execute");
    }
    private sealed class InspectValidator(Func<ModuleConfiguration, IEnumerable<ModuleConfigurationFailure>> inspect)
        : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token) =>
            inspect(configuration);
    }
}
