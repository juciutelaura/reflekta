# Reflekta — Claude Code Project Instructions

## 1. Project

Reflekta is an AI-guided self-reflection web application inspired by the structure of traditional Indian Leela/Lila, but intentionally designed as a secular reflection experience.

The product helps users explore:

* questions
* thoughts
* beliefs
* fears
* values
* recurring themes
* perspectives

The core principle is:

> AI helps you think, not tells you what to think.

---

# 2. Source of Truth

Before implementing a feature, read the relevant project documentation.

```text
docs/
├── PRODUCT.md
├── PRODUCT_REQUIREMENTS.md
├── UX_FLOW.md
├── ARCHITECTURE.md
├── TECH_STACK.md
├── DATA_MODEL.md
└── AI_SPECIFICATION.md
```

Use the documents as the primary source of product and architectural requirements.

Do not invent requirements when they are not specified.

If an implementation decision is genuinely ambiguous, choose the simplest reasonable approach and document the decision when it materially affects architecture or product behavior.

---

# 3. Project Goals

Build Reflekta as a real production-oriented product rather than a demo.

The project should demonstrate:

* full-stack engineering
* clean API design
* database design
* authentication
* authorization
* deterministic game mechanics
* LLM integration
* AI workflows
* context engineering
* structured outputs
* AI evaluation
* memory
* RAG when justified
* safety boundaries
* testing
* Docker
* CI/CD
* production deployment

Technology should be introduced because it solves a real problem.

Do not add technology merely to make the project look more complex.

---

# 4. Architecture

The main architecture is:

```text
React + TypeScript
        ↓
ASP.NET Core API
        ↓
PostgreSQL
        ↕
Python AI Service
        ↓
LLM Provider
```

Responsibilities must remain separated.

## React

Owns:

* UI
* user interaction
* presentation
* client-side interaction state

Does not own authoritative business logic.

## ASP.NET Core

Owns:

* authentication integration
* authorization
* users
* intentions
* journeys
* sessions
* game mechanics
* dice
* card selection
* persistence
* application business rules

## Python AI Service

Owns:

* intention clarification
* reflection facilitation
* session summarization
* later memory extraction
* later journey analysis
* AI context construction
* AI-specific orchestration

## PostgreSQL

Owns persisted application data.

## LLM

Generates language-based AI outputs.

The LLM does not own application state.

---

# 5. Critical Deterministic Boundary

Never allow an LLM to control critical game or application state.

The following must remain deterministic:

* dice results
* game state
* card selection
* journey progression
* authorization
* permissions
* database state
* ownership validation

AI may facilitate reflection around the game.

AI must not decide which card the user receives.

---

# 6. AI Behavior

Reflekta's AI is a reflection facilitator.

It is not:

* an oracle
* a fortune teller
* a therapist
* a diagnostician
* a psychological authority
* a decision-maker

The AI should:

* ask open questions
* listen to what the user actually says
* deepen gradually
* use tentative language
* preserve user agency
* accept disagreement
* explore perspectives
* prefer reflection over unsolicited advice

The AI must not:

* force a connection between card and intention
* impose card meaning
* claim to know the user's emotions
* diagnose
* predict the future
* claim supernatural knowledge
* fabricate memories
* present AI inference as fact

---

# 7. Card Principle

The card contains authored wisdom.

The LLM does not generate the card's wisdom during gameplay.

The card is:

> a reflection stimulus, not an answer.

The AI should facilitate exploration of the card without claiming that the card has a hidden or predetermined meaning for the user.

---

# 8. Context Engineering

Do not send unrestricted user history to the LLM.

Build explicit context for each AI operation.

Prefer context such as:

```text
Current user message
Current card
Current reflection
Recent conversation
Current intention
Relevant session context
Relevant memories
AI behavior rules
```

Prioritize current user input over old context.

Long-term memory must never override what the user says now.

Keep context relevant and bounded.

---

# 9. User Agency

The user is the authority on their own experience.

The AI must allow the user to:

* disagree
* reject an interpretation
* say a card is irrelevant
* change direction
* stop reflecting
* choose their own meaning

Never attempt to prove that an AI interpretation is correct.

---

# 10. One Question at a Time

During reflection dialogue, normally ask one primary question at a time.

Avoid questionnaire-style responses containing multiple unrelated questions.

Depth should emerge from the conversation.

Conceptually:

```text
Initial reaction
    ↓
Meaning
    ↓
Example
    ↓
Pattern
    ↓
Assumption / value
    ↓
Alternative perspective
```

Do not jump directly to deep interpretations.

---

# 11. Data and Privacy

Treat user reflections and conversations as sensitive application data.

Always enforce:

* authentication
* authorization
* user ownership
* data isolation

Never log private reflection content unnecessarily.

Never expose another user's data.

Never commit secrets to source control.

---

# 12. AI Data

Distinguish between:

```text
User statement
AI-generated summary
AI-derived observation
Candidate memory
Confirmed memory
```

Do not silently convert AI inference into user fact.

Persist provenance for AI-derived long-term information where appropriate.

---

# 13. Agents

Do not create agents merely for the sake of having agents.

Use the simplest implementation that satisfies the requirement.

Prefer:

```text
LLM call
```

when sufficient.

Use a:

```text
workflow
```

when multiple controlled AI steps are required.

Use an:

```text
agent
```

when dynamic tool use, delegation, decision-making, or autonomous multi-step execution provides genuine value.

---

# 14. RAG

Do not introduce RAG before there is a real retrieval requirement.

When semantic retrieval is useful, prefer:

```text
PostgreSQL + pgvector
```

before introducing a separate vector database.

Retrieval should provide relevant context, not replace relational application state.

---

# 15. MCP

Do not introduce MCP unless the product has a real tool or integration use case that benefits from it.

Do not add MCP purely for portfolio demonstration.

---

# 16. Coding Principles

Prefer:

* simple solutions
* readable code
* explicit behavior
* small focused components
* strong typing
* modern idiomatic language features
* meaningful tests
* clear naming

Avoid:

* premature abstraction
* unnecessary design patterns
* speculative infrastructure
* duplicate logic
* over-engineering
* unnecessary dependencies

The simplest correct implementation is preferred.

---

# 17. Development Workflow

Before implementing a substantial feature:

1. Read the relevant documentation.
2. Understand the requirements.
3. Identify affected components.
4. Create an implementation plan.
5. Define success criteria.
6. Implement incrementally.
7. Run relevant tests.
8. Fix defects.
9. Validate the feature against its requirements.
10. Only then consider the work complete.

Do not start large implementation work without understanding the relevant requirements.

---

# 18. Planning

For substantial changes, create a concise implementation plan before coding.

The plan should include:

```text
Goal
Affected components
Implementation phases
Important decisions
Success criteria
Testing strategy
```

Plans should be implementation-oriented rather than unnecessarily detailed.

---

# 19. Testing

Do not consider a feature complete because the code compiles.

Validate:

* functionality
* business rules
* authorization
* edge cases
* integration behavior
* relevant AI behavior

For AI features, also validate:

* prompt behavior
* structured outputs
* safety rules
* context construction
* golden scenarios
* regression cases

---

# 20. AI Evaluation

AI behavior is part of the product and must be testable.

Maintain golden scenarios for critical behavior.

Examples:

```text
User disagrees with card
→ AI accepts disagreement.

User sees no connection
→ AI does not force relevance.

User expresses an emotion
→ AI does not claim certainty about the user's emotional state.

User asks for prediction
→ AI does not provide prophecy.

User asks what the card means about them
→ AI does not present an authoritative interpretation.
```

Prompt changes should trigger relevant AI regression evaluation.

---

# 21. Definition of Done

A feature is complete only when:

* requirements are satisfied
* implementation matches the architecture
* business rules are enforced server-side
* relevant tests pass
* relevant AI evaluations pass
* error cases are handled
* security/authorization is considered
* no unnecessary complexity was introduced
* the implementation is understandable to another developer

For MVP work, do not continue expanding scope after the acceptance criteria are satisfied.

---

# 22. Scope Discipline

Reflekta MVP should remain focused.

Do not introduce:

* social networking
* leaderboards
* achievements
* complex gamification
* therapist dashboards
* community features
* billing
* native mobile applications
* Kubernetes
* unnecessary microservices
* complex multi-agent systems
* speculative integrations

unless the product requirements explicitly change.

---

# 23. Requirement Changes

If a requirement changes:

1. Identify which documentation is affected.
2. Update the relevant source-of-truth document.
3. Re-check dependent architecture/data/AI decisions.
4. Then implement the change.

Do not silently modify product behavior while leaving requirements inconsistent.

---

# 24. Git and Changes

Keep changes focused.

Prefer small, understandable commits.

Do not mix unrelated refactoring with feature implementation unless necessary.

Before considering a change complete:

```text
Build
↓
Test
↓
Validate
↓
Review diff
```

Avoid leaving unrelated generated files, debug code, secrets, or temporary artifacts in the repository.

---

# 25. Documentation Discipline

Documentation should be concise and useful.

Do not document implementation details that are obvious from the code unless they explain an important decision.

When a significant architectural decision is made, update the relevant documentation.

Keep documentation aligned with the actual implementation.

---

# 26. When Unsure

When multiple technically valid approaches exist:

1. Prefer the simplest one.
2. Prefer consistency with the existing architecture.
3. Prefer established project patterns.
4. Avoid introducing a new dependency.
5. Avoid introducing infrastructure prematurely.
6. Check the relevant project documentation.
7. If the decision materially affects product behavior or architecture, stop and surface the decision before proceeding.

---

# 27. Core Engineering Principle

> Build the simplest system that can satisfy the requirement, test it, verify it, and only then add complexity when a real problem demands it.

---

# 28. Product Principle

> Wisdom is in the card. AI is the facilitator.

Reflekta should help users explore their own thinking.

The system should never attempt to decide what the user's thoughts, feelings, life, or future "really" mean.
