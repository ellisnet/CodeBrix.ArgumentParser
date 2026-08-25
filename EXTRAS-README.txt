================================================================================
EXTRAS-README: CodeBrix.ArgumentParser
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This repository ships no samples, no demo applications and no tools. It
contains exactly one packable project and one test project.

THE TEST PROJECT
================

  Path:  tests/CodeBrix.ArgumentParser.Tests/

The unit-test project is the only non-package content in the repository. It is
not packed and not published; it exists to lock down parser behaviour, and it
doubles as the worked-example set that AGENT-README.txt points consumers at
under "WORKING EXAMPLES ON GITHUB".

  dotnet test CodeBrix.ArgumentParser.slnx

It needs no test data files, no environment variables and no network access.
The response-file tests create their own temporary files with
`Path.GetTempFileName()` and delete them in a `finally` block, so nothing under
the repository is written to while the suite runs.

See MAINTAINER-README.txt for the test framework, naming conventions and the
provenance of the ported test classes.

WHAT IS NOT IN THIS REPOSITORY
==============================

No `samples/`, `libs/` or `tools/` folder, no benchmark project, no optional
test-data set, and no scripts. `bin/` and `obj/` folders that appear after a
build are ignored build output, not repository content.
