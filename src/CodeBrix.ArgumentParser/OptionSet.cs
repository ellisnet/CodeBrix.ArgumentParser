//
// OptionSet.cs  (derived from Mono.Options Options.cs)
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

//
// A Getopt::Long-inspired option parsing library for C#.
//
// OptionSet is built upon a key/value table, where the key is a option
// format string and the value is a delegate that is invoked when the
// format string is matched.
//
// Option format strings:
//  Regex-like BNF Grammar:
//    name: .+
//    type: [=:]
//    sep: ( [^{}]+ | '{' .+ '}' )?
//    aliases: ( name type sep ) ( '|' name type sep )*
//
// Each '|'-delimited name is an alias for the associated action. If the
// format string ends in a '=', it has a required value. If the format
// string ends in a ':', it has an optional value. If neither '=' or ':'
// is present, no value is supported. '=' or ':' need only be defined on
// one alias, but if they are provided on more than one they must be
// consistent.
//
// See the AGENT-README and README files for full usage documentation.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CodeBrix.ArgumentParser.ArgumentSources;
using CodeBrix.ArgumentParser.Commands;
using CodeBrix.ArgumentParser.Internal;
using CodeBrix.ArgumentParser.Options;
using MessageLocalizerConverter = System.Converter<string, string>;

namespace CodeBrix.ArgumentParser; //was previously: Mono.Options;

/// <summary>
/// A keyed collection of <see cref="Option"/> instances that parses
/// command-line arguments, invoking each option's callback as matches
/// are found. Supports short, long, and Windows-style option prefixes;
/// required and optional values; boolean flags with <c>+</c>/<c>-</c>
/// negation; option bundling (<c>-abc</c>); typed conversion via
/// <see cref="System.ComponentModel.TypeConverter"/>; multi-value
/// options with custom separators; response-file expansion via
/// <see cref="ArgumentSource"/>; and automatic help generation.
/// </summary>
public class OptionSet : KeyedCollection<string, Option>
{
    /// <summary>
    /// Initializes a new <see cref="OptionSet"/> with the default
    /// (pass-through) message localizer.
    /// </summary>
    public OptionSet()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="OptionSet"/> with the specified
    /// message localizer, which is applied to every user-facing string
    /// emitted by the parser (help text, error messages).
    /// </summary>
    /// <param name="localizer">A converter from untranslated to localized text, or <c>null</c> for pass-through.</param>
    public OptionSet(MessageLocalizerConverter localizer)
    {
        this.roSources = new ReadOnlyCollection<ArgumentSource>(sources);
        this.localizer = localizer;
        if (this.localizer == null)
        {
            this.localizer = delegate (string f)
            {
                return f;
            };
        }
    }

    MessageLocalizerConverter localizer;

    /// <summary>
    /// Gets or sets the message localizer used to translate user-facing
    /// strings. The setter is internal; use the constructor to supply
    /// one externally.
    /// </summary>
    public MessageLocalizerConverter MessageLocalizer
    {
        get { return localizer; }
        internal set { localizer = value; }
    }

    List<ArgumentSource> sources = new List<ArgumentSource>();
    ReadOnlyCollection<ArgumentSource> roSources;

    /// <summary>Gets the registered <see cref="ArgumentSource"/>s as a read-only collection.</summary>
    public ReadOnlyCollection<ArgumentSource> ArgumentSources
    {
        get { return roSources; }
    }

    /// <inheritdoc />
    protected override string GetKeyForItem(Option item)
    {
        if (item == null)
            throw new ArgumentNullException("option");
        if (item.Names != null && item.Names.Length > 0)
            return item.Names[0];
        // This should never happen, as it's invalid for Option to be
        // constructed w/o any names.
        throw new InvalidOperationException("Option has no names!");
    }

    /// <summary>
    /// Legacy lookup helper. Prefer the indexer <c>this[string]</c>.
    /// </summary>
    /// <param name="option">The option name to look up.</param>
    /// <returns>The matching <see cref="Option"/>, or <c>null</c> if not found.</returns>
    [Obsolete("Use KeyedCollection.this[string]")]
    protected Option GetOptionForName(string option)
    {
        if (option == null)
            throw new ArgumentNullException("option");
        try
        {
            return base[option];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, Option item)
    {
        base.InsertItem(index, item);
        AddImpl(item);
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        Option p = Items[index];
        base.RemoveItem(index);
        // KeyedCollection.RemoveItem() handles the 0th item
        for (int i = 1; i < p.Names.Length; ++i)
        {
            Dictionary.Remove(p.Names[i]);
        }
    }

    /// <inheritdoc />
    protected override void SetItem(int index, Option item)
    {
        base.SetItem(index, item);
        AddImpl(item);
    }

    private void AddImpl(Option option)
    {
        if (option == null)
            throw new ArgumentNullException("option");
        List<string> added = new List<string>(option.Names.Length);
        try
        {
            // KeyedCollection.InsertItem/SetItem handle the 0th name.
            for (int i = 1; i < option.Names.Length; ++i)
            {
                Dictionary.Add(option.Names[i], option);
                added.Add(option.Names[i]);
            }
        }
        catch (Exception)
        {
            foreach (string name in added)
                Dictionary.Remove(name);
            throw;
        }
    }

    /// <summary>Adds a header/category line that appears in help output.</summary>
    /// <param name="header">The category text.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string header)
    {
        if (header == null)
            throw new ArgumentNullException("header");
        Add(new Category(header));
        return this;
    }

    internal sealed class Category : Option
    {
        // Prototype starts with '=' because this is an invalid prototype
        // (see Option.ParsePrototype(), and thus it'll prevent Category
        // instances from being accidentally used as normal options.
        public Category(string description)
            : base("=:Category:= " + description, description)
        {
        }

        protected override void OnParseComplete(OptionContext c)
        {
            throw new NotSupportedException("Category.OnParseComplete should not be invoked.");
        }
    }

    /// <summary>Adds an <see cref="Option"/> to the set.</summary>
    /// <param name="option">The option to add.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public new OptionSet Add(Option option)
    {
        base.Add(option);
        return this;
    }

    sealed class ActionOption : Option
    {
        Action<OptionValueCollection> action;

        public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action)
            : this(prototype, description, count, action, false)
        {
        }

        public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action, bool hidden)
            : base(prototype, description, count, hidden)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            this.action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(c.OptionValues);
        }
    }

    /// <summary>Adds a single-value option whose value is passed to <paramref name="action"/>.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string prototype, Action<string> action)
    {
        return Add(prototype, null, action);
    }

    /// <summary>Adds a single-value option with a description.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string prototype, string description, Action<string> action)
    {
        return Add(prototype, description, action, false);
    }

    /// <summary>Adds a single-value option with a description and visibility.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The callback.</param>
    /// <param name="hidden">When <c>true</c>, the option is omitted from help output.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string prototype, string description, Action<string> action, bool hidden)
    {
        if (action == null)
            throw new ArgumentNullException("action");
        Option p = new ActionOption(prototype, description, 1,
                delegate (OptionValueCollection v) { action(v[0]); }, hidden);
        base.Add(p);
        return this;
    }

    /// <summary>Adds a two-value (key, value) option.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string prototype, OptionAction<string, string> action)
    {
        return Add(prototype, null, action);
    }

    /// <summary>Adds a two-value (key, value) option with a description.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
    {
        return Add(prototype, description, action, false);
    }

    /// <summary>Adds a two-value (key, value) option with a description and visibility.</summary>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The two-value callback.</param>
    /// <param name="hidden">When <c>true</c>, the option is omitted from help output.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(string prototype, string description, OptionAction<string, string> action, bool hidden)
    {
        if (action == null)
            throw new ArgumentNullException("action");
        Option p = new ActionOption(prototype, description, 2,
                delegate (OptionValueCollection v) { action(v[0], v[1]); }, hidden);
        base.Add(p);
        return this;
    }

    sealed class ActionOption<T> : Option
    {
        Action<T> action;

        public ActionOption(string prototype, string description, Action<T> action)
            : base(prototype, description, 1)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            this.action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(Parse<T>(c.OptionValues[0], c));
        }
    }

    sealed class ActionOption<TKey, TValue> : Option
    {
        OptionAction<TKey, TValue> action;

        public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action)
            : base(prototype, description, 2)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            this.action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(
                    Parse<TKey>(c.OptionValues[0], c),
                    Parse<TValue>(c.OptionValues[1], c));
        }
    }

    /// <summary>Adds a typed single-value option.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The typed callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add<T>(string prototype, Action<T> action)
    {
        return Add(prototype, null, action);
    }

    /// <summary>Adds a typed single-value option with a description.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The typed callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add<T>(string prototype, string description, Action<T> action)
    {
        return Add(new ActionOption<T>(prototype, description, action));
    }

    /// <summary>Adds a typed two-value option.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="action">The typed two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
    {
        return Add(prototype, null, action);
    }

    /// <summary>Adds a typed two-value option with a description.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="prototype">The option prototype.</param>
    /// <param name="description">The help description.</param>
    /// <param name="action">The typed two-value callback.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
    {
        return Add(new ActionOption<TKey, TValue>(prototype, description, action));
    }

    /// <summary>Registers an <see cref="ArgumentSource"/> for expansion during parsing.</summary>
    /// <param name="source">The source to register.</param>
    /// <returns>This set, for fluent chaining.</returns>
    public OptionSet Add(ArgumentSource source)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        sources.Add(source);
        return this;
    }

    /// <summary>
    /// When overridden in a derived class, creates the per-parse
    /// <see cref="OptionContext"/>.
    /// </summary>
    /// <returns>A new context instance bound to this set.</returns>
    protected virtual OptionContext CreateOptionContext()
    {
        return new OptionContext(this);
    }

    /// <summary>
    /// Parses <paramref name="arguments"/>, invoking each matched option's
    /// callback and returning any arguments that were not consumed.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <returns>The arguments that were not matched by any option.</returns>
    public List<string> Parse(IEnumerable<string> arguments)
    {
        if (arguments == null)
            throw new ArgumentNullException("arguments");
        OptionContext c = CreateOptionContext();
        c.OptionIndex = -1;
        bool process = true;
        List<string> unprocessed = new List<string>();
        Option def = Contains("<>") ? this["<>"] : null;
        ArgumentEnumerator ae = new ArgumentEnumerator(arguments);
        foreach (string argument in ae)
        {
            ++c.OptionIndex;
            if (argument == "--")
            {
                process = false;
                continue;
            }
            if (!process)
            {
                Unprocessed(unprocessed, def, c, argument);
                continue;
            }
            if (AddSource(ae, argument))
                continue;
            if (!Parse(argument, c))
                Unprocessed(unprocessed, def, c, argument);
        }
        if (c.Option != null)
            c.Option.Invoke(c);
        return unprocessed;
    }

    class ArgumentEnumerator : IEnumerable<string>
    {
        List<IEnumerator<string>> sources = new List<IEnumerator<string>>();

        public ArgumentEnumerator(IEnumerable<string> arguments)
        {
            sources.Add(arguments.GetEnumerator());
        }

        public void Add(IEnumerable<string> arguments)
        {
            sources.Add(arguments.GetEnumerator());
        }

        public IEnumerator<string> GetEnumerator()
        {
            try
            {
                do
                {
                    IEnumerator<string> c = sources[sources.Count - 1];
                    if (c.MoveNext())
                        yield return c.Current;
                    else
                    {
                        c.Dispose();
                        sources.RemoveAt(sources.Count - 1);
                    }
                } while (sources.Count > 0);
            }
            finally
            {
                // Ensure that if the consumer (or an exception) abandons this
                // enumerator early, we still dispose every nested source so
                // file handles and per-source bookkeeping (e.g. response-file
                // cycle tracking) are released deterministically.
                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    try { sources[i].Dispose(); }
                    catch { /* swallow to keep the original exception propagating */ }
                }
                sources.Clear();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    bool AddSource(ArgumentEnumerator ae, string argument)
    {
        foreach (ArgumentSource source in sources)
        {
            IEnumerable<string> replacement;
            if (!source.GetArguments(argument, out replacement))
                continue;
            ae.Add(replacement);
            return true;
        }
        return false;
    }

    private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
    {
        if (def == null)
        {
            extra.Add(argument);
            return false;
        }
        c.OptionValues.Add(argument);
        c.Option = def;
        c.Option.Invoke(c);
        return false;
    }

    private readonly Regex ValueOption = new Regex(
        @"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

    /// <summary>
    /// Decomposes <paramref name="argument"/> into its flag prefix, name,
    /// separator, and value components.
    /// </summary>
    /// <param name="argument">The raw command-line argument.</param>
    /// <param name="flag">The matched flag prefix (<c>--</c>, <c>-</c>, or <c>/</c>).</param>
    /// <param name="name">The option name without prefix.</param>
    /// <param name="sep">The separator character (<c>=</c> or <c>:</c>), if any.</param>
    /// <param name="value">The inline value, if any.</param>
    /// <returns><c>true</c> if the argument matched the option pattern; otherwise <c>false</c>.</returns>
    protected bool GetOptionParts(string argument, out string flag, out string name, out string sep, out string value)
    {
        if (argument == null)
            throw new ArgumentNullException("argument");

        flag = name = sep = value = null;
        Match m = ValueOption.Match(argument);
        if (!m.Success)
        {
            return false;
        }
        flag = m.Groups["flag"].Value;
        name = m.Groups["name"].Value;
        if (m.Groups["sep"].Success && m.Groups["value"].Success)
        {
            sep = m.Groups["sep"].Value;
            value = m.Groups["value"].Value;
        }
        return true;
    }

    /// <summary>
    /// Attempts to parse a single argument, dispatching to the matched
    /// <see cref="Option"/>'s callback.
    /// </summary>
    /// <param name="argument">The argument to parse.</param>
    /// <param name="c">The current context.</param>
    /// <returns><c>true</c> if the argument was matched; otherwise <c>false</c>.</returns>
    protected virtual bool Parse(string argument, OptionContext c)
    {
        if (c.Option != null)
        {
            ParseValue(argument, c);
            return true;
        }

        string f, n, s, v;
        if (!GetOptionParts(argument, out f, out n, out s, out v))
            return false;

        Option p;
        if (Contains(n))
        {
            p = this[n];
            c.OptionName = f + n;
            c.Option = p;
            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    c.OptionValues.Add(n);
                    c.Option.Invoke(c);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    ParseValue(v, c);
                    break;
            }
            return true;
        }
        // no match; is it a bool option?
        if (ParseBool(argument, n, c))
            return true;
        // is it a bundled option?
        if (ParseBundledValue(f, string.Concat(n + s + v), c))
            return true;

        return false;
    }

    private void ParseValue(string option, OptionContext c)
    {
        if (option != null)
            foreach (string o in c.Option.ValueSeparators != null
                    ? option.Split(c.Option.ValueSeparators, c.Option.MaxValueCount - c.OptionValues.Count, StringSplitOptions.None)
                    : new string[] { option })
            {
                c.OptionValues.Add(o);
            }
        if (c.OptionValues.Count == c.Option.MaxValueCount ||
                c.Option.OptionValueType == OptionValueType.Optional)
            c.Option.Invoke(c);
        else if (c.OptionValues.Count > c.Option.MaxValueCount)
        {
            throw new OptionException(localizer(string.Format(
                            "Error: Found {0} option values when expecting {1}.",
                            c.OptionValues.Count, c.Option.MaxValueCount)),
                    c.OptionName);
        }
    }

    private bool ParseBool(string option, string n, OptionContext c)
    {
        Option p;
        string rn;
        if (n.Length >= 1 && (n[n.Length - 1] == '+' || n[n.Length - 1] == '-') &&
                Contains((rn = n.Substring(0, n.Length - 1))))
        {
            p = this[rn];
            string v = n[n.Length - 1] == '+' ? option : null;
            c.OptionName = option;
            c.Option = p;
            c.OptionValues.Add(v);
            p.Invoke(c);
            return true;
        }
        return false;
    }

    private bool ParseBundledValue(string f, string n, OptionContext c)
    {
        if (f != "-")
            return false;
        for (int i = 0; i < n.Length; ++i)
        {
            Option p;
            string opt = f + n[i].ToString();
            string rn = n[i].ToString();
            if (!Contains(rn))
            {
                if (i == 0)
                    return false;
                throw new OptionException(string.Format(localizer(
                                "Cannot use unregistered option '{0}' in bundle '{1}'."), rn, f + n), null);
            }
            p = this[rn];
            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    Invoke(c, opt, n, p);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    {
                        string v = n.Substring(i + 1);
                        c.Option = p;
                        c.OptionName = opt;
                        ParseValue(v.Length != 0 ? v : null, c);
                        return true;
                    }
                default:
                    throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
            }
        }
        return true;
    }

    private static void Invoke(OptionContext c, string name, string value, Option option)
    {
        c.OptionName = name;
        c.Option = option;
        c.OptionValues.Add(value);
        option.Invoke(c);
    }

    private const int OptionWidth = 29;
    private const int Description_FirstWidth = 80 - OptionWidth;
    private const int Description_RemWidth = 80 - OptionWidth - 2;

    static readonly string CommandHelpIndentStart = new string(' ', OptionWidth);
    static readonly string CommandHelpIndentRemaining = new string(' ', OptionWidth + 2);

    /// <summary>
    /// Writes a formatted listing of every registered option (and
    /// <see cref="ArgumentSource"/>) to <paramref name="o"/>. Hidden
    /// options are omitted.
    /// </summary>
    /// <param name="o">The destination text writer.</param>
    public void WriteOptionDescriptions(TextWriter o)
    {
        foreach (Option p in this)
        {
            int written = 0;

            if (p.Hidden)
                continue;

            Category c = p as Category;
            if (c != null)
            {
                WriteDescription(o, p.Description, "", 80, 80);
                continue;
            }
            CommandOption co = p as CommandOption;
            if (co != null)
            {
                WriteCommandDescription(o, co.Command, co.CommandName);
                continue;
            }

            if (!WriteOptionPrototype(o, p, ref written))
                continue;

            if (written < OptionWidth)
                o.Write(new string(' ', OptionWidth - written));
            else
            {
                o.WriteLine();
                o.Write(new string(' ', OptionWidth));
            }

            WriteDescription(o, p.Description, new string(' ', OptionWidth + 2),
                    Description_FirstWidth, Description_RemWidth);
        }

        foreach (ArgumentSource s in sources)
        {
            string[] names = s.GetNames();
            if (names == null || names.Length == 0)
                continue;

            int written = 0;

            Write(o, ref written, "  ");
            Write(o, ref written, names[0]);
            for (int i = 1; i < names.Length; ++i)
            {
                Write(o, ref written, ", ");
                Write(o, ref written, names[i]);
            }

            if (written < OptionWidth)
                o.Write(new string(' ', OptionWidth - written));
            else
            {
                o.WriteLine();
                o.Write(new string(' ', OptionWidth));
            }

            WriteDescription(o, s.Description, new string(' ', OptionWidth + 2),
                    Description_FirstWidth, Description_RemWidth);
        }
    }

    internal void WriteCommandDescription(TextWriter o, Command c, string commandName)
    {
        var name = new string(' ', 8) + (commandName ?? c.Name);
        if (name.Length < OptionWidth - 1)
        {
            WriteDescription(o, name + new string(' ', OptionWidth - name.Length) + c.Help, CommandHelpIndentRemaining, 80, Description_RemWidth);
        }
        else
        {
            WriteDescription(o, name, "", 80, 80);
            WriteDescription(o, CommandHelpIndentStart + c.Help, CommandHelpIndentRemaining, 80, Description_RemWidth);
        }
    }

    void WriteDescription(TextWriter o, string value, string prefix, int firstWidth, int remWidth)
    {
        bool indent = false;
        foreach (string line in GetLines(localizer(GetDescription(value)), firstWidth, remWidth))
        {
            if (indent)
                o.Write(prefix);
            o.WriteLine(line);
            indent = true;
        }
    }

    bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
    {
        string[] names = p.Names;

        int i = GetNextOptionIndex(names, 0);
        if (i == names.Length)
            return false;

        if (names[i].Length == 1)
        {
            Write(o, ref written, "  -");
            Write(o, ref written, names[0]);
        }
        else
        {
            Write(o, ref written, "      --");
            Write(o, ref written, names[0]);
        }

        for (i = GetNextOptionIndex(names, i + 1);
                i < names.Length; i = GetNextOptionIndex(names, i + 1))
        {
            Write(o, ref written, ", ");
            Write(o, ref written, names[i].Length == 1 ? "-" : "--");
            Write(o, ref written, names[i]);
        }

        if (p.OptionValueType == OptionValueType.Optional ||
                p.OptionValueType == OptionValueType.Required)
        {
            if (p.OptionValueType == OptionValueType.Optional)
            {
                Write(o, ref written, localizer("["));
            }
            Write(o, ref written, localizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
            string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0
                ? p.ValueSeparators[0]
                : " ";
            for (int c = 1; c < p.MaxValueCount; ++c)
            {
                Write(o, ref written, localizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
            }
            if (p.OptionValueType == OptionValueType.Optional)
            {
                Write(o, ref written, localizer("]"));
            }
        }
        return true;
    }

    static int GetNextOptionIndex(string[] names, int i)
    {
        while (i < names.Length && names[i] == "<>")
        {
            ++i;
        }
        return i;
    }

    static void Write(TextWriter o, ref int n, string s)
    {
        n += s.Length;
        o.Write(s);
    }

    static string GetArgumentName(int index, int maxIndex, string description)
    {
        var matches = Regex.Matches(description ?? "", @"(?<=(?<!\{)\{)[^{}]*(?=\}(?!\}))"); // ignore double braces
        string argName = "";
        foreach (Match match in matches)
        {
            var parts = match.Value.Split(':');
            // for maxIndex=1 it can be {foo} or {0:foo}
            if (maxIndex == 1)
            {
                argName = parts[parts.Length - 1];
            }
            // look for {i:foo} if maxIndex > 1
            if (maxIndex > 1 && parts.Length == 2 &&
                parts[0] == index.ToString(CultureInfo.InvariantCulture))
            {
                argName = parts[1];
            }
        }

        if (string.IsNullOrEmpty(argName))
        {
            argName = maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
        }
        return argName;
    }

    private static string GetDescription(string description)
    {
        if (description == null)
            return string.Empty;
        StringBuilder sb = new StringBuilder(description.Length);
        int start = -1;
        for (int i = 0; i < description.Length; ++i)
        {
            switch (description[i])
            {
                case '{':
                    if (i == start)
                    {
                        sb.Append('{');
                        start = -1;
                    }
                    else if (start < 0)
                        start = i + 1;
                    break;
                case '}':
                    if (start < 0)
                    {
                        if ((i + 1) == description.Length || description[i + 1] != '}')
                            throw new InvalidOperationException("Invalid option description: " + description);
                        ++i;
                        sb.Append("}");
                    }
                    else
                    {
                        sb.Append(description.Substring(start, i - start));
                        start = -1;
                    }
                    break;
                case ':':
                    if (start < 0)
                        goto default;
                    start = i + 1;
                    break;
                default:
                    if (start < 0)
                        sb.Append(description[i]);
                    break;
            }
        }
        return sb.ToString();
    }

    private static IEnumerable<string> GetLines(string description, int firstWidth, int remWidth)
    {
        return StringCoda.WrappedLines(description, firstWidth, remWidth);
    }
}
