import { useEffect, useState } from "react";
import "./JourneyPage.css";
import { ApiError } from "../lib/apiClient";
import type { ApiClient, ConversationMessageDto, RollResultDto } from "../lib/apiClient";
import { ReflectionConversation } from "../components/ReflectionConversation";
import { BoardGrid } from "../components/board/BoardGrid";
import type { OpenPosition } from "../components/board/BoardGrid";
import { DicePanel } from "../components/board/DicePanel";
import { VisitedGallery } from "../components/board/VisitedGallery";
import type { VisitedCard } from "../components/board/VisitedGallery";
import { BOARD_SIZE, animationSteps } from "../components/board/boardLayout";

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

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function JourneyPage({ apiClient, journeyId, onJourneyCompleted }: JourneyPageProps) {
  const [isLoading, setIsLoading] = useState(true);
  const [intentionText, setIntentionText] = useState("");
  const [openPositions, setOpenPositions] = useState<OpenPosition[]>([]);
  const [visitedCards, setVisitedCards] = useState<VisitedCard[]>([]);
  const [animatingPosition, setAnimatingPosition] = useState<number | null>(null);
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

    async function load() {
      try {
        const detail = await apiClient.getJourneyDetail(journeyId);
        if (ignore) return;
        setIntentionText(detail.intentionText);
        const uniqueByPosition = Array.from(
          new Map(detail.playedCards.map((pc) => [pc.boardPosition, pc])).values(),
        );
        setOpenPositions(uniqueByPosition.map((pc) => ({ boardPosition: pc.boardPosition, cardId: pc.cardId })));
        setVisitedCards(
          uniqueByPosition.map((pc) => ({ cardId: pc.cardId, cardTitle: pc.cardTitle, boardPosition: pc.boardPosition })),
        );
      } catch {
        // The board still renders fully closed; the current-card fetch below still lets play continue.
      }

      try {
        const current = await apiClient.getCurrentCard(journeyId);
        if (ignore) return;
        setCard(current);
        setSubmittedReflection(current.reflectionText);
        if (current.reflectionText !== null) {
          setConversation({ messages: current.messages, aiUnavailable: isAwaitingAiReply(current.messages) });
        }
      } catch (caught) {
        if (ignore) return;
        if (!(caught instanceof ApiError && caught.status === 404)) {
          setError("Something went wrong. Please try again.");
        }
      }
    }

    load().finally(() => {
      if (!ignore) setIsLoading(false);
    });
    return () => {
      ignore = true;
    };
  }, [apiClient, journeyId]);

  async function handleRoll() {
    if (isRolling) return;
    setError(null);
    setIsRolling(true);
    try {
      const result = await apiClient.rollDice(journeyId);
      const startPosition = (result.boardPosition - result.diceResult + BOARD_SIZE + BOARD_SIZE) % BOARD_SIZE;
      const steps = animationSteps(startPosition, result.diceResult);

      for (const step of steps.slice(0, -1)) {
        setAnimatingPosition(step);
        await sleep(150);
      }

      const landingPosition = steps[steps.length - 1];
      setOpenPositions((current) =>
        current.some((entry) => entry.boardPosition === result.boardPosition)
          ? current
          : [...current, { boardPosition: result.boardPosition, cardId: result.cardId }],
      );
      setVisitedCards((current) =>
        current.some((entry) => entry.boardPosition === result.boardPosition)
          ? current
          : [...current, { cardId: result.cardId, cardTitle: result.cardTitle, boardPosition: result.boardPosition }],
      );
      setAnimatingPosition(landingPosition);
      await sleep(400);
      setAnimatingPosition(null);

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
    <div className="board-page">
      <header className="board-page__intention">
        <p>{intentionText}</p>
      </header>

      <div className="board-page__layout">
        <BoardGrid openPositions={openPositions} animatingPosition={animatingPosition} />

        <aside className="board-page__sidebar">
          <DicePanel
            onRoll={handleRoll}
            isRolling={isRolling}
            disabled={card !== null && submittedReflection === null}
          />

          <VisitedGallery visitedCards={visitedCards} totalBoardSize={BOARD_SIZE} />

          <section className="board-page__reflection">
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
          </section>
        </aside>
      </div>
    </div>
  );
}
