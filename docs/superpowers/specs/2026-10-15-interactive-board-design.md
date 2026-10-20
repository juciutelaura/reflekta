# Interactive Journey Board — Design

## Goal

Replace the active-journey page's plain text flow ("Roll" button → single card as text → reflection form)
with an interactive visual board: a 72-cell snake-path grid of **eye-shaped cells**, a large dice button
that rolls and animates the token across the rolled number of cells, a gallery of visited cards, and the
existing reflection/AI conversation relocated into a side panel. Visual direction is set by two reference
images the user supplied: `Minimalist Lithuanian Journey Game UI.png` (overall minimalist cream/black
dashboard — board on the left, stacked right sidebar) and `Tavo kelionę_ Akių žaidimo lenta.png` (the board
cells themselves: each one is an eye — closed (just an eyelid/lashes glyph) until the player lands on it,
then it opens to reveal that card's icon inside the iris. No separate "you are here" token marker is used —
the most recently opened eye *is* the current position, confirmed with the user).

## Context (what exists today)

- `frontend/src/pages/JourneyPage.tsx` renders the active journey as a vertical `.stack`: a "Roll" button,
  then the rolled card as plain text (`<article class="card">`), then a reflection `<textarea>`, then the
  existing `ReflectionConversation` component, then "Continue"/"Complete" buttons. No visual board exists.
- The 72-card deck is seeded from `backend/src/Reflekta.Api/Data/reflekta_72_cards.json` via `CardSeeder`.
  Every card has a **unique** `theme` string (verified: 72 cards, 72 distinct themes) — themes cannot be
  used to group cards into a small shared icon set.
- `Card.BoardPosition` (0–71) already exists in the backend `Card` model and is used server-side by
  `CardSelectionService` to compute the next card via `(currentPosition + diceResult) % boardSize`. It is
  **not** currently exposed in any API response.
- `backend/src/Reflekta.Api/Controllers/JourneysController.cs` defines:
  - `RollResultDto(int DiceResult, Guid CardId, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, int SequenceNumber)`
  - `CurrentCardDto` (extends the same shape, adds `ReflectionText`, `Messages`)
  - `PlayedCardDetailDto(Guid Id, int SequenceNumber, int DiceResult, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, string? ReflectionText)`
  None of these expose `BoardPosition` or the journey's intention text.
- Existing visual language (`frontend/src/index.css`, `App.css`): cream background (`--color-bg: #f6f1e7`),
  dark ink text (`--color-ink: #16202a`), serif accent font (`--font-serif: "Newsreader"`) for card wisdom
  text, sans-serif UI font (`--font-sans: "Hanken Grotesk"`). This already closely matches the reference
  image's minimalist monochrome style — the board page should reuse these tokens, not introduce a new
  palette.
- `frontend/src/components/ReflectionConversation.tsx` already implements the reflection + AI chat
  behavior and must be reused as-is (relocated, not rewritten) — no AI prompt or backend AI behavior
  changes are part of this design.

## Out of scope (explicitly deferred, confirmed with the user)

- Real unique illustrations per card (72 custom artworks). Placeholder icons are used instead; swapping
  in real art later is a content change, not a code change (see "Placeholder visuals" below).
- A proactive AI that greets the player per-position without a submitted reflection first. The existing
  reactive flow (reflection submitted → AI replies) is kept, just moved to the sidebar.
- The decorative "Ką norėtum pamatyti kitaip?" top search input from the reference image — the user asked
  for the journey's intention text there instead.
- Any backend endpoint beyond small DTO field additions. No new controller actions, no new tables, no
  migrations, no AI service changes.

## Backend changes

Three additive DTO field changes in `JourneysController.cs`. All values already exist in the domain model
(`Card.BoardPosition`, `Journey.Intention.ClarifiedText ?? OriginalText` — same intention-text resolution
already used by `JourneyDetailDto`/`JourneySummaryDto` elsewhere in this file) — this is exposure, not new
computation:

1. `RollResultDto` gains `int BoardPosition` (and therefore so does `CurrentCardDto`, which extends it).
   Set from the `Card.BoardPosition` of the card returned by `ICardSelectionService.SelectNext` in both
   the `Roll` and `GetCurrentCard` actions.
2. `PlayedCardDetailDto` gains `int BoardPosition`, set the same way in `GetById` (history/detail) and
   wherever played cards are projected for the active journey.
3. `CurrentCardDto` gains `string IntentionText`, resolved identically to how `JourneyDetailDto` already
   resolves it (loaded from the journey's `Intention` navigation).

No other backend files change. No migration needed (the column already exists).

## Frontend architecture

### New components (under `frontend/src/components/board/`)

- **`BoardGrid.tsx`** — renders the 72-cell snake-path grid of eye cells. Props: `openPositions: { boardPosition: number; cardId: string }[]`
  (every previously played card, each rendered as an open eye showing its placeholder icon — this already
  includes the current position, since the current position is just the most recently played card; no
  separate "current" prop or visual state exists) and `animatingPosition: number | null` (the cell the
  token is transiently passing through mid-roll, rendered as a brief flash — see "Dice and animation").
  Pure presentational; no data fetching.
- **`DicePanel.tsx`** — the large dice button + roll-in-progress state. Props: `onRoll: () => Promise<{ boardPosition: number; diceResult: number }>`,
  `disabled: boolean`, `currentPosition: number` (the starting point for computing the animation's step
  sequence — see "Dice and animation"). Owns the step-by-step animation by holding local animation state,
  then calls back up once the animation completes.
- **`VisitedGallery.tsx`** — "Mano kelionė" thumbnail list. Props: `visitedCards: { cardId: string; cardTitle: string }[]`,
  `totalBoardSize: number` (constant `72`, see below). Renders `x/72` and up to the existing cards;
  no pagination needed at 72 max.
- **`boardLayout.ts`** — pure functions, unit-testable in isolation:
  - `BOARD_COLUMNS = 9`, `BOARD_ROWS = 8` (`9 × 8 = 72`, matching the seeded deck size exactly).
  - `cellGridPosition(boardPosition: number): { row: number; column: number }` — snake-path math (see below).
  - `iconForCard(cardId: string): IconKey` — deterministic placeholder icon selection (see below).

### `JourneyPage.tsx` restructuring

Keep the existing data-fetching/state logic (`getCurrentCard`, `rollDice`, `submitReflection`,
`completeJourney` calls and their state) — only the render output changes:

```
<div className="board-page">
  <header className="board-page__intention">{card?.intentionText}</header>
  <div className="board-page__layout">
    <BoardGrid openPositions={...} animatingPosition={...} />
    <aside className="board-page__sidebar">
      <DicePanel onRoll={handleRoll} disabled={isRolling} />
      <VisitedGallery visitedCards={...} totalBoardSize={72} />
      <section className="board-page__reflection">
        {/* existing reflection textarea + ReflectionConversation, unchanged */}
      </section>
    </aside>
  </div>
</div>
```

`visitedPositions`/`visitedCards` are derived from the journey's played cards. The active-journey screen
does not currently fetch the full played-card list (only the current card) — it needs it to render already
-visited cells. Reuse the existing `apiClient.getJourneyDetail(journeyId)` call (already used by
`JourneyDetailPage`, already returns `playedCards` with the needed fields now that `BoardPosition` is
added to `PlayedCardDetailDto`) alongside the existing `getCurrentCard` call, fetched in parallel on mount
and refetched after each roll/reflection submit. No new API client method is needed — `getJourneyDetail`
already exists in `apiClient.ts`.

### Responsive layout

CSS Grid at the `.board-page__layout` level: two columns (`board` / `sidebar`) above a `768px` breakpoint,
single column (sidebar stacks below the board, sidebar's three blocks remain in their existing vertical
order) below it — this is the "Option A" stacked behavior the user already approved as the mobile
fallback for "Option B".

## Board layout math (snake path)

72 cells, `BOARD_COLUMNS = 9`, `BOARD_ROWS = 8`. `BoardPosition` is 0-indexed (0–71, matching the existing
backend convention from `CardSeeder`). Band 0 (positions 0–8) is the bottom visual row and runs
left-to-right; band 1 (positions 9–17) is the row above it and runs right-to-left; bands continue
alternating direction up the board:

```ts
function cellGridPosition(boardPosition: number): { row: number; column: number } {
  const band = Math.floor(boardPosition / BOARD_COLUMNS);
  const indexInBand = boardPosition % BOARD_COLUMNS;
  const column = band % 2 === 0 ? indexInBand : BOARD_COLUMNS - 1 - indexInBand;
  const row = BOARD_ROWS - 1 - band; // band 0 = bottom row
  return { row, column };
}
```

This is a pure function covered directly by unit tests (Task-level detail in the implementation plan).

## Cell states: closed and open eyes

Each of the 72 cells is one of exactly two states — no third "current/token" state exists (confirmed with
the user: the most recently opened eye already reads as "you are here", no extra ring or glow needed):

- **Closed** (default): an eyelid + lashes glyph, no icon visible. This is every `boardPosition` not in
  `openPositions`.
- **Open**: the eyelid is raised; the card's placeholder icon (see below) renders inside a circular/iris
  mask. This is every `boardPosition` in `openPositions` — i.e. every card the player has already landed
  on, current position included.

A brand-new journey (no cards played yet) renders all 72 cells closed.

## Placeholder visuals

A fixed local set of ~12 abstract, neutral SVG icons (`frontend/src/components/board/icons/`, e.g. circle,
leaf, spiral, wave, square, triangle, dot-cluster, arc, cross, diamond, line, grid — no attempt at semantic
matching, since themes are all unique and cannot be grouped), clipped into the open eye's iris circle.
Selection is deterministic per card:

```ts
function iconForCard(cardId: string): IconKey {
  const hash = cardId.split("-").join("").slice(0, 8);
  const index = parseInt(hash, 16) % ICON_SET.length;
  return ICON_SET[index];
}
```

(`cardId` is a GUID string; using its hex prefix as a number is sufficient for a stable, evenly-distributed
placeholder pick — no cryptographic property is needed here.) The same function is used for both the board
cell and the gallery thumbnail, so a given card always shows the same placeholder icon in both places.
Swapping in real illustrations later means replacing `iconForCard`'s return with a real asset lookup — the
call sites in `BoardGrid`/`VisitedGallery` do not change.

## Dice and animation

1. User clicks the dice button in `DicePanel`. Button becomes disabled; dice shows a rolling state (CSS
   animation on the dice glyph itself, no new state needed beyond a boolean).
2. `onRoll` calls the existing `apiClient.rollDice(journeyId)`, which already returns `diceResult` and
   (after the backend change above) `boardPosition`.
3. `DicePanel` computes the sequence of intermediate positions from its `currentPosition` prop to the new
   `boardPosition` (stepping `diceResult` times, wrapping via `% 72` exactly as the backend already does —
   the frontend does not need to reimplement the modulo card-selection logic, only replay it visually using
   the two position values it already has: current and new).
4. Each intermediate position (all but the last) is passed up as `animatingPosition` for `~150ms` via
   `setTimeout`, in turn — `BoardGrid` renders that one cell with a brief transient flash (eyelid twitches
   but does not fully open; the icon never appears). These cells stay **closed** once the flash ends,
   since the player only passed over them, not landed on them.
5. The final step sets `animatingPosition` to the new `boardPosition` one last time, but this time it is
   also already present in `openPositions` (the roll response is what adds it there) — so instead of the
   transient flash, `BoardGrid` plays the full closed→open transition on that cell: the eyelid lifts and
   the card's placeholder icon fades in, and the state stays open permanently from then on.
6. Only after that opening transition finishes does `JourneyPage` reveal the card content in the sidebar
   (title, wisdom text, reflection prompt, reflection form) — reusing the existing reveal-after-roll
   behavior, just gated on the animation completing instead of the API response alone.
7. If the roll API call fails, no animation starts; the existing `.error` message renders as it does today.

## Error handling

No new error paths. `BoardGrid`/`VisitedGallery`/`DicePanel` are pure/presentational and do not call the
API directly — `JourneyPage` keeps owning all API calls and the existing `error` state / `.error` class
rendering. The only new failure surface is the added `getJourneyDetail` call on mount (for visited
positions) — on failure it falls back to an empty visited list (board still renders, just without visited
highlights) rather than blocking the page, since the current-card data alone is enough to show the token
and let the user keep playing.

## Testing

**Frontend (Vitest):**
- `boardLayout.test.ts` — `cellGridPosition` for boundary positions (0, 8, 9, 17, 71) covering both scan
  directions; `iconForCard` returns a value from `ICON_SET` and is stable across repeated calls with the
  same `cardId`.
- `BoardGrid.test.tsx` — renders 72 cells; positions in `openPositions` render open with that card's icon;
  all other positions render closed with no icon; `animatingPosition` renders a transient flash without
  opening the cell or showing an icon.
- `DicePanel.test.tsx` — clicking the dice button calls `onRoll`; button is disabled during the roll; the
  computed step sequence wraps correctly across the `% 72` boundary (e.g. `currentPosition: 70`,
  `diceResult: 5` → steps through 71, 0, 1, 2, lands on 3); card reveal callback fires only after the final
  step's full open transition (fake timers).
- `JourneyPage.test.tsx` — update existing assertions for the new DOM structure (intention header present,
  sidebar contains the reflection form after a roll); existing test scenarios (reflect, continue, complete)
  keep their current behavioral assertions, just against the new markup.

**Backend (xUnit):**
- Extend the existing `Roll_...`, `GetCurrentCard`-covering, and `GetById`/history tests in
  `JourneysControllerTests.cs` / `JourneyHistoryTests.cs` with assertions that `BoardPosition` in the
  response matches the seeded card's `BoardPosition`, and that `CurrentCardDto.IntentionText` matches the
  journey's intention text. No new test files needed — additive assertions on already-covered endpoints.

## Self-review

- **Placeholder scan:** no TBD/TODO; the one open question (theme-based icon grouping) was found to be
  invalid during this review and resolved with the user (hash-based deterministic icon pick) before this
  document was written.
- **Internal consistency:** `BOARD_COLUMNS × BOARD_ROWS = 72` matches the seeded deck size exactly (no
  filler cells, no overflow). `BoardPosition` stays 0-indexed end-to-end (backend `Card.BoardPosition`,
  API DTOs, frontend grid math) — no off-by-one translation layer needed anywhere.
- **Scope:** single page redesign + three small additive DTO fields. No backend subsystem, no AI changes,
  no new persistence. Right-sized for one implementation plan.
- **Revision (2026-10-19):** user supplied a second reference image (`Tavo kelionę_ Akių žaidimo lenta.png`)
  showing eye-shaped cells (closed/open) instead of plain square tiles, and confirmed no separate "current
  position" marker is needed beyond the most recently opened eye. Updated: cell visual model (closed/open
  eye instead of square tile), `BoardGrid` props (`openPositions`/`animatingPosition` replace
  `currentPosition`/`visitedPositions`), and the dice animation section (transient flash for passed-over
  cells vs. a permanent closed→open transition for the landing cell). Board dimensions, snake-path math,
  backend DTO changes, and the hash-based placeholder icon system are unchanged by this revision.
