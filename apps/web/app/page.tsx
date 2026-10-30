import { BootstrapStatus } from "../features/bootstrap/BootstrapStatus";

export default function Page() {
  return (
    <main>
      <header>
        <p className="eyebrow">A small start. A solid foundation.</p>
        <h1>Köfte</h1>
        <p className="intro">
          Your local foundation for a modular developer platform. This checkout
          is taking its first steps.
        </p>
      </header>
      <BootstrapStatus />
      <section className="panel" aria-labelledby="scope-heading">
        <h2 id="scope-heading">Nothing installed. Nothing invented.</h2>
        <p>
          This is the repository bootstrap, not a working developer portal. No
          feature modules, teams, services or tools are available yet. There is
          no sample business data to confuse with your own.
        </p>
        <p>
          <a href="/identity">Sign in or verify your identity</a>
        </p>
      </section>
      <footer>
        Local foundation preview. Identity grants no unrelated permissions. This
        is not a production-readiness assessment.
      </footer>
    </main>
  );
}
