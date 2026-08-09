// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Editor.Tags;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Reads gameplay tags out of a scene or resource file as text, without loading it.
/// </summary>
/// <remarks>
/// <para>
/// Loading assets to inspect them is not safe in Godot 4.7. Reading the properties of loaded resources leaves the
/// editor in a state where later filesystem work - reimporting a file the repair just wrote, for instance - segfaults
/// it. Instantiating scenes is worse, and so is loading one that uses a <c>ViewportTexture</c>, which cannot resolve
/// its viewport outside a running tree.
/// </para>
/// <para>
/// Tags are plain text in a <c>.tscn</c> or <c>.tres</c>, so they are read - and rewritten - directly. Nothing is
/// loaded, nothing is instantiated, and no asset can misbehave. Binary <c>.scn</c> and <c>.res</c> files are not text
/// and are reported as skipped instead.
/// </para>
/// </remarks>
internal static class AssetTagParser
{
	private const string ContainerProperty = "ContainerTags";
	private const string TagProperty = "Tag";
	private const string TypedArrayPrefix = "Array[String](";
	private const int MaxOwnerDepth = 16;

	/// <summary>
	/// Finds every tag-bearing property written into an asset file.
	/// </summary>
	/// <param name="lines">The file's lines.</param>
	/// <param name="tagScript">The <c>ForgeTag</c> script identity.</param>
	/// <param name="containerScript">The <c>ForgeTagContainer</c> script identity.</param>
	/// <returns>The tag properties found, in file order.</returns>
	public static List<AssetTagReference> Parse(
		IReadOnlyList<string> lines,
		ScriptIdentity tagScript,
		ScriptIdentity containerScript)
	{
		HashSet<string> tagScriptIds = CollectScriptIds(lines, tagScript);
		HashSet<string> containerScriptIds = CollectScriptIds(lines, containerScript);

		var references = new List<AssetTagReference>();
		var owners = new Dictionary<string, string>(StringComparer.Ordinal);

		string section = string.Empty;
		string sectionId = string.Empty;
		string ownerName = string.Empty;
		bool isTagScript = false;
		bool isContainerScript = false;

		for (int i = 0; i < lines.Count; i++)
		{
			string line = lines[i];

			if (line.StartsWith('['))
			{
				section = SectionName(line);

				// A scene names its owners by node; a resource file has a single unnamed [resource] section.
				sectionId = section == "sub_resource" ? ExtractQuoted(line, "id") : string.Empty;
				ownerName = ResolveOwnerName(section, sectionId, line);
				isTagScript = false;
				isContainerScript = false;
				continue;
			}

			if (section is "sub_resource" or "resource")
			{
				ReadTagLine(
					line,
					i,
					sectionId,
					tagScriptIds,
					containerScriptIds,
					ref isTagScript,
					ref isContainerScript,
					references);
			}

			if (string.IsNullOrEmpty(ownerName))
			{
				continue;
			}

			string referencedId = ExtractCall(line, "SubResource");

			if (!string.IsNullOrEmpty(referencedId))
			{
				owners[referencedId] = $"{ownerName}/{PropertyName(line)}";
			}
		}

		return [.. references.Select(reference => WithResolvedLocation(reference, owners))];
	}

	/// <summary>
	/// Rewrites a tag property line with a new set of tags, in the same form the file already used.
	/// </summary>
	/// <param name="reference">The property to rewrite.</param>
	/// <param name="tags">The tags it should declare.</param>
	/// <returns>The replacement line.</returns>
	public static string BuildLine(AssetTagReference reference, IEnumerable<string> tags)
	{
		if (!reference.IsContainer)
		{
			return $"{reference.PropertyName} = \"{tags.FirstOrDefault() ?? string.Empty}\"";
		}

		string joined = string.Join(", ", tags.Select(tag => $"\"{tag}\""));

		return reference.UsesTypedArray
			? $"{reference.PropertyName} = {TypedArrayPrefix}[{joined}])"
			: $"{reference.PropertyName} = [{joined}]";
	}

	private static AssetTagReference WithResolvedLocation(
		AssetTagReference reference,
		Dictionary<string, string> owners)
	{
		return reference with { Location = ResolveLocation(reference.Location, owners) };
	}

	private static void ReadTagLine(
		string line,
		int lineIndex,
		string sectionId,
		HashSet<string> tagScriptIds,
		HashSet<string> containerScriptIds,
		ref bool isTagScript,
		ref bool isContainerScript,
		List<AssetTagReference> references)
	{
		string propertyName = PropertyName(line);

		if (propertyName == "script")
		{
			string scriptId = ExtractCall(line, "ExtResource");

			isTagScript = tagScriptIds.Contains(scriptId);
			isContainerScript = containerScriptIds.Contains(scriptId);

			return;
		}

		if (isContainerScript && propertyName == ContainerProperty)
		{
			references.Add(new AssetTagReference(
				lineIndex,
				propertyName,
				string.IsNullOrEmpty(sectionId) ? "resource" : sectionId,
				IsContainer: true,
				line.Contains(TypedArrayPrefix, StringComparison.Ordinal),
				ParseStringList(line)));

			return;
		}

		if (isTagScript && propertyName == TagProperty)
		{
			string value = ExtractFirstQuoted(line);

			if (!string.IsNullOrEmpty(value))
			{
				references.Add(new AssetTagReference(
					lineIndex,
					propertyName,
					string.IsNullOrEmpty(sectionId) ? "resource" : sectionId,
					IsContainer: false,
					UsesTypedArray: false,
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

			bool matches = (!string.IsNullOrEmpty(script.Path)
					&& string.Equals(ExtractQuoted(line, "path"), script.Path, StringComparison.OrdinalIgnoreCase))
				|| (!string.IsNullOrEmpty(script.Uid)
					&& string.Equals(ExtractQuoted(line, "uid"), script.Uid, StringComparison.Ordinal));

			if (matches)
			{
				ids.Add(ExtractQuoted(line, "id"));
			}
		}

		return ids;
	}

	private static string ResolveOwnerName(string section, string sectionId, string line)
	{
		if (section == "node")
		{
			return ExtractQuoted(line, "name");
		}

		// A sub-resource owns whatever it references, and is named by its id so the chain can be walked back to a
		// node - or to the file's own [resource] - when a location is reported.
		return section == "resource" ? "resource" : sectionId;
	}

	/// <summary>
	/// Turns a sub-resource id into a readable path, by following the chain of owners back to a node.
	/// </summary>
	/// <param name="id">The sub-resource id the tag was found in.</param>
	/// <param name="owners">The owner map gathered while parsing.</param>
	/// <returns>A path such as <c>Forge Entity/BaseTags</c> or <c>resource/Components/TagsToAdd</c>.</returns>
	private static string ResolveLocation(string id, Dictionary<string, string> owners)
	{
		string tail = string.Empty;
		string current = id;

		// Guarded rather than trusted: a hand-edited file could reference itself in a loop.
		for (int depth = 0; depth < MaxOwnerDepth && owners.TryGetValue(current, out string? owner); depth++)
		{
			int separator = owner.IndexOf('/', StringComparison.Ordinal);
			string property = separator < 0 ? string.Empty : owner[(separator + 1)..];

			tail = string.IsNullOrEmpty(tail) ? property : $"{property}/{tail}";
			current = separator < 0 ? owner : owner[..separator];
		}

		return string.IsNullOrEmpty(tail) ? current : $"{current}/{tail}";
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

	private static string[] ParseStringList(string line)
	{
		var tags = new List<string>();
		int cursor = line.IndexOf('[', StringComparison.Ordinal);

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
