import { useEffect, useState } from "react";
import { ApiError } from "../lib/apiClient";
import type { ApiClient, ConversationMessageDto, RollResultDto } from "../lib/apiClient";
import { ReflectionConversation } from "../components/ReflectionConversation";

interface JourneyPageProps {
  apiClient: ApiClient;
  journeyId: string;
  onJourneyCompleted: () => void;
}

interface Conversation {
  messages: ConversationMessageDto[];
  aiUnavailable: boolean;
}

/** True when the reflection or the latest user message still waits for an AI reply. */
function isAwaitingAiReply(messages: ConversationMessageDto[]): boolean {
  return messages.length === 0 || messages[messages.length - 1].role === "user";
}

export function JourneyPage({ apiClient, journeyId, onJourneyCompleted }: JourneyPageProps) {
  const [isLoading, setIsLoading] = useState(true);
  const [card, setCard] = useState<RollResultDto | null>(null);
  const [reflectionText, setReflectionText] = useState("");
  const [submittedReflection, setSubmittedReflection] = useState<string | null>(null);
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [isRolling, setIsRolling] = useState(false);
  const [isSubmittingReflection, setIsSubmittingReflection] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let ignore = false;
    apiClient
      .getCurrentCard(journeyId)
      .then((current) => {
        if (ignore) return;
        setCard(current);
        setSubmittedReflection(current.reflectionText);
        if (current.reflectionText !== null) {
          setConversation({ messages: current.messages, aiUnavailable: isAwaitingAiReply(current.messages) });
        }
      })
      .catch((caught) => {
        if (ignore) return;
        if (!(caught instanceof ApiError && caught.status === 404)) {
          setError("Something went wrong. Please try again.");
        }
      })
      .finally(() => {
        if (!ignore) setIsLoading(false);
      });
    return () => {
      ignore = true;
    };
  }, [apiClient, journeyId]);

  async function handleRoll() {
    setError(null);
    setIsRolling(true);
    try {
      const result = await apiClient.rollDice(journeyId);
      setCard(result);
      setReflectionText("");
      setSubmittedReflection(null);
      setConversation(null);
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsRolling(false);
    }
  }

  async function handleSubmitReflection(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmittingReflection(true);
    try {
      const result = await apiClient.submitReflection(journeyId, reflectionText);
      setSubmittedReflection(result.text);
      setConversation({ messages: result.messages, aiUnavailable: result.aiUnavailable });
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsSubmittingReflection(false);
    }
  }

  async function handleComplete() {
    setError(null);
    setIsCompleting(true);
    try {
      await apiClient.completeJourney(journeyId);
      onJourneyCompleted();
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsCompleting(false);
    }
  }

  if (isLoading) {
    return <p>Loading…</p>;
  }

  return (
    <div className="stack">
      {!card && (
        <button onClick={handleRoll} disabled={isRolling} className="btn">
          Roll
        </button>
      )}

      {card && (
        <article className="card">
          <h2>{card.cardTitle}</h2>
          <p>{card.cardWisdomText}</p>
          <p className="prompt">{card.cardReflectionPrompt}</p>
        </article>
      )}

      {card && submittedReflection === null && (
        <form className="stack" onSubmit={handleSubmitReflection}>
          <div className="field">
            <label htmlFor="reflection-text">Your reflection</label>
            <textarea
              id="reflection-text"
              value={reflectionText}
              onChange={(event) => setReflectionText(event.target.value)}
              placeholder="What came up for you?"
            />
          </div>
          <button
            type="submit"
            className="btn"
            disabled={isSubmittingReflection || reflectionText.trim().length === 0}
          >
            Save reflection
          </button>
        </form>
      )}

      {card && submittedReflection !== null && (
        <div className="stack">
          <p>{submittedReflection}</p>
          {conversation && (
            <ReflectionConversation
              key={card.sequenceNumber}
              apiClient={apiClient}
              journeyId={journeyId}
              initialMessages={conversation.messages}
              initialAiUnavailable={conversation.aiUnavailable}
            />
          )}
          <button onClick={handleRoll} disabled={isRolling} className="btn">
            Continue journey
          </button>
          <button onClick={handleComplete} disabled={isCompleting} className="btn btn-ghost">
            Complete journey
          </button>
        </div>
      )}

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}