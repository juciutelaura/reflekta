import { useState } from "react";
import { BoardIcon } from "./BoardIcon";
import { cardImageSrc, iconForCard } from "./boardLayout";

interface CardArtProps {
  boardPosition: number;
  cardId: string;
}

export function CardArt({ boardPosition, cardId }: CardArtProps) {
  const [imageFailed, setImageFailed] = useState(false);

  if (imageFailed) {
    return <BoardIcon icon={iconForCard(cardId)} />;
  }

  return (
    <img
      className="card-art"
      src={cardImageSrc(boardPosition)}
      alt=""
      onError={() => setImageFailed(true)}
    />
  );
}
