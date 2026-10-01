"""One session-summary generation. Structured output, unlike the facilitator's free-form replies."""

from app.context_builder import build_summary_input
from app.llm_client import LlmClient
from app.prompts import SUMMARY_SYSTEM_PROMPT
from app.schemas import SummaryReply, SummaryRequest


async def summarize(request: SummaryRequest, llm: LlmClient) -> SummaryReply:
    return await llm.parse(SUMMARY_SYSTEM_PROMPT, build_summary_input(request), SummaryReply)
