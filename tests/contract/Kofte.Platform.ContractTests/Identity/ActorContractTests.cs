using Kofte.Identity.Consumer;
using Kofte.Platform.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public sealed class ActorContractTests
{
    [Fact]
    public void SeparatelyCompiledProbeHasOnlyImmutableLocalActorAndNoCredentialOrPermissionPayload()
    {
        var properties = typeof(AuthenticatedActorV1).GetProperties();
        Assert.Equal(["IdentityMode", "IsInitialAdministrator", "UserId"],
            properties.Select(item => item.Name).Order(StringComparer.Ordinal));
        Assert.All(properties, item => Assert.Null(item.SetMethod));
        Assert.True(typeof(AuthenticatedActorV1).IsSealed);
        Assert.Equal(["Actor"], typeof(ICurrentActorV1).GetProperties().Select(item => item.Name));
        Assert.Null(typeof(ICurrentActorV1).GetProperty("Actor")!.SetMethod);
        Assert.Null(ActorProbe.Observe(new CurrentActor(null)));
        var actor = new AuthenticatedActorV1(Guid.NewGuid(), IdentityMode.Oidc, true);
        Assert.Same(actor, ActorProbe.Observe(new CurrentActor(actor)));
        Assert.Equal(actor, new AuthenticatedActorV1(actor.UserId, IdentityMode.Oidc, true));
    }

    [Theory]
    [InlineData(IdentityMode.Oidc)]
    [InlineData(IdentityMode.Development)]
    public void InvalidIdentityCannotBeConstructedEvenByTrustedConsumer(IdentityMode mode)
    {
        Assert.Throws<ArgumentException>(() => new AuthenticatedActorV1(Guid.Empty, mode, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticatedActorV1(Guid.NewGuid(), (IdentityMode)99, false));
    }

    private sealed class CurrentActor(AuthenticatedActorV1? actor) : ICurrentActorV1
    {
        public AuthenticatedActorV1? Actor { get; } = actor;
    }
}
