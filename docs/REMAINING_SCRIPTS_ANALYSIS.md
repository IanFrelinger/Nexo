# Remaining Scripts Analysis

**Total .sh files:** 32  
**Already replaced:** 9 (test scripts)  
**Remaining:** 23

## ✅ Already Replaced (by `nexo test`)

1. ✅ `test-caching-multi-env.sh` → `nexo test --platforms ubuntu alpine debian android`
2. ✅ `test-framework-multi-env.sh` → `nexo test --project Nexo.Tests.Infrastructure`
3. ✅ `test-ai-agent-multi-env.sh` → `nexo test --project Nexo.Tests.GeospatialE2E --filter "GeospatialAIAgentTests"`
4. ✅ `test-android.sh` → `nexo test --platforms android`
5. ✅ `test-ios.sh` → `nexo test --platforms ios`
6. ✅ `test-caching-ios.sh` → `nexo test --platforms ios`
7. ✅ `test-caching-unity.sh` → `nexo test --platforms unity`
8. ✅ `test-framework-unity.sh` → `nexo test --project Nexo.Tests.Infrastructure --platforms unity`
9. ✅ `test-local.sh` → `nexo test local`

## 🚧 Can Be Replaced by Enhanced `nexo test`

10. ⚠️ `test-visual-validation-multi-env.sh` → `nexo test --project Nexo.Tests.GeospatialVisual`
11. ⚠️ `test-visual-validation-all-platforms.sh` → `nexo test --project Nexo.Tests.GeospatialVisual --platforms ubuntu alpine debian android ios unity`
12. ⚠️ `test-framework-coverage.sh` → `nexo test --coverage`
13. ⚠️ `test-framework-stress.sh` → `nexo test --stress`
14. ⚠️ `test-code-analysis-unity-docker.sh` → `nexo test --project Nexo.Tests.Infrastructure --platforms unity --filter "CodeAnalysis"`

## 📦 Build & CI Scripts (Need New Commands)

15. **`build-portable.sh`** - Builds netstandard2.0 targets
    - **Replace with:** `nexo build --portable`
    - **Complexity:** Low - Just dotnet build calls

16. **`ci-verify.sh`** - CI verification workflow
    - **Replace with:** `nexo ci verify`
    - **Complexity:** Medium - Multiple checks

17. **`check-promotion.sh`** - Check if promotion criteria met
    - **Replace with:** `nexo ci check-promotion`
    - **Complexity:** Low - File existence checks

18. **`check-promotion-cs.sh`** - C# version of promotion check
    - **Replace with:** `nexo ci check-promotion` (same as above)
    - **Complexity:** Low

## 🔧 Utility Scripts (Need New Commands)

19. **`aggregate-junit.sh`** - Aggregate JUnit XML files
    - **Replace with:** `nexo aggregate junit`
    - **Complexity:** Medium - XML parsing and aggregation

20. **`aggregate-junit-cs.sh`** - C# version of JUnit aggregation
    - **Replace with:** `nexo aggregate junit` (same as above)
    - **Complexity:** Medium

21. **`artifact-diff.sh`** - Compare artifact outputs
    - **Replace with:** `nexo diff artifacts`
    - **Complexity:** Low - JSON diff

22. **`review-summary-md.sh`** - Generate markdown from review JSON
    - **Replace with:** `nexo report markdown`
    - **Complexity:** Low - JSON to Markdown conversion

## 🎮 Unity Scripts (Enhance Existing `nexo unity`)

23. **`create-unity-project.sh`** - Already uses `nexo unity create` ✅
    - **Status:** Already dogfooded, just wrapper

24. **`open-unity-editor.sh`** - Open Unity editor
    - **Replace with:** `nexo unity open`
    - **Complexity:** Low

25. **`create-unity-demo-assets.sh`** - Create demo assets
    - **Replace with:** `nexo unity create-assets`
    - **Complexity:** Medium

26. **`analyze-and-fix-unity-errors.sh`** - Analyze Unity errors
    - **Replace with:** `nexo unity analyze-errors`
    - **Complexity:** Medium - Error parsing

27. **`capture-unity-logs.sh`** - Capture Unity logs
    - **Replace with:** `nexo unity capture-logs`
    - **Complexity:** Low

## 🎯 Demo/Orchestration Scripts (Already Use Nexo CLI)

28. **`synthesize-feedback.sh`** - Already uses `nexo demo synthesize-feedback` ✅
    - **Status:** Already dogfooded

29. **`apply-feedback-changes.sh`** - Already uses `nexo demo apply-feedback` ✅
    - **Status:** Already dogfooded

30. **`demo-smoke-test.sh`** - Demo smoke testing
    - **Replace with:** `nexo demo smoke-test`
    - **Complexity:** Low

31. **`generate-game-via-orchestrator.sh`** - Game generation
    - **Replace with:** `nexo orchestrate "generate game"`
    - **Complexity:** Medium - Uses orchestrator

32. **`playtest-via-orchestrator.sh`** - Playtesting
    - **Replace with:** `nexo orchestrate "playtest"`
    - **Complexity:** Medium - Uses orchestrator

## Summary

### High Priority (Core Functionality)
- **Build:** `build-portable.sh` → `nexo build --portable`
- **CI:** `ci-verify.sh`, `check-promotion.sh` → `nexo ci verify`, `nexo ci check-promotion`
- **Aggregate:** `aggregate-junit.sh` → `nexo aggregate junit`

### Medium Priority (Enhanced Testing)
- **Test enhancements:** Coverage, stress, visual validation flags for `nexo test`
- **Unity enhancements:** Add subcommands to `nexo unity`

### Low Priority (Already Wrapped)
- Many scripts already call `nexo` commands internally
- Just need to remove wrapper scripts

## Recommended Implementation Order

1. ✅ **DONE:** Multi-platform test command
2. **Next:** Build command (`nexo build`)
3. **Next:** CI command (`nexo ci`)
4. **Next:** Aggregate command (`nexo aggregate`)
5. **Next:** Enhance `nexo test` with coverage/stress flags
6. **Next:** Enhance `nexo unity` with missing subcommands
7. **Last:** Remove wrapper scripts that just call `nexo` commands
