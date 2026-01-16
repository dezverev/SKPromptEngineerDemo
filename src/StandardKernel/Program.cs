using SKRoutingStyles.Core;

// Parse args: support --quiet or -q flag
var argList = args.ToList();
var quiet = argList.RemoveAll(a => a.Equals("--quiet", StringComparison.OrdinalIgnoreCase) 
                                || a.Equals("-q", StringComparison.OrdinalIgnoreCase)) > 0;

if (argList.Count == 0)
{
    Console.WriteLine("Usage: StandardKernel [--quiet] <prompt>");
    Console.WriteLine("Example: StandardKernel --quiet \"What is 15 + 27?\"");
    return 1;
}

var prompt = string.Join(" ", argList);

// Setup and run
var kernel = new StandardKernel();
kernel.Setup("http://localhost:11434/v1", "granite4:3b", quiet);
await kernel.ChatAsync(prompt);

return 0;
