import { act, cleanup, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { useBootstrapStatus } from "./useBootstrapStatus";

const ready = {
  version: "1",
  state: "ready",
  component: "storage",
  correlationId: "0123456789abcdef0123456789abcdef",
};

beforeEach(() => vi.useFakeTimers());
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

it("clears healthy state on failure and schedules refresh starts 30 seconds apart", async () => {
  const fetch = vi
    .fn()
    .mockImplementation(() => Promise.resolve(Response.json(ready)));
  vi.stubGlobal("fetch", fetch);
  const { result } = renderHook(useBootstrapStatus);
  await act(() => vi.advanceTimersByTimeAsync(0));
  expect(result.current.state.kind).toBe("ready");
  fetch.mockRejectedValue(new TypeError("transport_failed"));
  await act(() => vi.advanceTimersByTimeAsync(30_000));
  expect(result.current.state.kind).toBe("unavailable");
  expect(fetch).toHaveBeenCalledTimes(2);
  await act(() => vi.advanceTimersByTimeAsync(30_000));
  expect(fetch).toHaveBeenCalledTimes(3);
});

it("bounds transfer and body parsing, deduplicates retry and ignores late results", async () => {
  let resolveBody: (value: unknown) => void = () => {};
  const response = Response.json(ready);
  vi.spyOn(response, "json").mockReturnValue(
    new Promise((resolve) => {
      resolveBody = resolve;
    }),
  );
  const fetch = vi.fn().mockResolvedValue(response);
  vi.stubGlobal("fetch", fetch);
  const { result } = renderHook(useBootstrapStatus);
  await act(() => vi.advanceTimersByTimeAsync(0));
  act(() => {
    result.current.retry();
    result.current.retry();
  });
  expect(fetch).toHaveBeenCalledTimes(1);
  await act(() => vi.advanceTimersByTimeAsync(10_000));
  expect(result.current.state.kind).toBe("unavailable");
  expect(fetch.mock.calls[0][1].signal.aborted).toBe(true);
  fetch.mockResolvedValue(
    Response.json(
      { ...ready, state: "unavailable", component: "host" },
      { status: 503 },
    ),
  );
  act(() => result.current.retry());
  await act(() => vi.advanceTimersByTimeAsync(0));
  await act(async () => resolveBody(ready));
  expect(result.current.state.kind).toBe("unavailable");
});

it("does not retain a green result across suspension and immediately revalidates", async () => {
  const fetch = vi
    .fn()
    .mockImplementation(() => Promise.resolve(Response.json(ready)));
  vi.stubGlobal("fetch", fetch);
  const hidden = vi.spyOn(document, "hidden", "get").mockReturnValue(false);
  const { result, unmount } = renderHook(useBootstrapStatus);
  await act(() => vi.advanceTimersByTimeAsync(0));
  expect(result.current.state.kind).toBe("ready");
  hidden.mockReturnValue(true);
  act(() => document.dispatchEvent(new Event("visibilitychange")));
  expect(result.current.state.kind).toBe("stale");
  await act(() => vi.advanceTimersByTimeAsync(30_000));
  expect(fetch).toHaveBeenCalledTimes(1);
  hidden.mockReturnValue(false);
  act(() => document.dispatchEvent(new Event("visibilitychange")));
  await act(() => vi.advanceTimersByTimeAsync(0));
  expect(fetch).toHaveBeenCalledTimes(2);
  unmount();
  await act(() => vi.advanceTimersByTimeAsync(60_000));
  expect(fetch).toHaveBeenCalledTimes(2);
});

it("expires a never-completing transfer and aborts a pending retry on unmount", async () => {
  const fetch = vi.fn().mockImplementation(() => new Promise(() => {}));
  vi.stubGlobal("fetch", fetch);
  const { result, unmount } = renderHook(useBootstrapStatus);
  await act(() => vi.advanceTimersByTimeAsync(9_999));
  expect(result.current.state.kind).toBe("loading");
  await act(() => vi.advanceTimersByTimeAsync(1));
  expect(result.current.state.kind).toBe("unavailable");
  expect(fetch.mock.calls[0][1].signal.aborted).toBe(true);
  act(() => result.current.retry());
  expect(fetch).toHaveBeenCalledTimes(2);
  unmount();
  expect(fetch.mock.calls[1][1].signal.aborted).toBe(true);
  await act(() => vi.advanceTimersByTimeAsync(60_000));
  expect(fetch).toHaveBeenCalledTimes(2);
});

it("rechecks on focus and refuses a suspended attempt's late result", async () => {
  let finish: (value: unknown) => void = () => {};
  const first = Response.json(ready);
  vi.spyOn(first, "json").mockImplementation(
    () =>
      new Promise((resolve) => {
        finish = resolve;
      }),
  );
  const fetch = vi
    .fn()
    .mockResolvedValueOnce(first)
    .mockImplementation(() => Promise.resolve(Response.json(ready)));
  vi.stubGlobal("fetch", fetch);
  const { result } = renderHook(useBootstrapStatus);
  await act(() => vi.advanceTimersByTimeAsync(0));
  act(() => window.dispatchEvent(new Event("blur")));
  expect(result.current.state.kind).toBe("stale");
  expect(fetch.mock.calls[0][1].signal.aborted).toBe(true);
  act(() => window.dispatchEvent(new Event("focus")));
  await act(() => vi.advanceTimersByTimeAsync(0));
  expect(result.current.state.kind).toBe("ready");
  await act(async () => finish({ ...ready, state: "unavailable" }));
  expect(result.current.state.kind).toBe("ready");
});

it("reschedules refresh from a manual attempt instead of skipping an occupied interval", async () => {
  const fetch = vi
    .fn()
    .mockImplementation(() => Promise.resolve(Response.json(ready)));
  vi.stubGlobal("fetch", fetch);
  const { result } = renderHook(useBootstrapStatus);
  await act(() => vi.advanceTimersByTimeAsync(21_000));
  fetch.mockImplementation(() => new Promise(() => {}));
  act(() => result.current.retry());
  expect(fetch).toHaveBeenCalledTimes(2);
  await act(() => vi.advanceTimersByTimeAsync(10_000));
  expect(result.current.state.kind).toBe("unavailable");
  await act(() => vi.advanceTimersByTimeAsync(19_999));
  expect(fetch).toHaveBeenCalledTimes(2);
  await act(() => vi.advanceTimersByTimeAsync(1));
  expect(fetch).toHaveBeenCalledTimes(3);
});
