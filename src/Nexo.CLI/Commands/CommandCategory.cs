using System.Collections.Generic;
using System.CommandLine;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Represents a command category
    /// </summary>
    public partial class CommandCategory
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<CommandInfo> Commands { get; set; }
        public Command RootCommand { get; set; }

        public CommandCategory(string name, string description)
        {
            Name = name;
            Description = description;
            Commands = new List<CommandInfo>();
            RootCommand = new Command(name, description);
        }

        public void AddCommand(string name, string description, string usage)
        {
            var commandInfo = new CommandInfo
            {
                Name = name,
                Description = description,
                Usage = usage
            };
            Commands.Add(commandInfo);
        }
    }
}

