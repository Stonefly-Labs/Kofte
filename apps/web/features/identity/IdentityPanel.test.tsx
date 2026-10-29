import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { IdentityPanel } from "./IdentityPanel";
import { startLogin } from "./startLoginAction";

vi.mock("./startLoginAction", () => ({ startLogin: vi.fn() }));

const user = {
  version: "1",
  userId: "12345678-1234-4321-8123-123456789abc",
  displayName: "Synthetic user",
  identityMode: "oidc",
  isInitialAdministrator: false,
};
let current: unknown;
let status: number;
let token: string;
let mode: string;
let fetcher: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.useFakeTimers();
  current = user;
  status = 401;
  token = "fresh-anonymous-token";
  mode = "oidc";
  window.history.replaceState(null, "", "/identity");
  fetcher = vi.fn(async (url: string) =>
    url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : Response.json(current, { status }),
  );
  vi.stubGlobal("fetch", fetcher);
  vi.mocked(startLogin).mockReset();
});

afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

async function settle() {
  await act(() => vi.advanceTimersByTimeAsync(0));
}

it("immediately clears identity during sign-out, then offers legitimate re-entry with a fresh token", async () => {
  status = 200;
  let finish: (value: Response) => void = () => {};
  fetcher.mockImplementation((url: string) => {
    if (url.endsWith("/sign-out"))
      return new Promise<Response>((resolve) => {
        finish = resolve;
      });
    return Promise.resolve(
      url.endsWith("browser-context")
        ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
        : Response.json(current, { status }),
    );
  });
  render(<IdentityPanel />);
  await settle();
  fireEvent.click(screen.getByRole("button", { name: "Sign out" }));
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(screen.queryByLabelText("One-time authorization")).toBeNull();
  expect(document.body.textContent).toContain(
    "Waiting for sign-out confirmation",
  );
  token = "fresh-after-sign-out";
  await act(async () => finish(new Response(null, { status: 204 })));
  await settle();
  expect(document.body.textContent).toContain("Signed out of Köfte");
  expect(screen.getByRole("button", { name: "Sign in" })).toBeTruthy();
  expect(
    document.querySelector<HTMLInputElement>(
      'input[name="__RequestVerificationToken"]',
    )!.value,
  ).toBe(token);
  expect(document.activeElement).toBe(
    screen.getByRole("heading", { name: "Your identity" }),
  );
  const submissions = fetcher.mock.calls.filter(([url]) =>
    url.endsWith("/sign-out"),
  );
  expect(submissions).toHaveLength(1);
  expect(submissions[0][1].body).toBeUndefined();
  expect(submissions[0][1].headers["X-Kofte-CSRF"]).toBe(
    "fresh-anonymous-token",
  );
});

it("keeps local identity cleared after uncertain sign-out, focus and explicit retry without a mutation retry", async () => {
  status = 200;
  fetcher.mockImplementation(async (url: string) => {
    if (url.endsWith("/sign-out"))
      throw new TypeError("private-sign-out-canary");
    return url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : Response.json(current, { status });
  });
  render(<IdentityPanel />);
  await settle();
  fireEvent.click(screen.getByRole("button", { name: "Sign out" }));
  await settle();
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(screen.getByRole("alert").textContent).toContain(
    "Server revocation could not be confirmed",
  );
  expect(document.body.textContent).not.toContain("Signed out of Köfte");
  expect(document.body.textContent).not.toContain("private-sign-out-canary");
  act(() => window.dispatchEvent(new Event("focus")));
  await settle();
  fireEvent.click(screen.getByRole("button", { name: "Retry identity check" }));
  await settle();
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(
    fetcher.mock.calls.filter(([url]) => url.endsWith("/me")),
  ).toHaveLength(1);
  expect(
    fetcher.mock.calls.filter(([url]) => url.endsWith("/sign-out")),
  ).toHaveLength(1);
});

it("bounds sign-out even if transport ignores cancellation and never accepts a late success", async () => {
  status = 200;
  let finish: (value: Response) => void = () => {};
  fetcher.mockImplementation((url: string) =>
    url.endsWith("/sign-out")
      ? new Promise<Response>((resolve) => {
          finish = resolve;
        })
      : Promise.resolve(
          url.endsWith("browser-context")
            ? Response.json({
                version: "1",
                identityMode: mode,
                csrfToken: token,
              })
            : Response.json(current, { status }),
        ),
  );
  render(<IdentityPanel />);
  await settle();
  fireEvent.click(screen.getByRole("button", { name: "Sign out" }));
  await act(() => vi.advanceTimersByTimeAsync(4500));
  expect(screen.getByRole("alert").textContent).toContain(
    "Server revocation could not be confirmed",
  );
  await act(async () => finish(new Response(null, { status: 204 })));
  expect(document.body.textContent).not.toContain("Signed out of Köfte");
  expect(screen.queryByText("Synthetic user")).toBeNull();
});

it("starts with a non-personal, announced shell while verification is pending", () => {
  fetcher.mockImplementation(() => new Promise(() => {}));
  render(<IdentityPanel />);
  expect(screen.getByRole("status").textContent).toContain("Checking");
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(screen.queryByRole("button", { name: "Sign in" })).toBeNull();
});

it("posts only a fresh token and admitted local return to the deliberate login route", async () => {
  const { container } = render(<IdentityPanel />);
  await settle();
  const form = container.querySelector("form")!;
  expect(form.getAttribute("action")).toBe("/auth/login");
  expect(form.getAttribute("method")).toBe("post");
  expect([...new FormData(form).entries()]).toEqual([
    ["returnPath", "/identity"],
    ["__RequestVerificationToken", token],
  ]);
  expect(screen.getByRole("button", { name: "Sign in" })).toBeTruthy();
  for (const [, options] of fetcher.mock.calls) {
    expect(options.cache).toBe("no-store");
    expect(options.redirect).toBe("error");
  }
  expect(fetcher).toHaveBeenCalledTimes(2);
});

it("renders only the verified identity as escaped text, not markup or privileges", async () => {
  status = 200;
  current = { ...user, displayName: "<img src=x onerror=alert(1)>" };
  const { container } = render(<IdentityPanel />);
  await settle();
  expect(screen.getByText("<img src=x onerror=alert(1)>")).toBeTruthy();
  expect(container.querySelector("img")).toBeNull();
  expect(screen.getByText(user.userId)).toBeTruthy();
  expect(screen.queryByText(/Initial administrator/)).toBeNull();
  expect(screen.queryByRole("button", { name: "Sign in" })).toBeNull();
});

it("shows nominated entry only for an ordinary production actor and re-fetches designation after confirmation", async () => {
  status = 200;
  fetcher.mockImplementation(async (url: string) => {
    if (url.endsWith("first-administrator")) {
      current = { ...user, isInitialAdministrator: true };
      return Response.json({
        version: "1",
        status: "enrolled",
        correlationId: "a".repeat(32),
      });
    }
    return url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : Response.json(current, { status });
  });
  const mounted = render(<IdentityPanel />);
  await settle();
  const input = screen.getByLabelText("One-time authorization");
  expect(screen.getAllByRole("status")).toHaveLength(1);
  input.focus();
  fireEvent.change(input, { target: { value: "A".repeat(43) } });
  fireEvent.submit(mounted.container.querySelector("form")!);
  await settle();
  expect(screen.getByText(/Initial administrator designated/)).toBeTruthy();
  expect(screen.queryByLabelText("One-time authorization")).toBeNull();
  expect(document.activeElement).toBe(
    screen.getByRole("heading", { name: "Your identity" }),
  );
  expect(
    fetcher.mock.calls.filter(([url]) => url.endsWith("/me")),
  ).toHaveLength(2);
  expect(
    fetcher.mock.calls.filter(([url]) => url.endsWith("first-administrator")),
  ).toHaveLength(1);
});

it("announces a bounded sign-in refusal without retrying the mutation", async () => {
  vi.mocked(startLogin).mockResolvedValue({
    kind: "uncertain",
    correlationId: "a".repeat(32),
  });
  const { container } = render(<IdentityPanel />);
  await settle();
  fireEvent.submit(container.querySelector("form")!);
  await settle();
  expect(screen.getByRole("alert").textContent).toContain(
    "could not be confirmed",
  );
  expect(screen.getByRole("alert").textContent).toContain("a".repeat(32));
  expect(startLogin).toHaveBeenCalledExactlyOnceWith({
    returnPath: "/identity",
    csrfToken: token,
  });
  expect(fetcher).toHaveBeenCalledTimes(2);
});

it("clears a stale start failure on deliberate verification without resubmitting or losing keyboard focus", async () => {
  vi.mocked(startLogin).mockResolvedValue({
    kind: "failed",
    correlationId: "a".repeat(32),
  });
  const { container } = render(<IdentityPanel />);
  await settle();
  fireEvent.submit(container.querySelector("form")!);
  await settle();
  expect(screen.getByRole("alert").textContent).toContain(
    "could not be started",
  );
  token = "corrected-browser-context";
  const retry = screen.getByRole("button", { name: "Retry identity check" });
  retry.focus();
  fireEvent.click(retry);
  await settle();
  expect(screen.queryByRole("alert")).toBeNull();
  expect(
    document.querySelector<HTMLInputElement>(
      'input[name="__RequestVerificationToken"]',
    )!.value,
  ).toBe(token);
  expect(document.activeElement).toBe(retry);
  expect(startLogin).toHaveBeenCalledTimes(1);
});

it("disables repeated submissions until the user-directed sign-in returns", async () => {
  let finish: (value: { kind: "failed" }) => void = () => {};
  vi.mocked(startLogin).mockImplementation(
    () =>
      new Promise((resolve) => {
        finish = resolve;
      }),
  );
  const { container } = render(<IdentityPanel />);
  await settle();
  fireEvent.submit(container.querySelector("form")!);
  fireEvent.submit(container.querySelector("form")!);
  expect(startLogin).toHaveBeenCalledTimes(1);
  expect(
    screen
      .getByRole("button", { name: "Starting sign-in…" })
      .hasAttribute("disabled"),
  ).toBe(true);
  await act(async () => finish({ kind: "failed" }));
  expect(screen.getByRole("alert").textContent).toContain(
    "could not be started",
  );
});

it("does not synthesize missing fields or show a mode-confused identity", async () => {
  status = 200;
  for (const value of [
    { ...user, userId: "not-an-id" },
    { ...user, displayName: undefined },
    { ...user, identityMode: "development" },
    { ...user, isInitialAdministrator: undefined },
  ]) {
    current = value;
    const mounted = render(<IdentityPanel />);
    await settle();
    expect(screen.getByRole("status").textContent).toContain("unavailable");
    expect(screen.queryByText("Synthetic user")).toBeNull();
    mounted.unmount();
  }
});

it("clears the previous identity on suspension, then announces expiry after fresh denial", async () => {
  status = 200;
  render(<IdentityPanel />);
  await settle();
  expect(screen.getByText("Synthetic user")).toBeTruthy();
  act(() => window.dispatchEvent(new Event("blur")));
  expect(screen.queryByText("Synthetic user")).toBeNull();
  status = 401;
  act(() => window.dispatchEvent(new Event("focus")));
  await settle();
  expect(screen.getByRole("status").textContent).toContain("expired");
  expect(screen.getByRole("button", { name: "Sign in" })).toBeTruthy();
});

it("renews the identity-bound token and never retains personal content after failed verification", async () => {
  status = 200;
  render(<IdentityPanel />);
  await settle();
  token = "fresh-signed-in-token";
  status = 401;
  act(() => window.dispatchEvent(new Event("focus")));
  await settle();
  expect(
    document.querySelector<HTMLInputElement>(
      'input[name="__RequestVerificationToken"]',
    )!.value,
  ).toBe(token);
  fetcher.mockRejectedValue(new TypeError("private-provider-canary"));
  fireEvent.click(screen.getByRole("button", { name: "Retry identity check" }));
  expect(screen.queryByText("Synthetic user")).toBeNull();
  await settle();
  expect(screen.getByRole("status").textContent).toContain("unavailable");
  expect(document.body.textContent).not.toContain("private-provider-canary");
  expect(document.querySelector("form")).toBeNull();
});

it("shows fixed cancellation and failure messages without reflecting arbitrary query content", async () => {
  for (const outcome of ["cancelled", "failed", "unavailable"]) {
    window.history.replaceState(
      null,
      "",
      `/identity?outcome=${outcome}&correlationId=${"b".repeat(32)}&error_description=private-canary`,
    );
    const mounted = render(<IdentityPanel />);
    await settle();
    expect(document.body.textContent).toContain(
      outcome === "cancelled"
        ? "cancelled"
        : outcome === "unavailable"
          ? "Sign-in is unavailable"
          : "could not be completed",
    );
    expect(document.body.textContent).toContain("b".repeat(32));
    expect(document.body.textContent).not.toContain("private-canary");
    expect(
      fetcher.mock.calls.every(([url]) => url.startsWith("/api/identity/v1/")),
    ).toBe(true);
    mounted.unmount();
  }
});

it("refuses unsafe return destinations rather than turning them into a navigation", async () => {
  for (const returnPath of [
    "//evil.invalid",
    "/%2fother",
    "/identity?next=1",
  ]) {
    const mounted = render(<IdentityPanel returnPath={returnPath} />);
    await settle();
    expect(document.querySelector("form")).toBeNull();
    expect(document.body.textContent).not.toContain(returnPath);
    mounted.unmount();
  }
  render(<IdentityPanel returnPath="/" />);
  await settle();
  expect(
    document.querySelector<HTMLInputElement>('input[name="returnPath"]')!.value,
  ).toBe("/");
});

it("bounds verification even when fetch ignores cancellation and refuses late identity results", async () => {
  let finish: (value: Response) => void = () => {};
  fetcher.mockImplementation((url: string) =>
    url.endsWith("browser-context")
      ? Promise.resolve(
          Response.json({ version: "1", identityMode: mode, csrfToken: token }),
        )
      : new Promise<Response>((resolve) => {
          finish = resolve;
        }),
  );
  render(<IdentityPanel />);
  await act(() => vi.advanceTimersByTimeAsync(4500));
  expect(screen.getByRole("status").textContent).toContain("unavailable");
  await act(async () => finish(Response.json(user)));
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(fetcher.mock.calls[0][1].signal.aborted).toBe(true);
});

it("keeps the retry keyboard target focused and announces a user-directed correction", async () => {
  fetcher.mockRejectedValue(new TypeError("transport_failed"));
  render(<IdentityPanel />);
  await settle();
  const retry = screen.getByRole("button", { name: "Retry identity check" });
  retry.focus();
  fetcher.mockImplementation(async (url: string) =>
    url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : Response.json({}, { status: 401 }),
  );
  fireEvent.click(retry);
  await settle();
  expect(document.activeElement).toBe(retry);
  expect(screen.getByRole("status").getAttribute("aria-live")).toBe("polite");
  expect(screen.getByRole("button", { name: "Sign in" })).toBeTruthy();
});

it("labels development mode without offering a production login or implicit account", async () => {
  mode = "development";
  render(<IdentityPanel />);
  await settle();
  expect(screen.getByText("Development identity")).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Sign in" })).toBeNull();
  expect(screen.queryByText("Synthetic user")).toBeNull();
});

it("fully reads the bounded unauthenticated body without cancelling the protected exchange", async () => {
  const cancelled = vi.fn();
  const privateValue = "private-unauthenticated-response";
  let finishBody = () => {};
  const body = new ReadableStream<Uint8Array>({
    start(controller) {
      controller.enqueue(new TextEncoder().encode('{"detail":"'));
      finishBody = () => {
        controller.enqueue(new TextEncoder().encode(privateValue + '"}'));
        controller.close();
      };
    },
    cancel: cancelled,
  });
  fetcher.mockImplementation(async (url: string) =>
    url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : new Response(body, {
          status: 401,
          headers: { "Content-Type": "application/problem+json" },
        }),
  );
  render(<IdentityPanel />);
  await settle();
  expect(screen.getByRole("status").textContent).toContain("Checking");
  await act(async () => finishBody());
  await settle();
  expect(screen.getByRole("status").textContent).toContain(
    "You are signed out",
  );
  expect(cancelled).not.toHaveBeenCalled();
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(document.body.textContent).not.toContain(privateValue);
});

it("fails closed when the unauthenticated body exceeds the existing identity response budget", async () => {
  const privateValue = "private-oversized-unauthenticated-response";
  fetcher.mockImplementation(async (url: string) =>
    url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : Response.json(
          { detail: privateValue + "x".repeat(8192) },
          { status: 401 },
        ),
  );
  render(<IdentityPanel />);
  await settle();
  expect(screen.getByRole("status").textContent).toContain("unavailable");
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(document.body.textContent).not.toContain(privateValue);
  expect(fetcher.mock.calls[0][1].signal.aborted).toBe(true);
});

it("bounds an unauthenticated body that never finishes within the existing verification deadline", async () => {
  fetcher.mockImplementation(async (url: string) =>
    url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : new Response(new ReadableStream<Uint8Array>(), {
          status: 401,
          headers: { "Content-Type": "application/problem+json" },
        }),
  );
  render(<IdentityPanel />);
  await act(() => vi.advanceTimersByTimeAsync(4500));
  expect(screen.getByRole("status").textContent).toContain("unavailable");
  expect(screen.queryByText("Synthetic user")).toBeNull();
  expect(fetcher.mock.calls[0][1].signal.aborted).toBe(true);
});

const developmentIdentities = [
  { key: "synthetic-one", displayName: "Local developer" },
  { key: "synthetic-two", displayName: null },
];

it("requires explicit configured Development selection and verifies fresh identity and CSRF afterward", async () => {
  mode = "development";
  fetcher.mockImplementation(async (url: string) => {
    if (url.endsWith("/development-session")) {
      current = { ...user, identityMode: "development" };
      status = 200;
      token = "fresh-selected-identity-token";
      return Response.json(current);
    }
    return url.endsWith("browser-context")
      ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
      : Response.json(current, { status });
  });
  render(<IdentityPanel developmentIdentities={developmentIdentities} />);
  await settle();
  const select = screen.getByLabelText("Configured Development identity");
  expect(
    screen
      .getByRole("button", { name: "Use Development identity" })
      .hasAttribute("disabled"),
  ).toBe(true);
  fireEvent.change(select, { target: { value: "synthetic-one" } });
  fireEvent.submit(select.closest("form")!);
  await settle();
  expect(screen.getByText("Synthetic user")).toBeTruthy();
  expect(screen.getByText("Development identity")).toBeTruthy();
  const submissions = fetcher.mock.calls.filter(([url]) =>
    url.endsWith("/development-session"),
  );
  expect(submissions).toHaveLength(1);
  expect(JSON.parse(submissions[0][1].body)).toEqual({
    identityKey: "synthetic-one",
  });
  expect(submissions[0][1].headers["X-Kofte-CSRF"]).toBe(
    "fresh-anonymous-token",
  );
  expect(document.activeElement).toBe(
    screen.getByRole("heading", { name: "Your identity" }),
  );
  fireEvent.click(screen.getByRole("button", { name: "Sign out" }));
  await settle();
  const signOut = fetcher.mock.calls.find(([url]) => url.endsWith("/sign-out"));
  expect(signOut?.[1].headers["X-Kofte-CSRF"]).toBe(
    "fresh-selected-identity-token",
  );
});

it.each([
  {
    status: 403,
    code: "identity_forbidden",
    notice: "selection was denied",
  },
  {
    status: 503,
    code: "identity_outcome_uncertain",
    notice: "selection could not be confirmed",
  },
])(
  "keeps the previous Development identity cleared after $status without retrying",
  async (failure) => {
    mode = "development";
    current = { ...user, identityMode: "development" };
    status = 200;
    let finish: (value: Response) => void = () => {};
    fetcher.mockImplementation((url: string) =>
      url.endsWith("/development-session")
        ? new Promise<Response>((resolve) => {
            finish = resolve;
          })
        : Promise.resolve(
            url.endsWith("browser-context")
              ? Response.json({
                  version: "1",
                  identityMode: mode,
                  csrfToken: token,
                })
              : Response.json(current, { status }),
          ),
    );
    render(<IdentityPanel developmentIdentities={developmentIdentities} />);
    await settle();
    const select = screen.getByLabelText("Configured Development identity");
    fireEvent.change(select, { target: { value: "synthetic-two" } });
    fireEvent.submit(select.closest("form")!);
    expect(screen.queryByText("Synthetic user")).toBeNull();
    expect(screen.getByText("Development identity")).toBeTruthy();
    await act(async () =>
      finish(
        Response.json(
          {
            type: `urn:kofte:problem:${failure.code}`,
            status: failure.status,
            code: failure.code,
            correlationId: "d".repeat(32),
          },
          { status: failure.status },
        ),
      ),
    );
    await settle();
    expect(screen.getByRole("alert").textContent).toContain(failure.notice);
    act(() => window.dispatchEvent(new Event("focus")));
    await settle();
    fireEvent.click(
      screen.getByRole("button", { name: "Retry identity check" }),
    );
    await settle();
    expect(screen.queryByText("Synthetic user")).toBeNull();
    expect(
      fetcher.mock.calls.filter(([url]) =>
        url.endsWith("/development-session"),
      ),
    ).toHaveLength(1);
    expect(
      fetcher.mock.calls.filter(([url]) => url.endsWith("/me")),
    ).toHaveLength(1);
  },
);

it("shows only a validated problem reference when a dependency is unavailable", async () => {
  const correlation = "d".repeat(32);
  fetcher.mockResolvedValue(
    Response.json(
      {
        type: "urn:kofte:problem:identity_unavailable",
        status: 503,
        title: "Identity unavailable",
        code: "identity_unavailable",
        correlationId: correlation,
        detail: "private-canary",
      },
      { status: 503 },
    ),
  );
  render(<IdentityPanel />);
  await settle();
  expect(document.body.textContent).toContain(correlation);
  expect(document.body.textContent).not.toContain("private-canary");
});

it("refuses oversized or non-JSON verification instead of retaining a previous identity", async () => {
  for (const response of [
    new Response("private-canary", {
      headers: { "Content-Type": "text/html" },
    }),
    Response.json({ ...user, extra: "x".repeat(8192) }),
  ]) {
    fetcher.mockImplementation(async (url: string) =>
      url.endsWith("browser-context")
        ? Response.json({ version: "1", identityMode: mode, csrfToken: token })
        : response,
    );
    const mounted = render(<IdentityPanel />);
    await settle();
    expect(screen.getByRole("status").textContent).toContain("unavailable");
    expect(document.body.textContent).not.toContain("private-canary");
    expect(screen.queryByText("Synthetic user")).toBeNull();
    mounted.unmount();
  }
});
