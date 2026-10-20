import type { IconKey } from "./boardLayout";

interface BoardIconProps {
  icon: IconKey;
}

export function BoardIcon({ icon }: BoardIconProps) {
  return (
    <svg
      className="board-icon"
      viewBox="0 0 24 24"
      width="20"
      height="20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      aria-hidden="true"
    >
      {renderShape(icon)}
    </svg>
  );
}

function renderShape(icon: IconKey) {
  switch (icon) {
    case "circle":
      return <circle cx="12" cy="12" r="7" />;
    case "leaf":
      return <path d="M5 19C5 10 10 5 19 5C19 14 14 19 5 19Z" />;
    case "spiral":
      return <path d="M12 12c0-2 2-3 3-1s0 5-3 5-5-2-5-5 2-6 6-6 7 3 7 7" />;
    case "wave":
      return <path d="M3 14c2-4 4-4 6 0s4 4 6 0 4-4 6 0" />;
    case "square":
      return <rect x="6" y="6" width="12" height="12" />;
    case "triangle":
      return <path d="M12 5L19 19H5Z" />;
    case "dot-cluster":
      return (
        <>
          <circle cx="8" cy="9" r="1.5" fill="currentColor" />
          <circle cx="16" cy="9" r="1.5" fill="currentColor" />
          <circle cx="12" cy="16" r="1.5" fill="currentColor" />
        </>
      );
    case "arc":
      return <path d="M5 17A10 10 0 0 1 19 17" />;
    case "cross":
      return <path d="M12 5V19M5 12H19" />;
    case "diamond":
      return <path d="M12 4L20 12L12 20L4 12Z" />;
    case "line":
      return <path d="M5 19L19 5" />;
    case "grid":
      return (
        <>
          <path d="M9 4V20" />
          <path d="M15 4V20" />
          <path d="M4 9H20" />
          <path d="M4 15H20" />
        </>
      );
  }
}
