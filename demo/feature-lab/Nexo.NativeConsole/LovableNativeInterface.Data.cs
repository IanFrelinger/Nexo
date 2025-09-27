using System;
using System.Collections.Generic;

namespace Nexo.NativeConsole;

/// <summary>
/// Data initialization functionality
/// </summary>
public partial class LovableNativeInterface
{
    private List<AppType> InitializeAppTypes()
    {
        return new List<AppType>
        {
            new AppType
            {
                Name = "Web Application",
                Description = "Modern web apps with React, Vue, or Angular",
                Icon = "🌐",
                Technologies = new[] { "React", "TypeScript", "CSS3", "Vite" }
            },
            new AppType
            {
                Name = "Mobile Application",
                Description = "Cross-platform mobile apps for iOS and Android",
                Icon = "📱",
                Technologies = new[] { "React Native", "TypeScript", "Expo" }
            },
            new AppType
            {
                Name = "Desktop Application",
                Description = "Native desktop apps for Windows, macOS, and Linux",
                Icon = "🖥️",
                Technologies = new[] { "Electron", "React", "TypeScript" }
            },
            new AppType
            {
                Name = "API Server",
                Description = "REST and GraphQL APIs with authentication",
                Icon = "🔌",
                Technologies = new[] { "Node.js", "Express", "TypeScript", "MongoDB" }
            },
            new AppType
            {
                Name = "Game Application",
                Description = "2D and 3D games with Unity or Unreal",
                Icon = "🎮",
                Technologies = new[] { "Unity", "C#", "2D/3D Graphics" }
            },
            new AppType
            {
                Name = "Console Application",
                Description = "Command-line tools and utilities",
                Icon = "💻",
                Technologies = new[] { "C#", "CLI", "Terminal" }
            }
        };
    }

    private List<Feature> InitializeFeatures()
    {
        return new List<Feature>
        {
            new Feature
            {
                Name = "Dark Mode",
                Description = "Toggle between light and dark themes",
                Icon = "🌙"
            },
            new Feature
            {
                Name = "Authentication",
                Description = "User login, registration, and session management",
                Icon = "🔐"
            },
            new Feature
            {
                Name = "Database",
                Description = "Data persistence with SQL or NoSQL database",
                Icon = "🗄️"
            },
            new Feature
            {
                Name = "Responsive Design",
                Description = "Adapts to different screen sizes and devices",
                Icon = "📱"
            },
            new Feature
            {
                Name = "Real-time Updates",
                Description = "Live data synchronization and notifications",
                Icon = "⚡"
            },
            new Feature
            {
                Name = "PWA Support",
                Description = "Progressive Web App capabilities",
                Icon = "📲"
            },
            new Feature
            {
                Name = "Payment Processing",
                Description = "Stripe, PayPal, and other payment integrations",
                Icon = "💳"
            },
            new Feature
            {
                Name = "Search & Filter",
                Description = "Advanced search and filtering capabilities",
                Icon = "🔍"
            },
            new Feature
            {
                Name = "File Upload",
                Description = "Upload and manage files and images",
                Icon = "📁"
            },
            new Feature
            {
                Name = "Data Visualization",
                Description = "Charts, graphs, and analytics dashboards",
                Icon = "📊"
            }
        };
    }

    private List<QuickExample> InitializeQuickExamples()
    {
        return new List<QuickExample>
        {
            new QuickExample
            {
                Title = "Todo App",
                Description = "A modern todo app with dark mode, drag-and-drop reordering, and real-time sync",
                Platform = "Web Application"
            },
            new QuickExample
            {
                Title = "Fitness Tracker",
                Description = "A mobile fitness tracker with workout plans, progress charts, and social features",
                Platform = "Mobile Application"
            },
            new QuickExample
            {
                Title = "E-commerce Store",
                Description = "A full-featured online store with product listings, shopping cart, and payment processing",
                Platform = "Web Application"
            },
            new QuickExample
            {
                Title = "Chat API",
                Description = "A real-time chat API with user management, messaging, and presence features",
                Platform = "API Server"
            },
            new QuickExample
            {
                Title = "Photo Editor",
                Description = "A desktop photo editing application with filters, cropping, and batch processing",
                Platform = "Desktop Application"
            },
            new QuickExample
            {
                Title = "2D Platformer",
                Description = "A simple 2D platformer game with character movement, jumping, and collectibles",
                Platform = "Game Application"
            }
        };
    }
}
