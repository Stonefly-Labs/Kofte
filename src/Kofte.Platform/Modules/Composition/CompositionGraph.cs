using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Composition;

internal static class CompositionGraph
{
    internal static (IReadOnlyList<ModuleId> Order, IReadOnlyList<CompositionDependency> Dependencies) Resolve(
        IEnumerable<ModuleDescription> deployed, IEnumerable<ModuleDescription> enabled,
        List<CompositionDiagnostic> errors, ResolutionWork work)
    {
        var all = deployed.ToDictionary(d => d.Id.Value, StringComparer.Ordinal);
        var active = enabled.ToDictionary(d => d.Id.Value, StringComparer.Ordinal);
        var edges = new List<CompositionDependency>();
        var adjacency = active.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var module in active.Values.OrderBy(d => d.Id.Value, StringComparer.Ordinal))
            foreach (var dependency in module.Dependencies.OrderBy(d => d.Id.Value, StringComparer.Ordinal))
            {
                work.Check();
                if (edges.Count == 1024) throw new ResolutionRefused("composition_input_limit");
                edges.Add(new(module.Id, dependency.Id, dependency.VersionRange));
                var code = !all.TryGetValue(dependency.Id.Value, out var implementation) ? "composition_dependency_missing"
                    : !active.ContainsKey(dependency.Id.Value) ? "composition_dependency_disabled"
                    : !dependency.VersionRange.Contains(implementation.Version) ? "composition_dependency_incompatible" : null;
                if (code is not null)
                    errors.Add(new(code, CompositionPhase.Dependency, module.Id, "dependencies", related: [dependency.Id]));
                else adjacency[module.Id.Value].Add(dependency.Id.Value);
            }
        if (errors.Count != 0) return ([], edges);

        // Tarjan visits each vertex/edge once; a dependent outside the SCC is not part of its cycle.
        var next = 0;
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lows = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        var stacked = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string id)
        {
            work.Check();
            indices[id] = lows[id] = next++;
            stack.Push(id);
            stacked.Add(id);
            foreach (var prerequisite in adjacency[id])
            {
                if (!indices.ContainsKey(prerequisite))
                {
                    Visit(prerequisite);
                    lows[id] = Math.Min(lows[id], lows[prerequisite]);
                }
                else if (stacked.Contains(prerequisite)) lows[id] = Math.Min(lows[id], indices[prerequisite]);
            }
            if (lows[id] != indices[id]) return;
            var component = new List<ModuleId>();
            string member;
            do
            {
                member = stack.Pop();
                stacked.Remove(member);
                component.Add(active[member].Id);
            } while (member != id);
            if (component.Count > 1)
            {
                component.Sort((a, b) => string.CompareOrdinal(a.Value, b.Value));
                errors.Add(new("composition_dependency_cycle", CompositionPhase.Dependency, component[0],
                    "dependencies", related: component));
            }
        }
        foreach (var id in active.Keys.Order(StringComparer.Ordinal))
            if (!indices.ContainsKey(id)) Visit(id);
        if (errors.Count != 0) return ([], edges);
        var remaining = adjacency.ToDictionary(e => e.Key, e => e.Value.Count, StringComparer.Ordinal);
        var dependents = active.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in edges) dependents[edge.Prerequisite.Value].Add(edge.Owner.Value);
        var ready = new SortedSet<string>(remaining.Where(e => e.Value == 0).Select(e => e.Key), StringComparer.Ordinal);
        var order = new List<ModuleId>();
        while (ready.Count > 0)
        {
            work.Check();
            var id = ready.Min!;
            ready.Remove(id);
            order.Add(active[id].Id);
            foreach (var dependent in dependents[id]) if (--remaining[dependent] == 0) ready.Add(dependent);
        }
        return (order.AsReadOnly(), edges.AsReadOnly());
    }
}
