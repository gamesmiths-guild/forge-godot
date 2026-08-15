// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Resources.Attributes;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

/// <summary>
/// Turns <see cref="ForgeAttributeSetDefinition"/> resources into C# attribute set classes.
/// </summary>
/// <remarks>
/// Generation runs inside the editor rather than as a Roslyn source generator, so it can load the definitions as
/// resources and read typed properties instead of parsing the <c>.tres</c> text, and so the output is a real file the
/// user can read. The generated classes are ordinary attribute sets: every editor and resolver that reflects over
/// <c>AttributeSet</c> subclasses picks them up without knowing they were generated.
/// </remarks>
internal static class AttributeSetCodeGenerator
{
	/// <summary>The folder the generator owns. Everything in it is rewritten or removed on each run.</summary>
	public const string GeneratedFolder = "res://forge_generated/attribute_sets";

	private const string GeneratedNamespace = "Gamesmiths.Forge.Godot.Generated";
	private const string GeneratedSuffix = ".generated.cs";

	/// <summary>
	/// Regenerates every definition in the project, replacing the contents of <see cref="GeneratedFolder"/>.
	/// </summary>
	/// <returns>A description of what happened, for reporting back to the user.</returns>
	public static AttributeSetGenerationReport RegenerateAll()
	{
		var report = new AttributeSetGenerationReport();
		List<ForgeAttributeSetDefinition> definitions = LoadDefinitions();

		if (!EnsureFolderExists())
		{
			report.Errors.Add($"Could not create {GeneratedFolder}.");
			return report;
		}

		var writtenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (ForgeAttributeSetDefinition definition in definitions)
		{
			string[] errors = Validate(definition, usedNames);

			if (errors.Length > 0)
			{
				report.Errors.AddRange(errors);
				continue;
			}

			usedNames.Add(definition.AttributeSetName);

			string fileName = definition.AttributeSetName + GeneratedSuffix;

			if (WriteFile($"{GeneratedFolder}/{fileName}", BuildSource(definition)))
			{
				writtenFiles.Add(fileName);
				report.GeneratedSets.Add(definition.AttributeSetName);
			}
			else
			{
				report.Errors.Add($"Could not write {fileName}.");
			}
		}

		report.RemovedFiles.AddRange(RemoveOrphans(writtenFiles));

		// Godot only compiles what it has indexed, so the files written here have to be picked up before the user
		// builds. Each one is announced individually rather than through a full Scan(), which reloads resources across
		// the project and drops whatever the inspector is currently editing - including the definition just saved.
		EditorFileSystem fileSystem = EditorInterface.Singleton.GetResourceFilesystem();

		foreach (string fileName in writtenFiles.Concat(report.RemovedFiles))
		{
			// update_file handles removals too: a path that no longer exists is dropped from the index.
			fileSystem.UpdateFile($"{GeneratedFolder}/{fileName}");
		}

		return report;
	}

	/// <summary>
	/// Loads every attribute set definition in the project.
	/// </summary>
	/// <returns>The definitions, in filesystem order.</returns>
	public static List<ForgeAttributeSetDefinition> LoadDefinitions()
	{
		var definitions = new List<ForgeAttributeSetDefinition>();

		foreach (string path in ProjectFileIndex.CollectResourcesByScriptClass(nameof(ForgeAttributeSetDefinition)))
		{
			if (ResourceLoader.Load(path) is ForgeAttributeSetDefinition definition)
			{
				definitions.Add(definition);
			}
		}

		return definitions;
	}

	/// <summary>
	/// Checks a definition for the problems that would produce code that does not compile, or a set that collides with
	/// another one.
	/// </summary>
	/// <param name="definition">The definition to check.</param>
	/// <param name="takenNames">Set names already claimed by other definitions in this run. May be null.</param>
	/// <returns>The problems found, empty when the definition is usable.</returns>
	public static string[] Validate(ForgeAttributeSetDefinition definition, HashSet<string>? takenNames = null)
	{
		var errors = new List<string>();

		if (!IsValidIdentifier(definition.AttributeSetName))
		{
			errors.Add(DescribeInvalidName("set name", definition.AttributeSetName));

			// Every later message would just repeat this one, since they all name the set.
			return [.. errors];
		}

		if (takenNames?.Contains(definition.AttributeSetName) == true)
		{
			errors.Add($"More than one definition is named \"{definition.AttributeSetName}\".");
		}

		if (CollidesWithCodeDefinedSet(definition.AttributeSetName))
		{
			errors.Add($"A C# class named \"{definition.AttributeSetName}\" already extends AttributeSet. Attribute "
				+ "keys are prefixed with the set name, so two sets cannot share one. Rename this definition, or "
				+ "delete the class if this definition is meant to replace it.");
		}

		var attributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (ForgeAttributeDefinition? attribute in definition.Attributes)
		{
			if (attribute is null)
			{
				errors.Add($"{definition.AttributeSetName} has an empty attribute slot.");
				continue;
			}

			if (!IsValidIdentifier(attribute.AttributeName))
			{
				errors.Add($"{definition.AttributeSetName}: "
					+ DescribeInvalidName("attribute name", attribute.AttributeName));
				continue;
			}

			if (!attributeNames.Add(attribute.AttributeName))
			{
				errors.Add($"{definition.AttributeSetName}: \"{attribute.AttributeName}\" is declared more than once.");
			}

			if (attribute.MinValue > attribute.MaxValue)
			{
				errors.Add($"{definition.AttributeSetName}.{attribute.AttributeName}: minimum is above maximum.");
			}
		}

		if (attributeNames.Count == 0 && errors.Count == 0)
		{
			errors.Add($"{definition.AttributeSetName} has no attributes.");
		}

		return [.. errors];
	}

	/// <summary>
	/// Checks whether a definition's generated class is present in the loaded assemblies and up to date, which is what
	/// tells the user whether a build is still pending.
	/// </summary>
	/// <param name="definition">The definition to check.</param>
	/// <returns>Whether the compiled set matches the definition.</returns>
	public static bool IsCompiledAndCurrent(ForgeAttributeSetDefinition definition)
	{
		if (!IsValidIdentifier(definition.AttributeSetName))
		{
			return false;
		}

		// Compiled-only on purpose: the general lookup falls back to definitions, which would make every definition
		// report itself as built.
		string[] compiled = EditorUtils.GetCompiledAttributeOptions(definition.AttributeSetName);

		if (compiled.Length == 0)
		{
			return false;
		}

		HashSet<string> compiledNames = new(compiled, StringComparer.OrdinalIgnoreCase);

		HashSet<string> defined = new(
			definition.Attributes
				.Where(attribute => attribute is not null)
				.Select(attribute => attribute.AttributeName),
			StringComparer.OrdinalIgnoreCase);

		return compiledNames.SetEquals(defined);
	}

	/// <summary>
	/// Rewrites a name into something usable as a C# identifier, for offering back to the user as a suggestion.
	/// </summary>
	/// <remarks>
	/// The suggestion is never applied automatically. An attribute's name becomes part of the key that effects, cues
	/// and graphs store, so silently rewriting it would silently repoint every reference to a key the user never typed.
	/// Showing the fix and letting them accept it keeps that decision theirs, which is the same reason the attribute
	/// pickers surface a broken reference instead of quietly replacing it.
	/// </remarks>
	/// <param name="name">The name to rewrite.</param>
	/// <returns>A valid identifier, or an empty string when nothing usable is left.</returns>
	public static string SuggestIdentifier(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}

		var builder = new StringBuilder();

		foreach (char character in name.Trim())
		{
			builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
		}

		string suggestion = builder.ToString();

		// A leading digit is legal in the middle of an identifier but not at the front, so it is kept rather than
		// dropped: losing the "2" from "2Handed" changes the name more than prefixing it does.
		return char.IsDigit(suggestion[0]) ? "_" + suggestion : suggestion;
	}

	/// <summary>
	/// Checks whether a name can be used as a C# identifier.
	/// </summary>
	/// <param name="name">The name to check.</param>
	/// <returns>Whether the name is usable.</returns>
	public static bool IsValidIdentifier(string? name)
	{
		if (string.IsNullOrEmpty(name) || (!char.IsLetter(name[0]) && name[0] != '_'))
		{
			return false;
		}

		return name.All(character => char.IsLetterOrDigit(character) || character == '_');
	}

	/// <summary>
	/// Builds the C# source for a definition.
	/// </summary>
	/// <param name="definition">The definition to render. Assumed to have passed <see cref="Validate"/>.</param>
	/// <returns>The file contents.</returns>
	public static string BuildSource(ForgeAttributeSetDefinition definition)
	{
		var lines = new List<string>
		{
			"// <auto-generated/>",
			"// Generated by Forge from an attribute set definition. Edits here are lost the next time the definition",
			"// is saved. To add logic to this set, override AttributeOnValueChanged, PreEffectExecute or",
			"// PostEffectExecute in a separate file declaring the same partial class.",
			string.Empty,
			"using Gamesmiths.Forge.Attributes;",
			string.Empty,
			$"namespace {GeneratedNamespace};",
			string.Empty,
			$"public partial class @{definition.AttributeSetName} : AttributeSet",
			"{",
		};

		foreach (ForgeAttributeDefinition attribute in definition.Attributes)
		{
			lines.Add($"\tpublic EntityAttribute @{attribute.AttributeName} {{ get; private set; }}");
			lines.Add(string.Empty);
		}

		lines.Add($"\tpublic @{definition.AttributeSetName}()");
		lines.Add("\t{");

		foreach (ForgeAttributeDefinition attribute in definition.Attributes)
		{
			// The identifiers are escaped so a name that happens to be a C# keyword still compiles. nameof strips the
			// escape, so the registered key stays the name the user typed. Numbers are formatted invariantly so the
			// generated source does not depend on the editor's locale.
			lines.Add(
				$"\t\t@{attribute.AttributeName} = InitializeAttribute("
				+ $"nameof(@{attribute.AttributeName}), "
				+ $"{Number(attribute.DefaultValue)}, "
				+ $"{Number(attribute.MinValue)}, "
				+ $"{Number(attribute.MaxValue)}, "
				+ $"{Number(attribute.Channels)}, "
				+ $"{Number(attribute.DecimalPlaces)});");
		}

		lines.Add("\t}");
		lines.Add("}");

		return string.Join("\n", lines) + "\n";
	}

	/// <summary>
	/// Checks whether a hand-written attribute set already claims a name.
	/// </summary>
	/// <remarks>
	/// Two classes of the same name in different namespaces compile perfectly well, which is why this is not caught by
	/// the build. It still breaks at runtime: <c>InitializeAttribute</c> prefixes every key with
	/// <c>GetType().Name</c>, so both sets would register keys under the same prefix, and the editor's lookups resolve
	/// a set by name and would pick whichever type reflection happened to return first.
	/// <para>
	/// Classes in the generated namespace are ignored, since those are this generator's own output.
	/// </para>
	/// </remarks>
	/// <param name="setName">The set name to check.</param>
	/// <returns>Whether a set of that name exists outside the generated namespace.</returns>
	private static bool CollidesWithCodeDefinedSet(string setName)
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(assembly => assembly.GetTypes())
			.Any(type => type.IsSubclassOf(typeof(AttributeSet))
				&& type.Name == setName
				&& type.Namespace != GeneratedNamespace);
	}

	private static string DescribeInvalidName(string kind, string? name)
	{
		string message = $"\"{name}\" is not a valid {kind}; use letters, digits and underscores, starting with a " +
			"letter or underscore.";

		string suggestion = SuggestIdentifier(name);

		return suggestion.Length > 0 && IsValidIdentifier(suggestion)
			? $"{message} Try \"{suggestion}\"."
			: message;
	}

	private static string Number(int value)
	{
		return value.ToString(CultureInfo.InvariantCulture);
	}

	private static bool EnsureFolderExists()
	{
		return DirAccess.DirExistsAbsolute(GeneratedFolder)
			|| DirAccess.MakeDirRecursiveAbsolute(GeneratedFolder) == Error.Ok;
	}

	private static bool WriteFile(string path, string contents)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

		if (file is null)
		{
			return false;
		}

		file.StoreString(contents);
		return true;
	}

	private static List<string> RemoveOrphans(HashSet<string> writtenFiles)
	{
		var removed = new List<string>();

		using var directory = DirAccess.Open(GeneratedFolder);

		if (directory is null)
		{
			return removed;
		}

		// Only files this generator produced are considered, so a hand-written class dropped into the folder survives.
		foreach (string fileName in directory.GetFiles())
		{
			if (!fileName.EndsWith(GeneratedSuffix, StringComparison.OrdinalIgnoreCase)
				|| writtenFiles.Contains(fileName))
			{
				continue;
			}

			if (directory.Remove(fileName) == Error.Ok)
			{
				removed.Add(fileName);
			}
		}

		return removed;
	}
}
#endif
