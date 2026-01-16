# Ollama Setup Guide

This guide explains how to install Ollama and the `granite4:3b` model required for running SKRoutingStyles.

## What is Ollama?

Ollama is a tool that runs large language models locally on your machine. It provides a local API endpoint that's compatible with OpenAI's API format, making it perfect for testing Semantic Kernel applications without requiring API keys or internet connectivity for the LLM.

## Installation

### Windows

1. **Download Ollama:**
   - Visit [https://ollama.com/download](https://ollama.com/download)
   - Download the Windows installer
   - Run the installer and follow the setup wizard

2. **Verify Installation:**
   ```powershell
   ollama --version
   ```

3. **Start Ollama (if not running automatically):**
   - Ollama should start automatically as a service
   - If not, you can start it manually from the Start Menu or run: `ollama serve`

### macOS

1. **Install via Homebrew:**
   ```bash
   brew install ollama
   ```

2. **Or download directly:**
   - Visit [https://ollama.com/download](https://ollama.com/download)
   - Download the macOS installer
   - Open the `.dmg` file and drag Ollama to Applications

3. **Start Ollama:**
   ```bash
   ollama serve
   ```

### Linux

1. **Install via script:**
   ```bash
   curl -fsSL https://ollama.com/install.sh | sh
   ```

2. **Or install manually:**
   - Visit [https://ollama.com/download](https://ollama.com/download)
   - Download the appropriate package for your distribution
   - Install using your package manager

3. **Start Ollama:**
   ```bash
   ollama serve
   ```

## Installing the granite4:3b Model

The `granite4:3b` model is a 3-billion parameter model from IBM that's optimized for function calling and tool use - perfect for Semantic Kernel applications.

### Download the Model

```bash
ollama pull granite4:3b
```

This will download the model (approximately 2-3 GB) and make it available locally.

### Verify Installation

```bash
ollama list
```

You should see `granite4:3b` in the list of available models.

### Test the Model

```bash
ollama run granite4:3b "Hello, can you help me with math?"
```

## Configuration

### Default API Endpoint

Ollama runs a local API server on:
- **URL:** `http://localhost:11434/v1`
- **Port:** `11434`

This matches the configuration used in SKRoutingStyles:
- `StandardKernel.cs` uses: `http://localhost:11434/v1`
- `IntegrationTesterApp` uses: `http://localhost:11434/v1`

### Changing the Port (Optional)

If you need to use a different port, you can:

1. **Set environment variable:**
   ```bash
   # Windows PowerShell
   $env:OLLAMA_HOST="http://localhost:11435"
   
   # Linux/macOS
   export OLLAMA_HOST="http://localhost:11435"
   ```

2. **Update SKRoutingStyles configuration:**
   - Update `src/StandardKernel/Program.cs`
   - Update `src/IntegrationTesterApp/Program.cs`
   - Change the endpoint URL to match your port

## Troubleshooting

### Ollama Not Starting

**Windows:**
- Check if Ollama service is running: `Get-Service ollama`
- Restart the service: `Restart-Service ollama`
- Check Windows Event Viewer for errors

**macOS/Linux:**
- Check if port 11434 is already in use: `lsof -i :11434`
- Kill any existing Ollama processes: `pkill ollama`
- Restart: `ollama serve`

### Model Not Found

If you get an error about the model not being found:

```bash
# Verify the model is installed
ollama list

# If not listed, pull it again
ollama pull granite4:3b

# Verify it's available
ollama show granite4:3b
```

### Connection Refused Errors

If you see connection errors when running tests:

1. **Verify Ollama is running:**
   ```bash
   curl http://localhost:11434/api/tags
   ```
   Should return a JSON response with available models.

2. **Check firewall settings:**
   - Ensure port 11434 is not blocked
   - On Windows, check Windows Firewall
   - On Linux, check `ufw` or `iptables`

3. **Test the API endpoint:**
   ```bash
   curl http://localhost:11434/v1/models
   ```

### Model Performance

The `granite4:3b` model is relatively small (3B parameters) and should run on:
- **Minimum:** 8GB RAM
- **Recommended:** 16GB+ RAM
- **GPU:** Optional but recommended for faster inference

If you experience slow responses:
- Close other applications to free up memory
- Consider using a GPU-accelerated version (if available)
- Reduce the number of concurrent requests

## Alternative Models

If `granite4:3b` doesn't work well for your use case, you can try other models:

### Function-Calling Optimized Models:
```bash
# Llama 3.2 (good function calling)
ollama pull llama3.2

# Mistral (excellent function calling)
ollama pull mistral

# Qwen (good for tool use)
ollama pull qwen2.5
```

### To Use a Different Model:

1. Update `src/StandardKernel/Program.cs`:
   ```csharp
   kernel.Setup("http://localhost:11434/v1", "llama3.2", quiet);
   ```

2. Update `src/IntegrationTesterApp/Program.cs`:
   ```csharp
   var runner = new TestRunner("http://localhost:11434/v1", "llama3.2", verbose);
   ```

## Verification

After setup, verify everything works:

1. **Test Ollama API:**
   ```bash
   curl http://localhost:11434/v1/models
   ```

2. **Run SKRoutingStyles tests:**
   ```powershell
   .\run-tests.ps1
   ```

3. **Run StandardKernel directly:**
   ```bash
   cd src/StandardKernel
   dotnet run "What is 5 + 3?"
   ```

## Additional Resources

- **Ollama Documentation:** [https://ollama.com/docs](https://ollama.com/docs)
- **Ollama GitHub:** [https://github.com/ollama/ollama](https://github.com/ollama/ollama)
- **Available Models:** [https://ollama.com/library](https://ollama.com/library)
- **Granite Models:** [IBM Granite Models](https://www.ibm.com/products/watsonx-ai/granite)

## Next Steps

Once Ollama and the model are installed:

1. ✅ Verify Ollama is running: `ollama list`
2. ✅ Run the test suite: `.\run-tests.ps1`
3. ✅ Start developing new plugins!

---

**Note:** The first time you run a model, Ollama may need to download it, which can take several minutes depending on your internet connection. Subsequent runs will be much faster as the model is cached locally.
