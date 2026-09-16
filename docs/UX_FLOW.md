# Reflekta — UX Flow

**Version:** 0.1
**Status:** Draft
**Purpose:** Define the core user experience and interaction flow for the Reflekta MVP.

---

## 1. UX Goal

Reflekta should feel like a guided reflection journey rather than a questionnaire, chatbot, therapy session, or fortune-telling experience.

The user should always understand:

* what they are doing;
* why they are doing it;
* what the current step is;
* that they are free to disagree with the card or AI;
* that the AI is helping them reflect rather than telling them what something means.

The experience should be calm, focused, simple, and progressive.

---

## 2. Core UX Flow

The MVP follows this high-level journey:

```text
Landing
   ↓
Sign up / Log in
   ↓
Create Intention
   ↓
Optional AI Clarification
   ↓
Journey Introduction
   ↓
Roll Dice
   ↓
Reveal Card
   ↓
Read Wisdom
   ↓
User Reflection
   ↓
AI Reflection Dialogue
   ↓
Continue Journey
   ↓
Roll Dice
   ↓
Reveal Next Card
   ↓
...
   ↓
End Session
   ↓
Session Summary
   ↓
Journey History
```

The user should always know where they are in this flow.

---

# 3. Entry / Landing

## Purpose

Introduce Reflekta and explain the basic concept without overwhelming the user.

## User should understand

Reflekta helps them explore a question, thought, fear, belief, or intention through a sequence of reflection cards and AI-guided questions.

The card does not predict the future or provide an objective answer.

## Primary action

**Start reflecting**

If the user is not authenticated, the action leads to authentication.

## Secondary actions

Potentially:

* Learn more
* Log in

The MVP should keep the landing page simple.

---

# 4. Authentication

## Purpose

Allow the user to access their personal reflection journey and history.

## Flow

```text
Landing
   ↓
Start reflecting
   ↓
Authentication
   ↓
Authenticated user
   ↓
Create / continue journey
```

Authentication is handled by the chosen authentication provider.

The UX should not make authentication feel like the main product.

---

# 5. Create Intention

## Purpose

Give the user an opportunity to define what they would like to explore.

The intention can be a:

* question;
* situation;
* concern;
* thought;
* fear;
* decision they are considering;
* general area they want to understand better.

## Screen

The user sees a clear prompt such as:

**What would you like to explore?**

Example placeholder:

> What is on your mind right now?

The user enters free-form text.

## Primary action

**Continue**

## UX principle

The intention is a starting point, not a problem that Reflekta promises to solve.

The system should not imply that the journey will produce a definitive answer.

---

# 6. Optional AI Clarification

## Purpose

The AI may help the user clarify what they want to explore before starting the journey.

This step should only be used when clarification would genuinely help.

It should not become a mandatory interview.

## Example

User:

> I don't know whether I should change my career.

AI:

> When you think about changing your career, what feels most important to understand — what you want, what you're afraid of losing, or something else?

The user can answer, modify the intention, or continue.

## UX principles

The AI should:

* ask open questions;
* avoid leading the user toward a particular answer;
* avoid diagnosing the situation;
* avoid immediately giving advice;
* help the user formulate their own intention.

## Important

The original intention should remain part of the journey context even if the user clarifies it.

---

# 7. Journey Introduction

Before the first card, briefly explain how the journey works.

The user should understand:

1. they will roll the dice;
2. the roll determines the next step/card;
3. each card contains a reflection stimulus;
4. they will have an opportunity to reflect;
5. AI may ask questions to help explore their reaction;
6. they can disagree with the card or AI;
7. there is no predetermined interpretation they are required to accept.

## Example

> Your journey begins with a roll.
>
> The card you land on is not an answer or prediction. It is a prompt for reflection.
>
> Read it, notice your reaction, and explore what comes up for you.

Primary action:

**Start journey**

---

# 8. Dice Roll

## Purpose

Create the game-like progression of the journey.

The user initiates a dice roll.

## Interaction

```text
User presses "Roll"
        ↓
Dice animation
        ↓
Dice result
        ↓
Game state determines destination
        ↓
Card is revealed
```

## UX principles

The dice result should feel meaningful as a game mechanic while remaining clearly deterministic from the underlying game rules.

The AI must not decide which card the user receives based on the user's intention, emotions, or previous conversation.

---

# 9. Card Reveal

## Purpose

Present the next reflection stimulus.

The card should be visually distinct from the AI conversation.

## Card contains

At minimum:

* card title;
* wisdom/reflection text;
* optional theme;
* game position/context where useful.

The wisdom text is authored content and should remain stable.

The AI does not rewrite the wisdom card for each user.

## User action

The user reads the card and continues when ready.

Primary action:

**Reflect**

---

# 10. User Reflection

## Purpose

Give the user space to react to the card before the AI begins deeper questioning.

The user should not feel that they must produce a profound answer.

## Prompt

Examples:

> What came up for you when you read this?

or:

> Which part of this card caught your attention?

or:

> What was your first reaction?

## Input

Free-form text.

The user may:

* describe a thought;
* describe a reaction;
* connect the card to their situation;
* disagree with the card;
* say that the card means nothing to them;
* say they do not know what to say.

All of these are valid responses.

---

# 11. AI Reflection Dialogue

## Purpose

The AI acts as a reflection facilitator.

It should help the user explore their own thinking rather than provide an interpretation of the card.

## Conversation pattern

```text
Card
 ↓
User reaction
 ↓
AI question
 ↓
User response
 ↓
AI follow-up question
 ↓
User response
 ↓
...
```

The AI generally asks one question at a time.

## First AI question

The first question should usually respond to the user's immediate reaction.

Examples:

> What part of that reaction feels most interesting to you?

> What made that particular part stand out?

> When you say that, what do you mean by it?

The AI should not immediately connect the card to the user's original intention.

---

# 12. Gradual Depth

The conversation can become deeper as the user provides more material.

A simplified progression:

```text
Immediate reaction
      ↓
Meaning / interpretation
      ↓
Personal context
      ↓
Underlying assumption
      ↓
Alternative perspective
      ↓
What the user now notices
```

The AI should not force the conversation through every level.

The depth should depend on what the user actually says.

---

# 13. User Disagreement

The user must always be able to reject the card's relevance.

For example:

User:

> This card doesn't resonate with me at all.

The AI should not respond by trying to prove that the card actually applies.

A suitable direction would be:

> That's completely fine. What about it feels unrelated to you?

The user remains the authority on their own experience.

---

# 14. AI Behavior During Reflection

The AI should:

* ask open questions;
* ask one main question at a time;
* build on the user's actual words;
* use tentative language;
* allow uncertainty;
* allow disagreement;
* help the user examine assumptions;
* encourage perspective taking;
* preserve the broader intention as context;
* focus on reflection rather than advice.

The AI should avoid:

* telling the user what the card means for them;
* claiming to know how the user feels;
* forcing a connection between the card and the original intention;
* diagnosing the user;
* acting as a therapist;
* making predictions;
* presenting supernatural or divine messages;
* treating the card as objectively meaningful;
* immediately giving life or career advice.

---

# 15. Continuing the Journey

After the reflection conversation, the user can continue to another card.

Primary action:

**Continue journey**

The user then returns to the dice interaction.

```text
Reflection dialogue
        ↓
Continue journey
        ↓
Roll dice
        ↓
Next card
```

The journey should preserve relevant session context.

---

# 16. Ending a Session

A session can end when the user reaches the configured session endpoint or chooses to finish, depending on the final MVP game design.

The user should receive a clear transition from active journey to completed reflection.

Example:

> You've reached the end of this journey.

Primary action:

**See reflection summary**

Secondary action:

**Return to journey history**

---

# 17. Session Summary

## Purpose

Help the user consolidate what they explored.

The summary should reflect the conversation rather than invent new conclusions.

It may contain:

* original intention;
* cards encountered;
* important themes discussed;
* notable questions;
* perspectives the user explored;
* changes or observations expressed by the user;
* unresolved questions.

## Summary principle

The summary should distinguish between:

**What the user explicitly said**

and

**Possible patterns or themes observed in the conversation.**

AI-generated interpretations should use tentative language.

The summary should not tell the user who they are.

---

# 18. Journey History

## Purpose

Allow the user to revisit previous reflection sessions.

A history item may show:

* session date;
* original intention;
* short summary;
* themes;
* completion status.

Selecting a session opens its relevant history.

The MVP should prioritize a simple readable history experience over advanced analytics.

---

# 19. Long-Term Journey

Reflekta may gradually build a picture of recurring reflection themes across sessions.

Potential examples:

* recurring questions;
* recurring themes;
* values mentioned repeatedly;
* fears repeatedly explored;
* perspectives that changed;
* unresolved questions.

This information should be presented as an aid to reflection, not as an objective psychological profile.

Example:

> You've explored questions about control several times recently.

rather than:

> You have a control issue.

---

# 20. Important UX Boundaries

The following distinctions should remain visible throughout the product.

| Reflekta element | What it is                        | What it is not                       |
| ---------------- | --------------------------------- | ------------------------------------ |
| Intention        | Starting point for exploration    | A problem Reflekta promises to solve |
| Dice             | Random/game progression mechanism | Source of prediction                 |
| Card             | Reflection stimulus               | Oracle or objective answer           |
| Wisdom text      | Author-created content            | AI-generated personal truth          |
| AI               | Reflection facilitator            | Therapist or fortune teller          |
| Conversation     | Exploration of user's thinking    | Diagnosis                            |
| Summary          | Consolidation of the session      | Definitive interpretation            |
| Memory           | Long-term reflection context      | Psychological profile                |

---

# 21. Core UX Principles

### 21.1 User agency

The user remains the authority on their own experience.

### 21.2 No forced meaning

A card can be useful, neutral, confusing, or irrelevant.

All are valid outcomes.

### 21.3 Reflection before advice

The default interaction should help the user think before suggesting actions.

### 21.4 One step at a time

The interface should avoid overwhelming the user with too many decisions or questions.

### 21.5 Progressive depth

Start with the immediate reaction and deepen only when appropriate.

### 21.6 Clear separation between game and AI

The game determines progression.

The AI facilitates reflection.

The AI does not control the game.

### 21.7 Calm interaction

The experience should feel focused and reflective rather than gamified for speed, competition, or rewards.

---

# 22. MVP UX Scope

The MVP should support this complete flow:

```text
Authentication
    ↓
Create intention
    ↓
Optional clarification
    ↓
Start journey
    ↓
Roll dice
    ↓
Reveal card
    ↓
Read card
    ↓
Write reflection
    ↓
AI dialogue
    ↓
Continue
    ↓
Roll again
    ↓
Next card
    ↓
...
    ↓
Complete session
    ↓
AI summary
    ↓
History
```

The MVP does not need:

* social features;
* public profiles;
* leaderboards;
* achievements;
* notifications;
* complex gamification;
* advanced analytics dashboards;
* therapist/coach dashboards;
* community features.

---

# 23. UX Success Criteria

The MVP UX is successful when:

* a new user can understand what Reflekta is without extensive explanation;
* a user can start a reflection journey without confusion;
* the intention-setting step feels natural;
* the dice/card interaction is clear;
* the user understands that the card is a reflection stimulus rather than a prediction;
* the user has meaningful space to respond before AI questioning begins;
* AI questions feel connected to the user's actual response;
* the AI does not force meaning onto the user's experience;
* the user can disagree with the card or AI without friction;
* the transition between game, reflection, and AI dialogue is clear;
* the user can complete a session and understand what they explored;
* previous sessions can be revisited;
* the overall experience feels like guided self-reflection rather than chatting with a generic AI.

---

# 24. UX Guiding Principle

> **The product should create space for the user to discover their own perspective.**

The interface, cards, game mechanics, and AI should support that goal without taking ownership of the user's interpretation.
