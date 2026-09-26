import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import { HistoryPage } from "../HistoryPage";
import type { ApiClient } from "../../lib/apiClient";

describe("HistoryPage", () => {
  it("lists the user's journeys with a link to each one", async () => {
    const apiClient = {
      listJourneys: vi.fn().mockResolvedValue([
        {
          id: "journey-1",
          intentionText: "Should I change my career?",
          status: "Completed",
          startedAt: "2026-01-01T00:00:00Z",
          completedAt: "2026-01-01T01:00:00Z",
          cardCount: 2,
        },
      ]),
    } as unknown as ApiClient;

    render(
      <MemoryRouter>
        <HistoryPage apiClient={apiClient} />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText("Should I change my career?")).toBeInTheDocument());
    expect(screen.getByRole("link", { name: /should i change my career/i })).toHaveAttribute(
      "href",
      "/history/journey-1",
    );
  });

  it("shows a message when there are no journeys yet", async () => {
    const apiClient = { listJourneys: vi.fn().mockResolvedValue([]) } as unknown as ApiClient;

    render(
      <MemoryRouter>
        <HistoryPage apiClient={apiClient} />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText(/haven't completed a journey yet/i)).toBeInTheDocument());
  });
});
