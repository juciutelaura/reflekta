"""The only module that talks to the LLM provider."""

from typing import Any, Protocol, TypeVar, cast

import openai
from openai import AsyncOpenAI
from pydantic import BaseModel

T = TypeVar("T", bound=BaseModel)


class LlmError(Exception):
    """The provider failed or returned no text. The message never contains user text."""


class LlmClient(Protocol):
    async def complete(self, instructions: str, input: list[dict[str, str]]) -> str: ...
    async def parse(self, instructions: str, input: list[dict[str, str]], response_model: type[T]) -> T: ...


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

    async def parse(self, instructions: str, input: list[dict[str, str]], response_model: type[T]) -> T:
        try:
            response = await self._client.responses.parse(
                model=self._model,
                instructions=instructions,
                input=cast(Any, input),
                text_format=response_model,
            )
        except openai.OpenAIError as error:
            raise LlmError(type(error).__name__) from error

        if response.output_parsed is None:
            raise LlmError("EmptyResponse")
        return response.output_parsed
