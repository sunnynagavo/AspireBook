# Use the official Python image as the base image
FROM python:3.13-slim-bookworm
COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /bin/

# Copy the project into the image
ADD . /app

# Sync the project into a new environment, using the frozen lockfile
WORKDIR /app
RUN uv sync --frozen

# Expose the port the application will run on
EXPOSE 8000

# Command to run the application
CMD ["uv", "run", "process-payment-api"]