import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { IntentionPage } from "../IntentionPage";
import type { ApiClient } from "../../lib/apiClient";

describe("IntentionPage", () => {
  it("submits the intention text and calls onJourneyStarted with the new journey id", async () => {
    const apiClient = {
      createIntention: vi.fn().mockResolvedValue({ id: "intention-1", originalText: "test", clarifiedText: null, createdAt: "" }),
      createJourney: vi.fn().mockResolvedValue({ id: "journey-1", intentionId: "intention-1", status: "Active", startedAt: "" }),
      rollDice: vi.fn(),
      getCurrentCard: vi.fn(),
    } as unknown as ApiClient;

    const onJourneyStarted = vi.fn();
    render(<IntentionPage apiClient={apiClient} onJourneyStarted={onJourneyStarted} />);

    fireEvent.change(screen.getByLabelText(/what would you like to explore/i), {
      target: { value: "Should I change my career?" },
    });
    fireEvent.click(screen.getByRole("button", { name: /continue/i }));

    await waitFor(() => expect(onJourneyStarted).toHaveBeenCalledWith("journey-1"));
    expect(apiClient.createIntention).toHaveBeenCalledWith("Should I change my career?");
    expect(apiClient.createJourney).toHaveBeenCalledWith("intention-1");
  });
});
