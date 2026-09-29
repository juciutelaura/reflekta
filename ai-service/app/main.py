"""Internal AI service. Only the ASP.NET API calls it; it never stores or logs user text."""

import logging
import secrets
import time
from functools import lru_cache
from typing import Annotated

from fastapi import Depends, FastAPI, Header, HTTPException, status

from app import facilitator
from app.llm_client import LlmClient, LlmError, OpenAiLlmClient
from app.schemas import ReflectionReply, ReflectionRequest
from app.settings import Settings

logger = logging.getLogger("reflekta.ai")

app = FastAPI(title="Reflekta AI Service")


@lru_cache
def get_settings() -> Settings:
    return Settings()


@lru_cache
def get_llm_client() -> LlmClient:
    settings = get_settings()
    return OpenAiLlmClient(settings.openai_api_key, settings.openai_model)


def require_internal_key(
    settings: Annotated[Settings, Depends(get_settings)],
    x_internal_key: Annotated[str | None, Header()] = None,
) -> None:
    if x_internal_key is None or not secrets.compare_digest(x_internal_key, settings.ai_internal_key):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post(
    "/ai/reflection/respond",
    response_model=ReflectionReply,
    dependencies=[Depends(require_internal_key)],
)
async def reflection_respond(
    request: ReflectionRequest,
    llm: Annotated[LlmClient, Depends(get_llm_client)],
) -> ReflectionReply:
    started = time.perf_counter()
    try:
        reply = await facilitator.respond(request, llm)
    except LlmError as error:
        logger.warning("Reflection reply failed: %s", error)
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="AI provider unavailable.") from error

    logger.info("Reflection reply generated in %.0f ms", (time.perf_counter() - started) * 1000)
    return ReflectionReply(reply=reply)
