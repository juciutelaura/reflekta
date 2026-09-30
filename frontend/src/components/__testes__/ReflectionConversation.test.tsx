import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { ReflectionConversation } from "../ReflectionConversation";
import { ApiError } from "../../lib/apiClient";
import type { ApiClient, ConversationMessageDto } from "../../lib/apiClient";

function message(id: string, role: "user" | "assistant", content: string): ConversationMessageDto {
  return { id, role, content, createdAt: "2026-01-01T00:00:00Z" };
}

const opening = message("m1", "assistant", "What stands out to you?");

describe("ReflectionConversation", () => {
  it("sends a reply and shows the AI answer", async () => {
    const apiClient = {
      sendMessage: vi.fn().mockResolvedValue({
        userMessage: message("m2", "user", "The gripping."),
        assistantMessage: message("m3", "assistant", "When did you first notice it?"),
      }),
    } as unknown as ApiClient;
    render(<ReflectionConversation apiClient={apiClient} journeyId="journey-1" initialMessages={[opening]} initialAiUnavailable={false} />);

    expect(screen.getByText("What stands out to you?")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/your reply/i), { target: { value: "The gripping." } });
    fireEvent.click(screen.getByRole("button", { name: /send/i }));

    await waitFor(() => expect(screen.getByText("When did you first notice it?")).toBeInTheDocument());
    expect(screen.getByText("The gripping.")).toBeInTheDocument();
    expect(apiClient.sendMessage).toHaveBeenCalledWith("journey-1", "The gripping.");
    expect(screen.getByLabelText(/your reply/i)).toHaveValue("");
  });

  it("disables sending while waiting so a double click sends once", async () => {
    const apiClient = { sendMessage: vi.fn().mockReturnValue(new Promise(() => {})) } as unknown as ApiClient;
    render(<ReflectionConversation apiClient={apiClient} journeyId="journey-1" initialMessages={[opening]} initialAiUnavailable={false} />);

    fireEvent.change(screen.getByLabelText(/your reply/i), { target: { value: "The gripping." } });
    fireEvent.click(screen.getByRole("button", { name: /send/i }));
    fireEvent.click(screen.getByRole("button", { name: /send/i }));

    await waitFor(() => expect(screen.getByText(/ai is thinking/i)).toBeInTheDocument());
    expect(screen.getByRole("button", { name: /send/i })).toBeDisabled();
    expect(apiClient.sendMessage).toHaveBeenCalledTimes(1);
  });

  it("keeps the message and offers retry when the AI is unavailable", async () => {
    const apiClient = {
      sendMessage: vi.fn().mockRejectedValue(new ApiError(503, "/messages")),
      requestReply: vi.fn().mockResolvedValue({ assistantMessage: message("m3", "assistant", "When did you first notice it?") }),
    } as unknown as ApiClient;
    render(<ReflectionConversation apiClient={apiClient} journeyId="journey-1" initialMessages={[opening]} initialAiUnavailable={false} />);

    fireEvent.change(screen.getByLabelText(/your reply/i), { target: { value: "The gripping." } });
    fireEvent.click(screen.getByRole("button", { name: /send/i }));

    await waitFor(() => expect(screen.getByRole("button", { name: /try again/i })).toBeInTheDocument());
    expect(screen.getByText("The gripping.")).toBeInTheDocument();
    expect(screen.queryByLabelText(/your reply/i)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /try again/i }));

    await waitFor(() => expect(screen.getByText("When did you first notice it?")).toBeInTheDocument());
    expect(apiClient.requestReply).toHaveBeenCalledWith("journey-1");
    expect(screen.getByLabelText(/your reply/i)).toBeInTheDocument();
  });

  it("offers retry immediately when the first AI turn failed", () => {
    const apiClient = {} as unknown as ApiClient;
    render(<ReflectionConversation apiClient={apiClient} journeyId="journey-1" initialMessages={[]} initialAiUnavailable={true} />);

    expect(screen.getByRole("button", { name: /try again/i })).toBeInTheDocument();
  });
});