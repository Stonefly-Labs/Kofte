using System.Security.Claims;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Microsoft.AspNetCore.Authentication;

namespace Kofte.Host.Features.Identity.Login;

internal sealed record ValidatedLoginResult(AuthenticationTicket Ticket, string ReturnPath);

internal sealed class ValidatedLogin(IdentityStore store, IdentityTicketStore tickets,
    IdentityConfiguration configuration, TimeProvider clock)
{
    internal async Task<ValidatedLoginResult> CompleteOidcAsync(HttpContext context, ClaimsPrincipal principal,
        AuthenticationProperties? properties, string browserDigest, OperationBudget budget)
    {
        var issuers = principal.FindAll("iss").ToArray();
        var subjects = principal.FindAll("sub").ToArray();
        var names = principal.FindAll("name").ToArray();
        if (configuration.Provider is not { } provider || principal.Identity?.IsAuthenticated != true ||
            issuers.Length != 1 || issuers[0].Value != provider.Issuer || subjects.Length != 1 || names.Length > 1)
            throw new IdentityAttemptException();
        var attemptId = LoginAttemptBinding.Read(properties, browserDigest);
        var now = clock.GetUtcNow().UtcDateTime;
        var reference = IdentityTicketStore.NewReference();
        var result = await store.MutateAsync(budget, async change =>
        {
            if (change.Before.Mode != "oidc") throw new StorageException(StorageCode.ReconciliationRequired);
            var user = await IdentityUserStore.ResolveAsync(change, issuers[0].Value, subjects[0].Value,
                names.SingleOrDefault()?.Value, now);
            var attempt = await IdentityLoginAttempts.ConsumeAsync(change, attemptId, browserDigest, now);
            var ticket = IdentityTicketStore.CreateTicket(user, now);
            await IdentitySessionStore.CreateAsync(change, user, reference.Digest, tickets.Protect(ticket), now);
            return new ValidatedLoginResult(ticket, attempt.ReturnPath);
        }, IdentityCommitKind.Login);
        IdentityTicketStore.BindCommitted(context, reference.Reference, result.Ticket);
        await context.SignInAsync(OidcConfiguration.SessionScheme, result.Ticket.Principal, result.Ticket.Properties);
        return result;
    }
}
