import { cleanup, fireEvent, render, screen } from "@testing-library/react";
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
