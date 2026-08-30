// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the direction four opposed input actions point in.
/// </summary>
[Tool]
internal sealed partial class InputVector2ResolverEditor : InputResolverEditorBase
{
	private LineEdit? _leftField;
	private LineEdit? _rightField;
	private LineEdit? _upField;
	private LineEdit? _downField;

	public override string DisplayName => "Input Vector 2";

	public override string ResolverTypeId => "InputVector2";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(NumericsVector2) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new InputVector2ResolverResource
		{
			LeftAction = ReadActionName(_leftField),
			RightAction = ReadActionName(_rightField),
			UpAction = ReadActionName(_upField),
			DownAction = ReadActionName(_downField),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Input Vector 2";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_leftField = null;
		_rightField = null;
		_upField = null;
		_downField = null;
	}

	protected override void BuildRows(VBoxContainer root, StatescriptResolverResource? existingResolver)
	{
		var resource = existingResolver as InputVector2ResolverResource;

		_leftField = AddActionRow(root, "Left:", resource?.LeftAction ?? string.Empty);
		_rightField = AddActionRow(root, "Right:", resource?.RightAction ?? string.Empty);
		_upField = AddActionRow(root, "Up:", resource?.UpAction ?? string.Empty);
		_downField = AddActionRow(root, "Down:", resource?.DownAction ?? string.Empty);
	}
}
#endif
