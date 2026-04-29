//
// Option.cs  (derived from Mono.Options Options.cs)
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
using System.ComponentModel;
using CodeBrix.ArgumentParser.Commands;

namespace CodeBrix.ArgumentParser.Options; //was previously: Mono.Options;

/// <summary>
/// Abstract base class for a single command-line option known to an
/// <see cref="OptionSet"/>. Parses its "prototype" string into one or
/// more aliases, determines whether the option takes a value (and how
/// many), and exposes metadata (name list, value separators, description)
/// used by <c>OptionSet</c> during parsing and help generation.
/// </summary>
public abstract class Option
{
    string prototype, description;
    string[] names;
    OptionValueType type;
    int count;
    string[] separators;
    bool hidden;

    /// <summary>
    /// Initializes a new <see cref="Option"/> with a single-value
    /// prototype and optional description.
    /// </summary>
    /// <param name="prototype">The option prototype string (e.g. <c>"v|verbose"</c>, <c>"n|name="</c>).</param>
    /// <param name="description">Human-readable description for help output; may be <c>null</c>.</param>
    protected Option(string prototype, string description)
        : this(prototype, description, 1, false)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="Option"/> with an explicit maximum
    /// number of values.
    /// </summary>
    /// <param name="prototype">The option prototype string.</param>
    /// <param name="description">Human-readable description for help output; may be <c>null</c>.</param>
    /// <param name="maxValueCount">The maximum number of values this option accepts.</param>
    protected Option(string prototype, string description, int maxValueCount)
        : this(prototype, description, maxValueCount, false)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="Option"/> with the specified
    /// prototype, description, value count, and visibility.
    /// </summary>
    /// <param name="prototype">The option prototype string.</param>
    /// <param name="description">Human-readable description for help output; may be <c>null</c>.</param>
    /// <param name="maxValueCount">The maximum number of values this option accepts.</param>
    /// <param name="hidden">When <c>true</c>, the option is omitted from help output.</param>
    /// <exception cref="ArgumentNullException"><paramref name="prototype"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The prototype is invalid for the given value count.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValueCount"/> is negative.</exception>
    protected Option(string prototype, string description, int maxValueCount, bool hidden)
    {
        if (prototype == null)
            throw new ArgumentNullException("prototype");
        if (prototype.Length == 0)
            throw new ArgumentException("Cannot be the empty string.", "prototype");
        if (maxValueCount < 0)
            throw new ArgumentOutOfRangeException("maxValueCount");

        this.prototype = prototype;
        this.description = description;
        this.count = maxValueCount;
        this.names = (this is OptionSet.Category)
            // append GetHashCode() so that "duplicate" categories have distinct
            // names, e.g. adding multiple "" categories should be valid.
            ? new[] { prototype + this.GetHashCode() }
            : prototype.Split('|');

        if (this is OptionSet.Category || this is CommandOption)
            return;

        this.type = ParsePrototype();
        this.hidden = hidden;

        if (this.count == 0 && type != OptionValueType.None)
            throw new ArgumentException(
                    "Cannot provide maxValueCount of 0 for OptionValueType.Required or " +
                        "OptionValueType.Optional.",
                    "maxValueCount");
        if (this.type == OptionValueType.None && maxValueCount > 1)
            throw new ArgumentException(
                    string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
                    "maxValueCount");
        if (Array.IndexOf(names, "<>") >= 0 &&
                ((names.Length == 1 && this.type != OptionValueType.None) ||
                 (names.Length > 1 && this.MaxValueCount > 1)))
            throw new ArgumentException(
                    "The default option handler '<>' cannot require values.",
                    "prototype");
    }

    /// <summary>Gets the prototype string passed to the constructor.</summary>
    public string Prototype { get { return prototype; } }

    /// <summary>Gets the human-readable description used in help output.</summary>
    public string Description { get { return description; } }

    /// <summary>Gets the value-acceptance mode inferred from the prototype.</summary>
    public OptionValueType OptionValueType { get { return type; } }

    /// <summary>Gets the maximum number of values this option accepts.</summary>
    public int MaxValueCount { get { return count; } }

    /// <summary>Gets a value indicating whether this option is hidden from help output.</summary>
    public bool Hidden { get { return hidden; } }

    /// <summary>
    /// Returns a defensive copy of the parsed name aliases.
    /// </summary>
    /// <returns>A new string array containing the option's aliases.</returns>
    public string[] GetNames()
    {
        return (string[])names.Clone();
    }

    /// <summary>
    /// Returns a defensive copy of the value separators parsed from the
    /// prototype, or an empty array if none were specified.
    /// </summary>
    /// <returns>A new string array of the option's value separators.</returns>
    public string[] GetValueSeparators()
    {
        if (separators == null)
            return new string[0];
        return (string[])separators.Clone();
    }

    /// <summary>
    /// Converts <paramref name="value"/> to an instance of <typeparamref name="T"/>
    /// using the type's <see cref="TypeConverter"/>, wrapping any failure
    /// in an <see cref="OptionException"/>.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="value">The raw string value from the command line.</param>
    /// <param name="c">The current option context (used for diagnostic messages).</param>
    /// <returns>The converted value, or the default of <typeparamref name="T"/> if <paramref name="value"/> is <c>null</c>.</returns>
    /// <exception cref="OptionException">Thrown when conversion fails.</exception>
    protected static T Parse<T>(string value, OptionContext c)
    {
        Type tt = typeof(T);
        Type ti = tt;
        bool nullable =
            ti.IsValueType &&
            ti.IsGenericType &&
            !ti.IsGenericTypeDefinition &&
            ti.GetGenericTypeDefinition() == typeof(Nullable<>);
        Type targetType = nullable ? tt.GetGenericArguments()[0] : tt;
        T t = default(T);
        try
        {
            if (value != null)
            {
                TypeConverter conv = TypeDescriptor.GetConverter(targetType);
                t = (T)conv.ConvertFromString(value);
            }
        }
        catch (Exception e)
        {
            throw new OptionException(
                    string.Format(
                        c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                        value, targetType.Name, c.OptionName),
                    c.OptionName, e);
        }
        return t;
    }

    internal string[] Names { get { return names; } }
    internal string[] ValueSeparators { get { return separators; } }

    static readonly char[] NameTerminator = new char[] { '=', ':' };

    private OptionValueType ParsePrototype()
    {
        char type = '\0';
        List<string> seps = new List<string>();
        for (int i = 0; i < names.Length; ++i)
        {
            string name = names[i];
            if (name.Length == 0)
                throw new ArgumentException("Empty option names are not supported.", "prototype");

            int end = name.IndexOfAny(NameTerminator);
            if (end == -1)
                continue;
            names[i] = name.Substring(0, end);
            if (type == '\0' || type == name[end])
                type = name[end];
            else
                throw new ArgumentException(
                        string.Format("Conflicting option types: '{0}' vs. '{1}'.", type, name[end]),
                        "prototype");
            AddSeparators(name, end, seps);
        }

        if (type == '\0')
            return OptionValueType.None;

        if (count <= 1 && seps.Count != 0)
            throw new ArgumentException(
                    string.Format("Cannot provide key/value separators for Options taking {0} value(s).", count),
                    "prototype");
        if (count > 1)
        {
            if (seps.Count == 0)
                this.separators = new string[] { ":", "=" };
            else if (seps.Count == 1 && seps[0].Length == 0)
                this.separators = null;
            else
                this.separators = seps.ToArray();
        }

        return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
    }

    private static void AddSeparators(string name, int end, ICollection<string> seps)
    {
        int start = -1;
        for (int i = end + 1; i < name.Length; ++i)
        {
            switch (name[i])
            {
                case '{':
                    if (start != -1)
                        throw new ArgumentException(
                                string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                                "prototype");
                    start = i + 1;
                    break;
                case '}':
                    if (start == -1)
                        throw new ArgumentException(
                                string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                                "prototype");
                    seps.Add(name.Substring(start, i - start));
                    start = -1;
                    break;
                default:
                    if (start == -1)
                        seps.Add(name[i].ToString());
                    break;
            }
        }
        if (start != -1)
            throw new ArgumentException(
                    string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                    "prototype");
    }

    /// <summary>
    /// Invokes the option's callback for the given context, then resets
    /// per-option state on the context.
    /// </summary>
    /// <param name="c">The current option context.</param>
    public void Invoke(OptionContext c)
    {
        OnParseComplete(c);
        c.OptionName = null;
        c.Option = null;
        c.OptionValues.Clear();
    }

    /// <summary>
    /// When overridden in a derived class, performs the option's action
    /// using the values currently accumulated in <paramref name="c"/>.
    /// </summary>
    /// <param name="c">The current option context.</param>
    protected abstract void OnParseComplete(OptionContext c);

    internal void InvokeOnParseComplete(OptionContext c)
    {
        OnParseComplete(c);
    }

    /// <summary>Returns the option's prototype string.</summary>
    /// <returns>The prototype string.</returns>
    public override string ToString()
    {
        return Prototype;
    }
}
