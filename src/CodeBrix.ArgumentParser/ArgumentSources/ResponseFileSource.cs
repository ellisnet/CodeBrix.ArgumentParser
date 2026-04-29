//
// ResponseFileSource.cs  (derived from Mono.Options Options.cs)
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

namespace CodeBrix.ArgumentParser.ArgumentSources; //was previously: Mono.Options;

/// <summary>
/// An <see cref="ArgumentSource"/> that expands arguments of the form
/// <c>@filename</c> by reading the file's contents and splitting them on
/// whitespace.
/// <para>
/// <b>Security note:</b> The file path supplied via <c>@filename</c> is
/// resolved by the operating system without any sandboxing — absolute
/// paths, UNC paths, and relative paths containing <c>..</c> are all
/// honored. Applications that pass partially-trusted command-line input
/// to <see cref="OptionSet.Parse(System.Collections.Generic.IEnumerable{string})"/>
/// should validate or reject <c>@</c>-prefixed tokens before parsing.
/// </para>
/// <para>
/// To prevent denial-of-service via mutually-recursive or deeply-nested
/// response files, this source enforces a per-parse cycle check (a file
/// cannot reference itself, directly or transitively) and a maximum
/// nesting depth of <see cref="MaxNestingDepth"/>. Either condition
/// causes an <see cref="OptionException"/> to be thrown.
/// </para>
/// </summary>
public class ResponseFileSource : ArgumentSource
{
    /// <summary>
    /// The maximum permitted nesting depth for chained <c>@file</c>
    /// references. Exceeding this depth causes <see cref="GetArguments"/>
    /// to throw an <see cref="OptionException"/>.
    /// </summary>
    public const int MaxNestingDepth = 16;

    // Per-thread stack of canonical paths currently being expanded, used
    // to detect direct and transitive cycles. Thread-static is sufficient
    // because OptionSet.Parse drives the entire expansion synchronously
    // on a single thread.
    [System.ThreadStatic]
    static Stack<string> activePaths;

    /// <inheritdoc />
    public override string[] GetNames()
    {
        return new string[] { "@file" };
    }

    /// <inheritdoc />
    public override string Description
    {
        get { return "Read response file for more options."; }
    }

    /// <inheritdoc />
    public override bool GetArguments(string value, out IEnumerable<string> replacement)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith("@"))
        {
            replacement = null;
            return false;
        }
        string path = value.Substring(1);
        replacement = GetArgumentsGuarded(path);
        return true;
    }

    static IEnumerable<string> GetArgumentsGuarded(string path)
    {
        string canonical;
        try
        {
            canonical = Path.GetFullPath(path);
        }
        catch (System.Exception e)
        {
            throw new OptionException(
                    string.Format("Invalid response file path '{0}'.", path),
                    "@file",
                    e);
        }

        Stack<string> stack = activePaths;
        if (stack == null)
        {
            stack = new Stack<string>();
            activePaths = stack;
        }

        if (stack.Count >= MaxNestingDepth)
            throw new OptionException(
                    string.Format(
                        "Response file nesting exceeds the maximum allowed depth of {0}: '{1}'.",
                        MaxNestingDepth, path),
                    "@file");

        foreach (string existing in stack)
        {
            if (string.Equals(existing, canonical, System.StringComparison.OrdinalIgnoreCase))
                throw new OptionException(
                        string.Format("Response file '{0}' references itself (cycle detected).", path),
                        "@file");
        }

        stack.Push(canonical);
        try
        {
            foreach (string token in ArgumentSource.GetArgumentsFromFile(path))
                yield return token;
        }
        finally
        {
            stack.Pop();
            if (stack.Count == 0)
                activePaths = null;
        }
    }
}
