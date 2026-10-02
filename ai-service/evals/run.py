"""Run golden scenarios against the real model: uv run python -m evals.run"""

import asyncio
import sys

from openai import AsyncOpenAI
from pydantic import BaseModel

from app.facilitator import respond
from app.llm_client import OpenAiLlmClient
from app.settings import Settings
from app.summarizer import summarize
from evals.scenarios import SCENARIOS, SUMMARY_SCENARIOS, Scenario, SummaryScenario

JUDGE_INSTRUCTIONS = """\
You evaluate one reply from a self-reflection facilitator or session summarizer. You receive the \
conversation context, the reply and the expected behavior. Decide strictly whether the reply \
meets the expected behavior. Return passed and a one-sentence reason."""


class Verdict(BaseModel):
    passed: bool
    reason: str


def at_most_one_question(reply: str) -> bool:
    return reply.count("?") <= 1


async def judge(client: AsyncOpenAI, model: str, scenario: Scenario | SummaryScenario, reply: str) -> Verdict:
    response = await client.responses.parse(
        model=model,
        instructions=JUDGE_INSTRUCTIONS,
        input=(
            f"<context>\n{scenario.request.model_dump_json(by_alias=True)}\n</context>\n"
            f"<reply>\n{reply}\n</reply>\n"
            f"<expected_behavior>\n{scenario.expectation}\n</expected_behavior>"
        ),
        text_format=Verdict,
    )
    return response.output_parsed


async def main() -> int:
    settings = Settings()
    llm = OpenAiLlmClient(settings.openai_api_key, settings.openai_model)
    judge_client = AsyncOpenAI(api_key=settings.openai_api_key)

    failures = 0

    for scenario in SCENARIOS:
        reply = await respond(scenario.request, llm)
        verdict = await judge(judge_client, settings.openai_model, scenario, reply)
        one_question = at_most_one_question(reply)
        passed = verdict.passed and one_question
        failures += not passed

        print(f"{'PASS' if passed else 'FAIL'}  {scenario.name}")
        print(f"      reply: {reply}")
        print(f"      judge: {verdict.reason}")
        if not one_question:
            print("      rule:  more than one question")

    for scenario in SUMMARY_SCENARIOS:
        summary = await summarize(scenario.request, llm)
        verdict = await judge(judge_client, settings.openai_model, scenario, summary.summary_text)
        failures += not verdict.passed

        print(f"{'PASS' if verdict.passed else 'FAIL'}  {scenario.name}")
        print(f"      summary: {summary.summary_text}")
        print(f"      themes:  {summary.themes}")
        print(f"      judge: {verdict.reason}")

    total = len(SCENARIOS) + len(SUMMARY_SCENARIOS)
    print(f"\n{total - failures}/{total} scenarios passed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
