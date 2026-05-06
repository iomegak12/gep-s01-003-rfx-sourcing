# ──────────────────────────────────────────────────────────────────
# Stage 1 — builder
# ──────────────────────────────────────────────────────────────────
FROM python:3.12-slim AS builder

WORKDIR /app

# Install uv
COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /usr/local/bin/

# Copy dependency manifests first for layer caching
COPY pyproject.toml uv.lock ./

# Install dependencies (no project source yet)
RUN uv sync --frozen --no-install-project --no-dev

# Copy the rest of the source
COPY server.py ./
COPY src/ ./src/

# Install the project itself into the venv
RUN uv sync --frozen --no-dev

# ──────────────────────────────────────────────────────────────────
# Stage 2 — runtime
# ──────────────────────────────────────────────────────────────────
FROM python:3.12-slim AS runtime

WORKDIR /app

# Create a non-root user
RUN groupadd --gid 1001 appgroup && \
    useradd --uid 1001 --gid appgroup --no-create-home appuser

# Copy the virtual environment and application source from builder
COPY --from=builder /app/.venv /app/.venv
COPY --from=builder /app/server.py /app/server.py
COPY --from=builder /app/src /app/src

# Create the data directory with correct ownership
RUN mkdir -p /app/data && chown -R appuser:appgroup /app/data

# Activate the venv
ENV PATH="/app/.venv/bin:$PATH"
ENV PYTHONUNBUFFERED=1
ENV PYTHONDONTWRITEBYTECODE=1

USER appuser

EXPOSE 8989

CMD ["python", "server.py"]
