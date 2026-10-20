import "./BoardGrid.css";
import { BOARD_SIZE, cellGridPosition } from "./boardLayout";
import { CardArt } from "./CardArt";

export interface OpenPosition {
  boardPosition: number;
  cardId: string;
}

interface BoardGridProps {
  openPositions: OpenPosition[];
  animatingPosition: number | null;
}

type CellState = "closed" | "flash" | "open" | "opening";

function cellState(boardPosition: number, open: OpenPosition | undefined, animatingPosition: number | null): CellState {
  const isAnimatingHere = animatingPosition === boardPosition;
  if (open && isAnimatingHere) return "opening";
  if (open) return "open";
  if (isAnimatingHere) return "flash";
  return "closed";
}

export function BoardGrid({ openPositions, animatingPosition }: BoardGridProps) {
  const openByPosition = new Map(openPositions.map((p) => [p.boardPosition, p]));

  return (
    <div className="board-grid" role="grid" aria-label="Journey board">
      {Array.from({ length: BOARD_SIZE }, (_, boardPosition) => {
        const { row, column } = cellGridPosition(boardPosition);
        const open = openByPosition.get(boardPosition);
        const state = cellState(boardPosition, open, animatingPosition);

        return (
          <div
            key={boardPosition}
            role="gridcell"
            className={`board-cell board-cell--${state}`}
            style={{ gridRow: row + 1, gridColumn: column + 1 }}
            data-testid={`board-cell-${boardPosition}`}
          >
            {open && <CardArt boardPosition={boardPosition} cardId={open.cardId} />}
          </div>
        );
      })}
    </div>
  );
}
