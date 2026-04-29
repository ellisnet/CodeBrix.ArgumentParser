# CodeBrix.ArgumentParser

A fully managed, cross-platform command-line option parser for .NET, forked from [Mono.Options](https://github.com/mono/mono/tree/mono-6.12.0.148/mcs/class/Mono.Options) 6.12.0.148 and adapted to the CodeBrix family conventions.
CodeBrix.ArgumentParser has no dependencies other than .NET, and is provided as a .NET 10 library and associated `CodeBrix.ArgumentParser.MitLicenseForever` NuGet package.

CodeBrix.ArgumentParser supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## CodeBrix.ArgumentParser supports:

* `Getopt::Long`-inspired option parsing with short (`-v`), long (`--verbose`), and Windows-style (`/v`) option prefixes.
* Required values (`name=`), optional values (`name:`), value-less boolean flags, and negatable switches (`-a+` / `-a-`).
* Option bundling (`-abc` == `-a -b -c`) and `-Dkey=value`-style prefix bundling.
* Typed option callbacks via `System.ComponentModel.TypeConverter` — e.g. `Add<int>`, `Add<FileInfo>`.
* Key/value options via the `OptionAction<TKey, TValue>` delegate.
* Multi-value options with customisable separators (e.g. `--define foo=bar,baz=qux`).
* Response-file expansion via `ResponseFileSource` (`@args.rsp`).
* A `CommandSet` / `Command` / `HelpCommand` model for building git-/svn-style command suites.
* Localisation via a pluggable `MessageLocalizerConverter` delegate.
* Automatic help output via `OptionSet.WriteOptionDescriptions(TextWriter)`.

## Sample Code

### Parsing a simple set of options

```csharp
using CodeBrix.ArgumentParser;
using CodeBrix.ArgumentParser.Options;

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

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License

CodeBrix.ArgumentParser incorporates source code from [Mono.Options](https://github.com/mono/mono/tree/mono-6.12.0.148/mcs/class/Mono.Options) 6.12.0.148, which is also MIT-licensed. See `THIRD-PARTY-NOTICES.txt` for the full attribution and upstream license text.
