using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Composition;

public enum CompositionStorageTransition { Entered, Completed, Failed }

public sealed record CompositionStorageProgress(
    ModuleId Owner, CompositionStorageTransition Transition, StorageResult? Result = null);
