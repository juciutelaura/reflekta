import { useState } from "react";
import type { ApiClient, RollResultDto } from "../lib/apiClient";

interface JourneyPageProps {
  apiClient: ApiClient;
  journeyId: string;
}

export function JourneyPage({ apiClient, journeyId }: JourneyPageProps) {
  const [card, setCard] = useState<RollResultDto | null>(null);
  const [isRolling, setIsRolling] = useState(false);

  async function handleRoll() {
    setIsRolling(true);
    try {
      const result = await apiClient.rollDice(journeyId);
      setCard(result);
    } finally {
      setIsRolling(false);
    }
  }

  return (
    <div>
      <button onClick={handleRoll} disabled={isRolling}>
        Roll
      </button>
      {card && (
        <article>
          <h2>{card.cardTitle}</h2>
          <p>{card.cardWisdomText}</p>
          <p>{card.cardReflectionPrompt}</p>
        </article>
      )}
    </div>
  );
}
