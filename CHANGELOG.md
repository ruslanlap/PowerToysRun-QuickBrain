# Changelog

All notable changes to QuickBrain plugin will be documented in this file.

## [1.1.0] - 2025-11-07

### 🚀 Performance Improvements
- **Lazy Initialization**: Plugin startup 5-10x faster (100ms → 10-20ms)
- **LRU Cache**: Repeated queries 25-50x faster (50ms → 1-2ms)
- **Memory Optimization**: 36% reduction in startup memory usage (70MB → 45MB)
- **Smart Routing**: QueryClassifier provides 95%+ accuracy in module selection

### ✨ New Features
- **Intelligent Query Classification**: Automatic detection of query type with 60+ patterns
- **Result Caching**: LRU cache with 100-entry capacity for instant repeated queries
- **Contextual Error Messages**: User-friendly errors with examples and actionable guidance
- **Smart Module Routing**: Priority-based module execution for faster results

### 🔧 Technical Improvements
- Lazy<T> pattern for all core modules (deferred initialization)
- Thread-safe LRU cache implementation
- Pattern-based query classification system
- Unified error handling with ErrorMessageBuilder
- Cleaner code with reduced duplication (-74 lines)

### 📊 User Experience
- Better error messages with context and examples
- Faster response for repeated calculations
- More reliable query processing
- Reduced waiting time overall

### 🛠️ Infrastructure
- Comprehensive optimization plan (OPTIMIZATION_PLAN.md)
- Detailed results documentation (OPTIMIZATION_RESULTS_V1.1.md)
- New utility classes: ResultCache, ErrorMessageBuilder, QueryClassifier

### 📝 Documentation
- Added optimization plan covering 6 phases
- Performance benchmarks and metrics
- Architecture improvements documented
- Expected impact analysis

## [1.0.0] - 2025-01-XX

### Initial Release
- Mathematical calculations (arithmetic, trigonometry, statistics)
- Unit conversions (length, weight, temperature, volume, data)
- Date calculations (differences, arithmetic)
- Logic evaluation (boolean, bitwise operations)
- Health calculations (BMI)
- Money calculations (tips, percentages)
- AI-powered assistance (OpenAI, Anthropic, HuggingFace, OpenRouter)
- History management with search
- Theme-aware icons
- Configurable settings
