# Reflekta — Product Requirements

**Version:** 0.1
**Status:** Draft / MVP Definition

---

# 1. Purpose

This document defines the functional and product requirements for the first version of Reflekta.

It describes what Reflekta must do from the user's perspective.

It does not define detailed technical implementation. Technical decisions belong in `ARCHITECTURE.md` and `TECH_STACK.md`.

The purpose of this document is to provide a clear implementation boundary for the development team and coding agents.

---

# 2. MVP Goal

The MVP must validate the core Reflekta experience:

> A user brings a question or intention, receives an unexpected reflection stimulus through the game, reflects on it, and explores their thoughts through an AI-guided conversation.

The MVP should be small enough to build and deploy, but complete enough for a real person to use from beginning to end.

---

# 3. User Roles

## 3.1 User

The MVP has one primary user role:

**User**

A user can:

* create an account;
* create an intention;
* start a reflection journey;
* interact with cards;
* write reflections;
* communicate with the AI facilitator;
* complete sessions;
* view previous journeys.

There are no separate admin, therapist, coach, or psychologist roles in the MVP.

---

# 4. Core User Journey

The primary journey is:

```text
Create account
      ↓
Create intention
      ↓
Optional intention clarification
      ↓
Start journey
      ↓
Roll dice
      ↓
Determine card
      ↓
Display card
      ↓
User reflects
      ↓
AI facilitates reflection
      ↓
Continue journey
      ↓
Next card
      ↓
Complete journey
      ↓
Session summary
      ↓
Journey history
```

The journey should feel continuous rather than like a collection of unrelated screens.

---

# 5. Authentication Requirements

### AUTH-001

The system shall allow a user to create an account.

### AUTH-002

The system shall allow an authenticated user to sign in.

### AUTH-003

The system shall associate journeys, reflections, conversations, and summaries with the authenticated user.

### AUTH-004

A user shall only be able to access their own private reflection data.

### AUTH-005

Unauthenticated users shall not be able to access private journey data.

---

# 6. Intention Requirements

An intention is the question, topic, or area that the user wants to explore during a journey.

### INT-001

The system shall allow the user to create an intention before starting a journey.

### INT-002

The intention shall be stored as part of the journey.

### INT-003

The user shall be able to review their intention during the journey.

### INT-004

The system may offer AI-assisted intention clarification.

### INT-005

AI clarification shall help the user express their intention more clearly.

### INT-006

AI clarification shall not tell the user what their intention should be.

### INT-007

The user shall remain in control of the final intention.

---

# 7. Journey Requirements

A journey is the main container for a user's reflection experience.

### JRN-001

The user shall be able to start a new journey.

### JRN-002

A journey shall have a starting intention.

### JRN-003

A journey shall contain an ordered sequence of played cards.

### JRN-004

A journey shall preserve the user's reflections.

### JRN-005

A journey shall preserve the AI conversation associated with each reflection.

### JRN-006

A journey shall have a lifecycle state.

At minimum:

```text
Active
Completed
```

### JRN-007

The user shall be able to continue an active journey.

### JRN-008

A completed journey shall remain available in the user's history.

---

# 8. Dice and Game Mechanics

The game mechanics are deterministic application logic and shall not be controlled by the AI.

### GAME-001

The user shall be able to roll a dice during an active journey.

### GAME-002

The dice roll shall produce a valid result according to the configured game rules.

### GAME-003

The dice result shall determine the next game position according to deterministic rules.

### GAME-004

The system shall determine the card independently of the AI.

### GAME-005

The AI shall not choose a card based on the user's intention, reflection, emotions, or conversation.

### GAME-006

The same game state and configured rules shall produce a reproducible card-selection result when deterministic replay is required.

### GAME-007

The system shall persist the dice result and resulting card as part of the journey history.

---

# 9. Card Requirements

Cards are the primary reflection stimuli in Reflekta.

### CARD-001

Each card shall have a unique identifier.

### CARD-002

Each card shall contain authored reflection/wisdom content.

### CARD-003

Each card may have one or more associated themes.

Examples:

* Fear
* Control
* Attachment
* Change
* Acceptance
* Identity
* Ambition
* Uncertainty
* Compassion


Card-*

## MVP Card Content

For Phase 1, Reflekta uses six approved pilot cards.

These cards are authoritative product content for the six-card MVP. Claude Code must not replace, rewrite, reinterpret, or invent their wisdom text. The seed data must reproduce the approved content exactly.

### Card 1 — Stebėtojas

**Theme:** Sąmoningumas

**Wisdom text:**

> Ne kiekviena mintis reikalauja tavo atsakymo. Kartais pirmas žingsnis yra pastebėti, kas vyksta tavo viduje, nebandant to pakeisti.

**Reflection prompt:**

> Ką pastebi savyje, kai tiesiog stebi savo mintis, jų nevertindamas?

**Board position:** 0

---

### Card 2 — „Aš“

**Theme:** Tapatybė

**Wisdom text:**

> Mes dažnai kalbame apie save taip, lyg jau tiksliai žinotume, kas esame. Tačiau dalis to, ką vadiname „aš“, gali būti istorijos, kurias apie save kartojame.

**Reflection prompt:**

> Kuri istorija apie save tau atrodo tokia pažįstama, kad retai ją kvestionuoji?

**Board position:** 1

---

### Card 3 — Už durų

**Theme:** Baimė

**Wisdom text:**

> Baimė dažnai kalba apie tai, kas gali nutikti. Tačiau kartais ji daugiau pasako apie tai, ką stengiamės apsaugoti.

**Reflection prompt:**

> Jeigu pažvelgtum už savo baimės — ką ji galbūt bando apsaugoti?

**Board position:** 2

---

### Card 4 — Paleidimas

**Theme:** Kontrolė

**Wisdom text:**

> Noras kontroliuoti gali suteikti saugumo jausmą. Tačiau ne viskas, kas vyksta tavo gyvenime, yra tavo rankose.

**Reflection prompt:**

> Ko šiandien labiausiai stengiesi kontroliuoti?

**Board position:** 3

---

### Card 5 — Tarp

**Theme:** Pokytis

**Wisdom text:**

> Pokytis ne visada prasideda nuo aiškaus sprendimo. Kartais pirmiausia atsiranda jausmas, kad tai, kas anksčiau tiko, nebetinka, nors dar nežinai, kas bus toliau.

**Reflection prompt:**

> Kas tavo gyvenime šiuo metu atrodo tarsi „tarp“ — tarp to, kas buvo, ir to, kas dar tik atsiranda?

**Board position:** 4

---

### Card 6 — Nežinau

**Theme:** Nežinomybė

**Wisdom text:**

> Nežinojimas gali atrodyti kaip problema, kurią reikia kuo greičiau išspręsti. Tačiau kartais atsakymo paieška per anksti neleidžia pamatyti to, kas dar tik formuojasi.

**Reflection prompt:**

> Kurioje savo gyvenimo vietoje tau sunkiausia pasakyti „aš dar nežinau“?

**Board position:** 5

### Content rules

* These six cards are the approved Phase 1 pilot content.
* Preserve the Lithuanian title, wisdom text, reflection prompt, theme, and board position exactly.
* Do not generate alternative card copy unless explicitly instructed by the product owner.
* Phase 1 contains six cards only.
* Additional cards may be authored later and are not part of the current Phase 1 scope.


### CARD-004

The system shall display the selected card to the user.

### CARD-005

The card's authored core content shall remain stable during gameplay.

### CARD-006

The LLM shall not rewrite the card's core wisdom content during gameplay.

### CARD-007

The card shall be presented as a reflection stimulus rather than a prediction or definitive answer.

---

# 10. User Reflection Requirements

### REF-001

The system shall provide the user with an opportunity to reflect on the card.

### REF-002

The user shall be able to submit a written reflection.

### REF-003

The reflection shall be associated with the current card and journey.

### REF-004

The user shall be able to see their submitted reflection in the context of the current journey.

### REF-005

The system shall preserve the reflection as part of the user's journey history.

---

# 11. AI Reflection Facilitator

The AI reflection facilitator is responsible for guiding the conversation around the user's reflection.

### AI-001

The AI shall act as a reflection facilitator rather than an oracle or authority.

### AI-002

The AI shall begin with an open, non-leading question when appropriate.

Examples:

* "What came to mind when you read this card?"
* "Which part of the card stood out to you?"
* "What was your first thought when you read it?"

### AI-003

The AI shall ask one primary question at a time.

### AI-004

The AI shall respond to the user's actual reflection rather than following a rigid predetermined conversation script.

### AI-005

The AI shall gradually deepen the conversation when the user's responses provide an opportunity for deeper reflection.

### AI-006

The AI shall allow the user to disagree with an interpretation.

### AI-007

The AI shall use tentative language when presenting possible interpretations.

### AI-008

The AI shall not assume that the card is related to the user's original intention.

### AI-009

The AI shall not force a connection between the card and the user's intention.

### AI-010

If the user does not see a connection with the card, the AI shall accept that response.

### AI-011

The AI shall not claim to know the user's internal emotional state unless the user explicitly describes it.

### AI-012

The AI shall not diagnose the user.

### AI-013

The AI shall not present itself as a therapist or mental-health professional.

### AI-014

The AI shall not make predictions about the user's future.

### AI-015

The AI shall not present card outcomes as supernatural messages.

### AI-016

The AI shall not tell the user what they must think.

### AI-017

The AI shall not make the user accept an interpretation.

### AI-018

The AI shall prioritize reflection over unsolicited advice.

### AI-019

The AI shall preserve awareness of the user's original intention when it is relevant to the conversation.

### AI-020

The AI shall focus primarily on the user's current reflection and conversation context rather than repeatedly restating the entire journey history.

---

# 12. AI Conversation Safety Boundary

Reflekta is a self-reflection product, not a clinical mental-health product.

### SAFE-001

The system shall not represent Reflekta as a diagnostic or treatment system.

### SAFE-002

The AI shall not diagnose mental-health conditions.

### SAFE-003

The AI shall not claim clinical authority.

### SAFE-004

The AI shall not make deterministic psychological claims about the user.

### SAFE-005

The AI shall not use mystical, supernatural, prophetic, or destiny-based claims as factual explanations.

### SAFE-006

The system shall have a defined safety behavior for situations where a user expresses serious distress or potential crisis.

The exact safety behavior will be defined before production release.

---

# 13. Conversation Persistence

### CONV-001

The system shall preserve the AI conversation associated with the current reflection.

### CONV-002

Conversation messages shall be associated with the correct user.

### CONV-003

Conversation messages shall be associated with the correct journey.

### CONV-004

Conversation messages shall be associated with the relevant card/reflection context where applicable.

### CONV-005

The user shall be able to continue an active reflection conversation.

---

# 14. Session Completion

### SES-001

The user shall be able to complete a journey/session.

### SES-002

The system shall generate a session summary after completion.

### SES-003

The summary shall reflect what actually emerged during the user's journey.

### SES-004

The summary shall not invent insights that were not supported by the user's reflections.

### SES-005

The summary shall distinguish between what the user explicitly expressed and possible patterns or interpretations.

### SES-006

The completed session shall be stored in the user's history.

---

# 15. Journey History

### HIS-001

The user shall be able to view previous completed journeys.

### HIS-002

The history shall show enough information for the user to identify a journey.

Potential information includes:

* date;
* intention;
* number of cards;
* summary;
* themes.

### HIS-003

The user shall be able to open a previous journey.

### HIS-004

The user shall be able to review their previous reflections.

### HIS-005

The user shall be able to review the session summary.

---

# 16. Long-Term Memory

Long-term memory is not required to prove the basic MVP experience.

However, the system should be designed so that memory can be introduced later.

Potential future memory may include:

* recurring themes;
* recurring questions;
* values;
* fears;
* important reflections;
* changes in perspective;
* user-confirmed insights.

### MEM-001

The MVP shall not require sophisticated long-term memory.

### MEM-002

Future memory must not automatically be treated as objective truth about the user.

### MEM-003

Where appropriate, important memories or insights should be confirmable by the user.

### MEM-004

Memory must remain attributable to the user's previous reflections.

---

# 17. Personalization

Personalization is a future capability.

The MVP should primarily personalize the experience using:

* current intention;
* current card;
* current reflection;
* current conversation;
* current journey state.

Advanced personalization should not be implemented until there is a demonstrated product need.

---

# 18. AI Context Requirements

The AI should receive relevant context rather than an indiscriminate dump of the user's entire history.

Relevant context may include:

* current intention;
* current card;
* current card themes;
* current user reflection;
* current conversation;
* relevant previous session summaries;
* confirmed memories;
* current journey state;
* AI behavior rules.

The system should prefer relevant context over maximum context.

---

# 19. RAG Requirements

RAG is not a mandatory MVP capability.

### RAG-001

The system shall only introduce retrieval when retrieval solves a concrete product requirement.

### RAG-002

Potential future retrieval sources may include:

* card knowledge;
* previous session summaries;
* themes;
* user-confirmed memories;
* previous reflections.

### RAG-003

The system should avoid introducing a separate vector database if PostgreSQL with pgvector is sufficient for the product requirements.

---

# 20. Non-Functional Requirements

## 20.1 Privacy

### NFR-PRIV-001

User reflection data shall be treated as private user data.

### NFR-PRIV-002

Users shall only be able to access their own private reflection data.

### NFR-PRIV-003

The system shall clearly communicate when user content is processed by AI.

### NFR-PRIV-004

The system shall avoid exposing private user content through application logs.

### NFR-PRIV-005

The product should provide a path for users to delete their data before production release.

---

## 20.2 Security

### NFR-SEC-001

Authenticated resources shall require appropriate authorization.

### NFR-SEC-002

User data shall be isolated between users.

### NFR-SEC-003

Secrets and API keys shall not be stored in source control.

### NFR-SEC-004

The AI service shall not receive data belonging to another user.

### NFR-SEC-005

The application shall validate externally supplied input.

---

## 20.3 Reliability

### NFR-REL-001

A temporary AI failure shall not corrupt the user's journey state.

### NFR-REL-002

Game state shall not depend on successful completion of an AI request.

### NFR-REL-003

A failed AI request shall be recoverable without losing the user's reflection.

### NFR-REL-004

Critical game operations shall remain deterministic and independent from LLM behavior.

---

## 20.4 Usability

### NFR-UX-001

The core journey shall be understandable without technical knowledge.

### NFR-UX-002

The user shall always understand what action is expected next.

### NFR-UX-003

The interface shall not overwhelm the user with unnecessary information.

### NFR-UX-004

The reflection experience shall feel conversational rather than like filling out a technical form.

---

# 21. Scope — MVP

The following capabilities are **IN SCOPE**:

```text
✓ Authentication
✓ User profile
✓ Intention creation
✓ Optional AI intention clarification
✓ Journey creation
✓ Dice roll
✓ Deterministic card selection
✓ Static wisdom cards
✓ User reflection
✓ AI reflection conversation
✓ Conversation persistence
✓ Journey persistence
✓ Session completion
✓ AI-generated session summary
✓ Basic journey history
```

---

# 22. Scope — Post MVP

The following capabilities are **NOT REQUIRED for MVP**:

```text
○ Long-term memory
○ Cross-session pattern analysis
○ Advanced RAG
○ Vector search
○ Journey-level AI analysis
○ Advanced personalization
○ Multiple AI agents
○ MCP integrations
○ External tools
○ Social features
○ Native mobile applications
○ Therapist/coach dashboard
○ Subscription/billing
○ Advanced analytics
○ Complex recommendation systems
```

These may be added later when a concrete product requirement justifies them.

---

# 23. Explicitly Out of Scope

The following are intentionally excluded from Reflekta:

```text
✗ Fortune telling
✗ Future prediction
✗ Astrology
✗ Karma/destiny claims
✗ Divine messages
✗ Supernatural card powers
✗ Diagnosis
✗ Mental-health treatment
✗ AI deciding what a card means for the user
✗ AI selecting cards based on what it thinks the user needs
✗ AI determining who the user "really is"
✗ AI making major life decisions for the user
```

---

# 24. MVP Acceptance Criteria

The MVP can be considered functionally complete when a new user can successfully perform the following journey:

```text
1. Create an account
2. Sign in
3. Create an intention
4. Start a journey
5. Roll the dice
6. Receive a deterministic card
7. Read the card
8. Write a reflection
9. Have a multi-turn AI reflection conversation
10. Continue to another card
11. Complete the journey
12. Receive a summary
13. Leave the application
14. Return later
15. View the completed journey in history
16. Open the journey
17. Review the reflection and summary
```

The complete journey must preserve the user's data correctly.

---

# 25. Product Quality Gate

Before a feature is considered complete, it should satisfy four questions:

### Product

Does this improve the user's reflection experience?

### Simplicity

Is this the simplest implementation that satisfies the requirement?

### AI behavior

Does the AI remain a facilitator rather than becoming an authority?

### Engineering

Can the behavior be tested and maintained reliably?

A technically impressive feature that does not improve the product should not be added merely for demonstration purposes.

---

# 26. Requirement Change Rule

These requirements are a living product specification.

Changes are expected as the product is learned and validated.

However, significant changes should be intentional.

Before adding a new capability, ask:

1. What user problem does it solve?
2. Is the problem demonstrated or only hypothetical?
3. Is it required for the MVP?
4. Can the existing system solve the problem more simply?
5. Does the change affect the core Reflekta principles?
6. Does it introduce unnecessary technical complexity?

The product requirements should be updated when a significant product decision is made.

---

# 27. Guiding Principle

The MVP should prove the experience before proving the technology.

The goal is not to demonstrate how many AI technologies can be integrated.

The goal is to determine whether the following experience is valuable:

> **Intention → Unexpected card → Personal reflection → AI-guided exploration → New perspective**

If that experience is valuable, additional AI engineering capabilities can be introduced around it over time.


