//
// ArgumentSourceTests.cs
//
// Baseline behavioral tests for CodeBrix.ArgumentParser.ArgumentSources.ArgumentSource
// (the abstract base class and its static GetArguments / GetArgumentsFromFile helpers).
// These tests pin the publicly-observable tokenization behavior so any future change
// breaks loudly.
//

using System.IO;
using System.Linq;
using CodeBrix.ArgumentParser.ArgumentSources;
using SilverAssertions;
using Xunit;

namespace CodeBrix.ArgumentParser.Tests;

public class ArgumentSourceTests
{
    [Fact]
    public void GetArguments_TextReader_splits_on_whitespace()
    {
        //Arrange
        using var reader = new StringReader("--name=foo --verbose -x");

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEquivalentTo(new[] { "--name=foo", "--verbose", "-x" });
    }

    [Fact]
    public void GetArguments_TextReader_double_quoted_string_is_single_token()
    {
        //Arrange
        using var reader = new StringReader("--name=\"hello world\" --verbose");

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEquivalentTo(new[] { "--name=hello world", "--verbose" });
    }

    [Fact]
    public void GetArguments_TextReader_single_quoted_string_is_single_token()
    {
        //Arrange
        using var reader = new StringReader("--name='hello world' --verbose");

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEquivalentTo(new[] { "--name=hello world", "--verbose" });
    }

    [Fact]
    public void GetArguments_TextReader_multiple_lines_yield_multiple_tokens()
    {
        //Arrange
        using var reader = new StringReader("--name=foo\n--verbose\n-x");

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEquivalentTo(new[] { "--name=foo", "--verbose", "-x" });
    }

    [Fact]
    public void GetArguments_TextReader_empty_returns_no_tokens()
    {
        //Arrange
        using var reader = new StringReader(string.Empty);

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEmpty();
    }

    [Fact]
    public void GetArguments_TextReader_blank_lines_produce_no_tokens()
    {
        //Arrange
        using var reader = new StringReader("\n\n   \n");

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEmpty();
    }

    [Fact]
    public void GetArguments_TextReader_extra_internal_whitespace_collapses()
    {
        //Arrange
        using var reader = new StringReader("a    b   c");

        //Act
        var tokens = ArgumentSource.GetArguments(reader).ToList();

        //Assert
        tokens.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void GetArguments_TextReader_does_not_dispose_reader()
    {
        //Arrange
        var inner = new StringReader("a b");
        using var reader = new TrackingTextReader(inner);

        //Act
        _ = ArgumentSource.GetArguments(reader).ToList();

        //Assert — overload taking TextReader is documented to NOT close it.
        reader.WasDisposed.Should().BeFalse();
    }

    [Fact]
    public void GetArgumentsFromFile_reads_and_tokenizes()
    {
        //Arrange
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "--name=foo \"a b\" -x");

        try
        {
            //Act
            var tokens = ArgumentSource.GetArgumentsFromFile(path).ToList();

            //Assert
            tokens.Should().BeEquivalentTo(new[] { "--name=foo", "a b", "-x" });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetArgumentsFromFile_throws_FileNotFoundException_when_consumed()
    {
        //Arrange
        var bogus = Path.Combine(Path.GetTempPath(), "definitely_does_not_exist_" + System.Guid.NewGuid().ToString("N") + ".rsp");

        //Act
        // Materialize the sequence — we don't pin whether the exception is raised at
        // the call site or at first MoveNext, only that consumers observe it.
        System.Action act = () => ArgumentSource.GetArgumentsFromFile(bogus).ToList();

        //Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GetArguments_TextReader_unterminated_double_quote_throws()
    {
        //Arrange
        using var reader = new StringReader("--name=\"hello\nworld");

        //Act
        System.Action act = () => ArgumentSource.GetArguments(reader).ToList();

        //Assert
        act.Should().Throw<OptionException>();
    }

    [Fact]
    public void GetArguments_TextReader_unterminated_single_quote_throws()
    {
        //Arrange
        using var reader = new StringReader("--name='hello");

        //Act
        System.Action act = () => ArgumentSource.GetArguments(reader).ToList();

        //Assert
        act.Should().Throw<OptionException>();
    }

    private sealed class TrackingTextReader : TextReader
    {
        private readonly TextReader inner;
        public bool WasDisposed { get; private set; }

        public TrackingTextReader(TextReader inner) { this.inner = inner; }

        public override string ReadLine() => inner.ReadLine();
        public override int Read() => inner.Read();
        public override int Peek() => inner.Peek();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
