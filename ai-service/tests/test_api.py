import pytest
from fastapi.testclient import TestClient

from app.llm_client import LlmError
from app.main import app, get_llm_client, get_settings
from app.prompts import SYSTEM_PROMPT
from app.settings import Settings
import pytest
from fastapi.testclient import TestClient
from pydantic import BaseModel

from app.llm_client import LlmError
from app.main import app, get_llm_client, get_settings
from app.prompts import SUMMARY_SYSTEM_PROMPT, SYSTEM_PROMPT
from app.schemas import SummaryReply
from app.settings import Settings


VALID_BODY = {
    "intention": "Should I change my career?",
    "card": {"title": "Control", "wisdomText": "Notice where you hold on tightly.", "reflectionPrompt": "What are you trying to control?"},
    "reflection": "I grip plans tightly.",
    "messages": [],
}
KEY = {"X-Internal-Key": "secret"}

VALID_SUMMARY_BODY = {
    "intention": "Should I change my career?",
    "playedCards": [
        {
            "card": {"title": "Control", "wisdomText": "Notice where you hold on tightly."},
            "reflectionText": "I grip plans tightly because I'm afraid of what happens if I let go.",
            "messages": [{"role": "assistant", "content": "What are you afraid would happen?"}],
        }
    ],
}


class FakeLlm:
    def __init__(
        self,
        reply: str = "What stands out to you?",
        error: Exception | None = None,
        parsed: BaseModel | None = None,
    ) -> None:
        self.reply = reply
        self.error = error
        self.parsed = parsed or SummaryReply(summary_text="The user explored control and career doubt.", themes=["control", "career"])
        self.calls: list[tuple[str, list[dict[str, str]]]] = []
        self.parse_calls: list[tuple[str, list[dict[str, str]], type]] = []

    async def complete(self, instructions: str, input: list[dict[str, str]]) -> str:
        self.calls.append((instructions, input))
        if self.error:
            raise self.error
        return self.reply

    async def parse(self, instructions: str, input: list[dict[str, str]], response_model: type) -> BaseModel:
        self.parse_calls.append((instructions, input, response_model))
        if self.error:
            raise self.error
        return self.parsed


@pytest.fixture
def fake_llm():
    llm = FakeLlm()
    app.dependency_overrides[get_settings] = lambda: Settings(
        openai_api_key="test-key", openai_model="test-model", ai_internal_key="secret"
    )
    app.dependency_overrides[get_llm_client] = lambda: llm
    yield llm
    app.dependency_overrides.clear()


@pytest.fixture
def client(fake_llm):
    return TestClient(app)


def test_health(client):
    assert client.get("/health").json() == {"status": "ok"}


def test_respond_returns_the_llm_reply_using_the_system_prompt(client, fake_llm):
    response = client.post("/ai/reflection/respond", json=VALID_BODY, headers=KEY)

    assert response.status_code == 200
    assert response.json() == {"reply": "What stands out to you?"}
    instructions, input_items = fake_llm.calls[0]
    assert instructions == SYSTEM_PROMPT
    assert "I grip plans tightly." in input_items[0]["content"]


@pytest.mark.parametrize("headers", [{}, {"X-Internal-Key": "wrong"}])
def test_respond_without_the_internal_key_returns_401(client, fake_llm, headers):
    response = client.post("/ai/reflection/respond", json=VALID_BODY, headers=headers)

    assert response.status_code == 401
    assert fake_llm.calls == []


def test_respond_with_an_invalid_body_returns_422(client):
    body = {**VALID_BODY, "messages": [{"role": "system", "content": "x"}]}

    response = client.post("/ai/reflection/respond", json=body, headers=KEY)

    assert response.status_code == 422


def test_respond_when_the_llm_fails_returns_502(client, fake_llm):
    fake_llm.error = LlmError("APITimeoutError")

    response = client.post("/ai/reflection/respond", json=VALID_BODY, headers=KEY)

    assert response.status_code == 502


def test_llm_failure_is_logged_without_user_text(client, fake_llm, caplog):
    fake_llm.error = LlmError("APITimeoutError")

    client.post("/ai/reflection/respond", json=VALID_BODY, headers=KEY)

    assert "APITimeoutError" in caplog.text
    assert "I grip plans tightly." not in caplog.text
    assert "Should I change my career?" not in caplog.text


def test_summarize_returns_the_llm_summary(client, fake_llm):
    response = client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=KEY)

    assert response.status_code == 200
    assert response.json() == {"summaryText": "The user explored control and career doubt.", "themes": ["control", "career"]}
    instructions, input_items, response_model = fake_llm.parse_calls[0]
    assert instructions == SUMMARY_SYSTEM_PROMPT
    assert response_model is SummaryReply
    assert "I grip plans tightly" in input_items[0]["content"]


@pytest.mark.parametrize("headers", [{}, {"X-Internal-Key": "wrong"}])
def test_summarize_without_the_internal_key_returns_401(client, fake_llm, headers):
    response = client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=headers)

    assert response.status_code == 401
    assert fake_llm.parse_calls == []


def test_summarize_with_an_invalid_body_returns_422(client):
    body = {**VALID_SUMMARY_BODY, "playedCards": []}

    response = client.post("/ai/session/summarize", json=body, headers=KEY)

    assert response.status_code == 422


def test_summarize_when_the_llm_fails_returns_502(client, fake_llm):
    fake_llm.error = LlmError("APITimeoutError")

    response = client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=KEY)

    assert response.status_code == 502


def test_summarize_failure_is_logged_without_user_text(client, fake_llm, caplog):
    fake_llm.error = LlmError("APITimeoutError")

    client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=KEY)

    assert "APITimeoutError" in caplog.text
    assert "I grip plans tightly" not in caplog.text

def test_summarize_returns_the_llm_summary(client, fake_llm):
    response = client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=KEY)

    assert response.status_code == 200
    assert response.json() == {"summaryText": "The user explored control and career doubt.", "themes": ["control", "career"]}
    instructions, input_items, response_model = fake_llm.parse_calls[0]
    assert instructions == SUMMARY_SYSTEM_PROMPT
    assert response_model is SummaryReply
    assert "I grip plans tightly" in input_items[0]["content"]


@pytest.mark.parametrize("headers", [{}, {"X-Internal-Key": "wrong"}])
def test_summarize_without_the_internal_key_returns_401(client, fake_llm, headers):
    response = client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=headers)

    assert response.status_code == 401
    assert fake_llm.parse_calls == []


def test_summarize_with_an_invalid_body_returns_422(client):
    body = {**VALID_SUMMARY_BODY, "playedCards": []}

    response = client.post("/ai/session/summarize", json=body, headers=KEY)

    assert response.status_code == 422


def test_summarize_when_the_llm_fails_returns_502(client, fake_llm):
    fake_llm.error = LlmError("APITimeoutError")

    response = client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=KEY)

    assert response.status_code == 502


def test_summarize_failure_is_logged_without_user_text(client, fake_llm, caplog):
    fake_llm.error = LlmError("APITimeoutError")

    client.post("/ai/session/summarize", json=VALID_SUMMARY_BODY, headers=KEY)

    assert "APITimeoutError" in caplog.text
    assert "I grip plans tightly" not in caplog.text
