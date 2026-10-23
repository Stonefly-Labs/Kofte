using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kofte.Infrastructure.Storage.Migrations;

public sealed class AssignedModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => context is AssignedDbContext assigned
        ? (context.GetType(), assigned.Storage.Provider, assigned.Storage.Schema, designTime)
        : throw new InvalidOperationException("storage_context_unbound");
}
