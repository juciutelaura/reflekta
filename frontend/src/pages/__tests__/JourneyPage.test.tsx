import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { JourneyPage } from "../JourneyPage";
import type { ApiClient } from "../../lib/apiClient";

describe("JourneyPage", () => {
  it("rolls the dice and displays the resulting card", async () => {
    const apiClient = {
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
    } as unknown as ApiClient;

    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));

    await waitFor(() => expect(screen.getByText("Control")).toBeInTheDocument());
    expect(screen.getByText(/notice where you try to hold on tightly/i)).toBeInTheDocument();
    expect(screen.getByText(/what are you trying hardest to control today/i)).toBeInTheDocument();
    expect(apiClient.rollDice).toHaveBeenCalledWith("journey-1");
  });
});
