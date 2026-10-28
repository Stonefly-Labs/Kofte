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
      outcome === "cancelled" ? "cancelled" : "could not be completed",
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
