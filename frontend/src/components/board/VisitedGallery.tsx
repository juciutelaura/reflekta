import { CardArt } from "./CardArt";

export interface VisitedCard {
  cardId: string;
  cardTitle: string;
  boardPosition: number;
}

interface VisitedGalleryProps {
  visitedCards: VisitedCard[];
  totalBoardSize: number;
}

export function VisitedGallery({ visitedCards, totalBoardSize }: VisitedGalleryProps) {
  return (
    <section className="visited-gallery" aria-label="Mano kelionė">
      <div className="visited-gallery__header">
        <h3>Mano kelionė</h3>
        <span className="muted">
          Atvertos kortelės ({visitedCards.length}/{totalBoardSize})
        </span>
      </div>
      <ul className="visited-gallery__list">
        {visitedCards.map((card) => (
          <li key={card.boardPosition} className="visited-gallery__item" title={card.cardTitle}>
            <CardArt boardPosition={card.boardPosition} cardId={card.cardId} />
          </li>
        ))}
      </ul>
    </section>
  );
}
