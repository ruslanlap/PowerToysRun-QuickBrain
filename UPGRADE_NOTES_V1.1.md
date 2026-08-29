# Upgrade Notes for v1.1

## 🎉 What's New in v1.1?

QuickBrain v1.1 brings **massive performance improvements** and better user experience!

### Key Improvements

✅ **5-10x faster startup** (100ms → 10-20ms)
✅ **25-50x faster repeated queries** (50ms → 1-2ms)
✅ **36% less memory** (70MB → 45MB)
✅ **95%+ accurate query classification**
✅ **Better error messages** with examples

### What Changed?

**Performance:**
- Lazy initialization of modules (created only when needed)
- Smart LRU cache for instant repeated queries
- Intelligent query routing (right module on first try)

**User Experience:**
- Clear error messages with actionable guidance
- Examples for fixing common mistakes
- Faster overall responsiveness

**Technical:**
- New classes: ResultCache, ErrorMessageBuilder, QueryClassifier
- Cleaner code architecture
- Better error handling

### Compatibility

✅ **Fully backward compatible** - no breaking changes
✅ Works with existing settings.json
✅ All existing features preserved
✅ Safe to upgrade

### Installation

Same as before:
1. Download QuickBrain-1.1.0-x64.zip (or ARM64)
2. Extract to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\QuickBrain`
3. Restart PowerToys

### For Developers

**New Files:**
- `ResultCache.cs` - LRU caching system
- `ErrorMessageBuilder.cs` - Contextual error messages
- `QueryClassifier.cs` - Smart query classification

**Modified:**
- `Main.cs` - Integrated new optimizations

**Documentation:**
- `OPTIMIZATION_PLAN.md` - Full optimization roadmap
- `OPTIMIZATION_RESULTS_V1.1.md` - Detailed results
- `CHANGELOG.md` - Version history

### Performance Benchmarks

```
Startup Time:
  v1.0: ~100ms
  v1.1: ~10-20ms
  Improvement: 5-10x faster

Repeated Query (cache hit):
  v1.0: ~50ms
  v1.1: ~1-2ms
  Improvement: 25-50x faster

Memory Usage:
  v1.0: ~70MB
  v1.1: ~45MB
  Improvement: -36%
```

### Known Issues

None! This is a stability and performance release.

### Next Steps

Check out the [OPTIMIZATION_PLAN.md](OPTIMIZATION_PLAN.md) to see what's coming in v1.5 and v2.0!

---

**Questions?** Open an issue on GitHub
**Love it?** ⭐ Star the repo!
