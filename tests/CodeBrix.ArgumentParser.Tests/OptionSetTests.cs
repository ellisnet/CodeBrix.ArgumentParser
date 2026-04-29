//
// OptionSetTests.cs  (derived from Mono.Options Test/Mono.Options/OptionSetTest.cs)
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
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
using System.IO;
using CodeBrix.ArgumentParser.Options;
using SilverAssertions;
using Xunit;

namespace CodeBrix.ArgumentParser.Tests; //was previously: MonoTests.Mono.Options;

public class OptionSetTests
{
    [Fact]
    public void Parse_null_arguments_throws_ArgumentNullException()
    {
        //Arrange
        var p = new OptionSet();

        //Act
        Action act = () => p.Parse(null);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_null_prototype_action_throws_ArgumentNullException()
    {
        //Arrange
        var p = new OptionSet();

        //Act
        Action act = () => p.Add("a=", (Action<string>)null);

        //Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_flag_option_sets_value()
    {
        //Arrange
        bool verbose = false;
        var p = new OptionSet { { "v|verbose", v => verbose = v != null } };

        //Act
        p.Parse(new[] { "-v" });

        //Assert
        verbose.Should().BeTrue();
    }

    [Fact]
    public void Parse_flag_option_long_form_sets_value()
    {
        //Arrange
        bool verbose = false;
        var p = new OptionSet { { "v|verbose", v => verbose = v != null } };

        //Act
        p.Parse(new[] { "--verbose" });

        //Assert
        verbose.Should().BeTrue();
    }

    [Fact]
    public void Parse_flag_option_windows_form_sets_value()
    {
        //Arrange
        bool verbose = false;
        var p = new OptionSet { { "v|verbose", v => verbose = v != null } };

        //Act
        p.Parse(new[] { "/verbose" });

        //Assert
        verbose.Should().BeTrue();
    }

    [Fact]
    public void Parse_flag_repeated_invokes_multiple_times()
    {
        //Arrange
        int verbose = 0;
        var p = new OptionSet { { "v", v => ++verbose } };

        //Act
        p.Parse(new[] { "-v", "--v", "/v" });

        //Assert
        verbose.Should().Be(3);
    }

    [Fact]
    public void Parse_required_value_with_equals_sets_value()
    {
        //Arrange
        string name = null;
        var p = new OptionSet { { "n|name=", v => name = v } };

        //Act
        p.Parse(new[] { "-n=Alice" });

        //Assert
        name.Should().Be("Alice");
    }

    [Fact]
    public void Parse_required_value_with_following_arg_sets_value()
    {
        //Arrange
        string name = null;
        var p = new OptionSet { { "n|name=", v => name = v } };

        //Act
        p.Parse(new[] { "--name", "Alice" });

        //Assert
        name.Should().Be("Alice");
    }

    [Fact]
    public void Parse_optional_value_without_arg_passes_null()
    {
        //Arrange
        string got = "unchanged";
        var p = new OptionSet { { "n|name:", v => got = v } };

        //Act
        p.Parse(new[] { "-n" });

        //Assert
        got.Should().BeNull();
    }

    [Fact]
    public void Parse_optional_value_with_colon_sets_value()
    {
        //Arrange
        string got = null;
        var p = new OptionSet { { "n|name:", v => got = v } };

        //Act
        p.Parse(new[] { "-n:Alice" });

        //Assert
        got.Should().Be("Alice");
    }

    [Fact]
    public void Parse_returns_unprocessed_args()
    {
        //Arrange
        var p = new OptionSet { { "v", v => { } } };

        //Act
        List<string> extra = p.Parse(new[] { "-v", "foo", "bar" });

        //Assert
        extra.Should().ContainInOrder("foo", "bar");
    }

    [Fact]
    public void Parse_double_dash_stops_processing()
    {
        //Arrange
        bool verbose = false;
        var p = new OptionSet { { "v|verbose", v => verbose = v != null } };

        //Act
        List<string> extra = p.Parse(new[] { "--", "--verbose", "foo" });

        //Assert
        verbose.Should().BeFalse();
        extra.Should().ContainInOrder("--verbose", "foo");
    }

    [Fact]
    public void Parse_default_handler_captures_unmatched()
    {
        //Arrange
        var captured = new List<string>();
        var p = new OptionSet
        {
            { "v", v => { } },
            { "<>", v => captured.Add(v) },
        };

        //Act
        p.Parse(new[] { "-v", "alpha", "beta" });

        //Assert
        captured.Should().ContainInOrder("alpha", "beta");
    }

    [Fact]
    public void Parse_negatable_bool_plus_sets_non_null()
    {
        //Arrange
        string value = "start";
        var p = new OptionSet { { "a", v => value = v } };

        //Act
        p.Parse(new[] { "-a+" });

        //Assert
        value.Should().NotBeNull();
    }

    [Fact]
    public void Parse_negatable_bool_minus_sets_null()
    {
        //Arrange
        string value = "start";
        var p = new OptionSet { { "a", v => value = v } };

        //Act
        p.Parse(new[] { "-a-" });

        //Assert
        value.Should().BeNull();
    }

    [Fact]
    public void Parse_option_bundling_invokes_all()
    {
        //Arrange
        int a = 0, b = 0, c = 0;
        var p = new OptionSet
        {
            { "a", v => ++a },
            { "b", v => ++b },
            { "c", v => ++c },
        };

        //Act
        p.Parse(new[] { "-abc" });

        //Assert
        a.Should().Be(1);
        b.Should().Be(1);
        c.Should().Be(1);
    }

    [Fact]
    public void Parse_typed_int_converts_value()
    {
        //Arrange
        int port = 0;
        var p = new OptionSet { { "p|port=", (int v) => port = v } };

        //Act
        p.Parse(new[] { "--port", "8080" });

        //Assert
        port.Should().Be(8080);
    }

    [Fact]
    public void Parse_typed_conversion_failure_throws_OptionException()
    {
        //Arrange
        var p = new OptionSet { { "p|port=", (int v) => { } } };

        //Act
        Action act = () => p.Parse(new[] { "--port", "notAnInt" });

        //Assert
        act.Should().Throw<OptionException>();
    }

    [Fact]
    public void Parse_key_value_option_invokes_with_both()
    {
        //Arrange
        string k = null, val = null;
        var p = new OptionSet { { "D=", (a, b) => { k = a; val = b; } } };

        //Act
        p.Parse(new[] { "-D", "key", "value" });

        //Assert
        k.Should().Be("key");
        val.Should().Be("value");
    }

    [Fact]
    public void Contains_after_add_finds_by_first_alias()
    {
        //Arrange
        var p = new OptionSet { { "v|verbose", v => { } } };

        //Act & Assert
        p.Contains("v").Should().BeTrue();
        p.Contains("verbose").Should().BeTrue();
        p.Contains("missing").Should().BeFalse();
    }

    [Fact]
    public void Add_header_renders_in_help()
    {
        //Arrange
        var p = new OptionSet
        {
            "Section Heading",
            { "v", "be verbose", v => { } },
        };
        var sw = new StringWriter();

        //Act
        p.WriteOptionDescriptions(sw);
        string output = sw.ToString();

        //Assert
        output.Should().Contain("Section Heading");
    }

    [Fact]
    public void WriteOptionDescriptions_includes_registered_options()
    {
        //Arrange
        var p = new OptionSet
        {
            { "v|verbose", "be verbose", v => { } },
            { "n|name=", "the {NAME}", v => { } },
        };
        var sw = new StringWriter();

        //Act
        p.WriteOptionDescriptions(sw);
        string output = sw.ToString();

        //Assert
        output.Should().Contain("verbose");
        output.Should().Contain("name");
        output.Should().Contain("be verbose");
    }

    [Fact]
    public void WriteOptionDescriptions_skips_hidden_options()
    {
        //Arrange
        var p = new OptionSet
        {
            { "v|verbose", "be verbose", v => { }, false },
            { "secret=", "secret", v => { }, true },
        };
        var sw = new StringWriter();

        //Act
        p.WriteOptionDescriptions(sw);
        string output = sw.ToString();

        //Assert
        output.Should().Contain("verbose");
        output.Should().NotContain("secret");
    }

    [Fact]
    public void MessageLocalizer_custom_is_used()
    {
        //Arrange
        var p = new OptionSet(s => s.ToUpperInvariant())
        {
            { "v", "desc", v => { } },
        };
        var sw = new StringWriter();

        //Act
        p.WriteOptionDescriptions(sw);

        //Assert
        sw.ToString().Should().Contain("DESC");
    }

    [Fact]
    public void ArgumentSources_empty_by_default()
        => new OptionSet().ArgumentSources.Count.Should().Be(0);
}
