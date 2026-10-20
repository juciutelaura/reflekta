import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { DicePanel } from "../DicePanel";

describe("DicePanel", () => {
  it("calls onRoll when clicked", () => {
    const onRoll = vi.fn();
    render(<DicePanel onRoll={onRoll} isRolling={false} disabled={false} />);

    fireEvent.click(screen.getByRole("button", { name: /roll the dice/i }));

    expect(onRoll).toHaveBeenCalledTimes(1);
  });

  it("disables the button while rolling", () => {
    const onRoll = vi.fn();
    render(<DicePanel onRoll={onRoll} isRolling={true} disabled={false} />);

    const button = screen.getByRole("button", { name: /roll the dice/i });
    expect(button).toBeDisabled();
    fireEvent.click(button);
    expect(onRoll).not.toHaveBeenCalled();
  });

  it("disables the button when disabled is true even if not rolling", () => {
    const onRoll = vi.fn();
    render(<DicePanel onRoll={onRoll} isRolling={false} disabled={true} />);

    expect(screen.getByRole("button", { name: /roll the dice/i })).toBeDisabled();
  });
});
