//
// OptionContextTests.cs  (derived from Mono.Options Test/Mono.Options/OptionContextTest.cs)
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

public class OptionContextTests
{
    [Fact]
    public void OptionValues_indexer_when_Option_null_throws_InvalidOperationException()
    {
        //Arrange
        OptionSet p = new OptionSet { { "a=", v => { } } };
        OptionContext c = new OptionContext(p);

        //Act
        Action act = () => { string _ = c.OptionValues[0]; };

        //Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void OptionValues_indexer_out_of_range_throws_ArgumentOutOfRangeException()
    {
        //Arrange
        OptionSet p = new OptionSet { { "a=", v => { } } };
        OptionContext c = new OptionContext(p) { Option = p[0] };

        //Act
        Action act = () => { string _ = c.OptionValues[2]; };

        //Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void OptionValues_missing_required_value_throws_OptionException()
    {
        //Arrange
        OptionSet p = new OptionSet { { "a=", v => { } } };
        OptionContext c = new OptionContext(p)
        {
            Option = p[0],
            OptionName = "-a",
        };

        //Act
        Action act = () => { string _ = c.OptionValues[0]; };

        //Assert
        act.Should().Throw<OptionException>();
    }

    [Fact]
    public void OptionSet_exposed_via_context()
    {
        //Arrange
        var set = new OptionSet();
        var c = new OptionContext(set);

        //Act & Assert
        c.OptionSet.Should().BeSameAs(set);
    }

    [Fact]
    public void OptionName_is_settable()
    {
        //Arrange
        var c = new OptionContext(new OptionSet());

        //Act
        c.OptionName = "-x";

        //Assert
        c.OptionName.Should().Be("-x");
    }

    [Fact]
    public void OptionIndex_is_settable()
    {
        //Arrange
        var c = new OptionContext(new OptionSet());

        //Act
        c.OptionIndex = 42;

        //Assert
        c.OptionIndex.Should().Be(42);
    }
}
