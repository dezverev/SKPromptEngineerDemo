using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKRoutingStyles.Plugins;
using SKRoutingStyles.Utilities;

namespace SKRoutingStyles.Core;

/// <summary>
/// Simple Semantic Kernel wrapper with Setup and Chat methods.
/// </summary>
public class StandardKernel
{
    private Kernel? _kernel;
    private ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Configures the kernel with endpoint, model, and optional logging.
    /// </summary>
    public void Setup(string endpoint, string modelId, bool quiet = false)
    {
        // Setup logging
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            if (quiet)
            {
                builder.ClearProviders();
                builder.AddProvider(new QuietConsoleLoggerProvider());
                builder.SetMinimumLevel(LogLevel.Information);
            }
            else
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            }
        });
        _loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

        // Build the kernel
        var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            endpoint: new Uri(endpoint),
            apiKey: "not-needed");
#pragma warning restore SKEXP0010

        // Register the Math plugin
        builder.Plugins.AddFromObject(new MathPlugin(_loggerFactory), "Math");

        // Register the Geolocation plugin
        builder.Plugins.AddFromObject(new GeolocationPlugin(_loggerFactory), "Geolocation");

        // Register the Weather plugin
        builder.Plugins.AddFromObject(new WeatherPlugin(_loggerFactory), "Weather");

        _kernel = builder.Build();

        if (!quiet)
        {
            var logger = _loggerFactory.CreateLogger<StandardKernel>();
            logger.LogInformation("Kernel ready. Endpoint: {Endpoint}, Model: {Model}", endpoint, modelId);
        }
    }

    /// <summary>
    /// Sends a message and streams the response to console.
    /// </summary>
    public async Task<string> ChatAsync(string message)
    {
        if (_kernel == null)
            throw new InvalidOperationException("Call Setup() first.");

        // Log the user query
        if (_loggerFactory != null)
        {
            var logger = _loggerFactory.CreateLogger<StandardKernel>();
            logger.LogInformation("💬 [USER QUERY] {Query}", message);
        }

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        
        // Add system message to enforce proper tool usage
        history.AddSystemMessage("You are a helpful assistant. For general knowledge questions (like capitals, facts, etc.), answer directly using your knowledge. For weather, temperature, or other weather-related queries about a specific location, you MUST first use GeolocationPlugin.GetCoordinates to get the coordinates for the location name, then use WeatherPlugin.GetWeatherByCoordinates with those coordinates. Never use hardcoded coordinates for weather queries.");
        
        history.AddUserMessage(message);

        // Log response start
        if (_loggerFactory != null)
        {
            var logger = _loggerFactory.CreateLogger<StandardKernel>();
            logger.LogInformation("💡 [RESPONSE]");
        }

        var response = "";
        await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history, settings, _kernel))
        {
            if (chunk.Content != null)
            {
                Console.Write(chunk.Content);
                response += chunk.Content;
            }
        }
        Console.WriteLine();

        return response;
    }
}
