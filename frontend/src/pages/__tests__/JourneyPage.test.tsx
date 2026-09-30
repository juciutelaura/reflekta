import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { JourneyPage } from "../JourneyPage";
import { ApiError } from "../../lib/apiClient";
import type { ApiClient, ConversationMessageDto } from "../../lib/apiClient";

const rolledCard = {
  diceResult: 4,
  cardId: "card-1",
  cardTitle: "Control",
  cardWisdomText: "Notice where you try to hold on tightly.",
  cardReflectionPrompt: "What are you trying hardest to control today?",
  cardThemes: ["Control"],
  sequenceNumber: 1,
};

const openingMessage: ConversationMessageDto = { id: "m1", role: "assistant", content: "What stands out to you?", createdAt: "" };

function createApiClientMock(): ApiClient {
  return {
    createIntention: vi.fn(),
    createJourney: vi.fn(),
    rollDice: vi.fn().mockResolvedValue(rolledCard),
    getCurrentCard: vi.fn().mockRejectedValue(new ApiError(404, "/current-card")),
    submitReflection: vi.fn().mockResolvedValue({
      id: "reflection-1",
      playedCardId: "card-1",
      text: "I noticed I grip tightly onto plans.",
      createdAt: "",
      messages: [openingMessage],
      aiUnavailable: false,
    }),
    sendMessage: vi.fn(),
    requestReply: vi.fn(),
    completeJourney: vi.fn().mockResolvedValue({
      id: "journey-1",
      intentionId: "intention-1",
      status: "Completed",
      startedAt: "",
      completedAt: "2026-01-01T00:00:00Z",
    }),
  } as unknown as ApiClient;
}

async function rollAndReflect(text: string) {
  fireEvent.click(await screen.findByRole("button", { name: /roll/i }));
  await waitFor(() => screen.getByLabelText(/your reflection/i));
  fireEvent.change(screen.getByLabelText(/your reflection/i), { target: { value: text } });
  fireEvent.click(screen.getByRole("button", { name: /save reflection/i }));
}

describe("JourneyPage", () => {
  it("rolls the dice and displays the resulting card", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    fireEvent.click(await screen.findByRole("button", { name: /roll/i }));

    await waitFor(() => expect(screen.getByText("Control")).toBeInTheDocument());
    expect(screen.getByText(/notice where you try to hold on tightly/i)).toBeInTheDocument();
    expect(screen.getByText(/what are you trying hardest to control today/i)).toBeInTheDocument();
    expect(apiClient.rollDice).toHaveBeenCalledWith("journey-1");
  });

  it("shows a reflection form after the card is revealed, and hides it once submitted", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    await rollAndReflect("I noticed I grip tightly onto plans.");

    await waitFor(() =>
      expect(apiClient.submitReflection).toHaveBeenCalledWith("journey-1", "I noticed I grip tightly onto plans."),
    );
    await waitFor(() => expect(screen.queryByLabelText(/your reflection/i)).not.toBeInTheDocument());
    expect(screen.getByRole("button", { name: /continue journey/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /complete journey/i })).toBeInTheDocument();
  });

  it("shows the AI's first question after the reflection is saved", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    await rollAndReflect("I noticed I grip tightly onto plans.");

    await waitFor(() => expect(screen.getByText("What stands out to you?")).toBeInTheDocument());
    expect(screen.getByLabelText(/your reply/i)).toBeInTheDocument();
  });

  it("calls onJourneyCompleted after completing the journey", async () => {
    const apiClient = createApiClientMock();
    const onJourneyCompleted = vi.fn();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={onJourneyCompleted} />);

    await rollAndReflect("Enough for today.");
    await waitFor(() => screen.getByRole("button", { name: /complete journey/i }));

    fireEvent.click(screen.getByRole("button", { name: /complete journey/i }));

    await waitFor(() => expect(apiClient.completeJourney).toHaveBeenCalledWith("journey-1"));
    await waitFor(() => expect(onJourneyCompleted).toHaveBeenCalled());
  });

  it("restores the card, reflection and conversation after a reload", async () => {
    const apiClient = createApiClientMock();
    vi.mocked(apiClient.getCurrentCard).mockResolvedValue({
      ...rolledCard,
      reflectionText: "I noticed I grip tightly onto plans.",
      messages: [openingMessage],
    });
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    await waitFor(() => expect(screen.getByText("Control")).toBeInTheDocument());
    expect(screen.getByText("I noticed I grip tightly onto plans.")).toBeInTheDocument();
    expect(screen.getByText("What stands out to you?")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /continue journey/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^roll$/i })).not.toBeInTheDocument();
  });

  it("restores a card without a reflection to the reflection form", async () => {
    const apiClient = createApiClientMock();
    vi.mocked(apiClient.getCurrentCard).mockResolvedValue({ ...rolledCard, reflectionText: null, messages: [] });
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    await waitFor(() => expect(screen.getByLabelText(/your reflection/i)).toBeInTheDocument());
  });

  it("offers retry after a reload when the last message is unanswered", async () => {
    const apiClient = createApiClientMock();
    vi.mocked(apiClient.getCurrentCard).mockResolvedValue({
      ...rolledCard,
      reflectionText: "I noticed I grip tightly onto plans.",
      messages: [openingMessage, { id: "m2", role: "user", content: "Since school.", createdAt: "" }],
    });
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    await waitFor(() => expect(screen.getByRole("button", { name: /try again/i })).toBeInTheDocument());
    expect(screen.queryByLabelText(/your reply/i)).not.toBeInTheDocument();
  });
});
