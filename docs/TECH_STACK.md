# Reflekta — Technical Stack

## 1. Purpose

This document defines the technology choices for Reflekta MVP.

The stack should support:

* production-quality web application development
* clear separation between product backend and AI capabilities
* reliable persistence
* authentication and authorization
* AI workflows and LLM integration
* future memory and RAG capabilities
* automated testing
* Docker-based local development
* future cloud deployment
* AI Engineering and Agentic Engineering experimentation without unnecessary complexity

Technology choices should remain pragmatic.

---

# 2. Stack Overview

| Layer                  | Technology                             |
| ---------------------- | -------------------------------------- |
| Frontend               | React + TypeScript                     |
| Frontend build         | Vite                                   |
| Backend                | ASP.NET Core + C#                      |
| Backend API            | REST / JSON                            |
| Authentication         | Clerk                                  |
| Database               | PostgreSQL                             |
| Vector search          | pgvector, when needed                  |
| AI service             | Python                                 |
| AI orchestration       | OpenAI Agents SDK, when appropriate    |
| LLM                    | OpenAI API, initially                  |
| Local development      | Docker + Docker Compose                |
| Testing — backend      | xUnit                                  |
| Testing — frontend     | Vitest + React Testing Library         |
| Testing — E2E          | Playwright                             |
| Infrastructure         | AWS, later                             |
| Infrastructure as Code | Terraform, later                       |
| CI/CD                  | GitHub Actions, later/early production |
| Source control         | Git + GitHub                           |

---

# 3. Frontend

## React

React is the frontend framework.

The frontend should remain focused on:

* user interaction
* presentation
* application state needed by the UI
* API communication
* navigation
* loading/error states

It should not contain authoritative business logic.

---

## TypeScript

TypeScript is required for frontend development.

Reasons:

* type safety
* clearer API contracts
* better maintainability
* safer refactoring
* improved AI-assisted development

Avoid using `any` unless there is a specific justified reason.

---

## Vite

Vite is the initial frontend build and development tool.

The project should use a straightforward React + TypeScript setup without unnecessary framework complexity.

---

# 4. Backend

## ASP.NET Core

ASP.NET Core is the primary application backend.

Responsibilities include:

* REST API
* authentication integration
* authorization
* users
* intentions
* journeys
* game mechanics
* dice
* card selection
* sessions
* persistence
* domain/business rules

The backend is the authoritative source of application state.

---

## C#

C# is used for backend development.

The codebase should use modern, idiomatic C# appropriate for the selected supported .NET version.

Prefer:

* clear domain-oriented code
* dependency injection
* async APIs
* strong typing
* small focused services
* explicit business rules

Avoid unnecessary abstraction layers.

---

# 5. API

The backend exposes a REST API using JSON.

The API should be organized around Reflekta's domain rather than technical implementation details.

Example domains:

```text
/api/intentions
/api/journeys
/api/sessions
/api/cards
/api/users
```

AI functionality should not be exposed directly to the browser.

The browser communicates with ASP.NET Core.

ASP.NET Core communicates with the Python AI service.

---

# 6. Authentication

## Clerk

Clerk provides user authentication.

Responsibilities include:

* registration
* login
* authentication sessions
* identity management
* authentication tokens

ASP.NET Core validates the authenticated identity and applies application-level authorization.

The backend must never trust a user ID supplied directly by the client without validating the authenticated identity.

---

# 7. Database

## PostgreSQL

PostgreSQL is the primary database for Reflekta.

It stores:

* users/application profiles
* intentions
* journeys
* cards
* game state
* dice results
* sessions
* reflections
* conversations
* summaries
* memories
* relevant AI-derived data

PostgreSQL is preferred over introducing multiple databases during MVP.

---

# 8. Vector Search

## pgvector

pgvector may be enabled in PostgreSQL when semantic retrieval becomes necessary.

Potential uses:

* retrieving relevant previous session summaries
* retrieving relevant memories
* semantic card/theme retrieval
* future RAG workflows

Vector search is not required for the first implementation if normal relational queries are sufficient.

Do not introduce a separate vector database unless a real requirement appears.

---

# 9. AI Service

## Python

AI functionality is isolated into a Python service.

Reasons:

* strong AI/LLM ecosystem
* convenient experimentation
* clear separation from the main application
* easier AI workflow development
* suitable environment for evaluation and retrieval experiments

The Python service is not the owner of Reflekta's core business state.

---

# 10. AI Orchestration

## OpenAI Agents SDK

The OpenAI Agents SDK may be used for AI workflows where it provides meaningful value.

Potential uses:

* intention clarification
* reflection facilitation
* structured AI workflows
* tool usage
* future agentic workflows
* evaluation of AI behavior

The project should not create multiple agents simply to demonstrate that agents exist.

For simple tasks, a normal LLM call or workflow is preferred.

An agent should be introduced when the task genuinely benefits from:

* multi-step reasoning/workflow
* tool usage
* delegation
* dynamic decision-making
* stateful execution
* autonomous task progression

---

# 11. LLM Provider

## OpenAI API

The initial implementation will use OpenAI models through the OpenAI API.

LLM-specific code should remain isolated inside the Python AI service.

The rest of the application should not depend directly on model-specific implementation details.

This makes it easier to change:

* model
* prompt
* orchestration
* provider
* evaluation strategy

without changing the core application.

---

# 12. Structured AI Outputs

AI workflows should use structured outputs where predictable machine-readable data is required.

Examples:

* intention clarification result
* session summary
* extracted memory
* theme analysis
* evaluation result

Free-form text should be used when the output is directly intended for the user's conversational experience.

The system should validate structured AI outputs before persisting or using them.

---

# 13. AI Context Engineering

The AI service should construct explicit context for every AI operation.

Context may include:

```text
Current intention
Current card
Card wisdom
Current user reflection
Recent conversation
Relevant session context
Relevant confirmed memories
Current journey state
AI behavior rules
```

The system should avoid automatically sending the complete user history to the LLM.

Context should be:

* relevant
* minimal
* explicit
* traceable
* evaluated where practical

---

# 14. RAG

RAG is a capability, not a mandatory architectural component.

Initial implementation:

```text
Normal PostgreSQL queries
        ↓
Relevant application data
        ↓
AI context
```

If semantic retrieval becomes useful:

```text
User/session context
        ↓
Embedding / retrieval
        ↓
PostgreSQL + pgvector
        ↓
Relevant memories/summaries
        ↓
AI context
        ↓
LLM
```

RAG should be introduced based on a real retrieval requirement.

---

# 15. AI Memory

Memory will initially be persisted in PostgreSQL.

Potential memory categories:

* recurring themes
* values
* questions
* fears or concerns explicitly expressed by the user
* changed perspectives
* important reflections

Memory should distinguish between:

* facts explicitly stated by the user
* AI-derived observations
* tentative patterns

AI must not turn a tentative inference into an established fact about the user.

---

# 16. Local Development

Docker Compose should be used for the local multi-service environment.

Expected services:

```text
frontend
backend
ai-service
postgres
```

The development environment should be reproducible with a small number of commands.

---

# 17. Testing

## Backend

Use xUnit for unit and integration testing.

Important areas:

* game mechanics
* dice behavior
* card selection
* journey state transitions
* authorization
* API behavior
* persistence

---

## Frontend

Use:

* Vitest
* React Testing Library

Focus on important user interactions and meaningful UI behavior rather than testing implementation details.

---

## End-to-End

Use Playwright for critical user journeys.

The primary E2E scenario should cover:

```text
Register / Login
      ↓
Create intention
      ↓
Start journey
      ↓
Roll dice
      ↓
Reveal card
      ↓
Write reflection
      ↓
AI dialogue
      ↓
Continue journey
      ↓
Complete session
      ↓
View summary
```

---

# 18. AI Evaluation

AI behavior requires a separate evaluation approach from conventional software tests.

The project should eventually maintain golden scenarios covering requirements such as:

* AI asks open questions
* AI does not force a connection to the original intention
* AI asks one question at a time
* AI does not claim to know the user's emotions
* AI does not diagnose
* AI does not behave as a therapist
* AI does not make predictions
* AI does not present supernatural messages
* AI does not impose card meaning
* AI respects disagreement
* AI does not immediately jump to advice
* AI preserves relevant intention context

AI evaluations should be executable repeatedly to detect regressions.

---

# 19. Package and Dependency Principles

Dependencies should be added only when they solve a concrete problem.

Before adding a library, consider:

1. Is it necessary?
2. Can the existing stack solve the problem?
3. Does it add meaningful complexity?
4. Is it actively maintained?
5. Does it make the system easier to understand?
6. Can it be removed later without a major rewrite if it turns out not to be needed?

A dependency that fails most of these questions should not be added, even if it would save some short-term effort.

---

# 20. Infrastructure and Deployment

Infrastructure choices follow the same "start simple" principle as the rest of the stack (see ARCHITECTURE.md §20, §26).

## AWS

AWS is the target cloud provider once the MVP moves beyond local development. It is introduced later, not for the first working version of the product.

## Terraform

Terraform is the intended infrastructure-as-code tool for provisioning AWS resources. Like AWS itself, it is introduced once there is a real deployment to manage, not upfront.

The MVP should run correctly with Docker Compose locally before any cloud infrastructure is defined.

---

# 21. CI/CD

## GitHub Actions

GitHub Actions is the intended CI/CD tool, introduced later or at the start of production hardening rather than during early MVP development.

A minimal pipeline should eventually:

* build the frontend, backend, and AI service;
* run backend tests (xUnit), frontend tests (Vitest), and relevant AI evaluations;
* run linting/formatting checks;
* build container images for deployment.

CI/CD infrastructure should not be built out before there is a working, tested MVP to run it against.

---

# 22. Source Control

## Git + GitHub

Git is used for version control, hosted on GitHub.

Changes should stay focused and incremental (see CLAUDE.md §24, "Git and Changes"): small, understandable commits, no mixing of unrelated refactoring with feature work, and no secrets or generated artifacts committed to the repository.

---

# 23. Guiding Principle

> Choose the simplest technology that solves a real, current problem. Let the product's actual requirements — not the desire to showcase a technology — decide what enters the stack.
