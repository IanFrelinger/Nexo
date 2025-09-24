using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Playground.Server.Models;

namespace Playground.Server.Services;

/// <summary>
/// Manages demo scenarios and their metadata.
/// </summary>
public class ScenarioManager
{
    private readonly List<DemoScenario> _scenarios = new()
    {
        new DemoScenario
        {
            Id = "todo-app",
            Name = "Todo App with Dark Mode",
            Description = "A modern todo application with dark mode, drag-and-drop, and real-time sync",
            Prompt = "Create a todo app with dark mode toggle, drag and drop reordering, real-time sync, and responsive design",
            AppType = "web",
            Features = new[] { "Dark Mode", "Drag & Drop", "Real-time", "Responsive" },
            GeneratedCode = CodeTemplateService.GenerateTodoAppCode(),
            PreviewUrl = "/preview-demo"
        },
        new DemoScenario
        {
            Id = "ecommerce",
            Name = "E-commerce Store",
            Description = "A full-featured online store with product catalog, cart, and checkout",
            Prompt = "Build an e-commerce website with product catalog, shopping cart, user authentication, and payment processing",
            AppType = "web",
            Features = new[] { "Authentication", "Database", "Payment", "Responsive" },
            GeneratedCode = CodeTemplateService.GenerateEcommerceCode(),
            PreviewUrl = "/preview-demo"
        },
        new DemoScenario
        {
            Id = "chat-app",
            Name = "Real-time Chat App",
            Description = "A messaging application with real-time communication and file sharing",
            Prompt = "Create a real-time chat application with message history, file uploads, and online status indicators",
            AppType = "web",
            Features = new[] { "Real-time", "File Upload", "Authentication", "PWA" },
            GeneratedCode = CodeTemplateService.GenerateChatAppCode(),
            PreviewUrl = "/preview-demo"
        },
        new DemoScenario
        {
            Id = "mobile-fitness",
            Name = "Fitness Tracker Mobile App",
            Description = "A mobile fitness tracking app with workout plans and progress charts",
            Prompt = "Create a mobile fitness tracking app with workout plans, progress tracking, and social features",
            AppType = "mobile",
            Features = new[] { "Mobile", "Charts", "Social", "Offline" },
            GeneratedCode = CodeTemplateService.GenerateMobileFitnessCode(),
            PreviewUrl = "/preview-demo"
        },
        new DemoScenario
        {
            Id = "api-server",
            Name = "REST API Server",
            Description = "A comprehensive REST API with authentication, database integration, and documentation",
            Prompt = "Build a REST API server with authentication, database models, CRUD operations, and API documentation",
            AppType = "api",
            Features = new[] { "REST API", "Authentication", "Database", "Documentation" },
            GeneratedCode = CodeTemplateService.GenerateApiServerCode(),
            PreviewUrl = "/preview-demo"
        },
        new DemoScenario
        {
            Id = "game",
            Name = "2D Platformer Game",
            Description = "A 2D platformer game with physics, animations, and multiple levels",
            Prompt = "Create a 2D platformer game with player movement, enemies, collectibles, and multiple levels",
            AppType = "game",
            Features = new[] { "2D Graphics", "Physics", "Animation", "Levels" },
            GeneratedCode = CodeTemplateService.GenerateGameCode(),
            PreviewUrl = "/preview-demo"
        }
    };

    public async Task<List<DemoScenario>> GetScenariosAsync()
    {
        await Task.CompletedTask;
        return _scenarios.ToList();
    }

    public async Task<DemoScenario?> GetScenarioAsync(string id)
    {
        await Task.CompletedTask;
        return _scenarios.FirstOrDefault(s => s.Id == id);
    }






}
