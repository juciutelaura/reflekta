"""Builds the LLM input for one facilitator turn from the context sent by the API."""

from app.schemas import ReflectionRequest

MAX_RECENT_MESSAGES = 10


def _escape(text: str) -> str:
    """Stop user text from opening or closing prompt blocks."""
    return text.replace("<", "&lt;").replace(">", "&gt;")


def _block(tag: str, text: str) -> str:
    return f"<{tag}>\n{_escape(text)}\n</{tag}>"


def build_input(request: ReflectionRequest) -> list[dict[str, str]]:
    """Return Responses API input: the card context first, then the recent conversation.

    The card, intention and reflection are always included; only the last
    MAX_RECENT_MESSAGES conversation messages are kept. User-written text is
    placed inside delimited blocks so it is treated as content, not instructions.
    """
    card = request.card
    context = "\n\n".join([
        _block("intention", request.intention),
        f"<card>\nTitle: {card.title}\nWisdom: {card.wisdom_text}\nReflection prompt: {card.reflection_prompt}\n</card>",
        _block("user_reflection", request.reflection),
    ])

    items = [{"role": "user", "content": context}]
    for message in request.messages[-MAX_RECENT_MESSAGES:]:
        content = _block("user_message", message.content) if message.role == "user" else message.content
        items.append({"role": message.role, "content": content})
    return items
