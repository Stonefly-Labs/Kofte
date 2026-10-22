using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public sealed class BindingContractTests
{
    [Fact]
    public void MetadataCannotBeConstructedOrUsedToMintAnOwnerBinding()
    {
        Assert.Empty(typeof(StorageAssignment).GetConstructors());
        Assert.All(typeof(StorageAssignment).GetProperties(), property => Assert.Null(property.SetMethod));
        Assert.Empty(typeof(ModuleStorageBinding).GetConstructors());
        Assert.DoesNotContain(typeof(ModuleStorageBinding).GetMethods(),
            method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(StorageAssignment)));
        Assert.All(typeof(IModuleStorage).GetMethods().Where(method => method.Name == "ResolveAsync"),
            method => Assert.Equal([typeof(CancellationToken)], method.GetParameters().Select(parameter => parameter.ParameterType)));
    }
}
