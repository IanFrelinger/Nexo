namespace Nexo.Feature.Template.Interfaces
{
    public interface ITemplateEngine
    {
        void Generate(string template, object context);
    }
} 