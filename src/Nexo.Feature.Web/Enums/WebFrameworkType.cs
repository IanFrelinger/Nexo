namespace Nexo.Feature.Web.Enums
{
    /// <summary>
    /// Supported web frameworks for code generation.
    /// </summary>
    public enum WebFrameworkType
    {
        /// <summary>
        /// React with TypeScript
        /// </summary>
        React = 0,
        
        /// <summary>
        /// Vue.js with TypeScript
        /// </summary>
        Vue = 1,
        
        /// <summary>
        /// Angular with TypeScript
        /// </summary>
        Angular = 2,
        
        /// <summary>
        /// Svelte with TypeScript
        /// </summary>
        Svelte = 3,
        
        /// <summary>
        /// Next.js with React and TypeScript
        /// </summary>
        NextJs = 4,
        
        /// <summary>
        /// Nuxt.js with Vue and TypeScript
        /// </summary>
        NuxtJs = 5
    }
} 