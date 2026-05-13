using Infrastructure.Compute;
using System.IO;
using System.IO.Compression;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Infrastructure.Observability;

namespace Application.Services
{
    public class TestLambdaService : TestingLambdaClient
    {
        public const string DefaultLambdaFunctionName = "TestLambdaFunction";
        private const string DefaultHandler = "index.handler";
        private static readonly Runtime DefaultRuntime = Runtime.Nodejs18X;
        private const string DefaultLambdaRole = "arn:aws:iam::000000000000:role/lambda-role";

        public TestLambdaService(TestingCloudWatchClient cloudWatchClient) : base(cloudWatchClient)
        {
        }

        public async Task CreateLambdaFunctionIfMissingAsync(string functionName = DefaultLambdaFunctionName)
        {
            if (await DoesFunctionExistAsync(functionName))
            {
                Console.WriteLine($"Lambda function '{functionName}' already exists.");
                return;
            }

            byte[] zipFile = CreateLambdaZipPackage();
            Console.WriteLine($"Creating lambda function '{functionName}'...");
            await CreateFunctionAsync(functionName, zipFile, DefaultHandler, DefaultRuntime, DefaultLambdaRole);
        }

        public async Task InvokeLambdaFunctionAsync(string functionName = DefaultLambdaFunctionName)
        {
            string payload = "{ \"message\": \"Hello from LambdaApp\" }";
            Console.WriteLine($"Invoking lambda function '{functionName}'...");
            await InvokeAsync(functionName, payload);
        }

        private byte[] CreateLambdaZipPackage()
        {
            const string handlerContent = "exports.handler = async (event) => {\n" +
                                          "  console.log('Lambda event received:', JSON.stringify(event));\n" +
                                          "  return {\n" +
                                          "    statusCode: 200,\n" +
                                          "    body: JSON.stringify({\n" +
                                          "      message: 'Hello from MiniStack Lambda test',\n" +
                                          "      event,\n" +
                                          "      timestamp: new Date().toISOString()\n" +
                                          "    })\n" +
                                          "  };\n" +
                                          "};\n";

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("index.js");
                using var entryWriter = new StreamWriter(entry.Open());
                entryWriter.Write(handlerContent);
            }

            return memoryStream.ToArray();
        }
    }
}
