using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Composition.CompositionResolverTests;

namespace Kofte.Platform.Tests.Modules.Composition;

public sealed class CompositionLimitTests
{
    [Theory]
    [InlineData(16, 64, false)]
    [InlineData(17, 1, false)]
    [InlineData(17, 1, true)]
    [InlineData(32, 64, false)]
    [InlineData(31, 64, true)]
    public void DisabledCatalogPrerequisitesDoNotConsumeEnabledGraphEdges(int disabled, int lastCount, bool enabled)
    {
        var owners = Enumerable.Range(0, disabled).Select(index => $"org.test.disabled{index}").ToArray();
        var callbacks = 0;
        var catalog = owners.Select((owner, index) => Deploy(owner, Enumerable.Range(0,
            index == disabled - 1 ? lastCount : 64).Select(dependency =>
                $"org.missing.required{index * 64 + dependency}").ToArray(), callback: () => callbacks++)).ToList();
        if (enabled) catalog.Add(Deploy("org.test.enabled"));
        foreach (var explicitFalse in new[] { false, true })
        {
            var selected = owners.Where(_ => explicitFalse).Select(owner => new ModuleSelection(owner, false)).ToList();
            if (enabled) selected.Add(new("org.test.enabled", true));
            var result = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(catalog, selected,
                new("1.0.0"), TestContext.Current.CancellationToken));
            Assert.Equal(catalog.Count, result.Composition.Deployed.Count);
            Assert.Equal(owners.Order(StringComparer.Ordinal), result.Composition.Disabled.Select(owner => owner.Value));
            Assert.Equal(enabled ? ["org.test.enabled"] : Array.Empty<string>(),
                result.Composition.Order.Select(owner => owner.Value));
            Assert.Empty(result.Composition.Dependencies);
            Assert.Equal(0, callbacks);
        }
    }

    [Fact]
    public void EnumerationStopsAtFirstRefusedEntry()
    {
        var seen = 0;
        IEnumerable<ModuleDeployment> Catalog()
        {
            for (var i = 0; i < 34; i++)
            {
                seen++;
                if (i == 33) throw new InvalidOperationException("over-enumeration");
                yield return Deploy($"org.test.m{i}");
            }
        }
        Reject(Resolve(Catalog()), "composition_input_limit");
        Assert.Equal(33, seen);
        var selected = 0;
        IEnumerable<ModuleSelection> Selection()
        {
            for (var i = 0; i < 18; i++)
            {
                selected++;
                if (i == 17) throw new InvalidOperationException("over-enumeration");
                yield return new($"org.test.m{i}", true);
            }
        }
        Reject(CompositionResolver.Resolve(Enumerable.Range(0, 32).Select(i => Deploy($"org.test.m{i}")),
            Selection(), new("1.0.0"), TestContext.Current.CancellationToken), "composition_input_limit");
        Assert.Equal(17, selected);
        Assert.IsType<CompositionResolution.Accepted>(Resolve(Enumerable.Range(0, 32).Select(i => Deploy($"org.test.m{i}"))));
        Assert.IsType<CompositionResolution.Accepted>(Resolve(Enumerable.Range(0, 16).Select(i => Deploy($"org.test.m{i}")),
            Enumerable.Range(0, 16).Select(i => $"org.test.m{i}").ToArray()));
    }

    [Fact]
    public void InputsAndResultsAreIndependentCopies()
    {
        var bytes = Bytes("org.test.a");
        var claims = new List<ContributionClaim> { new("org.test.a", "permission", "missing") };
        var deployment = new ModuleDeployment(bytes);
        Array.Fill(bytes, (byte)'!');
        var returned = deployment.Manifest;
        Assert.True(MemoryMarshal.TryGetArray(returned, out var segment));
        Array.Fill(segment.Array!, (byte)'!');
        Order(Resolve([deployment], "org.test.a"), "org.test.a");
        var copied = new ModuleDeployment(Bytes("org.test.b"), claims);
        claims.Clear();
        Assert.Single(copied.Claims);
        var result = Assert.IsType<CompositionResolution.Accepted>(Resolve([deployment], "org.test.a"));
        Assert.Throws<NotSupportedException>(() => ((IList<ModuleId>)result.Composition.Order).Clear());
    }

    [Fact]
    public void ByteAndContractLimitsRejectWithoutPartialAcceptance()
    {
        var valid = Bytes("org.test.a");
        var exact = valid.Concat(Enumerable.Repeat((byte)' ', 65536 - valid.Length)).ToArray();
        Order(Resolve([new ModuleDeployment(exact)], "org.test.a"), "org.test.a");
        Reject(Resolve([new ModuleDeployment(exact.Append((byte)' ').ToArray())]), "composition_input_limit");
        Reject(Resolve([new ModuleDeployment(valid, contractVersion: 2)]), "composition_contract_unsupported");
    }

    [Fact]
    public void CancellationAndZeroRemainingBudgetAreExplicitRefusals()
    {
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancelled.Cancel();
        Reject(CompositionResolver.Resolve([], [], new("1.0.0"), cancelled.Token), "composition_cancelled");
        Reject(CompositionResolver.Resolve([], [], new("1.0.0"), TestContext.Current.CancellationToken, TimeSpan.Zero),
            "composition_deadline_exceeded");
    }

    [Fact]
    public void CyclesLimitRelatedMembersAndDiagnosticsKeepExactPrefixWithMarker()
    {
        var ids = Enumerable.Range(0, 10).Select(i => $"org.test.m{i}").ToArray();
        var cycle = ids.Select((id, i) => Deploy(id, [ids[(i + 1) % ids.Length]])).ToArray();
        var rejected = Reject(Resolve(cycle, ids), "composition_dependency_cycle");
        Assert.Equal(ids.Take(8), rejected.Diagnostics[0].RelatedIds.Select(id => id.Value));
        Assert.Equal(2, rejected.Diagnostics[0].OmittedRelatedCount);
        var errors = Enumerable.Range(0, 16).Select(i => Deploy($"org.test.m{i}",
            Enumerable.Range(0, 5).Select(d => $"org.missing.m{d}").ToArray())).ToArray();
        var result = Assert.IsType<CompositionResolution.Rejected>(Resolve(errors,
            Enumerable.Range(0, 16).Select(i => $"org.test.m{i}").ToArray()));
        Assert.Equal(64, result.Diagnostics.Count);
        Assert.All(result.Diagnostics.Take(63), d => Assert.Equal("composition_dependency_missing", d.Code));
        Assert.Equal("composition_diagnostics_truncated", result.Diagnostics[^1].Code);
        Assert.True(JsonSerializer.SerializeToUtf8Bytes(result.Diagnostics).Length <= 65_536);
        Assert.Equal(Enumerable.Range(0, 16).Select(i => $"org.test.m{i}").Order(StringComparer.Ordinal).SelectMany(id =>
            Enumerable.Range(0, 5).Select(d => (id, related: $"org.missing.m{d}"))).Take(63),
            result.Diagnostics.Take(63).Select(d => (d.Owner!.Value, d.RelatedIds.Single().Value)));
    }

    [Fact]
    public void EnabledContributionTotalIsBoundedAndDisabledClaimsRemainInert()
    {
        ModuleDeployment Module(int i, int count) => Deploy($"org.test.m{i}",
            claims: Enumerable.Range(0, count).Select(n => new ContributionClaim($"org.test.m{i}", "resource", $"item{n}")),
            capabilities: ["resources"]);
        var enabled = Enumerable.Range(0, 8).Select(i => $"org.test.m{i}").ToArray();
        var catalog = Enumerable.Range(0, 8).Select(i => Module(i, 128)).ToArray();
        var accepted = Assert.IsType<CompositionResolution.Accepted>(Resolve(catalog, enabled));
        Assert.Equal(1024, accepted.Composition.Contributions.Count);
        Assert.All(accepted.Composition.Contributions.GroupBy(c => c.Owner),
            owner => Assert.Equal(128, owner.Count()));
        Reject(Resolve(catalog.Append(Module(8, 1)), enabled.Append("org.test.m8").ToArray()), "composition_input_limit");
        Assert.IsType<CompositionResolution.Accepted>(Resolve([Module(0, 129)]));
    }

    [Fact]
    public void AggregateManifestBudgetIsInclusiveAndSelectionEnumerationStopsAtThirtyThird()
    {
        var deployed = Enumerable.Range(0, 32).Select(i =>
        {
            var bytes = Bytes($"org.test.m{i}");
            return new ModuleDeployment(bytes.Concat(Enumerable.Repeat((byte)' ', 65_536 - bytes.Length)).ToArray());
        }).ToArray();
        // 32 * 65,536 = 2,097,152; aggregate +1 must first exceed a catalog or individual-manifest cap.
        Assert.IsType<CompositionResolution.Accepted>(Resolve(deployed));
        var selection = Enumerable.Range(0, 32).Select(i => new ModuleSelection($"org.test.m{i}", false)).ToArray();
        var accepted = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(deployed, selection,
            new("1.0.0"), TestContext.Current.CancellationToken));
        Assert.Equal(32, accepted.Composition.Disabled.Count);
        var count = 0;
        IEnumerable<ModuleSelection> Entries()
        {
            while (count < 34)
            {
                if (++count == 34) throw new InvalidOperationException("over-enumeration");
                yield return new($"org.test.m{count}", false);
            }
        }
        Reject(CompositionResolver.Resolve([], Entries(), new("1.0.0"), TestContext.Current.CancellationToken),
            "composition_input_limit");
        Assert.Equal(33, count);
    }

    [Fact]
    public void SixtyFourDiagnosticsFitWithoutMarkerAndSixtyFifthReservesTheMarker()
    {
        var enabled = Enumerable.Range(0, 16).Select(i => $"org.test.m{i}").ToArray();
        ModuleDeployment[] Graph(bool extra) => enabled.Select((id, index) => Deploy(id,
            Enumerable.Range(0, extra && index == 9 ? 5 : 4).Select(i => $"org.missing.m{i}").ToArray())).ToArray();
        var exact = Assert.IsType<CompositionResolution.Rejected>(Resolve(Graph(false), enabled));
        Assert.Equal(64, exact.Diagnostics.Count);
        Assert.All(exact.Diagnostics, d => Assert.Equal("composition_dependency_missing", d.Code));
        var overflow = Assert.IsType<CompositionResolution.Rejected>(Resolve(Graph(true), enabled));
        Assert.Equal(exact.Diagnostics.Take(63).Select(d => JsonSerializer.Serialize(d)),
            overflow.Diagnostics.Take(63).Select(d => JsonSerializer.Serialize(d)));
        Assert.Equal("composition_diagnostics_truncated", overflow.Diagnostics[^1].Code);
    }

    [Fact]
    public void GraphAdmitsExactlyOneThousandTwentyFourEdgesButManifestRejectsSixtyFifthPerOwner()
    {
        // 16 * 64 declarations reaches 1,024; +1 must first violate enabled or per-manifest admission.
        // Sixteen present owners allow at most 16 * 15 = 240 distinct nonself edges (120 in an accepted DAG).
        var enabled = Enumerable.Range(0, 16).Select(i => $"org.test.m{i}").ToArray();
        var dependencies = Enumerable.Range(0, 64).Select(i => $"org.missing.m{i}").ToArray();
        var graph = enabled.Select(id => Deploy(id, dependencies)).ToArray();
        var result = Assert.IsType<CompositionResolution.Rejected>(Resolve(graph, enabled));
        Assert.Equal(64, result.Diagnostics.Count);
        Assert.Equal("composition_dependency_missing", result.Diagnostics[0].Code);
        Assert.Equal("composition_diagnostics_truncated", result.Diagnostics[^1].Code);
        var rejected = Reject(Resolve([Deploy("org.test.a", dependencies.Append("org.missing.extra").ToArray())]),
            "composition_manifest_rejected");
        Assert.Equal("collection_limit", rejected.Diagnostics[0].ManifestDiagnostic!.Code);
    }

    [Fact]
    public void CancellationTriggeredDuringInputScanNeverReturnsPartialAcceptance()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        IEnumerable<ModuleDeployment> Input()
        {
            yield return Deploy("org.test.a");
            source.Cancel();
            yield return Deploy("org.test.b");
        }
        Reject(CompositionResolver.Resolve(Input(), [], new("1.0.0"), source.Token), "composition_cancelled");
    }

    [Fact]
    public void SmallerEnclosingMonotonicBudgetIsCheckedAfterEachInputEntry()
    {
        IEnumerable<ModuleDeployment> Input()
        {
            var started = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(started) < TimeSpan.FromMilliseconds(10)) Thread.SpinWait(32);
            yield return Deploy("org.test.a");
        }
        Reject(CompositionResolver.Resolve(Input(), [], new("1.0.0"), TestContext.Current.CancellationToken,
            TimeSpan.FromMilliseconds(1)), "composition_deadline_exceeded");
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(9, 1)]
    public void RelatedIdsHaveAnInclusiveEightMemberBoundary(int members, int omitted)
    {
        var ids = Enumerable.Range(0, members).Select(i => $"org.test.m{i}").ToArray();
        var cycle = ids.Select((id, index) => Deploy(id, [ids[(index + 1) % members]])).ToArray();
        var rejected = Reject(Resolve(cycle, ids), "composition_dependency_cycle");
        Assert.Equal(ids.Take(8), rejected.Diagnostics[0].RelatedIds.Select(id => id.Value));
        Assert.Equal(omitted, rejected.Diagnostics[0].OmittedRelatedCount);
    }

    [Fact]
    public void LargerRequestedBudgetCannotExtendTheTwoSecondResolverMaximum()
    {
        IEnumerable<ModuleDeployment> Input()
        {
            var started = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(started) < TimeSpan.FromMilliseconds(2050))
                Thread.Sleep(10);
            yield return Deploy("org.test.a");
        }
        Reject(CompositionResolver.Resolve(Input(), [], new("1.0.0"), TestContext.Current.CancellationToken,
            TimeSpan.FromSeconds(30)), "composition_deadline_exceeded");
    }

    [Theory]
    [InlineData("capabilities", 9)]
    [InlineData("dependencies", 64)]
    [InlineData("permissions", 128)]
    public void WrapperPreservesAcceptedManifestCollectionLimits(string field, int limit)
    {
        static byte[] Manifest(string field, int count) => field switch
        {
            "capabilities" => Bytes("org.test.a", capabilities: Enumerable.Range(0, count).Select(i =>
                new[] { "backend", "frontend", "resources", "providers", "events", "workflows", "jobs", "permissions", "mcp-tools" }[i % 9]).ToArray()),
            "dependencies" => Bytes("org.test.a", Enumerable.Range(0, count).Select(i => $"org.test.d{i}").ToArray()),
            _ => Bytes("org.test.a", permissions: Enumerable.Range(0, count)
                .Select(i => (object)new { name = $"p{i}", description = "Read." }).ToArray())
        };
        Assert.IsType<CompositionResolution.Accepted>(Resolve([new ModuleDeployment(Manifest(field, limit))]));
        var rejected = Reject(Resolve([new ModuleDeployment(Manifest(field, limit + 1))]), "composition_manifest_rejected");
        Assert.Equal("collection_limit", rejected.Diagnostics[0].ManifestDiagnostic!.Code);
        Assert.Equal("/" + field, rejected.Diagnostics[0].ManifestDiagnostic!.Path);
    }

    [Theory]
    [InlineData("displayName", "display_name_invalid")]
    [InlineData("version", "version_invalid")]
    public void WrapperPreservesAcceptedManifestScalarLimits(string field, string expected)
    {
        byte[] Manifest(int length)
        {
            var root = JsonNode.Parse(Bytes("org.test.a"))!.AsObject();
            root[field] = field == "version" ? "1.0.0+" + new string('a', length - 6) : new string('a', length);
            return JsonSerializer.SerializeToUtf8Bytes(root);
        }
        Assert.IsType<CompositionResolution.Accepted>(Resolve([new ModuleDeployment(Manifest(128))]));
        var rejected = Reject(Resolve([new ModuleDeployment(Manifest(129))]), "composition_manifest_rejected");
        Assert.Equal(expected, rejected.Diagnostics[0].ManifestDiagnostic!.Code);
        Assert.Equal("/" + field, rejected.Diagnostics[0].ManifestDiagnostic!.Path);
    }

    [Theory]
    [InlineData(7, "field_unknown")]
    [InlineData(8, "depth_limit")]
    public void WrapperPreservesAcceptedManifestContainerDepth(int nestedArrays, string expected)
    {
        var manifest = Encoding.UTF8.GetString(Bytes("org.test.a"))[..^1]
            + ",\"unexpected\":" + new string('[', nestedArrays) + "0" + new string(']', nestedArrays) + "}";
        var rejected = Reject(Resolve([new ModuleDeployment(Encoding.UTF8.GetBytes(manifest))]), "composition_manifest_rejected");
        Assert.Equal(expected, rejected.Diagnostics[0].ManifestDiagnostic!.Code);
        Assert.Equal("", rejected.Diagnostics[0].ManifestDiagnostic!.Path);
    }

    [Theory]
    [InlineData("name", 64)]
    [InlineData("description", 256)]
    public void WrapperPreservesAcceptedPermissionScalarLimits(string field, int limit)
    {
        byte[] Manifest(int length) => Bytes("org.test.a", permissions:
            [new { name = field == "name" ? new string('a', length) : "read",
                description = field == "description" ? new string('a', length) : "Read." }]);
        Assert.IsType<CompositionResolution.Accepted>(Resolve([new ModuleDeployment(Manifest(limit))]));
        var rejected = Reject(Resolve([new ModuleDeployment(Manifest(limit + 1))]), "composition_manifest_rejected");
        Assert.Equal("permission_invalid", rejected.Diagnostics[0].ManifestDiagnostic!.Code);
        Assert.Equal("/permissions/0/" + field, rejected.Diagnostics[0].ManifestDiagnostic!.Path);
    }

    [Fact]
    public void WrapperPreservesAcceptedCanonicalIdLength()
    {
        var prefix = new string('a', 63) + "." + new string('b', 63) + "." + new string('c', 63) + ".";
        var accepted = Assert.IsType<CompositionResolution.Accepted>(Resolve([Deploy(prefix + new string('d', 61))]));
        Assert.Equal(253, Assert.Single(accepted.Composition.Deployed).Id.Value.Length);
        var rejected = Reject(Resolve([Deploy(prefix + new string('d', 62))]), "composition_manifest_rejected");
        Assert.Equal("module_id_invalid", rejected.Diagnostics[0].ManifestDiagnostic!.Code);
    }
}
