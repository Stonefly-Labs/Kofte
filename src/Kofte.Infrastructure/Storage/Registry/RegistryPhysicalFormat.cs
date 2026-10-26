using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Infrastructure.Storage.Registry;

internal static class RegistryPhysicalFormat
{
    // Frozen native-catalog fingerprints include definitions, not installation data or object OIDs.
    private static readonly IReadOnlyDictionary<(string Provider, int Version), string> Digests =
        new Dictionary<(string, int), string>
        {
            [("sqlite", 1)] = "4ab7a665924f8fde963cc508101a7fcc1751ca36f80d9c5d5987e9dda0b5347f",
            [("sqlite", 2)] = "74a91a561694f987a6f47c805616443441f2b19922b1c3faf813259c9ea5c3e8",
            [("postgresql", 1)] = "8d66bb5df94fd4d50aef4e1d25cded9d2cd79a4f8ebdc26ce1432ae4360ecc97",
            [("postgresql", 2)] = "731227e24b428f67c43dcae6f9d6d9edc8dfe06d76cfde8c6a3d1e3c0dd2e1dc"
        };

    internal static async Task VerifyAsync(DbConnection connection, string provider, int version,
        OperationBudget budget, DbTransaction? transaction = null)
    {
        if (!Digests.TryGetValue((provider, version), out var expected) ||
            await FingerprintAsync(connection, budget, transaction) != expected)
            throw new StorageException(StorageCode.Corrupt);
    }

    internal static async Task<string> FingerprintAsync(DbConnection connection, OperationBudget budget,
        DbTransaction? transaction = null)
    {
        var sql = connection is SqliteConnection ? """
            SELECT type,name,tbl_name,sql FROM sqlite_master ORDER BY type,name
            """ : """
            SELECT jsonb_build_object(
              'relations', (SELECT jsonb_agg(jsonb_build_array(c.relname,c.relkind,c.relpersistence,
                c.relrowsecurity,c.relforcerowsecurity,c.relispartition,c.reloptions,a.amname) ORDER BY c.relname)
                FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                LEFT JOIN pg_catalog.pg_am a ON a.oid=c.relam WHERE n.nspname='kofte_system'),
              'columns', (SELECT jsonb_agg(jsonb_build_array(c.relname,a.attnum,a.attname,
                pg_catalog.format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity,a.attgenerated,
                a.attcollation::regcollation::text,a.attstorage,a.attcompression,a.attisdropped,
                pg_catalog.pg_get_expr(d.adbin,d.adrelid)) ORDER BY c.relname,a.attnum)
                FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_class c ON c.oid=a.attrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
                WHERE n.nspname='kofte_system' AND a.attnum>0),
              'constraints', (SELECT jsonb_agg(jsonb_build_array(c.relname,k.conname,k.contype,
                pg_catalog.pg_get_constraintdef(k.oid,false),k.condeferrable,k.condeferred,k.convalidated,k.connoinherit)
                ORDER BY c.relname,k.conname)
                FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_namespace n ON n.oid=k.connamespace
                LEFT JOIN pg_catalog.pg_class c ON c.oid=k.conrelid WHERE n.nspname='kofte_system'),
              'indexes', (SELECT jsonb_agg(jsonb_build_array(c.relname,x.relname,
                pg_catalog.pg_get_indexdef(i.indexrelid),i.indisvalid,i.indisready,i.indislive,i.indisreplident)
                ORDER BY c.relname,x.relname)
                FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class c ON c.oid=i.indrelid
                JOIN pg_catalog.pg_class x ON x.oid=i.indexrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='kofte_system'),
              'triggers', (SELECT jsonb_agg(jsonb_build_array(c.relname,
                CASE WHEN t.tgisinternal THEN k.conname ELSE t.tgname END,t.tgtype,t.tgenabled,
                t.tgdeferrable,t.tginitdeferred,p.proname,
                CASE WHEN t.tgisinternal THEN NULL ELSE pg_catalog.pg_get_triggerdef(t.oid,false) END)
                ORDER BY c.relname,CASE WHEN t.tgisinternal THEN k.conname ELSE t.tgname END,t.tgtype,p.proname)
                FROM pg_catalog.pg_trigger t JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                JOIN pg_catalog.pg_proc p ON p.oid=t.tgfoid
                LEFT JOIN pg_catalog.pg_constraint k ON k.oid=t.tgconstraint WHERE n.nspname='kofte_system'),
              'routines', (SELECT jsonb_agg(jsonb_build_array(p.proname,p.prokind,
                CASE WHEN p.prokind IN ('f','p') THEN pg_catalog.pg_get_functiondef(p.oid) ELSE NULL END,
                p.provolatile,p.proisstrict,p.prosecdef,
                p.proleakproof,p.proparallel,p.proconfig) ORDER BY p.proname,pg_catalog.pg_get_function_identity_arguments(p.oid))
                FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='kofte_system'),
              'types', (SELECT jsonb_agg(jsonb_build_array(t.typname,t.typtype,t.typcategory,t.typnotnull,
                t.typdefault,t.typcollation::regcollation::text,e.typname) ORDER BY t.typname)
                FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
                LEFT JOIN pg_catalog.pg_type e ON e.oid=t.typelem WHERE n.nspname='kofte_system'),
              'rules', (SELECT jsonb_agg(pg_catalog.pg_get_ruledef(r.oid,false) ORDER BY c.relname,r.rulename)
                FROM pg_catalog.pg_rewrite r JOIN pg_catalog.pg_class c ON c.oid=r.ev_class
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='kofte_system'),
              'policies', (SELECT jsonb_agg(to_jsonb(p) ORDER BY p.tablename,p.policyname)
                FROM pg_catalog.pg_policies p WHERE p.schemaname='kofte_system'))::text
            """;
        await using var command = RegistryMigrationRunner.Command(connection, sql, budget, transaction);
        await using var reader = await command.ExecuteReaderAsync(budget.Token);
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            while (await reader.ReadAsync(budget.Token))
            {
                writer.Write(reader.FieldCount);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    var present = !reader.IsDBNull(index);
                    writer.Write(present);
                    if (present) writer.Write(reader.GetString(index));
                }
            }
        }
        budget.Check();
        return Convert.ToHexStringLower(SHA256.HashData(buffer.ToArray()));
    }
}
