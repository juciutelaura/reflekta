import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { JourneyPage } from "../JourneyPage";
import { JourneyDetailPage } from "../JourneyDetailPage";
import type { ApiClient } from "../../lib/apiClient";

function createApiClientMock(): ApiClient {
  return {
    createIntention: vi.fn(),
    createJourney: vi.fn(),
    rollDice: vi.fn().mockResolvedValue({
      diceResult: 4,
      cardId: "card-1",
      cardTitle: "Control",
      cardWisdomText: "Notice where you try to hold on tightly.",
      cardReflectionPrompt: "What are you trying hardest to control today?",
      cardThemes: ["Control"],
      sequenceNumber: 1,
    }),
    getCurrentCard: vi.fn(),
    submitReflection: vi.fn().mockResolvedValue({
      id: "reflection-1",
      playedCardId: "card-1",
      text: "I noticed I grip tightly onto plans.",
      createdAt: "",
    }),
    completeJourney: vi.fn().mockResolvedValue({
      id: "journey-1",
      intentionId: "intention-1",
      status: "Completed",
      startedAt: "",
      completedAt: "2026-01-01T00:00:00Z",
    }),
  } as unknown as ApiClient;
}

describe("JourneyPage", () => {
  it("rolls the dice and displays the resulting card", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));

    await waitFor(() => expect(screen.getByText("Control")).toBeInTheDocument());
    expect(screen.getByText(/notice where you try to hold on tightly/i)).toBeInTheDocument();
    expect(screen.getByText(/what are you trying hardest to control today/i)).toBeInTheDocument();
    expect(apiClient.rollDice).toHaveBeenCalledWith("journey-1");
  });

  it("shows a reflection form after the card is revealed, and hides it once submitted", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));
    await waitFor(() => expect(screen.getByLabelText(/your reflection/i)).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText(/your reflection/i), {
      target: { value: "I noticed I grip tightly onto plans." },
    });
    fireEvent.click(screen.getByRole("button", { name: /save reflection/i }));

    await waitFor(() =>
      expect(apiClient.submitReflection).toHaveBeenCalledWith("journey-1", "I noticed I grip tightly onto plans."),
    );
    await waitFor(() => expect(screen.queryByLabelText(/your reflection/i)).not.toBeInTheDocument());
    expect(screen.getByRole("button", { name: /continue journey/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /complete journey/i })).toBeInTheDocument();
  });

  it("calls onJourneyCompleted after completing the journey", async () => {
    const apiClient = createApiClientMock();
    const onJourneyCompleted = vi.fn();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={onJourneyCompleted} />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));
    await waitFor(() => screen.getByLabelText(/your reflection/i));
    fireEvent.change(screen.getByLabelText(/your reflection/i), { target: { value: "Enough for today." } });
    fireEvent.click(screen.getByRole("button", { name: /save reflection/i }));
    await waitFor(() => screen.getByRole("button", { name: /complete journey/i }));

    fireEvent.click(screen.getByRole("button", { name: /complete journey/i }));

    await waitFor(() => expect(apiClient.completeJourney).toHaveBeenCalledWith("journey-1"));
    await waitFor(() => expect(onJourneyCompleted).toHaveBeenCalled());
  });

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
});


  
});
