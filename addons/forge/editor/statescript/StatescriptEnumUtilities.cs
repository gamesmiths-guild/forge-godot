// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Shared lookup and formatting helpers for <see cref="ForgeStatescriptEnum"/> assets in the graph editor, so an enum
/// is picked and a member reads the same way everywhere they are shown (port labels, dropdowns, folded summaries).
/// </summary>
internal static class StatescriptEnumUtilities
{
	private static List<string>? _cachedEnumPaths;

	/// <summary>
	/// Gets the project paths of every <see cref="ForgeStatescriptEnum"/> asset, sorted by path. Results are cached
	/// until <see cref="InvalidateCache"/> is called, since the scan runs every time a node using an enum is drawn.
	/// </summary>
	/// <returns>The enum asset paths.</returns>
	public static IReadOnlyList<string> FindAllEnumPaths()
	{
		_cachedEnumPaths ??= ScanForEnums();
		return _cachedEnumPaths;
	}

	/// <summary>
	/// Clears the cached asset scan, so an enum added, removed, or moved on disk is picked up.
	/// </summary>
	public static void InvalidateCache()
	{
		_cachedEnumPaths = null;
	}

	/// <summary>
	/// Loads an enum asset by path.
	/// </summary>
	/// <param name="path">The resource path.</param>
	/// <returns>The enum, or <see langword="null"/> when the path is empty or does not hold one.</returns>
	public static ForgeStatescriptEnum? Load(string path)
	{
		return string.IsNullOrEmpty(path) ? null : ResourceLoader.Load(path) as ForgeStatescriptEnum;
	}

	/// <summary>
	/// Fills a dropdown with <c>(None)</c> followed by every enum asset in the project, selecting the given one. Each
	/// item carries its resource path as metadata; read the selection back with <see cref="GetSelectedEnum"/>.
	/// </summary>
	/// <param name="dropdown">The dropdown to populate.</param>
	/// <param name="selected">The enum currently bound, if any.</param>
	public static void PopulateEnumDropdown(OptionButton dropdown, ForgeStatescriptEnum? selected)
	{
		dropdown.Clear();
		dropdown.AddItem("(None)");
		dropdown.SetItemMetadata(0, string.Empty);

		IReadOnlyList<string> paths = FindAllEnumPaths();
		string selectedPath = selected?.ResourcePath ?? string.Empty;
		int selectedIndex = 0;

		for (int i = 0; i < paths.Count; i++)
		{
			dropdown.AddItem(GetPathDisplayName(paths[i]));
			dropdown.SetItemMetadata(dropdown.ItemCount - 1, paths[i]);

			if (paths[i] == selectedPath)
			{
				selectedIndex = dropdown.ItemCount - 1;
			}
		}

		// An enum saved inside another resource, or one whose file has since moved, has no entry of its own. List it
		// anyway so a node it is still driving does not read as unbound.
		if (selected is not null && selectedIndex == 0)
		{
			string displayName = selected.GetDisplayName();
			dropdown.AddItem(displayName.Length == 0 ? "(Embedded enum)" : displayName);
			dropdown.SetItemMetadata(dropdown.ItemCount - 1, selectedPath);
			selectedIndex = dropdown.ItemCount - 1;
		}

		dropdown.Selected = selectedIndex;
	}

	/// <summary>
	/// Loads the enum a dropdown populated by <see cref="PopulateEnumDropdown"/> currently points at, from the resource
	/// path stored on the selected item.
	/// </summary>
	/// <param name="dropdown">The dropdown to read.</param>
	/// <param name="index">The selected item index.</param>
	/// <returns>
	/// The selected enum; <see langword="null"/> for <c>(None)</c>, and also when the stored path no longer holds an
	/// enum, which callers distinguish by <paramref name="index"/>.
	/// </returns>
	public static ForgeStatescriptEnum? GetSelectedEnum(OptionButton dropdown, int index)
	{
		if (index <= 0 || index >= dropdown.ItemCount)
		{
			return null;
		}

		return Load(dropdown.GetItemMetadata(index).AsString());
	}

	/// <summary>
	/// Formats a member for display as <c>Name (value)</c>, making the ordinal the graph actually stores visible next to
	/// the name.
	/// </summary>
	/// <param name="enumDefinition">The enum the member belongs to.</param>
	/// <param name="value">The member's value.</param>
	/// <returns>The formatted member, or the bare value when the enum has no member for it.</returns>
	public static string FormatMemberName(ForgeStatescriptEnum? enumDefinition, int value)
	{
		string memberName = GetMemberName(enumDefinition, value);

		return memberName.Length == 0
			? value.ToString(CultureInfo.InvariantCulture)
			: $"{memberName} ({value.ToString(CultureInfo.InvariantCulture)})";
	}

	/// <summary>
	/// Formats a value for a compact summary: the member name alone when there is one, the bare number otherwise.
	/// </summary>
	/// <param name="enumDefinition">The enum the member belongs to.</param>
	/// <param name="value">The member's value.</param>
	/// <returns>The formatted value.</returns>
	public static string FormatValue(ForgeStatescriptEnum? enumDefinition, int value)
	{
		string memberName = GetMemberName(enumDefinition, value);

		return memberName.Length == 0 ? value.ToString(CultureInfo.InvariantCulture) : memberName;
	}

	/// <summary>
	/// Gets a member's name, treating an unnamed or blank member as no name at all.
	/// </summary>
	/// <param name="enumDefinition">The enum the member belongs to.</param>
	/// <param name="value">The member's value.</param>
	/// <returns>The member name, or an empty string when there is none.</returns>
	public static string GetMemberName(ForgeStatescriptEnum? enumDefinition, int value)
	{
		string? memberName = enumDefinition?.GetMemberName(value);

		return string.IsNullOrWhiteSpace(memberName) ? string.Empty : memberName;
	}

	/// <summary>
	/// Names an enum for a dropdown entry: its <see cref="ForgeStatescriptEnum.EnumName"/> when set, otherwise its file
	/// name.
	/// </summary>
	/// <param name="path">The enum's resource path.</param>
	/// <returns>The display name.</returns>
	private static string GetPathDisplayName(string path)
	{
		string displayName = Load(path)?.GetDisplayName() ?? string.Empty;

		return displayName.Length == 0 ? path.GetFile().GetBaseName() : displayName;
	}

	private static List<string> ScanForEnums()
	{
		var results = new List<string>();
		ScanDirectory(EditorInterface.Singleton.GetResourceFilesystem().GetFilesystem(), results);
		results.Sort(StringComparer.OrdinalIgnoreCase);
		return results;
	}

	private static void ScanDirectory(EditorFileSystemDirectory directory, List<string> results)
	{
		for (int i = 0; i < directory.GetFileCount(); i++)
		{
			string path = directory.GetFilePath(i);

			if (!path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
				&& !path.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// The recorded type and script class both come from the resource header, so a script-backed global class
			// is recognized without opening the file. Loading every candidate here crashed the editor whenever this
			// ran during layout restore, while the C# script instances were still being bound.
			if (directory.GetFileScriptClassName(i) != nameof(ForgeStatescriptEnum)
				&& directory.GetFileType(i) != nameof(ForgeStatescriptEnum))
			{
				continue;
			}

			results.Add(path);
		}

		for (int i = 0; i < directory.GetSubdirCount(); i++)
		{
			ScanDirectory(directory.GetSubdir(i), results);
		}
	}
}
#endif
