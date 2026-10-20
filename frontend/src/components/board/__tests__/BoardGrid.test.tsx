import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { BoardGrid } from "../BoardGrid";
import { BOARD_SIZE } from "../boardLayout";

describe("BoardGrid", () => {
  it("renders all 72 cells, closed by default", () => {
    render(<BoardGrid openPositions={[]} animatingPosition={null} />);

    expect(screen.getAllByRole("gridcell")).toHaveLength(BOARD_SIZE);
    expect(screen.getByTestId("board-cell-0").className).toContain("board-cell--closed");
  });

  it("renders an open cell with its card art for a visited position", () => {
    render(
      <BoardGrid
        openPositions={[{ boardPosition: 5, cardId: "11111111-1111-1111-1111-111111111111" }]}
        animatingPosition={null}
      />,
    );

    const cell = screen.getByTestId("board-cell-5");
    expect(cell.className).toContain("board-cell--open");
    expect(cell.querySelector("img")).toBeInTheDocument();
  });

  it("renders a transient flash for an animating position that is not yet open", () => {
    render(<BoardGrid openPositions={[]} animatingPosition={10} />);

    const cell = screen.getByTestId("board-cell-10");
    expect(cell.className).toContain("board-cell--flash");
    expect(cell.querySelector("img")).not.toBeInTheDocument();
  });

  it("renders the opening transition for the landing cell, which is both open and animating", () => {
    render(
      <BoardGrid
        openPositions={[{ boardPosition: 20, cardId: "22222222-2222-2222-2222-222222222222" }]}
        animatingPosition={20}
      />,
    );

    const cell = screen.getByTestId("board-cell-20");
    expect(cell.className).toContain("board-cell--opening");
    expect(cell.querySelector("img")).toBeInTheDocument();
  });
});
