using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Infrastructure.Observability;
using System.Diagnostics;

namespace Infrastructure.Persistence
{
    public class TestingDynamoDBClient
    {
        public AmazonDynamoDBClient client;
        public TestingCloudWatchClient cloudWatchClient;
        public string endpoint;
        internal string cloudWatchEndpoint;
        private string AccessKey;
        private string SecretKey;

        public TestingDynamoDBClient()
        {
            //TODO: move this to a config file and use dependency injection for better flexibility and testability, especially when we expand to other AWS services like Lambda and RDS.
            endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:4566";
            cloudWatchEndpoint = Environment.GetEnvironmentVariable("CLOUDWATCH_ENDPOINT") ?? endpoint;
            AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test";
            SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test";
            client = SetUpClient();
            cloudWatchClient = SetUpCloudWatchClient();
        }

        //SetUp dynamo test service
        internal AmazonDynamoDBClient SetUpClient()
        {
            // Configure DynamoDB client for Ministack
            
            AmazonDynamoDBConfig dynamoDbConfig = new()
            {
                ServiceURL = endpoint,
                AuthenticationRegion = "us-east-1"
            };
            
            AWSCredentials basicCredentials = new BasicAWSCredentials(AccessKey, SecretKey);
            return new AmazonDynamoDBClient(basicCredentials, dynamoDbConfig);;
        }

        internal TestingCloudWatchClient SetUpCloudWatchClient()
        {
            return new TestingCloudWatchClient();
        }

        internal async Task WaitForTableActiveAsync(string tableName)
        {
            var request = new DescribeTableRequest { TableName = tableName };
            while (true)
            {
                var response = await client.DescribeTableAsync(request);
                if (response.Table.TableStatus == TableStatus.ACTIVE)
                    break;
                await Task.Delay(1000);
            }
            Console.WriteLine("Table is active.");
        }

        public async Task CreateGSIProvisionedTableAsync(string tableName)
        {
            var request = new CreateTableRequest
            {
                TableName = tableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                    new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "PK", AttributeType = "S" },
                    new AttributeDefinition { AttributeName = "SK", AttributeType = "S" },
                    new AttributeDefinition { AttributeName = "GSI_PK", AttributeType = "S" }
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "GSI1",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "GSI_PK", KeyType = KeyType.HASH }
                        },
                        Projection = new Projection { ProjectionType = "ALL" },
                        ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 5, WriteCapacityUnits = 5 }
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput { ReadCapacityUnits = 5, WriteCapacityUnits = 5 }
            };

            var stopwatch = Stopwatch.StartNew();
            try
            {
                stopwatch.Restart();
                await client.CreateTableAsync(request);
                stopwatch.Stop();
                Console.WriteLine("Table created successfully.");
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateSuccessMetric, 1, tableName, "GSIProvisioned", StandardUnit.Count);
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateMetric, stopwatch.Elapsed.TotalMilliseconds, tableName, "GSIProvisioned");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateFailureMetric, 1, tableName, "GSIProvisioned", StandardUnit.Count);
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateMetric, stopwatch.Elapsed.TotalMilliseconds, tableName, "GSIProvisioned");
                Console.WriteLine($"Table creation failed: {ex.Message}");
            }

            // Wait for table to be active
            await WaitForTableActiveAsync(tableName);
        }

        //When populated the DynamoDB table with test data and running performance tests
        public async Task PutSmallItemTestDataAsync(string tableName, int sampleNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < sampleNumber; i++)
            {
                var item = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"User{i % 10}" },
                    ["SK"] = new AttributeValue { S = $"Item{i}" },
                    ["GSI_PK"] = new AttributeValue { S = $"Category{i % 5}" },
                    ["Data"] = new AttributeValue { S = $"Data for item {i}" }
                };

                await client.PutItemAsync(tableName, item);
            }
            stopwatch.Stop();
            await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTestDataInsertDurationMsMetric, stopwatch.Elapsed.TotalMilliseconds, tableName, "PutSmallItemTestData");
            Console.WriteLine($"Test data inserted: {sampleNumber} items in {stopwatch.ElapsedMilliseconds} ms.");
        }

        internal async Task PutLargeItemTestDataAsync(string tableName, int sampleNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            var random = new Random();
            
            for (int i = 0; i < sampleNumber; i++)
            {
                int randomSize = random.Next(1, 11);//dynamodb item up to 400KB
                string largeData = GenerateLargeDataString(randomSize);
                
                var item = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"User{i % 10}" },
                    ["SK"] = new AttributeValue { S = $"Item{i}" },
                    ["GSI_PK"] = new AttributeValue { S = $"Category{i % 5}" },
                    ["Data"] = new AttributeValue { S = $"Data for item {i}" },
                    ["LargeData"] = new AttributeValue { S = $"Large data for item {i}: {largeData}" }
                };

                await client.PutItemAsync(tableName, item);
            }
            stopwatch.Stop();
            await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTestDataInsertDurationMsMetric, stopwatch.Elapsed.TotalMilliseconds, tableName, "PutLargeItemTestData");
            Console.WriteLine($"Test data inserted: {sampleNumber} items in {stopwatch.ElapsedMilliseconds} ms (with random 1-10KB large data per item).");
        }

        public string GenerateLargeDataString(int sizeInKB)
        {
            // Generate a string of approximately the specified size in KB
            const string baseString = "NsQlhbisDW5JVlLSaZVtCLSUUrkBijbkc5f9gFFscDkoGnN0J6GgIFqdCLyhbdWLHxRVY8IwDCrWF555JeY0yD0GtgH21NotZAEeiWJR1A4bxqq9VKKAzMJ0tW7TCOqNtMzVtPB6NrtCIg8NSmhrO7QjNcOzi4Nb";
            int baseSizeInBytes = baseString.Length;
            int targetSizeInBytes = sizeInKB * 1024;
            
            // Calculate how many times we need to repeat the base string
            int repetitions = (targetSizeInBytes / baseSizeInBytes) + 1;
            
            var result = new System.Text.StringBuilder(targetSizeInBytes);
            for (int i = 0; i < repetitions; i++)
            {
                result.Append(baseString);
                if (result.Length >= targetSizeInBytes)
                {
                    break;
                }
            }
            
            // Trim to exact size
            return result.ToString(0, Math.Min(result.Length, targetSizeInBytes));
        }

        public async Task CreateGSIOnDemandTableAsync(string tableName)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = new CreateTableRequest
            {
                TableName = tableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "PK", KeyType = "HASH" },
                    new KeySchemaElement { AttributeName = "SK", KeyType = "RANGE" }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "PK", AttributeType = "S" },
                    new AttributeDefinition { AttributeName = "SK", AttributeType = "S" },
                    new AttributeDefinition { AttributeName = "GSI_PK", AttributeType = "S" }
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "GSI1",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "GSI_PK", KeyType = "HASH" }
                        },
                        Projection = new Projection { ProjectionType = "ALL" }
                        // No ProvisionedThroughput needed for PAY_PER_REQUEST
                    }
                }
            };

            try
            {
                stopwatch.Restart();
                await client.CreateTableAsync(request);
                stopwatch.Stop();
                Console.WriteLine("Table created successfully.");
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateSuccessMetric, 1, tableName, "GSIOnDemand", StandardUnit.Count);
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateMetric, stopwatch.Elapsed.TotalMilliseconds, tableName, "GSIOnDemand");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateFailureMetric, 1, tableName, "GSIOnDemand", StandardUnit.Count);
                await cloudWatchClient.PublishCloudWatchMetricAsync(TestingCloudWatchClient.DynamoDBTableCreateMetric, stopwatch.Elapsed.TotalMilliseconds, tableName, "GSIOnDemand");
                Console.WriteLine($"Table creation failed: {ex.Message}");
            }
        }

        public DynamoDBContext GetDynamoDBContext()
        {
            DynamoDBContext context = new DynamoDBContextBuilder()
                        .ConfigureContext(x =>
                        {
                            x.DisableFetchingTableMetadata = false; // add this line to avoid issues with non-existent table metadata when using DynamoDB Local
                        })
                        .WithDynamoDBClient(() => client)
                        .Build();
            return context;
        }
    }


}
