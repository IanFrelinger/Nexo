using System.Collections.Generic;
using Nexo.Feature.Web.Enums;

namespace Nexo.Feature.Web.Services
{
    public partial class FrameworkTemplateProvider
    {
        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactFunctionalTemplate();
            templates[(WebFrameworkType.React, WebComponentType.Class)] = GetReactClassTemplate();
            templates[(WebFrameworkType.React, WebComponentType.Pure)] = GetReactPureTemplate();
            templates[(WebFrameworkType.React, WebComponentType.Hook)] = GetReactHookTemplate();

            // Vue Templates
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueCompositionTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Class)] = GetVueOptionsTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Pure)] = GetVuePureTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Hook)] = GetVueComposableTemplate();

            // Next.js Templates
            templates[(WebFrameworkType.NextJs, WebComponentType.Page)] = GetNextJsPageTemplate();
            templates[(WebFrameworkType.NextJs, WebComponentType.Functional)] = GetNextJsFunctionalTemplate();

            // Nuxt.js Templates
            templates[(WebFrameworkType.NuxtJs, WebComponentType.Page)] = GetNuxtJsPageTemplate();
            templates[(WebFrameworkType.NuxtJs, WebComponentType.Functional)] = GetNuxtJsFunctionalTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeTypeScriptTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React TypeScript Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactTypeScriptTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueTypeScriptTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeStylingTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React CSS Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactCSSTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueSCSSTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeTestTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // React Test Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactTestTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueTestTemplate();

            return templates;
        }

        private Dictionary<(WebFrameworkType, WebComponentType), string> InitializeDocumentationTemplates()
        {
            var templates = new Dictionary<(WebFrameworkType, WebComponentType), string>();

            // Documentation Templates
            templates[(WebFrameworkType.React, WebComponentType.Functional)] = GetReactDocumentationTemplate();
            templates[(WebFrameworkType.Vue, WebComponentType.Functional)] = GetVueDocumentationTemplate();

            return templates;
        }
    }
}

