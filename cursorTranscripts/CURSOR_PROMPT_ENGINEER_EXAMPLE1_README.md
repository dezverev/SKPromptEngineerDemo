# Cursor Prompt Engineer Example 1 Summary

## Overview
This document summarizes the work done to fix the test script and implement proper tool routing for weather queries using system prompts.

## Steps Performed

### Initial Request
- **User Request**: Make `run-tests.ps1` work

### Script Fixes
- Fixed argument passing for verbose flag (`--verbose` only when `-Verbose` switch provided)
- Added proper exit code capture using `$LASTEXITCODE`
- Implemented safe directory handling with `try/finally` to ensure return to original directory
- Script now supports:
  - Normal mode: `.\run-tests.ps1`
  - Verbose mode: `.\run-tests.ps1 -Verbose` or `.\run-tests.ps1 -v`

### Weather Query Issue
- **User Request**: Why isn't `GeolocationPlugin.GetCoordinates` being called for weather queries?
- **Problem Identified**: Model was using hardcoded Seattle coordinates instead of calling the geolocation plugin

### Solution Implementation
- Added system message to `StandardKernel.cs` and `TestRunner.cs` to enforce proper tool usage
- Initial system message required geolocation for all location-based queries
- **Issue**: System message was too broad, triggering geolocation for general questions like "What is the capital of France?"
- **Refinement**: Updated system message to only apply to weather-related queries

### Function Selection Issue
- **Problem**: Model was calling `GetTemperatureByCoordinates` instead of `GetWeatherByCoordinates` for weather queries
- **Solution**: Updated system message to specify which function to use:
  - `WeatherPlugin.GetWeatherByCoordinates` for general weather queries
  - `WeatherPlugin.GetTemperatureByCoordinates` for temperature-only queries

### Final System Message
- Requires `GeolocationPlugin.GetCoordinates` first for weather queries (even if coordinates are known)
- Specifies which weather function to use based on query type
- Only applies to weather-related queries, not general knowledge questions

## Outcome
- All 6 tests pass
- Weather queries now properly call `GeolocationPlugin.GetCoordinates` first, then `WeatherPlugin.GetWeatherByCoordinates`
- System prompts enforce proper tool routing without interfering with general knowledge questions
