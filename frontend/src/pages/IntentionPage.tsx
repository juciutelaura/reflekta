import { useState } from "react";
import type { ApiClient } from "../lib/apiClient";

interface IntentionPageProps {
  apiClient: ApiClient;
  onJourneyStarted: (journeyId: string) => void;
}

export function IntentionPage({ apiClient, onJourneyStarted }: IntentionPageProps) {
  const [text, setText] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      const intention = await apiClient.createIntention(text);
      const journey = await apiClient.createJourney(intention.id);
      onJourneyStarted(journey.id);
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="stack" onSubmit={handleSubmit}>
      <div className="field">
        <label htmlFor="intention-text">What would you like to explore?</label>
        <textarea
          id="intention-text"
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder="What is on your mind right now?"
        />
      </div>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      <button type="submit" className="btn" disabled={isSubmitting || text.trim().length === 0}>
        Continue
      </button>
    </form>
  );
}
