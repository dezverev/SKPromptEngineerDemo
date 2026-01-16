using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SKRoutingStyles.Plugins;

/// <summary>
/// Math plugin with basic arithmetic operations.
/// </summary>
public class MathPlugin
{
    private readonly ILogger<MathPlugin>? _logger;

    public MathPlugin(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<MathPlugin>();
    }

    [KernelFunction]
    [Description("Adds two numbers together")]
    public double Add(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        _logger?.LogInformation("🔧 [FUNCTION CALL] MathPlugin.Add(a={A}, b={B})", a, b);
        var result = a + b;
        _logger?.LogInformation("✅ [FUNCTION RESULT] MathPlugin.Add = {Result}", result);
        return result;
    }

    [KernelFunction]
    [Description("Subtracts the second number from the first")]
    public double Subtract(
        [Description("The number to subtract from")] double a,
        [Description("The number to subtract")] double b)
    {
        _logger?.LogInformation("🔧 [FUNCTION CALL] MathPlugin.Subtract(a={A}, b={B})", a, b);
        var result = a - b;
        _logger?.LogInformation("✅ [FUNCTION RESULT] MathPlugin.Subtract = {Result}", result);
        return result;
    }
}
