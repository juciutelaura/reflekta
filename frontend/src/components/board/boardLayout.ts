export const BOARD_COLUMNS = 9;
export const BOARD_ROWS = 8;
export const BOARD_SIZE = BOARD_COLUMNS * BOARD_ROWS;

export interface GridPosition {
  row: number;
  column: number;
}

/** Snake-path placement: band 0 is the bottom row and runs left-to-right, each band above it reverses direction. */
export function cellGridPosition(boardPosition: number): GridPosition {
  const band = Math.floor(boardPosition / BOARD_COLUMNS);
  const indexInBand = boardPosition % BOARD_COLUMNS;
  const column = band % 2 === 0 ? indexInBand : BOARD_COLUMNS - 1 - indexInBand;
  const row = BOARD_ROWS - 1 - band;
  return { row, column };
}

export const ICON_KEYS = [
  "circle",
  "leaf",
  "spiral",
  "wave",
  "square",
  "triangle",
  "dot-cluster",
  "arc",
  "cross",
  "diamond",
  "line",
  "grid",
] as const;

export type IconKey = (typeof ICON_KEYS)[number];

/** Deterministic placeholder icon pick — not semantic (every card's theme is unique), just stable per card id. */
export function iconForCard(cardId: string): IconKey {
  const hex = cardId.replace(/-/g, "").slice(0, 8);
  const index = parseInt(hex, 16) % ICON_KEYS.length;
  return ICON_KEYS[index];
}

/** The sequence of board positions a token passes through for a roll, wrapping at BOARD_SIZE, last entry is the landing position. */
export function animationSteps(startPosition: number, diceResult: number): number[] {
  const steps: number[] = [];
  for (let i = 1; i <= diceResult; i++) {
    steps.push((startPosition + i) % BOARD_SIZE);
  }
  return steps;
}

/** Real card illustrations live in frontend/public/cards/, named by 1-indexed, zero-padded position. */
export function cardImageSrc(boardPosition: number): string {
  return `/cards/${String(boardPosition + 1).padStart(2, "0")}.png`;
}
