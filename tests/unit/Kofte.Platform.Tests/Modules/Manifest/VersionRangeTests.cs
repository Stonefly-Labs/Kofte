using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class VersionRangeTests
{
    [Theory]
    [InlineData(">=1.0.0 <2.0.0", "1.0.0", true)]
    [InlineData(">=1.0.0 <2.0.0", "1.999.0+build", true)]
    [InlineData(">=1.0.0 <2.0.0", "2.0.0", false)]
    [InlineData(">=1.0.0 <2.0.0", "0.9.9", false)]
    [InlineData(">=1.0.0 <2.0.0", "1.1.0-rc.1", false)]
    [InlineData(">1.0.0 <=2.0.0", "1.0.0", false)]
    [InlineData(">1.0.0 <=2.0.0", "2.0.0", true)]
    [InlineData(">=1.1.0-rc.1 <2.0.0", "1.1.0-rc.2", true)]
    [InlineData(">=1.1.0-rc.1 <2.0.0", "1.2.0-rc.1", false)]
    [InlineData("1.0.0+one", "1.0.0+two", true)]
    [InlineData("1.0.0+one", "1.0.0-rc.1", false)]
    [InlineData(">=1.0.0 <=1.0.0", "1.0.0", true)]
    [InlineData(">1.0.0 <1.0.2", "1.0.1", true)]
    public void IndependentBounds(string text, string target, bool includes)
    {
        Assert.True(VersionRange.TryParse(text, out var range));
        Assert.Equal(includes, range.Contains(SemanticVersionTests.Parse(target)));
    }

    [Theory]
    [InlineData(">=2.0.0 <1.0.0")]
    [InlineData(">=1.0.0 <1.0.0")]
    [InlineData(">1.0.0 <=1.0.0")]
    [InlineData("^1.0.0")]
    [InlineData("~1.0.0")]
    [InlineData("=1.0.0")]
    [InlineData("1.0")]
    [InlineData("*")]
    [InlineData(">=1.0.0")]
    [InlineData(">=1.0.0  <2.0.0")]
    [InlineData(">=1.0.0\t<2.0.0")]
    [InlineData(" >=1.0.0 <2.0.0")]
    [InlineData(">=1.0.0 <2.0.0 ")]
    [InlineData(">=1.02.0 <2.0.0")]
    [InlineData("1.0.0 || 2.0.0")]
    public void UnsupportedOrEmptyRangesReject(string text) => Assert.False(VersionRange.TryParse(text, out _));
}
