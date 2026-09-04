# CodeBrix.ArgumentParser

A fully managed, cross-platform command-line option parser for .NET, for applications and tools that need `Getopt::Long`-style argument handling and git-style command suites.
CodeBrix.ArgumentParser has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.ArgumentParser.MitLicenseForever` NuGet package.

CodeBrix.ArgumentParser supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.ArgumentParser.MitLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.ArgumentParser`:

* NuGet package ID: `CodeBrix.ArgumentParser.MitLicenseForever`
* Assembly and primary namespace: `CodeBrix.ArgumentParser` - i.e. `using CodeBrix.ArgumentParser;`

XML documentation (IntelliSense) ships alongside the assembly.

The package has no NuGet dependencies of its own; it builds only against the .NET base class libraries.

## CodeBrix.ArgumentParser supports:

* `Getopt::Long`-inspired option parsing with short (`-v`), long (`--verbose`), and Windows-style (`/v`) option prefixes.
* Required values (`name=`), optional values (`name:`), value-less boolean flags, and negatable switches (`-a+` / `-a-`).
* Option bundling (`-abc` == `-a -b -c`) and `-Dkey=value`-style prefix bundling.
* Typed option callbacks via `System.ComponentModel.TypeConverter` — e.g. `Add<int>`, `Add<FileInfo>`.
* Key/value options via the `OptionAction<TKey, TValue>` delegate.
* Multi-value options with customisable separators (e.g. `--define foo=bar,baz=qux`).
* Response-file expansion via `ResponseFileSource` (`@args.rsp`).
* A `CommandSet` / `Command` / `HelpCommand` model for building git-/svn-style command suites.
* Localisation via the `OptionSet.MessageLocalizer` converter (a `System.Converter<string, string>`), supplied through the `OptionSet(Converter<string, string>)` constructor.
* Automatic help output via `OptionSet.WriteOptionDescriptions(TextWriter)`.

## Sample Code

### Parsing a simple set of options

```csharp
using CodeBrix.ArgumentParser;

int verbose = 0;
bool showHelp = false;
string name = null;

var p = new OptionSet
{
    { "v|verbose",  "Increase verbosity.",      v => ++verbose },
    { "n|name=",    "The {NAME} to greet.",     v => name = v },
    { "h|?|help",   "Show this message and exit.", v => showHelp = v != null },
};

List<string> extra = p.Parse(args);

if (showHelp)
{
    p.WriteOptionDescriptions(Console.Out);
    return;
}

Console.WriteLine($"Hello, {name}! (verbosity={verbose})");
```

### Localising the parser's user-facing text

```csharp
using CodeBrix.ArgumentParser;

var p = new OptionSet(text => MyResources.Translate(text))
{
    { "v|verbose", "Increase verbosity.", v => ++verbose },
};

// p.MessageLocalizer is the converter that was supplied above.
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.ArgumentParser.Tests` project:
https://github.com/ellisnet/CodeBrix.ArgumentParser/tree/main/tests/CodeBrix.ArgumentParser.Tests

## License

CodeBrix.ArgumentParser is licensed under the MIT License - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.ArgumentParser/blob/main/LICENSE) file.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.ArgumentParser/blob/main/THIRD-PARTY-NOTICES.txt).
