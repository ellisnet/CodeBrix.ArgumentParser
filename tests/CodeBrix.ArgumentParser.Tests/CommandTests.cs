//
// CommandTests.cs  (derived from Mono.Options Test/Mono.Options/CommandTest.cs)
//
// Authors:
//  Jonathan Pryor <Jonathan.Pryor@microsoft.com>
//
// Copyright (C) 2017 Microsoft (http://www.microsoft.com)
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
using CodeBrix.ArgumentParser.Commands;
using SilverAssertions;
using Xunit;

namespace CodeBrix.ArgumentParser.Tests; //was previously: MonoTests.Mono.Options;

public class CommandTests
{
    [Fact]
    public void Constructor_null_name_throws_ArgumentNullException()
        => ((Action)(() => new Command(name: null, help: null))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Constructor_empty_name_throws_ArgumentNullException()
        => ((Action)(() => new Command(name: "", help: null))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Constructor_sets_name_and_help()
    {
        //Arrange
        var c = new Command("command", "help");

        //Act & Assert
        c.Name.Should().Be("command");
        c.Help.Should().Be("help");
    }

    [Fact]
    public void Constructor_normalizes_whitespace_in_name()
        => new Command("foo  bar\t baz").Name.Should().Be("foo bar baz");

    [Fact]
    public void Invoke_calls_Run()
    {
        //Arrange
        bool runInvoked = false;
        var c = new Command("command")
        {
            Run = v => runInvoked = true,
        };

        //Act
        int exit = c.Invoke(null);

        //Assert
        exit.Should().Be(0);
        runInvoked.Should().BeTrue();
    }

    [Fact]
    public void Invoke_no_run_and_no_options_returns_zero()
        => new Command("c").Invoke(null).Should().Be(0);

    [Fact]
    public void Invoke_uses_Options_then_Run()
    {
        //Arrange
        bool showHelp = false;
        var c = new Command("c")
        {
            Options = new OptionSet { { "help", v => showHelp = v != null } },
        };

        //Act
        int exit = c.Invoke(new[] { "--help" });

        //Assert
        exit.Should().Be(0);
        showHelp.Should().BeTrue();
    }
}
