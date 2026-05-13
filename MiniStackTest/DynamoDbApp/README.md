# DynamoDB Service with Ministack

A simple .NET C# console application that demonstrates connecting to DynamoDB using Ministack local emulator, with performance comparisons between Query, Scan, and Global Secondary Index (GSI) operations.

## Created using Copilot

Prompt:

   ```bash
   simple service connecting to dynamo from .net c#
   using docker 
   using ministack https://github.com/ministackorg/ministack 
   performance with query vs scan vs index vs more
   plan how to the performance test that service
   ```

## Project Structure

- DynamoDBService/: Main project directory
-- DynamoDBService/: .NET console app
--- Program.cs: Main code with DynamoDB operations
--- DynamoDBService.csproj: Project file with AWS SDK dependencies
--- Dockerfile: For containerizing the app
-- docker-compose.yml: Runs both Ministack and the app
-- README.md: Setup and usage instructions

## Prerequisites

- Docker
- ~ .NET 10 SDK: update DynamoDBService.csproj and Dockerfile with custom version in you local

## Setup

1. Clone or navigate to the project directory.

2. Build and run with Docker Compose:

   ```bash
   docker-compose up --build
   ```

   This will start Ministack (DynamoDB emulator) and the .NET service.

## What it does

- Creates a DynamoDB table with partition key (PK), sort key (SK), and a Global Secondary Index (GSI).
- Inserts 100 test items.
- Runs performance tests:
  - Query on partition key
  - Full table scan
  - Query on GSI

## Performance Insights

- **Query**: Efficient for retrieving items with a specific partition key. Uses the primary index, fast and cost-effective.
- **Scan**: Scans the entire table, slow and expensive for large tables. Avoid in production.
- **GSI Query**: Allows querying on non-primary keys. Slightly slower than primary query due to index maintenance overhead, but much faster than scan.
- **Ministack**: Provides local emulation with realistic DynamoDB behavior

## Performance Testing Plan

To thoroughly test performance:

1. **Scale Data**: Increase to 10,000+ items.
2. **Multiple Runs**: Execute 10+ iterations, calculate averages, min/max.
3. **Load Testing**: Use JMeter or custom scripts to simulate concurrent requests.
4. **Resource Monitoring**: Track CPU, memory, and network during tests.
5. **Parameter Tuning**: Test with different RCUs, filters, projections.
6. **Tracing**: Implement logging or use AWS X-Ray for detailed metrics.
7. **Comparison Metrics**: Latency, throughput, cost (RCU consumption).

## Running Locally (without Docker)

1. Start Ministack:

   ```bash
   docker run -p 4566:4566 ministackorg/ministack
   ```

2. Run the app:

   ```bash
   cd DynamoDBService
   dotnet run
   ```

## Related documentation

- Dotnet & Docker: <https://learn.microsoft.com/en-us/dotnet/core/docker/build-container?tabs=windows&pivots=dotnet-10-0>
- Docker Compose:
-- <https://docs.docker.com/compose/>
-- <https://docs.docker.com/reference/compose-file/services/>
-- <https://certidevs.com/tutorial-docker-compose-services>
- Ministack <https://ministack.org/>
- AWS SDK: <https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/creds-assign.html>
-DynamoDB: <https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/CodeSamples.DotNet.html#CodeSamples.DotNet.Credentials>

## Usefull commands

   ```bash
   dotnet --version
   dotnet --list-sdks
   docker init
   dotnet new console --name DynamoDBService
   dotnet add package AWSSDK.DynamoDBv2
   dotnet add package AWSSDK.Extensions.NETCore.Setup
   dotnet build
   dotnet run
   docker-compose up --build
   ```
