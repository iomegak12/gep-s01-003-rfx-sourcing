"""Application configuration loaded from environment / .env file."""

from __future__ import annotations

from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    database_url: str = "sqlite:///./data/tickets.db"
    mcp_host: str = "0.0.0.0"
    mcp_port: int = 8989
    log_level: str = "INFO"

    # Seed settings
    seed_on_startup: bool = True
    seed_force: bool = False


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
