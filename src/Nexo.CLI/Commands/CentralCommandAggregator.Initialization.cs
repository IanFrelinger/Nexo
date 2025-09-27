using System;
using System.Collections.Generic;
using System.CommandLine;

namespace Nexo.CLI.Commands
{
    public partial class CentralCommandAggregator
    {
        private void InitializeCommandCategories()
        {
            // Core CLI Commands
            var coreCommands = new CommandCategory("core", "Core CLI functionality");
            coreCommands.AddCommand("interactive", "Start interactive CLI mode", "interactive start");
            coreCommands.AddCommand("dashboard", "Open real-time dashboard", "dashboard show");
            coreCommands.AddCommand("status", "Show system status", "status");
            coreCommands.AddCommand("help", "Show help information", "help");
            _commandCategories["core"] = coreCommands;

            // Demo Commands (consolidated from DemoCommandAggregator)
            var demoCommands = new CommandCategory("demo", "Demo and showcase commands");
            demoCommands.AddCommand("feature-lab", "Feature Lab playground commands", "demo feature-lab start --platform blazor");
            demoCommands.AddCommand("validation", "Validation and preflight checks", "demo validation run");
            demoCommands.AddCommand("showcase", "Showcase different features", "demo showcase factory --type web");
            demoCommands.AddCommand("frontend", "Frontend generation demos", "demo frontend generate \"E-commerce app\" --type mobile");
            demoCommands.AddCommand("orchestration", "Orchestration demos", "demo orchestration run");
            demoCommands.AddCommand("discovery", "Command discovery demos", "demo discovery list");
            demoCommands.AddCommand("unity", "Unity game development with natural language", "demo unity create \"FPS controller\" --platform pc");
            _commandCategories["demo"] = demoCommands;

            // Project Management Commands
            var projectCommands = new CommandCategory("project", "Project management and scaffolding");
            projectCommands.AddCommand("init", "Initialize new project", "project init <name>");
            projectCommands.AddCommand("scaffold", "Scaffold project structure", "project scaffold <template>");
            projectCommands.AddCommand("template", "Manage project templates", "project template list");
            projectCommands.AddCommand("env", "Environment management", "project env setup");
            _commandCategories["project"] = projectCommands;

            // AI Commands
            var aiCommands = new CommandCategory("ai", "AI-powered features and operations");
            aiCommands.AddCommand("chat", "Interactive AI chat", "ai chat interactive");
            aiCommands.AddCommand("analyze", "AI code analysis", "ai analyze code <path>");
            aiCommands.AddCommand("generate", "AI code generation", "ai generate code <description>");
            aiCommands.AddCommand("documentation", "AI documentation generation", "ai docs generate <path>");
            aiCommands.AddCommand("operations", "AI operations management", "ai ops list");
            _commandCategories["ai"] = aiCommands;

            // Development Commands
            var devCommands = new CommandCategory("dev", "Development acceleration tools");
            devCommands.AddCommand("accelerate", "Development acceleration", "dev accelerate <workflow>");
            devCommands.AddCommand("workflow", "Workflow management", "dev workflow create <name>");
            devCommands.AddCommand("template", "Intelligent templates", "dev template generate <type>");
            devCommands.AddCommand("interactive", "Interactive development", "dev interactive start");
            _commandCategories["dev"] = devCommands;

            // Unity Game Development Commands
            var unityCommands = new CommandCategory("unity", "Unity game development tools");
            unityCommands.AddCommand("project", "Unity project management", "unity project create <name>");
            unityCommands.AddCommand("build", "Unity build operations", "unity build <platform>");
            unityCommands.AddCommand("test", "Unity testing", "unity test run");
            unityCommands.AddCommand("game", "Game development features", "game generate <feature>");
            _commandCategories["unity"] = unityCommands;

            // Pipeline Commands
            var pipelineCommands = new CommandCategory("pipeline", "Workflow and pipeline management");
            pipelineCommands.AddCommand("create", "Create new pipeline", "pipeline create <name>");
            pipelineCommands.AddCommand("execute", "Execute pipeline", "pipeline execute <name>");
            pipelineCommands.AddCommand("validate", "Validate pipeline", "pipeline validate <name>");
            pipelineCommands.AddCommand("list", "List pipelines", "pipeline list");
            _commandCategories["pipeline"] = pipelineCommands;

            // Testing Commands
            var testCommands = new CommandCategory("test", "Testing and validation");
            testCommands.AddCommand("run", "Run tests", "test run <filter>");
            testCommands.AddCommand("coverage", "Test coverage", "test coverage <path>");
            testCommands.AddCommand("generate", "Generate tests", "test generate <path>");
            testCommands.AddCommand("standalone", "Standalone test runner", "test standalone <assembly>");
            _commandCategories["test"] = testCommands;

            // Configuration Commands
            var configCommands = new CommandCategory("config", "Configuration management");
            configCommands.AddCommand("ai", "AI configuration", "config ai setup");
            configCommands.AddCommand("project", "Project configuration", "config project <path>");
            configCommands.AddCommand("environment", "Environment setup", "config env <name>");
            configCommands.AddCommand("policies", "Policy management", "config policies list");
            _commandCategories["config"] = configCommands;

            // Natural Language Commands
            var naturalLanguageCommands = new CommandCategory("build", "Natural language application building");
            naturalLanguageCommands.AddCommand("app", "Build app from description", "build \"description\" --platform <type>");
            naturalLanguageCommands.AddCommand("quick", "Quick build commands", "build quick <type> <name>");
            naturalLanguageCommands.AddCommand("templates", "List available templates", "build templates");
            naturalLanguageCommands.AddCommand("examples", "Show example descriptions", "build examples");
            _commandCategories["build"] = naturalLanguageCommands;

            // Demo Commands
            var demoCommands2 = new CommandCategory("demo", "Demo and showcase commands");
            demoCommands2.AddCommand("feature-lab", "Feature Lab demo", "demo feature-lab start");
            demoCommands2.AddCommand("ticket-processing", "Ticket processing demo", "demo ticket-processing run");
            demoCommands2.AddCommand("validation", "Demo validation", "demo validation run");
            demoCommands2.AddCommand("showcase", "Full showcase", "demo showcase all");
            _commandCategories["demo"] = demoCommands2;

            // Agent Commands
            var agentCommands = new CommandCategory("agents", "AI agent management");
            agentCommands.AddCommand("list", "List agents", "agents list");
            agentCommands.AddCommand("analyze", "Analyze agent performance", "agents analyze <name>");
            agentCommands.AddCommand("test", "Test agent", "agents test <name>");
            agentCommands.AddCommand("registry", "Agent registry", "agents registry list");
            _commandCategories["agents"] = agentCommands;

            // Iteration Commands
            var iterationCommands = new CommandCategory("iteration", "Iteration strategy management");
            iterationCommands.AddCommand("analyze", "Analyze iteration strategy", "iteration analyze <project>");
            iterationCommands.AddCommand("benchmark", "Benchmark performance", "iteration benchmark <strategy>");
            iterationCommands.AddCommand("generate", "Generate strategy", "iteration generate <type>");
            iterationCommands.AddCommand("optimize", "Optimize strategy", "iteration optimize <strategy>");
            _commandCategories["iteration"] = iterationCommands;

            // Adaptation Commands
            var adaptationCommands = new CommandCategory("adaptation", "Real-time adaptation management");
            adaptationCommands.AddCommand("enable", "Enable adaptation", "adaptation enable <feature>");
            adaptationCommands.AddCommand("disable", "Disable adaptation", "adaptation disable <feature>");
            adaptationCommands.AddCommand("status", "Adaptation status", "adaptation status");
            adaptationCommands.AddCommand("analyze", "Analyze adaptations", "adaptation analyze");
            _commandCategories["adaptation"] = adaptationCommands;

            // Web Commands
            var webCommands = new CommandCategory("web", "Web development and optimization");
            webCommands.AddCommand("generate", "Generate web app", "web generate <type>");
            webCommands.AddCommand("optimize", "Optimize web app", "web optimize <path>");
            webCommands.AddCommand("build", "Build web app", "web build <project>");
            webCommands.AddCommand("deploy", "Deploy web app", "web deploy <target>");
            _commandCategories["web"] = webCommands;

            // Model Commands
            var modelCommands = new CommandCategory("model", "AI model management");
            modelCommands.AddCommand("list", "List available models", "model list");
            modelCommands.AddCommand("test", "Test model", "model test <name>");
            modelCommands.AddCommand("configure", "Configure model", "model configure <name>");
            modelCommands.AddCommand("orchestrate", "Model orchestration", "model orchestrate <strategy>");
            _commandCategories["model"] = modelCommands;

            // Policy Commands
            var policyCommands = new CommandCategory("policy", "Policy and compliance management");
            policyCommands.AddCommand("list", "List policies", "policy list");
            policyCommands.AddCommand("validate", "Validate policies", "policy validate <path>");
            policyCommands.AddCommand("enforce", "Enforce policies", "policy enforce <name>");
            policyCommands.AddCommand("create", "Create policy", "policy create <name>");
            _commandCategories["policy"] = policyCommands;

            // Verification Commands
            var verifyCommands = new CommandCategory("verify", "Verification and validation");
            verifyCommands.AddCommand("code", "Verify code quality", "verify code <path>");
            verifyCommands.AddCommand("security", "Verify security", "verify security <path>");
            verifyCommands.AddCommand("performance", "Verify performance", "verify performance <path>");
            verifyCommands.AddCommand("compliance", "Verify compliance", "verify compliance <path>");
            _commandCategories["verify"] = verifyCommands;
        }
    }
}

