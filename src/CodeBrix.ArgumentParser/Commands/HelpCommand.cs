//
// HelpCommand.cs  (derived from Mono.Options Options.cs)
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>, <Jonathan.Pryor@microsoft.com>
//  Federico Di Gregorio <fog@initd.org>
//  Rolf Bjarne Kvinge <rolf@xamarin.com>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
// Copyright (C) 2009 Federico Di Gregorio.
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace CodeBrix.ArgumentParser.Commands; //was previously: Mono.Options;

/// <summary>
/// The default <c>help</c> command registered automatically by
/// <see cref="CommandSet.Run(IEnumerable{string})"/> when no
/// <see cref="HelpCommand"/> is already present. Prints usage, a list
/// of available commands, or per-command help.
/// </summary>
public class HelpCommand : Command
{
    /// <summary>Initializes a new <see cref="HelpCommand"/>.</summary>
    public HelpCommand()
        : base("help", help: "Show this message and exit")
    {
    }

    /// <inheritdoc />
    public override int Invoke(IEnumerable<string> arguments)
    {
        var extra = new List<string>(arguments ?? new string[0]);
        var _ = CommandSet.Options.MessageLocalizer;
        if (extra.Count == 0)
        {
            CommandSet.Options.WriteOptionDescriptions(CommandSet.Out);
            return 0;
        }
        var command = CommandSet.GetCommand(extra);
        if (command == this || extra.Contains("--help"))
        {
            CommandSet.Out.WriteLine(_($"Usage: {CommandSet.Suite} COMMAND [OPTIONS]"));
            CommandSet.Out.WriteLine(_($"Use `{CommandSet.Suite} help COMMAND` for help on a specific command."));
            CommandSet.Out.WriteLine();
            CommandSet.Out.WriteLine(_($"Available commands:"));
            CommandSet.Out.WriteLine();
            var commands = GetCommands();
            commands.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase));
            foreach (var c in commands)
            {
                if (c.Key == "help")
                {
                    continue;
                }
                CommandSet.Options.WriteCommandDescription(CommandSet.Out, c.Value, c.Key);
            }
            CommandSet.Options.WriteCommandDescription(CommandSet.Out, CommandSet.help, "help");
            return 0;
        }
        if (command == null)
        {
            WriteUnknownCommand(extra[0]);
            return 1;
        }
        if (command.Options != null)
        {
            command.Options.WriteOptionDescriptions(CommandSet.Out);
            return 0;
        }
        return command.Invoke(new[] { "--help" });
    }

    List<KeyValuePair<string, Command>> GetCommands()
    {
        var commands = new List<KeyValuePair<string, Command>>();

        foreach (var c in CommandSet)
        {
            commands.Add(new KeyValuePair<string, Command>(c.Name, c));
        }

        if (CommandSet.NestedCommandSets == null)
            return commands;

        foreach (var nc in CommandSet.NestedCommandSets)
        {
            AddNestedCommands(commands, "", nc);
        }

        return commands;
    }

    void AddNestedCommands(List<KeyValuePair<string, Command>> commands, string outer, CommandSet value)
    {
        foreach (var v in value)
        {
            commands.Add(new KeyValuePair<string, Command>($"{outer}{value.Suite} {v.Name}", v));
        }
        if (value.NestedCommandSets == null)
            return;
        foreach (var nc in value.NestedCommandSets)
        {
            AddNestedCommands(commands, $"{outer}{value.Suite} ", nc);
        }
    }

    internal void WriteUnknownCommand(string unknownCommand)
    {
        CommandSet.Error.WriteLine(CommandSet.Options.MessageLocalizer($"{CommandSet.Suite}: Unknown command: {unknownCommand}"));
        CommandSet.Error.WriteLine(CommandSet.Options.MessageLocalizer($"{CommandSet.Suite}: Use `{CommandSet.Suite} help` for usage."));
    }
}
