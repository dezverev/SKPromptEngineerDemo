using SKRoutingStyles.IntegrationTesterApp;

// Parse command line arguments
var verbose = args.Contains("--verbose", StringComparer.OrdinalIgnoreCase) || 
              args.Contains("-v", StringComparer.OrdinalIgnoreCase);

// Define test cases
var testCases = new List<TestCase>
{
    new()
    {
        Name = "Simple Addition",
        Prompt = "What is 5 + 3?",
        ExpectedToolsToCall = { "MathPlugin.Add" },
        ExpectedToolsNotToCall = { "MathPlugin.Subtract" },
        ResponseMustContain = { "8" }
    },
    new()
    {
        Name = "Subtraction Test",
        Prompt = "Calculate 20 minus 7",
        ExpectedToolsToCall = { "MathPlugin.Subtract" },
        ExpectedToolsNotToCall = { "MathPlugin.Add" },
        ResponseMustContain = { "13" }
    },
    new()
    {
        Name = "No Math Needed",
        Prompt = "What is the capital of France?",
        ExpectedToolsNotToCall = { "MathPlugin.Add", "MathPlugin.Subtract" },
        ResponseMustContain = { "Paris" }
    },
    new()
    {
        Name = "Geolocation Query",
        Prompt = "What are the coordinates for Paris?",
        ExpectedToolsToCall = { "GeolocationPlugin.GetCoordinates" },
        ExpectedToolsNotToCall = { "MathPlugin.Add", "MathPlugin.Subtract" },
        ResponseMustContain = { "Paris", "Latitude", "Longitude" }
    },
    new()
    {
        Name = "Weather Query",
        Prompt = "What's the weather like in Seattle?",
        ExpectedToolsToCall = { "GeolocationPlugin.GetCoordinates", "WeatherPlugin.GetWeatherByCoordinates" },
        ExpectedToolsNotToCall = { "MathPlugin.Add", "MathPlugin.Subtract" },
        ResponseMustContain = { "Seattle", "weather" }
    },
    new()
    {
        Name = "Temperature Query",
        Prompt = "What's the temperature in Tokyo?",
        ExpectedToolsToCall = { "GeolocationPlugin.GetCoordinates", "WeatherPlugin.GetWeatherByCoordinates" },
        ExpectedToolsNotToCall = { "MathPlugin.Add", "MathPlugin.Subtract" },
        ResponseMustContain = { "Tokyo", "temperature" }
    }
};

// Run tests
var runner = new TestRunner("http://localhost:11434/v1", "granite4:3b", verbose);
var results = new List<TestResult>();

if (verbose)
{
    Console.WriteLine("=== VERBOSE MODE ENABLED ===\n");
    Console.WriteLine($"Configuration:");
    Console.WriteLine($"  Endpoint: http://localhost:11434/v1");
    Console.WriteLine($"  Model: granite4:3b");
    Console.WriteLine($"  Test Cases: {testCases.Count}\n");
}

Console.WriteLine($"Running {testCases.Count} test cases...\n");

foreach (var testCase in testCases)
{
    if (verbose)
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"TEST: {testCase.Name}");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Prompt: {testCase.Prompt}");
        Console.WriteLine($"Expected Tools to Call: {string.Join(", ", testCase.ExpectedToolsToCall)}");
        Console.WriteLine($"Expected Tools NOT to Call: {string.Join(", ", testCase.ExpectedToolsNotToCall)}");
        Console.WriteLine($"Response Must Contain: {string.Join(", ", testCase.ResponseMustContain)}");
        Console.WriteLine();
    }

    Console.Write($"Test: {testCase.Name}... ");
    var result = await runner.RunTestAsync(testCase);
    results.Add(result);

    if (result.Passed)
    {
        Console.WriteLine("✓ PASSED");
    }
    else
    {
        Console.WriteLine("✗ FAILED");
        if (!string.IsNullOrEmpty(result.Error))
            Console.WriteLine($"  Error: {result.Error}");
        foreach (var failure in result.Failures)
            Console.WriteLine($"  {failure}");
    }

    if (verbose)
    {
        Console.WriteLine($"\n--- Detailed Results ---");
        Console.WriteLine($"Duration: {result.DurationMs}ms");
        Console.WriteLine($"Function Calls: {string.Join(", ", result.FunctionCalls)}");
        Console.WriteLine($"Full Response ({result.Response.Length} chars):");
        Console.WriteLine($"  {result.Response}");
        Console.WriteLine($"Verification:");
        Console.WriteLine($"  ✓ Expected tools called: {testCase.ExpectedToolsToCall.Count > 0}");
        Console.WriteLine($"  ✓ Forbidden tools not called: {testCase.ExpectedToolsNotToCall.Count > 0}");
        Console.WriteLine($"  ✓ Response contains keywords: {testCase.ResponseMustContain.Count > 0}");
    }
    else
    {
        Console.WriteLine($"  Called: {string.Join(", ", result.FunctionCalls)}");
    }
    Console.WriteLine();
}

// Summary
var passed = results.Count(r => r.Passed);
var failed = results.Count - passed;
var totalDuration = results.Sum(r => r.DurationMs);

Console.WriteLine($"Summary: {passed} passed, {failed} failed out of {results.Count} tests");
if (verbose)
{
    Console.WriteLine($"Total Duration: {totalDuration}ms");
    Console.WriteLine($"Average Duration: {totalDuration / results.Count}ms per test");
}

return failed > 0 ? 1 : 0;
