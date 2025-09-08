using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Base class for all Feature Factory commands
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger _logger;
        
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Usage { get; }
        
        protected BaseCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public abstract Task<int> ExecuteAsync(string[] args);
        
        /// <summary>
        /// Display command help information
        /// </summary>
        protected void DisplayHelp()
        {
            Console.WriteLine($"\n📋 {Name} Command");
            Console.WriteLine("=" + new string('=', Name.Length + 8));
            Console.WriteLine($"Description: {Description}");
            Console.WriteLine($"Usage: {Usage}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Display error message
        /// </summary>
        protected void DisplayError(string message)
        {
            Console.WriteLine($"❌ Error: {message}");
        }
        
        /// <summary>
        /// Display success message
        /// </summary>
        protected void DisplaySuccess(string message)
        {
            Console.WriteLine($"✅ {message}");
        }
        
        /// <summary>
        /// Display info message
        /// </summary>
        protected void DisplayInfo(string message)
        {
            Console.WriteLine($"ℹ️  {message}");
        }
    }
}
