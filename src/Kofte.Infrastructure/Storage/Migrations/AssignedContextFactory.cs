using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class AssignedContextFactory
{
    internal static async Task<TContext> CreateAsync<TContext>(ModuleStorageBinding binding,
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory, CancellationToken token)
        where TContext : AssignedDbContext
    {
        ArgumentNullException.ThrowIfNull(factory);
        var connection = await binding.OpenAsync(token);
        TContext? context = null;
        try
        {
            var options = new DbContextOptionsBuilder();
            if (binding.Provider == "postgresql")
                options.UseNpgsql(connection, contextOwnsConnection: true, configure =>
                    configure.MigrationsHistoryTable("__EFMigrationsHistory", binding.Schema)
                        .CommandTimeout(3));
            else
                options.UseSqlite(connection, contextOwnsConnection: true, configure =>
                    configure.MigrationsHistoryTable("__EFMigrationsHistory").CommandTimeout(1));
            options.ReplaceService<IModelCacheKeyFactory, AssignedModelCacheKeyFactory>()
                .ReplaceService<IMigrationsAssembly, AssignedMigrationsAssembly>()
                .AddInterceptors(new AssignedCommandInterceptor(binding));
            context = factory(options.Options, binding);
            if (!ReferenceEquals(context.Storage, binding) ||
                !ReferenceEquals(context.Database.GetDbConnection(), connection) ||
                context.GetService<IMigrationsAssembly>() is not AssignedMigrationsAssembly ||
                context.GetService<IModelCacheKeyFactory>() is not AssignedModelCacheKeyFactory)
                throw new StorageException(StorageCode.OwnerMismatch);
            MigrationBoundaryValidator.ValidateModel(context.GetService<IDesignTimeModel>().Model, binding);
            return context;
        }
        catch
        {
            if (context is not null) await context.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }
}
