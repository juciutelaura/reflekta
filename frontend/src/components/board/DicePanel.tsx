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
        <svg
          className="dice-button__icon"
          viewBox="0 0 24 24"
          width="28"
          height="28"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          aria-hidden="true"
        >
          <rect x="4" y="4" width="16" height="16" rx="3" />
          <circle cx="8.5" cy="8.5" r="1.3" fill="currentColor" stroke="none" />
          <circle cx="15.5" cy="8.5" r="1.3" fill="currentColor" stroke="none" />
          <circle cx="12" cy="12" r="1.3" fill="currentColor" stroke="none" />
          <circle cx="8.5" cy="15.5" r="1.3" fill="currentColor" stroke="none" />
          <circle cx="15.5" cy="15.5" r="1.3" fill="currentColor" stroke="none" />
        </svg>
      </button>
      <span className="dice-panel__hint">{isRolling ? "Rolling…" : "Roll the dice"}</span>
    </div>
  );
}
