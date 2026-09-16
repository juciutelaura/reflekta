# Reflekta — Architecture

## 1. Purpose

This document defines the technical architecture of Reflekta MVP.

The architecture should provide:

* clear separation of responsibilities
* deterministic game mechanics
* isolated AI capabilities
* secure persistence
* a foundation for future memory, RAG, evaluation, and agentic workflows
* a simple development and deployment model

The architecture should remain as simple as possible while supporting the product requirements.

---

# 2. Architecture Principles

## 2.1 Deterministic logic stays deterministic

Critical product and game mechanics must not depend on an LLM.

The backend is responsible for:

* authentication and authorization
* users
* intentions
* journeys
* dice rolls
* game state
* card selection
* session state
* persistence
* permissions
* business rules

The AI service must not decide critical game state.

---

## 2.2 AI facilitates reflection

AI is responsible for capabilities where language understanding and generation are useful.

Examples:

* intention clarification
* reflective questioning
* conversation facilitation
* session summarization
* memory extraction
* journey/theme analysis

AI should not determine what a card means for the user.

---

## 2.3 The card is the source of wisdom

Card content is authored and stored as product data.

The LLM does not generate or redefine the card's core wisdom during gameplay.

The AI uses the card as context for facilitating reflection.

---

## 2.4 Backend is the source of truth

The ASP.NET Core backend is the authoritative source for:

* user identity
* authorization
* journey state
* game state
* dice results
* card selection
* session state
* persisted user data

The frontend must not be trusted to enforce business rules.

---

## 2.5 AI must be replaceable

The core product should not depend on one specific LLM provider.

AI functionality should be isolated behind the Python AI service so that models, prompts, orchestration approaches, and providers can evolve independently from the core application.

---

## 2.6 Start simple

Reflekta MVP does not require:

* Kubernetes
* multiple independent backend services
* a separate vector database
* a complex multi-agent architecture
* event-driven infrastructure
* distributed queues
* complex MCP infrastructure

These may be introduced later if real requirements justify them.

---

# 3. High-Level Architecture

```text
┌──────────────────────────────┐
│          React UI            │
│       TypeScript            │
└──────────────┬───────────────┘
               │ HTTPS / JSON
               ▼
┌──────────────────────────────┐
│      ASP.NET Core API        │
│                              │
│ Auth / Authorization         │
│ Users                        │
│ Intentions                   │
│ Journeys                     │
│ Game Mechanics               │
│ Dice / Card Selection        │
│ Sessions                     │
│ Persistence                  │
└──────────────┬───────────────┘
               │
        ┌──────┴──────┐
        │             │
        ▼             ▼
┌──────────────┐  ┌────────────────────┐
│ PostgreSQL   │  │ Python AI Service  │
│              │  │                    │
│ Product data │  │ AI orchestration  │
│ Game state   │  │ LLM calls         │
│ Sessions     │  │ Reflection         │
│ Memories     │  │ Summaries          │
│ AI outputs   │  │ Memory extraction │
│              │  │ RAG / retrieval    │
└──────────────┘  └─────────┬──────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │   LLM Provider│
                    └───────────────┘
```

The React application communicates with the ASP.NET Core API.

The ASP.NET Core API owns the application and game domain.

The Python AI service provides AI-specific capabilities.

PostgreSQL is the primary persistent data store and may also provide vector search through pgvector when RAG becomes necessary.

---

# 4. Frontend — React + TypeScript

The frontend is responsible for the user interface and user interaction.

## Responsibilities

The frontend handles:

* landing page
* authentication UI
* intention creation
* AI clarification interface
* journey UI
* dice interaction
* card presentation
* reflection input
* AI conversation interface
* session summary
* journey history
* loading and error states

The frontend should provide a clear and calm reflection experience.

---

## What the frontend must not own

The frontend must not be the authoritative source for:

* dice results
* card selection
* journey state
* authorization
* user ownership
* session permissions
* business rules

The frontend may display these values, but the backend determines them.

---

# 5. Backend — ASP.NET Core

The ASP.NET Core API is the main application backend.

It acts as the system's business and domain layer.

## Responsibilities

### Authentication and authorization

The backend validates authentication tokens and determines what the authenticated user is allowed to access.

Clerk provides authentication.

The backend remains responsible for application-level authorization.

---

### User management

The backend manages application-specific user data associated with the authenticated identity.

---

### Intention management

The backend stores:

* user's original intention
* clarified intention when applicable
* intention state
* timestamps

AI may help clarify an intention, but the final intention belongs to the user.

---

### Journey management

The backend manages:

* journey creation
* current journey state
* progression
* session state
* completed sessions

---

### Game mechanics

The backend is responsible for:

* generating dice results
* validating rolls
* applying game rules
* determining the resulting position
* selecting the card
* returning the card to the client

The card selection algorithm must be deterministic according to the game's rules.

The AI service must not select cards.

---

### Session management

The backend stores:

* session start/end
* cards encountered
* user reflections
* conversation messages or appropriate conversation representation
* session completion
* generated summaries

---

### AI orchestration boundary

The backend calls the Python AI service when an AI capability is required.

For example:

```text
React
  ↓
POST /sessions/{id}/reflection
  ↓
ASP.NET Core
  ↓
Python AI Service
  ↓
LLM
  ↓
Python AI Service
  ↓
ASP.NET Core
  ↓
React
```

The backend remains responsible for deciding whether the user is authorized to perform the operation.

The AI service remains responsible for generating the AI response.

---

# 6. Python AI Service

The Python service contains AI-specific functionality.

It should be treated as an AI application/service rather than as the owner of the Reflekta domain.

## Responsibilities

Potential MVP capabilities:

* intention clarification
* reflection facilitation
* session summarization

Later capabilities:

* memory extraction
* journey analysis
* semantic retrieval
* RAG
* more sophisticated AI workflows
* AI evaluation infrastructure

---

# 7. AI Capability Architecture

The AI service should initially use a small number of clearly defined workflows.

Conceptually:

```text
AI Service
│
├── Intention Clarification
│
├── Reflection Facilitator
│
├── Session Summarizer
│
├── Memory Extractor       ← later
│
└── Journey Analyzer       ← later
```

These capabilities do not need to become separate autonomous agents.

A workflow should only become an agent when there is a real need for:

* multiple reasoning steps
* tool usage
* dynamic decisions
* stateful execution
* delegation
* autonomous workflow behavior

The architecture should not introduce agents merely to increase the number of agents.

---

# 8. Reflection Facilitator Flow

The reflection conversation is the most important AI interaction in the MVP.

A simplified request flow is:

```text
User
 │
 ▼
React
 │
 ▼
ASP.NET Core
 │
 │  validates user/session
 ▼
Python AI Service
 │
 │  builds context
 ▼
Reflection Facilitator
 │
 │
 ▼
LLM
 │
 │
 ▼
AI response
 │
 ▼
Python AI Service
 │
 ▼
ASP.NET Core
 │
 ▼
React
```

The AI context should contain only information necessary for the current interaction.

Potential context:

* current intention
* current card
* card wisdom text
* current reflection
* recent conversation
* relevant session context
* relevant confirmed memories
* applicable AI behavior rules

The system should avoid sending the entire user's history to the model by default.

---

# 9. Context Engineering

Context should be deliberately constructed rather than simply passing all available data to the LLM.

Conceptually:

```text
                    ┌─────────────────────┐
                    │ Current Intention   │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ Current Card        │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ Current Reflection  │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ Recent Conversation │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ Relevant Memory     │
                    └──────────┬──────────┘
                               │
                               ▼
                       AI Context Builder
                               │
                               ▼
                              LLM
```

The context builder should explicitly determine what information is relevant for the current AI operation.

This creates a clear place to later introduce:

* retrieval
* memory selection
* context prioritization
* token budgeting
* context evaluation

---

# 10. RAG Architecture

RAG is not required for the initial MVP.

When retrieval becomes useful, PostgreSQL with pgvector can be used rather than introducing a separate vector database.

Potential retrieval sources include:

* previous session summaries
* confirmed memories
* card knowledge
* themes
* relevant journey history

Conceptually:

```text
Current AI Request
       │
       ▼
Context Builder
       │
       ├──────────────► PostgreSQL
       │                    │
       │                    ▼
       │              Semantic Retrieval
       │                    │
       └────────────────────┘
                    │
                    ▼
              Selected Context
                    │
                    ▼
                   LLM
```

Retrieval should be introduced only when there is a measurable product or technical reason to use it.

---

# 11. Database — PostgreSQL

PostgreSQL is the primary database.

It stores both product/domain data and AI-related persisted data where appropriate.

Potential categories include:

### Identity/application data

* users
* user preferences

### Product/game data

* intentions
* journeys
* sessions
* cards
* game state
* dice rolls

### Reflection data

* user reflections
* conversations
* session summaries

### Long-term journey data

* memories
* themes
* journey observations

### AI data

* AI operation metadata
* evaluation-related data where appropriate

---

# 12. Data Ownership

Each type of data should have a clear owner.

| Data                    | Primary Owner                            |
| ----------------------- | ---------------------------------------- |
| Authentication identity | Clerk                                    |
| Application user        | ASP.NET Core                             |
| Intention               | ASP.NET Core                             |
| Journey                 | ASP.NET Core                             |
| Dice result             | ASP.NET Core                             |
| Card selection          | ASP.NET Core                             |
| Card content            | PostgreSQL / application domain          |
| User reflection         | ASP.NET Core / PostgreSQL                |
| Conversation            | ASP.NET Core / PostgreSQL                |
| AI response generation  | Python AI Service                        |
| Session summary         | Python AI Service → persisted by backend |
| Memory extraction       | Python AI Service → persisted by backend |
| Authorization           | ASP.NET Core                             |
| Game state              | ASP.NET Core                             |

The AI service generates AI-derived information but does not become the authoritative owner of the user's product state.

---

# 13. API Boundary

The API should expose product-oriented operations rather than exposing internal AI implementation details.

Examples:

```text
POST   /api/intentions
POST   /api/intentions/{id}/clarify

POST   /api/journeys
GET    /api/journeys
GET    /api/journeys/{id}

POST   /api/journeys/{id}/roll
GET    /api/journeys/{id}/current-card

POST   /api/journeys/{id}/reflection
POST   /api/journeys/{id}/continue
POST   /api/journeys/{id}/complete
```

A journey and a "session" are the same resource (see PRODUCT_REQUIREMENTS.md §14, "journey/session", and DATA_MODEL.md §6). The API does not expose a separate `/api/sessions` resource — reflection, continuation, and completion are actions on a journey.

Exact endpoint design should be finalized during implementation planning.

The important architectural rule is that the API should represent Reflekta's domain, not simply mirror AI-service endpoints.

---

# 14. AI Service Boundary

The Python service can expose internal AI-oriented operations such as:

```text
POST /ai/clarify-intention

POST /ai/reflection/respond

POST /ai/session/summarize

POST /ai/memory/extract

POST /ai/journey/analyze
```

These endpoints are internal service boundaries.

The public frontend should not call the AI service directly.

---

# 15. Request Flow: Starting a Journey

```text
User
 │
 ▼
React
 │
 │ Create intention
 ▼
ASP.NET Core
 │
 ▼
PostgreSQL
 │
 ▼
Journey created
 │
 ▼
React
 │
 │ Start journey
 ▼
ASP.NET Core
 │
 ├── creates game state
 │
 └── applies deterministic rules
 │
 ▼
Roll dice
 │
 ▼
Determine position
 │
 ▼
Determine card
 │
 ▼
Return card
 │
 ▼
React displays card
```

No LLM is required for the game mechanics.

---

# 16. Request Flow: AI Reflection

```text
User reads card
 │
 ▼
User writes reflection
 │
 ▼
React
 │
 ▼
ASP.NET Core
 │
 ├── validates session
 ├── validates authorization
 └── persists reflection
 │
 ▼
Python AI Service
 │
 ├── builds context
 ├── applies AI behavior rules
 └── calls LLM
 │
 ▼
AI response
 │
 ▼
ASP.NET Core
 │
 └── persists appropriate conversation state
 │
 ▼
React
 │
 ▼
User sees next reflection question
```

The AI should normally produce one conversational step at a time.

---

# 17. Error and Failure Boundaries

The system should distinguish between:

### Product/business failures

Examples:

* invalid session
* unauthorized resource
* invalid game action
* invalid journey state

These are handled by ASP.NET Core.

### AI failures

Examples:

* LLM timeout
* provider failure
* malformed AI output
* AI service unavailable

These should not corrupt game state.

The product should be able to recover from an AI failure without losing the underlying journey or card state.

---

# 18. Security Boundary

The system should follow a clear trust boundary:

```text
Browser
   │
   │ untrusted
   ▼
ASP.NET Core
   │
   │ authenticated + authorized
   ▼
Application data
```

The browser must never be trusted to enforce authorization or game rules.

The AI service should also treat user-provided text as untrusted input.

Potential prompt-injection attempts must not be allowed to override system-level AI behavior or application rules.

---

# 19. Privacy Boundary

Reflection data may be personally sensitive even though Reflekta is not a clinical product.

The architecture should therefore support:

* authenticated access
* authorization checks
* user-level data isolation
* secure transport
* secure secret management
* controlled logging
* data deletion
* data export
* transparency around AI processing

Sensitive user reflection content should not be unnecessarily written to application logs.

---

# 20. Deployment Architecture — MVP

The initial deployment can remain simple.

Conceptually:

```text
                    Internet
                       │
                       ▼
                ┌─────────────┐
                │ React / Web │
                └──────┬──────┘
                       │
                       ▼
                ┌─────────────┐
                │ ASP.NET API │
                └──────┬──────┘
                       │
              ┌────────┴────────┐
              ▼                 ▼
       ┌─────────────┐   ┌─────────────┐
       │ PostgreSQL  │   │ Python AI   │
       │             │   │ Service     │
       └─────────────┘   └──────┬──────┘
                                │
                                ▼
                           LLM Provider
```

Development should use Docker Compose to reproduce the main services locally.

Cloud deployment and infrastructure-as-code can be introduced after the application has a working MVP.

---

# 21. Local Development

The initial local environment should provide:

```text
React
ASP.NET Core API
Python AI Service
PostgreSQL
```

Docker Compose can orchestrate the backend services and database.

The developer should be able to start the core system with a small number of commands.

---

# 22. Testing Architecture

Testing should follow the separation of responsibilities.

## Frontend

Test:

* important user flows
* components where useful
* API integration behavior

## ASP.NET Core

Test:

* business rules
* authorization
* dice mechanics
* card selection
* journey state transitions
* API behavior
* persistence integration

## Python AI Service

Test:

* prompt/workflow behavior
* structured outputs
* safety rules
* context construction
* AI regression scenarios
* retrieval behavior when RAG exists

## End-to-end

Test the critical journey:

```text
Register
  ↓
Create intention
  ↓
Start journey
  ↓
Roll dice
  ↓
Reveal card
  ↓
Reflect
  ↓
AI dialogue
  ↓
Continue
  ↓
Complete session
  ↓
View summary
```

---

# 23. Observability

The system should eventually provide enough observability to understand:

* API failures
* AI service failures
* latency
* LLM usage
* token/cost behavior
* failed AI outputs
* important product events

AI logs should avoid unnecessarily storing private reflection content.

AI observability should make it possible to investigate why an AI interaction failed without turning user reflections into unrestricted logs.

---

# 24. Future Architecture Extensions

The architecture intentionally leaves room for future capabilities.

Potential future additions include:

### Memory system

```text
Sessions
   ↓
Session Summary
   ↓
Memory Extraction
   ↓
Memory Store
   ↓
Relevant Memory Retrieval
   ↓
AI Context
```

### RAG

Add pgvector-based retrieval when semantic retrieval provides measurable value.

### Evaluation pipeline

```text
Golden Scenarios
       ↓
AI Workflow
       ↓
Generated Response
       ↓
Evaluators
       ↓
Metrics / Regression Results
```

### Agentic workflows

Agents may be introduced where autonomous multi-step behavior or tool use provides genuine value.

### MCP

MCP should only be introduced when Reflekta needs a real tool or external integration that benefits from an MCP-based interface.

---

# 25. Architecture Decision Rules

When considering a new technical component, ask:

1. What real requirement does it solve?
2. Could the existing architecture solve it more simply?
3. Does it introduce a new operational burden?
4. Does it improve the product or only demonstrate a technology?
5. Can the component be added later without major architectural changes?

Technology should follow product requirements, not the other way around.

---

# 26. Architecture Success Criteria

The architecture is successful when:

* game mechanics are deterministic
* AI cannot control critical game state
* frontend/backend responsibilities are clear
* AI responsibilities are isolated
* user data is properly authorized
* the MVP can run locally with a simple setup
* AI failures do not corrupt game state
* AI context can evolve without rewriting the core application
* memory and RAG can be added later
* AI evaluation can be added without restructuring the whole system
* the architecture remains understandable to a single developer

---

# 27. Guiding Principle

> Keep the product deterministic where determinism matters, use AI where language intelligence creates value, and keep the architecture simple enough that one developer can understand the whole system.
