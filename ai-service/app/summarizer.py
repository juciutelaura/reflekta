"""One session-summary generation. Structured output, unlike the facilitator's free-form replies."""

from app.context_builder import build_summary_input
from app.llm_client import LlmClient, LlmError
from app.prompts import SUMMARY_SYSTEM_PROMPT
from app.schemas import SummaryReply, SummaryRequest

MAX_ATTEMPTS = 2

# AI_SPECIFICATION.md §20: the summarizer must never diagnose or make a clinical claim about the
# person. A term from this list is only safe in the summary if the person's own words used it.
_CLINICAL_TERMS = ("anxiety", "anxious", "depress", "disorder", "diagnos", "ptsd", "trauma", "ocd")


def _source_text(request: SummaryRequest) -> str:
    """The person's own words: the intention, every reflection, and every conversation message."""
    parts = [request.intention]
    for played in request.played_cards:
        parts.append(played.reflection_text)
        parts.extend(message.content for message in played.messages)
    return "\n".join(parts).lower()


def _invents_clinical_language(summary_text: str, source_text: str) -> bool:
    """True if the summary uses a clinical/diagnostic term the person's own words never used."""
    summary_lower = summary_text.lower()
    return any(term in summary_lower and term not in source_text for term in _CLINICAL_TERMS)


async def summarize(request: SummaryRequest, llm: LlmClient) -> SummaryReply:
    """Ask the LLM for a structured summary, rejecting and retrying one that invents a clinical
    claim the person's own words don't support (AI_SPECIFICATION.md §19: validate before use)."""
    input_items = build_summary_input(request)
    source = _source_text(request)

    for attempt in range(MAX_ATTEMPTS):
        reply = await llm.parse(SUMMARY_SYSTEM_PROMPT, input_items, SummaryReply)
        if not _invents_clinical_language(reply.summary_text, source):
            return reply

    raise LlmError("InventedClinicalLanguage")
