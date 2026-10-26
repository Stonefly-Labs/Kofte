using Kofte.Platform.Modules.Composition;
using static Kofte.Platform.Tests.Modules.Composition.CompositionResolverTests;

namespace Kofte.Platform.Tests.Modules.Composition;

public sealed class ContributionClaimTests
{
    [Theory]
    [InlineData("org.test.other", "route", "read", "1.0.0", "GET", "/items")]
    [InlineData("org.test.a", "invented", "read", "1.0.0", null, null)]
    [InlineData("org.test.a", "route", "BAD/secret", "1.0.0", "GET", "/items")]
    [InlineData("org.test.a", "route", "read", "2.0.0", "GET", "/items")]
    [InlineData("org.test.a", "route", "read", "1.0.0-alpha", "GET", "/items")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "get", "/items")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items/")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/Items")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items/{id:int}")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items/%61")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items/../read")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items?secret")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items//read")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items/*")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items/{id?}")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/items#fragment")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/köfte")]
    [InlineData("org.test.a", "route", "köfte", "1.0.0", "GET", "/items")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/health/detail")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "POST", "/openapi")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "DELETE", "/api/kofte/modules")]
    [InlineData("org.test.a", "route", "read", "1.0.0", "GET", "/{first}/{second}")]
    public void InvalidAndReservedClaimsFailClosed(string owner, string kind, string name, string version,
        string? method, string? route) =>
        Reject(Resolve([Deploy("org.test.a", claims: [new(owner, kind, name, version, Method: method, Route: route)],
            capabilities: ["backend"])], "org.test.a"), "composition_contribution_conflict");

    [Theory]
    [InlineData("route")]
    [InlineData("resource")]
    [InlineData("tool")]
    [InlineData("frontend")]
    [InlineData("job")]
    [InlineData("event")]
    [InlineData("workflow")]
    [InlineData("provider")]
    [InlineData("permission")]
    public void FutureExecutionIsNeverSilentlyAccepted(string kind) =>
        Reject(Resolve([Deploy("org.test.a", claims: [new("org.test.a", kind, "read",
            Mode: ContributionMode.Executable, Method: kind == "route" ? "GET" : null,
            Route: kind == "route" ? "/items" : null)], capabilities:
            ["backend", "resources", "mcp-tools", "frontend", "jobs", "events", "workflows", "providers"],
            permissions: [new { name = "read", description = "Read." }])], "org.test.a"),
            "composition_contribution_unsupported");

    [Theory]
    [InlineData("/items/{name}")]
    [InlineData("/items/current")]
    public void RouteRenamesAndLiteralOverlapsConflictGlobally(string second) =>
        Reject(Resolve([
            Deploy("org.test.a", claims: [new("org.test.a", "route", "read", Method: "GET", Route: "/items/{id}")], capabilities: ["backend"]),
            Deploy("org.test.b", claims: [new("org.test.b", "route", "read", Method: "GET", Route: second)], capabilities: ["backend"])
        ], "org.test.a", "org.test.b"), "composition_contribution_conflict");

    [Fact]
    public void CapabilityIsRequiredAndPermissionOwnershipComesFromDescriptor()
    {
        Reject(Resolve([Deploy("org.test.a", claims: [new("org.test.a", "resource", "item")])], "org.test.a"),
            "composition_contribution_conflict");
        Order(Resolve([
            Deploy("org.test.a", claims: [new("org.test.a", "permission", "read")], permissions: [new { name = "read", description = "Read A." }]),
            Deploy("org.test.b", claims: [new("org.test.b", "permission", "read")], permissions: [new { name = "read", description = "Read B." }])
        ], "org.test.a", "org.test.b"), "org.test.a", "org.test.b");
        var claim = new ContributionClaim("org.test.a", "permission", "read");
        Reject(Resolve([Deploy("org.test.a", claims: [claim, claim],
            permissions: [new { name = "read", description = "Read." }])], "org.test.a"),
            "composition_contribution_conflict");
    }

    [Fact]
    public void NamesRoutesAndCountsHaveInclusiveBounds()
    {
        ContributionClaim Route(int length) => new("org.test.a", "route", new string('a', 128),
            Method: "GET", Route: "/" + new string('a', length - 1));
        Order(Resolve([Deploy("org.test.a", claims: [Route(256)], capabilities: ["backend"])], "org.test.a"), "org.test.a");
        Reject(Resolve([Deploy("org.test.a", claims: [Route(257)], capabilities: ["backend"])], "org.test.a"),
            "composition_contribution_conflict");
        Reject(Resolve([Deploy("org.test.a", claims: [Route(256) with { LocalName = new string('a', 129) }],
            capabilities: ["backend"])], "org.test.a"), "composition_contribution_conflict");
        var claims = Enumerable.Range(0, 128).Select(i => new ContributionClaim("org.test.a", "resource", $"item{i}")).ToArray();
        var accepted = Assert.IsType<CompositionResolution.Accepted>(
            Resolve([Deploy("org.test.a", claims: claims, capabilities: ["resources"])], "org.test.a"));
        Assert.Equal(128, accepted.Composition.Contributions.Count);
        Assert.Equal(claims.Select(c => c.LocalName).Order(StringComparer.Ordinal),
            accepted.Composition.Contributions.Select(c => c.LocalName));
        Reject(Resolve([Deploy("org.test.a", claims: claims.Append(new("org.test.a", "resource", "overflow")), capabilities: ["resources"])],
            "org.test.a"), "composition_input_limit");
    }

    [Fact]
    public void BackendExecutionRequiresDeclaredOwnedServiceAndCannotClaimPlatformServices()
    {
        var claim = new ContributionClaim("org.test.a", "backend-service", "public-service",
            Mode: ContributionMode.Executable, ServiceType: typeof(ContributionClaimTests));
        var deployment = new ModuleDeployment(Bytes("org.test.a", capabilities: ["backend"]), [claim],
            ownedServiceTypes: [typeof(ContributionClaimTests)]);
        Order(Resolve([deployment], "org.test.a"), "org.test.a");
        Reject(Resolve([Deploy("org.test.a", claims: [claim], capabilities: ["backend"])], "org.test.a"),
            "composition_contribution_conflict");
        var reserved = new ModuleDeployment(Bytes("org.test.a", capabilities: ["backend"]),
            [claim with { ServiceType = typeof(Kofte.Platform.Modules.Storage.IStorageRegistry) }],
            ownedServiceTypes: [typeof(Kofte.Platform.Modules.Storage.IStorageRegistry)]);
        Reject(Resolve([reserved], "org.test.a"), "composition_contribution_conflict");
        var other = new ModuleDeployment(Bytes("org.test.b", capabilities: ["backend"]),
            [claim with { Owner = "org.test.b" }], ownedServiceTypes: [typeof(ContributionClaimTests)]);
        Reject(Resolve([deployment, other], "org.test.a", "org.test.b"), "composition_contribution_conflict");
    }

    [Fact]
    public void ReservationIdentityCannotBeEvadedByVersionBuildMetadataAndRoutesKeepMethodIsolation()
    {
        ContributionClaim claim = new("org.test.a", "resource", "item");
        Reject(Resolve([Deploy("org.test.a", claims: [claim, claim with { ContractVersion = "1.0.0+different" }],
            capabilities: ["resources"])], "org.test.a"), "composition_contribution_conflict");
        var result = Assert.IsType<CompositionResolution.Accepted>(Resolve([
            Deploy("org.test.a", claims:
                [new("org.test.a", "route", "read", Method: "GET", Route: "/items/{id}"),
                 new("org.test.a", "route", "write", Method: "POST", Route: "/items/{name}")],
                capabilities: ["backend"])], "org.test.a"));
        Assert.Equal(["/items/{}", "/items/{}"], result.Composition.Contributions.Select(c => c.Route));
    }
}
