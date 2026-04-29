//
// CommandSet.cs  (derived from Mono.Options Options.cs)
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
using System.Collections.ObjectModel;
using System.IO;
using CodeBrix.ArgumentParser.ArgumentSources;
using CodeBrix.ArgumentParser.Options;
using MessageLocalizerConverter = System.Converter<string, string>;

namespace CodeBrix.ArgumentParser.Commands; //was previously: Mono.Options;

/// <summary>
/// A keyed collection of <see cref="Command"/> instances together with
/// its own <see cref="OptionSet"/>. Models a <c>git</c>-/<c>svn</c>-style
/// command suite: top-level options, multiple named commands, nested
/// sub-suites, and an automatic <c>help</c> command.
/// </summary>
public class CommandSet : KeyedCollection<string, Command>
{
    readonly string suite;

    OptionSet options;
    TextWriter outWriter;
    TextWriter errorWriter;

    internal List<CommandSet> NestedCommandSets;

    internal HelpCommand help;

    internal bool showHelp;

    internal OptionSet Options => options;

    /// <summary>
    /// Initializes a new <see cref="CommandSet"/> with the specified
    /// suite name, writing help/error output to the console.
    /// </summary>
    /// <param name="suite">The suite name (e.g. <c>"git"</c>).</param>
    /// <param name="localizer">Optional message localizer.</param>
    public CommandSet(string suite, MessageLocalizerConverter localizer = null)
        : this(suite, Console.Out, Console.Error, localizer)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="CommandSet"/> with explicit output
    /// and error writers.
    /// </summary>
    /// <param name="suite">The suite name (e.g. <c>"git"</c>).</param>
    /// <param name="output">The writer for normal output and help text.</param>
    /// <param name="error">The writer for error messages.</param>
    /// <param name="localizer">Optional message localizer.</param>
    public CommandSet(string suite, TextWriter output, TextWriter error, MessageLocalizerConverter localizer = null)
    {
        if (suite == null)
            throw new ArgumentNullException(nameof(suite));
        if (output == null)
            throw new ArgumentNullException(nameof(output));
        if (error == null)
            throw new ArgumentNullException(nameof(error));

        this.suite = suite;
        options = new CommandOptionSet(this, localizer);
        outWriter = output;
        errorWriter = error;
    }

    /// <summary>Gets the suite name supplied at construction.</summary>
    public string Suite => suite;

    /// <summary>Gets the writer used for normal output and help text.</summary>
    public TextWriter Out => outWriter;

    /// <summary>Gets the writer used for error messages.</summary>
    public TextWriter Error => errorWriter;

    /// <summary>Gets the message localizer shared with the suite's <see cref="OptionSet"/>.</summary>
    public MessageLocalizerConverter MessageLocalizer => options.MessageLocalizer;

    /// <inheritdoc />
    protected override string GetKeyForItem(Command item)
    {
        return item?.Name;
    }

    /// <summary>Adds a <see cref="Command"/> to the suite.</summary>
    /// <param name="value">The command to add.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public new CommandSet Add(Command value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        AddCommand(value);
        options.Add(new CommandOption(value));
        return this;
    }

    void AddCommand(Command value)
    {
        if (value.CommandSet != null && value.CommandSet != this)
        {
            throw new ArgumentException("Command instances can only be added to a single CommandSet.", nameof(value));
        }
        value.CommandSet = this;
        if (value.Options != null)
        {
            value.Options.MessageLocalizer = options.MessageLocalizer;
        }

        base.Add(value);

        help = help ?? value as HelpCommand;
    }

    /// <summary>Adds a header/category line to the suite's option help.</summary>
    /// <param name="header">The header text.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string header)
    {
        options.Add(header);
        return this;
    }

    /// <summary>Adds a bare <see cref="Option"/> to the suite's option set.</summary>
    /// <param name="option">The option to add.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(Option option)
    {
        options.Add(option);
        return this;
    }

    /// <summary>Adds a single-value option to the suite's option set.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string prototype, Action<string> action)
    {
        options.Add(prototype, action);
        return this;
    }

    /// <summary>Adds a single-value option with a description.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string prototype, string description, Action<string> action)
    {
        options.Add(prototype, description, action);
        return this;
    }

    /// <summary>Adds a single-value option with a description and visibility.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The callback.</param>
    /// <param name="hidden">When <c>true</c>, the option is omitted from help output.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string prototype, string description, Action<string> action, bool hidden)
    {
        options.Add(prototype, description, action, hidden);
        return this;
    }

    /// <summary>Adds a two-value (key, value) option.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string prototype, OptionAction<string, string> action)
    {
        options.Add(prototype, action);
        return this;
    }

    /// <summary>Adds a two-value (key, value) option with a description.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string prototype, string description, OptionAction<string, string> action)
    {
        options.Add(prototype, description, action);
        return this;
    }

    /// <summary>Adds a two-value (key, value) option with a description and visibility.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The two-value callback.</param>
    /// <param name="hidden">When <c>true</c>, the option is omitted from help output.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(string prototype, string description, OptionAction<string, string> action, bool hidden)
    {
        options.Add(prototype, description, action, hidden);
        return this;
    }

    /// <summary>Adds a typed single-value option.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The typed callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add<T>(string prototype, Action<T> action)
    {
        options.Add(prototype, null, action);
        return this;
    }

    /// <summary>Adds a typed single-value option with a description.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The typed callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add<T>(string prototype, string description, Action<T> action)
    {
        options.Add(prototype, description, action);
        return this;
    }

    /// <summary>Adds a typed two-value option.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The typed two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
    {
        options.Add(prototype, action);
        return this;
    }

    /// <summary>Adds a typed two-value option with a description.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The typed two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
    {
        options.Add(prototype, description, action);
        return this;
    }

    /// <summary>Registers an <see cref="ArgumentSource"/> with the suite's option set.</summary>
    /// <param name="source">The source to register.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(ArgumentSource source)
    {
        options.Add(source);
        return this;
    }

    /// <summary>
    /// Nests another <see cref="CommandSet"/> inside this one. The nested
    /// suite's commands become reachable as <c>&lt;nested.Suite&gt; command</c>.
    /// </summary>
    /// <param name="nestedCommands">The nested suite.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public CommandSet Add(CommandSet nestedCommands)
    {
        if (nestedCommands == null)
            throw new ArgumentNullException(nameof(nestedCommands));

        if (NestedCommandSets == null)
        {
            NestedCommandSets = new List<CommandSet>();
        }

        if (!AlreadyAdded(nestedCommands))
        {
            NestedCommandSets.Add(nestedCommands);
            foreach (var o in nestedCommands.options)
            {
                if (o is CommandOption c)
                {
                    options.Add(new CommandOption(c.Command, $"{nestedCommands.Suite} {c.CommandName}"));
                }
                else
                {
                    options.Add(o);
                }
            }
        }

        nestedCommands.options = this.options;
        nestedCommands.outWriter = this.outWriter;
        nestedCommands.errorWriter = this.errorWriter;

        return this;
    }

    bool AlreadyAdded(CommandSet value)
    {
        if (value == this)
            return true;
        if (NestedCommandSets == null)
            return false;
        foreach (var nc in NestedCommandSets)
        {
            if (nc.AlreadyAdded(value))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns command-name completions matching <paramref name="prefix"/>,
    /// including names from any nested <see cref="CommandSet"/>s.
    /// </summary>
    /// <param name="prefix">The text to complete.</param>
    /// <returns>A sequence of matching command names.</returns>
    public IEnumerable<string> GetCompletions(string prefix = null)
    {
        string rest;
        ExtractToken(ref prefix, out rest);

        foreach (var command in this)
        {
            if (command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return command.Name;
            }
        }

        if (NestedCommandSets == null)
            yield break;

        foreach (var subset in NestedCommandSets)
        {
            if (subset.Suite.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var c in subset.GetCompletions(rest))
                {
                    yield return $"{subset.Suite} {c}";
                }
            }
        }
    }

    static void ExtractToken(ref string input, out string rest)
    {
        rest = "";
        input = input ?? "";

        int top = input.Length;
        for (int i = 0; i < top; i++)
        {
            if (char.IsWhiteSpace(input[i]))
                continue;

            for (int j = i; j < top; j++)
            {
                if (char.IsWhiteSpace(input[j]))
                {
                    rest = input.Substring(j).Trim();
                    input = input.Substring(i, j).Trim();
                    return;
                }
            }
            rest = "";
            if (i != 0)
                input = input.Substring(i).Trim();
            return;
        }
    }

    /// <summary>
    /// Parses <paramref name="arguments"/>, dispatches to the selected
    /// <see cref="Command"/>, and returns its exit code. If no command
    /// is selected, shows help or a usage hint.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <returns>The exit code from the invoked command, or <c>1</c> on usage error.</returns>
    public int Run(IEnumerable<string> arguments)
    {
        if (arguments == null)
            throw new ArgumentNullException(nameof(arguments));

        this.showHelp = false;
        if (help == null)
        {
            help = new HelpCommand();
            AddCommand(help);
        }
        Action<string> setHelp = v => showHelp = v != null;
        if (!options.Contains("help"))
        {
            options.Add("help", "", setHelp, hidden: true);
        }
        if (!options.Contains("?"))
        {
            options.Add("?", "", setHelp, hidden: true);
        }
        var extra = options.Parse(arguments);
        if (extra.Count == 0)
        {
            if (showHelp)
            {
                return help.Invoke(extra);
            }
            Out.WriteLine(options.MessageLocalizer($"Use `{Suite} help` for usage."));
            return 1;
        }
        var command = GetCommand(extra);
        if (command == null)
        {
            help.WriteUnknownCommand(extra[0]);
            return 1;
        }
        if (showHelp)
        {
            if (command.Options?.Contains("help") ?? true)
            {
                extra.Add("--help");
                return command.Invoke(extra);
            }
            command.Options.WriteOptionDescriptions(Out);
            return 0;
        }
        return command.Invoke(extra);
    }

    internal Command GetCommand(List<string> extra)
    {
        return TryGetLocalCommand(extra) ?? TryGetNestedCommand(extra);
    }

    Command TryGetLocalCommand(List<string> extra)
    {
        var name = extra[0];
        if (Contains(name))
        {
            extra.RemoveAt(0);
            return this[name];
        }
        for (int i = 1; i < extra.Count; ++i)
        {
            name = name + " " + extra[i];
            if (!Contains(name))
                continue;
            extra.RemoveRange(0, i + 1);
            return this[name];
        }
        return null;
    }

    Command TryGetNestedCommand(List<string> extra)
    {
        if (NestedCommandSets == null)
            return null;

        var nestedCommands = NestedCommandSets.Find(c => c.Suite == extra[0]);
        if (nestedCommands == null)
            return null;

        var extraCopy = new List<string>(extra);
        extraCopy.RemoveAt(0);
        if (extraCopy.Count == 0)
            return null;

        var command = nestedCommands.GetCommand(extraCopy);
        if (command != null)
        {
            extra.Clear();
            extra.AddRange(extraCopy);
            return command;
        }
        return null;
    }
}
