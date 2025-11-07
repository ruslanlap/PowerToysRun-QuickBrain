# QuickBrain - PowerToys Run Plugin

QuickBrain is a smart calculator plugin for PowerToys Run that provides advanced mathematical computations, unit conversions, date calculations, and optional AI-powered natural language processing.

## Features

- **Mathematical Operations**: Arithmetic, algebraic, trigonometric, logarithmic, and statistical functions
- **Unit Conversions**: Length, weight, temperature, volume, area, time, speed, and data storage
- **Date Calculations**: Date arithmetic, differences, timezone conversions, and relative dates
- **Logic Evaluation**: Boolean logic, bitwise operations, and truth tables
- **Natural Language Processing**: Query understanding (optional, AI-powered)
- **History Management**: Track and search previous calculations with context actions
- **Offline First**: All core functionality works without internet connection
- **AI Integration**: Optional OpenAI, Anthropic, HuggingFace, and OpenRouter API support for complex natural language queries

## Prerequisites

- **PowerToys**: Version 0.70.0 or later
- **.NET**: .NET 9.0 SDK (for building from source)
- **Operating System**: Windows 10/11

## Installation

### Option 1: Install from Release (Recommended)

1. Download the latest release from the [GitLab repository](https://gitlab.com/info.ucucenter/powertoysrun-quickbrain/-/releases)
2. Extract the zip file to a temporary location
3. Copy the `QuickBrain` folder to your PowerToys Run plugins directory:
   ```
   C:\Users\<YourUsername>\AppData\Local\Microsoft\PowerToys\PowerToys Run\Plugins\QuickBrain
   ```
4. Restart PowerToys or PowerToys Run

### Option 2: Build from Source

1. Clone the repository:
   ```bash
   git clone https://gitlab.com/info.ucucenter/powertoysrun-quickbrain.git
   cd powertoysrun-quickbrain
   ```

2. Build the plugin:
   ```bash
   dotnet build QuickBrain.sln --configuration Release
   ```

3. Copy the output to PowerToys plugins directory:
   ```bash
   # Default PowerToys plugin location:
   # C:\Users\<YourUsername>\AppData\Local\Microsoft\PowerToys\PowerToys Run\Plugins\QuickBrain
   ```

### Verification

After installation:
1. Open PowerToys Run (default: `Alt+Space`)
2. Type `qb` and press Enter
3. You should see QuickBrain plugin results appear
4. Try a simple calculation: `qb 2 + 2` should show `4`

## Usage

Activate PowerToys Run (default: `Alt+Space`) and type the action keyword `qb` followed by your query:

### Mathematical Operations

#### Basic Arithmetic
```
qb 2 + 2                    # Addition: 4
qb 10 - 3                   # Subtraction: 7
qb 5 * 6                    # Multiplication: 30
qb 15 / 3                   # Division: 5
qb 2 ^ 8                    # Exponentiation: 256
qb sqrt(25)                 # Square root: 5
qb cbrt(27)                 # Cube root: 3
```

#### Advanced Mathematics
```
qb sin(pi/4)                # Trigonometric: 0.7071
qb cos(60)                  # Cosine (degrees): 0.5
qb tan(45)                  # Tangent: 1
qb log(100)                 # Natural logarithm: 4.6052
qb log10(1000)              # Base-10 logarithm: 3
qb exp(2)                   # Exponential: 7.3891
qb abs(-5.5)                # Absolute value: 5.5
qb floor(3.7)               # Floor: 3
qb ceil(3.2)                # Ceiling: 4
qb round(3.14159, 2)        # Round to 2 decimals: 3.14
```

#### Statistics
```
qb mean([1,2,3,4,5])        # Mean: 3
qb median([1,3,5,7,9])      # Median: 5
qb mode([1,2,2,3,4])        # Mode: 2
qb stddev([1,2,3,4,5])      # Standard deviation: 1.5811
qb variance([1,2,3,4,5])    # Variance: 2.5
```

### Unit Conversions

#### Length
```
qb 100 cm to inches         # 39.3701 inches
qb 5 miles to km            # 8.04672 km
qb 10 feet to meters        # 3.048 meters
qb 1000 yards to miles      # 0.568182 miles
```

#### Weight
```
qb 70 kg to pounds          # 154.324 pounds
qb 200 lbs to kg            # 90.7185 kg
qb 1000 g to ounces         # 35.274 ounces
qb 2 tons to kg             # 2032.09 kg
```

#### Temperature
```
qb 32 F to C                # 0 Celsius
qb 100 C to F               # 212 Fahrenheit
qb 0 K to C                 # -273.15 Celsius
qb 25 C to K                # 298.15 Kelvin
```

#### Volume
```
qb 1 gallon to liters       # 3.78541 liters
qb 500 ml to cups           # 2.11338 cups
qb 2 liters to quarts       # 2.11338 quarts
qb 100 cubic meters to liters # 100000 liters
```

#### Data Storage
```
qb 1 GB to MB               # 1024 MB
qb 1000 MB to GB            # 0.976562 GB
qb 1 TB to bytes            # 1099511627776 bytes
qb 1 PB to GB               # 1048576 GB
```

### Date Calculations

```
qb days between 2024-01-01 and 2024-12-31    # 365 days
qb weeks between 2024-01-01 and 2024-06-30   # 26 weeks
qb months between 2020-01-01 and 2024-01-01  # 48 months
qb 2024-01-01 + 30 days                     # 2024-01-31
qb 2024-12-31 - 90 days                     # 2024-10-02
qb today + 7 days                           # Current date + 7 days
qb now + 2 hours                            # Current time + 2 hours
```

### Logic Evaluation

```
qb true AND false           # false
qb true OR false            # true
qb NOT true                 # false
qb true XOR false           # true
qb 5 > 3 AND 2 < 4         # true
qb (true OR false) AND true # true
qb 5 & 3                    # Bitwise AND: 1
qb 5 | 3                    # Bitwise OR: 7
qb 5 ^ 3                    # Bitwise XOR: 6
```

### Natural Language Processing (AI-powered)

When AI features are enabled, you can use natural language:

```
qb what is the square root of 144
qb calculate 15 percent of 200
qb how many days until christmas
qb convert 100 dollars to euros
qb what is 2 to the power of 10
qb calculate the area of a circle with radius 5
```

## Configuration

Settings are stored in `settings.json` in the plugin directory. You can edit this file directly or use the PowerToys Run settings interface.

### Basic Settings

- **MaxResults**: Maximum number of results to display (1-20, default: 5)
- **Precision**: Decimal precision for calculations (0-28, default: 10)
- **AngleUnit**: Unit for trigonometric functions (Radians/Degrees/Gradians, default: Radians)
- **Theme**: UI theme (Auto/Light/Dark, default: Auto)

### History Settings

- **EnableHistory**: Enable calculation history (default: true)
- **HistoryLimit**: Maximum history entries (0-1000, default: 100)

### AI and Natural Language Settings

- **EnableNaturalLanguage**: Enable NLP query understanding (default: false)
- **EnableAIParsing**: Enable AI-powered query parsing (default: false)
- **EnableAiCalculations**: Enable AI-powered calculations (default: false)
- **AiTimeout**: AI API timeout in milliseconds (1000-60000, default: 10000)
- **OfflineFirst**: Try offline methods before AI (default: true)

### AI Provider Configuration

#### OpenAI
```json
{
  "aiProvider": "OpenAI",
  "openAiApiKey": "your-openai-api-key-here",
  "openAiModel": "gpt-4"
}
```

#### Anthropic
```json
{
  "aiProvider": "Anthropic",
  "anthropicApiKey": "your-anthropic-api-key-here",
  "anthropicModel": "claude-3-sonnet-20240229"
}
```

#### HuggingFace
```json
{
  "useHuggingFace": true,
  "huggingFaceApiKey": "your-huggingface-api-key-here",
  "huggingFaceModel": "microsoft/DialoGPT-medium"
}
```

#### OpenRouter
```json
{
  "useOpenRouter": true,
  "openRouterApiKey": "your-openrouter-api-key-here",
  "openRouterModel": "anthropic/claude-3-haiku"
}
```

### Example Configuration File

```json
{
  "maxResults": 5,
  "precision": 10,
  "angleUnit": "Radians",
  "theme": "Auto",
  "enableHistory": true,
  "historyLimit": 100,
  "enableNaturalLanguage": true,
  "enableAiParsing": true,
  "enableAiCalculations": false,
  "aiProvider": "OpenAI",
  "openAiApiKey": "sk-...",
  "openAiModel": "gpt-4",
  "aiTimeout": 10000,
  "offlineFirst": true
}
```

### Getting API Keys

#### OpenAI
1. Visit [OpenAI Platform](https://platform.openai.com/)
2. Create an account or sign in
3. Navigate to API Keys section
4. Create a new API key
5. Copy the key and add it to your settings

#### Anthropic
1. Visit [Anthropic Console](https://console.anthropic.com/)
2. Create an account or sign in
3. Navigate to API Keys section
4. Create a new API key
5. Copy the key and add it to your settings

#### HuggingFace
1. Visit [Hugging Face](https://huggingface.co/)
2. Create an account or sign in
3. Navigate to Settings → Access Tokens
4. Create a new token with read permissions
5. Copy the token and add it to your settings

#### OpenRouter
1. Visit [OpenRouter](https://openrouter.ai/)
2. Create an account or sign in
3. Navigate to API Keys section
4. Create a new API key
5. Copy the key and add it to your settings

### Security Note

- API keys are masked in logs and never displayed in plain text
- Store your API keys securely and do not commit them to version control
- Consider using environment variables for additional security
- Only enable AI features when you have a valid API key configured

## History and Context Actions

QuickBrain maintains a history of your calculations with convenient context actions:

### History Features
- **Automatic Storage**: All calculations are automatically saved to history
- **FIFO Management**: Oldest entries are removed when the limit is reached
- **Persistent Storage**: History is saved to `%AppData%\PowerToys\QuickBrain\history.json`

### Context Actions
Right-click on any result to access context actions:
- **Copy**: Copy the result to clipboard
- **Recalculate**: Re-run the calculation and add to history
- **Add to History**: Manually add the result to history
- **Explain via AI**: Get an AI explanation (when AI features are enabled)

## Troubleshooting

### Plugin Not Showing

1. **Verify Installation**: Ensure the QuickBrain folder is in the correct PowerToys Run plugins directory
2. **Restart PowerToys**: Completely restart PowerToys or PowerToys Run
3. **Check Dependencies**: Ensure .NET 9.0 runtime is installed on your system
4. **Verify Files**: Make sure all required files are present (QuickBrain.dll, plugin.json, etc.)

### Calculations Not Working

1. **Check Syntax**: Verify you're using correct mathematical syntax
2. **Check Settings**: Ensure your settings.json is valid JSON
3. **Review Logs**: Check PowerToys logs for error messages
4. **Test Simple Cases**: Start with basic calculations like `qb 2 + 2`

### AI Features Not Working

1. **API Key**: Verify your API key is correct and active
2. **Internet Connection**: Ensure you have an active internet connection
3. **API Limits**: Check if you've exceeded API rate limits
4. **Provider Settings**: Verify the correct AI provider is configured
5. **Timeout**: Increase AiTimeout if experiencing timeouts

### Performance Issues

1. **Reduce MaxResults**: Lower the number of maximum results
2. **Disable AI**: Turn off AI features if not needed
3. **Clear History**: Reduce HistoryLimit or clear history file
4. **Check Resources**: Ensure sufficient system resources are available

### History Not Saving

1. **Permissions**: Ensure PowerToys has write permissions to AppData directory
2. **Disk Space**: Check available disk space
3. **Enable History**: Verify EnableHistory is set to true
4. **Check File**: Verify history.json file is not corrupted

### Common Error Messages

- **"Plugin failed to load"**: Check .NET runtime and plugin files
- **"Invalid API key"**: Verify API key configuration and permissions
- **"Network timeout"**: Increase AiTimeout or check internet connection
- **"Invalid expression"**: Check mathematical syntax and formatting

## Offline-First Design

QuickBrain is designed to work primarily offline:

### Always Available (No Internet Required)
- Basic arithmetic operations
- Advanced mathematical functions
- Unit conversions
- Date calculations
- Logic evaluation
- Expression parsing
- History management

### Internet Required (Optional)
- Natural language processing
- AI-powered calculations
- AI query parsing
- AI explanations

### Fallback Behavior
When `OfflineFirst` is enabled (default):
1. QuickBrain attempts to process queries using offline methods first
2. If offline processing fails and AI is enabled, it falls back to AI
3. This ensures maximum reliability and minimum dependency on internet connectivity

## Development

### Prerequisites for Development
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Git

### Build

```bash
dotnet build QuickBrain.sln
```

### Test

```bash
dotnet test QuickBrain.sln
```

### Clean

```bash
dotnet clean QuickBrain.sln
```

### Restore Dependencies

```bash
dotnet restore QuickBrain.sln
```

### Build and Package

#### Linux/macOS
```bash
./build-and-zip.sh
```

#### Windows
```batch
build-and-zip.bat
```

#### Using Makefile (Cross-platform)
```bash
make package
# Or for the full cycle:
make all
```

#### Manual Build Steps
```bash
# Restore dependencies
dotnet restore QuickBrain.sln

# Build in Release mode
dotnet build QuickBrain.sln --configuration Release

# Run tests
dotnet test QuickBrain.sln

# Package manually (if needed)
mkdir -p package/QuickBrain
cp QuickBrain/bin/Release/net9.0/* package/QuickBrain/
cp QuickBrain/plugin.json package/QuickBrain/
cp -r QuickBrain/Images package/QuickBrain/
cd package && zip -r ../QuickBrain-v0.1.0.zip .
```

All build scripts will:
1. Restore NuGet packages
2. Clean previous builds
3. Build the solution in Release mode
4. Run all tests
5. Package the plugin binaries into a distributable zip file
6. Display installation instructions

### Development Quick Start

For rapid development cycles:
```bash
make quick    # Build and test in Debug mode
make dev-build    # Debug build only
make dev-test     # Test only
make install      # Build and install to local PowerToys directory
```

## Project Structure

```
QuickBrain/
├── QuickBrain/                 # Main plugin project
│   ├── Plugin.cs               # PowerToys plugin entry point
│   ├── Settings.cs             # Configuration and settings management
│   ├── CalculationResult.cs    # Result model
│   ├── HistoryManager.cs       # Calculation history management
│   ├── plugin.json             # Plugin metadata
│   ├── QuickBrain.csproj       # Project file
│   ├── Images/                 # Plugin icons and resources
│   └── Modules/                # Calculation modules
│       ├── MathEngine.cs       # Mathematical operations
│       ├── Converter.cs        # Unit conversions
│       ├── DateCalc.cs         # Date calculations
│       ├── LogicEval.cs        # Logic evaluation
│       ├── ExpressionParser.cs # Expression parsing
│       └── NaturalLanguageProcessor.cs # NLP and AI integration
├── QuickBrain.Tests/           # Unit tests
│   ├── HistoryManagerTests.cs  # History management tests
│   ├── PluginContextActionsTests.cs # Plugin context actions tests
│   └── QuickBrain.Tests.csproj # Test project file
├── build-and-zip.sh            # Linux/macOS build script
├── build-and-zip.bat           # Windows build script
├── Makefile                    # Cross-platform build system
├── CHANGELOG.md                # Version history and changes
├── README.md                   # This file
├── QuickBrain.sln              # Solution file
└── .gitignore                  # Git ignore file
```

## Technology Stack

- **.NET 9.0**: Target framework
- **C# 12**: Language version
- **PowerToys SDK**: PowerToys.Common.UI, Wox.Plugin
- **xUnit**: Testing framework
- **System.Text.Json**: JSON serialization

## Roadmap

### Completed ✅
- [x] Project scaffolding and build system
- [x] Mathematical expression parser
- [x] Basic arithmetic operations
- [x] Trigonometric and logarithmic functions
- [x] Unit conversion engine
- [x] Date calculation module
- [x] Logic evaluation module
- [x] History persistence and search
- [x] Natural language processing
- [x] OpenAI API integration
- [x] Anthropic API integration
- [x] HuggingFace API integration
- [x] OpenRouter API integration
- [x] History persistence with context actions
- [x] Comprehensive test coverage
- [x] Documentation and examples

### Future Enhancements 🚧
- [ ] Settings UI panel (visual configuration)
- [ ] Plugin icons and themes
- [ ] Additional unit conversion categories
- [ ] Graphing capabilities
- [ ] Statistical analysis tools
- [ ] Custom function definitions
- [ ] Voice input support
- [ ] Multi-language support
- [ ] Performance optimizations
- [ ] Extended AI model support

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Write tests for your changes
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes with clear messages
6. Push to your fork and submit a merge request

## License

This project is open source. See the LICENSE file for details.

## Support

For issues, questions, or feature requests, please open an issue on the GitLab repository:
https://gitlab.com/info.ucucenter/powertoysrun-quickbrain/-/issues

## Acknowledgments

- PowerToys team for the excellent plugin framework
- Contributors and users of QuickBrain

## Project Status

**Status**: Stable Release (v0.1.0)

QuickBrain is a fully functional PowerToys Run plugin with comprehensive features:

- ✅ All core modules implemented and tested
- ✅ Full mathematical computation engine
- ✅ Complete unit conversion system
- ✅ Date and time calculations
- ✅ Logic evaluation capabilities
- ✅ History management with persistence
- ✅ AI integration with multiple providers
- ✅ Comprehensive test coverage
- ✅ Complete documentation

The plugin is ready for production use and actively maintained.
