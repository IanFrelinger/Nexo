using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Generates web application code based on requirements.
/// </summary>
public class WebAppCodeGenerator
{
    public string GenerateWebAppCode(string prompt, List<string> features)
    {
        var baseCode = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Generated Web App</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
        .header { background: #007bff; color: white; padding: 2rem; text-align: center; margin-bottom: 2rem; border-radius: 8px; }
        .features { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; margin: 2rem 0; }
        .feature-card { background: white; padding: 1.5rem; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); text-align: center; }
        .feature-icon { font-size: 2rem; margin-bottom: 1rem; }
        .content { background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Generated Web Application</h1>
            <p>Built with modern web technologies</p>
        </div>
        
        <div class=""features"">
            {FEATURES}
        </div>
        
        <div class=""content"">
            <h2>Application Details</h2>
            <p>This web application was generated based on your requirements:</p>
            <p><strong>Prompt:</strong> {PROMPT}</p>
            <p><strong>Features:</strong> {FEATURE_LIST}</p>
        </div>
    </div>

    <script>
        console.log('Web application loaded successfully!');
        
        // Add interactive functionality based on features
        {JAVASCRIPT_CODE}
    </script>
</body>
</html>";

        var featureCards = features.Select(feature => $@"
            <div class=""feature-card"">
                <div class=""feature-icon"">{GetFeatureIcon(feature)}</div>
                <h3>{feature}</h3>
                <p>Implemented with modern web standards</p>
            </div>").ToList();

        var javascriptCode = GenerateJavaScriptCode(features);

        return baseCode
            .Replace("{FEATURES}", string.Join("", featureCards))
            .Replace("{PROMPT}", prompt)
            .Replace("{FEATURE_LIST}", string.Join(", ", features))
            .Replace("{JAVASCRIPT_CODE}", javascriptCode);
    }

    private string GetFeatureIcon(string feature)
    {
        return feature.ToLower() switch
        {
            "authentication" => "🔐",
            "database" => "🗄️",
            "payment" => "💳",
            "responsive" => "📱",
            "real-time" => "⚡",
            "file upload" => "📁",
            "pwa" => "📲",
            "dark mode" => "🌙",
            "drag & drop" => "🖱️",
            _ => "✨"
        };
    }

    private string GenerateJavaScriptCode(List<string> features)
    {
        var code = new List<string>();

        if (features.Contains("Authentication"))
        {
            code.Add(@"
                // Authentication functionality
                function login(username, password) {
                    console.log('Login attempt:', username);
                    // Implement authentication logic
                }
                
                function logout() {
                    console.log('User logged out');
                    // Implement logout logic
                }");
        }

        if (features.Contains("Real-time"))
        {
            code.Add(@"
                // Real-time functionality
                function connectWebSocket() {
                    console.log('Connecting to WebSocket...');
                    // Implement WebSocket connection
                }
                
                function sendMessage(message) {
                    console.log('Sending message:', message);
                    // Implement message sending
                }");
        }

        if (features.Contains("File Upload"))
        {
            code.Add(@"
                // File upload functionality
                function handleFileUpload(file) {
                    console.log('Uploading file:', file.name);
                    // Implement file upload logic
                }
                
                function previewFile(file) {
                    console.log('Previewing file:', file.name);
                    // Implement file preview
                }");
        }

        if (features.Contains("Payment"))
        {
            code.Add(@"
                // Payment functionality
                function processPayment(amount, currency) {
                    console.log('Processing payment:', amount, currency);
                    // Implement payment processing
                }
                
                function validateCard(cardNumber) {
                    console.log('Validating card:', cardNumber);
                    // Implement card validation
                }");
        }

        if (features.Contains("Dark Mode"))
        {
            code.Add(@"
                // Dark mode functionality
                function toggleDarkMode() {
                    document.body.classList.toggle('dark-mode');
                    localStorage.setItem('darkMode', document.body.classList.contains('dark-mode'));
                }
                
                // Load saved theme
                if (localStorage.getItem('darkMode') === 'true') {
                    document.body.classList.add('dark-mode');
                }");
        }

        return string.Join("\n", code);
    }
}
