namespace Kofte.Platform.Identity;

public enum IdentityMode
{
    Oidc = 1,
    Development = 2
}

public sealed record AuthenticatedActorV1
{
    public AuthenticatedActorV1(Guid userId, IdentityMode identityMode, bool isInitialAdministrator)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A local user ID is required.", nameof(userId));
        if (identityMode is not (IdentityMode.Oidc or IdentityMode.Development))
            throw new ArgumentOutOfRangeException(nameof(identityMode));
        UserId = userId;
        IdentityMode = identityMode;
        IsInitialAdministrator = isInitialAdministrator;
    }

    public Guid UserId { get; }
    public IdentityMode IdentityMode { get; }
    public bool IsInitialAdministrator { get; }
}

public interface ICurrentActorV1
{
    AuthenticatedActorV1? Actor { get; }
}
