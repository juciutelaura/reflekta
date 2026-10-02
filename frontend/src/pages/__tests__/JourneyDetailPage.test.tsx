import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { JourneyDetailPage } from "../JourneyDetailPage";
import type { ApiClient } from "../../lib/apiClient";


describe("JourneyDetailPage", () => {
  it("shows the intention and each played card with its reflection", async () => {
    const apiClient = {
      getJourneyDetail: vi.fn().mockResolvedValue({
        id: "journey-1",
        intentionText: "Should I change my career?",
        status: "Completed",
        startedAt: "2026-01-01T00:00:00Z",
        completedAt: "2026-01-01T01:00:00Z",
        playedCards: [
          {
            id: "played-1",
            sequenceNumber: 1,
            diceResult: 4,
            cardTitle: "Control",
            cardWisdomText: "Notice where you try to hold on tightly.",
            cardReflectionPrompt: "What are you trying hardest to control today?",
            cardThemes: ["Control"],
            reflectionText: "I noticed I grip tightly onto plans.",
          },
        ],
      }),
    } as unknown as ApiClient;

    render(<JourneyDetailPage apiClient={apiClient} journeyId="journey-1" />);

    await waitFor(() => expect(screen.getByText("Should I change my career?")).toBeInTheDocument());
    expect(screen.getByText("Control")).toBeInTheDocument();
    expect(screen.getByText("I noticed I grip tightly onto plans.")).toBeInTheDocument();
  });
it("shows the session summary when present", async () => {
    const apiClient = {
      getJourneyDetail: vi.fn().mockResolvedValue({
        id: "journey-1",
        intentionText: "Should I change my career?",
        status: "Completed",
        startedAt: "2026-01-01T00:00:00Z",
        completedAt: "2026-01-01T01:00:00Z",
        playedCards: [],
        summary: {
          id: "summary-1",
          journeyId: "journey-1",
          summaryText: "The user explored what it means to hold on tightly to plans.",
          themes: ["control", "career"],
          createdAt: "2026-01-01T01:00:00Z",
        },
      }),
    } as unknown as ApiClient;

    render(<JourneyDetailPage apiClient={apiClient} journeyId="journey-1" />);

    await waitFor(() =>
      expect(screen.getByText("The user explored what it means to hold on tightly to plans.")).toBeInTheDocument(),
    );
    expect(screen.getByText("control, career")).toBeInTheDocument();
  });

  it("offers a retry button when a completed journey has no summary yet", async () => {
    const apiClient = {
      getJourneyDetail: vi.fn().mockResolvedValue({
        id: "journey-1",
        intentionText: "Should I change my career?",
        status: "Completed",
        startedAt: "2026-01-01T00:00:00Z",
        completedAt: "2026-01-01T01:00:00Z",
        playedCards: [],
        summary: null,
      }),
      retrySummary: vi.fn().mockResolvedValue({
        id: "summary-1",
        journeyId: "journey-1",
        summaryText: "The user explored control.",
        themes: [],
        createdAt: "2026-01-01T01:00:00Z",
      }),
    } as unknown as ApiClient;

    render(<JourneyDetailPage apiClient={apiClient} journeyId="journey-1" />);

    await waitFor(() => expect(screen.getByRole("button", { name: /generate summary/i })).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: /generate summary/i }));

    await waitFor(() => expect(screen.getByText("The user explored control.")).toBeInTheDocument());
    expect(apiClient.retrySummary).toHaveBeenCalledWith("journey-1");
  });

});
