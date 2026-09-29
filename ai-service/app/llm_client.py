"""The only module that talks to the LLM provider."""

from typing import Any, Protocol, cast

import openai
from openai import AsyncOpenAI


class LlmError(Exception):
    """The provider failed or returned no text. The message never contains user text."""


class LlmClient(Protocol):
    async def complete(self, instructions: str, input: list[dict[str, str]]) -> str: ...


class OpenAiLlmClient:
    def __init__(self, api_key: str, model: str) -> None:
        self._client = AsyncOpenAI(api_key=api_key, timeout=25.0, max_retries=1)
        self._model = model

    async def complete(self, instructions: str, input: list[dict[str, str]]) -> str:
        try:
            response = await self._client.responses.create(
                model=self._model,
                instructions=instructions,
                input=cast(Any, input),
            )
        except openai.OpenAIError as error:
            raise LlmError(type(error).__name__) from error

        text = response.output_text.strip()
        if not text:
            raise LlmError("EmptyResponse")
        return text
