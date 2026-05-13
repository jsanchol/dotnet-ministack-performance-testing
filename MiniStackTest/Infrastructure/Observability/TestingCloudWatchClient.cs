using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Runtime;

namespace Infrastructure.Observability
{
        public class TestingCloudWatchClient
    {
        public AmazonCloudWatchClient cloudWatchClient;
        internal string endpoint;
        private string AccessKey;
        private string SecretKey;

        public const string DynamoDBNamespace = "MiniStack/AWS/DynamoDB";
        public const string ConsumedRCUMetric = "ConsumedReadCapacityUnits";
        public const string ConsumedWCUMetric = "ConsumedWriteCapacityUnits";
        public const string UserErrorsMetric = "UserErrors";
        public const string SystemErrorsMetric = "SystemErrors";
        public const string SuccessfulRequestLatencyMetric = "SuccessfulRequestLatency";
        public const string QueryMetric = "QueryCount";
        public const string ScanMetric = "ScanCount";
        public const string DynamoDBTableCreateSuccessMetric = "DynamoDBTableCreateSuccess";
        public const string DynamoDBTableCreateMetric = "DynamoDBTableCreateDurationMs";
        public const string DynamoDBTableCreateFailureMetric = "DynamoDBTableCreateFailure";
        public const string DynamoDBTestDataInsertDurationMsMetric = "DynamoDBTestDataInsertDurationMs";
        public const string CloudWatchMetricFetchFailureMetric = "CloudWatchMetricFetchFailure";
        public const string PaginationQueryMetric = "PaginationQueryCount";

        public TestingCloudWatchClient()
        {
            endpoint = Environment.GetEnvironmentVariable("CLOUDWATCH_ENDPOINT") ?? "http://localhost:4566";
            AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test";
            SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test";
            cloudWatchClient = SetUpClient();
        }

        internal AmazonCloudWatchClient SetUpClient()
        {
            AmazonCloudWatchConfig cloudWatchConfig = new()
            {
                ServiceURL = endpoint,
                AuthenticationRegion = "us-east-1"
            };

            AWSCredentials basicCredentials = new BasicAWSCredentials(AccessKey, SecretKey);
            return new AmazonCloudWatchClient(basicCredentials, cloudWatchConfig);
        }

        public async Task GetMetricAsync(string tableName, string metricName, DateTime startTime, DateTime endTime)
        {
            try
            {
                var request = new GetMetricStatisticsRequest
                {
                    Namespace = DynamoDBNamespace,
                    MetricName = metricName,
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "TableName", Value = tableName }
                    },
                    StartTime = startTime,
                    EndTime = endTime,
                    Period = 300, // 5-minute intervals
                    Statistics = ["Sum", "Average", "Maximum"],
                    ExtendedStatistics = ["p55", "p90", "p95"]
                };

                var response = await cloudWatchClient.GetMetricStatisticsAsync(request);

                if (response.Datapoints.Count > 0)
                {
                    Console.WriteLine($"\n{metricName}:");
                    foreach (var datapoint in response.Datapoints.OrderBy(d => d.Timestamp))
                    {
                        Console.WriteLine($"  Timestamp: {datapoint.Timestamp:yyyy-MM-dd HH:mm:ss}");
                        if (datapoint.Sum.HasValue)
                            Console.WriteLine($"    Sum: {datapoint.Sum:N2}");
                        if (datapoint.Average.HasValue)
                            Console.WriteLine($"    Average: {datapoint.Average:N2}");
                        if (datapoint.Maximum.HasValue)
                            Console.WriteLine($"    Maximum: {datapoint.Maximum:N2}");
                        if (datapoint.ExtendedStatistics.ContainsKey("p55"))
                            Console.WriteLine($"    p55: {datapoint.ExtendedStatistics["p55"]:N2}");
                        if (datapoint.ExtendedStatistics.ContainsKey("p90"))
                            Console.WriteLine($"    p90: {datapoint.ExtendedStatistics["p90"]:N2}");
                        if (datapoint.ExtendedStatistics.ContainsKey("p95"))
                            Console.WriteLine($"    p95: {datapoint.ExtendedStatistics["p95"]:N2}");
                    }
                }
                else
                {
                    Console.WriteLine($"\n{metricName}: No data available");
                }
            }
            catch (Exception ex)
            {
                await PublishCloudWatchMetricAsync(CloudWatchMetricFetchFailureMetric, 1, tableName, metricName);
                Console.WriteLine($"\n{metricName}: Error retrieving metrics - {ex.Message}");
            }
        }

        public Task PublishCloudWatchMetricAsync(string metricName, double value, string tableName, string operation)
            => PublishCloudWatchMetricAsync(metricName, value, tableName, operation, StandardUnit.Milliseconds);

        public async Task PublishCloudWatchMetricAsync(string metricName, double value, string tableName, string operation, StandardUnit unit)
        {
            try
            {
                var metricDatum = new MetricDatum
                {
                    MetricName = metricName,
                    Timestamp = DateTime.UtcNow,
                    Value = value,
                    Unit = unit,
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "TableName", Value = tableName },
                        new Dimension { Name = "Operation", Value = operation }
                    }
                };

                var request = new PutMetricDataRequest
                {
                    Namespace = DynamoDBNamespace,
                    MetricData = new List<MetricDatum> { metricDatum }
                };

                await cloudWatchClient.PutMetricDataAsync(request);
                Console.WriteLine($"Published CloudWatch metric: {metricName} for table {tableName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CloudWatch publish error: {ex.Message}");
            }
        }
    }
}
