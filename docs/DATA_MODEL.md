# Reflekta — Data Model

## 1. Purpose

This document defines the core data model for Reflekta MVP.

The model should support:

* user accounts and application profiles
* reflection intentions
* journeys
* game state
* cards
* sessions
* user reflections
* AI conversations
* session summaries
* long-term memories
* future semantic retrieval through PostgreSQL + pgvector

The model should remain simple and domain-oriented.

Database implementation details such as exact indexes, EF Core configurations, migrations, and SQL should be decided during implementation.

---

# 2. Data Modeling Principles

## 2.1 Backend owns application state

The ASP.NET Core backend is the authoritative owner of application data.

The frontend must not determine or modify authoritative game state directly.

---

## 2.2 User-owned data must be isolated

A user must only be able to access their own:

* intentions
* journeys
* sessions
* reflections
* conversations
* summaries
* memories

Authorization must be enforced by the backend.

---

## 2.3 Game data and AI data are separate concerns

Game state represents deterministic product state: dice results, card selection, journey lifecycle.

AI-derived data represents generated language content: clarifications, session summaries, and (later) memory candidates and journey analysis.

These two categories are stored in separate entities and are never merged into a single authoritative record. A generated summary or memory never overwrites or changes a journey's deterministic state.

---

# 3. Core Entities

```text
User
Intention
Journey
Card
PlayedCard            (a card encountered during a journey)
Reflection
ConversationMessage
SessionSummary
Memory                (post-MVP)
```

Each entity is described below. Exact column types, indexes, and migrations are implementation details left to the backend team.

---

# 4. User

Represents an application user linked to their authentication identity.

```text
User
- id
- external_auth_id       (Clerk subject identifier)
- created_at
- updated_at
```

The backend never trusts a user id supplied by the client. It always resolves the user from the validated authentication token (see ARCHITECTURE.md §5, "Authentication and authorization"; NFR-SEC-002).

---

# 5. Intention

Represents what the user wants to explore during a journey.

```text
Intention
- id
- user_id
- original_text
- clarified_text          (nullable)
- created_at
```

An intention belongs to exactly one journey (INT-002).

AI clarification does not replace `original_text` — the clarified version is stored alongside it. The user remains the final authority over the intention (INT-006, INT-007; see AI_SPECIFICATION.md §11).

---

# 6. Journey

The main container for a user's reflection experience (JRN-001..008).

```text
Journey
- id
- user_id
- intention_id
- status                  ('active' | 'completed')
- started_at
- completed_at            (nullable)
```

A journey owns an ordered sequence of `PlayedCard` records, which together represent the game history and the reflections/conversations attached to it.

Some documents refer to a journey informally as a "session" (see PRODUCT_REQUIREMENTS.md §14, "journey/session"). The MVP model does not introduce a separate `Session` entity — a journey together with its `status` fulfills that role. This keeps the model simple until a real product need for a distinct session concept appears.

---

# 7. Card

Static, authored reflection content (CARD-001..007).

```text
Card
- id
- title
- wisdom_text
- themes                  (e.g. ["Control", "Fear"])
- board_position           (position used by the deterministic game rules)
```

Cards are seeded product/domain data. They are not created or edited through normal application user flows, and their wisdom text is never rewritten by the LLM during gameplay (CARD-005, CARD-006).

---

# 8. PlayedCard

Represents one card encountered during a journey: the deterministic dice/game outcome, plus a link to the user's reflection and conversation for that card.

```text
PlayedCard
- id
- journey_id
- card_id
- sequence_number          (order within the journey)
- dice_result
- created_at
```

This is where GAME-007 ("persist the dice result and resulting card as part of the journey history") is satisfied. Dice result and card selection are written by ASP.NET Core only — the AI service never writes to this record (GAME-004, GAME-005).

---

# 9. Reflection

The user's written reaction to a played card (REF-001..005).

```text
Reflection
- id
- played_card_id
- text
- created_at
```

A reflection belongs to exactly one `PlayedCard`.

---

# 10. ConversationMessage

A single message in the AI reflection dialogue for a played card (CONV-001..005).

```text
ConversationMessage
- id
- played_card_id
- role                     ('user' | 'assistant')
- content
- created_at
```

Messages are ordered by `created_at` within a `played_card_id`. This is the "recent conversation" context referenced in AI_SPECIFICATION.md §6–7.

---

# 11. SessionSummary

The AI-generated summary produced when a journey is completed (SES-001..006).

```text
SessionSummary
- id
- journey_id
- summary_text
- themes                   (nullable list)
- created_at
```

The summary is generated by the Python AI service and persisted by ASP.NET Core (see ARCHITECTURE.md §12, Data Ownership). It is stored as AI-derived data, distinct from the user's own reflections, and must distinguish what the user explicitly said from possible observed patterns (SES-005).

---

# 12. Memory (post-MVP)

Long-term, cross-journey observations (MEM-001..004). Not required for the MVP, but the model reserves space for it so it can be introduced later without restructuring the schema.

```text
Memory
- id
- user_id
- content
- status                   ('candidate' | 'confirmed' | 'rejected' | 'archived')
- source_journey_id         (provenance)
- created_at
```

A memory always carries provenance back to the journey it was derived from (AI_SPECIFICATION.md §16–17), and is never treated as objective truth about the user until confirmed.

---

# 13. Relationships

```text
User 1───* Intention
User 1───* Journey
User 1───* Memory                 (post-MVP)

Intention 1───1 Journey

Journey 1───* PlayedCard
Journey 1───1 SessionSummary      (created on completion)

Card 1───* PlayedCard

PlayedCard 1───1 Reflection
PlayedCard 1───* ConversationMessage
```

---

# 14. Data Ownership

See ARCHITECTURE.md §12 for the authoritative ownership table describing which system is allowed to write which data.

This document defines the *shape* of the data. ARCHITECTURE.md defines *who may write it*. The two must stay consistent — in particular, `PlayedCard.dice_result` and `PlayedCard.card_id` are ASP.NET Core-owned and must never be written by the Python AI service.

---

# 15. Future: pgvector

When semantic retrieval becomes necessary (see ARCHITECTURE.md §10, PRODUCT_REQUIREMENTS.md §19), embeddings can be added as columns on `SessionSummary` and `Memory` rather than introducing new tables or a separate vector database.

---

# 16. Guiding Principle

> The data model should make the deterministic/AI-derived boundary visible in the schema itself, not only in application code.
