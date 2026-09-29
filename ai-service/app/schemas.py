"""HTTP contract between the ASP.NET API and this service (camelCase JSON)."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class Card(CamelModel):
    title: str = Field(min_length=1)
    wisdom_text: str = Field(min_length=1)
    reflection_prompt: str = Field(min_length=1)


class Message(CamelModel):
    role: Literal["user", "assistant"]
    content: str = Field(min_length=1)


class ReflectionRequest(CamelModel):
    intention: str = Field(min_length=1)
    card: Card
    reflection: str = Field(min_length=1)
    messages: list[Message] = []


class ReflectionReply(CamelModel):
    reply: str
