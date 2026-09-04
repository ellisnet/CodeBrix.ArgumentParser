================================================================================
AGENT-README: CodeBrix.ArgumentParser
A Guide for AI Coding Agents — CONSUMING the CodeBrix.ArgumentParser.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========

CodeBrix.ArgumentParser is a fully managed, dependency-free, cross-platform
command-line option parser for .NET 10 or later. It turns a `string[] args`
into invocations of callbacks you register, and it generates the matching
`--help` text for you.

What it supports:

  * Getopt::Long-style parsing with three prefixes: `-v`, `--verbose`, `/v`.
  * Required values (`name=`), optional values (`name:`), value-less flags,
    and `+`/`-` boolean negation (`-a+`, `-a-`).
  * Short-flag bundling (`-abc` == `-a -b -c`) and value bundling (`-Dk=v`).
  * Typed callbacks through `System.ComponentModel.TypeConverter`, so
    `Add<int>`, `Add<FileInfo>`, `Add<int?>` and friends work with no extra
    code.
  * Two-value (key/value) options with configurable separators.
  * A default handler (`"<>"`) that receives every unmatched argument.
  * A `--` terminator that turns off option processing for the rest of the
    command line.
  * Response files: `@options.rsp` expands in place, with cycle detection and
    a nesting-depth cap.
  * A `Command` / `CommandSet` / `HelpCommand` model for git-style suites of
    sub-commands, including nested suites and command-name completion.
  * Pluggable message localization for every user-facing string.
  * 80-column help rendering with option categories and hidden options.

Provenance: this library is a port of Mono.Options, MIT-licensed, with the
namespaces re-rooted under `CodeBrix.ArgumentParser`. Do not write
`using Mono.Options;` -- that namespace does not exist in this package, and the
types are split across four namespaces (see KEY NAMESPACES / USINGS below).
Behaviour is otherwise compatible with the upstream project, with added
hardening of the response-file reader (deterministic file-handle disposal,
strict unterminated-quote handling, cycle detection, depth cap).


INSTALLATION
============

  dotnet add package CodeBrix.ArgumentParser.MitLicenseForever

  PackageId:      CodeBrix.ArgumentParser.MitLicenseForever
  Assembly:       CodeBrix.ArgumentParser.dll
  Target:         .NET 10 or later (net10.0)
  Dependencies:   none beyond the .NET base class libraries
  License:        MIT
  Native code:    none; the package is pure managed IL with no platform
                  specific code, so it runs on every platform .NET 10 supports

There is exactly one package in this repository, so there is no "which one do
I reference" decision to make.

One caution for trimmed or native-AOT publishing: the typed `Add<T>` overloads
convert values through `System.ComponentModel.TypeDescriptor.GetConverter`,
which is reflection-based. If you trim aggressively, either keep the converter
types you rely on, or use the `Action<string>` overloads and convert the value
yourself inside the callback. Nothing else in the library reflects.

Note that the assembly name (`CodeBrix.ArgumentParser`) differs from the
package id (`CodeBrix.ArgumentParser.MitLicenseForever`); the `...Forever`
suffix is a licence marker on the package only. Namespaces follow the assembly
name, never the package id.


KEY NAMESPACES / USINGS
=======================

    using CodeBrix.ArgumentParser;                  // OptionSet, OptionException,
                                                    // OptionAction<TKey,TValue>
    using CodeBrix.ArgumentParser.Options;          // Option, OptionContext,
                                                    // OptionValueCollection,
                                                    // OptionValueType
    using CodeBrix.ArgumentParser.ArgumentSources;  // ArgumentSource,
                                                    // ResponseFileSource
    using CodeBrix.ArgumentParser.Commands;         // Command, CommandSet,
                                                    // HelpCommand

For the common case -- build an `OptionSet`, call `Parse`, catch
`OptionException` -- the single `using CodeBrix.ArgumentParser;` is enough.
You only need the `Options` namespace when you touch `OptionContext`,
`OptionValueCollection`, `OptionValueType`, or subclass `Option`.

The public surface is exactly twelve types. There are no hidden extension
methods, no source generators and no analyzers in this package.


CORE API REFERENCE
==================

OptionSet
---------
`CodeBrix.ArgumentParser.OptionSet : KeyedCollection<string, Option>`

The primary entry point: a keyed collection of `Option` objects, keyed by
every alias of every option. Construction:

    OptionSet()
    OptionSet(Converter<string, string> localizer)

The localizer is applied to every user-facing string the parser emits (help
text and error messages). Pass `null` for pass-through.

Properties:

    Converter<string, string> MessageLocalizer { get; }   // set is internal
    ReadOnlyCollection<ArgumentSource> ArgumentSources { get; }

Registration -- every `Add` returns the same `OptionSet`, so calls chain, and
all of them are usable from a C# collection initializer:

    OptionSet Add(string header)
    OptionSet Add(Option option)
    OptionSet Add(string prototype, Action<string> action)
    OptionSet Add(string prototype, string description, Action<string> action)
    OptionSet Add(string prototype, string description, Action<string> action,
                  bool hidden)
    OptionSet Add(string prototype, OptionAction<string, string> action)
    OptionSet Add(string prototype, string description,
                  OptionAction<string, string> action)
    OptionSet Add(string prototype, string description,
                  OptionAction<string, string> action, bool hidden)
    OptionSet Add<T>(string prototype, Action<T> action)
    OptionSet Add<T>(string prototype, string description, Action<T> action)
    OptionSet Add<TKey, TValue>(string prototype,
                                OptionAction<TKey, TValue> action)
    OptionSet Add<TKey, TValue>(string prototype, string description,
                                OptionAction<TKey, TValue> action)
    OptionSet Add(ArgumentSource source)

  -> `Add(string header)` is NOT an option. A single-string `Add` inserts a
     category/heading line into the help output at that position.
  -> The `Action<string>` overloads create an option with `MaxValueCount == 1`.
  -> The `OptionAction<...>` overloads create an option with
     `MaxValueCount == 2`.
  -> `Add<T>` / `Add<TKey, TValue>` convert each value with the target type's
     `TypeConverter`; a conversion failure is wrapped in an `OptionException`.
  -> `hidden: true` keeps the option working but omits it from
     `WriteOptionDescriptions`.

Parsing and help:

    List<string> Parse(IEnumerable<string> arguments)
    void WriteOptionDescriptions(TextWriter o)

`Parse` throws `ArgumentNullException` for a null argument list, invokes each
matched option's callback as it walks the arguments, and returns the arguments
it did not consume. If a `"<>"` default handler is registered, unmatched
arguments go to that handler instead and the returned list is EMPTY.

Inherited from `KeyedCollection<string, Option>` and useful in consumer code:

    Option this[string name] { get; }      // throws KeyNotFoundException
    Option this[int index] { get; set; }
    bool Contains(string name)
    bool Remove(string name)
    int Count { get; }
    void Clear()
    IEnumerator<Option> GetEnumerator()

Every alias is a key, so `Contains("v")` and `Contains("verbose")` are both
true for the prototype `"v|verbose"`, and the default handler is reachable as
`this["<>"]`.

Extension points for subclasses (`protected`):

    protected virtual OptionContext CreateOptionContext()
    protected virtual bool Parse(string argument, OptionContext c)
    protected bool GetOptionParts(string argument, out string flag,
                                  out string name, out string sep,
                                  out string value)
    protected override string GetKeyForItem(Option item)
    protected override void InsertItem(int index, Option item)
    protected override void RemoveItem(int index)
    protected override void SetItem(int index, Option item)
    protected Option GetOptionForName(string option)   // [Obsolete]

Option
------
`CodeBrix.ArgumentParser.Options.Option` (abstract)

One registered option. You rarely construct one directly -- the `Add`
overloads do it -- but you subclass it when a callback is not enough.

    protected Option(string prototype, string description)
    protected Option(string prototype, string description, int maxValueCount)
    protected Option(string prototype, string description, int maxValueCount,
                     bool hidden)

    string Prototype { get; }                // the raw prototype string
    string Description { get; }              // help text, may be null
    OptionValueType OptionValueType { get; } // None / Optional / Required
    int MaxValueCount { get; }               // values this option accepts
    bool Hidden { get; }                     // omitted from help output
    string[] GetNames()                      // defensive copy of the aliases
    string[] GetValueSeparators()            // defensive copy; empty if none
    void Invoke(OptionContext c)             // run the option, then reset c
    override string ToString()               // returns Prototype

    protected abstract void OnParseComplete(OptionContext c)
    protected static T Parse<T>(string value, OptionContext c)

`OnParseComplete` is where a subclass does its work; read the values from
`c.OptionValues`. `Option.Parse<T>` is the same TypeConverter-based conversion
`Add<T>` uses, including the `OptionException` wrapping, and it returns
`default(T)` when `value` is null.

Constructor validation (all throw before parsing ever starts):

  * null prototype -> `ArgumentNullException`; empty prototype ->
    `ArgumentException`.
  * negative `maxValueCount` -> `ArgumentOutOfRangeException`.
  * `maxValueCount == 0` with a `=` or `:` prototype -> `ArgumentException`.
  * `maxValueCount > 1` on a value-less prototype -> `ArgumentException`.
  * mixing `=` and `:` across aliases of one prototype -> `ArgumentException`.
  * value separators on an option that takes 0 or 1 values ->
    `ArgumentException`.
  * a `"<>"` prototype that requires values -> `ArgumentException`.

OptionValueType
---------------
`CodeBrix.ArgumentParser.Options.OptionValueType` (enum)

    None       // pure flag; prototype has no '=' or ':'
    Optional   // prototype ends with ':'
    Required   // prototype ends with '='

OptionContext
-------------
`CodeBrix.ArgumentParser.Options.OptionContext`

The per-argument state handed to an option while parsing. You see it when you
subclass `Option`.

    OptionContext(OptionSet set)
    Option Option { get; set; }                        // option being filled
    string OptionName { get; set; }                    // e.g. "--name"
    int OptionIndex { get; set; }                      // index into the args
    OptionSet OptionSet { get; }                       // owning set
    OptionValueCollection OptionValues { get; }        // values so far

`OptionName` carries the flag prefix that the user actually typed, which is
what you want in error messages.

OptionValueCollection
---------------------
`CodeBrix.ArgumentParser.Options.OptionValueCollection : IList, IList<string>`

The values gathered for the option currently being parsed.

    string this[int index] { get; set; }
    int Count { get; }
    bool IsReadOnly { get; }        // always false
    void Add(string item)
    void Clear()
    bool Contains(string item)
    void CopyTo(string[] array, int arrayIndex)
    bool Remove(string item)
    int IndexOf(string item)
    void Insert(int index, string item)
    void RemoveAt(int index)
    IEnumerator<string> GetEnumerator()
    List<string> ToList()
    string[] ToArray()
    override string ToString()      // values joined with ", "

The getter of the indexer validates before it returns:

  * `InvalidOperationException` if no option is currently being parsed.
  * `ArgumentOutOfRangeException` if `index >= Option.MaxValueCount`.
  * `OptionException` ("Missing required value for option ...") if the option
    is `Required` and no value arrived. This is how a missing value at the end
    of the command line surfaces.
  * otherwise `null` when `index` is past the values supplied so far.

OptionException
---------------
`CodeBrix.ArgumentParser.OptionException : Exception`

    OptionException()
    OptionException(string message, string optionName)
    OptionException(string message, string optionName, Exception innerException)
    string OptionName { get; }      // null when constructed without a name

This is the ONE exception type to catch around `Parse` for user-input errors.
`OptionName` names the offending option; `InnerException` carries the original
`TypeConverter` failure for typed options. Response-file failures report an
`OptionName` of `"@file"`.

OptionAction<TKey, TValue>
--------------------------
`CodeBrix.ArgumentParser.OptionAction<TKey, TValue>`

    public delegate void OptionAction<TKey, TValue>(TKey key, TValue value);

The callback shape for two-value options.

ArgumentSource
--------------
`CodeBrix.ArgumentParser.ArgumentSources.ArgumentSource` (abstract)

A hook that expands one command-line token into a sequence of arguments.
Register instances with `OptionSet.Add(ArgumentSource)`.

    protected ArgumentSource()
    abstract string[] GetNames()          // trigger tokens, e.g. { "@file" }
    abstract string Description { get; }  // shown in help output
    abstract bool GetArguments(string value, out IEnumerable<string> replacement)
    static IEnumerable<string> GetArgumentsFromFile(string file)
    static IEnumerable<string> GetArguments(TextReader reader)

Both static readers are lazy and tokenize the same way: tokens are separated
by the space character, and `"..."` or `'...'` quoted runs become one token
(the quotes are stripped). `GetArgumentsFromFile` opens the file and disposes
the reader when enumeration finishes or is abandoned; `GetArguments(TextReader)`
never disposes the reader you hand it. An unterminated quote throws
`OptionException`.

ResponseFileSource
------------------
`CodeBrix.ArgumentParser.ArgumentSources.ResponseFileSource : ArgumentSource`

    const int MaxNestingDepth = 16
    override string[] GetNames()        // { "@file" }
    override string Description         // "Read response file for more options."
    override bool GetArguments(string value, out IEnumerable<string> replacement)

Add one to an `OptionSet` and any argument of the form `@path` is replaced, in
place, by the whitespace-separated tokens read from `path`. Response files may
reference further response files.

Guarantees this implementation makes:

  * Files are opened lazily and closed deterministically, including when
    parsing throws part-way through.
  * A response file that references itself, directly or transitively, throws
    `OptionException` (cycle detection by canonical full path,
    case-insensitive).
  * Nesting deeper than `MaxNestingDepth` throws `OptionException`.
  * An unterminated quoted string throws `OptionException` instead of being
    silently truncated.
  * `@` paths are NOT sandboxed. Absolute paths, UNC paths and `..`-relative
    paths are all honoured, and a missing file surfaces as
    `FileNotFoundException` from inside `Parse`. If the command line can come
    from an untrusted source, screen `@`-prefixed tokens yourself before
    calling `Parse`.

Command
-------
`CodeBrix.ArgumentParser.Commands.Command`

One named sub-command, in the `git commit` sense.

    Command(string name, string help = null)

    string Name { get; }                        // whitespace-normalized
    string Help { get; }                        // one-line help text
    OptionSet Options { get; set; }             // command-specific options
    Action<IEnumerable<string>> Run { get; set; }
    CommandSet CommandSet { get; }              // set when added to a suite
    virtual int Invoke(IEnumerable<string> arguments)

`Invoke` parses `arguments` through `Options` when `Options` is non-null,
passes whatever is left to `Run`, and returns `0`. Both `Options` and `Run` are
optional; a `Command` with neither is legal and does nothing. Override
`Invoke` when you need a different exit code or want to skip the default flow.

A null or empty `name` throws `ArgumentNullException`. Runs of whitespace in
the name collapse to single spaces, so `new Command("foo  bar")` has
`Name == "foo bar"` -- multi-word command names are supported and are matched
greedily by `CommandSet`.

CommandSet
----------
`CodeBrix.ArgumentParser.Commands.CommandSet : KeyedCollection<string, Command>`

A suite of commands plus its own suite-level `OptionSet`.

    CommandSet(string suite, Converter<string, string> localizer = null)
    CommandSet(string suite, TextWriter output, TextWriter error,
               Converter<string, string> localizer = null)

The single-argument form writes to `Console.Out` / `Console.Error`; the
four-argument form is the one to use in tests. A null `suite`, `output` or
`error` throws `ArgumentNullException`.

    string Suite { get; }                       // e.g. "widget"
    TextWriter Out { get; }
    TextWriter Error { get; }
    Converter<string, string> MessageLocalizer { get; }

    new CommandSet Add(Command value)
    CommandSet Add(CommandSet nestedCommands)
    CommandSet Add(string header)
    CommandSet Add(Option option)
    CommandSet Add(ArgumentSource source)
    CommandSet Add(string prototype, Action<string> action)
    CommandSet Add(string prototype, string description, Action<string> action)
    CommandSet Add(string prototype, string description, Action<string> action,
                   bool hidden)
    CommandSet Add(string prototype, OptionAction<string, string> action)
    CommandSet Add(string prototype, string description,
                   OptionAction<string, string> action)
    CommandSet Add(string prototype, string description,
                   OptionAction<string, string> action, bool hidden)
    CommandSet Add<T>(string prototype, Action<T> action)
    CommandSet Add<T>(string prototype, string description, Action<T> action)
    CommandSet Add<TKey, TValue>(string prototype,
                                 OptionAction<TKey, TValue> action)
    CommandSet Add<TKey, TValue>(string prototype, string description,
                                 OptionAction<TKey, TValue> action)

    IEnumerable<string> GetCompletions(string prefix = null)
    int Run(IEnumerable<string> arguments)

Every `Add` mirrors the matching `OptionSet.Add` and returns the `CommandSet`
for chaining. The option overloads add to the suite-level option set, which is
the set `Run` parses before it picks a command.

`Run(args)` is the whole entry point of a command suite:

  1. Registers a `HelpCommand` if the suite does not already contain one, and
     registers hidden `help` / `?` options if the suite does not already
     define them.
  2. Parses `args` with the suite-level option set.
  3. With nothing left over: prints "Use `<suite> help` for usage." to `Out`
     and returns `1` (or shows help and returns `0` if `--help` was given).
  4. Otherwise resolves the leading argument(s) to a command -- longest match
     wins, and nested suites are searched too -- and returns that command's
     `Invoke` result. `Command.Invoke` returns `0` by default.
  5. An unrecognized command writes "Unknown command" plus a usage hint to
     `Error` and returns `1`.

Because it returns a process exit code, the idiomatic entry point is
`return new CommandSet("widget") { ... }.Run(args);`.

`Add(CommandSet)` nests a suite: its commands become reachable as
`<nested-suite> <command>` and appear in help under that compound name. A
`Command` instance may belong to only one `CommandSet`; adding it to a second
throws `ArgumentException`.

`GetCompletions(prefix)` returns the command names that start with `prefix`
(case-insensitive), including nested `"<suite> <command>"` names. It is a
building block for shell completion, not a completion script generator.

Inherited from `KeyedCollection<string, Command>`: `Contains(string)`,
`this[string]`, `this[int]`, `Count`, `Remove(string)`, `Clear()`, enumeration.

HelpCommand
-----------
`CodeBrix.ArgumentParser.Commands.HelpCommand : Command`

    HelpCommand()                                   // Name "help"
    override int Invoke(IEnumerable<string> arguments)

The built-in `help` sub-command, named `help` with the help text "Show this
message and exit". `CommandSet.Run` adds one automatically the first time it
runs, so you only construct one yourself to place it deliberately or to
subclass it. Its behaviour:

  * `<suite> help` -- writes the suite's full option descriptions (which
    include one line per command) to `CommandSet.Out`, returns `0`.
  * `<suite> help <command>` -- writes that command's own option descriptions,
    or, if the command has no `Options`, invokes the command with `--help`.
  * `<suite> help help` -- writes a usage banner plus an alphabetically sorted
    "Available commands:" listing that includes nested suites.
  * `<suite> help <unknown>` -- writes "Unknown command" to `CommandSet.Error`
    and returns `1`.


OPTION PROTOTYPE GRAMMAR
========================

An option is registered by its "prototype" string, which encodes the aliases,
whether a value is taken, and how multiple values are separated.

    name:    .+
    type:    [=:]        // '=' required value, ':' optional value,
                         // absent = value-less flag
    sep:     ( [^{}]+ | '{' .+ '}' )?
    aliases: ( name type sep ) ( '|' name type sep )*

Worked prototypes:

    "v|verbose"      flag; matches -v, --v, /v, --verbose, /verbose
    "n|name="        required value: --name=X, --name:X, --name X, -nX
    "n|name:"        optional value: --name=X, --name:X, or --name (null)
    "<>"             default handler; receives every unmatched argument
    "D="             two-value option; default separators ':' and '='
    "D={=}"          two-value option; '=' is the only separator
    "D={-->}{=>}"    two-value option with two multi-character separators
    "D=+-*/"         two-value option; each of + - * / separates
    "D={}"           two-value option with NO separator: the two values must
                     arrive as two separate command-line arguments

Rules that the constructor enforces:

  * The value type must agree across all aliases: `"a=|b:"` throws.
  * Separators are only meaningful when the option takes more than one value;
    declaring them on a one-value option throws.
  * `"<>"` may not require a value.
  * `{` and `}` must balance; an ill-formed separator group throws.

Description strings carry the value's display name for help output:

    "the {NAME} to greet"                -> renders "--name=NAME"
    "define a {0:NAME}={1:VALUE} macro"  -> renders "--define=NAME=VALUE"
    "50{{%}} of the time"                -> `{{` and `}}` are literal braces

Without a `{...}` marker the placeholder is `VALUE` for a one-value option and
`VALUE1`, `VALUE2` ... for a multi-value option.


HELP OUTPUT FORMATTING
======================

`WriteOptionDescriptions` renders a fixed 80-column layout: the option column
is 29 characters wide, descriptions wrap at column 80 and continuation lines
indent to column 31. An option whose prototype column exceeds 29 characters
puts its description on the next line. The layout is not console-width aware.

Rendering order is: options and category headers in the order you added them,
then every registered `ArgumentSource` last, regardless of when it was added.
Hidden options are skipped entirely. Short aliases render as `-x`, long
aliases as `--xyz`, optional values in brackets: `-l, --log[=FILE]`.


COMPLETE EXAMPLES
=================

Example 1 -- a full single-command program
------------------------------------------

    using System;
    using System.Collections.Generic;
    using CodeBrix.ArgumentParser;
    using CodeBrix.ArgumentParser.ArgumentSources;

    internal static class Program
    {
        private static int Main(string[] args)
        {
            var names = new List<string>();
            var macros = new Dictionary<string, string>();
            int repeat = 1;
            int verbosity = 0;
            bool upper = false;
            bool showHelp = false;
            string log = null;

            var options = new OptionSet
            {
                "Usage: greet [OPTIONS]+ MESSAGE",
                "Greet a person a number of times.",
                "",
                "Options:",
                { "n|name=", "the {NAME} of someone to greet",
                    v => names.Add(v) },
                { "r|repeat=", "the number of {TIMES} to repeat",
                    (int v) => repeat = v },
                { "u|upper", "greet in upper case",
                    v => upper = v != null },
                { "l|log:", "write a log to {FILE}",
                    v => log = v ?? "greet.log" },
                { "D|define={=}", "define a {0:NAME}={1:VALUE} macro",
                    (k, v) => macros[k] = v },
                { "v", "increase debug message verbosity",
                    v => { if (v != null) { ++verbosity; } } },
                { "h|help", "show this message and exit",
                    v => showHelp = v != null },
                new ResponseFileSource(),
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Error.WriteLine("greet: " + e.Message);
                Console.Error.WriteLine("Try `greet --help' for more information.");
                return 1;
            }

            if (showHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            string message = extra.Count > 0
                ? string.Join(" ", extra)
                : "Hello";
            if (upper)
            {
                message = message.ToUpperInvariant();
            }

            foreach (string name in names)
            {
                for (int i = 0; i < repeat; ++i)
                {
                    Console.WriteLine("{0}, {1}!", message, name);
                }
            }

            if (verbosity > 0)
            {
                Console.Error.WriteLine("verbosity: " + verbosity);
            }
            if (log != null)
            {
                Console.Error.WriteLine("log file: " + log);
            }
            foreach (KeyValuePair<string, string> macro in macros)
            {
                Console.Error.WriteLine($"macro {macro.Key} = {macro.Value}");
            }
            return 0;
        }
    }

What that program does, invocation by invocation:

    greet --name Alice Hello there
        -> "Hello there, Alice!"        (required value from the next argument;
                                         "Hello" and "there" are unconsumed and
                                         come back in `extra`)

    greet -n Alice -n Bob -r 2 -u
        -> "HELLO, Alice!" twice, then "HELLO, Bob!" twice
                                        (a repeated option calls its callback
                                         once per occurrence)

    greet -n=Alice   /  greet --name:Alice   /  greet /name=Alice
        -> all three prefixes and both separators are interchangeable; the
           attached short form -nAlice works too

    greet -uv -n Alice
        -> bundled short flags: -u and -v are both set

    greet -Dfoo=bar --define baz=qux -n Alice
        -> macros: foo = bar, baz = qux

    greet -l -n Alice                   -> log defaults to "greet.log"
    greet -l:run.log -n Alice           -> log is "run.log"

    greet -n Alice -- --not-an-option
        -> "--not-an-option, Alice!": after `--` the remaining arguments are
           text, never options, and they land in `extra`

    greet --help
        -> writes exactly:

    Usage: greet [OPTIONS]+ MESSAGE
    Greet a person a number of times.

    Options:
      -n, --name=NAME            the NAME of someone to greet
      -r, --repeat=TIMES         the number of TIMES to repeat
      -u, --upper                greet in upper case
      -l, --log[=FILE]           write a log to FILE
      -D, --define=NAME=VALUE    define a NAME=VALUE macro
      -v                         increase debug message verbosity
      -h, --help                 show this message and exit
      @file                      Read response file for more options.

    greet --repeat=NaN -n Alice
        -> the TypeConverter fails, `Parse` throws OptionException, and the
           catch block prints:
           greet: Could not convert string `NaN' to type Int32 for option `--repeat'.

Example 2 -- collecting unmatched arguments with the "<>" handler
-----------------------------------------------------------------

    var files = new List<string>();
    bool recurse = false;

    var options = new OptionSet
    {
        { "r|recurse", "recurse into sub-directories", v => recurse = v != null },
        { "<>", v => files.Add(v) },
    };

    List<string> extra = options.Parse(new[] { "-r", "a.txt", "b.txt" });
    // files: a.txt, b.txt
    // extra: EMPTY -- the "<>" handler consumed both, so nothing is returned

Register `"<>"` OR read the return value of `Parse`, not both. The handler also
receives everything after a `--` terminator.

Example 3 -- a command suite with two sub-commands and help
-----------------------------------------------------------

    using System;
    using System.Collections.Generic;
    using CodeBrix.ArgumentParser;
    using CodeBrix.ArgumentParser.Commands;

    internal static class Program
    {
        private static int Main(string[] args)
        {
            bool verbose = false;

            var build = new Command("build", "Build the widget");
            string output = "bin";
            bool release = false;
            build.Options = new OptionSet
            {
                { "o|output=", "output {DIR}", v => output = v },
                { "release", "build in release mode",
                    v => release = v != null },
            };
            build.Run = extra =>
            {
                Console.WriteLine($"building into {output} " +
                    $"({(release ? "release" : "debug")}); " +
                    $"targets: {string.Join(",", extra)}");
            };

            var clean = new Command("clean", "Remove build output")
            {
                Run = extra => Console.WriteLine("cleaning"),
            };

            var suite = new CommandSet("widget")
            {
                "Usage: widget COMMAND [OPTIONS]",
                { "v|verbose", "be verbose", v => verbose = v != null },
            };
            suite.Add(build);
            suite.Add(clean);

            return suite.Run(args);       // returns the process exit code
        }
    }

Behaviour:

    widget                      -> "Use `widget help` for usage."   (exit 1)
    widget build --release x y  -> "building into bin (release); targets: x,y"
    widget -v clean             -> "cleaning"                       (exit 0)
    widget frobnicate           -> stderr "widget: Unknown command: frobnicate"
                                          "widget: Use `widget help` for usage."
                                                                    (exit 1)

    widget help                 -> writes:

    Usage: widget COMMAND [OPTIONS]
      -v, --verbose              be verbose
            build                Build the widget
            clean                Remove build output

    widget help build           -> writes the build command's own options:

      -o, --output=DIR           output DIR
          --release              build in release mode

    widget help help            -> writes the sorted command listing:

    Usage: widget COMMAND [OPTIONS]
    Use `widget help COMMAND` for help on a specific command.

    Available commands:

            build                Build the widget
            clean                Remove build output
            help                 Show this message and exit

For a unit-testable suite, use the four-argument constructor and assert on the
writers:

    var stdout = new StringWriter();
    var stderr = new StringWriter();
    var suite = new CommandSet("widget", stdout, stderr);
    suite.Add(new Command("do") { Run = _ => { } });
    int exit = suite.Run(new[] { "do" });      // exit == 0

Example 4 -- response files
---------------------------

    // options.rsp:
    //     --name=Alice --verbose
    //     "--message=hello there"

    var options = new OptionSet
    {
        new ResponseFileSource(),
        { "n|name=", v => name = v },
        { "m|message=", v => message = v },
        { "v|verbose", v => verbose = v != null },
    };

    options.Parse(new[] { "@options.rsp" });
    // name == "Alice", verbose == true, message == "hello there"

Nested response files work -- a line inside `options.rsp` may itself be
`@more.rsp` -- up to `ResponseFileSource.MaxNestingDepth` levels, and a cycle
throws `OptionException` rather than looping forever. Arguments expand exactly
where the `@` token appeared, so later command-line arguments still override
earlier ones for last-one-wins options.

Example 5 -- a custom Option subclass and a custom ArgumentSource
------------------------------------------------------------------

    using CodeBrix.ArgumentParser.Options;

    internal sealed class DateOption : Option
    {
        private readonly Action<DateTime> _action;

        public DateOption(string prototype, string description,
                          Action<DateTime> action)
            : base(prototype, description, 1)
        {
            _action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            string raw = c.OptionValues[0];
            if (!DateTime.TryParse(raw, out DateTime value))
            {
                throw new OptionException(
                    $"'{raw}' is not a date for option {c.OptionName}.",
                    c.OptionName);
            }
            _action(value);
        }
    }

    var options = new OptionSet();
    options.Add(new DateOption("since=", "only entries after {DATE}",
        d => since = d));

    // A source that pulls defaults out of the environment:
    internal sealed class EnvironmentSource : ArgumentSource
    {
        public override string[] GetNames() => new[] { "%VAR%" };

        public override string Description =>
            "Read more options from an environment variable.";

        public override bool GetArguments(string value,
                                          out IEnumerable<string> replacement)
        {
            if (value == null || value.Length < 3 ||
                !value.StartsWith("%") || !value.EndsWith("%"))
            {
                replacement = null;
                return false;
            }
            string text = Environment.GetEnvironmentVariable(
                value.Trim('%')) ?? string.Empty;
            replacement = ArgumentSource.GetArguments(new StringReader(text));
            return true;
        }
    }

Example 6 -- localizing every message
--------------------------------------

    // MessageLocalizer runs over help text AND error messages.
    var options = new OptionSet(s => Resources.ResourceManager
        .GetString(s) ?? s)
    {
        { "v|verbose", "be verbose", v => verbose = v != null },
    };

The localizer can only be supplied through the constructor -- the
`MessageLocalizer` property has an internal setter. A `CommandSet` shares its
localizer with the suite's option set and with each command's `Options`.


MINIMUM VIABLE PROJECT
======================

greet.csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>Greet</RootNamespace>
        <AssemblyName>greet</AssemblyName>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.ArgumentParser.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs:

    using System;
    using System.Collections.Generic;
    using CodeBrix.ArgumentParser;

    bool showHelp = false;
    string name = "world";

    var options = new OptionSet
    {
        { "n|name=", "the {NAME} to greet", v => name = v },
        { "h|help", "show this message and exit", v => showHelp = v != null },
    };

    try
    {
        List<string> extra = options.Parse(args);
        if (showHelp)
        {
            options.WriteOptionDescriptions(Console.Out);
            return 0;
        }
        Console.WriteLine($"Hello, {name}!");
        return 0;
    }
    catch (OptionException e)
    {
        Console.Error.WriteLine("greet: " + e.Message);
        return 1;
    }

    dotnet run -- --name Alice        -> Hello, Alice!
    dotnet run -- --help             -> the option listing

(Add the package version yourself, or use central package management; version
pins are deliberately not written into this file.)


PERFORMANCE TIPS
================

Argument parsing is a once-per-process, microsecond-scale operation on inputs
of a few dozen strings; there is nothing here that needs tuning. The only
advice worth giving: build the `OptionSet` once rather than inside a loop
(each instance allocates its own `Regex`), and treat the callbacks as the hot
path only if you do real work inside them -- do the work after `Parse`
returns, from the variables the callbacks set. Response files are read lazily
and streamed token by token, so a large `@file` does not have to fit in memory
as a token array.


COMMON PITFALLS TO AVOID
========================

1.  A flag's callback receives a STRING, not a bool. For `-v` the callback gets
    the option name (`"v"`); for `-v+` it gets the raw argument; for `-v-` it
    gets `null`. The idiom is `v => flag = v != null`. Writing
    `v => flag = bool.Parse(v)` will throw.

2.  `"<>"` swallows the return value of `Parse`. When a default handler is
    registered, unmatched arguments are routed to it and `Parse` returns an
    empty list. Pick one mechanism. `"<>"` also cannot require a value --
    `"<>="` throws `ArgumentException` at registration time.

3.  Optional-value options (`:`) never take the NEXT argument. `--log run.txt`
    with the prototype `"l|log:"` invokes the callback with `null` and leaves
    `run.txt` in the unmatched list. The value must be attached:
    `--log=run.txt` or `--log:run.txt` or `-lrun.txt`. Required-value options
    (`=`) do consume the next argument.

4.  Bundling only works with the single `-` prefix. `-abc` expands to `-a -b
    -c`; `--abc` and `/abc` are looked up as whole names. Inside a bundle, an
    unregistered letter in the FIRST position simply means "not a bundle" (the
    argument falls through unmatched), but an unregistered letter in any later
    position throws `OptionException` ("Cannot use unregistered option ...").

5.  `--` stops option processing but is itself discarded, and it does NOT stop
    a `"<>"` handler from firing -- the remaining arguments go to the handler
    if one is registered, otherwise to the returned list. Response-file
    expansion is also disabled after `--`, so a trailing `@file` stays a
    literal string.

6.  TypeConverter failures surface as `OptionException`, not
    `FormatException`. The original exception is on `InnerException`. Catch
    `OptionException` around `Parse` and print `e.Message`; that message
    already names the option and the target type.

7.  Add order IS help order. Options, categories and commands render in
    registration order; `ArgumentSource` entries always render last regardless
    of where they were added. If you want a heading above a group, call
    `Add(header)` immediately before that group.

8.  A single-string `Add` is a heading, not an option. `options.Add("verbose")`
    silently adds the text "verbose" to the help output. An option always
    needs at least a prototype AND a callback.

9.  Aliases are dictionary keys, so two options may not share a name. Adding
    `"v|verbose"` and then `"v|version"` throws `ArgumentException` on the
    second `Add`.

10. Nothing enforces that an option was supplied. A "required option" is your
    job: check your variables after `Parse` and report the error yourself.
    Only a missing VALUE for an option the user did type raises
    `OptionException` ("Missing required value for option ...") -- and that
    error surfaces at the END of parsing, when the pending option is flushed.

11. `Add<T>` is for options that take a value. Applying it to a value-less
    prototype (`Add<bool>("v", ...)`) makes the parser hand the option NAME to
    the converter, which throws. Use the `Action<string>` overload for flags.

12. Two-value options default to `:` and `=` as separators, so
    `-Dkey:value` and `-Dkey=value` both work, and a value containing `=` will
    be split. Pin the separator with a brace group (`"D={=}"`) or suppress
    splitting entirely with `"D={}"` and pass the two values as separate
    arguments.

13. `@file` paths are not sandboxed and response-file tokens are split on the
    SPACE character only -- a tab inside a response file becomes part of the
    token. There is no comment syntax; every non-blank token is an argument.

14. `CommandSet.Run` mutates the suite the first time it is called (it adds a
    `HelpCommand` and hidden `help` / `?` options). Do not assert on `Count`
    before and after the first run, and do not share a `Command` instance
    between two `CommandSet`s -- that throws `ArgumentException`.

15. `Command.Invoke` returns `0` unless you override it, so a failing command
    still produces exit code 0 unless you subclass `Command` (or set a field
    from `Run` and return it yourself from `Main`).

16. `OptionValueCollection`'s indexer throws for a required option with no
    value; if you subclass `Option` and read `c.OptionValues[0]` you are
    relying on that behaviour, so do not "helpfully" guard it away with a
    `Count` check unless you intend to accept missing values.


WHAT THIS PACKAGE DOES NOT DO
=============================

  * No attribute- or POCO-based binding. There is no `[Option]` attribute and
    no mapping to a settings class; you register callbacks.
  * No dependency-injection, configuration-provider, or `IHost` integration.
  * No automatic `--version`, no automatic `--help` for a bare `OptionSet`
    (only `CommandSet` wires up help for you), and no man-page generation.
  * No required-option, mutually-exclusive-option or argument-count
    validation.
  * No shell-completion script generation. `CommandSet.GetCompletions` returns
    candidate names; wiring them into bash/zsh/pwsh is your job.
  * No environment-variable or config-file input. Response files are the only
    built-in external source; anything else means writing an
    `ArgumentSource`.
  * No sandboxing or allow-listing of `@file` paths.
  * No console-width detection, colour or ANSI styling in help output; the
    layout is a fixed 80 columns.
  * No async or cancellable parsing, and no thread-safety guarantee: an
    `OptionSet` is meant to be built and parsed on one thread.
  * No localization resources. `MessageLocalizer` is the hook; the strings
    are yours to translate.
  * It is not a shell: quoting, globbing and variable expansion are done by
    the operating system before your process starts (only response files do
    their own quote handling).


WORKING EXAMPLES ON GITHUB
==========================

The test suite is the executable specification for everything above -- roughly
100 tests, one behaviour per test, all named for the behaviour they lock down.

  https://github.com/ellisnet/CodeBrix.ArgumentParser/tree/main/tests/CodeBrix.ArgumentParser.Tests

  OptionSetTests.cs
      Prefixes, flags, required and optional values, `--` termination, the
      `"<>"` handler, `+`/`-` negation, bundling, typed and key/value options,
      `Contains`, headers, hidden options, help rendering, localization.
  OptionTests.cs
      Prototype grammar and its error cases; `GetValueSeparators` for default,
      brace, multi-character and character-sequence separators; `GetNames`
      defensive copying; `OptionValueType` mapping; `ToString`.
  OptionContextTests.cs
      `OptionValues` indexer validation (`InvalidOperationException`,
      `ArgumentOutOfRangeException`, missing-required-value
      `OptionException`), and the settable context properties.
  CommandTests.cs
      Constructor validation, name normalization, `Invoke` with and without
      `Options` and `Run`.
  CommandSetTests.cs
      Constructor validation, suite dispatch, unknown-command handling, the
      usage hint and exit codes, `help` listing, `GetCompletions`, and the
      one-command-one-set rule.
  ArgumentSourceTests.cs
      Response-file tokenization: whitespace splitting, single and double
      quoting, multi-line files, blank lines, reader-disposal semantics, and
      unterminated-quote failures.
  ResponseFileSourceTests.cs
      `@file` triggering, end-to-end expansion through `OptionSet.Parse`,
      nested files, missing files, self-referencing and mutually-recursive
      cycles, and the `MaxNestingDepth` cap.

The single-file usage sample in the package README is a smaller version of
Example 1 above.


QUICK REFERENCE CARD
====================

    // Registration
    var o = new OptionSet {
        "Heading",                                  // category line
        { "v|verbose", "desc", v => f = v != null },// flag
        { "n|name=",   "desc", v => s = v },        // required value
        { "l|log:",    "desc", v => s = v },        // optional value
        { "p|port=",   "desc", (int v) => p = v },  // typed value
        { "D=",        "desc", (k, v) => d[k] = v },// key/value pair
        { "secret=",   "desc", v => {}, true },     // hidden from help
        { "<>",                v => rest.Add(v) },  // default handler
        new ResponseFileSource(),                   // @file expansion
    };

    // Parse / help
    List<string> extra = o.Parse(args);   // throws OptionException on bad input
    o.WriteOptionDescriptions(Console.Out);

    // Command suite
    int exit = new CommandSet("suite") {
        "Usage: suite COMMAND [OPTIONS]",
        { "v|verbose", "desc", v => f = v != null },
    }
    .Add(new Command("build", "Build it") {
        Options = new OptionSet { { "o|out=", v => outDir = v } },
        Run = rest => DoBuild(outDir, rest),
    })
    .Run(args);

    Prototype       Meaning
    ------------    ---------------------------------------------------------
    "a"             flag           -a  --a  /a  -a+  -a-
    "a|alpha"       flag, aliases  -a  --alpha  /alpha
    "a="            required value -a X  -a=X  -a:X  -aX  --a=X  /a=X
    "a:"            optional value -a  -a=X  -a:X  -aX      (NOT: -a X)
    "a={=}"         two values separated by '='  -akey=value
    "a={}"          two values as two arguments  -a key value
    "<>"            everything unmatched

    Type          Namespace
    ------------- -------------------------------------------------------
    OptionSet     CodeBrix.ArgumentParser
    OptionException, OptionAction<TKey,TValue>
                  CodeBrix.ArgumentParser
    Option, OptionContext, OptionValueCollection, OptionValueType
                  CodeBrix.ArgumentParser.Options
    ArgumentSource, ResponseFileSource
                  CodeBrix.ArgumentParser.ArgumentSources
    Command, CommandSet, HelpCommand
                  CodeBrix.ArgumentParser.Commands

    Failure                                  Result
    ---------------------------------------  ------------------------------
    bad prototype at registration            ArgumentException family
    duplicate alias                          ArgumentException
    unconvertible typed value                OptionException (+ InnerException)
    missing value for a required option      OptionException
    unregistered letter inside a bundle      OptionException
    missing / cyclic / too-deep @file        OptionException or IOException
    unmatched argument                       returned by Parse (or "<>")
