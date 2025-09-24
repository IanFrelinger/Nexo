using System;
using System.Collections.Generic;
using Playground.Server.Services.Templates;

namespace Playground.Server.Services
{
    /// <summary>
    /// Orchestrator for code template generation that delegates to specialized template generators.
    /// </summary>
    public class CodeTemplateService
    {
        private readonly WebAppTemplateGenerator _webAppGenerator;
        private readonly MobileAppTemplateGenerator _mobileAppGenerator;
        private readonly ApiTemplateGenerator _apiGenerator;
        private readonly GameTemplateGenerator _gameGenerator;

        public CodeTemplateService()
        {
            _webAppGenerator = new WebAppTemplateGenerator();
            _mobileAppGenerator = new MobileAppTemplateGenerator();
            _apiGenerator = new ApiTemplateGenerator();
            _gameGenerator = new GameTemplateGenerator();
        }

        public static string GenerateTodoAppCode() => WebAppTemplateGenerator.GenerateTodoAppCode();
        public static string GenerateEcommerceCode() => WebAppTemplateGenerator.GenerateEcommerceCode();
        public static string GenerateChatAppCode() => WebAppTemplateGenerator.GenerateChatAppCode();
        public static string GenerateMobileFitnessCode() => MobileAppTemplateGenerator.GenerateMobileFitnessCode();
        public static string GenerateApiServerCode() => ApiTemplateGenerator.GenerateApiServerCode();
        public static string GenerateGameCode() => GameTemplateGenerator.GenerateGameCode();
    }
}
