//
// OptionContext.cs  (derived from Mono.Options Options.cs)
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

namespace CodeBrix.ArgumentParser.Options; //was previously: Mono.Options;

/// <summary>
/// Per-argument parsing context passed to each <see cref="Option"/>'s
/// callback as <c>OptionSet.Parse</c> iterates through the command line.
/// Stores the current option being processed, its zero-based argument
/// index, the accumulated values, and a back-reference to the owning
/// <c>OptionSet</c>.
/// </summary>
public class OptionContext
{
    private Option option;
    private string name;
    private int index;
    private OptionSet set;
    private OptionValueCollection c;

    /// <summary>
    /// Initializes a new <see cref="OptionContext"/> bound to the
    /// specified <paramref name="set"/>.
    /// </summary>
    /// <param name="set">The owning <see cref="OptionSet"/>.</param>
    public OptionContext(OptionSet set)
    {
        this.set = set;
        this.c = new OptionValueCollection(this);
    }

    /// <summary>
    /// Gets or sets the <see cref="Option"/> currently being processed.
    /// </summary>
    public Option Option
    {
        get { return option; }
        set { option = value; }
    }

    /// <summary>
    /// Gets or sets the command-line name (including any flag prefix such
    /// as <c>"--"</c>) that triggered the current option.
    /// </summary>
    public string OptionName
    {
        get { return name; }
        set { name = value; }
    }

    /// <summary>
    /// Gets or sets the zero-based index of the current argument within
    /// the arguments passed to <c>OptionSet.Parse</c>.
    /// </summary>
    public int OptionIndex
    {
        get { return index; }
        set { index = value; }
    }

    /// <summary>
    /// Gets the <see cref="OptionSet"/> that owns this context.
    /// </summary>
    public OptionSet OptionSet
    {
        get { return set; }
    }

    /// <summary>
    /// Gets the values accumulated for the current option during parsing.
    /// </summary>
    public OptionValueCollection OptionValues
    {
        get { return c; }
    }
}
