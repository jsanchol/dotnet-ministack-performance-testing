using Application.Services;

namespace LambdaApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Lambda test app...");
            var testService = new TestLambdaService(new Infrastructure.Observability.TestingCloudWatchClient());

            await Task.Delay(TimeSpan.FromSeconds(10));

            await testService.CreateLambdaFunctionIfMissingAsync();

            int iterations = Environment.GetEnvironmentVariable("TEST_ITERATIONS") != null ? int.Parse(Environment.GetEnvironmentVariable("TEST_ITERATIONS")!) : 1;

            List<Task> setupTasks = [];

            while (iterations-- > 0)
            {
                setupTasks.Add(testService.InvokeLambdaFunctionAsync());
            }

            await Task.WhenAll(setupTasks);

            Console.WriteLine("Lambda test app completed.");}
    }
}
