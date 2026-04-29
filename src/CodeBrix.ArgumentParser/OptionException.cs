//
// OptionException.cs  (derived from Mono.Options Options.cs)
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

namespace CodeBrix.ArgumentParser; //was previously: Mono.Options;

/// <summary>
/// The exception thrown by <c>OptionSet</c> when an error occurs while
/// parsing command-line arguments — for example when a required value is
/// missing, a value cannot be converted to the target type, or an
/// unregistered option is encountered inside a bundle.
/// </summary>
public class OptionException : Exception
{
    private string option;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionException"/>
    /// class with no message and no associated option name.
    /// </summary>
    public OptionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionException"/>
    /// class with the specified message and the name of the option that
    /// triggered the error.
    /// </summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="optionName">The command-line name of the offending option.</param>
    public OptionException(string message, string optionName)
        : base(message)
    {
        this.option = optionName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionException"/>
    /// class with the specified message, option name, and inner exception.
    /// </summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="optionName">The command-line name of the offending option.</param>
    /// <param name="innerException">The underlying exception that triggered this one.</param>
    public OptionException(string message, string optionName, Exception innerException)
        : base(message, innerException)
    {
        this.option = optionName;
    }

    /// <summary>
    /// Gets the command-line name of the option that triggered the error,
    /// or <c>null</c> if the exception was constructed without a name.
    /// </summary>
    public string OptionName
    {
        get { return this.option; }
    }
}
