using Amazon.CloudWatch;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Infrastructure.Observability;
using System.Net;

namespace Infrastructure.Compute
{
    public class TestingLambdaClient
    {
        private const string SuccessMetricName = "LambdaInvocationSuccess";
        private const string FailureMetricName = "LambdaInvocationFailure";
        public AmazonLambdaClient client;
        public string endpoint;
        private string AccessKey;
        private string SecretKey;
        private readonly TestingCloudWatchClient cloudWatchClient;

        public TestingLambdaClient(TestingCloudWatchClient cloudWatchClient)
        {
            endpoint = System.Environment.GetEnvironmentVariable("LAMBDA_ENDPOINT") ?? "http://localhost:4566";
            AccessKey = System.Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "test";
            SecretKey = System.Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "test";
            client = SetUpClient();
            this.cloudWatchClient = cloudWatchClient;
        }

        internal AmazonLambdaClient SetUpClient()
        {
            AmazonLambdaConfig lambdaConfig = new()
            {
                ServiceURL = endpoint,
                AuthenticationRegion = "us-east-1"
            };

            AWSCredentials basicCredentials = new BasicAWSCredentials(AccessKey, SecretKey);
            return new AmazonLambdaClient(basicCredentials, lambdaConfig);
        }

        public async Task<bool> DoesFunctionExistAsync(string functionName)
        {
            try
            {
                await client.GetFunctionAsync(new GetFunctionRequest
                {
                    FunctionName = functionName
                });
                return true;
            }
            catch (AmazonLambdaException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking lambda function existence: {ex.Message}");
                return false;
            }
        }

        public async Task CreateFunctionAsync(string functionName, byte[] zipFile, string handler, Runtime runtime, string role)
        {
            try
            {
                using MemoryStream zipStream = new(zipFile);
                var request = new CreateFunctionRequest
                {
                    FunctionName = functionName,
                    Runtime = runtime,
                    Role = role,
                    Handler = handler,
                    Code = new FunctionCode
                    {
                        ZipFile = zipStream
                    },
                    Publish = true
                };

                var response = await client.CreateFunctionAsync(request);
                Console.WriteLine($"Created Lambda function '{functionName}' with ARN: {response.FunctionArn}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lambda function creation failed: {ex.Message}");
            }
        }

        public async Task<string> InvokeAsync(string functionName, string payload)
        {
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var request = new InvokeRequest
                {
                    FunctionName = functionName,
                    Payload = payload,
                    LogType = LogType.None
                };

                var response = await client.InvokeAsync(request);
                using var reader = new StreamReader(response.Payload);
                string output = await reader.ReadToEndAsync();
                stopWatch.Stop();
                await cloudWatchClient.PublishCloudWatchMetricAsync(SuccessMetricName, stopWatch.ElapsedMilliseconds, functionName, "Invoke", StandardUnit.Milliseconds, TestingCloudWatchClient.LambdaMetricNamespace);
                return output;
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                await cloudWatchClient.PublishCloudWatchMetricAsync(FailureMetricName, stopWatch.ElapsedMilliseconds, functionName, "Invoke", StandardUnit.Milliseconds, TestingCloudWatchClient.LambdaMetricNamespace);
                return $"Lambda invocation failed: {ex.Message}";
            }
        }
    }
}
