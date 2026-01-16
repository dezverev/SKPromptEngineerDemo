# Self-Prompt-Engineering Test Framework

A Semantic Kernel project demonstrating how automated testing enables AI coding assistants (like GitHub Copilot, Cursor, etc.) to **prompt-engineer themselves** by generating plugins, testing them, and self-correcting based on test failures.

## 🎯 Core Concept

This project showcases a **self-correcting development workflow** where:

1. **AI tools generate plugins** based on natural language prompts
2. **Automated tests validate** that plugins work correctly and route properly
3. **Test failures guide corrections** - the AI can iteratively fix:
   - Function parameters and types
   - Function descriptions (`[Description]` attributes)
   - Function structure and naming
   - Plugin registration
4. **Human oversight ensures quality** - developers review generated code and test assertions

## 🔄 How It Works

### The Self-Prompt-Engineering Cycle

```
┌─────────────────────────────────────────────────────────┐
│ 1. Prompt AI: "Add a weather plugin"                     │
│    ↓                                                      │
│ 2. AI generates WeatherPlugin.cs                         │
│    ↓                                                      │
│ 3. Tests run automatically (via .cursor/rules)             │
│    ↓                                                      │
│ 4. Test fails: "Expected tool not called"                  │
│    ↓                                                      │
│ 5. AI analyzes failure, fixes Description/parameters      │
│    ↓                                                      │
│ 6. Tests pass → Plugin works correctly                    │
└─────────────────────────────────────────────────────────┘
```

### Example: Weather Plugin Development

**Initial Prompt:**
> "Add a weather plugin that gets weather for a location"

**AI generates:**
- `WeatherPlugin.cs` with `GetWeather(string location)`
- Registers plugin in `StandardKernel.cs`
- Adds test case expecting `WeatherPlugin.GetWeather`

**Test runs and fails:**
```
✗ FAILED
  Expected tool 'WeatherPlugin.GetWeather' was not called
  Called: GeolocationPlugin.GetCoordinates, WeatherPlugin.GetWeatherByCoordinates
```

**AI self-corrects:**
- Realizes weather needs coordinates first
- Updates test to expect: `GeolocationPlugin.GetCoordinates` → `WeatherPlugin.GetWeatherByCoordinates`
- Or adjusts function signature to match actual routing behavior

**Tests pass** → Plugin correctly integrated!

## 🛡️ Preventing Routing Regressions

The test framework ensures that when new plugins are added:

1. **Existing plugins still route correctly** - Tests verify expected tools are called
2. **Forbidden tools aren't accidentally called** - Tests verify unwanted tools aren't invoked
3. **Response quality is maintained** - Tests verify responses contain required keywords
4. **Function call chains work** - Tests verify multi-step workflows (e.g., geolocation → weather)

### Test Structure

Each test case validates:
- ✅ **Expected tools are called** - Ensures correct routing
- ✅ **Forbidden tools are NOT called** - Prevents routing regressions
- ✅ **Response contains keywords** - Ensures quality output

```csharp
new()
{
    Name = "Weather Query",
    Prompt = "What's the weather like in Seattle?",
    ExpectedToolsToCall = { 
        "GeolocationPlugin.GetCoordinates", 
        "WeatherPlugin.GetWeatherByCoordinates" 
    },
    ExpectedToolsNotToCall = { 
        "MathPlugin.Add", 
        "MathPlugin.Subtract" 
    },
    ResponseMustContain = { "Seattle", "weather" }
}
```

## 📁 Project Structure

```
SKRoutingStyles/
├── src/
│   ├── StandardKernel/          # Main kernel implementation
│   │   └── StandardKernel.cs   # Plugin registration & chat interface
│   ├── Plugins/                 # Plugin implementations
│   │   ├── MathPlugin.cs
│   │   ├── GeolocationPlugin.cs
│   │   └── WeatherPlugin.cs
│   ├── IntegrationTesterApp/    # Test framework
│   │   ├── Program.cs           # Test cases
│   │   ├── TestRunner.cs        # Test execution
│   │   └── TestLogger.cs        # Function call capture
│   └── Utilities/               # Shared utilities
├── .cursor/
│   └── rules/
│       └── StandardKernelDevGuide.mdc  # Auto-test rule
└── run-tests.ps1                # Test runner script
```

## 🚀 Usage

### Running Tests

```powershell
# Run all tests
.\run-tests.ps1

# Run with verbose output
.\run-tests.ps1 --verbose
```

### Adding a New Plugin (AI-Assisted Workflow)

1. **Prompt your AI assistant:**
   > "Add a [feature] plugin that [does something]"

2. **AI generates:**
   - Plugin class in `src/Plugins/`
   - Registration in `StandardKernel.cs`
   - Test case in `IntegrationTesterApp/Program.cs`

3. **Tests run automatically** (via `.cursor/rules/StandardKernelDevGuide.mdc`)

4. **If tests fail:**
   - AI analyzes failures
   - AI fixes function descriptions, parameters, or test expectations
   - Tests re-run automatically

5. **Human review:**
   - ✅ Verify test assertions are correct
   - ✅ Ensure code quality and safety
   - ✅ Check that routing logic makes sense

### Example Test Output

```
Test: Weather Query... 
💬 [USER QUERY] What's the weather like in Seattle?
💡 [RESPONSE]
🔧 [FUNCTION CALL] GeolocationPlugin.GetCoordinates(location=Seattle)
✅ [FUNCTION RESULT] GeolocationPlugin.GetCoordinates = Latitude: 47.60621, Longitude: -122.33207
🔧 [FUNCTION CALL] WeatherPlugin.GetWeatherByCoordinates(latitude=47.60621, longitude=-122.33207)
✅ [FUNCTION RESULT] WeatherPlugin.GetWeatherByCoordinates = Current weather: Clear sky, Temperature: 42.8°F...
✓ PASSED
  Called: GeolocationPlugin.GetCoordinates, WeatherPlugin.GetWeatherByCoordinates
```

## 🧠 The Human Role

While AI handles code generation and self-correction, **human developers provide critical oversight**:

### What Humans Review

1. **Test Assertions**
   - Are the expected tools correct?
   - Do forbidden tools make sense?
   - Are response keywords appropriate?

2. **Code Quality**
   - Is the generated code safe?
   - Are there security concerns?
   - Does it follow project patterns?

3. **Routing Logic**
   - Does the function chain make sense?
   - Are there edge cases not covered?
   - Is the plugin design appropriate?

## ⚠️ Important Notes

### This is a Basic Example

- **Iterations may be needed** - Complex plugins might require multiple test/feedback cycles
- **Not all edge cases covered** - Tests focus on happy paths and basic routing
- **Human review is essential** - Don't blindly trust generated code or test assertions

### Limitations

- Tests validate routing but not all business logic
- Some routing issues may require domain knowledge
- Complex multi-step workflows may need manual test design

## 🔧 Technical Details

### Test Framework Components

1. **TestRunner** - Executes tests against the kernel
2. **TestLogger** - Captures function calls via logging
3. **TestCase** - Defines expected behavior
4. **TestResult** - Validates actual vs expected

### Logging System

The framework uses structured logging with emojis for clarity:
- 💬 `[USER QUERY]` - Original user prompt
- 💡 `[RESPONSE]` - Response start marker
- 🔧 `[FUNCTION CALL]` - Plugin function invoked
- ✅ `[FUNCTION RESULT]` - Function return value

### Auto-Testing Rule

The `.cursor/rules/StandardKernelDevGuide.mdc` file automatically:
- Triggers tests when `src/StandardKernel/` files change
- Requires tests to pass before considering work complete
- Provides feedback for iterative improvement

## 📚 Example Plugins

### MathPlugin
Basic arithmetic operations (Add, Subtract)

### GeolocationPlugin
Uses OpenMeteo API to convert location names to coordinates

### WeatherPlugin
Uses OpenMeteo API to get weather data from coordinates

**Key Insight:** This framework demonstrates that with proper test infrastructure, AI coding assistants can engage in a form of "prompt engineering themselves" - iteratively improving their output based on test feedback, with human oversight ensuring quality and correctness.
