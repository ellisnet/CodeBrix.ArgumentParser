//
// ResponseFileSourceTests.cs
//
// Baseline behavioral tests for CodeBrix.ArgumentParser.ArgumentSources.ResponseFileSource.
// These tests pin the publicly-observable behavior consumers may rely on, so any future
// change to ResponseFileSource that breaks consumers will fail loudly.
//

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeBrix.ArgumentParser.ArgumentSources;
using SilverAssertions;
using Xunit;

namespace CodeBrix.ArgumentParser.Tests;

public class ResponseFileSourceTests
{
    [Fact]
    public void GetNames_returns_at_file_token()
    {
        //Arrange
        var src = new ResponseFileSource();

        //Act
        var names = src.GetNames();

        //Assert
        names.Should().NotBeNull();
        names.Length.Should().Be(1);
        names[0].Should().Be("@file");
    }

    [Fact]
    public void Description_is_non_empty()
    {
        //Arrange
        var src = new ResponseFileSource();

        //Act
        var desc = src.Description;

        //Assert
        desc.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetArguments_null_value_returns_false_and_null_replacement()
    {
        //Arrange
        var src = new ResponseFileSource();

        //Act
        IEnumerable<string> replacement;
        bool matched = src.GetArguments(null, out replacement);

        //Assert
        matched.Should().BeFalse();
        replacement.Should().BeNull();
    }

    [Fact]
    public void GetArguments_empty_value_returns_false_and_null_replacement()
    {
        //Arrange
        var src = new ResponseFileSource();

        //Act
        IEnumerable<string> replacement;
        bool matched = src.GetArguments("", out replacement);

        //Assert
        matched.Should().BeFalse();
        replacement.Should().BeNull();
    }

    [Fact]
    public void GetArguments_non_at_prefixed_value_returns_false()
    {
        //Arrange
        var src = new ResponseFileSource();

        //Act
        IEnumerable<string> replacement;
        bool matched = src.GetArguments("--name=foo", out replacement);

        //Assert
        matched.Should().BeFalse();
        replacement.Should().BeNull();
    }

    [Fact]
    public void GetArguments_at_prefixed_value_returns_true_with_replacement()
    {
        //Arrange
        var src = new ResponseFileSource();
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "--name=foo --verbose");

        try
        {
            //Act
            IEnumerable<string> replacement;
            bool matched = src.GetArguments("@" + path, out replacement);
            var tokens = replacement?.ToList();

            //Assert
            matched.Should().BeTrue();
            tokens.Should().NotBeNull();
            tokens.Should().BeEquivalentTo(new[] { "--name=foo", "--verbose" });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OptionSet_with_response_file_expands_and_parses()
    {
        //Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "--name=Alice --verbose");

        try
        {
            string name = null;
            bool verbose = false;
            var p = new OptionSet
            {
                new ResponseFileSource(),
                { "n|name=", v => name = v },
                { "v|verbose", v => verbose = v != null }
            };

            //Act
            var unprocessed = p.Parse(new[] { "@" + path });

            //Assert
            name.Should().Be("Alice");
            verbose.Should().BeTrue();
            unprocessed.Count.Should().Be(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OptionSet_with_nested_response_files_expands_recursively()
    {
        //Arrange
        var inner = Path.GetTempFileName();
        var outer = Path.GetTempFileName();
        File.WriteAllText(inner, "--name=Bob");
        File.WriteAllText(outer, "@" + inner + " --verbose");

        try
        {
            string name = null;
            bool verbose = false;
            var p = new OptionSet
            {
                new ResponseFileSource(),
                { "n|name=", v => name = v },
                { "v|verbose", v => verbose = v != null }
            };

            //Act
            p.Parse(new[] { "@" + outer });

            //Assert
            name.Should().Be("Bob");
            verbose.Should().BeTrue();
        }
        finally
        {
            File.Delete(inner);
            File.Delete(outer);
        }
    }

    [Fact]
    public void OptionSet_with_missing_response_file_throws_when_parsed()
    {
        //Arrange
        var p = new OptionSet
        {
            new ResponseFileSource(),
            { "n|name=", v => { } }
        };
        var bogus = "@" + Path.Combine(Path.GetTempPath(), "definitely_does_not_exist_" + System.Guid.NewGuid().ToString("N") + ".rsp");

        //Act
        System.Action act = () => p.Parse(new[] { bogus });

        //Assert
        // We deliberately do NOT pin *when* the FileNotFoundException is thrown
        // (call vs. iteration); only that consumers see one when parsing.
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void OptionSet_self_referencing_response_file_throws_OptionException()
    {
        //Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "@" + path);

        try
        {
            var p = new OptionSet
            {
                new ResponseFileSource(),
                { "n|name=", v => { } }
            };

            //Act
            System.Action act = () => p.Parse(new[] { "@" + path });

            //Assert
            act.Should().Throw<OptionException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OptionSet_mutually_recursive_response_files_throws_OptionException()
    {
        //Arrange
        var a = Path.GetTempFileName();
        var b = Path.GetTempFileName();
        File.WriteAllText(a, "@" + b);
        File.WriteAllText(b, "@" + a);

        try
        {
            var p = new OptionSet
            {
                new ResponseFileSource(),
                { "n|name=", v => { } }
            };

            //Act
            System.Action act = () => p.Parse(new[] { "@" + a });

            //Assert
            act.Should().Throw<OptionException>();
        }
        finally
        {
            File.Delete(a);
            File.Delete(b);
        }
    }

    [Fact]
    public void OptionSet_response_file_nesting_beyond_max_depth_throws_OptionException()
    {
        //Arrange
        // Build a chain a0 -> a1 -> a2 -> ... -> aN that exceeds MaxNestingDepth.
        int chainLength = ResponseFileSource.MaxNestingDepth + 2;
        var files = new string[chainLength];
        for (int i = 0; i < chainLength; i++)
            files[i] = Path.GetTempFileName();

        try
        {
            // Last file in the chain just contains a real option so the chain terminates.
            File.WriteAllText(files[chainLength - 1], "--name=deep");
            for (int i = 0; i < chainLength - 1; i++)
                File.WriteAllText(files[i], "@" + files[i + 1]);

            var p = new OptionSet
            {
                new ResponseFileSource(),
                { "n|name=", v => { } }
            };

            //Act
            System.Action act = () => p.Parse(new[] { "@" + files[0] });

            //Assert
            act.Should().Throw<OptionException>();
        }
        finally
        {
            foreach (var f in files)
            {
                try { File.Delete(f); } catch { }
            }
        }
    }
}
