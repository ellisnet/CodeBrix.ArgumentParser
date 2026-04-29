//
// OptionTests.cs  (derived from Mono.Options Test/Mono.Options/OptionTest.cs)
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
using CodeBrix.ArgumentParser.Options;
using SilverAssertions;
using Xunit;

namespace CodeBrix.ArgumentParser.Tests; //was previously: MonoTests.Mono.Options;

public class OptionTests
{
    [Fact]
    public void Constructor_null_prototype_throws_ArgumentNullException()
        => ((Action)(() => new TestDefaultOption(null, null))).Should().Throw<ArgumentNullException>();

    [Fact]
    public void Constructor_empty_prototype_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("", null))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_empty_alias_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("a|b||c=", null))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_conflicting_types_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("a=|b:", null))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_default_handler_with_required_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("<>=", null))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_default_handler_with_optional_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("<>:", null))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_default_handler_with_single_value_allowed()
        => new TestDefaultOption("t|<>=", null, 1).Prototype.Should().Be("t|<>=");

    [Fact]
    public void Constructor_default_handler_with_multi_value_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("t|<>=", null, 2))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_multi_value_alias_allowed()
        => new TestDefaultOption("a|b=", null, 2).MaxValueCount.Should().Be(2);

    [Fact]
    public void Constructor_negative_maxValueCount_throws_ArgumentOutOfRangeException()
        => ((Action)(() => new TestDefaultOption("a", null, -1))).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void Constructor_zero_maxValueCount_with_required_throws_ArgumentException()
        => ((Action)(() => new TestDefaultOption("a=", null, 0))).Should().Throw<ArgumentException>();

    [Fact]
    public void Constructor_ill_formed_brace_separator_throws_ArgumentException()
    {
        //Arrange
        Action act1 = () => new TestDefaultOption("a={", null);
        Action act2 = () => new TestDefaultOption("a=}", null);
        Action act3 = () => new TestDefaultOption("a={{}}", null);
        Action act4 = () => new TestDefaultOption("a={}}", null);
        Action act5 = () => new TestDefaultOption("a={}{", null);

        //Act & Assert
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
        act4.Should().Throw<ArgumentException>();
        act5.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_separator_with_single_value_throws_ArgumentException()
    {
        //Arrange
        Action act1 = () => new TestDefaultOption("a==", null);
        Action act2 = () => new TestDefaultOption("a={}", null);
        Action act3 = () => new TestDefaultOption("a=+-*/", null);

        //Act & Assert
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_zero_maxValueCount_with_flag_allowed()
        => new TestDefaultOption("a", null, 0).MaxValueCount.Should().Be(0);

    [Fact]
    public void GetValueSeparators_flag_option_is_empty()
        => new TestDefaultOption("a", null).GetValueSeparators().Length.Should().Be(0);

    [Fact]
    public void GetValueSeparators_single_value_required_is_empty()
        => new TestDefaultOption("a=", null, 1).GetValueSeparators().Length.Should().Be(0);

    [Fact]
    public void GetValueSeparators_two_value_default_is_colon_and_equals()
    {
        //Arrange
        var d = new TestDefaultOption("a=", null, 2);

        //Act
        string[] s = d.GetValueSeparators();

        //Assert
        s.Length.Should().Be(2);
        s[0].Should().Be(":");
        s[1].Should().Be("=");
    }

    [Fact]
    public void GetValueSeparators_two_value_empty_brace_is_no_separator()
        => new TestDefaultOption("a={}", null, 2).GetValueSeparators().Length.Should().Be(0);

    [Fact]
    public void GetValueSeparators_two_value_custom_multichar_separators()
    {
        //Arrange
        var d = new TestDefaultOption("a={-->}{=>}", null, 2);

        //Act
        string[] s = d.GetValueSeparators();

        //Assert
        s.Length.Should().Be(2);
        s[0].Should().Be("-->");
        s[1].Should().Be("=>");
    }

    [Fact]
    public void GetValueSeparators_two_value_char_sequence_becomes_individual_separators()
    {
        //Arrange
        var d = new TestDefaultOption("a=+-*/", null, 2);

        //Act
        string[] s = d.GetValueSeparators();

        //Assert
        s.Length.Should().Be(4);
        s[0].Should().Be("+");
        s[1].Should().Be("-");
        s[2].Should().Be("*");
        s[3].Should().Be("/");
    }

    [Fact]
    public void GetNames_returns_defensive_copy()
    {
        //Arrange
        var d = new TestDefaultOption("a|b|c", null);
        string[] n1 = d.GetNames();

        //Act
        n1[0] = "mutated";
        string[] n2 = d.GetNames();

        //Assert
        n2[0].Should().Be("a");
    }

    [Fact]
    public void OptionValueType_flag_is_None()
        => new TestDefaultOption("v|verbose", null).OptionValueType.Should().Be(OptionValueType.None);

    [Fact]
    public void OptionValueType_equals_is_Required()
        => new TestDefaultOption("name=", null).OptionValueType.Should().Be(OptionValueType.Required);

    [Fact]
    public void OptionValueType_colon_is_Optional()
        => new TestDefaultOption("name:", null).OptionValueType.Should().Be(OptionValueType.Optional);

    [Fact]
    public void ToString_returns_prototype()
        => new TestDefaultOption("v|verbose", null).ToString().Should().Be("v|verbose");
}
