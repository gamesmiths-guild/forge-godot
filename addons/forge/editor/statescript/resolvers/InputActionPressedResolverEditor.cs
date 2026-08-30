// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads whether an input action is down.
/// </summary>
[Tool]
internal sealed partial class InputActionPressedResolverEditor : InputResolverEditorBase
{
	private static readonly string[] _modeNames = ["Pressed", "Just Pressed", "Just Released"];

	private LineEdit? _actionField;
	private OptionButton? _modeDropdown;

	public override string DisplayName => "Input Action Pressed";

	public override string ResolverTypeId => "InputActionPressed";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new InputActionPressedResolverResource
		{
			ActionName = ReadActionName(_actionField),
			Mode = (InputActionMode)(_modeDropdown?.Selected ?? 0),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		string actionName = ReadActionName(_actionField);
		summary = actionName.Length > 0 ? actionName : "(None)";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_actionField = null;
		_modeDropdown = null;
	}

	protected override void BuildRows(VBoxContainer root, StatescriptResolverResource? existingResolver)
	{
		var resource = existingResolver as InputActionPressedResolverResource;

		_actionField = AddActionRow(root, "Action:", resource?.ActionName ?? string.Empty);
		_modeDropdown = AddEnumRow(root, "Mode:", _modeNames, (int)(resource?.Mode ?? InputActionMode.Pressed));
	}
}
#endif
