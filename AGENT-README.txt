========================================================================
  AGENT-README: CodeBrix.ArgumentParser
  A Comprehensive Guide for AI Coding Agents
========================================================================

OVERVIEW
--------
CodeBrix.ArgumentParser is a fully managed, cross-platform command-line
option parser for .NET. It is a direct fork of Mono.Options 6.12.0.148,
adapted to the CodeBrix family conventions. The library provides
Getopt::Long-style option parsing (short, long, and Windows-style
prefixes), typed option callbacks, multi-value options, option bundling,
response files, and a Command/CommandSet model for building
git-/svn-style CLI suites.

The library is distributed as a .NET 10 assembly in the NuGet package
`CodeBrix.ArgumentParser.MitLicenseForever`. Namespaces are rooted at
`CodeBrix.ArgumentParser` regardless of the package suffix.


INSTALLATION
------------
  dotnet add package CodeBrix.ArgumentParser.MitLicenseForever

Target framework:  .NET 10.0 or higher.
Namespace root:    CodeBrix.ArgumentParser
Dependencies:      none other than .NET itself.


KEY NAMESPACES
--------------
  using CodeBrix.ArgumentParser;                  // OptionSet, OptionException, OptionAction
  using CodeBrix.ArgumentParser.Options;          // Option, OptionContext, OptionValueCollection, OptionValueType
  using CodeBrix.ArgumentParser.ArgumentSources;  // ArgumentSource, ResponseFileSource
  using CodeBrix.ArgumentParser.Commands;         // Command, CommandSet, CommandOption, CommandOptionSet, HelpCommand, HelpOption
  using CodeBrix.ArgumentParser.Internal;         // StringCoda (internal helpers — not part of public API)


CORE API REFERENCE
------------------

  OptionSet
    The primary entry point. A keyed collection of Option instances
    that parses string arrays into invocations of registered callbacks.

    Construction:
      new OptionSet()
      new OptionSet(MessageLocalizerConverter localizer)

    Adding options (fluent, each returns `this`):
      Add(string header)                          // adds a header/category line in help output
      Add(string prototype, Action<string>)       // e.g. "v|verbose"
      Add(string prototype, string description, Action<string>)
      Add(string prototype, string description, Action<string>, bool hidden)
      Add(string prototype, OptionAction<string,string>)
      Add<T>(string prototype, Action<T>)         // typed via TypeConverter
      Add<TKey,TValue>(string prototype, OptionAction<TKey,TValue>)
      Add(ArgumentSource source)

    Parsing:
      List<string> Parse(IEnumerable<string> arguments)

    Help output:
      void WriteOptionDescriptions(TextWriter output)

  Option (abstract)
    Base class for all option types. Exposes Prototype, Description,
    OptionValueType, MaxValueCount, Hidden, GetNames(),
    GetValueSeparators(), and Invoke(OptionContext).

  OptionValueType
    enum { None, Optional, Required }

  OptionException : Exception
    Thrown on parse failures. Carries the OptionName that triggered it.

  OptionAction<TKey, TValue>
    delegate for two-value options.

  ArgumentSource (abstract) / ResponseFileSource
    Pluggable mechanism for expanding arguments from external sources
    (e.g. @response.rsp files).
    Security behavior shipped with this library:
      * Response files are opened lazily and the underlying file handle
        is disposed deterministically when enumeration ends or is
        abandoned early.
      * Unterminated quoted strings throw `OptionException` instead of
        being silently truncated.
      * `ResponseFileSource` rejects self-referencing and mutually-
        recursive `@file` chains (cycle detection by canonical path).
      * Nesting depth is capped at `ResponseFileSource.MaxNestingDepth`;
        exceeding it throws `OptionException`.
      * `@file` paths are NOT sandboxed — absolute, UNC, and `..`-
        relative paths are honored. Callers handling untrusted input
        should validate `@`-prefixed tokens before parsing.

  Command / CommandSet / CommandOption / CommandOptionSet / HelpCommand / HelpOption
    A Command is a named action ("commit", "status") with optional
    OptionSet and Run delegate. CommandSet is a keyed collection of
    Commands plus its own OptionSet for suite-level options; it provides
    a `help` command automatically. CommandOption and CommandOptionSet
    bridge Commands into an OptionSet so they appear in help output;
    HelpOption is the option-form counterpart to HelpCommand.


OPTION PROTOTYPE GRAMMAR
------------------------

  name: .+
  type: [=:]       // '=' required value, ':' optional value, absent = flag
  sep:  ( [^{}]+ | '{' .+ '}' )?
  aliases: ( name type sep ) ( '|' name type sep )*

  Example prototypes:
    "v|verbose"            // boolean flag, short or long form
    "n|name="              // required value:  --name=foo  or  --name foo
    "D="                   // repeated key/value pairs
    "i|include={:}"        // optional value with '{}' as separator
    "<>"                   // default handler for unmatched bare arguments


CODING CONVENTIONS (CodeBrix family)
------------------------------------

  * Nullable reference types: OFF. Do not use `?` on reference types or
    the null-forgiveness `!` operator. Value-type `int?`, `bool?`, etc.
    are fine.
  * Namespaces: file-scoped (`namespace Foo;`). No block namespaces.
  * No global usings. No `<ImplicitUsings>enable</>`.
  * All usings fully qualified, above the namespace, System.* first,
    alphabetical within each group.
  * Every public / protected member on a public type has an XML doc
    comment — `<GenerateDocumentationFile>` is on, so CS1591 fires for
    anything missing. Never suppress with <NoWarn>.
  * Ported files preserve the upstream copyright header verbatim and
    carry a `//was previously: Mono.Options;` comment on the namespace
    line.


ARCHITECTURE (sub-folder / sub-namespace layout)
-------------------------------------------------

  src/CodeBrix.ArgumentParser/
    OptionSet.cs                 — main entry class, parsing engine
    OptionException.cs           — exception type
    OptionAction.cs              — two-value delegate
    InternalsVisibleTo.cs        — grants internals to the test project

    Options/
      Option.cs                  — abstract base
      OptionContext.cs           — per-invocation parse context
      OptionValueCollection.cs   — values captured during parse
      OptionValueType.cs         — enum

    ArgumentSources/
      ArgumentSource.cs          — abstract
      ResponseFileSource.cs      — @file expansion

    Commands/
      Command.cs                 — named command
      CommandSet.cs              — keyed collection of Commands
      CommandOption.cs           — Option wrapper that surfaces a Command in OptionSet help
      CommandOptionSet.cs        — OptionSet specialization used by CommandSet
      HelpCommand.cs             — built-in `help` command
      HelpOption.cs              — built-in `--help` option

    Internal/
      StringCoda.cs              — internal line-wrapping helpers


TESTING
-------
Tests live in `tests/CodeBrix.ArgumentParser.Tests/` and use xUnit v3
plus SilverAssertions (fluent `.Should()` form). The OptionSet / Option /
OptionContext / Command / CommandSet test files were ported from the
upstream NUnit test suite at `mcs/class/Mono.Options/Test/Mono.Options/`
in the `mono-6.12.0.148` tag. The `ArgumentSourceTests` and
`ResponseFileSourceTests` files are CodeBrix-specific regression tests
that lock down response-file behavior and the security hardening listed
above (lazy file handles, cycle detection, max nesting depth, strict
unterminated-quote handling).

Run the full suite with:

  dotnet test CodeBrix.ArgumentParser.slnx
