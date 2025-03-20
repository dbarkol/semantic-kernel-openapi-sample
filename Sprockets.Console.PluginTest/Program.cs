using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using dotenv.net;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Sprockets.Console.PluginTest.Models;
using System.Text.Json;
using Microsoft.VisualBasic;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // services.AddLogging(builder =>
        // {
        //     builder.ClearProviders();
        //     builder.AddConsole();
        //     builder.SetMinimumLevel(LogLevel.Debug);
        // });

        LoadEnvFile();
        string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Environment variable 'AZURE_OPENAI_ENDPOINT' is not set.");

        string deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
            ?? throw new InvalidOperationException("Environment variable 'AZURE_OPENAI_DEPLOYMENT_NAME' is not set.");        

        services.AddKernel()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, new DefaultAzureCredential());  

        var serviceProvider = services.BuildServiceProvider();
        var kernel = serviceProvider.GetRequiredService<Kernel>();

        //await TestOrdersPlugin(kernel);
        await AgentOpenApiPlugin(kernel);        
    }

    static async Task AgentOpenApiPlugin(Kernel kernel)
    {
        var ordersAgentPrompt = await ReadYamlFile("PromptTemplates/Agents/OrdersAgent.yaml");
        PromptTemplateConfig templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(ordersAgentPrompt);
        ChatCompletionAgent agent = new(templateConfig, new KernelPromptTemplateFactory())
        {
            Kernel = kernel
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

        ChatHistory chat = []; 
        
        var newOrder = new Order { Id = 12, Product = "Widget1", Quantity = 10 };
        string serializedOrder = JsonSerializer.Serialize(newOrder, new JsonSerializerOptions { WriteIndented = true });
        chat.AddUserMessage(serializedOrder);

        await foreach (ChatMessageContent response in agent.InvokeAsync(chat))
        {
            Console.WriteLine(response);
        }
    }

    static async Task TestOrdersPlugin(Kernel kernel)
    {
        PromptExecutionSettings settings = new() { 
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

        // Get orders
        var getOrdersResult = await kernel.InvokePromptAsync("Get all orders", new(settings));    
        Console.WriteLine(getOrdersResult);

        // Test with natural language prompt
        string userPrompt = "Please create two orders: one for product 'Widget A' with id 101 and quantity 5, and another for product 'Widget B' with id 102 and quantity 10.";
        var createOrderResult = await kernel.InvokePromptAsync(userPrompt, new(settings));
        Console.WriteLine(createOrderResult);

        // Test with JSON payload
        var newOrder = new Order { Id = 12, Product = "Widget1", Quantity = 10 };
        string serializedOrder = JsonSerializer.Serialize(newOrder, new JsonSerializerOptions { WriteIndented = true });
        var jsonPromptResult = await kernel.InvokePromptAsync($"{serializedOrder}", new(settings));
        Console.WriteLine(jsonPromptResult);
    }

    private static async Task<string> ReadYamlFile(string filename)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File '{filename}' not found at '{filePath}'.");

        return await File.ReadAllTextAsync(filePath);
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

class LoggingHandler : DelegatingHandler
{
    public LoggingHandler() : base(new HttpClientHandler()) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = request.Content != null ? await request.Content.ReadAsStringAsync() : "No content";
        Console.WriteLine($"Constructed URI: {request.Method} - {request.RequestUri} - {content}");
        return await base.SendAsync(request, cancellationToken);
    }
}

