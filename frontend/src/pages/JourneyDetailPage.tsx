import { useEffect, useState } from "react";
import type { ApiClient, JourneyDetailDto } from "../lib/apiClient";

interface JourneyDetailPageProps {
  apiClient: ApiClient;
  journeyId: string;
}

export function JourneyDetailPage({ apiClient, journeyId }: JourneyDetailPageProps) {
  const [journey, setJourney] = useState<JourneyDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .getJourneyDetail(journeyId)
      .then(setJourney)
      .catch(() => setError("Something went wrong. Please try again."));
  }, [apiClient, journeyId]);

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
