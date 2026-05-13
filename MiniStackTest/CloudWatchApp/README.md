# CloudWatch Service - DynamoDB Metrics Reader

This project reads and displays CloudWatch metrics from DynamoDB tables created by the DynamoDBService project.

## Features

- Reads metrics from DynamoDB tables:
  - `TestTableGSIProvisioned`
  - `TestTableGSIOnDemand`
- Displays the following metrics for each table:
  - **ConsumedReadCapacityUnits (RCU)**: Read operations consumed
  - **ConsumedWriteCapacityUnits (WCU)**: Write operations consumed
  - **UserErrors**: Client-side errors
  - **SystemErrors**: Server-side errors
  - **SuccessfulRequestLatency**: Request latency
  - **Query**: Query count
  - **Scan**: Scan count

## Prerequisites

- Docker
- .NET 10.0 SDK
- DynamoDBService running and generating metrics

## Running the Metrics Reader

```bash
docker-compose up --build
```

This will:

1. Start MiniStack with CloudWatch and DynamoDB services
2. Build and run the CloudWatchService container
3. Display all metrics for the DynamoDB tables

## Environment Variables

- `CLOUDWATCH_ENDPOINT`: CloudWatch endpoint (default: http://localhost:4566)
- `AWS_ACCESS_KEY_ID`: AWS access key (default: test)
- `AWS_SECRET_ACCESS_KEY`: AWS secret key (default: test)
- `AWS_DEFAULT_REGION`: AWS region (default: us-east-1)