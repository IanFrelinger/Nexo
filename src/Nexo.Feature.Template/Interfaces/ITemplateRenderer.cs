namespace Nexo.Feature.Template.Interfaces
{
    public interface ITemplateRenderer
    {
        string Render(string template, object context);
    }
} 