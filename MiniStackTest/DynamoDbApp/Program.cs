using Application.Services;

namespace DynamoDbTestingApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool resetMinistack = Environment.GetEnvironmentVariable("DYNAMODB_RESET") == "true";

            Console.WriteLine(resetMinistack
                ? "DYNAMODB_RESET is set to true. Ministack will be reset before running tests."
                : "DYNAMODB_RESET is not set to true. Ministack will NOT be reset before running tests.");

            TestDynamoDBService testService = new();

            if(resetMinistack)
            {
                Console.WriteLine("Resetting Ministack...");
                await testService.ResetMinistackAsync();
            }

            int iteration = Environment.GetEnvironmentVariable("TEST_ITERATIONS") != null ? int.Parse(Environment.GetEnvironmentVariable("TEST_ITERATIONS")!) : 1;

            List<Task> setupTasks = [];

            while (iteration-- > 0)
            {
                setupTasks.AddRange(
                [
                    testService.RunTestsTableGSIProvisioned(),
                    testService.RunTestsTableGSIOnDemand()
                ]);
            }

            await Task.WhenAll(setupTasks);

            // Plan for more comprehensive testing
            Console.WriteLine("\nPerformance Testing Plan:");
            Console.WriteLine("1. Test with different RCUs (Read Capacity Units) to simulate throttling.");
            Console.WriteLine("2. Docker Monitor CPU, memory usage during tests.");
            Console.WriteLine("3. Compare Query vs Scan vs GSI Query in terms of latency and throughput.");
            // refactoring code to be more modular and reusable for different test cases like other AWS services.
            //NEXT: lambda and RDS
        }
    }

}