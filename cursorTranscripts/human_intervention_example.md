# Weather plugin GetTemperatureByCoordinates removal
_Exported on 1/15/2026 at 20:17:18 PST from Cursor (2.3.35)_

---

**User**

We dont need the GetTemperatureByCoordinates of @src/Plugins/WeatherPlugin.cs  we can remove that since weather will cover it, fix it, adjust the prompts and retest everything

---

**Cursor**


Reviewing `WeatherPlugin.cs` and `StandardKernel` to identify required changes.



Checking test cases for references to `GetTemperatureByCoordinates`:



Removing `GetTemperatureByCoordinates` and updating references. Creating a todo list and making the changes:



Updating todos and running tests:



Running tests with the correct path:



Weather and temperature tests are failing. Running with verbose output to see what's happening:



All tests pass. Summarizing changes:



## Changes Made:

1. **Removed `GetTemperatureByCoordinates` method** from `WeatherPlugin.cs` — the method that returned only temperature has been removed.

2. **Updated system prompts** in both `StandardKernel.cs` and `TestRunner.cs`:
   - Removed references to `GetTemperatureByCoordinates`
   - Updated to state that `GetWeatherByCoordinates` should be used for all weather-related queries, including temperature queries

3. **Updated the Temperature Query test case** in `Program.cs`:
   - Changed expected tools from `WeatherPlugin.GetTemperatureByCoordinates` to `GeolocationPlugin.GetCoordinates` and `WeatherPlugin.GetWeatherByCoordinates`
   - This ensures the test expects the proper workflow (geolocation first, then weather)

## Test Results:

All 6 tests pass:
- Simple Addition ✓
- Subtraction Test ✓
- No Math Needed ✓
- Geolocation Query ✓
- Weather Query ✓ (now uses `GetWeatherByCoordinates`)
- Temperature Query ✓ (now uses `GetWeatherByCoordinates` instead of the removed method)

The system now uses `GetWeatherByCoordinates` for all weather queries, including temperature-only queries, since it returns temperature along with other weather information.

---

**User**

Test: No Math Needed... 💬 [USER QUERY] What is the capital of France?
💡 [RESPONSE]
I'm sorry, but I can't retrieve additional information beyond basic facts using only the available tools. The GeolocationPlugin.GetCoordinates would first need a location string like "France" to convert to coordinates before anything else could be done. However, this system doesn’t have access to external knowledge databases for answering factual questions like the capital of France directly without relying on location data processing. Thus, I can confirm that the capital of France is **Paris**, but I don't have a tool to verify it automatically from the APIs provided here.
✓ PASSED
  Called: 

general queries should still work...

---

**Cursor**


The system prompt is too restrictive, forcing tool usage for general knowledge questions. Updating it so general queries work without tools, while still enforcing tool usage for weather queries. Checking the current prompts:



Updating the system prompt to clarify: use tools only for weather queries; answer general knowledge questions directly.



All tests pass. The system prompt now:

1. **Allows general knowledge questions** to be answered directly — "What is the capital of France?" now returns "The capital of France is **Paris**." without unnecessary tool-related explanations.

2. **Still enforces proper tool usage** for weather queries — weather and temperature queries still use GeolocationPlugin first, then WeatherPlugin.GetWeatherByCoordinates.

## Test Results:
- Simple Addition ✓
- Subtraction Test ✓
- No Math Needed ✓ (now answers directly: "The capital of France is **Paris**.")
- Geolocation Query ✓
- Weather Query ✓
- Temperature Query ✓

The system now handles both general knowledge questions and weather queries appropriately.

