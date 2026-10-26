using Kofte.Platform.Identity;

namespace Kofte.Identity.Consumer;

public static class ActorProbe
{
    public static AuthenticatedActorV1? Observe(ICurrentActorV1 current) => current.Actor;
}
