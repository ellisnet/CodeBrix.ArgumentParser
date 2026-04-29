//
// ArgumentSource.cs  (derived from Mono.Options Options.cs)
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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeBrix.ArgumentParser.ArgumentSources; //was previously: Mono.Options;

/// <summary>
/// Abstract base for classes that expand one incoming argument into a
/// sequence of additional arguments — typically used to pull options
/// from a response file (<c>@args.rsp</c>) or a similar external source.
/// Derived types are registered via <c>OptionSet.Add(ArgumentSource)</c>.
/// </summary>
public abstract class ArgumentSource
{
    /// <summary>
    /// Initializes a new <see cref="ArgumentSource"/>.
    /// </summary>
    protected ArgumentSource()
    {
    }

    /// <summary>
    /// Returns the set of command-line tokens that trigger this source.
    /// For example, <see cref="ResponseFileSource"/> returns <c>{"@file"}</c>.
    /// </summary>
    /// <returns>An array of trigger tokens.</returns>
    public abstract string[] GetNames();

    /// <summary>Gets the human-readable description shown in help output.</summary>
    public abstract string Description { get; }

    /// <summary>
    /// When overridden, determines whether <paramref name="value"/> matches
    /// this source and, if so, yields the expanded argument list.
    /// </summary>
    /// <param name="value">The raw command-line token.</param>
    /// <param name="replacement">Receives the expanded arguments when matched.</param>
    /// <returns><c>true</c> if <paramref name="value"/> was consumed by this source; otherwise <c>false</c>.</returns>
    public abstract bool GetArguments(string value, out IEnumerable<string> replacement);

    /// <summary>
    /// Reads arguments from a file, one whitespace-delimited token at a time.
    /// Quoted segments (<c>"..."</c> or <c>'...'</c>) are treated as single tokens.
    /// </summary>
    /// <param name="file">The path to the response file.</param>
    /// <returns>A lazy sequence of arguments.</returns>
    public static IEnumerable<string> GetArgumentsFromFile(string file)
    {
        return GetArguments(File.OpenText(file), true);
    }

    /// <summary>
    /// Reads arguments from <paramref name="reader"/> without disposing it.
    /// </summary>
    /// <param name="reader">The text reader to consume.</param>
    /// <returns>A lazy sequence of arguments.</returns>
    public static IEnumerable<string> GetArguments(TextReader reader)
    {
        return GetArguments(reader, false);
    }

    // Cribbed from mcs/driver.cs:LoadArgs(string)
    static IEnumerable<string> GetArguments(TextReader reader, bool close)
    {
        try
        {
            StringBuilder arg = new StringBuilder();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int t = line.Length;

                for (int i = 0; i < t; i++)
                {
                    char c = line[i];

                    if (c == '"' || c == '\'')
                    {
                        char end = c;
                        bool closed = false;

                        for (i++; i < t; i++)
                        {
                            c = line[i];

                            if (c == end)
                            {
                                closed = true;
                                break;
                            }
                            arg.Append(c);
                        }

                        if (!closed)
                            throw new OptionException(
                                    string.Format(
                                        "Unterminated quoted string in response file (missing closing {0}).",
                                        end),
                                    "@file");
                    }
                    else if (c == ' ')
                    {
                        if (arg.Length > 0)
                        {
                            yield return arg.ToString();
                            arg.Length = 0;
                        }
                    }
                    else
                        arg.Append(c);
                }
                if (arg.Length > 0)
                {
                    yield return arg.ToString();
                    arg.Length = 0;
                }
            }
        }
        finally
        {
            if (close)
                reader.Dispose();
        }
    }
}
