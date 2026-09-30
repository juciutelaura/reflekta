from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Read from environment variables, or ai-service/.env for local development."""

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    openai_api_key: str
    openai_model: str
    ai_internal_key: str
