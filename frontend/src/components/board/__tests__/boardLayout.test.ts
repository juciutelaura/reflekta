import { describe, it, expect } from "vitest";
import {
  BOARD_COLUMNS,
  BOARD_ROWS,
  BOARD_SIZE,
  ICON_KEYS,
  cellGridPosition,
  iconForCard,
  cardImageSrc,
  animationSteps,
} from "../boardLayout";

describe("boardLayout", () => {
  it("has a board of exactly 72 cells", () => {
    expect(BOARD_COLUMNS * BOARD_ROWS).toBe(72);
    expect(BOARD_SIZE).toBe(72);
  });

  describe("cellGridPosition", () => {
    it("places position 0 at the bottom-left", () => {
      expect(cellGridPosition(0)).toEqual({ row: 7, column: 0 });
    });

    it("places position 8 (end of the first band) at the bottom-right", () => {
      expect(cellGridPosition(8)).toEqual({ row: 7, column: 8 });
    });

    it("reverses direction for the second band (position 9)", () => {
      expect(cellGridPosition(9)).toEqual({ row: 6, column: 8 });
    });

    it("places position 17 (end of the second band) back at the left", () => {
      expect(cellGridPosition(17)).toEqual({ row: 6, column: 0 });
    });

    it("places the last position (71) at the top-left", () => {
      expect(cellGridPosition(71)).toEqual({ row: 0, column: 0 });
    });
  });

  describe("iconForCard", () => {
    it("returns a key from the fixed icon set", () => {
      const icon = iconForCard("11111111-1111-1111-1111-111111111111");
      expect(ICON_KEYS).toContain(icon);
    });

    it("is stable for the same card id", () => {
      const id = "9c3f2a10-aaaa-bbbb-cccc-000000000000";
      expect(iconForCard(id)).toBe(iconForCard(id));
    });

    it("does not throw and returns a valid key for an uppercase-hex id", () => {
      const icon = iconForCard("ABCDEF12-3456-7890-ABCD-EF1234567890");
      expect(ICON_KEYS).toContain(icon);
    });
  });

  describe("animationSteps", () => {
    it("computes a simple non-wrapping sequence", () => {
      expect(animationSteps(10, 3)).toEqual([11, 12, 13]);
    });

    it("wraps around the 72-cell boundary", () => {
      expect(animationSteps(70, 5)).toEqual([71, 0, 1, 2, 3]);
    });

    it("treats the pre-first-roll start position as 0, matching the backend", () => {
      expect(animationSteps(0, 1)).toEqual([1]);
    });
  });

  describe("cardImageSrc", () => {
    it("maps board position 0 to the 1-indexed, zero-padded file name", () => {
      expect(cardImageSrc(0)).toBe("/cards/01.png");
    });

    it("pads double-digit positions the same way", () => {
      expect(cardImageSrc(70)).toBe("/cards/71.png");
    });
  });
});
