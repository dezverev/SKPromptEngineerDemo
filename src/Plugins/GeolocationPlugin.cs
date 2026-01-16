using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SKRoutingStyles.Plugins;

/// <summary>
/// Geolocation plugin that uses OpenMeteo to get coordinates from location names.
/// </summary>
public class GeolocationPlugin
{
    private readonly ILogger<GeolocationPlugin>? _logger;
    private static readonly HttpClient HttpClient = new();

    public GeolocationPlugin(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<GeolocationPlugin>();
    }

    [KernelFunction]
    [Description("Gets the latitude and longitude coordinates for a location name or postal code")]
    public async Task<string> GetCoordinates(
        [Description("The city name, location name, or postal code")] string location)
    {
        _logger?.LogInformation("🔧 [FUNCTION CALL] GeolocationPlugin.GetCoordinates(location={Location})", location);

        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1";
            var response = await HttpClient.GetFromJsonAsync<GeocodingResponse>(url);

            if (response?.Results == null || response.Results.Length == 0)
            {
                var error = $"No coordinates found for location: {location}";
                _logger?.LogWarning("⚠️ [FUNCTION RESULT] GeolocationPlugin.GetCoordinates = {Error}", error);
                return error;
            }

            var result = response.Results[0];
            var coordinates = $"Latitude: {result.Latitude}, Longitude: {result.Longitude}";
            
            _logger?.LogInformation("✅ [FUNCTION RESULT] GeolocationPlugin.GetCoordinates = {Result}", coordinates);
            return coordinates;
        }
        catch (Exception ex)
        {
            var error = $"Error getting coordinates for {location}: {ex.Message}";
            _logger?.LogError(ex, "❌ [FUNCTION ERROR] GeolocationPlugin.GetCoordinates");
            return error;
        }
    }

    [KernelFunction]
    [Description("Gets the latitude for a location name or postal code")]
    public async Task<double> GetLatitude(
        [Description("The city name, location name, or postal code")] string location)
    {
        _logger?.LogInformation("🔧 [FUNCTION CALL] GeolocationPlugin.GetLatitude(location={Location})", location);

        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1";
            var response = await HttpClient.GetFromJsonAsync<GeocodingResponse>(url);

            if (response?.Results == null || response.Results.Length == 0)
            {
                _logger?.LogWarning("⚠️ [FUNCTION RESULT] GeolocationPlugin.GetLatitude = No location found");
                return 0.0;
            }

            var latitude = response.Results[0].Latitude;
            _logger?.LogInformation("✅ [FUNCTION RESULT] GeolocationPlugin.GetLatitude = {Result}", latitude);
            return latitude;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ [FUNCTION ERROR] GeolocationPlugin.GetLatitude");
            return 0.0;
        }
    }

    [KernelFunction]
    [Description("Gets the longitude for a location name or postal code")]
    public async Task<double> GetLongitude(
        [Description("The city name, location name, or postal code")] string location)
    {
        _logger?.LogInformation("🔧 [FUNCTION CALL] GeolocationPlugin.GetLongitude(location={Location})", location);

        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1";
            var response = await HttpClient.GetFromJsonAsync<GeocodingResponse>(url);

            if (response?.Results == null || response.Results.Length == 0)
            {
                _logger?.LogWarning("⚠️ [FUNCTION RESULT] GeolocationPlugin.GetLongitude = No location found");
                return 0.0;
            }

            var longitude = response.Results[0].Longitude;
            _logger?.LogInformation("✅ [FUNCTION RESULT] GeolocationPlugin.GetLongitude = {Result}", longitude);
            return longitude;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ [FUNCTION ERROR] GeolocationPlugin.GetLongitude");
            return 0.0;
        }
    }

    private class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public GeocodingResult[]? Results { get; set; }
    }

    private class GeocodingResult
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
