using System;
using System.Net.Http;
using System.Threading.Tasks;

// Simple network test to verify Docker network isolation
class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Docker Network Isolation Test ===");
        
        try
        {
            // Try to make an HTTP request
            using var client = new HttpClient();
            var response = await client.GetAsync("https://httpbin.org/get");
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine("✗ Network request succeeded - isolation failed");
            Console.WriteLine($"Response: {content.Substring(0, Math.Min(100, content.Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine("✓ Network request failed as expected - isolation working");
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\n=== Network Isolation Test Completed ===");
    }
}
