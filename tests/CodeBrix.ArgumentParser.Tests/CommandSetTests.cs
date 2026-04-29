//
// CommandSetTests.cs  (derived from Mono.Options Test/Mono.Options/CommandSetTest.cs)
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
using System.IO;
using System.Linq;
using CodeBrix.ArgumentParser.Commands;
using SilverAssertions;
using Xunit;

namespace CodeBrix.ArgumentParser.Tests; //was previously: MonoTests.Mono.Options;

public class CommandSetTests
{
    static CommandSet NewSuite(out StringWriter output, out StringWriter error)
    {
        output = new StringWriter();
        error = new StringWriter();
        return new CommandSet("suite", output, error);
    }

    [Fact]
    public void Constructor_null_suite_throws_ArgumentNullException()
        => ((Action)(() => new CommandSet(null))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Constructor_null_output_throws_ArgumentNullException()
        => ((Action)(() => new CommandSet("s", null, TextWriter.Null))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Constructor_null_error_throws_ArgumentNullException()
        => ((Action)(() => new CommandSet("s", TextWriter.Null, null))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Suite_matches_constructor()
    {
        //Arrange
        var suite = NewSuite(out _, out _);

        //Act & Assert
        suite.Suite.Should().Be("suite");
    }

    [Fact]
    public void Add_null_command_throws_ArgumentNullException()
    {
        //Arrange
        var suite = NewSuite(out _, out _);

        //Act
        Action act = () => suite.Add((Command)null);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_null_nested_commandset_throws_ArgumentNullException()
    {
        //Arrange
        var suite = NewSuite(out _, out _);

        //Act
        Action act = () => suite.Add((CommandSet)null);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Command_cannot_be_added_to_two_sets()
    {
        //Arrange
        var suiteA = NewSuite(out _, out _);
        var suiteB = NewSuite(out _, out _);
        var c = new Command("cmd");
        suiteA.Add(c);

        //Act
        Action act = () => suiteB.Add(c);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_null_arguments_throws_ArgumentNullException()
    {
        //Arrange
        var suite = NewSuite(out _, out _);

        //Act
        Action act = () => suite.Run(null);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Run_empty_arguments_prints_usage_hint_and_returns_nonzero()
    {
        //Arrange
        var suite = NewSuite(out var output, out _);

        //Act
        int exit = suite.Run(new string[0]);

        //Assert
        exit.Should().Be(1);
        output.ToString().Should().Contain("Use `suite help`");
    }

    [Fact]
    public void Run_known_command_dispatches()
    {
        //Arrange
        var suite = NewSuite(out _, out _);
        bool invoked = false;
        suite.Add(new Command("do") { Run = _ => invoked = true });

        //Act
        int exit = suite.Run(new[] { "do" });

        //Assert
        exit.Should().Be(0);
        invoked.Should().BeTrue();
    }

    [Fact]
    public void Run_unknown_command_writes_error_and_returns_nonzero()
    {
        //Arrange
        var suite = NewSuite(out _, out var error);

        //Act
        int exit = suite.Run(new[] { "mysteryCommand" });

        //Assert
        exit.Should().Be(1);
        error.ToString().Should().Contain("Unknown command");
    }

    [Fact]
    public void Run_help_lists_commands()
    {
        //Arrange
        var suite = NewSuite(out var output, out _);
        suite.Add(new Command("alpha", "help for alpha") { Run = _ => { } });

        //Act
        int exit = suite.Run(new[] { "help" });

        //Assert
        exit.Should().Be(0);
        output.ToString().Should().Contain("alpha");
    }

    [Fact]
    public void GetCompletions_returns_prefix_matches()
    {
        //Arrange
        var suite = NewSuite(out _, out _);
        suite.Add(new Command("alpha"));
        suite.Add(new Command("beta"));
        suite.Add(new Command("alphabet"));

        //Act
        var matches = suite.GetCompletions("alph").ToList();

        //Assert
        matches.Should().Contain("alpha");
        matches.Should().Contain("alphabet");
        matches.Should().NotContain("beta");
    }
}
