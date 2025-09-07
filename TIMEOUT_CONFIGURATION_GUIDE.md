# ⏰ Timeout Configuration Guide

## Overview

The Nexo Feature Factory testing system includes comprehensive timeout handling to ensure tests don't hang indefinitely and provide better control over test execution duration. This guide explains how to configure and use timeouts effectively.

## 🎯 Timeout Types

### 1. Command-Specific Timeouts

Each test command has its own configurable timeout:

- **AI Connectivity Test**: `AiConnectivityTimeout` (default: 30 seconds)
- **Domain Analysis Test**: `DomainAnalysisTimeout` (default: 2 minutes)
- **Code Generation Test**: `CodeGenerationTimeout` (default: 3 minutes)
- **End-to-End Test**: `EndToEndTimeout` (default: 5 minutes)
- **Performance Test**: `PerformanceTimeout` (default: 2 minutes)
- **Validation Test**: `ValidationTimeout` (default: 10 seconds)
- **Cleanup Operations**: `CleanupTimeout` (default: 30 seconds)

### 2. Overall Execution Timeout

The test orchestrator calculates an overall timeout based on:
- Sum of all estimated command durations
- Minimum of 10 minutes
- 2x multiplier for safety margin

### 3. Default Timeout

Used as fallback for any command not explicitly configured: `DefaultTimeout` (default: 5 minutes)

## 🔧 Configuration Methods

### 1. CLI Command Line Options

```bash
# Basic timeout configuration
nexo test feature-factory --timeout 10 --ai-timeout 60 --domain-timeout 3

# Comprehensive timeout configuration
nexo test feature-factory \
  --validate-e2e \
  --timeout 10 \
  --ai-timeout 60 \
  --domain-timeout 5 \
  --code-timeout 8 \
  --e2e-timeout 10 \
  --perf-timeout 5 \
  --output ./test-results \
  --verbose
```

### 2. TestConfiguration Object

```csharp
var configuration = new TestConfiguration
{
    DefaultTimeout = TimeSpan.FromMinutes(10),
    AiConnectivityTimeout = TimeSpan.FromSeconds(60),
    DomainAnalysisTimeout = TimeSpan.FromMinutes(5),
    CodeGenerationTimeout = TimeSpan.FromMinutes(8),
    EndToEndTimeout = TimeSpan.FromMinutes(10),
    PerformanceTimeout = TimeSpan.FromMinutes(5),
    ValidationTimeout = TimeSpan.FromSeconds(15),
    CleanupTimeout = TimeSpan.FromSeconds(45)
};
```

### 3. Environment Variables

```bash
export FEATURE_FACTORY_DEFAULT_TIMEOUT_MINUTES=10
export FEATURE_FACTORY_AI_TIMEOUT_SECONDS=60
export FEATURE_FACTORY_DOMAIN_TIMEOUT_MINUTES=5
export FEATURE_FACTORY_CODE_TIMEOUT_MINUTES=8
export FEATURE_FACTORY_E2E_TIMEOUT_MINUTES=10
export FEATURE_FACTORY_PERF_TIMEOUT_MINUTES=5
```

## 📊 Timeout Behavior

### 1. Timeout Detection

When a timeout occurs:
- `OperationCanceledException` is caught
- Timeout duration is compared with actual execution time
- Specific timeout error message is generated
- Test result includes timeout metadata

### 2. Timeout Metadata

Timeout information is included in test results:

```json
{
  "ExecutionResult": {
    "OutputData": {
      "TimeoutOccurred": true,
      "ActualDuration": "00:01:45",
      "TimeoutDuration": "00:02:00"
    },
    "ErrorMessage": "Test command validate-domain-analysis timed out after 120.0 seconds"
  }
}
```

### 3. Graceful Degradation

- Individual command timeouts don't stop the entire test suite
- Critical command timeouts may stop execution depending on priority
- Cleanup operations have their own timeout protection
- Overall execution timeout prevents infinite hanging

## 🎮 Usage Examples

### 1. Quick Testing (Short Timeouts)

```bash
# Fast validation with short timeouts
nexo test feature-factory \
  --ai-timeout 15 \
  --domain-timeout 1 \
  --code-timeout 2 \
  --output ./quick-results
```

### 2. Comprehensive Testing (Extended Timeouts)

```bash
# Thorough testing with extended timeouts
nexo test feature-factory \
  --validate-e2e \
  --timeout 15 \
  --ai-timeout 120 \
  --domain-timeout 8 \
  --code-timeout 12 \
  --e2e-timeout 15 \
  --perf-timeout 8 \
  --verbose
```

### 3. Docker Testing (Extended Timeouts)

```bash
# Docker environment with extended timeouts
./run-docker-tests.sh --logs
```

The Docker configuration includes extended timeouts:
- AI Connectivity: 120 seconds
- Domain Analysis: 8 minutes
- Code Generation: 12 minutes
- End-to-End: 15 minutes
- Performance: 8 minutes

### 4. Demo Scripts

```bash
# Quick demo with short timeouts
./demo-feature-factory-with-testing.sh --no-tests

# Full demo with extended timeouts
./demo-feature-factory-with-testing.sh
```

## ⚙️ Timeout Recommendations

### 1. Development Environment

- **AI Connectivity**: 15-30 seconds
- **Domain Analysis**: 1-2 minutes
- **Code Generation**: 2-3 minutes
- **End-to-End**: 3-5 minutes
- **Performance**: 1-2 minutes

### 2. CI/CD Pipeline

- **AI Connectivity**: 30-60 seconds
- **Domain Analysis**: 2-5 minutes
- **Code Generation**: 3-8 minutes
- **End-to-End**: 5-10 minutes
- **Performance**: 2-5 minutes

### 3. Production Testing

- **AI Connectivity**: 60-120 seconds
- **Domain Analysis**: 5-10 minutes
- **Code Generation**: 8-15 minutes
- **End-to-End**: 10-20 minutes
- **Performance**: 5-10 minutes

## 🔍 Timeout Troubleshooting

### 1. Common Timeout Issues

**AI Connectivity Timeouts:**
- Check Ollama service status
- Verify network connectivity
- Increase `--ai-timeout` value
- Use mock responses for testing

**Domain Analysis Timeouts:**
- Complex feature descriptions may need more time
- Increase `--domain-timeout` value
- Check AI model performance

**Code Generation Timeouts:**
- Large features may need extended time
- Increase `--code-timeout` value
- Monitor system resources

**End-to-End Timeouts:**
- Complex workflows may need more time
- Increase `--e2e-timeout` value
- Check all service dependencies

### 2. Timeout Monitoring

```bash
# Run with verbose output to see timeout information
nexo test feature-factory --verbose --timeout 5

# Check timeout results in output
grep -i "timeout" test-results/*.json
```

### 3. Timeout Adjustment

```bash
# Start with default timeouts
nexo test feature-factory

# If timeouts occur, increase specific timeouts
nexo test feature-factory --ai-timeout 60 --domain-timeout 5

# For comprehensive testing, use extended timeouts
nexo test feature-factory --validate-e2e --timeout 15 --ai-timeout 120
```

## 📈 Performance Considerations

### 1. Timeout Impact on Performance

- **Short timeouts**: Faster feedback, may cause false failures
- **Long timeouts**: More reliable, slower feedback
- **Balanced timeouts**: Optimal for most use cases

### 2. Resource Management

- Timeouts prevent resource leaks
- Cleanup operations have their own timeouts
- Overall execution timeout prevents infinite hanging

### 3. Parallel Execution

- Timeouts work with parallel execution
- Each command has its own timeout
- Overall timeout applies to entire execution

## 🎯 Best Practices

### 1. Timeout Configuration

- Start with default timeouts
- Adjust based on actual execution times
- Use different timeouts for different environments
- Document timeout requirements

### 2. Error Handling

- Monitor timeout occurrences
- Adjust timeouts based on failure patterns
- Use verbose logging for timeout debugging
- Implement retry logic for transient timeouts

### 3. Testing Strategy

- Use short timeouts for quick validation
- Use extended timeouts for comprehensive testing
- Use different timeouts for different test types
- Monitor and adjust timeouts regularly

## 🔮 Future Enhancements

### 1. Dynamic Timeout Adjustment

- Automatic timeout adjustment based on historical data
- Machine learning for optimal timeout prediction
- Adaptive timeout based on system performance

### 2. Advanced Timeout Features

- Timeout escalation (retry with longer timeout)
- Timeout notifications and alerts
- Timeout analytics and reporting

### 3. Integration Improvements

- Timeout configuration via configuration files
- Timeout inheritance and overrides
- Timeout validation and verification

---

**Repository**: https://github.com/IanFrelinger/Nexo  
**Status**: ✅ **COMPREHENSIVE TIMEOUT HANDLING IMPLEMENTED**  
**Usage**: Run `nexo test feature-factory --help` to see all timeout options!

The timeout system ensures reliable test execution while providing flexibility for different testing scenarios and environments! ⏰
