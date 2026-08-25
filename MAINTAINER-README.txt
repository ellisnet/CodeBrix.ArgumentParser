================================================================================
MAINTAINER-README: CodeBrix.ArgumentParser
Notes for people and agents MAINTAINING this repository — not for package consumers
================================================================================

If you are consuming the NuGet package, stop reading and open AGENT-README.txt
instead. This file is about the repository itself.


PURPOSE AND SCOPE
=================

This repository produces exactly one NuGet package:

  Package id:   CodeBrix.ArgumentParser.MitLicenseForever
  Assembly:     CodeBrix.ArgumentParser.dll
  Project:      src/CodeBrix.ArgumentParser/CodeBrix.ArgumentParser.csproj
  Consumer doc: AGENT-README.txt (repo root)

One package, one AGENT-README, no sub-packages and no per-project
AGENT-README files.


REPOSITORY LAYOUT
=================

  CodeBrix.ArgumentParser.slnx        solution (Solution Items + Tests folders)
  AGENT-README.txt                    consumer documentation; SHIPS in the
                                      nupkg
  MAINTAINER-README.txt               this file
  EXTRAS-README.txt                   non-package content in the repo
  README-INDEX.txt                    map of the README files
  README.md                           human-facing readme; SHIPS in the nupkg
  LICENSE                             MIT
  THIRD-PARTY-NOTICES.txt             upstream attribution; SHIPS in the nupkg
  icon-codebrix-128.png               package icon; SHIPS in the nupkg
  AGENTS.md, CLAUDE.md, .clinerules, .cursorrules,
  .cursor/rules/agent-readme.mdc, .windsurfrules,
  .github/copilot-instructions.md, .junie/guidelines.md
                                      the eight AI-agent pointer stubs; they
                                      are reconciled centrally against the
                                      CodeBrix.SkiaSvg canonical copies -- do
                                      not hand-edit them here

  src/CodeBrix.ArgumentParser/
    CodeBrix.ArgumentParser.csproj    the one packable project
    OptionSet.cs                      parsing engine and help renderer
    OptionException.cs                the parse-failure exception
    OptionAction.cs                   two-value callback delegate
    InternalsVisibleTo.cs             grants internals to the test assembly
    Options/
      Option.cs                       abstract option; prototype parsing
      OptionContext.cs                per-argument parse state
      OptionValueCollection.cs        values captured for the current option
      OptionValueType.cs              None / Optional / Required
    ArgumentSources/
      ArgumentSource.cs               abstract source + static file readers
      ResponseFileSource.cs           @file expansion, cycles, depth cap
    Commands/
      Command.cs                      one named sub-command
      CommandSet.cs                   suite of commands + suite option set
      HelpCommand.cs                  the built-in `help` sub-command
      CommandOption.cs                INTERNAL: Option that renders a Command
                                      in help output
      CommandOptionSet.cs             INTERNAL: OptionSet specialization used
                                      by CommandSet
      HelpOption.cs                   INTERNAL: option-form counterpart to
                                      HelpCommand
    Internal/
      StringCoda.cs                   INTERNAL: line-wrapping helpers

  tests/CodeBrix.ArgumentParser.Tests/
    CodeBrix.ArgumentParser.Tests.csproj
    OptionSetTests.cs, OptionTests.cs, OptionContextTests.cs,
    CommandTests.cs, CommandSetTests.cs, ArgumentSourceTests.cs,
    ResponseFileSourceTests.cs, TestDefaultOption.cs

The public surface is twelve types. `CommandOption`, `CommandOptionSet`,
`HelpOption`, `StringCoda`, `OptionSet.Category` and the nested `ActionOption`
classes are all `internal` and must stay that way -- earlier revisions of the
consumer documentation wrongly presented the first three as public API.

Folder rules: `src/`, `tests/`, `samples/`, `libs/` are lower-case and
case-sensitive; every source file lives in a sub-folder that matches its
sub-namespace, except the entry types (`OptionSet`, `OptionException`,
`OptionAction`) which sit at the project root in the root namespace.


BUILDING
========

  dotnet restore CodeBrix.ArgumentParser.slnx
  dotnet build CodeBrix.ArgumentParser.slnx

Target framework is `net10.0` only, and it stays that way -- the CodeBrix
family is .NET 10 or later, full stop; `netstandard` targets exist elsewhere
in the family only for Roslyn analyzer hosts, which this repository does not
have.

`GenerateDocumentationFile` is on, so CS1591 fires for any public or protected
member without an XML doc comment. Fix the comment; never add `<NoWarn>`.

`GeneratePackageOnBuild` is true, so an ordinary `dotnet build` also produces
a .nupkg under `src/CodeBrix.ArgumentParser/bin/`.


TESTING
=======

  dotnet test CodeBrix.ArgumentParser.slnx

Roughly 100 tests across seven test classes; xUnit v3 with
`xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`
and SilverAssertions (fluent `.Should()` form). Test naming is
`<Class>Tests.cs` with snake_case method names describing the behaviour, and
bodies carry `//Arrange`, `//Act`, `//Assert` comments.

No opt-in environment variables, no special prep, no external services: every
test runs offline and in-process. The response-file tests create and delete
their own temp files via `Path.GetTempFileName()`.

Where the tests came from: `OptionSetTests`, `OptionTests`,
`OptionContextTests`, `CommandTests`, `CommandSetTests` and the
`TestDefaultOption` helper were ported from the upstream NUnit suite at
`mcs/class/Mono.Options/Test/Mono.Options/` in the `mono-6.12.0.148` tag and
converted to xUnit v3 plus SilverAssertions. `ArgumentSourceTests` and
`ResponseFileSourceTests` are CodeBrix-specific regression tests that lock
down the hardened response-file behaviour (lazy handles disposed
deterministically, strict unterminated-quote failures, cycle detection,
`MaxNestingDepth`).

`src/CodeBrix.ArgumentParser/InternalsVisibleTo.cs` grants internals to
`CodeBrix.ArgumentParser.Tests`, which is the family convention: every
packaging library ships one such file pointing at its `.Tests` assembly.


PACKAGING AND PUBLISHING
========================

Packaging is driven entirely by the csproj -- there is no pack script, no
.nuspec and no props/targets file in this repository.

What ships in the nupkg besides the assembly:

  icon-codebrix-128.png    -> PackageIcon
  README.md                -> PackageReadmeFile
  AGENT-README.txt         -> package root
  THIRD-PARTY-NOTICES.txt  -> package root

Keep the `<None Include="..\..\AGENT-README.txt" Pack="true" ... />` item in
the csproj: shipping the consumer documentation inside the package is the
whole point of the AGENT-README convention.

Versioning is the family date-stamped scheme, computed in the csproj from
`System.DateTime.UtcNow`: `1.<years since base year>.<day of year>.<minute of
day>`, always strictly increasing, never SemVer. `_VersionBaseYear` in the
csproj re-baselines the minor field. Two builds inside the same UTC minute
produce the same version, so do not publish twice within one minute.

Publishing rules that apply to this repository:

  * Never write a version number into AGENT-README.txt (the one exception is
    the upstream source version in the provenance sentence, which is a
    historical fact).
  * The git tag for a release must match the published nuget.org version.
  * The package id keeps its `.MitLicenseForever` suffix; the assembly and
    the namespaces never carry it.


PROVENANCE AND VENDORED SOURCES
===============================

The entire `src/CodeBrix.ArgumentParser/` tree is a port of Mono.Options
6.12.0.148 (MIT), taken from `mcs/class/Mono.Options/Mono.Options/Options.cs`
at the `mono-6.12.0.148` tag. Upstream shipped one very large source file; the
port splits it into one type per file under sub-folders that match the
sub-namespaces.

Rules for ported files:

  * Preserve the upstream copyright header verbatim at the top of each file,
    followed by a `(derived from Mono.Options Options.cs)` note on the file
    name line.
  * Each namespace declaration carries a `//was previously: Mono.Options;`
    trailing comment. Comment out replaced lines with `//was previously:`
    rather than deleting them.
  * `THIRD-PARTY-NOTICES.txt` carries the full upstream attribution and
    licence text and must be updated if any further upstream code is taken.

Deliberate divergences from upstream, all of them in the response-file path
and all covered by tests:

  * `OptionSet.ArgumentEnumerator.GetEnumerator` disposes every nested source
    in a `finally`, so file handles are released even when the consumer
    abandons enumeration early or an exception unwinds through it.
  * `ArgumentSource.GetArguments` throws `OptionException` on an unterminated
    quoted string instead of silently truncating the token.
  * `ResponseFileSource` adds `MaxNestingDepth` and thread-static cycle
    detection by canonical path.

If upstream code is ever re-taken, re-apply those three changes.


CODING CONVENTIONS
==================

These are the repo-specific rules; they were previously carried in the
consumer AGENT-README, where they did not belong.

  * Nullable reference types are OFF. Do not annotate reference types with
    `?` and do not use the null-forgiving `!` operator. Nullable value types
    (`int?`, `bool?`) are fine.
  * File-scoped namespaces (`namespace Foo;`). No block namespaces.
  * No global usings; no `<ImplicitUsings>enable</ImplicitUsings>`.
  * Usings go above the namespace, fully qualified, `System.*` first,
    alphabetical within each group. The `using MessageLocalizerConverter =
    System.Converter<string, string>;` alias is an upstream idiom and is kept
    in the three files that use it.
  * Top-of-file layout: licence/provenance header, then usings, then the
    file-scoped namespace.
  * Every public and protected member on a public type carries an XML doc
    comment.
  * The ported code keeps upstream's older C# idioms (explicit `delegate`
    expressions, `string.Format`, field-backed properties). Do not modernize
    ported files wholesale; it makes future upstream diffs unreadable.
  * Diagnostic and API identifiers inherited from upstream are never renamed.


NOTES
=====

  * The library has no dependencies, no native assets and no platform-specific
    code, so there is nothing to test per-OS beyond what CI already does.
  * `ResponseFileSource` uses a `[ThreadStatic]` stack for cycle detection.
    That is sound only because `OptionSet.Parse` drives the whole expansion
    synchronously on the calling thread. If parsing is ever made async or
    parallel, that mechanism has to be revisited.
  * `OptionSet.WriteOptionDescriptions` hard-codes an option column of 29 and
    a total width of 80. Changing either is a breaking change to every
    consumer's help output and to the tests that assert on it.
  * The public surface is small and stable by design. New API is added only
    when it is needed by a CodeBrix consumer; the goal is fidelity to the
    upstream behaviour, not feature growth.
