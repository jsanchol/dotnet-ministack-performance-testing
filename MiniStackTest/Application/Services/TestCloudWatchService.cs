using Amazon.CloudWatch.Model;
using Infrastructure.Observability;

namespace Application.Services
{
    public class TestCloudWatchService : TestingCloudWatchClient
    {
        public TestCloudWatchService()
        {
        }

        public async Task ReadDynamoDBMetricsAsync(string tableName)
        {
            Console.WriteLine($"Fetching CloudWatch metrics for table: {tableName}");
            Console.WriteLine("=".PadRight(80, '='));

            // Get metrics for last 1 hour
            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime.AddHours(-1);

            //TODO:
            var availableMetrics = await cloudWatchClient.ListMetricsAsync();
            foreach (var metric in availableMetrics.Metrics)
            {
                Console.WriteLine($"Available metric: {metric.MetricName} in namespace {metric.Namespace}");
                await GetMetricAsync(tableName, metric.MetricName, startTime, endTime);
            }

            // // Read Capacity Units (RCU)
            // await GetMetricAsync(tableName, ConsumedRCUMetric, startTime, endTime);

            // // Write Capacity Units (WCU)
            // await GetMetricAsync(tableName, ConsumedWCUMetric, startTime, endTime);

            // // User Errors
            // await GetMetricAsync(tableName, UserErrorsMetric, startTime, endTime);

            // // System Errors
            // await GetMetricAsync(tableName, SystemErrorsMetric, startTime, endTime);

            // // Latency metrics
            // await GetMetricAsync(tableName, SuccessfulRequestLatencyMetric, startTime, endTime);

            // // Query count
            // await GetMetricAsync(tableName, QueryMetric, startTime, endTime);

            // // Scan count
            // await GetMetricAsync(tableName, ScanMetric, startTime, endTime);
        }
    }
}
