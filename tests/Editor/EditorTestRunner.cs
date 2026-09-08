// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Runs the editor-context test suite inside a headless editor and quits with a non-zero code on failure.
/// </summary>
/// <remarks>
/// <para>
/// These tests cannot run under gdUnit4. It launches a Godot <em>runtime</em> process, where
/// <c>EditorInterface.Singleton</c> is null and <c>EditorUndoRedoManager</c> cannot be instantiated - verified, not
/// assumed. Anything driving the editor docks therefore needs a real <c>--editor</c> session, which is what this is.
/// </para>
/// <para>
/// Launch with <c>godot --path . --editor --headless --forge-editor-tests</c>.
/// </para>
/// </remarks>
internal static class EditorTestRunner
{
	/// <summary>
	/// The command-line flag that opts a headless editor run into the suite.
	/// </summary>
	public const string CommandLineFlag = "--forge-editor-tests";

	private const string ReportPath = "res://TestResults/editor-tests.xml";

	/// <summary>
	/// Reports whether this editor run was asked to execute the suite.
	/// </summary>
	/// <returns><see langword="true"/> when the opt-in flag is present.</returns>
	public static bool ShouldRun()
	{
		return OS.GetCmdlineArgs().Contains(CommandLineFlag);
	}

	/// <summary>
	/// Discovers and runs every editor test, writes a JUnit report, and quits the editor.
	/// </summary>
	/// <param name="dock">The live Statescript dock to drive.</param>
	/// <param name="undoRedo">The editor's undo/redo manager.</param>
	public static void RunAndQuit(StatescriptGraphEditorDock dock, EditorUndoRedoManager? undoRedo)
	{
		GD.Print("=== Forge editor tests ===");

		if (undoRedo is null)
		{
			GD.PrintErr("FATAL: no EditorUndoRedoManager available; nothing can be tested.");
			Quit(1);
			return;
		}

		// Everything from here runs under a finally that quits. A headless editor that never quits does not fail the
		// run, it hangs it, and CI would sit there until the runner's own timeout with nothing useful in the log.
		int exitCode = 1;

		try
		{
			List<EditorTestResult> results = [.. Discover().Select(test => Run(test, dock, undoRedo))];
			int failed = results.Count(result => result.Failure is not null);

			foreach (EditorTestResult result in results.Where(result => result.Failure is not null))
			{
				GD.PrintErr($"FAIL {result.Name}: {result.Failure}");
			}

			GD.Print($"--- {results.Count} tests, {failed} failed ---");
			GD.Print(failed == 0 ? "RESULT: PASS" : "RESULT: FAIL");

			WriteReport(results);
			exitCode = failed == 0 ? 0 : 1;
		}
#pragma warning disable CA1031 // Discovery or reporting failing is a run failure, not a reason to hang the editor.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			GD.PrintErr($"FATAL: the editor test run did not complete: {ex}");
			GD.Print("RESULT: FAIL");
		}
		finally
		{
			Quit(exitCode);
		}
	}

	/// <summary>
	/// Finds every method marked <see cref="EditorTestAttribute"/>, so adding a test needs no registration.
	/// </summary>
	private static IEnumerable<MethodInfo> Discover()
	{
		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
			.Where(method => method.GetCustomAttribute<EditorTestAttribute>() is not null)
			.OrderBy(method => method.DeclaringType!.Name, StringComparer.Ordinal)
			.ThenBy(method => method.Name, StringComparer.Ordinal);
	}

	private static EditorTestResult Run(
		MethodInfo test,
		StatescriptGraphEditorDock dock,
		EditorUndoRedoManager undoRedo)
	{
		string name = $"{test.DeclaringType!.Name}.{test.Name}";
		var stopwatch = Stopwatch.StartNew();

		try
		{
			test.Invoke(null, [new EditorTestContext(dock, undoRedo)]);
			return new EditorTestResult(test.DeclaringType!.FullName!, name, stopwatch.Elapsed, null);
		}
#pragma warning disable CA1031 // A failing test is reported, never allowed to take the runner down with it.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			// Reflection wraps whatever the test threw, and the wrapper carries none of the useful detail.
			Exception failure = ex is TargetInvocationException { InnerException: not null } wrapper
				? wrapper.InnerException!
				: ex;

			return new EditorTestResult(test.DeclaringType!.FullName!, name, stopwatch.Elapsed, failure);
		}
	}

	private static void WriteReport(IReadOnlyList<EditorTestResult> results)
	{
		string reportPath = ProjectSettings.GlobalizePath(ReportPath);
		Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);

		var settings = new XmlWriterSettings { Indent = true };
		using var writer = XmlWriter.Create(reportPath, settings);

		writer.WriteStartElement("testsuites");
		writer.WriteStartElement("testsuite");
		writer.WriteAttributeString("name", "ForgeEditorTests");
		writer.WriteAttributeString("tests", results.Count.ToString(CultureInfo.InvariantCulture));
		writer.WriteAttributeString(
			"failures",
			results.Count(result => result.Failure is not null).ToString(CultureInfo.InvariantCulture));

		foreach (EditorTestResult result in results)
		{
			writer.WriteStartElement("testcase");
			writer.WriteAttributeString("classname", result.ClassName);
			writer.WriteAttributeString("name", result.Name);
			writer.WriteAttributeString(
				"time",
				result.Duration.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture));

			if (result.Failure is not null)
			{
				writer.WriteStartElement("failure");
				writer.WriteAttributeString("message", result.Failure.Message);
				writer.WriteString(result.Failure.ToString());
				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		}

		writer.WriteEndElement();
		writer.WriteEndElement();

		GD.Print($"Report written to {ReportPath}");
	}

	private static void Quit(int exitCode)
	{
		EditorInterface.Singleton.GetBaseControl().GetTree().Quit(exitCode);
	}

	private sealed record EditorTestResult(
		string ClassName,
		string Name,
		TimeSpan Duration,
		Exception? Failure);
}
#endif
