using System.Text;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Composition.CompositionResolverTests;

namespace Kofte.Platform.Tests.Modules.Composition;

public sealed class CompositionConfigurationTests
{
    private static ModuleId Owner(string id = "org.test.a")
    {
        Assert.True(ModuleId.TryParse(id, out var owner));
        return owner;
    }

    [Fact]
    public void SettingsAndFieldPathsAreCopiedBoundedAndOwnerOnly()
    {
        var path = new List<string> { "credentials", "token" };
        var field = new ModuleConfigurationField("access-token", path, ModuleSettingType.String, required: true);
        path.Clear();
        var settings = Encoding.UTF8.GetBytes("""{"credentials":{"token":"private-value"}}""");
        var configuration = new ModuleConfiguration(Owner(), settings);
        Array.Fill(settings, (byte)'!');
        Assert.True(configuration.TryGetValue(field, out var value));
        Assert.Equal("private-value", value.GetString());
        Assert.Equal("org.test.a", configuration.Owner.Value);
        Assert.DoesNotContain("private-value", configuration.ToString());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)field.Path).Clear());
        Assert.Throws<ArgumentException>(() => new ModuleConfigurationField("secret/value", ["field"], ModuleSettingType.String));
        Assert.Throws<ArgumentException>(() => new ModuleConfigurationField(new string('a', 65), ["field"], ModuleSettingType.String));
        Assert.Throws<ArgumentException>(() => new ModuleConfigurationField("field", Enumerable.Repeat("nested", 9), ModuleSettingType.String));
        Assert.Equal(8, new ModuleConfigurationField(new string('a', 64), Enumerable.Repeat("nested", 8), ModuleSettingType.String).Path.Count);
    }

    [Fact]
    public void StrictSettingsRejectDuplicateDecodedFieldsAndOriginalByteOverflow()
    {
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), """{"a":1,"\u0061":2}"""u8));
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), "[]"u8));
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), "{/*x*/}"u8));
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), "{\"value\":\"\\ud800\"}"u8));
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), new byte[] { 123, 34, 97, 34, 58, 34, 255, 34, 125 }));
        var exact = Encoding.UTF8.GetBytes("{}" + new string(' ', 16_382));
        _ = new ModuleConfiguration(Owner(), exact);
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), exact.Append((byte)' ').ToArray()));
    }

    [Fact]
    public void ResolverNeverInvokesSettingsValidatorOrRegistrarAndCopiesFieldDefinitions()
    {
        var fields = Enumerable.Range(0, 128).Select(i => new ModuleConfigurationField($"field{i}", [$"field{i}"],
            ModuleSettingType.String)).ToList();
        var deployment = new ModuleDeployment(Bytes("org.test.a"), configurationFields: fields,
            configurationValidator: new ForbiddenValidator(),
            configureServices: (_, _) => throw new InvalidOperationException("registrar"));
        fields.Clear();
        Order(Resolve([deployment], "org.test.a"), "org.test.a");
        Assert.Equal(128, deployment.ConfigurationFields.Count);
        var overflow = new ModuleDeployment(Bytes("org.test.a"), configurationFields:
            Enumerable.Range(0, 129).Select(i => new ModuleConfigurationField($"field{i}", [$"field{i}"], ModuleSettingType.String)));
        Reject(Resolve([overflow], "org.test.a"), "composition_input_limit");
    }

    [Fact]
    public void ConfigurationJsonDepthEightIsInclusiveAndNineRejects()
    {
        static byte[] Nested(int depth) => Encoding.UTF8.GetBytes(
            string.Concat(Enumerable.Repeat("{\"nested\":", depth)) + "0" + new string('}', depth));
        var field = new ModuleConfigurationField("leaf", Enumerable.Repeat("nested", 8), ModuleSettingType.Number);
        var accepted = new ModuleConfiguration(Owner(), Nested(8));
        Assert.True(accepted.TryGetValue(field, out var leaf));
        Assert.Equal(0, leaf.GetInt32());
        Assert.Throws<ArgumentException>(() => new ModuleConfiguration(Owner(), Nested(9)));
    }

    private sealed class ForbiddenValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("validator must not execute in resolver");
    }
}
