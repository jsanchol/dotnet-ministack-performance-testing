using Infrastructure.Observability;

namespace Application.Services
{
    public class TestCloudWatchService : TestingCloudWatchClient
    {
        public TestCloudWatchService()
        {
        }

        public async Task PrintPublishedMetricsAsync()
        {
            // Get metrics for last 1 hour
            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime.AddHours(-1);

            //TODO:
            var availableMetrics = await cloudWatchClient.ListMetricsAsync();
            foreach (var metric in availableMetrics.Metrics)
            {
                Console.WriteLine(">".PadRight(80, '>'));
                Console.WriteLine($"Available metric: {metric.MetricName} in namespace {metric.Namespace}");
                
                metric.Dimensions.ForEach(d => Console.WriteLine($"Dimension: {d.Name} = {d.Value}"));
                //await GetMetricAsync(tableName, metric.MetricName, startTime, endTime);
            }
        }
    }
}
