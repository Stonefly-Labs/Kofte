import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { BootstrapStatus } from "./BootstrapStatus";
import { useBootstrapStatus } from "./useBootstrapStatus";

vi.mock("./useBootstrapStatus", () => ({ useBootstrapStatus: vi.fn() }));
afterEach(cleanup);

it("provides a polite textual status and preserves focused retry across updates", () => {
  const retry = vi.fn();
  vi.mocked(useBootstrapStatus).mockReturnValue({
    state: { kind: "ready" },
    checking: false,
    retry,
  });

  const { rerender } = render(<BootstrapStatus />);
  expect(screen.getByRole("status").textContent).toContain(
    "Local host is ready",
  );
  expect(screen.getByRole("status").getAttribute("aria-live")).toBe("polite");
  const button = screen.getByRole("button", { name: "Retry connection check" });
  button.focus();
  fireEvent.click(button);
  expect(retry).toHaveBeenCalledTimes(1);
  vi.mocked(useBootstrapStatus).mockReturnValue({
    state: { kind: "unavailable", correlationId: "a".repeat(32) },
    checking: true,
    retry,
  });
  rerender(<BootstrapStatus />);
  expect(document.activeElement).toBe(button);
  expect(screen.getByRole("status").textContent).toContain("unavailable");
  expect(screen.getByText(/Retrying never resets your data/)).toBeTruthy();
});

it("announces meaningful state changes without rewriting unchanged polling status", async () => {
  const retry = vi.fn();
  vi.mocked(useBootstrapStatus).mockReturnValue({
    state: { kind: "loading" },
    checking: true,
    retry,
  });
  const { rerender } = render(<BootstrapStatus />);
  const status = screen.getByRole("status");
  expect(status.textContent).toContain("Checking the local host");
  vi.mocked(useBootstrapStatus).mockReturnValue({
    state: { kind: "ready" },
    checking: false,
    retry,
  });
  rerender(<BootstrapStatus />);
  let changes = 0;
  const observer = new MutationObserver((records) => {
    changes += records.length;
  });
  observer.observe(status, {
    childList: true,
    subtree: true,
    characterData: true,
  });
  vi.mocked(useBootstrapStatus).mockReturnValue({
    state: { kind: "ready" },
    checking: true,
    retry,
  });
  await act(async () => rerender(<BootstrapStatus />));
  expect(changes).toBe(0);
  vi.mocked(useBootstrapStatus).mockReturnValue({
    state: { kind: "stale" },
    checking: false,
    retry,
  });
  await act(async () => rerender(<BootstrapStatus />));
  expect(status.textContent).toContain("Readiness needs to be checked");
  expect(changes).toBeGreaterThan(0);
  observer.disconnect();
});
