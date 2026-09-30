import { useState } from "react";
import { ApiError } from "../lib/apiClient";
import type { ApiClient, ConversationMessageDto } from "../lib/apiClient";

interface ReflectionConversationProps {
  apiClient: ApiClient;
  journeyId: string;
  initialMessages: ConversationMessageDto[];
  initialAiUnavailable: boolean;
}

function isAiUnavailable(error: unknown): boolean {
  return error instanceof ApiError && error.status === 503;
}

export function ReflectionConversation({
  apiClient,
  journeyId,
  initialMessages,
  initialAiUnavailable,
}: ReflectionConversationProps) {
  const [messages, setMessages] = useState(initialMessages);
  const [draft, setDraft] = useState("");
  const [isWaiting, setIsWaiting] = useState(false);
  const [aiFailed, setAiFailed] = useState(initialAiUnavailable);
  const [error, setError] = useState<string | null>(null);

  async function handleSend(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isWaiting) return;
    const content = draft.trim();
    setError(null);
    setIsWaiting(true);
    try {
      const result = await apiClient.sendMessage(journeyId, content);
      setMessages((current) => [...current, result.userMessage, result.assistantMessage]);
      setDraft("");
    } catch (caught) {
      if (isAiUnavailable(caught)) {
        // The backend saved the user's message; show it and offer a retry for the AI reply.
        const pending: ConversationMessageDto = {
          id: `pending-${Date.now()}`,
          role: "user",
          content,
          createdAt: new Date().toISOString(),
        };
        setMessages((current) => [...current, pending]);
        setDraft("");
        setAiFailed(true);
      } else {
        setError("Something went wrong. Please try again.");
      }
    } finally {
      setIsWaiting(false);
    }
  }

  async function handleRetry() {
    setError(null);
    setIsWaiting(true);
    try {
      const result = await apiClient.requestReply(journeyId);
      setMessages((current) => [...current, result.assistantMessage]);
      setAiFailed(false);
    } catch (caught) {
      if (!isAiUnavailable(caught)) setError("Something went wrong. Please try again.");
    } finally {
      setIsWaiting(false);
    }
  }

  return (
    <section className="conversation" aria-label="Reflection conversation">
      <ol className="message-list">
        {messages.map((message) => (
          <li key={message.id} className={`message message-${message.role}`}>
            {message.content}
          </li>
        ))}
      </ol>

      {isWaiting && <p className="muted">AI is thinking…</p>}

      {aiFailed && !isWaiting && (
        <div className="stack">
          <p className="error" role="alert">
            Couldn't get a response. Please try again.
          </p>
          <button onClick={handleRetry} className="btn btn-ghost">
            Try again
          </button>
        </div>
      )}

      {!aiFailed && (
        <form className="stack" onSubmit={handleSend}>
          <div className="field">
            <label htmlFor="conversation-reply">Your reply</label>
            <textarea
              id="conversation-reply"
              value={draft}
              maxLength={2000}
              onChange={(event) => setDraft(event.target.value)}
            />
          </div>
          <button type="submit" className="btn" disabled={isWaiting || draft.trim().length === 0}>
            Send
          </button>
        </form>
      )}

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </section>
  );
}
