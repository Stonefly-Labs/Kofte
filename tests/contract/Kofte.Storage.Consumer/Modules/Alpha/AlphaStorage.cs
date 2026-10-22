using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Storage.Consumer.Modules.Alpha;

public sealed class Record
{
    public int Id { get; set; }
    public string Payload { get; set; } = "";
    public string? Note { get; set; }
}

public abstract class AlphaStorage(DbContextOptions options, ModuleStorageBinding storage)
    : AssignedDbContext(options, storage)
{
    public DbSet<Record> Records => Set<Record>();
    public static AlphaStorage Create(DbContextOptions options, ModuleStorageBinding binding) =>
        binding.Provider == "postgresql" ? new PostgresAlphaStorage(options, binding) : new SqliteAlphaStorage(options, binding);
    protected override void ConfigureModel(ModelBuilder builder) => Describe(builder, Storage.Schema, Storage.Provider, true);

    internal static void Describe(ModelBuilder builder, string? schema, string provider, bool upgraded)
    {
        builder.HasAnnotation("ProductVersion", "10.0.13");
        if (provider == "postgresql")
        {
            builder.HasDefaultSchema(schema);
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy",
                Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
        builder.Entity<Record>(entity =>
        {
            entity.ToTable("Records", schema);
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever()
                .HasColumnType(provider == "postgresql" ? "integer" : "INTEGER");
            entity.Property(record => record.Payload).IsRequired()
                .HasColumnType(provider == "postgresql" ? "text" : "TEXT");
            if (upgraded)
                entity.Property(record => record.Note).HasColumnType(provider == "postgresql" ? "text" : "TEXT");
            else entity.Ignore(record => record.Note);
        });
    }
}
public sealed class SqliteAlphaStorage(DbContextOptions options, ModuleStorageBinding storage) : AlphaStorage(options, storage);
public sealed class PostgresAlphaStorage(DbContextOptions options, ModuleStorageBinding storage) : AlphaStorage(options, storage);
public abstract class Initial : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable("Records", schema: AssignedSchema, columns: table => new
        {
            Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
            Payload = table.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Records", record => record.Id));
    protected override void BuildTargetModel(ModelBuilder builder) => AlphaStorage.Describe(builder, AssignedSchema, AssignedProvider, false);
}
public abstract class AddNote : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) => builder.AddColumn<string>("Note", "Records",
        type: AssignedProvider == "postgresql" ? "text" : "TEXT", schema: AssignedSchema, nullable: true);
    protected override void BuildTargetModel(ModelBuilder builder) => AlphaStorage.Describe(builder, AssignedSchema, AssignedProvider, true);
}
[DbContext(typeof(SqliteAlphaStorage)), Migration("202610210001_Initial")]
public sealed class SqliteInitial : Initial;
[DbContext(typeof(PostgresAlphaStorage)), Migration("202610210001_Initial")]
public sealed class PostgresInitial : Initial;
[DbContext(typeof(SqliteAlphaStorage)), Migration("202610210002_AddNote")]
public sealed class SqliteAddNote : AddNote;
[DbContext(typeof(PostgresAlphaStorage)), Migration("202610210002_AddNote")]
public sealed class PostgresAddNote : AddNote;
public abstract class Snapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) => AlphaStorage.Describe(builder, AssignedSchema, AssignedProvider, true);
}
[DbContext(typeof(SqliteAlphaStorage))]
public sealed class SqliteSnapshot : Snapshot;
[DbContext(typeof(PostgresAlphaStorage))]
public sealed class PostgresSnapshot : Snapshot;
