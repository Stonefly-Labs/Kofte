using System.Text.Json;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class ModuleConfigurationTests
{
    [Fact]
    public void RequiredTypeAndNestedUnknownFailuresUseOnlyKnownTokens()
    {
        ModuleConfigurationField[] fields =
        [
            new("required", ["required"], ModuleSettingType.String, true),
            new("server", ["server"], ModuleSettingType.Object),
            new("port", ["server", "port"], ModuleSettingType.Number)
        ];
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: fields);
        var settings = new ModuleConfiguration(HostCompositionTestData.Id("org.test.a"),
            """{"server":{"port":"secret-value","secret-key":"credential"},"private-key":42}"""u8);
        var failures = ModuleConfigurationValidator.Validate(deployment, settings, CancellationToken.None);
        Assert.Contains(failures, f => f.Field == "required" && f.Reason == ModuleSettingFailureReason.Required);
        Assert.Contains(failures, f => f.Field == "port" && f.Reason == ModuleSettingFailureReason.Type);
        Assert.Contains(failures, f => f.Field == "server" && f.Reason == ModuleSettingFailureReason.Unknown);
        Assert.Contains(failures, f => f.Field is null && f.Reason == ModuleSettingFailureReason.Unknown);
        var serialized = JsonSerializer.Serialize(failures);
        Assert.DoesNotContain("secret", serialized);
        Assert.DoesNotContain("credential", serialized);
        Assert.DoesNotContain("private-key", serialized);
    }

    [Fact]
    public void ConstraintValidatorSeesCopiedOwnerOnlySettingsAndOnlyItsDeclaredFieldsAreAccepted()
    {
        var field = new ModuleConfigurationField("name", ["name"], ModuleSettingType.String);
        var validator = new Validator(configuration =>
        {
            Assert.Equal("org.test.a", configuration.Owner.Value);
            Assert.False(configuration.Snapshot().TryGetProperty("other-owner", out _));
            return [new(field, ModuleSettingFailureReason.Constraint)];
        });
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: [field], configurationValidator: validator);
        var settings = new ModuleConfiguration(HostCompositionTestData.Id("org.test.a"), """{"name":"value"}"""u8);
        Assert.Equal("name", Assert.Single(ModuleConfigurationValidator.Validate(deployment, settings,
            CancellationToken.None)).Field);
        Assert.Equal(1, validator.Calls);
        var alien = new ModuleConfigurationField("alien", ["alien"], ModuleSettingType.String);
        var invalid = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: [field], configurationValidator: new Validator(_ =>
                [new(alien, ModuleSettingFailureReason.Constraint)]));
        Assert.Equal("composition_setting_invalid", Assert.Throws<CompositionBoundaryException>(() =>
            ModuleConfigurationValidator.Validate(invalid, settings, CancellationToken.None)).Code);
    }

    [Fact]
    public void StructuralFailureDoesNotInvokeAuthorAndEnumerationIsBounded()
    {
        var validator = new Validator(_ => throw new InvalidOperationException("must not execute"));
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: [new("name", ["name"], ModuleSettingType.String, true)],
            configurationValidator: validator);
        Assert.Single(ModuleConfigurationValidator.Validate(deployment,
            new(HostCompositionTestData.Id("org.test.a"), "{}"u8), CancellationToken.None));
        Assert.Equal(0, validator.Calls);
        var field = new ModuleConfigurationField("name", ["name"], ModuleSettingType.String);
        var enumerated = 0;
        IEnumerable<ModuleConfigurationFailure> Infinite()
        {
            while (true)
            {
                enumerated++;
                yield return new(field, ModuleSettingFailureReason.Constraint);
            }
        }
        var overflow = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            configurationFields: [field], configurationValidator: new Validator(_ => Infinite()));
        Assert.Throws<CompositionBoundaryException>(() => ModuleConfigurationValidator.Validate(overflow,
            new(HostCompositionTestData.Id("org.test.a"), "{}"u8), CancellationToken.None));
        Assert.Equal(129, enumerated);
    }

    private sealed class Validator(Func<ModuleConfiguration, IEnumerable<ModuleConfigurationFailure>> validate)
        : IModuleConfigurationValidator
    {
        public int Calls { get; private set; }
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            Calls++;
            return validate(configuration);
        }
    }
}

internal static class HostCompositionTestData
{
    internal static ModuleId Id(string value)
    {
        Assert.True(ModuleId.TryParse(value, out var owner));
        return owner;
    }

    internal static byte[] Manifest(string id, params string[] dependencies) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = "1",
        id,
        version = "1.0.0",
        displayName = "Test",
        hostContract = "1.0.0",
        capabilities = Array.Empty<object>(),
        dependencies = dependencies.Select(dependency => new { id = dependency, versionRange = "1.0.0" }),
        permissions = Array.Empty<object>(),
        storage = new { relational = false }
    });
}
