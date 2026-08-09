// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Editor.Tags;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Reads gameplay tags out of a scene file as text, without loading the scene.
/// </summary>
/// <remarks>
/// <para>
/// Tags are plain text in a <c>.tscn</c>, so they can be read - and rewritten - directly. That sidesteps the engine
/// entirely: nothing is loaded, nothing is instantiated, and no scene can misbehave. Binary <c>.scn</c> files are not
/// text and are reported as skipped instead.
/// </para>
/// </remarks>
internal static class SceneTagParser
{
	/// <summary>
	/// Finds every tag-bearing property written into a scene file.
	/// </summary>
	/// <param name="lines">The scene file's lines.</param>
	/// <param name="tagScript">The <c>ForgeTag</c> script identity.</param>
	/// <param name="containerScript">The <c>ForgeTagContainer</c> script identity.</param>
	/// <returns>The tag properties found, in file order.</returns>
	public static List<SceneTagReference> Parse(
		IReadOnlyList<string> lines,
		ScriptIdentity tagScript,
		ScriptIdentity containerScript)
	{
		HashSet<string> tagScriptIds = CollectScriptIds(lines, tagScript);
		HashSet<string> containerScriptIds = CollectScriptIds(lines, containerScript);

		var references = new List<SceneTagReference>();
		var subResourceOwners = new Dictionary<string, string>(StringComparer.Ordinal);

		string currentSection = string.Empty;
		string currentSubResourceId = string.Empty;
		string currentNodeName = string.Empty;
		bool currentIsTagScript = false;
		bool currentIsContainerScript = false;

		for (int i = 0; i < lines.Count; i++)
		{
			string line = lines[i];

			if (line.StartsWith('['))
			{
				currentSection = SectionName(line);
				currentSubResourceId = currentSection == "sub_resource" ? ExtractQuoted(line, "id") : string.Empty;
				currentNodeName = currentSection == "node" ? ExtractQuoted(line, "name") : string.Empty;
				currentIsTagScript = false;
				currentIsContainerScript = false;
				continue;
			}

			if (currentSection == "sub_resource")
			{
				ReadSubResourceLine(
					line,
					i,
					currentSubResourceId,
					tagScriptIds,
					containerScriptIds,
					ref currentIsTagScript,
					ref currentIsContainerScript,
					references);
			}
			else if (currentSection == "node" && !string.IsNullOrEmpty(currentNodeName))
			{
				string referencedId = ExtractCall(line, "SubResource");

				if (!string.IsNullOrEmpty(referencedId))
				{
					subResourceOwners[referencedId] = $"{currentNodeName}/{PropertyName(line)}";
				}
			}
		}

		return [.. references.Select(reference => WithResolvedLocation(reference, subResourceOwners))];
	}

	/// <summary>
	/// Rewrites a tag property line with a new set of tags.
	/// </summary>
	/// <param name="reference">The property to rewrite.</param>
	/// <param name="tags">The tags it should declare.</param>
	/// <returns>The replacement line.</returns>
	public static string BuildLine(SceneTagReference reference, IEnumerable<string> tags)
	{
		if (!reference.IsContainer)
		{
			return $"{reference.PropertyName} = \"{tags.FirstOrDefault() ?? string.Empty}\"";
		}

		string joined = string.Join(", ", tags.Select(tag => $"\"{tag}\""));

		return $"{reference.PropertyName} = Array[String]([{joined}])";
	}

	private static SceneTagReference WithResolvedLocation(
		SceneTagReference reference,
		Dictionary<string, string> subResourceOwners)
	{
		return subResourceOwners.TryGetValue(reference.Location, out string? owner)
			? reference with { Location = owner }
			: reference;
	}

	private static void ReadSubResourceLine(
		string line,
		int lineIndex,
		string subResourceId,
		HashSet<string> tagScriptIds,
		HashSet<string> containerScriptIds,
		ref bool isTagScript,
		ref bool isContainerScript,
		List<SceneTagReference> references)
	{
		string scriptId = ExtractCall(line, "ExtResource");

		if (!string.IsNullOrEmpty(scriptId) && line.TrimStart().StartsWith("script", StringComparison.Ordinal))
		{
			isTagScript = tagScriptIds.Contains(scriptId);
			isContainerScript = containerScriptIds.Contains(scriptId);
			return;
		}

		if (isContainerScript && line.Contains("Array[String](", StringComparison.Ordinal))
		{
			references.Add(new SceneTagReference(
				lineIndex,
				PropertyName(line),
				subResourceId,
				IsContainer: true,
				ParseStringArray(line)));

			return;
		}

		if (isTagScript && line.Contains('=', StringComparison.Ordinal) && line.Contains('"', StringComparison.Ordinal))
		{
			string value = ExtractFirstQuoted(line);

			if (!string.IsNullOrEmpty(value))
			{
				references.Add(new SceneTagReference(
					lineIndex,
					PropertyName(line),
					subResourceId,
					IsContainer: false,
					[value]));
			}
		}
	}

	private static HashSet<string> CollectScriptIds(IReadOnlyList<string> lines, ScriptIdentity script)
	{
		var ids = new HashSet<string>(StringComparer.Ordinal);

		foreach (string line in lines)
		{
			if (!line.StartsWith("[ext_resource", StringComparison.Ordinal))
			{
				continue;
			}

			string path = ExtractQuoted(line, "path");
			string uid = ExtractQuoted(line, "uid");

			bool matches = (!string.IsNullOrEmpty(script.Path)
					&& string.Equals(path, script.Path, StringComparison.OrdinalIgnoreCase))
				|| (!string.IsNullOrEmpty(script.Uid) && string.Equals(uid, script.Uid, StringComparison.Ordinal));

			if (matches)
			{
				ids.Add(ExtractQuoted(line, "id"));
			}
		}

		return ids;
	}

	private static string SectionName(string line)
	{
		int end = line.IndexOfAny([' ', ']']);

		return end > 1 ? line[1..end] : string.Empty;
	}

	private static string PropertyName(string line)
	{
		int equals = line.IndexOf('=', StringComparison.Ordinal);

		return equals > 0 ? line[..equals].Trim() : string.Empty;
	}

	private static string ExtractQuoted(string line, string key)
	{
		string needle = $"{key}=\"";
		int cursor = 0;

		while (cursor < line.Length)
		{
			int found = line.IndexOf(needle, cursor, StringComparison.Ordinal);

			if (found < 0)
			{
				return string.Empty;
			}

			// Attributes are separated by spaces, and the check matters: without it `id="` happily matches inside
			// `uid="`, which silently hands back the wrong value for every ext_resource in the file.
			if (found == 0 || line[found - 1] is ' ' or '[')
			{
				int valueStart = found + needle.Length;
				int end = line.IndexOf('"', valueStart);

				return end < 0 ? string.Empty : line[valueStart..end];
			}

			cursor = found + needle.Length;
		}

		return string.Empty;
	}

	private static string ExtractCall(string line, string function)
	{
		int start = line.IndexOf($"{function}(\"", StringComparison.Ordinal);

		if (start < 0)
		{
			return string.Empty;
		}

		start += function.Length + 2;
		int end = line.IndexOf('"', start);

		return end < 0 ? string.Empty : line[start..end];
	}

	private static string ExtractFirstQuoted(string line)
	{
		int start = line.IndexOf('"', StringComparison.Ordinal);

		if (start < 0)
		{
			return string.Empty;
		}

		int end = line.IndexOf('"', start + 1);

		return end < 0 ? string.Empty : line[(start + 1)..end];
	}

	private static string[] ParseStringArray(string line)
	{
		var tags = new List<string>();
		int cursor = line.IndexOf("Array[String](", StringComparison.Ordinal);

		while (cursor >= 0)
		{
			int start = line.IndexOf('"', cursor);

			if (start < 0)
			{
				break;
			}

			int end = line.IndexOf('"', start + 1);

			if (end < 0)
			{
				break;
			}

			tags.Add(line[(start + 1)..end]);
			cursor = end + 1;
		}

		return [.. tags];
	}
}
#endif
