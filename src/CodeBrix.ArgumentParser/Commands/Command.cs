//
// Command.cs  (derived from Mono.Options Options.cs)
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
using System.Text;

namespace CodeBrix.ArgumentParser.Commands; //was previously: Mono.Options;

/// <summary>
/// A named action within a <see cref="CommandSet"/> — analogous to a
/// sub-command in tools like <c>git</c> or <c>svn</c>. Optionally carries
/// its own <see cref="OptionSet"/> of command-specific options and a
/// <see cref="Run"/> delegate that is invoked with any positional
/// arguments left after option parsing.
/// </summary>
public class Command
{
    /// <summary>Gets the normalized command name (whitespace collapsed).</summary>
    public string Name { get; }

    /// <summary>Gets the short help text shown in <c>help</c> listings.</summary>
    public string Help { get; }

    /// <summary>Gets or sets the command's own options.</summary>
    public OptionSet Options { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked once argument parsing completes;
    /// receives the positional arguments (arguments that were not
    /// consumed by the command's <see cref="Options"/>).
    /// </summary>
    public Action<IEnumerable<string>> Run { get; set; }

    /// <summary>
    /// Gets the <see cref="CommandSet"/> to which this command belongs,
    /// or <c>null</c> if it has not yet been added to one.
    /// </summary>
    public CommandSet CommandSet { get; internal set; }

    /// <summary>
    /// Initializes a new <see cref="Command"/> with the specified name
    /// and optional help text.
    /// </summary>
    /// <param name="name">The command name (e.g. <c>"commit"</c>).</param>
    /// <param name="help">Optional short help text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null or empty.</exception>
    public Command(string name, string help = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        Name = NormalizeCommandName(name);
        Help = help;
    }

    static string NormalizeCommandName(string name)
    {
        var value = new StringBuilder(name.Length);
        var space = false;
        for (int i = 0; i < name.Length; ++i)
        {
            if (!char.IsWhiteSpace(name, i))
            {
                space = false;
                value.Append(name[i]);
            }
            else if (!space)
            {
                space = true;
                value.Append(' ');
            }
        }
        return value.ToString();
    }

    /// <summary>
    /// Parses <paramref name="arguments"/> through <see cref="Options"/>
    /// (if any) and then invokes <see cref="Run"/> with the leftovers.
    /// </summary>
    /// <param name="arguments">The arguments to process.</param>
    /// <returns>An integer exit code (always <c>0</c> in the default implementation).</returns>
    public virtual int Invoke(IEnumerable<string> arguments)
    {
        var rest = Options?.Parse(arguments) ?? arguments;
        Run?.Invoke(rest);
        return 0;
    }
}
