using Nexo.Feature.Web.Enums;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Web command utility functions
    /// </summary>
    public static partial class WebCommands
    {
        private static string GetFileExtension(WebFrameworkType framework)
        {
            return framework switch
            {
                WebFrameworkType.React => "tsx",
                WebFrameworkType.Vue => "vue",
                WebFrameworkType.Angular => "ts",
                WebFrameworkType.Svelte => "svelte",
                WebFrameworkType.NextJs => "tsx",
                WebFrameworkType.NuxtJs => "vue",
                _ => "tsx"
            };
        }

        private static string GetStylingExtension(WebFrameworkType framework)
        {
            return framework switch
            {
                WebFrameworkType.React => "css",
                WebFrameworkType.Vue => "scss",
                WebFrameworkType.Angular => "scss",
                WebFrameworkType.Svelte => "css",
                WebFrameworkType.NextJs => "css",
                WebFrameworkType.NuxtJs => "scss",
                _ => "css"
            };
        }
    }
}
