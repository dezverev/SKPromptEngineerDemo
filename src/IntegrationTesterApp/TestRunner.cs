using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKRoutingStyles.Plugins;

namespace SKRoutingStyles.IntegrationTesterApp;

/// <summary>
/// Runs test cases and verifies results.
/// </summary>
public class TestRunner
{
    private readonly string _endpoint;
    private readonly string _modelId;
    private readonly bool _verbose;

    public TestRunner(string endpoint, string modelId, bool verbose = false)
    {
        _endpoint = endpoint;
        _modelId = modelId;
        _verbose = verbose;
    }

    public async Task<TestResult> RunTestAsync(TestCase testCase)
    {
        var result = new TestResult { TestCase = testCase };
        var startTime = DateTime.UtcNow;

        try
        {
            if (_verbose)
            {
                Console.WriteLine("--- Kernel Setup ---");
                Console.WriteLine($"Endpoint: {_endpoint}");
                Console.WriteLine($"Model: {_modelId}");
            }
            // Setup logging to capture function calls
            var services = new ServiceCollection();
            var loggerProvider = new TestLoggerProvider();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(loggerProvider);
                builder.SetMinimumLevel(LogLevel.Information);
            });
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            // Build kernel
            var kernelBuilder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: _modelId,
                endpoint: new Uri(_endpoint),
                apiKey: "not-needed");
#pragma warning restore SKEXP0010

            kernelBuilder.Plugins.AddFromObject(new MathPlugin(loggerFactory), "Math");
            kernelBuilder.Plugins.AddFromObject(new GeolocationPlugin(loggerFactory), "Geolocation");
            kernelBuilder.Plugins.AddFromObject(new WeatherPlugin(loggerFactory), "Weather");
            var kernel = kernelBuilder.Build();

            // Get test loggers to capture function calls from all plugins
            var mathLogger = loggerProvider.GetTestLogger("SKRoutingStyles.Plugins.MathPlugin");
            var geolocationLogger = loggerProvider.GetTestLogger("SKRoutingStyles.Plugins.GeolocationPlugin");
            var weatherLogger = loggerProvider.GetTestLogger("SKRoutingStyles.Plugins.WeatherPlugin");

            // Log the user query
            var kernelLogger = loggerFactory.CreateLogger("StandardKernel");
            kernelLogger.LogInformation("💬 [USER QUERY] {Query}", testCase.Prompt);

            // Run the prompt
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            
            // Add system message to enforce proper tool usage
            history.AddSystemMessage("You are a helpful assistant. For general knowledge questions (like capitals, facts, etc.), answer directly using your knowledge. For weather, temperature, or other weather-related queries about a specific location, you MUST first use GeolocationPlugin.GetCoordinates to get the coordinates for the location name, then use WeatherPlugin.GetWeatherByCoordinates with those coordinates. Never use hardcoded coordinates for weather queries.");
            
            history.AddUserMessage(testCase.Prompt);

            // Log response start
            kernelLogger.LogInformation("💡 [RESPONSE]");

            var response = "";
            await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history, settings, kernel))
            {
                if (chunk.Content != null)
                {
                    Console.Write(chunk.Content);
                    response += chunk.Content;
                }
            }
            Console.WriteLine();

            result.Response = response;
            
            // Combine function calls from all plugin loggers
            var allFunctionCalls = new List<string>();
            if (mathLogger != null)
                allFunctionCalls.AddRange(mathLogger.GetFunctionCalls());
            if (geolocationLogger != null)
                allFunctionCalls.AddRange(geolocationLogger.GetFunctionCalls());
            if (weatherLogger != null)
                allFunctionCalls.AddRange(weatherLogger.GetFunctionCalls());
            result.FunctionCalls = allFunctionCalls;

            if (_verbose)
            {
                Console.WriteLine($"\n--- Execution Complete ---");
                Console.WriteLine($"Response Length: {response.Length} characters");
                Console.WriteLine($"Function Calls Made: {result.FunctionCalls.Count}");
            }

            // Verify expectations
            result.Passed = VerifyTest(testCase, result);
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Passed = false;
            if (_verbose)
            {
                Console.WriteLine($"\n--- Error Details ---");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            }
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            result.DurationMs = (int)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    private bool VerifyTest(TestCase testCase, TestResult result)
    {
        var allPassed = true;

        if (_verbose)
        {
            Console.WriteLine("\n--- Verification ---");
        }

        // Check expected tools were called
        foreach (var expectedTool in testCase.ExpectedToolsToCall)
        {
            var wasCalled = result.FunctionCalls.Contains(expectedTool);
            if (_verbose)
            {
                Console.WriteLine($"  Expected '{expectedTool}': {(wasCalled ? "✓ CALLED" : "✗ NOT CALLED")}");
            }
            if (!wasCalled)
            {
                result.Failures.Add($"Expected tool '{expectedTool}' was not called");
                allPassed = false;
            }
        }

        // Check expected tools were NOT called
        foreach (var forbiddenTool in testCase.ExpectedToolsNotToCall)
        {
            var wasCalled = result.FunctionCalls.Contains(forbiddenTool);
            if (_verbose)
            {
                Console.WriteLine($"  Forbidden '{forbiddenTool}': {(!wasCalled ? "✓ NOT CALLED" : "✗ WAS CALLED")}");
            }
            if (wasCalled)
            {
                result.Failures.Add($"Tool '{forbiddenTool}' was called but should not have been");
                allPassed = false;
            }
        }

        // Check response contains keywords
        foreach (var keyword in testCase.ResponseMustContain)
        {
            var contains = result.Response.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            if (_verbose)
            {
                Console.WriteLine($"  Response contains '{keyword}': {(contains ? "✓ YES" : "✗ NO")}");
            }
            if (!contains)
            {
                result.Failures.Add($"Response should contain '{keyword}' but does not");
                allPassed = false;
            }
        }

        if (_verbose)
        {
            Console.WriteLine($"\nOverall: {(allPassed ? "✓ ALL CHECKS PASSED" : "✗ SOME CHECKS FAILED")}");
        }

        return allPassed;
    }
}

public class TestResult
{
    public TestCase TestCase { get; set; } = null!;
    public bool Passed { get; set; }
    public string Response { get; set; } = string.Empty;
    public List<string> FunctionCalls { get; set; } = new();
    public List<string> Failures { get; set; } = new();
    public string? Error { get; set; }
    public int DurationMs { get; set; }
}
