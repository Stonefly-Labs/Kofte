using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("0.0.0", true)]
    [InlineData("2147483648.0.0+001", true)]
    [InlineData("99999999999999999999999999999.0.0-a.0+01", true)]
    [InlineData("1.0.0-0.alpha-2+01.x", true)]
    [InlineData("1.02.0", false)]
    [InlineData("1.0", false)]
    [InlineData("1.0.0-01", false)]
    [InlineData("1.0.0+", false)]
    [InlineData("1.0.0-a..b", false)]
    [InlineData("1.0.0-ä", false)]
    [InlineData(" 1.0.0", false)]
    [InlineData("1.0.0\n", false)]
    public void FullBoundedSemver(string text, bool valid) =>
        Assert.Equal(valid, SemanticVersion.TryParse(text, out _));

    [Fact]
    public void PrecedenceIsNumericOrdinalAndBuildInsensitive()
    {
        var chain = new[] { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta",
            "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0", "1.0.1", "1.1.0", "1.10.0",
            "99999999999999999999999999999.0.0" };
        for (var i = 1; i < chain.Length; i++) Assert.True(Parse(chain[i - 1]).CompareTo(Parse(chain[i])) < 0);
        Assert.Equal(0, Parse("1.0.0+one").CompareTo(Parse("1.0.0+two")));
        Assert.True(Parse("1.0.0-9").CompareTo(Parse("1.0.0-a")) < 0);
        Assert.True(Parse("1.0.0-a").CompareTo(Parse("1.0.0-a.0")) < 0);
        Assert.True(SemanticVersion.TryParse("1.0.0+" + new string('a', 122), out _));
        Assert.False(SemanticVersion.TryParse("1.0.0+" + new string('a', 123), out _));
    }
    internal static SemanticVersion Parse(string value)
    {
        Assert.True(SemanticVersion.TryParse(value, out var parsed));
        return parsed;
    }
}
