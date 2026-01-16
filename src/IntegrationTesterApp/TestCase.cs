namespace SKRoutingStyles.IntegrationTesterApp;

/// <summary>
/// Defines a test case for kernel behavior verification.
/// </summary>
public class TestCase
{
    public string Name { get; set; } = string.Empty;
    public string KernelType { get; set; } = "StandardKernel";
    public string Endpoint { get; set; } = "http://localhost:11434/v1";
    public string ModelId { get; set; } = "granite4:3b";
    public string Prompt { get; set; } = string.Empty;
    public List<string> ExpectedToolsToCall { get; set; } = new();
    public List<string> ExpectedToolsNotToCall { get; set; } = new();
    public List<string> ResponseMustContain { get; set; } = new();
}
