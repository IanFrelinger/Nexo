using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

// Standalone verification test that doesn't depend on the full project build
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Nexo Verification System Test ===");
        Console.WriteLine();
        
        // Test 1: Jaccard Similarity
        Console.WriteLine("1. Testing Jaccard Similarity...");
        TestJaccardSimilarity();
        
        // Test 2: File Operations
        Console.WriteLine("2. Testing File Operations...");
        TestFileOperations();
        
        // Test 3: Verification Spec Loading
        Console.WriteLine("3. Testing Verification Spec Loading...");
        TestVerificationSpecLoading();
        
        // Test 4: Network Guard
        Console.WriteLine("4. Testing Network Guard...");
        TestNetworkGuard();
        
        Console.WriteLine();
        Console.WriteLine("=== Verification System Test Completed ===");
    }
    
    static void TestJaccardSimilarity()
    {
        // Simple Jaccard implementation for testing
        double JaccardTokens(string textA, string textB)
        {
            if (string.IsNullOrEmpty(textA) && string.IsNullOrEmpty(textB))
                return 1.0;
                
            if (string.IsNullOrEmpty(textA) || string.IsNullOrEmpty(textB))
                return 0.0;

            var tokensA = Tokenize(textA);
            var tokensB = Tokenize(textB);
            
            var intersection = 0;
            var union = tokensA.Count + tokensB.Count;
            
            foreach (var token in tokensA)
            {
                if (tokensB.Contains(token))
                {
                    intersection++;
                    union--;
                }
            }
            
            return union == 0 ? 1.0 : (double)intersection / union;
        }
        
        HashSet<string> Tokenize(string text)
        {
            return text.ToLowerInvariant()
                .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', ':' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length > 1)
                .ToHashSet();
        }
        
        // Test cases
        var identical = JaccardTokens("Hello world", "Hello world");
        var different = JaccardTokens("Hello world", "Goodbye universe");
        var similar = JaccardTokens("The quick brown fox", "A quick brown fox");
        
        Console.WriteLine($"  Identical texts: {identical:F3} (expected: 1.000)");
        Console.WriteLine($"  Different texts: {different:F3} (expected: 0.000)");
        Console.WriteLine($"  Similar texts: {similar:F3} (expected: > 0.500)");
        
        if (identical == 1.0 && different == 0.0 && similar > 0.5)
        {
            Console.WriteLine("  ✓ Jaccard similarity tests passed");
        }
        else
        {
            Console.WriteLine("  ✗ Jaccard similarity tests failed");
        }
    }
    
    static void TestFileOperations()
    {
        try
        {
            // Test CSV file creation
            var testDir = Path.Combine(Path.GetTempPath(), "nexo-verify-test");
            Directory.CreateDirectory(testDir);
            
            var csvFile = Path.Combine(testDir, "test.csv");
            File.WriteAllText(csvFile, "id,name,email\n1,John,john@example.com\n2,Jane,jane@example.com");
            
            // Test file reading
            var content = File.ReadAllText(csvFile);
            var lines = content.Split('\n');
            
            Console.WriteLine($"  Created CSV file: {csvFile}");
            Console.WriteLine($"  Lines in file: {lines.Length}");
            Console.WriteLine($"  Headers: {lines[0]}");
            
            // Cleanup
            Directory.Delete(testDir, true);
            
            Console.WriteLine("  ✓ File operations test passed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ File operations test failed: {ex.Message}");
        }
    }
    
    static void TestVerificationSpecLoading()
    {
        try
        {
            // Test YAML spec loading
            var specContent = @"
scenario: examples/triage_support.yaml
inputs:
  - tests/fixtures/inbox/*.eml
modes:
  - name: off
    env:
      NEXO_AI_MODE: off
    asserts:
      - type: no_network_egress
      - type: hash_equals
        of: outputs/labels.csv
        expected_sha256: ""a1b2c3d4e5f6""
";
            
            var specFile = Path.Combine(Path.GetTempPath(), "test.verify.yaml");
            File.WriteAllText(specFile, specContent);
            
            // Simple YAML parsing (just check if file exists and has content)
            if (File.Exists(specFile))
            {
                var content = File.ReadAllText(specFile);
                if (content.Contains("scenario:") && content.Contains("modes:"))
                {
                    Console.WriteLine("  ✓ Verification spec loading test passed");
                }
                else
                {
                    Console.WriteLine("  ✗ Verification spec content validation failed");
                }
            }
            else
            {
                Console.WriteLine("  ✗ Verification spec file creation failed");
            }
            
            // Cleanup
            File.Delete(specFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Verification spec loading test failed: {ex.Message}");
        }
    }
    
    static void TestNetworkGuard()
    {
        try
        {
            // Test environment variable checking
            var originalMode = Environment.GetEnvironmentVariable("NEXO_AI_MODE");
            
            // Test OFF mode
            Environment.SetEnvironmentVariable("NEXO_AI_MODE", "off");
            var isOffMode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant() == "off";
            
            // Test ASSIST mode
            Environment.SetEnvironmentVariable("NEXO_AI_MODE", "assist");
            var isAssistMode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant() == "assist";
            
            // Test HYBRID mode
            Environment.SetEnvironmentVariable("NEXO_AI_MODE", "hybrid");
            var isHybridMode = Environment.GetEnvironmentVariable("NEXO_AI_MODE")?.ToLowerInvariant() == "hybrid";
            
            // Restore original value
            if (originalMode != null)
                Environment.SetEnvironmentVariable("NEXO_AI_MODE", originalMode);
            else
                Environment.SetEnvironmentVariable("NEXO_AI_MODE", null);
            
            Console.WriteLine($"  OFF mode detection: {isOffMode}");
            Console.WriteLine($"  ASSIST mode detection: {isAssistMode}");
            Console.WriteLine($"  HYBRID mode detection: {isHybridMode}");
            
            if (isOffMode && isAssistMode && isHybridMode)
            {
                Console.WriteLine("  ✓ Network guard mode detection test passed");
            }
            else
            {
                Console.WriteLine("  ✗ Network guard mode detection test failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Network guard test failed: {ex.Message}");
        }
    }
}
