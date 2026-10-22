using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class MigrationBoundaryValidator
{
    internal static void ValidateModel(IModel model, ModuleStorageBinding binding)
    {
        Schema(model.GetDefaultSchema(), binding);
        if (model.GetDbFunctions().Any()) Reject();
        ProviderAnnotations(model);
        foreach (var entity in model.GetEntityTypes())
        {
            ProviderAnnotations(entity);
            if (entity.GetDeclaredTriggers().Any() || entity.GetCheckConstraints().Any()) Reject();
            if (entity.GetTableName() is { } table)
            { Name(table); Schema(entity.GetSchema(), binding); }
            if (entity.GetViewName() is { } view)
            { Name(view); Schema(entity.GetViewSchema(), binding); }
            if (entity.GetSqlQuery() is not null || entity.GetFunctionName() is not null)
                Reject();
            foreach (var property in entity.GetProperties())
            {
                ProviderAnnotations(property);
                if (property.GetComputedColumnSql() is not null || property.GetDefaultValueSql() is not null)
                    Reject();
            }
            foreach (var fragment in entity.GetMappingFragments())
            {
                Name(fragment.StoreObject.Name);
                Schema(fragment.StoreObject.Schema, binding);
            }
            foreach (var foreignKey in entity.GetForeignKeys())
            {
                Schema(foreignKey.PrincipalEntityType.GetSchema(), binding);
                Name(foreignKey.PrincipalEntityType.GetTableName());
            }
        }
        foreach (var sequence in model.GetSequences())
        { Name(sequence.Name); Schema(sequence.Schema, binding); }
    }

    internal static IReadOnlyList<string> Preflight(AssignedDbContext context,
        IReadOnlyList<string> applied, string? target)
    {
        var assembly = context.GetService<IMigrationsAssembly>();
        var ids = assembly.Migrations.Keys.ToArray();
        if (!applied.SequenceEqual(ids.Take(applied.Count), StringComparer.Ordinal))
            Reject();
        var targetId = target is null ? ids.LastOrDefault() : assembly.FindMigrationId(target);
        if (targetId is null || targetId.Length > 150 || targetId.Any(c => c is < ' ' or > '~'))
            Reject();
        var count = Array.IndexOf(ids, targetId) + 1;
        if (count < applied.Count) Reject();
        if (assembly.ModelSnapshot is not AssignedModelSnapshot snapshot) Reject();
        else ValidateModel(snapshot.Model, context.Storage);
        var pending = ids.Take(count).Skip(applied.Count).ToArray();
        foreach (var id in pending)
        {
            var migration = assembly.CreateMigration(assembly.Migrations[id], context.Database.ProviderName!);
            ValidateModel(migration.TargetModel, context.Storage);
            foreach (var operation in migration.UpOperations)
                ValidateOperation(operation, context.Storage);
            // SQL generation is also part of preflight: reject provider-generated
            // transaction suppression before EF executes even the first migration.
            var commands = context.GetService<IMigrationsSqlGenerator>()
                .Generate(migration.UpOperations, migration.TargetModel);
            if (commands.Any(command => command.TransactionSuppressed)) Reject();
        }
        return ids.Take(count).ToArray();
    }

    internal static void ValidateOperation(MigrationOperation operation, ModuleStorageBinding binding)
    {
        if (operation.GetAnnotations().Any()) Reject();
        switch (operation)
        {
            case CreateTableOperation table:
                Name(table.Name); Schema(table.Schema, binding);
                if (table.Comment is not null || table.CheckConstraints.Count != 0 ||
                    table.UniqueConstraints.Count != 0 || table.ForeignKeys.Count != 0) Reject();
                foreach (var column in table.Columns)
                {
                    if (column.Table != table.Name || column.Schema != table.Schema) Reject();
                    ValidateOperation(column, binding);
                }
                if (table.PrimaryKey is { } primary)
                {
                    if (primary.Table != table.Name || primary.Schema != table.Schema) Reject();
                    ValidateConstraint(primary, binding);
                }
                break;
            case AddColumnOperation column:
                Name(column.Name); Name(column.Table); Schema(column.Schema, binding);
                if (column.ComputedColumnSql is not null || column.DefaultValueSql is not null ||
                    column.Collation is not null || column.Comment is not null) Reject();
                var expected = column.ClrType == typeof(int)
                    ? binding.Provider == "postgresql" ? "integer" : "INTEGER"
                    : column.ClrType == typeof(string)
                        ? binding.Provider == "postgresql" ? "text" : "TEXT" : null;
                if (expected is null || column.ColumnType != expected) Reject();
                break;
            default:
                Reject();
                break;
        }
    }

    private static void ValidateConstraint(MigrationOperation operation, ModuleStorageBinding binding)
    {
        if (operation.GetAnnotations().Any()) Reject();
        switch (operation)
        {
            case AddPrimaryKeyOperation primary:
                Name(primary.Name); Name(primary.Table); Schema(primary.Schema, binding);
                foreach (var column in primary.Columns) Name(column);
                break;
            default: Reject(); break;
        }
    }

    private static void Schema(string? schema, ModuleStorageBinding binding)
    {
        if (!string.Equals(schema, binding.Schema, StringComparison.Ordinal)) Reject();
    }

    private static void ProviderAnnotations(IReadOnlyAnnotatable value)
    {
        foreach (var annotation in value.GetAnnotations())
        {
            if (annotation.Name.StartsWith("Npgsql:", StringComparison.Ordinal) &&
                annotation.Name != "Npgsql:ValueGenerationStrategy") Reject();
            if (annotation.Name.StartsWith("Sqlite:", StringComparison.Ordinal) &&
                annotation.Name != "Sqlite:Autoincrement") Reject();
        }
    }

    private static void Name(string? name)
    {
        if (name is null || name.Length is < 1 or > 63 ||
            name.StartsWith("__kofte", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("__EF", StringComparison.OrdinalIgnoreCase) ||
            name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_'))) Reject();
    }

    private static void Reject() => throw new StorageException(StorageCode.RequestInvalid);
}
