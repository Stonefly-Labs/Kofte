import { IdentityPanel } from "../../features/identity/IdentityPanel";

export const dynamic = "force-dynamic";

export default async function IdentityPage({
  searchParams,
}: {
  searchParams: Promise<{ returnPath?: string | string[] }>;
}) {
  const query = await searchParams;
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
      <IdentityPanel returnPath={returnPath} />
      <footer>
        <a href="/">Return to the local foundation</a>
      </footer>
    </main>
  );
}
