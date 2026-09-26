import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { ApiClient, JourneySummaryDto } from "../lib/apiClient";

interface HistoryPageProps {
  apiClient: ApiClient;
}

export function HistoryPage({ apiClient }: HistoryPageProps) {
  const [journeys, setJourneys] = useState<JourneySummaryDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .listJourneys()
      .then(setJourneys)
      .catch(() => setError("Something went wrong. Please try again."));
  }, [apiClient]);

  if (error) {
    return (
      <p className="error" role="alert">
        {error}
      </p>
    );
  }

  if (journeys === null) {
    return <p>Loading…</p>;
  }

  if (journeys.length === 0) {
    return <p>You haven't completed a journey yet.</p>;
  }

  return (
    <ul className="history-list">
      {journeys.map((journey) => (
        <li key={journey.id} className="history-item">
          <Link to={`/history/${journey.id}`}>{journey.intentionText}</Link>
          <span>
            {" "}
            — {journey.status} — {journey.cardCount} card{journey.cardCount === 1 ? "" : "s"}
          </span>
        </li>
      ))}
    </ul>
  );
}
