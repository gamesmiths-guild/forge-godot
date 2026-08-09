// Copyright © Gamesmiths Guild.

using System;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// Project settings describing where Forge reads its gameplay tags from.
/// </summary>
/// <remarks>
/// Tags come from an ordered list of tag source resources, merged into a single registry. The order is a display
/// concern only - the merged set is the same whatever the order - but it is preserved so the Tags dock can group tags
/// the way the project organizes them.
/// </remarks>
public static class ForgeSettings
{
	/// <summary>
	/// Setting holding the ordered list of tag source resources this project reads from.
	/// </summary>
	public const string SourcesSetting = "forge/tags/sources";

	/// <summary>
	/// Where the first tag source is created when a project does not have one yet.
	/// </summary>
	public const string DefaultSourcePath = "res://forge_tags.tres";

	/// <summary>
	/// Gets the configured tag source references, each a <c>uid://</c> or <c>res://</c> string.
	/// </summary>
	/// <returns>The configured references, or an empty array if unset.</returns>
	public static string[] GetSourceReferences()
	{
		return ProjectSettings.GetSetting(SourcesSetting, Array.Empty<string>()).AsStringArray();
	}

	/// <summary>
	/// Replaces the tag source references and persists them to project.godot.
	/// </summary>
	/// <param name="references">The references to store, in display order.</param>
	public static void SetSourceReferences(string[] references)
	{
		ProjectSettings.SetSetting(SourcesSetting, references);
		ProjectSettings.Save();
	}

	/// <summary>
	/// Turns a stored reference into a loadable resource path, resolving <c>uid://</c> references through the UID cache
	/// so that moving the file in the editor does not break the setting.
	/// </summary>
	/// <param name="reference">A <c>uid://</c> or <c>res://</c> reference.</param>
	/// <returns>A resource path, or <paramref name="reference"/> unchanged if it cannot be resolved.</returns>
	/// <remarks>
	/// A dangling UID is returned as-is rather than resolved to an empty string, so a source whose file is temporarily
	/// missing still shows up - with its reference intact - instead of silently disappearing.
	/// </remarks>
	public static string ResolveReference(string reference)
	{
		if (!reference.StartsWith("uid://", StringComparison.Ordinal))
		{
			return reference;
		}

		long id = ResourceUid.TextToId(reference);

		return ResourceUid.HasId(id) ? ResourceUid.GetIdPath(id) : reference;
	}

	/// <summary>
	/// Prefers a <c>uid://</c> reference for <paramref name="resourcePath"/>, so the setting survives the file being
	/// moved or renamed in the editor.
	/// </summary>
	/// <param name="resourcePath">The resource path to describe.</param>
	/// <returns>A <c>uid://</c> reference when the resource has a UID, otherwise the path itself.</returns>
	public static string ToPreferredReference(string resourcePath)
	{
		long id = ResourceLoader.GetResourceUid(resourcePath);

		return id == ResourceUid.InvalidId ? resourcePath : ResourceUid.IdToText(id);
	}

#if TOOLS
	/// <summary>
	/// Declares the setting so it shows up in Project Settings with a sensible editing widget.
	/// </summary>
	/// <remarks>
	/// The initial value is deliberately an empty array and no value is written here. ProjectSettings skips saving any
	/// property whose value still equals its initial value, so declaring a non-empty default would keep the setting out
	/// of project.godot entirely until it happened to diverge.
	/// </remarks>
	public static void EnsureRegistered()
	{
		bool declared = ProjectSettings.HasSetting(SourcesSetting);

		if (!declared)
		{
			ProjectSettings.SetSetting(SourcesSetting, Array.Empty<string>());
		}

		ProjectSettings.AddPropertyInfo(new GodotDictionary
		{
			{ "name", SourcesSetting },
			{ "type", (int)Variant.Type.PackedStringArray },
			{ "hint", (int)PropertyHint.TypeString },
			{ "hint_string", $"{(int)Variant.Type.String}/{(int)PropertyHint.File}:*.tres" },
		});

		ProjectSettings.SetInitialValue(SourcesSetting, Array.Empty<string>());
		ProjectSettings.SetAsBasic(SourcesSetting, true);

		// Property info and initial values are editor-session state, not file content, so only an actual first-time
		// declaration is worth rewriting project.godot for.
		if (!declared)
		{
			ProjectSettings.Save();
		}
	}
#endif
}
