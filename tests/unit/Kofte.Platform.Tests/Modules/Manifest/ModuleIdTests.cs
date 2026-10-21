using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class ModuleIdTests
{
    [Theory]
    [InlineData("org.example.notes", true)]
    [InlineData("0.1.a-b2", true)]
    [InlineData("a.b.c.d", true)]
    [InlineData("Org.example.notes", false)]
    [InlineData(" org.example.notes", false)]
    [InlineData("org.example.notes ", false)]
    [InlineData("org.exämple.notes", false)]
    [InlineData("org..notes", false)]
    [InlineData("org.example.-notes", false)]
    [InlineData("org.example.notes-", false)]
    [InlineData(".org.example.notes", false)]
    [InlineData("org.example.notes.", false)]
    [InlineData("org/example/notes", false)]
    [InlineData("org.notes", false)]
    [InlineData("org.example.no tes", false)]
    [InlineData("org.examp\u200ble.notes", false)]
    [InlineData("org.example.notes:read", false)]
    [InlineData("org\\example\\notes", false)]
    public void CanonicalGrammarHasNoNormalization(string text, bool valid) =>
        Assert.Equal(valid, ModuleId.TryParse(text, out _));

    [Fact]
    public void LabelAndTotalLengthsAreIndependent()
    {
        Assert.True(ModuleId.TryParse("org.example." + new string('a', 63), out _));
        Assert.False(ModuleId.TryParse("org.example." + new string('a', 64), out _));
        var prefix = string.Join('.', Enumerable.Repeat(new string('a', 63), 3));
        Assert.True(ModuleId.TryParse(prefix + "." + new string('a', 61), out var id));
        Assert.False(ModuleId.TryParse(prefix + "." + new string('a', 62), out _));
        Assert.True(ModuleId.TryParse(id!.Value, out var same));
        Assert.Equal(id, same);
        Assert.Equal(id.GetHashCode(), same!.GetHashCode());
    }
}
