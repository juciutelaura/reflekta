import { useEffect, useState } from "react";
import { ApiError } from "../lib/apiClient";
import type { ApiClient, JourneyDetailDto, SessionSummaryDto } from "../lib/apiClient";

interface JourneyDetailPageProps {
  apiClient: ApiClient;
  journeyId: string;
}

function isAiUnavailable(error: unknown): boolean {
  return error instanceof ApiError && error.status === 503;
}

export function JourneyDetailPage({ apiClient, journeyId }: JourneyDetailPageProps) {
  const [journey, setJourney] = useState<JourneyDetailDto | null>(null);
  const [summary, setSummary] = useState<SessionSummaryDto | null>(null);
  const [isRetrying, setIsRetrying] = useState(false);
  const [summaryFailed, setSummaryFailed] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .getJourneyDetail(journeyId)
      .then((result) => {
        setJourney(result);
        setSummary(result.summary);
      })
      .catch(() => setError("Something went wrong. Please try again."));
  }, [apiClient, journeyId]);

  async function handleRetrySummary() {
    setSummaryFailed(false);
    setIsRetrying(true);
    try {
      const result = await apiClient.retrySummary(journeyId);
      setSummary(result);
    } catch (caught) {
      if (isAiUnavailable(caught)) {
        setSummaryFailed(true);
      } else {
        setError("Something went wrong. Please try again.");
      }
    } finally {
      setIsRetrying(false);
    }
  }

  if (error) {
    return (
      <p className="error" role="alert">
        {error}
      </p>
    );
  }

  if (journey === null) {
    return <p>Loading…</p>;
  }

  return (
    <div className="stack">
      <h1>{journey.intentionText}</h1>
      <p>{journey.status}</p>

      {summary && (
        <article className="card">
          <h2>Summary</h2>
          <p>{summary.summaryText}</p>
          {summary.themes.length > 0 && <p className="muted">{summary.themes.join(", ")}</p>}
        </article>
      )}

      {!summary && journey.status === "Completed" && (
        <div className="stack">
          {summaryFailed && (
            <p className="error" role="alert">
              Couldn't generate a summary. Please try again.
            </p>
          )}
          <button onClick={handleRetrySummary} disabled={isRetrying} className="btn btn-ghost">
            {summaryFailed ? "Try again" : "Generate summary"}
          </button>
        </div>
      )}

      {journey.playedCards.map((playedCard) => (
        <article className="card" key={playedCard.id}>
          <h2>{playedCard.cardTitle}</h2>
          <p>{playedCard.cardWisdomText}</p>
          <p className="prompt">{playedCard.cardReflectionPrompt}</p>
          {playedCard.reflectionText && <p>{playedCard.reflectionText}</p>}
        </article>
      ))}
    </div>
  );
}
