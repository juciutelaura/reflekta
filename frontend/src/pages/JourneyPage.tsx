import { useState } from "react";
import type { ApiClient, RollResultDto } from "../lib/apiClient";

interface JourneyPageProps {
  apiClient: ApiClient;
  journeyId: string;
  onJourneyCompleted: () => void;
}

export function JourneyPage({ apiClient, journeyId, onJourneyCompleted }: JourneyPageProps) {
  const [card, setCard] = useState<RollResultDto | null>(null);
  const [reflectionText, setReflectionText] = useState("");
  const [submittedReflection, setSubmittedReflection] = useState<string | null>(null);
  const [isRolling, setIsRolling] = useState(false);
  const [isSubmittingReflection, setIsSubmittingReflection] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleRoll() {
    setError(null);
    setIsRolling(true);
    try {
      const result = await apiClient.rollDice(journeyId);
      setCard(result);
      setReflectionText("");
      setSubmittedReflection(null);
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
      await apiClient.submitReflection(journeyId, reflectionText);
      setSubmittedReflection(reflectionText);
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

      {submittedReflection !== null && (
        <div className="stack">
          <p>{submittedReflection}</p>
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
