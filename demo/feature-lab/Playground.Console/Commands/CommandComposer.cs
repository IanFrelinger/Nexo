using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Playground.Console.Commands;

public class CommandComposer
{
    private readonly List<Command> _commands = new();
    private readonly Dictionary<string, object> _context = new();

    public CommandComposer AddCommand(string name, Func<Task> action, string description = "")
    {
        _commands.Add(new Command(name, action, description));
        return this;
    }

    public CommandComposer AddCommand(string name, Action action, string description = "")
    {
        _commands.Add(new Command(name, () => Task.Run(action), description));
        return this;
    }

    public CommandComposer SetContext(string key, object value)
    {
        _context[key] = value;
        return this;
    }

    public T? GetContext<T>(string key)
    {
        return _context.TryGetValue(key, out var value) ? (T)value : default(T);
    }

    public async Task ExecuteAsync(string commandName)
    {
        var command = _commands.Find(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        if (command == null)
        {
            throw new ArgumentException($"Command '{commandName}' not found");
        }

        global::System.Console.WriteLine($"Executing: {command.Name}");
        if (!string.IsNullOrEmpty(command.Description))
        {
            global::System.Console.WriteLine($"Description: {command.Description}");
        }
        global::System.Console.WriteLine();

        try
        {
            await command.Action();
            global::System.Console.WriteLine($"✅ Command '{command.Name}' completed successfully");
        }
        catch (Exception ex)
        {
            global::System.Console.WriteLine($"❌ Command '{command.Name}' failed: {ex.Message}");
            throw;
        }
    }

    public async Task ExecuteAllAsync()
    {
        foreach (var command in _commands)
        {
            await ExecuteAsync(command.Name);
            global::System.Console.WriteLine();
        }
    }

    public void ListCommands()
    {
        global::System.Console.WriteLine("Available Commands:");
        global::System.Console.WriteLine("==================");
        foreach (var command in _commands)
        {
            global::System.Console.WriteLine($"• {command.Name}");
            if (!string.IsNullOrEmpty(command.Description))
            {
                global::System.Console.WriteLine($"  {command.Description}");
            }
        }
    }

    private class Command
    {
        public string Name { get; }
        public Func<Task> Action { get; }
        public string Description { get; }

        public Command(string name, Func<Task> action, string description)
        {
            Name = name;
            Action = action;
            Description = description;
        }
    }
}
