import { IdentityPanel } from "../../features/identity/IdentityPanel";
import { randomBytes } from "node:crypto";
import {
  identityConfiguration,
  type DevelopmentIdentityChoice,
} from "../../features/identity/identityConfiguration";

export const dynamic = "force-dynamic";

export default async function IdentityPage({
  searchParams,
}: {
  searchParams: Promise<{ returnPath?: string | string[] }>;
}) {
  const query = await searchParams;
  let developmentIdentities: readonly DevelopmentIdentityChoice[] | undefined;
  try {
    const configuration = await identityConfiguration();
    developmentIdentities = configuration.developmentIdentities;
  } catch (error) {
    if (!(error instanceof TypeError)) throw error;
    const correlationId = randomBytes(16).toString("hex");
    console.error(
      JSON.stringify({
        eventCode: "identity_unavailable",
        component: "identity_page",
        correlationId,
      }),
    );
    return (
      <main>
        <h1>Köfte</h1>
        <p role="alert">
          Identity configuration is unavailable. Correct the configuration and
          reload this page.
        </p>
        <p className="correlation">Reference: {correlationId}</p>
      </main>
    );
  }
  const returnPath =
    query.returnPath === undefined
      ? "/identity"
      : query.returnPath === "/" || query.returnPath === "/identity"
        ? query.returnPath
        : "";
  return (
    <main>
      <header>
        <p className="eyebrow">Your platform, your identity.</p>
        <h1>Köfte</h1>
        <p className="intro">Sign in and verify your current local identity.</p>
      </header>
      <IdentityPanel
        returnPath={returnPath}
        developmentIdentities={developmentIdentities}
      />
      <footer>
        <a href="/">Return to the local foundation</a>
      </footer>
    </main>
  );
}
