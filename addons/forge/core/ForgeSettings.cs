// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// Project settings describing where Forge reads its gameplay tags from, and how its debug drawing looks.
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
	/// Setting deciding whether a physics query outlines the entities it found, on top of the query geometry itself.
	/// </summary>
	public const string HighlightQueryTargetsSetting = "forge/statescript/highlight_query_targets";

	/// <summary>
	/// Setting listing the input actions a player's input carries each tick, in the order that gives each its slot.
	/// </summary>
	public const string InputActionsSetting = "forge/input/actions";

	/// <summary>
	/// Setting listing the analog vectors a player's input carries each tick, each as four comma-separated actions:
	/// negative x, positive x, negative y, positive y.
	/// </summary>
	public const string InputVectorsSetting = "forge/input/vectors";

	/// <summary>
	/// Gets a value indicating whether a physics query outlines the entities it found.
	/// </summary>
	/// <remarks>
	/// A developer preference rather than something a graph authors, which is why it is a project setting and not a
	/// node row: the answer is the same for every query in the project, and a per-query checkbox would be twenty
	/// settings to keep in step. It only ever narrows what Godot's own Visible Collision Shapes already turned on -
	/// with that off nothing is drawn either way - so an outline is one switch away when a crowded scene makes the
	/// query geometry hard to read, and the query's own colour still says whether it found anything.
	/// <para>The colours the drawing uses are settings too, one group per dimension under
	/// <see cref="PhysicsDebugDraw3D.ColorSettingPrefix"/> and <see cref="PhysicsDebugDraw2D.ColorSettingPrefix"/>. The
	/// drawers own them - their defaults are read where they are drawn - and only their registration lives here.</para>
	/// </remarks>
	public static bool HighlightQueryTargets =>
		ProjectSettings.GetSetting(HighlightQueryTargetsSetting, true).AsBool();

	/// <summary>
	/// Builds the input action set the project's settings describe: the actions of <see cref="InputActionsSetting"/>
	/// and the vectors of <see cref="InputVectorsSetting"/>.
	/// </summary>
	/// <returns>The set. A vector entry that does not name four actions is reported and left out.</returns>
	public static InputActionSet GetInputActionSet()
	{
		string[] actions = ProjectSettings.GetSetting(InputActionsSetting, Array.Empty<string>()).AsStringArray();
		string[] vectors = ProjectSettings.GetSetting(InputVectorsSetting, Array.Empty<string>()).AsStringArray();
		var parsedVectors = new List<InputVector>(vectors.Length);

		foreach (string vector in vectors)
		{
			string[] parts = vector.Split(',', StringSplitOptions.TrimEntries);

			if (parts.Length != 4)
			{
				GD.PushError(
					$"Forge: the input vector [{vector}] in {InputVectorsSetting} needs four comma-separated " +
					"actions: negative x, positive x, negative y, positive y. It was left out.");
				continue;
			}

			parsedVectors.Add(new InputVector(parts[0], parts[1], parts[2], parts[3]));
		}

		return new InputActionSet(actions, parsedVectors);
	}

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
	/// Declares the settings so they show up in Project Settings with sensible editing widgets.
	/// </summary>
	/// <remarks>
	/// The initial values are deliberately what an unset project already behaves as, and no value is written here.
	/// ProjectSettings skips saving any property whose value still equals its initial value, so declaring a different
	/// default would keep the setting out of project.godot entirely until it happened to diverge - which is also why
	/// nothing is saved from here: neither declaration can reach the file, and the tag source writes its own setting
	/// when it creates one.
	/// </remarks>
	public static void EnsureRegistered()
	{
		bool sourcesMissing = !ProjectSettings.HasSetting(SourcesSetting);
		bool highlightMissing = !ProjectSettings.HasSetting(HighlightQueryTargetsSetting);

		if (sourcesMissing)
		{
			ProjectSettings.SetSetting(SourcesSetting, Array.Empty<string>());
		}

		if (highlightMissing)
		{
			ProjectSettings.SetSetting(HighlightQueryTargetsSetting, true);
		}

		ProjectSettings.AddPropertyInfo(new GodotDictionary
		{
			{ "name", HighlightQueryTargetsSetting },
			{ "type", (int)Variant.Type.Bool },
		});

		ProjectSettings.SetInitialValue(HighlightQueryTargetsSetting, true);
		ProjectSettings.SetAsBasic(HighlightQueryTargetsSetting, true);

		foreach (PhysicsDebugColor color in PhysicsDebugDraw3D.Colors)
		{
			RegisterColor(color);
		}

		foreach (PhysicsDebugColor color in PhysicsDebugDraw2D.Colors)
		{
			RegisterColor(color);
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

		RegisterStringArray(InputActionsSetting);
		RegisterStringArray(InputVectorsSetting);
	}

	private static void RegisterStringArray(string setting)
	{
		if (!ProjectSettings.HasSetting(setting))
		{
			ProjectSettings.SetSetting(setting, Array.Empty<string>());
		}

		ProjectSettings.AddPropertyInfo(new GodotDictionary
		{
			{ "name", setting },
			{ "type", (int)Variant.Type.PackedStringArray },
		});

		ProjectSettings.SetInitialValue(setting, Array.Empty<string>());
		ProjectSettings.SetAsBasic(setting, true);
	}

	private static void RegisterColor(PhysicsDebugColor color)
	{
		if (!ProjectSettings.HasSetting(color.Setting))
		{
			ProjectSettings.SetSetting(color.Setting, color.Default);
		}

		ProjectSettings.AddPropertyInfo(new GodotDictionary
		{
			{ "name", color.Setting },
			{ "type", (int)Variant.Type.Color },
		});

		ProjectSettings.SetInitialValue(color.Setting, color.Default);
		ProjectSettings.SetAsBasic(color.Setting, true);
	}
#endif
}
