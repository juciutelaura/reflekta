import { render, fireEvent } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { CardArt } from "../CardArt";

describe("CardArt", () => {
  it("renders an img pointing at the card's position file", () => {
    render(<CardArt boardPosition={0} cardId="11111111-1111-1111-1111-111111111111" />);

    const { container } = render(
      <CardArt boardPosition={0} cardId="11111111-1111-1111-1111-111111111111" />,
    );

    const img = container.querySelector("img") as HTMLImageElement;
    expect(img.src).toContain("/cards/01.png");
  });

  it("falls back to the placeholder icon when the image fails to load", () => {
    const { container } = render(
      <CardArt boardPosition={0} cardId="11111111-1111-1111-1111-111111111111" />,
    );

    fireEvent.error(container.querySelector("img") as HTMLImageElement);

    expect(container.querySelector("img")).not.toBeInTheDocument();
    expect(document.querySelector("svg")).toBeInTheDocument();
  });
});
