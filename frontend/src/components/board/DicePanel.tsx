interface DicePanelProps {
  onRoll: () => void;
  isRolling: boolean;
  disabled: boolean;
}

export function DicePanel({ onRoll, isRolling, disabled }: DicePanelProps) {
  return (
    <div className="dice-panel">
      <button
        type="button"
        className={`dice-button${isRolling ? " dice-button--rolling" : ""}`}
        onClick={onRoll}
        disabled={isRolling || disabled}
        aria-label="Roll the dice"
      >
        🎲
      </button>
      <span className="dice-panel__hint">{isRolling ? "Rolling…" : "Roll the dice"}</span>
    </div>
  );
}
