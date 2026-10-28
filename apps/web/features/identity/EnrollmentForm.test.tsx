import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { EnrollmentForm } from "./EnrollmentForm";

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function form(onRefresh = vi.fn()) {
  const mounted = render(
    <EnrollmentForm csrfToken="signed-in-token" onRefresh={onRefresh} />,
  );
  const input = screen.getByLabelText<HTMLInputElement>(
    "One-time authorization",
  );
  return {
    ...mounted,
    input,
    onRefresh,
    form: mounted.container.querySelector("form")!,
  };
}

it("keeps the explicit secret only in a masked input and clears it before the necessary protected exchange", async () => {
  const receipt = {
    version: "1",
    status: "enrolled",
    correlationId: "a".repeat(32),
  };
  const fetcher = vi.fn().mockResolvedValue(Response.json(receipt));
  vi.stubGlobal("fetch", fetcher);
  const view = form();
  expect(view.input.type).toBe("password");
  expect(view.input.value).toBe("");
  expect(view.input.autocomplete).toBe("off");
  fireEvent.change(view.input, { target: { value: "A".repeat(43) } });
  fireEvent.submit(view.form);
  expect(view.input.value).toBe("");
  await act(async () => {});
  expect(fetcher).toHaveBeenCalledTimes(1);
  const [url, options] = fetcher.mock.calls[0];
  expect(url).toBe("/api/identity/v1/first-administrator");
  expect(options.method).toBe("POST");
  expect(options.headers["X-Kofte-CSRF"]).toBe("signed-in-token");
  expect(options.body).toBe(JSON.stringify({ authorization: "A".repeat(43) }));
  expect(options.cache).toBe("no-store");
  expect(view.container.innerHTML).not.toContain("A".repeat(43));
  expect(view.onRefresh).toHaveBeenCalledExactlyOnceWith({
    kind: "enrolled",
    correlationId: "a".repeat(32),
  });
});

it("announces refusal without retaining the secret, discovering nominees or retrying", async () => {
  const fetcher = vi.fn().mockResolvedValue(
    Response.json(
      {
        type: "urn:kofte:problem:identity_enrollment_unavailable",
        status: 409,
        code: "identity_enrollment_unavailable",
        correlationId: "b".repeat(32),
      },
      { status: 409 },
    ),
  );
  vi.stubGlobal("fetch", fetcher);
  const view = form();
  fireEvent.change(view.input, { target: { value: "A".repeat(43) } });
  fireEvent.submit(view.form);
  await act(async () => {});
  expect(screen.getByRole("alert").textContent).toContain("not completed");
  expect(screen.getByRole("alert").textContent).toContain("b".repeat(32));
  expect(view.input.value).toBe("");
  expect(document.activeElement).toBe(view.input);
  expect(view.onRefresh).not.toHaveBeenCalled();
  expect(fetcher).toHaveBeenCalledTimes(1);
});

it("allows only one submission in flight and requests fresh identity after a lost reply", async () => {
  let reject: (error: Error) => void = () => {};
  const fetcher = vi.fn(
    () =>
      new Promise((_resolve, fail) => {
        reject = fail;
      }),
  );
  vi.stubGlobal("fetch", fetcher);
  const view = form();
  fireEvent.change(view.input, { target: { value: "A".repeat(43) } });
  fireEvent.submit(view.form);
  fireEvent.submit(view.form);
  expect(
    screen
      .getByRole("button", { name: "Confirming enrollment…" })
      .hasAttribute("disabled"),
  ).toBe(true);
  expect(screen.getByRole("status").textContent).toContain("once");
  await act(async () => reject(new TypeError("private synthetic exchange")));
  expect(view.onRefresh).toHaveBeenCalledExactlyOnceWith({ kind: "uncertain" });
  expect(fetcher).toHaveBeenCalledTimes(1);
  expect(view.input.value).toBe("");
  expect(view.container.textContent).not.toContain(
    "private synthetic exchange",
  );
});

it("never treats malformed success as a known designation", async () => {
  const fetcher = vi
    .fn()
    .mockResolvedValue(Response.json({ version: "1", status: "enrolled" }));
  vi.stubGlobal("fetch", fetcher);
  const view = form();
  fireEvent.change(view.input, { target: { value: "A".repeat(43) } });
  fireEvent.submit(view.form);
  await act(async () => {});
  expect(view.onRefresh).toHaveBeenCalledExactlyOnceWith({ kind: "uncertain" });
  expect(fetcher).toHaveBeenCalledTimes(1);
});

it("aborts a suspended form without letting its late reply affect another identity", async () => {
  let finish: (value: Response) => void = () => {};
  const fetcher = vi.fn<
    (url: string, options: RequestInit) => Promise<Response>
  >(
    () =>
      new Promise<Response>((resolve) => {
        finish = resolve;
      }),
  );
  vi.stubGlobal("fetch", fetcher);
  const view = form();
  fireEvent.change(view.input, { target: { value: "A".repeat(43) } });
  fireEvent.submit(view.form);
  view.unmount();
  expect(fetcher.mock.calls[0][1].signal?.aborted).toBe(true);
  await act(async () =>
    finish(
      Response.json({
        version: "1",
        status: "enrolled",
        correlationId: "a".repeat(32),
      }),
    ),
  );
  expect(view.onRefresh).not.toHaveBeenCalled();
});
