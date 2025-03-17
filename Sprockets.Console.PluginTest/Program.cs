using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using dotenv.net;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel?view=semantic-kernel-dotnet

class LoggingHandler : DelegatingHandler
{
    public LoggingHandler() : base(new HttpClientHandler()) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Constructed URI: {request.RequestUri}");
        return await base.SendAsync(request, cancellationToken);
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        LoadEnvFile();

        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Environment variable 'AZURE_OPENAI_ENDPOINT' is not set.");

        string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
            ?? throw new InvalidOperationException("Environment variable 'AZURE_OPENAI_DEPLOYMENT_NAME' is not set.");        

        var credential = new DefaultAzureCredential();

        services.AddKernel()
            .AddAzureOpenAIChatCompletion(endpoint, deployment, credential);  

        var serviceProvider = services.BuildServiceProvider();

        // Retrieve Kernel instance
        var kernel = serviceProvider.GetRequiredService<Kernel>();

        await TestOrdersPlugin(kernel);
        Console.WriteLine("Plugin test completed.");
    }

    static async Task TestOrdersPlugin(Kernel kernel)
    {

        PromptExecutionSettings settings = new() { 
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0,
            TopP = 0.1,
            PresencePenalty = 0,
            FrequencyPenalty = 0,
            MaxTokens = 2000,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };        

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };        
     
        using var httpClient = new HttpClient(new LoggingHandler());
        var plugin = await kernel.ImportPluginFromOpenApiAsync(
            "OrderService", 
            new Uri("http://localhost:5275/swagger/v1/swagger.json"), 
            new OpenApiFunctionExecutionParameters(httpClient)
            {
                ServerUrlOverride = new Uri("http://localhost:5275/")
            }
        );

        var result = await kernel.InvokeAsync(plugin["GetAllOrders"], new KernelArguments());
        Console.WriteLine(result.ToString());

        try
        {
            var promptResult = await kernel.InvokePromptAsync("Invoke the GetAllOrders function", new(settings));
            Console.WriteLine(promptResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Semantic inference call error: {ex}");
        }
    }

    private static void LoadEnvFile()
    {
        string[] possiblePaths = {
            "../.env",
            ".env"
        };        

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                DotEnv.Load(options: new DotEnvOptions(
                    ignoreExceptions: true,
                    envFilePaths: new[] { path }
                ));
                return;
            }
        }     
    }    
}


