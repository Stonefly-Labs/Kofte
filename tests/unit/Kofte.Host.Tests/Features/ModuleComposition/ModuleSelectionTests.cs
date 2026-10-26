using System.Runtime.InteropServices;
using System.Text;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Infrastructure.Storage;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class ModuleSelectionTests
{
    [Fact]
    public void UnsetIsEmptyButExplicitEmptyAndMissingAreRefused()
    {
        using var fixture = new SelectionFile();
        Assert.Empty(ModuleSelectionReader.Read(null, fixture.Root).Modules);
        foreach (var path in new[] { "", " ", Path.Combine(fixture.Root, "missing-private-canary") })
        {
            var error = Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Read(path, fixture.Root));
            Assert.Equal("composition_configuration_invalid", error.Code);
            Assert.DoesNotContain("private-canary", error.Message);
            Assert.DoesNotContain(fixture.Root, error.Message);
        }
    }

    [Theory]
    [InlineData("""{"schemaVersion":"1","modules":[]}""")]
    [InlineData("""{"modules":[{"id":"org.example.alpha","settings":{},"enabled":false}],"schemaVersion":"1"}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":true,"settings":{"field":{"nested":"value"}}}]}""")]
    public void StrictVersionedDocumentsReadOnceAndPreserveExplicitIntent(string json)
    {
        using var fixture = new SelectionFile();
        fixture.Write(Encoding.UTF8.GetBytes(json));
        var document = ModuleSelectionReader.Read("modules.json", fixture.Root);
        fixture.Write(Encoding.UTF8.GetBytes("""{"schemaVersion":"1","modules":[]}"""));
        Assert.Equal(json.Contains("org.example.alpha", StringComparison.Ordinal) ? 1 : 0, document.Modules.Count);
        if (document.Modules.Count > 0)
        {
            Assert.Equal("org.example.alpha", document.Modules[0].Owner.Value);
            Assert.Equal(json.Contains("\"enabled\":true", StringComparison.Ordinal), document.Modules[0].Enabled);
        }
        Assert.DoesNotContain("nested", document.ToString());
    }

    [Theory]
    [InlineData("""{"schemaVersion":"1","modules":[],"private-canary":"value"}""")]
    [InlineData("""{"schemaVersion":1,"modules":[]}""")]
    [InlineData("""{"schemaVersion":"2","modules":[]}""")]
    [InlineData("""{"schemaVersion":"1","modules":null}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":"true","settings":{}}]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":null,"settings":{}}]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","settings":{}}]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"Org.example.alpha","enabled":true,"settings":{}}]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":false,"settings":null}]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":true,"settings":{},"private-canary":null}]}""")]
    [InlineData("""{"schemaVersion":"1","schema\u0056ersion":"1","modules":[]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":true,"settings":{"same":1,"\u0073ame":2}}]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":true,"settings":{}},{"id":"org.example.alpha","enabled":false,"settings":{}}]}""")]
    [InlineData("""{"schemaVersion":"1",/*private-canary*/"modules":[]}""")]
    [InlineData("""{"schemaVersion":"1","modules":[],}""")]
    [InlineData("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":false,"settings":{"x":"\uD800"}}]}""")]
    public void InvalidDocumentsAreSanitizedEvenForDisabledEntries(string json)
    {
        var error = Assert.Throws<ModuleSelectionException>(() =>
            ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(json)));
        Assert.DoesNotContain("private-canary", error.Message);
    }

    [Fact]
    public void InvalidUtf8BomDepthEntriesAndOriginalSettingsBytesReject()
    {
        foreach (var input in new[]
        {
            new byte[] { 0xff },
            new byte[] { 0xef, 0xbb, 0xbf }.Concat(Encoding.UTF8.GetBytes("""{"schemaVersion":"1","modules":[]}""")).ToArray(),
            Encoding.UTF8.GetBytes("""{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":true,"settings":{"a":{"b":{"c":{"d":{"e":{}}}}}}}]}"""),
            new byte[1_048_577]
        })
            Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(input));
        var entry = """{"id":"org.example.alpha","enabled":false,"settings":{}}""";
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","modules":[""" + string.Join(',', Enumerable.Repeat(entry, 33)) + "]}")));
        var prefix = """{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":false,"settings":""";
        ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(prefix + "{" + new string(' ', 16_382) + "}}]}"));
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(
            prefix + "{" + new string(' ', 16_383) + "}}]}")));
    }

    [Fact]
    public void NonPrivateDirectoriesFilesAndLinkedFilesAreRefused()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        using var fixture = new SelectionFile();
        var valid = Encoding.UTF8.GetBytes("""{"schemaVersion":"1","modules":[]}""");
        fixture.Write(valid);
        File.SetUnixFileMode(fixture.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        Refused();
        File.SetUnixFileMode(fixture.Path, UnixStorePaths.FileMode);
        File.SetUnixFileMode(fixture.Root, UnixStorePaths.DirectoryMode | UnixFileMode.GroupExecute);
        Refused();
        File.SetUnixFileMode(fixture.Root, UnixStorePaths.DirectoryMode);
        var other = System.IO.Path.Combine(fixture.Root, "target");
        File.Move(fixture.Path, other);
        File.CreateSymbolicLink(fixture.Path, other);
        Refused();
        File.Delete(fixture.Path);
        Assert.Equal(0, Link(other, fixture.Path));
        Refused();

        void Refused() => Assert.Throws<ModuleSelectionException>(() =>
            ModuleSelectionReader.Read(fixture.Path, fixture.Root));
    }

    [Fact]
    public void ExactFileEntryAndDepthLimitsAreAcceptedAndOneOverRefused()
    {
        using var fixture = new SelectionFile();
        const string empty = """{"schemaVersion":"1","modules":[]}""";
        var exact = Encoding.UTF8.GetBytes(empty + new string(' ', 1_048_576 - empty.Length));
        fixture.Write(exact);
        Assert.Empty(ModuleSelectionReader.Read(fixture.Path, fixture.Root).Modules);
        fixture.Write([.. exact, (byte)' ']);
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Read(fixture.Path, fixture.Root));
        var entries = Enumerable.Range(0, 32).Select(index =>
            "{\"id\":\"org.test.module" + index + "\",\"enabled\":false,\"settings\":{}}").ToArray();
        Assert.Equal(32, ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","modules":[""" + string.Join(',', entries) + "]}")).Modules.Count);
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","modules":[""" + string.Join(',', entries.Append(
                """{"id":"org.test.extra","enabled":false,"settings":{}}""")) + "]}")));
        const string prefix = """{"schemaVersion":"1","modules":[{"id":"org.test.a","enabled":false,"settings":""";
        ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(prefix + """{"a":{"b":{"c":{"d":{}}}}}}]}"""));
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(Encoding.UTF8.GetBytes(
            prefix + """{"a":{"b":{"c":{"d":{"e":{}}}}}}}]}""")));
    }

    [Fact]
    public void OversizedOriginalSettingsAreRejectedBeforeTheirValueIsMaterialized()
    {
        var payload = Encoding.UTF8.GetBytes(
            "{\"schemaVersion\":\"1\",\"modules\":[{\"id\":\"org.test.a\",\"enabled\":false,\"settings\":{\"value\":\"" +
            new string('x', 131_072) + "\"}}]}");
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(payload));
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<ModuleSelectionException>(() => ModuleSelectionReader.Parse(payload));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 65_536,
            "An over-limit settings value must be refused by streaming admission, before DOM/string copies.");
    }

    private sealed class SelectionFile : IDisposable
    {
        public string Root { get; } = System.IO.Path.Combine(Directory.GetCurrentDirectory(),
            "artifacts", "test-runs", "selection-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(Root, "modules.json");
        public SelectionFile()
        {
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
            Directory.CreateDirectory(Root, UnixStorePaths.DirectoryMode);
        }
        public void Write(byte[] bytes)
        {
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
            using var file = new FileStream(Path, new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                UnixCreateMode = UnixStorePaths.FileMode
            });
            file.Write(bytes);
        }
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    private static extern int Link(string existing, string added);
}
