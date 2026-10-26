using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Tests.Modules.Composition;

public sealed class CompositionLifecycleContractTests
{
    private const string Attempt = "0123456789abcdef0123456789abcdef";
    private static ModuleId Id(string value)
    {
        Assert.True(ModuleId.TryParse(value, out var id));
        return id;
    }
    private static ModuleConfiguration Configuration(ModuleId owner) => new(owner, "{}"u8);

    [Theory]
    [InlineData(65)]
    [InlineData(1024)]
    public void ActivationCopiesEveryBoundedPrerequisiteServiceAndRejectsTheFirstOverflow(int count)
    {
        var owner = Id("org.test.dependent");
        Assert.True(SemanticVersion.TryParse("1.0.0", out var version));
        var bindings = Enumerable.Range(0, count).Select(index =>
            new ModuleDependencyBinding(Id($"org.test.root{index / 128}"), $"service{index}", version, new object())).ToList();
        var expected = bindings.ToArray();
        var activated = new Activation(owner, Configuration(owner), new Resources(owner), bindings);
        bindings.Clear();
        Assert.Equal(expected, activated.Dependencies);
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<ModuleDependencyBinding>>(activated.Dependencies).Clear());
        var enumerated = 0;
        Assert.Throws<ArgumentException>(() => new Activation(owner, Configuration(owner), new Resources(owner), Overflow()));
        Assert.Equal(1025, enumerated);

        IEnumerable<ModuleDependencyBinding> Overflow()
        {
            while (true)
            {
                enumerated++;
                yield return expected[0];
            }
        }
    }

    [Theory]
    [InlineData(128, false, true)]
    [InlineData(129, false, false)]
    [InlineData(128, true, true)]
    [InlineData(129, true, false)]
    public void DependencyBindingsPreserveBoundedLocalAndOwnerQualifiedIdentities(
        int localLength, bool qualified, bool valid)
    {
        var owner = Id(string.Join(".", new string('a', 63), new string('b', 63),
            new string('c', 63), new string('d', 61)));
        var id = (qualified ? owner.Value + "." : "") + new string('s', localLength);
        Assert.True(SemanticVersion.TryParse("1.0.0", out var version));
        var operations = new object();
        if (!valid)
        {
            Assert.Throws<ArgumentException>(() => new ModuleDependencyBinding(owner, id, version, operations));
            return;
        }
        var binding = new ModuleDependencyBinding(owner, id, version, operations);
        Assert.Equal(id, binding.ContractId);
        Assert.Same(owner, binding.Owner);
        Assert.Same(operations, binding.Operations);
        Assert.Equal(1, binding.ContractVersion);
        if (qualified)
            Assert.Throws<ArgumentException>(() =>
                new ModuleDependencyBinding(Id("org.test.other"), id, version, operations));
    }

    [Fact]
    public void IssuedContextsEnforceSameOwnerAndInitializeCarriesNoStoreOrDependencies()
    {
        var owner = Id("org.test.a");
        var other = Id("org.test.b");
        var resources = new Resources(owner);
        var context = new Initialization(owner, Configuration(owner), resources);
        Assert.Same(resources, context.Resources);
        Assert.Equal(Attempt, context.AttemptId);
        Assert.Throws<ArgumentException>(() => new Initialization(owner, Configuration(other), resources));
        Assert.Throws<ArgumentException>(() => new Initialization(owner, Configuration(owner), new Resources(other)));
        Assert.Null(typeof(ModuleInitializationContext).GetProperty("Storage"));
        Assert.Null(typeof(ModuleInitializationContext).GetProperty("Dependencies"));
        var bindings = new List<ModuleDependencyBinding>();
        var activated = new Activation(owner, Configuration(owner), resources, bindings);
        Assert.Null(activated.Storage);
        Assert.Empty(activated.Dependencies);
        Assert.True(SemanticVersion.TryParse("1.0.0", out var version));
        bindings.Add(new(other, "catalog.read", version, new object()));
        Assert.Empty(activated.Dependencies);
    }

    [Fact]
    public void ImmutableObservationsRejectAnotherAttemptAndBoundCollectionsBeforeCopy()
    {
        var owner = Id("org.test.a");
        var affected = new List<ModuleId> { Id("org.test.b") };
        var observation = new ModuleObservation(owner, true, ModuleLifecycleStage.Health,
            ModuleLifecycleOutcome.Completed, ModuleHealth.Healthy, ModuleHealth.Failed,
            Attempt, TimeSpan.FromSeconds(1), affected, ModuleObservationCode.DependencyUnavailable);
        affected.Clear();
        Assert.Single(observation.AffectedPrerequisites);
        var observations = new List<ModuleObservation> { observation };
        var snapshot = new CompositionSnapshot(Attempt, CompositionAvailability.Unavailable, false,
            TimeSpan.FromSeconds(2), observations);
        observations.Clear();
        Assert.Single(snapshot.Modules);
        Assert.Throws<ArgumentException>(() => new CompositionSnapshot(new string('a', 32),
            CompositionAvailability.Ready, true, TimeSpan.FromSeconds(2), [observation]));
        Assert.Throws<ArgumentException>(() => new CompositionSnapshot(Attempt,
            CompositionAvailability.Ready, true, TimeSpan.Zero, [observation]));
        Assert.Throws<ArgumentException>(() => new CompositionSnapshot(Attempt,
            CompositionAvailability.Ready, true, TimeSpan.FromSeconds(2), Enumerable.Repeat(observation, 33)));
    }

    private sealed class Resources(ModuleId owner) : ModuleAttemptResources(owner)
    {
        public override void RegisterCleanup(Func<CancellationToken, ValueTask> cleanup) =>
            throw new NotSupportedException("The host owns resource registration.");
    }
    private sealed class Initialization(ModuleId owner, ModuleConfiguration configuration, ModuleAttemptResources resources)
        : ModuleInitializationContext(owner, configuration, Attempt, resources);
    private sealed class Activation(ModuleId owner, ModuleConfiguration configuration, ModuleAttemptResources resources,
        IEnumerable<ModuleDependencyBinding> bindings)
        : ModuleActivationContext(owner, configuration, Attempt, resources, null, bindings, []);
}
