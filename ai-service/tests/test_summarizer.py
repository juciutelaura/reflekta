import asyncio

import pytest

from app.llm_client import LlmError
from app.schemas import SummaryCard, SummaryPlayedCard, SummaryReply, SummaryRequest
from app.summarizer import summarize


class SequenceLlm:
    """Returns each queued reply in order on successive parse() calls."""

    def __init__(self, replies: list[SummaryReply]) -> None:
        self.replies = list(replies)
        self.calls = 0

    async def complete(self, instructions: str, input: list[dict[str, str]]) -> str:
        raise NotImplementedError

    async def parse(self, instructions: str, input: list[dict[str, str]], response_model: type) -> SummaryReply:
        reply = self.replies[self.calls]
        self.calls += 1
        return reply


def make_request(reflection_text: str = "I grip plans tightly.") -> SummaryRequest:
    return SummaryRequest(
        intention="Should I change my career?",
        played_cards=[
            SummaryPlayedCard(
                card=SummaryCard(title="Control", wisdom_text="Notice where you hold on tightly."),
                reflection_text=reflection_text,
                messages=[],
            )
        ],
    )


def test_returns_the_reply_when_it_uses_no_invented_clinical_language():
    llm = SequenceLlm([SummaryReply(summary_text="The user considered letting go of a project.", themes=["control"])])

    result = asyncio.run(summarize(make_request(), llm))

    assert result.summary_text == "The user considered letting go of a project."
    assert llm.calls == 1


def test_allows_a_clinical_term_the_person_actually_used():
    llm = SequenceLlm([SummaryReply(summary_text="The user said they feel anxious about the decision.", themes=["anxiety"])])

    result = asyncio.run(summarize(make_request(reflection_text="I feel anxious about this decision."), llm))

    assert result.summary_text == "The user said they feel anxious about the decision."
    assert llm.calls == 1


def test_retries_once_when_the_summary_invents_a_clinical_term():
    llm = SequenceLlm([
        SummaryReply(summary_text="The user has severe anxiety.", themes=[]),
        SummaryReply(summary_text="The user considered letting go of a project.", themes=["control"]),
    ])

    result = asyncio.run(summarize(make_request(), llm))

    assert result.summary_text == "The user considered letting go of a project."
    assert llm.calls == 2


def test_raises_when_the_summary_keeps_inventing_a_clinical_term():
    llm = SequenceLlm([
        SummaryReply(summary_text="The user has severe anxiety.", themes=[]),
        SummaryReply(summary_text="The user is managing an anxiety disorder.", themes=[]),
    ])

    with pytest.raises(LlmError):
        asyncio.run(summarize(make_request(), llm))

    assert llm.calls == 2
