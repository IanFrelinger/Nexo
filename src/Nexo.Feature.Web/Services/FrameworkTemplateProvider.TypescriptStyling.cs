namespace Nexo.Feature.Web.Services
{
    public partial class FrameworkTemplateProvider
    {
        private string GetReactTypeScriptTemplate()
        {
            return @"export interface {{ComponentName}}Props {
  // Add your props here
}

export interface {{ComponentName}}State {
  // Add your state here
}

export type {{ComponentName}}Ref = React.RefObject<HTMLDivElement>;";
        }

        private string GetVueTypeScriptTemplate()
        {
            return @"export interface {{ComponentName}}Props {
  // Add your props here
}

export interface {{ComponentName}}Emits {
  // Add your emits here
}";
        }

        private string GetReactCSSTemplate()
        {
            return @".{{ComponentName}}-container {
  padding: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  background-color: #f9f9f9;
}

.{{ComponentName}}-container h1 {
  color: #333;
  margin-bottom: 0.5rem;
}

.{{ComponentName}}-container p {
  color: #666;
  font-size: 0.9rem;
}";
        }

        private string GetVueSCSSTemplate()
        {
            return @".{{ComponentName}}-container {
  padding: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  background-color: #f9f9f9;

  h1 {
    color: #333;
    margin-bottom: 0.5rem;
  }

  p {
    color: #666;
    font-size: 0.9rem;
  }
}";
        }
    }
}

