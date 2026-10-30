using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityRestoreJournalTests : IdentityRestoreTests
{
    [Theory]
    [InlineData("restore_intent_partial")]
    [InlineData("restore_keys_partial")]
    [InlineData("restore_result_partial")]
    [InlineData("restore_intent_empty")]
    public Task ActualRestoreCommandResumesARealPartiallyFlushedJournal(string phase) =>
        RestoreCommandAsync(phase);

    [Theory]
    [InlineData("unknown")]
    [InlineData("binding")]
    [InlineData("invalid")]
    public Task ActualRestoreCommandNeverDiscardsACompleteConflictingOrInvalidJournal(string fault) =>
        RestoreCommandAsync("restore_state_written", invalidPending: fault);
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteIdentityRestoreJournalTests : IdentityRestoreJournalTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresIdentityRestoreJournalTests : IdentityRestoreJournalTests
{
    protected override string Provider => "postgres";
}
