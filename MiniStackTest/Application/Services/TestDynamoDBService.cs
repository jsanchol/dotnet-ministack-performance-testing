using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Domain.Entities;
using Infrastructure.Observability;
using Infrastructure.Persistence;
using System.Diagnostics;

namespace Application.Services
{
    public class TestDynamoDBService : TestingDynamoDBClient
    {
        internal const string NameTestTableGSIProvisioned = "TestTableGSIProvisioned";
        internal const string NameTestTableGSIOnDemand = "TestTableGSIOnDemand";

        //Given a containerized environment with Ministack running
        public TestDynamoDBService()
        {
            
        }

        //Then Runnning tests against Provisioned the DynamoDB service table
        public async Task RunTestsTableGSIProvisioned(string tableName = NameTestTableGSIProvisioned)
        {
            // Create table
            await CreateGSIProvisionedTableAsync(tableName);

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var tasks = new List<Task>
            {
                // Monitor RCUs and WCUs in the background while running tests
                MonitorTableAsync(tableName, cancellationToken),
                // GSI+Provisioned throughput with partition key only (no sort key) to test performance of queries on GSI without sort key
                RunIncrementalTestGSIAsync(tableName, cancellationTokenSource)
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        //Then Runnning tests against On-Demand the DynamoDB service table
        public async Task RunTestsTableGSIOnDemand(string tableName = NameTestTableGSIOnDemand)
        {
            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            
            // Create table
            await CreateGSIOnDemandTableAsync(tableName);

            var tasks = new List<Task>
            {
                // Monitor RCUs and WCUs in the background while running tests
                MonitorTableAsync(tableName, cancellationToken),
                // GSI+On-Demand throughput with partition key only (no sort key) to test performance of queries on GSI without sort key
                RunIncrementalTestGSIAsync(tableName, cancellationTokenSource)
            };
            await Task.WhenAll(tasks);
        }

        private async Task MonitorTableAsync(string tableName, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Monitoring table {tableName} for RCUs and WCUs...");

            // This is a placeholder for monitoring logic. In a real implementation, you would use CloudWatch metrics or DynamoDB's DescribeTable API to monitor RCUs and WCUs.
            // For example, you could periodically call DescribeTable and log the ProvisionedThroughput and ConsumedCapacity.

            while (!cancellationToken.IsCancellationRequested)
            {
                // Example of using DescribeTable to get current provisioned throughput
                var describeResponse = await client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableName
                });

                Console.WriteLine($"Initial provisioned RCUs: {describeResponse.Table.ProvisionedThroughput.ReadCapacityUnits}");
                Console.WriteLine($"Initial provisioned WCUs: {describeResponse.Table.ProvisionedThroughput.WriteCapacityUnits}");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ContinueWith(_ => { }); // Adjust monitoring frequency as needed
            }
        }

        private async Task RunIncrementalTestGSIAsync(string tableName, CancellationTokenSource cancellationTokenSource)
        {
            for (int i = 3; i <= 3; i++)
            // Insert i=3=1k, i=4=10k, i=5=100k, i=6=1M items
            {
                int sampleNumber = (int)Math.Pow(10, i);
                // Put sample items small test data
                Console.WriteLine($"Inserting small item test data for table {tableName} sample number: {sampleNumber}.");
                
                await PutSmallItemTestDataAsync(tableName, sampleNumber);

                // Performance tests
                await RunPKGSIQueryTestsAsync(tableName);
                await RunPaginationQueryTestsAsync(tableName);
                await RunScanTestsAsync(tableName);

                await RunContextTestsAsync(tableName, sampleNumber);
            }
            
            cancellationTokenSource.Cancel(); // Stop monitoring after tests are done
        }

        private async Task RunContextTestsAsync(string tableName, int sampleNumber)
        {
            DynamoDBContext context = GetDynamoDBContext();

            Console.WriteLine("Running DynamoDBContext performance tests...");
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < sampleNumber; i++)
            {
                var item = new TestItem(
                    $"User{i % 10}",
                    $"Item{i}",
                    $"Category{i % 5}",
                    GenerateLargeDataString(1)
                );
                stopwatch.Restart();
                await context.SaveAsync(item);
                stopwatch.Stop();
                if (i == sampleNumber - 1)
                {
                    Console.WriteLine($"DynamoDBContext SaveAsync for {sampleNumber} items, time: {stopwatch.ElapsedMilliseconds} ms.");
                }
            }

            for (int i = 0; i < sampleNumber; i++)
            {
                stopwatch.Restart();
                var item = await context.LoadAsync<TestItem>($"User{i % 10}", $"Item{i}");
                stopwatch.Stop();
                if (i == sampleNumber - 1)
                {
                    Console.WriteLine($"DynamoDBContext LoadAsync for SK={item.SK} & PK={item.PK} item, time: {stopwatch.ElapsedMilliseconds} ms.");
                }
            }
        }

        private async Task RunPKGSIQueryTestsAsync(string tableName)
        {
            Console.WriteLine("Running Query performance tests...");

            // Query on partition key
            var queryRequest = new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = "User0" }
                }
            };

            var stopwatch = Stopwatch.StartNew();
            var queryResponse = await client.QueryAsync(queryRequest);
            stopwatch.Stop();
            Console.WriteLine($"PK Query 10% of items, time: {stopwatch.ElapsedMilliseconds} ms, Items: {queryResponse.Items.Count}");
            await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.QueryMetric, queryResponse.Items.Count, tableName, "Query");

            // Query on GSI
            var gsiQueryRequest = new QueryRequest
            {
                TableName = tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "GSI_PK = :gsi_pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":gsi_pk"] = new AttributeValue { S = "Category0" }
                }
            };

            stopwatch.Restart();
            var gsiQueryResponse = await client.QueryAsync(gsiQueryRequest);
            stopwatch.Stop();
            await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.QueryMetric, gsiQueryResponse.Items.Count, tableName, "GSIQuery");
            Console.WriteLine($"Finished GSI Query 20% of items, time: {stopwatch.ElapsedMilliseconds} ms, Items: {gsiQueryResponse.Items.Count}");
        }

        private async Task RunScanTestsAsync(string tableName)
        {
            Console.WriteLine("Running Scan performance tests...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            // Scan
            var scanRequest = new ScanRequest
            {
                TableName = tableName
            };

            stopwatch.Restart();
            var scanResponse = await client.ScanAsync(scanRequest);
            stopwatch.Stop();
            await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.ScanMetric, scanResponse.Items.Count, tableName, "Scan");
            Console.WriteLine($"Finished Scan 100% items, time: {stopwatch.ElapsedMilliseconds} ms, Items: {scanResponse.Items.Count}");
        }

        private async Task RunPaginationQueryTestsAsync(string tableName)
        {
            Console.WriteLine("Running pagination query tests...");

            var queryRequest = new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = "User0" }
                },
                Limit = 100 // Adjust page size as needed
            };

            string? lastEvaluatedKey = null;
            int totalItems = 0;
            var stopwatch = Stopwatch.StartNew();

            do
            {
                if (lastEvaluatedKey != null)
                {
                    queryRequest.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = "User0" },
                        ["SK"] = new AttributeValue { S = lastEvaluatedKey }
                    };
                }

                var queryResponse = await client.QueryAsync(queryRequest);
                totalItems += queryResponse.Items.Count;
                lastEvaluatedKey = queryResponse.LastEvaluatedKey != null && queryResponse.LastEvaluatedKey.ContainsKey("SK") ? queryResponse.LastEvaluatedKey["SK"].S : null;
            } while (lastEvaluatedKey != null);
            await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.PaginationQueryMetric, totalItems, tableName, "PaginationQuery");
            stopwatch.Stop();
            Console.WriteLine($"Finished pagination query 10% of items by 100 Limit, time: {stopwatch.ElapsedMilliseconds} ms, Total items retrieved: {totalItems}");
        }

        public async Task ResetMinistackAsync()
        {
            using HttpClient httpClient= new();
            try
            {
                string url = $"{endpoint}/_ministack/reset";
                Console.WriteLine($"Sending request to reset Ministack: {url}");
                var response = await httpClient.PostAsync(url, null);
                Console.WriteLine($"Ministack reset response: {response.Content.ReadAsStringAsync().Result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting Ministack: {ex.Message}");
            }
        }
    }
}
