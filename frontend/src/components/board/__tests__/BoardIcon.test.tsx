import { render } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { BoardIcon } from "../BoardIcon";
import { ICON_KEYS } from "../boardLayout";

describe("BoardIcon", () => {
  it.each(ICON_KEYS)("renders an svg for icon '%s'", (icon) => {
    const { container } = render(<BoardIcon icon={icon} />);
    expect(container.querySelector("svg")).toBeInTheDocument();
  });
});
