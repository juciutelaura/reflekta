FROM python:3.14-slim
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv

WORKDIR /app
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev

COPY app ./app
EXPOSE 8000
CMD ["uv", "run", "--no-dev", "fastapi", "run", "app/main.py", "--port", "8000"]
