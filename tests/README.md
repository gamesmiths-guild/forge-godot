# Tests

Tests for the Forge Godot plugin, using [gdUnit4](https://github.com/godot-gdunit-labs/gdUnit4Net) with FluentAssertions, matching the assertion style of the core Forge test suite.

## Why these build into the plugin assembly

gdUnit4 resolves the Godot project from the `.csproj`'s own directory, so a separate test project cannot reach this Godot project. Tests therefore compile into `Gamesmiths.Forge.Godot.dll` alongside the plugin, and the test packages live in `Forge.Dev.props` under a `Debug`-only condition.

Nothing here ships. `.gitattributes` allowlists only `addons/`, `forge_samples/` and `Directory.Build.props` for `git archive`, and `Forge.Dev.props` is `export-ignore`d, so neither these tests nor gdUnit4 reach an installed copy of the plugin.

## Running them

Create `.runsettings` (gitignored, it pins your own Godot binary) by copying `.runsettings.ci` and adding:

```xml
<RunConfiguration>
  <EnvironmentVariables>
    <GODOT_BIN>C:\path\to\Godot_v4.7-stable_mono_win64_console.exe</GODOT_BIN>
  </EnvironmentVariables>
</RunConfiguration>
```

Then:

```bash
dotnet test "Forge Godot.csproj" --settings .runsettings
```

The first run takes ~30s longer while gdUnit4 installs its runner into `gdunit4_testadapter_v5/` and rebuilds the Godot project.

The editor tests are a separate command, because they need a real editor session rather than gdUnit4's runtime process:

```powershell
& "C:\path\to\Godot_v4.7-stable_mono_win64_console.exe" --path . --editor --headless --forge-editor-tests
```

PowerShell needs the leading `&` to run a quoted path as a command; from a POSIX shell, drop it. Use the `_console.exe` build on Windows - the plain one does not attach stdout. The run exits 0 or 1 and writes a JUnit report to `TestResults/editor-tests.xml`.

### In Visual Studio or Rider

**The configuration must be `Debug`.** The test packages and `tests/**` are Debug-only, so in `ExportDebug` or `ExportRelease` there is no test adapter and Test Explorer is silently empty rather than reporting an error.

Point the IDE at `.runsettings` as well, or every test fails to connect for want of `GODOT_BIN`:

- Visual Studio: Test > Configure Run Settings > Select Solution Wide runsettings File.
- Rider: Settings > Build, Execution, Deployment > Unit Testing > VSTest > run settings file.

## Two kinds of test

Tests run in a plain .NET process by default. `[RequireGodotRuntime]` runs them inside a real Godot process instead, which is required for anything touching a Godot type - every `Resource`, `Node`, or `res://` load.

Editor context is **not** available under `[RequireGodotRuntime]`: `EditorInterface.Singleton` is null and `EditorUndoRedoManager` cannot be instantiated. Tests that drive the editor docks need a separate `--editor` harness.

## Conventions

Same as the core Forge suite: `// Copyright © Gamesmiths Guild.` header, tabs, file-scoped namespaces, `<Subject>Tests` class names, `Snake_case_method_names`, and FluentAssertions rather than gdUnit4's own assertions.

Suites that assert a property across a whole family of types use `[DataPoint]` over a reflection-driven source, so adding a resolver or node automatically adds its test cases.
