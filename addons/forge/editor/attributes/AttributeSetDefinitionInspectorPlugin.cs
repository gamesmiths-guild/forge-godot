// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Attributes;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Attributes;

/// <summary>
/// Puts the state of an attribute set definition at the top of its inspector: whether it is valid, whether the project
/// still has to be built for it to exist, and a button to regenerate on demand.
/// </summary>
[Tool]
public partial class AttributeSetDefinitionInspectorPlugin : EditorInspectorPlugin
{
	private const string ErrorColor = "error_color";
	private const string WarningColor = "warning_color";
	private const string SuccessColor = "success_color";

	/// <inheritdoc/>
	public override bool _CanHandle(GodotObject @object)
	{
		return @object is ForgeAttributeSetDefinition;
	}

	/// <inheritdoc/>
	public override void _ParseBegin(GodotObject @object)
	{
		if (@object is not ForgeAttributeSetDefinition definition)
		{
			return;
		}

		var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

		var status = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		container.AddChild(status);
		ShowCurrentState(status, definition);

		var button = new Button { Text = "Regenerate Attribute Set Code" };

		// The banner is rewritten in place rather than left for the next inspector rebuild, so pressing the button
		// visibly does something. Without it the label only refreshed after selecting something else and coming back,
		// which reads as the button having done nothing.
		button.Pressed += () => OnRegeneratePressed(status, definition);

		container.AddChild(button);

		AddCustomControl(container);
	}

	private static void OnRegeneratePressed(Label status, ForgeAttributeSetDefinition definition)
	{
		AttributeSetGenerationReport report = AttributeSetCodeGenerator.RegenerateAll();

		foreach (string error in report.Errors)
		{
			GD.PushWarning($"Forge attribute set generation: {error}");
		}

		if (!IsInstanceValid(status) || !IsInstanceValid(definition))
		{
			return;
		}

		if (report.Errors.Count > 0)
		{
			SetStatus(status, string.Join("\n", report.Errors), ErrorColor);
			return;
		}

		var summary = new List<string>();

		if (report.GeneratedSets.Count > 0)
		{
			summary.Add($"Generated {string.Join(", ", report.GeneratedSets)}.");
		}

		if (report.RemovedFiles.Count > 0)
		{
			summary.Add($"Removed {report.RemovedFiles.Count} stale file(s).");
		}

		if (summary.Count == 0)
		{
			SetStatus(status, "Nothing to generate.", WarningColor);
			return;
		}

		summary.Add("Build the project for the changes to take effect.");
		SetStatus(status, string.Join(" ", summary), WarningColor);
	}

	private static void ShowCurrentState(Label status, ForgeAttributeSetDefinition definition)
	{
		string[] errors = AttributeSetCodeGenerator.ValidateInProject(definition);

		if (errors.Length > 0)
		{
			SetStatus(status, string.Join("\n", errors), ErrorColor);
		}
		else if (!AttributeSetCodeGenerator.IsCompiledAndCurrent(definition))
		{
			const string pending = "Build the project for this set to take effect. Attribute pickers already offer it, "
				+ "but nothing can use it at runtime until the class exists.";

			SetStatus(status, pending, WarningColor);
		}
		else
		{
			SetStatus(status, "This set is generated and built.", SuccessColor);
		}
	}

	private static void SetStatus(Label status, string text, string colorName)
	{
		status.Text = text;

		Theme editorTheme = EditorInterface.Singleton.GetEditorTheme();

		if (editorTheme.HasColor(colorName, "Editor"))
		{
			status.AddThemeColorOverride("font_color", editorTheme.GetColor(colorName, "Editor"));
		}
	}
}
#endif
