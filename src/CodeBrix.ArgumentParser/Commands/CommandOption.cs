//
// CommandOption.cs  (derived from Mono.Options Options.cs)
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
using CodeBrix.ArgumentParser.Options;

namespace CodeBrix.ArgumentParser.Commands; //was previously: Mono.Options;

internal class CommandOption : Option
{
    public Command Command { get; }
    public string CommandName { get; }

    // Prototype starts with '=' because this is an invalid prototype
    // (see Option.ParsePrototype(), and thus it'll prevent Category
    // instances from being accidentally used as normal options.
    public CommandOption(Command command, string commandName = null, bool hidden = false)
        : base("=:Command:= " + (commandName ?? command?.Name), (commandName ?? command?.Name), maxValueCount: 0, hidden: hidden)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));
        Command = command;
        CommandName = commandName ?? command.Name;
    }

    protected override void OnParseComplete(OptionContext c)
    {
        throw new NotSupportedException("CommandOption.OnParseComplete should not be invoked.");
    }
}
