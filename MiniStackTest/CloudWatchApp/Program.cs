using Application.Services;

namespace CloudWatchApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("CloudWatch DynamoDB Metrics Reader");
            Console.WriteLine("Running metric collection for 1 hour against: TestTableGSIProvisioned and TestTableGSIOnDemand\n");

            TestCloudWatchService testService = new();
            DateTime endTime = DateTime.UtcNow.AddHours(1);
            await Task.Delay(TimeSpan.FromSeconds(60)); // Short delay to allow DynamoDB tests to populate some metrics before we start reading
            int cycle = 1;

            while (DateTime.UtcNow < endTime)
            {
                Console.WriteLine($"Starting cycle {cycle} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

                await testService.ReadDynamoDBMetricsAsync("TestTableGSIProvisioned");
                Console.WriteLine("\n" + new string('-', 80) + "\n");
                await testService.ReadDynamoDBMetricsAsync("TestTableGSIOnDemand");

                cycle++;
                if (DateTime.UtcNow < endTime)
                {
                    TimeSpan remaining = endTime - DateTime.UtcNow;
                    TimeSpan delay = remaining < TimeSpan.FromMinutes(1) ? remaining : TimeSpan.FromMinutes(1);
                    Console.WriteLine($"Waiting {delay.TotalSeconds:N0} seconds before next cycle...\n");
                    await Task.Delay(delay);
                }
            }

            Console.WriteLine("\nOne-hour metrics collection completed.");
        }
    }
}