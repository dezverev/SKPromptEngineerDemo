# Run-tests.ps1 script functionality
_Exported on 1/15/2026 at 20:11:17 PST from Cursor (2.3.35)_

---

**User**

make @run-tests.ps1 work

---

**Cursor**


Reviewing the test script and IntegrationTesterApp to identify issues.



Fixing the script: handling the verbose flag and capturing the exit code correctly.


```powershell
# Run IntegrationTesterApp tests
# Usage: .\run-tests.ps1 [--verbose|-v]

param(
    [switch]$Verbose
)

Write-Host "Running IntegrationTesterApp tests..." -ForegroundColor Cyan

$originalLocation = Get-Location
try {
    Set-Location "$PSScriptRoot\src\IntegrationTesterApp"
    
    if ($Verbose) {
        dotnet run -- --verbose
    } else {
        dotnet run
    }
    
    $exitCode = $LASTEXITCODE
} finally {
    Set-Location $originalLocation
}

exit $exitCode

```


Testing the script to verify it works:



The script works. It runs the tests and correctly captures the exit code (1 due to a test failure). Testing the verbose flag:



The script is working. Changes:

1. **Fixed argument passing**: Passes `--verbose` only when the `-Verbose` switch is provided, avoiding empty string issues.
2. **Proper exit code capture**: Uses `$LASTEXITCODE` to capture the exit code from `dotnet run`.
3. **Safe directory handling**: Uses `try/finally` to ensure we return to the original directory even if an error occurs.

The script now:
- Runs tests without the verbose flag
- Runs tests with `-Verbose` or `--verbose` for detailed output
- Returns the correct exit code (0 for success, 1 for failures)

You can use it like:
- `.\run-tests.ps1` - Normal mode
- `.\run-tests.ps1 -Verbose` or `.\run-tests.ps1 -v` - Verbose mode

The script is functional and ready to use.

---

**User**

Test: Weather Query... 💬 [USER QUERY] What's the weather like in Seattle?
💡 [RESPONSE]
🔧 [FUNCTION CALL] WeatherPlugin.GetWeatherByCoordinates(latitude=47.6062, longitude=-122.3321)
✅ [FUNCTION RESULT] WeatherPlugin.GetWeatherByCoordinates = Current weather: Clear sky, Temperature: 42.4°F, Humidity: 92%, Wind Speed: 5 mph
The current weather in Seattle is clear with a temperature of **42.4°F**, humidity at **92%**, and wind speed around **5 mph**. Enjoy your day!
✗ FAILED
  Expected tool 'GeolocationPlugin.GetCoordinates' was not called
  Called: WeatherPlugin.GetWeatherByCoordinates 

why this not calling?

---

**Cursor**


Investigating why `GeolocationPlugin.GetCoordinates` isn't being called. Checking the test case and the StandardKernel prompt engineering:



Checking the WeatherPlugin to understand its requirements:



The model is using known Seattle coordinates and skipping `GeolocationPlugin.GetCoordinates`. Adding a system message to require using the geolocation plugin first for location-based queries. Updating `StandardKernel` and `TestRunner`:



Adding a system message instructing the model to use the geolocation plugin first for location-based queries. Updating both `StandardKernel` and `TestRunner`:


```csharp
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
        history.AddSystemMessage("When a user asks about weather or location-based queries, you MUST first use the GeolocationPlugin.GetCoordinates function to get the coordinates for any location name, even if you know the coordinates. Never use hardcoded coordinates. Always call GeolocationPlugin.GetCoordinates first, then use the returned coordinates with WeatherPlugin functions.");
        
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

```

```csharp
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
            history.AddSystemMessage("When a user asks about weather or location-based queries, you MUST first use the GeolocationPlugin.GetCoordinates function to get the coordinates for any location name, even if you know the coordinates. Never use hardcoded coordinates. Always call GeolocationPlugin.GetCoordinates first, then use the returned coordinates with WeatherPlugin functions.");
            
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

```


Running tests to verify the fix:



The Weather Query test passes. The system message is too broad: it triggers geolocation for "What is the capital of France?" when it shouldn't. Refining the system message to only apply to weather queries:


```csharp
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
        history.AddSystemMessage("When a user asks about weather, temperature, or other weather-related queries for a location, you MUST first use the GeolocationPlugin.GetCoordinates function to get the coordinates for the location name, even if you know the coordinates. Never use hardcoded coordinates for weather queries. Always call GeolocationPlugin.GetCoordinates first, then use the returned coordinates with WeatherPlugin functions.");
        
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

```

```csharp
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
            history.AddSystemMessage("When a user asks about weather, temperature, or other weather-related queries for a location, you MUST first use the GeolocationPlugin.GetCoordinates function to get the coordinates for the location name, even if you know the coordinates. Never use hardcoded coordinates for weather queries. Always call GeolocationPlugin.GetCoordinates first, then use the returned coordinates with WeatherPlugin functions.");
            
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

```


Rerunning tests to verify all pass:



The model is calling `GetTemperatureByCoordinates` instead of `GetWeatherByCoordinates` for the weather query. Updating the system message to specify which function to use:


```csharp
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
        history.AddSystemMessage("When a user asks about weather, temperature, or other weather-related queries for a location, you MUST first use the GeolocationPlugin.GetCoordinates function to get the coordinates for the location name, even if you know the coordinates. Never use hardcoded coordinates for weather queries. Always call GeolocationPlugin.GetCoordinates first, then use the returned coordinates with WeatherPlugin functions. For general weather queries (asking 'what's the weather like' or 'weather'), use WeatherPlugin.GetWeatherByCoordinates. For temperature-only queries, use WeatherPlugin.GetTemperatureByCoordinates.");
        
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

```

```csharp
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
            history.AddSystemMessage("When a user asks about weather, temperature, or other weather-related queries for a location, you MUST first use the GeolocationPlugin.GetCoordinates function to get the coordinates for the location name, even if you know the coordinates. Never use hardcoded coordinates for weather queries. Always call GeolocationPlugin.GetCoordinates first, then use the returned coordinates with WeatherPlugin functions. For general weather queries (asking 'what's the weather like' or 'weather'), use WeatherPlugin.GetWeatherByCoordinates. For temperature-only queries, use WeatherPlugin.GetTemperatureByCoordinates.");
            
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

```


Rerunning tests to verify the fix:



All tests are passing.

## Summary

The model was skipping `GeolocationPlugin.GetCoordinates` for the Seattle weather query because it used hardcoded coordinates. It also used the wrong weather function (`GetTemperatureByCoordinates` instead of `GetWeatherByCoordinates`).

**The fix:** Added a system message in both `StandardKernel.cs` and `TestRunner.cs` that:
1. Requires using `GeolocationPlugin.GetCoordinates` first for weather queries, even if coordinates are known
2. Specifies which weather function to use:
   - `WeatherPlugin.GetWeatherByCoordinates` for general weather queries ("what's the weather like")
   - `WeatherPlugin.GetTemperatureByCoordinates` for temperature-only queries

The Weather Query test now calls both `GeolocationPlugin.GetCoordinates` and `WeatherPlugin.GetWeatherByCoordinates` as expected, and all 6 tests pass.

