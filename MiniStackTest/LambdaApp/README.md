# Lambda Service

This project test lambda call and log  metrics.

## Features

- Metrics:
  - **Errors**: Server-side errors
  - **SuccessfulRequestLatency**: Request latency

## Prerequisites

- Docker
- .NET 10.0 SDK
- Cloudwatch running and generating metrics

## Running the Metrics Reader

```bash
docker-compose up --build
```

This will:

1. Start MiniStack with CloudWatch
2. Build and run app container
3. Publish all metrics

## Environment Variables

- `LAMBDA_ENDPOINT`: CloudWatch endpoint (default: <http://localhost:4566>)
- `AWS_ACCESS_KEY_ID`: AWS access key (default: test)
- `AWS_SECRET_ACCESS_KEY`: AWS secret key (default: test)
- `AWS_DEFAULT_REGION`: AWS region (default: us-east-1)
