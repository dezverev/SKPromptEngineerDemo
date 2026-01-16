using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SKRoutingStyles.Plugins;

/// <summary>
/// Weather plugin that uses OpenMeteo API to get real weather data.
/// </summary>
public class WeatherPlugin
{
    private readonly ILogger<WeatherPlugin>? _logger;
    private static readonly HttpClient HttpClient = new();

    public WeatherPlugin(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<WeatherPlugin>();
    }

    [KernelFunction]
    [Description("Gets the current weather for a location using latitude and longitude coordinates")]
    public async Task<string> GetWeatherByCoordinates(
        [Description("The latitude coordinate")] double latitude,
        [Description("The longitude coordinate")] double longitude)
    {
        _logger?.LogInformation("🔧 [FUNCTION CALL] WeatherPlugin.GetWeatherByCoordinates(latitude={Latitude}, longitude={Longitude})", latitude, longitude);

        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code,relative_humidity_2m,wind_speed_10m&temperature_unit=fahrenheit&wind_speed_unit=mph";
            var response = await HttpClient.GetFromJsonAsync<WeatherResponse>(url);

            if (response?.Current == null)
            {
                var error = $"No weather data available for coordinates ({latitude}, {longitude})";
                _logger?.LogWarning("⚠️ [FUNCTION RESULT] WeatherPlugin.GetWeatherByCoordinates = {Error}", error);
                return error;
            }

            var current = response.Current;
            var condition = GetWeatherCondition(current.WeatherCode);
            var result = $"Current weather: {condition}, Temperature: {current.Temperature2m}°F, Humidity: {current.RelativeHumidity2m}%, Wind Speed: {current.WindSpeed10m} mph";

            _logger?.LogInformation("✅ [FUNCTION RESULT] WeatherPlugin.GetWeatherByCoordinates = {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            var error = $"Error getting weather for coordinates ({latitude}, {longitude}): {ex.Message}";
            _logger?.LogError(ex, "❌ [FUNCTION ERROR] WeatherPlugin.GetWeatherByCoordinates");
            return error;
        }
    }

    private static string GetWeatherCondition(int weatherCode)
    {
        // WMO Weather interpretation codes (WW)
        // Simplified mapping for common codes
        return weatherCode switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 => "Foggy",
            48 => "Depositing rime fog",
            51 => "Light drizzle",
            53 => "Moderate drizzle",
            55 => "Dense drizzle",
            56 => "Light freezing drizzle",
            57 => "Dense freezing drizzle",
            61 => "Slight rain",
            63 => "Moderate rain",
            65 => "Heavy rain",
            66 => "Light freezing rain",
            67 => "Heavy freezing rain",
            71 => "Slight snow fall",
            73 => "Moderate snow fall",
            75 => "Heavy snow fall",
            77 => "Snow grains",
            80 => "Slight rain showers",
            81 => "Moderate rain showers",
            82 => "Violent rain showers",
            85 => "Slight snow showers",
            86 => "Heavy snow showers",
            95 => "Thunderstorm",
            96 => "Thunderstorm with slight hail",
            99 => "Thunderstorm with heavy hail",
            _ => "Unknown condition"
        };
    }

    private class WeatherResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; set; }
    }

    private class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public int RelativeHumidity2m { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed10m { get; set; }
    }
}
