import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { VisitedGallery } from "../VisitedGallery";

describe("VisitedGallery", () => {
  it("shows the opened-count out of the total board size", () => {
    render(
      <VisitedGallery
        visitedCards={[
          { cardId: "11111111-1111-1111-1111-111111111111", cardTitle: "Control", boardPosition: 4 },
          { cardId: "22222222-2222-2222-2222-222222222222", cardTitle: "Fear", boardPosition: 20 },
        ]}
        totalBoardSize={72}
      />,
    );

    expect(screen.getByText("Atvertos kortelės (2/72)")).toBeInTheDocument();
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
  });

  it("shows 0/72 and no items for a brand-new journey", () => {
    render(<VisitedGallery visitedCards={[]} totalBoardSize={72} />);

    expect(screen.getByText("Atvertos kortelės (0/72)")).toBeInTheDocument();
    expect(screen.queryAllByRole("listitem")).toHaveLength(0);
  });
});
