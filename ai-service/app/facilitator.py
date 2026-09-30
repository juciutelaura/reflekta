"""One reflection-facilitator turn. Later this can become an agent without changing the HTTP contract."""

from app.context_builder import build_input
from app.llm_client import LlmClient
from app.prompts import SYSTEM_PROMPT
from app.schemas import ReflectionRequest


async def respond(request: ReflectionRequest, llm: LlmClient) -> str:
    return await llm.complete(SYSTEM_PROMPT, build_input(request))
